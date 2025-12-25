using System.Data;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;
using BareORM.SqlServer.Internal;

namespace BareORM.SqlServer;

/// <summary>
/// Administrador de transacciones para SQL Server basado en “ambient transaction”.
/// </summary>
/// <remarks>
/// <para>
/// Implementa <see cref="ITransactionManager"/> y mantiene el estado transaccional en el contexto “ambient”
/// (<c>SqlServerAmbient.Current</c>). Esto permite que otros componentes (por ejemplo <c>SqlServerExecutor</c>
/// o <c>SqlServerBulkProvider</c>) reutilicen la misma conexión/transacción sin que tengas que pasarlas
/// explícitamente en cada llamada.
/// </para>
/// <para>
/// Flujo típico:
/// </para>
/// <list type="number">
/// <item><description><see cref="Begin"/> / <see cref="BeginAsync"/>: abre conexión y crea una transacción.</description></item>
/// <item><description>Ejecutas queries/commands/bulk usando otros servicios (ellos detectan el ambient).</description></item>
/// <item><description><see cref="Commit"/> / <see cref="Rollback"/>: cierra la transacción y limpia recursos.</description></item>
/// </list>
/// <para>
/// Reglas importantes:
/// </para>
/// <list type="bullet">
/// <item><description>No soporta transacciones anidadas: si ya existe una activa, lanza excepción.</description></item>
/// <item><description>Los recursos se liberan en <c>Cleanup()</c> (Dispose de tx + connection) luego de Commit/Rollback.</description></item>
/// <item><description><see cref="Dispose"/> intenta hacer rollback si queda una transacción activa.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var tm = new SqlServerTransactionManager(factory);
/// tm.Begin();
/// try
/// {
///     executor.Execute(cmd1);
///     bulk.BulkInsert("dbo.Users", dt);
///     tm.Commit();
/// }
/// catch
/// {
///     tm.Rollback();
///     throw;
/// }
/// </code>
/// </example>
public sealed class SqlServerTransactionManager : ITransactionManager
{
    private readonly IConnectionFactory _factory;

    /// <summary>
    /// Indica si existe una transacción activa en el contexto actual.
    /// </summary>
    public bool HasActiveTransaction => SqlServerAmbient.Current is not null;

    /// <summary>
    /// Crea el manager con una fábrica de conexiones.
    /// </summary>
    /// <param name="factory">Fábrica que produce <see cref="SqlConnection"/>.</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="factory"/> es null.</exception>
    public SqlServerTransactionManager(IConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Inicia una transacción en el contexto ambient, abriendo una conexión nueva.
    /// </summary>
    /// <param name="isolationLevel">Isolation level (default: <see cref="IsolationLevel.ReadCommitted"/>).</param>
    /// <exception cref="InvalidOperationException">Si ya existe una transacción activa.</exception>
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

    /// <summary>
    /// Inicia una transacción async en el contexto ambient, abriendo una conexión nueva.
    /// </summary>
    /// <param name="isolationLevel">Isolation level (default: <see cref="IsolationLevel.ReadCommitted"/>).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <exception cref="InvalidOperationException">Si ya existe una transacción activa.</exception>
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

    /// <summary>
    /// Confirma la transacción activa y libera recursos.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay transacción activa.</exception>
    public void Commit()
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        ctx.Transaction.Commit();
        Cleanup();
    }

    /// <summary>
    /// Confirma la transacción activa async y libera recursos.
    /// </summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        await ctx.Transaction.CommitAsync(ct);
        Cleanup();
    }

    /// <summary>
    /// Revierte (rollback) la transacción activa y libera recursos.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si no hay transacción activa.</exception>
    public void Rollback()
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        ctx.Transaction.Rollback();
        Cleanup();
    }

    /// <summary>
    /// Revierte (rollback) la transacción activa async y libera recursos.
    /// </summary>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        var ctx = SqlServerAmbient.Current ?? throw new InvalidOperationException("No active transaction.");
        await ctx.Transaction.RollbackAsync(ct);
        Cleanup();
    }

    /// <summary>
    /// Limpia el contexto ambient y dispone transacción y conexión.
    /// </summary>
    /// <remarks>
    /// Se ejecuta luego de Commit/Rollback para evitar fugas de conexión.
    /// </remarks>
    private static void Cleanup()
    {
        var ctx = SqlServerAmbient.Current;
        SqlServerAmbient.Current = null;

        if (ctx is null) return;

        ctx.Transaction.Dispose();
        ctx.Connection.Dispose();
    }

    /// <summary>
    /// Dispose defensivo: si queda una transacción activa, intenta hacer rollback.
    /// </summary>
    public void Dispose()
    {
        if (!HasActiveTransaction) return;

        try { Rollback(); }
        catch { /* swallow on dispose */ }
    }
}
