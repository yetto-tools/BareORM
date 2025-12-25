using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

/// <summary>
/// Executor de base de datos para SQL Server.
/// Implementa <see cref="IDbExecutor"/> usando <see cref="SqlConnection"/> / <see cref="SqlCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Responsabilidades principales:
/// </para>
/// <list type="bullet">
/// <item><description>Resolver conexión/transacción (soporta transacciones “ambient” vía <c>SqlServerAmbient.Current</c>).</description></item>
/// <item><description>Ejecutar comandos (NonQuery, Scalar, DataTable, DataSet, Reader) en modo sync/async.</description></item>
/// <item><description>Capturar parámetros de salida (<see cref="ParameterDirection.Output"/> / <see cref="ParameterDirection.InputOutput"/>).</description></item>
/// <item><description>Exponer resultados “enterprise” con metadata mediante <see cref="DbResult"/> y <see cref="DbResult{T}"/>.</description></item>
/// </list>
/// <para>
/// Comportamiento con transacción ambient:
/// </para>
/// <list type="bullet">
/// <item><description>Si existe <c>SqlServerAmbient.Current</c>, reutiliza su conexión y transacción y NO la dispone.</description></item>
/// <item><description>Si no existe, crea y abre una conexión propia usando <see cref="IConnectionFactory"/>.</description></item>
/// </list>
/// <para>
/// Nota sobre readers:
/// los outputs de parámetros (OUTPUT) se extraen al cerrar/dispose del reader, ya que SQL Server los materializa al finalizar la ejecución.
/// Por eso se usa <see cref="ReaderWrapper"/> para garantizar el orden correcto y el cierre de recursos.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var executor = new SqlServerExecutor(factory);
/// var cmd = new SqlServerCommandFactory().Create("dbo.sp_CreateUser", CommandType.StoredProcedure, new[]
/// {
///     new DbParam("Email", "a@b.com"),
///     new DbParam("NewId", null) { Direction = ParameterDirection.Output, DbType = DbType.Guid }
/// });
///
/// var result = executor.ExecuteWithMeta(cmd);
/// Console.WriteLine(result.RecordsAffected);
/// Console.WriteLine(result.OutputValues["@NewId"]);
/// </code>
/// </example>
public sealed class SqlServerExecutor : IDbExecutor
{
    private readonly IConnectionFactory _factory;

    /// <summary>
    /// Crea un executor usando una fábrica de conexiones.
    /// </summary>
    /// <param name="factory">Fábrica para crear conexiones <see cref="SqlConnection"/>.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="factory"/> es null.</exception>
    public SqlServerExecutor(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    // ---------------------------
    // Connection/transaction resolve
    // ---------------------------

    /// <summary>
    /// Resuelve si existe una conexión/transacción “ambient” o si el executor debe abrir una conexión propia.
    /// </summary>
    /// <returns>
    /// Tupla con:
    /// <list type="bullet">
    /// <item><description><c>conn</c>: conexión ambient si existe; null si no.</description></item>
    /// <item><description><c>tx</c>: transacción ambient si existe; null si no.</description></item>
    /// <item><description><c>ownsConnection</c>: true si el executor debe crear/poseer conexión propia.</description></item>
    /// </list>
    /// </returns>
    private static (SqlConnection? conn, SqlTransaction? tx, bool ownsConnection) Resolve()
    {
        var ambient = SqlServerAmbient.Current;
        if (ambient is not null)
            return (ambient.Connection, ambient.Transaction, ownsConnection: false);

        return (null, null, ownsConnection: true);
    }

    /// <summary>
    /// Crea un <see cref="SqlCommand"/> a partir de un <see cref="CommandDefinition"/>, con transacción opcional.
    /// </summary>
    private SqlCommand CreateCommand(SqlConnection conn, SqlTransaction? tx, CommandDefinition cmd)
    {
        var sqlCmd = conn.CreateCommand();
        sqlCmd.CommandText = cmd.CommandText;
        sqlCmd.CommandType = cmd.CommandType;
        sqlCmd.CommandTimeout = cmd.TimeoutSeconds;
        sqlCmd.Transaction = tx;

        if (cmd.Parameters is not null)
        {
            foreach (var p in cmd.Parameters)
                sqlCmd.Parameters.Add(p);
        }

        return sqlCmd;
    }

    /// <summary>
    /// Extrae parámetros de salida (OUTPUT / InputOutput / ReturnValue) de un comando ya ejecutado.
    /// </summary>
    /// <param name="cmd">Comando ejecutado.</param>
    /// <returns>Diccionario (case-insensitive) de nombre parámetro → valor (null si DBNull).</returns>
    private static Dictionary<string, object?> ExtractOutput(SqlCommand cmd)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (SqlParameter p in cmd.Parameters)
        {
            if (p.Direction == ParameterDirection.Input) continue;

            var name = p.ParameterName;
            var value = p.Value;
            dict[name] = value is DBNull ? null : value;
        }

        return dict;
    }

    // ---------------------------
    // Classic
    // ---------------------------

    /// <summary>Ejecuta un comando como NonQuery y retorna filas afectadas.</summary>
    public int Execute(CommandDefinition command)
        => ExecuteWithMeta(command).RecordsAffected;

    /// <summary>Ejecuta un comando como NonQuery async y retorna filas afectadas.</summary>
    public async Task<int> ExecuteAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteWithMetaAsync(command, ct)).RecordsAffected;

    /// <summary>Ejecuta un comando scalar y retorna el valor (object).</summary>
    public object? ExecuteScalar(CommandDefinition command)
        => ExecuteScalarWithMeta<object>(command).Data;

    /// <summary>Ejecuta un comando scalar async y retorna el valor (object).</summary>
    public async Task<object?> ExecuteScalarAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteScalarWithMetaAsync<object>(command, ct)).Data;

    /// <summary>Ejecuta un comando y llena un <see cref="DataTable"/>.</summary>
    public DataTable ExecuteDataTable(CommandDefinition command)
        => ExecuteDataTableWithMeta(command).Data;

    /// <summary>Ejecuta un comando y llena un <see cref="DataSet"/>.</summary>
    public DataSet ExecuteDataSet(CommandDefinition command)
        => ExecuteDataSetWithMeta(command).Data;

    /// <summary>
    /// Ejecuta un comando y retorna un <see cref="IDataReader"/>.
    /// </summary>
    /// <remarks>
    /// El caller debe disponer el reader para liberar recursos y materializar outputs.
    /// </remarks>
    public IDataReader ExecuteReader(CommandDefinition command)
        => ExecuteReaderWithMeta(command).Data;

    /// <summary>
    /// Ejecuta un comando y retorna un <see cref="IDataReader"/> async.
    /// </summary>
    public async Task<IDataReader> ExecuteReaderAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteReaderWithMetaAsync(command, ct)).Data;

    // ---------------------------
    // WithMeta (enterprise)
    // ---------------------------

    /// <summary>
    /// Ejecuta NonQuery retornando <see cref="DbResult"/> con filas afectadas y outputs.
    /// </summary>
    public DbResult ExecuteWithMeta(CommandDefinition command)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            using var cmd = CreateCommand(ambientConn!, tx, command);
            var affected = cmd.ExecuteNonQuery();
            var outputs = ExtractOutput(cmd);
            return new DbResult(affected, outputs);
        }

        using var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        using var sqlCmd = CreateCommand(conn, tx: null, command);
        var records = sqlCmd.ExecuteNonQuery();
        var output = ExtractOutput(sqlCmd);
        return new DbResult(records, output);
    }

    /// <summary>
    /// Ejecuta NonQuery async retornando <see cref="DbResult"/> con filas afectadas y outputs.
    /// </summary>
    public async Task<DbResult> ExecuteWithMetaAsync(CommandDefinition command, CancellationToken ct = default)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            await using var cmd = CreateCommand(ambientConn!, tx, command);
            var affected = await cmd.ExecuteNonQueryAsync(ct);
            var outputs = ExtractOutput(cmd);
            return new DbResult(affected, outputs);
        }

        await using var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);

        await using var sqlCmd = CreateCommand(conn, tx: null, command);
        var records = await sqlCmd.ExecuteNonQueryAsync(ct);
        var output = ExtractOutput(sqlCmd);
        return new DbResult(records, output);
    }

    /// <summary>
    /// Ejecuta Scalar y retorna <see cref="DbResult{T}"/> con el valor convertido y outputs.
    /// </summary>
    /// <typeparam name="T">Tipo esperado del scalar.</typeparam>
    public DbResult<T?> ExecuteScalarWithMeta<T>(CommandDefinition command)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            using var cmd = CreateCommand(ambientConn!, tx, command);
            var value = cmd.ExecuteScalar();
            var outputs = ExtractOutput(cmd);
            return new DbResult<T?>(ConvertScalar<T>(value), 0, outputs);
        }

        using var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        using var sqlCmd = CreateCommand(conn, tx: null, command);
        var v = sqlCmd.ExecuteScalar();
        var o = ExtractOutput(sqlCmd);
        return new DbResult<T?>(ConvertScalar<T>(v), 0, o);
    }

    /// <summary>
    /// Ejecuta Scalar async y retorna <see cref="DbResult{T}"/> con el valor convertido y outputs.
    /// </summary>
    /// <typeparam name="T">Tipo esperado del scalar.</typeparam>
    public async Task<DbResult<T?>> ExecuteScalarWithMetaAsync<T>(CommandDefinition command, CancellationToken ct = default)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            await using var cmd = CreateCommand(ambientConn!, tx, command);
            var value = await cmd.ExecuteScalarAsync(ct);
            var outputs = ExtractOutput(cmd);
            return new DbResult<T?>(ConvertScalar<T>(value), 0, outputs);
        }

        await using var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);

        await using var sqlCmd = CreateCommand(conn, tx: null, command);
        var v = await sqlCmd.ExecuteScalarAsync(ct);
        var o = ExtractOutput(sqlCmd);
        return new DbResult<T?>(ConvertScalar<T>(v), 0, o);
    }

    /// <summary>
    /// Ejecuta un comando y llena un <see cref="DataTable"/>, retornando outputs.
    /// </summary>
    public DbResult<DataTable> ExecuteDataTableWithMeta(CommandDefinition command)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            using var cmd = CreateCommand(ambientConn!, tx, command);
            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            var outputs = ExtractOutput(cmd);
            return new DbResult<DataTable>(table, 0, outputs);
        }

        using var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        using var sqlCmd = CreateCommand(conn, tx: null, command);
        using var adapter2 = new SqlDataAdapter(sqlCmd);
        var dt = new DataTable();
        adapter2.Fill(dt);
        var out2 = ExtractOutput(sqlCmd);
        return new DbResult<DataTable>(dt, 0, out2);
    }

    /// <summary>
    /// Ejecuta un comando y llena un <see cref="DataSet"/>, retornando outputs.
    /// </summary>
    public DbResult<DataSet> ExecuteDataSetWithMeta(CommandDefinition command)
    {
        var (ambientConn, tx, owns) = Resolve();

        if (!owns)
        {
            using var cmd = CreateCommand(ambientConn!, tx, command);
            using var adapter = new SqlDataAdapter(cmd);
            var ds = new DataSet();
            adapter.Fill(ds);
            var outputs = ExtractOutput(cmd);
            return new DbResult<DataSet>(ds, 0, outputs);
        }

        using var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        using var sqlCmd = CreateCommand(conn, tx: null, command);
        using var adapter2 = new SqlDataAdapter(sqlCmd);
        var ds2 = new DataSet();
        adapter2.Fill(ds2);
        var out2 = ExtractOutput(sqlCmd);
        return new DbResult<DataSet>(ds2, 0, out2);
    }

    /// <summary>
    /// Ejecuta un comando retornando <see cref="IDataReader"/> y un diccionario de outputs.
    /// </summary>
    /// <remarks>
    /// Los outputs se materializan al cerrar/dispose del reader (ver <see cref="ReaderWrapper"/>).
    /// </remarks>
    public DbResult<IDataReader> ExecuteReaderWithMeta(CommandDefinition command)
    {
        var (ambientConn, tx, owns) = Resolve();

        // Diccionario mutable que se llena al Dispose del reader (outputs disponibles al cerrar reader)
        var outputs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (!owns)
        {
            var cmd = CreateCommand(ambientConn!, tx, command);
            var reader = cmd.ExecuteReader(command.ReaderBehavior);

            return new DbResult<IDataReader>(
                new ReaderWrapper(reader, cmd, closeConnection: false, fillOutputs: () =>
                {
                    foreach (var kv in ExtractOutput(cmd)) outputs[kv.Key] = kv.Value;
                }),
                0,
                outputs
            );
        }

        var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        var sqlCmd = CreateCommand(conn, tx: null, command);

        // CloseConnection asegura cerrar conexión al cerrar reader (pero igual limpiamos todo con wrapper)
        var behavior = command.ReaderBehavior | CommandBehavior.CloseConnection;
        var rdr = sqlCmd.ExecuteReader(behavior);

        return new DbResult<IDataReader>(
            new ReaderWrapper(rdr, sqlCmd, closeConnection: true, fillOutputs: () =>
            {
                foreach (var kv in ExtractOutput(sqlCmd)) outputs[kv.Key] = kv.Value;
            }),
            0,
            outputs
        );
    }

    /// <summary>
    /// Ejecuta un comando retornando <see cref="IDataReader"/> async y un diccionario de outputs.
    /// </summary>
    public async Task<DbResult<IDataReader>> ExecuteReaderWithMetaAsync(CommandDefinition command, CancellationToken ct = default)
    {
        var (ambientConn, tx, owns) = Resolve();
        var outputs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (!owns)
        {
            var cmd = CreateCommand(ambientConn!, tx, command);
            var reader = await cmd.ExecuteReaderAsync(command.ReaderBehavior, ct);

            return new DbResult<IDataReader>(
                new ReaderWrapper(reader, cmd, closeConnection: false, fillOutputs: () =>
                {
                    foreach (var kv in ExtractOutput(cmd)) outputs[kv.Key] = kv.Value;
                }),
                0,
                outputs
            );
        }

        var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);

        var sqlCmd = CreateCommand(conn, tx: null, command);
        var behavior = command.ReaderBehavior | CommandBehavior.CloseConnection;
        var rdr = await sqlCmd.ExecuteReaderAsync(behavior, ct);

        return new DbResult<IDataReader>(
            new ReaderWrapper(rdr, sqlCmd, closeConnection: true, fillOutputs: () =>
            {
                foreach (var kv in ExtractOutput(sqlCmd)) outputs[kv.Key] = kv.Value;
            }),
            0,
            outputs
        );
    }

    /// <summary>
    /// Convierte un valor de <see cref="SqlCommand.ExecuteScalar"/> al tipo esperado.
    /// </summary>
    /// <typeparam name="T">Tipo destino.</typeparam>
    /// <param name="value">Valor obtenido del scalar.</param>
    /// <returns>Valor convertido o default si null/DBNull.</returns>
    private static T? ConvertScalar<T>(object? value)
    {
        if (value is null || value is DBNull) return default;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    /// Libera recursos del executor.
    /// </summary>
    /// <remarks>
    /// Esta implementación es stateless: no mantiene recursos por instancia.
    /// </remarks>
    public void Dispose()
    {
        // Executor es stateless; no mantiene recursos por instancia.
    }

    /// <summary>
    /// Wrapper de <see cref="IDataReader"/> para garantizar el Dispose correcto del reader,
    /// del comando, y (si aplica) de la conexión, además de capturar outputs al final.
    /// </summary>
    private sealed class ReaderWrapper : IDataReader
    {
        private readonly IDataReader _inner;
        private readonly SqlCommand _cmd;
        private readonly bool _closeConnection;
        private readonly Action _fillOutputs;
        private bool _disposed;

        /// <summary>
        /// Crea un wrapper de reader.
        /// </summary>
        /// <param name="inner">Reader real.</param>
        /// <param name="cmd">Comando asociado.</param>
        /// <param name="closeConnection">Si true, se dispone la conexión al cerrar el reader.</param>
        /// <param name="fillOutputs">Acción que copia los outputs del comando al diccionario externo.</param>
        public ReaderWrapper(IDataReader inner, SqlCommand cmd, bool closeConnection, Action fillOutputs)
        {
            _inner = inner;
            _cmd = cmd;
            _closeConnection = closeConnection;
            _fillOutputs = fillOutputs;
        }

        /// <summary>
        /// Dispone el reader, captura outputs y libera comando/conexión según corresponda.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _inner.Dispose();
            }
            finally
            {
                try { _fillOutputs(); } catch { /* ignore */ }
                _cmd.Dispose();

                if (_closeConnection)
                    _cmd.Connection?.Dispose();
            }
        }

        // IDataReader passthrough
        public bool Read() => _inner.Read();
        public bool NextResult() => _inner.NextResult();
        public int Depth => _inner.Depth;
        public bool IsClosed => _inner.IsClosed;
        public int RecordsAffected => _inner.RecordsAffected;
        public void Close() => _inner.Close();
        public DataTable GetSchemaTable() => _inner.GetSchemaTable();
        public bool GetBoolean(int i) => ((IDataRecord)_inner).GetBoolean(i);
        public byte GetByte(int i) => ((IDataRecord)_inner).GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => ((IDataRecord)_inner).GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        public char GetChar(int i) => ((IDataRecord)_inner).GetChar(i);
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => ((IDataRecord)_inner).GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public IDataReader GetData(int i) => ((IDataRecord)_inner).GetData(i);
        public string GetDataTypeName(int i) => ((IDataRecord)_inner).GetDataTypeName(i);
        public DateTime GetDateTime(int i) => ((IDataRecord)_inner).GetDateTime(i);
        public decimal GetDecimal(int i) => ((IDataRecord)_inner).GetDecimal(i);
        public double GetDouble(int i) => ((IDataRecord)_inner).GetDouble(i);
        public Type GetFieldType(int i) => ((IDataRecord)_inner).GetFieldType(i);
        public float GetFloat(int i) => ((IDataRecord)_inner).GetFloat(i);
        public Guid GetGuid(int i) => ((IDataRecord)_inner).GetGuid(i);
        public short GetInt16(int i) => ((IDataRecord)_inner).GetInt16(i);
        public int GetInt32(int i) => ((IDataRecord)_inner).GetInt32(i);
        public long GetInt64(int i) => ((IDataRecord)_inner).GetInt64(i);
        public string GetName(int i) => ((IDataRecord)_inner).GetName(i);
        public int GetOrdinal(string name) => ((IDataRecord)_inner).GetOrdinal(name);
        public string GetString(int i) => ((IDataRecord)_inner).GetString(i);
        public object GetValue(int i) => ((IDataRecord)_inner).GetValue(i);
        public int GetValues(object[] values) => ((IDataRecord)_inner).GetValues(values);
        public bool IsDBNull(int i) => ((IDataRecord)_inner).IsDBNull(i);
        public int FieldCount => ((IDataRecord)_inner).FieldCount;
        public object this[int i] => ((IDataRecord)_inner)[i];
        public object this[string name] => ((IDataRecord)_inner)[name];
    }
}
