using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

/// <summary>
/// Implementación de <see cref="IBulkProvider"/> para SQL Server basada en <see cref="SqlBulkCopy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provee operaciones de alta performance para carga masiva:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="BulkInsert(string, DataTable, BulkOptions?)"/>: inserta filas de forma directa.</description></item>
/// <item><description><see cref="BulkMerge(string, DataTable, BulkOptions?)"/>: hace upsert (MERGE) usando una tabla staging temporal.</description></item>
/// </list>
/// <para>
/// Soporta “transacción ambiente” a través de <see cref="SqlServerAmbient"/>:
/// si existe una conexión/transacción activa, las operaciones se ejecutan dentro de ella.
/// Si no existe, el provider abre/cierra su propia conexión.
/// </para>
/// <para>
/// Requisitos para <see cref="BulkMerge(string, DataTable, BulkOptions?)"/>:
/// debes indicar columnas llave (match) mediante:
/// <c>data.ExtendedProperties["KeyColumns"] = new[] { "Id", ... };</c>
/// </para>
/// <para>
/// Nota: este provider asume que los nombres de columna en el <see cref="DataTable"/> coinciden con los nombres de columna
/// en la tabla destino (o que el destino acepta el mismo orden/estructura). Si necesitas mapeos, debes configurarlos antes
/// (p.ej. DataTable con columnas ya alineadas).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dt = new DataTable();
/// dt.Columns.Add("Id", typeof(Guid));
/// dt.Columns.Add("Email", typeof(string));
/// dt.Rows.Add(Guid.NewGuid(), "a@b.com");
///
/// // Insert
/// bulkProvider.BulkInsert("[dbo].[Users]", dt);
///
/// // Merge (Upsert): requiere llaves
/// dt.ExtendedProperties["KeyColumns"] = new[] { "Id" };
/// bulkProvider.BulkMerge("[dbo].[Users]", dt);
/// </code>
/// </example>
public sealed class SqlServerBulkProvider : IBulkProvider
{
    private readonly IConnectionFactory _factory;

    /// <summary>
    /// Crea una instancia de <see cref="SqlServerBulkProvider"/>.
    /// </summary>
    /// <param name="factory">Factory para crear conexiones a SQL Server.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="factory"/> es null.</exception>
    public SqlServerBulkProvider(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Inserta masivamente filas en <paramref name="tableName"/> usando <see cref="SqlBulkCopy"/>.
    /// </summary>
    /// <param name="tableName">
    /// Tabla destino (recomendado: nombre calificado, p.ej. <c>[dbo].[Users]</c>).
    /// </param>
    /// <param name="data">Datos a insertar.</param>
    /// <param name="options">Opciones de bulk (batch size, timeout, table lock, etc.).</param>
    /// <remarks>
    /// Ejecuta dentro de una transacción ambiente si existe (ver <see cref="SqlServerAmbient"/>).
    /// </remarks>
    public void BulkInsert(string tableName, DataTable data, BulkOptions? options = null)
    {
        options ??= new BulkOptions();
        ExecuteWithConnection((conn, tx) =>
        {
            using var bulk = CreateBulkCopy(conn, tx, options);
            bulk.DestinationTableName = tableName;
            bulk.WriteToServer(data);
        });
    }

    /// <summary>
    /// Inserta masivamente filas en <paramref name="tableName"/> de forma asíncrona.
    /// </summary>
    /// <param name="tableName">Tabla destino (recomendado: nombre calificado, p.ej. <c>[dbo].[Users]</c>).</param>
    /// <param name="data">Datos a insertar.</param>
    /// <param name="options">Opciones de bulk.</param>
    /// <param name="ct">Token de cancelación.</param>
    public async Task BulkInsertAsync(string tableName, DataTable data, BulkOptions? options = null, CancellationToken ct = default)
    {
        options ??= new BulkOptions();
        await ExecuteWithConnectionAsync(async (conn, tx) =>
        {
            using var bulk = CreateBulkCopy(conn, tx, options);
            bulk.DestinationTableName = tableName;
            await bulk.WriteToServerAsync(data, ct);
        }, ct);
    }

    /// <summary>
    /// Realiza un upsert masivo (MERGE): actualiza filas existentes y crea nuevas filas si no existen.
    /// </summary>
    /// <param name="tableName">
    /// Tabla destino (recomendado: nombre calificado, p.ej. <c>[dbo].[Users]</c>).
    /// </param>
    /// <param name="data">Datos fuente.</param>
    /// <param name="options">Opciones de bulk.</param>
    /// <remarks>
    /// <para>
    /// Flujo:
    /// </para>
    /// <list type="number">
    /// <item><description>Lee columnas llave desde <c>data.ExtendedProperties["KeyColumns"]</c>.</description></item>
    /// <item><description>Crea una staging table temporal (<c>#stg_...</c>) con la misma estructura que la tabla destino.</description></item>
    /// <item><description>Carga el <see cref="DataTable"/> a la staging con <see cref="SqlBulkCopy"/>.</description></item>
    /// <item><description>Ejecuta un <c>MERGE</c> usando las columnas llave para el match.</description></item>
    /// <item><description>Elimina la staging table.</description></item>
    /// </list>
    /// <para>
    /// Si no hay columnas para actualizar (solo llaves), el MERGE solo insertará las filas no existentes.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Si no se definen columnas llave en <c>data.ExtendedProperties["KeyColumns"]</c>.
    /// </exception>
    public void BulkMerge(string tableName, DataTable data, BulkOptions? options = null)
    {
        options ??= new BulkOptions();
        ExecuteWithConnection((conn, tx) =>
        {
            var keyCols = GetKeyColumnsOrThrow(data);

            var stage = "#stg_" + Guid.NewGuid().ToString("N");
            CreateStagingTable(conn, tx, tableName, stage);

            using (var bulk = CreateBulkCopy(conn, tx, options))
            {
                bulk.DestinationTableName = stage;
                bulk.WriteToServer(data);
            }

            var allCols = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            var updateCols = allCols.Where(c => !keyCols.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();

            var mergeSql = BuildMergeSql(tableName, stage, keyCols, updateCols, allCols);
            using var cmd = new SqlCommand(mergeSql, conn, tx);
            cmd.ExecuteNonQuery();

            DropStagingTable(conn, tx, stage);
        });
    }

    /// <summary>
    /// Realiza un upsert masivo (MERGE) de forma asíncrona.
    /// </summary>
    /// <param name="tableName">Tabla destino (recomendado: nombre calificado, p.ej. <c>[dbo].[Users]</c>).</param>
    /// <param name="data">Datos fuente.</param>
    /// <param name="options">Opciones de bulk.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="InvalidOperationException">
    /// Si no se definen columnas llave en <c>data.ExtendedProperties["KeyColumns"]</c>.
    /// </exception>
    public async Task BulkMergeAsync(string tableName, DataTable data, BulkOptions? options = null, CancellationToken ct = default)
    {
        options ??= new BulkOptions();
        await ExecuteWithConnectionAsync(async (conn, tx) =>
        {
            var keyCols = GetKeyColumnsOrThrow(data);

            var stage = "#stg_" + Guid.NewGuid().ToString("N");
            await CreateStagingTableAsync(conn, tx, tableName, stage, ct);

            using (var bulk = CreateBulkCopy(conn, tx, options))
            {
                bulk.DestinationTableName = stage;
                await bulk.WriteToServerAsync(data, ct);
            }

            var allCols = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            var updateCols = allCols.Where(c => !keyCols.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();

            var mergeSql = BuildMergeSql(tableName, stage, keyCols, updateCols, allCols);
            await using var cmd = new SqlCommand(mergeSql, conn, tx);
            await cmd.ExecuteNonQueryAsync(ct);

            await DropStagingTableAsync(conn, tx, stage, ct);
        }, ct);
    }

    /// <summary>
    /// Obtiene las columnas llave necesarias para un MERGE desde <see cref="DataTable.ExtendedProperties"/>.
    /// </summary>
    /// <param name="data">DataTable fuente.</param>
    /// <returns>Arreglo de nombres de columnas llave.</returns>
    /// <exception cref="InvalidOperationException">
    /// Si no se especifican llaves en <c>data.ExtendedProperties["KeyColumns"]</c>.
    /// </exception>
    private static string[] GetKeyColumnsOrThrow(DataTable data)
    {
        if (data.ExtendedProperties["KeyColumns"] is string[] keys && keys.Length > 0)
            return keys;

        throw new InvalidOperationException(
            "BulkMerge requires key columns. Set: data.ExtendedProperties[\"KeyColumns\"] = new[] { \"Id\", ... };");
    }

    /// <summary>
    /// Crea y configura una instancia de <see cref="SqlBulkCopy"/> según <paramref name="options"/>.
    /// </summary>
    /// <param name="conn">Conexión abierta.</param>
    /// <param name="tx">Transacción actual (opcional).</param>
    /// <param name="options">Opciones de bulk.</param>
    /// <returns>Instancia lista para usar.</returns>
    private static SqlBulkCopy CreateBulkCopy(SqlConnection conn, SqlTransaction? tx, BulkOptions options)
    {
        var sqlOptions = SqlBulkCopyOptions.Default;

        if (options.KeepIdentity) sqlOptions |= SqlBulkCopyOptions.KeepIdentity;
        if (options.TableLock) sqlOptions |= SqlBulkCopyOptions.TableLock;
        if (!options.UseInternalTransaction) { /* keep default */ }
        else sqlOptions |= SqlBulkCopyOptions.UseInternalTransaction;

        var bulk = tx is null ? new SqlBulkCopy(conn, sqlOptions, null) : new SqlBulkCopy(conn, sqlOptions, tx);
        bulk.BatchSize = options.BatchSize;
        bulk.BulkCopyTimeout = options.TimeoutSeconds;
        return bulk;
    }

    /// <summary>
    /// Crea una tabla staging temporal (local temp table) con la misma estructura que la tabla destino.
    /// </summary>
    /// <remarks>
    /// Usa <c>SELECT TOP (0) * INTO #stage FROM target</c>.
    /// </remarks>
    private static void CreateStagingTable(SqlConnection conn, SqlTransaction? tx, string targetTable, string stageTable)
    {
        var sql = $"SELECT TOP (0) * INTO {stageTable} FROM {targetTable};";
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Versión asíncrona de <see cref="CreateStagingTable(SqlConnection, SqlTransaction?, string, string)"/>.
    /// </summary>
    private static async Task CreateStagingTableAsync(SqlConnection conn, SqlTransaction? tx, string targetTable, string stageTable, CancellationToken ct)
    {
        var sql = $"SELECT TOP (0) * INTO {stageTable} FROM {targetTable};";
        await using var cmd = new SqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Elimina la tabla staging temporal.
    /// </summary>
    private static void DropStagingTable(SqlConnection conn, SqlTransaction? tx, string stageTable)
    {
        var sql = $"DROP TABLE {stageTable};";
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Versión asíncrona de <see cref="DropStagingTable(SqlConnection, SqlTransaction?, string)"/>.
    /// </summary>
    private static async Task DropStagingTableAsync(SqlConnection conn, SqlTransaction? tx, string stageTable, CancellationToken ct)
    {
        var sql = $"DROP TABLE {stageTable};";
        await using var cmd = new SqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Construye el SQL de MERGE (upsert) entre la tabla destino y la staging table.
    /// </summary>
    /// <param name="targetTable">Tabla destino.</param>
    /// <param name="stageTable">Tabla staging temporal.</param>
    /// <param name="keyCols">Columnas llave para el match (ON).</param>
    /// <param name="updateCols">Columnas actualizables (excluye llaves).</param>
    /// <param name="allCols">Todas las columnas (para INSERT).</param>
    /// <returns>SQL MERGE listo para ejecutar.</returns>
    private static string BuildMergeSql(string targetTable, string stageTable, string[] keyCols, string[] updateCols, string[] allCols)
    {
        // Escapa nombres de columna para SQL Server (] -> ]])
        static string Q(string c) => $"[{c.Replace("]", "]]")}]";

        var on = string.Join(" AND ", keyCols.Select(k => $"T.{Q(k)} = S.{Q(k)}"));
        var set = updateCols.Length == 0 ? "" : string.Join(", ", updateCols.Select(c => $"T.{Q(c)} = S.{Q(c)}"));

        var insertCols = string.Join(", ", allCols.Select(Q));
        var insertVals = string.Join(", ", allCols.Select(c => $"S.{Q(c)}"));

        var whenMatched = updateCols.Length == 0 ? "" : $"WHEN MATCHED THEN UPDATE SET {set}";
        return $@"
MERGE {targetTable} AS T
USING {stageTable} AS S
ON {on}
{whenMatched}
WHEN NOT MATCHED THEN
  INSERT ({insertCols}) VALUES ({insertVals});
";
    }

    /// <summary>
    /// Ejecuta una acción con una conexión abierta, reutilizando transacción ambiente si existe.
    /// </summary>
    private void ExecuteWithConnection(Action<SqlConnection, SqlTransaction?> action)
    {
        var ambient = SqlServerAmbient.Current;
        if (ambient is not null)
        {
            action(ambient.Connection, ambient.Transaction);
            return;
        }

        using var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();
        action(conn, null);
    }

    /// <summary>
    /// Ejecuta una acción asíncrona con una conexión abierta, reutilizando transacción ambiente si existe.
    /// </summary>
    private async Task ExecuteWithConnectionAsync(Func<SqlConnection, SqlTransaction?, Task> action, CancellationToken ct)
    {
        var ambient = SqlServerAmbient.Current;
        if (ambient is not null)
        {
            await action(ambient.Connection, ambient.Transaction);
            return;
        }

        await using var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);
        await action(conn, null);
    }
}
