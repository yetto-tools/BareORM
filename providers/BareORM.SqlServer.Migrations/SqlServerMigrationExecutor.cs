using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Ejecuta batches SQL de migración en SQL Server con soporte transaccional.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta clase es un adaptador sobre <see cref="SqlServerMigrationSession"/> para exponer una interfaz agnóstica
    /// (<see cref="ITransactionalMigrationExecutor"/>), usada por el engine de migraciones.
    /// </para>
    /// <para>
    /// Responsabilidades:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Ejecutar un batch SQL con timeout configurable.</description></item>
    /// <item><description>Iniciar, confirmar o revertir una transacción.</description></item>
    /// </list>
    /// <para>
    /// Normalmente se usa junto con un “script splitter” (por ejemplo <c>GO</c> splitter) para ejecutar múltiples
    /// batches en orden, idealmente dentro de una misma transacción.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var session = new SqlServerMigrationSession(factory);
    /// var exec = new SqlServerMigrationExecutor(session);
    ///
    /// exec.BeginTransaction();
    /// try
    /// {
    ///     exec.ExecuteBatch("CREATE TABLE dbo.Users(Id INT NOT NULL);");
    ///     exec.Commit();
    /// }
    /// catch
    /// {
    ///     exec.Rollback();
    ///     throw;
    /// }
    /// </code>
    /// </example>
    public sealed class SqlServerMigrationExecutor : ITransactionalMigrationExecutor
    {
        private readonly SqlServerMigrationSession _s;

        /// <summary>
        /// Crea un executor de migraciones basado en una sesión de migración SQL Server.
        /// </summary>
        /// <param name="session">Sesión que mantiene conexión/transacción y ejecuta comandos.</param>
        public SqlServerMigrationExecutor(SqlServerMigrationSession session) => _s = session;

        /// <summary>
        /// Ejecuta un batch SQL (un bloque) con timeout configurable.
        /// </summary>
        /// <param name="sql">SQL del batch a ejecutar.</param>
        /// <param name="timeoutSeconds">Timeout en segundos (default: 120).</param>
        public void ExecuteBatch(string sql, int timeoutSeconds = 120)
            => _s.ExecuteNonQuery(sql, timeoutSeconds);

        /// <summary>
        /// Inicia una transacción en la sesión.
        /// </summary>
        public void BeginTransaction() => _s.BeginTransaction();

        /// <summary>
        /// Confirma (commit) la transacción actual.
        /// </summary>
        public void Commit() => _s.Commit();

        /// <summary>
        /// Revierte (rollback) la transacción actual.
        /// </summary>
        public void Rollback() => _s.Rollback();
    }
}
