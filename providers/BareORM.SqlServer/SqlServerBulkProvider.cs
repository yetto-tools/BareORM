using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

public sealed class SqlServerBulkProvider : IBulkProvider
{
    private readonly IConnectionFactory _factory;

    public SqlServerBulkProvider(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

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

    private static string[] GetKeyColumnsOrThrow(DataTable data)
    {
        if (data.ExtendedProperties["KeyColumns"] is string[] keys && keys.Length > 0)
            return keys;

        throw new InvalidOperationException(
            "BulkMerge requires key columns. Set: data.ExtendedProperties[\"KeyColumns\"] = new[] { \"Id\", ... };");
    }

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

    private static void CreateStagingTable(SqlConnection conn, SqlTransaction? tx, string targetTable, string stageTable)
    {
        var sql = $"SELECT TOP (0) * INTO {stageTable} FROM {targetTable};";
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.ExecuteNonQuery();
    }

    private static async Task CreateStagingTableAsync(SqlConnection conn, SqlTransaction? tx, string targetTable, string stageTable, CancellationToken ct)
    {
        var sql = $"SELECT TOP (0) * INTO {stageTable} FROM {targetTable};";
        await using var cmd = new SqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static void DropStagingTable(SqlConnection conn, SqlTransaction? tx, string stageTable)
    {
        var sql = $"DROP TABLE {stageTable};";
        using var cmd = new SqlCommand(sql, conn, tx);
        cmd.ExecuteNonQuery();
    }

    private static async Task DropStagingTableAsync(SqlConnection conn, SqlTransaction? tx, string stageTable, CancellationToken ct)
    {
        var sql = $"DROP TABLE {stageTable};";
        await using var cmd = new SqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string BuildMergeSql(string targetTable, string stageTable, string[] keyCols, string[] updateCols, string[] allCols)
    {
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
