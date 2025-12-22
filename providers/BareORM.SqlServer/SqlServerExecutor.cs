using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

public sealed class SqlServerExecutor : IDbExecutor
{
    private readonly IConnectionFactory _factory;

    public SqlServerExecutor(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    // ---------------------------
    // Connection/transaction resolve
    // ---------------------------
    private static (SqlConnection? conn, SqlTransaction? tx, bool ownsConnection) Resolve()
    {
        var ambient = SqlServerAmbient.Current;
        if (ambient is not null)
            return (ambient.Connection, ambient.Transaction, ownsConnection: false);

        return (null, null, ownsConnection: true);
    }

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
    public int Execute(CommandDefinition command)
        => ExecuteWithMeta(command).RecordsAffected;

    public async Task<int> ExecuteAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteWithMetaAsync(command, ct)).RecordsAffected;

    public object? ExecuteScalar(CommandDefinition command)
        => ExecuteScalarWithMeta<object>(command).Data;

    public async Task<object?> ExecuteScalarAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteScalarWithMetaAsync<object>(command, ct)).Data;

    public DataTable ExecuteDataTable(CommandDefinition command)
        => ExecuteDataTableWithMeta(command).Data;

    public DataSet ExecuteDataSet(CommandDefinition command)
        => ExecuteDataSetWithMeta(command).Data;

    public IDataReader ExecuteReader(CommandDefinition command)
        => ExecuteReaderWithMeta(command).Data;

    public async Task<IDataReader> ExecuteReaderAsync(CommandDefinition command, CancellationToken ct = default)
        => (await ExecuteReaderWithMetaAsync(command, ct)).Data;

    // ---------------------------
    // WithMeta (enterprise)
    // ---------------------------
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

    private static T? ConvertScalar<T>(object? value)
    {
        if (value is null || value is DBNull) return default;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    public void Dispose()
    {
        // Executor es stateless; no mantiene recursos por instancia.
    }

    // Wrapper para garantizar Dispose correcto de command y (si aplica) outputs luego de cerrar reader
    private sealed class ReaderWrapper : IDataReader
    {
        private readonly IDataReader _inner;
        private readonly SqlCommand _cmd;
        private readonly bool _closeConnection;
        private readonly Action _fillOutputs;
        private bool _disposed;

        public ReaderWrapper(IDataReader inner, SqlCommand cmd, bool closeConnection, Action fillOutputs)
        {
            _inner = inner;
            _cmd = cmd;
            _closeConnection = closeConnection;
            _fillOutputs = fillOutputs;
        }

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
