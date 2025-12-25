using System;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Proveedor de lock para migraciones en SQL Server usando application locks (sp_getapplock).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este proveedor evita que múltiples procesos/instancias ejecuten migraciones en paralelo dentro del mismo
    /// servidor SQL Server, adquiriendo un lock exclusivo por <c>scope</c> (ver <see cref="Acquire(string)"/>).
    /// </para>
    /// <para>Implementación:</para>
    /// <list type="bullet">
    /// <item><description>Usa <c>sp_getapplock</c> con <c>@LockMode = 'Exclusive'</c>.</description></item>
    /// <item><description>El owner del lock es <c>Session</c> (se mantiene por la conexión de la sesión).</description></item>
    /// <item><description>El release se hace con <c>sp_releaseapplock</c> en <c>IMigrationLock.Dispose</c> />.</description></item>
    /// <item><description>El timeout se controla con <c>@LockTimeout</c> (ms), por defecto 30 segundos.</description></item>
    /// </list>
    /// <para>Notas:</para>
    /// <list type="bullet">
    /// <item><description>Un código &lt; 0 devuelto por <c>sp_getapplock</c> se considera fallo y lanza excepción.</description></item>
    /// <item><description>Como el owner es <c>Session</c>, si la conexión muere, el lock se libera automáticamente por SQL Server.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var session = new SqlServerMigrationSession(factory);
    /// var locks = new SqlServerMigrationLockProvider(session);
    ///
    /// using var lck = locks.Acquire("BareORM.Migrations");
    /// // ... ejecutar migraciones de forma exclusiva ...
    /// </code>
    /// </example>

    public sealed class SqlServerMigrationLockProvider : IMigrationLockProvider
    {
        private readonly SqlServerMigrationSession _s;
        private readonly int _timeoutMs;

        /// <summary>
        /// Crea el lock provider.
        /// </summary>
        /// <param name="session">Sesión de migración usada para ejecutar los comandos.</param>
        /// <param name="timeoutMs">Timeout del lock en milisegundos (default: 30,000).</param>
        public SqlServerMigrationLockProvider(SqlServerMigrationSession session, int timeoutMs = 30_000)
        {
            _s = session;
            _timeoutMs = timeoutMs;
        }

        /// <summary>
        /// Adquiere un lock exclusivo a nivel de servidor para el scope indicado.
        /// </summary>
        /// <param name="scope">Nombre del recurso de lock (ej: <c>"BareORM.Migrations"</c>).</param>
        /// <returns>Un <see cref="IMigrationLock"/> que libera el lock al hacer Dispose.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si <c>sp_getapplock</c> devuelve un código negativo.
        /// </exception>
        public IMigrationLock Acquire(string scope)
        {
            var sql = $@"
DECLARE @res INT;
EXEC @res = sp_getapplock
    @Resource = N'{Lit(scope)}',
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = {_timeoutMs};
SELECT @res;";

            var resObj = _s.ExecuteScalar(sql);
            var res = Convert.ToInt32(resObj);

            if (res < 0)
                throw new InvalidOperationException($"sp_getapplock failed for '{scope}', code={res}");

            return new SqlServerMigrationLock(_s, scope);
        }

        /// <summary>
        /// Implementación concreta del lock que libera el applock al hacer Dispose.
        /// </summary>
        private sealed class SqlServerMigrationLock : IMigrationLock
        {
            private readonly SqlServerMigrationSession _s;
            private readonly string _scope;
            private bool _disposed;

            public SqlServerMigrationLock(SqlServerMigrationSession s, string scope)
            {
                _s = s;
                _scope = scope;
            }

            /// <summary>
            /// Libera el lock asociado al scope actual (sp_releaseapplock).
            /// </summary>
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                _s.ExecuteNonQuery($@"
                    EXEC sp_releaseapplock
                        @Resource = N'{Lit(_scope)}',
                        @LockOwner = 'Session';");
            }
        }

        /// <summary>
        /// Escapa un literal SQL (string) duplicando comillas simples.
        /// </summary>
        private static string Lit(string s) => s.Replace("'", "''");
    }
}
