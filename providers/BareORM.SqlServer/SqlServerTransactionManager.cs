using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

public sealed class SqlServerTransactionManager : ITransactionManager
{
    private readonly IConnectionFactory _factory;

    public bool HasActiveTransaction => SqlServerAmbient.Current is not null;

    public SqlServerTransactionManager(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public void Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        if (HasActiveTransaction)
            throw new InvalidOperationException("A transaction is already active in this async context.");

        var conn = (SqlConnection)_factory.CreateConnection();
        conn.Open();

        var tx = conn.BeginTransaction(isolationLevel);

        SqlServerAmbient.Current = new SqlServerAmbientContext
        {
            Connection = conn,
            Transaction = tx
        };
    }

    public async Task BeginAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (HasActiveTransaction)
            throw new InvalidOperationException("A transaction is already active in this async context.");

        var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);

        var tx = (SqlTransaction)await conn.BeginTransactionAsync(isolationLevel, ct);

        SqlServerAmbient.Current = new SqlServerAmbientContext
        {
            Connection = conn,
            Transaction = tx
        };
    }

    public void Commit()
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        ctx.Transaction.Commit();
        Cleanup();
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        await ctx.Transaction.CommitAsync(ct);
        Cleanup();
    }

    public void Rollback()
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        ctx.Transaction.Rollback();
        Cleanup();
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        await ctx.Transaction.RollbackAsync(ct);
        Cleanup();
    }

    private static void Cleanup()
    {
        var ctx = SqlServerAmbient.Current;
        SqlServerAmbient.Current = null;

        if (ctx is null) return;

        ctx.Transaction.Dispose();
        ctx.Connection.Dispose();
    }

    public void Dispose()
    {
        if (!HasActiveTransaction) return;

        try { Rollback(); }
        catch { /* swallow on dispose */ }
    }
}
