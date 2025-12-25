using System;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Repositorio de historial de migraciones para SQL Server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementa <see cref="IMigrationHistoryRepository"/> usando una tabla de historial (por defecto
    /// <c>dbo.__BareORMMigrationsHistory</c>) para registrar qué migraciones ya fueron aplicadas.
    /// </para>
    /// <para>
    /// Uso típico dentro del engine de migraciones:
    /// </para>
    /// <list type="number">
    /// <item><description>Llamar <see cref="EnsureCreated"/> (bootstrap) antes de aplicar migraciones.</description></item>
    /// <item><description>Leer ids aplicados con <see cref="GetAppliedMigrationIds"/>.</description></item>
    /// <item><description>Aplicar las migraciones pendientes.</description></item>
    /// <item><description>Insertar un registro por cada migración aplicada usando <see cref="Insert"/>.</description></item>
    /// </list>
    /// <para>
    /// Seguridad / escaping:
    /// este repositorio construye SQL dinámico para bootstrap e insert, escapando literales y
    /// nombres de identificadores con helpers internos (<see cref="Lit"/> / <see cref="Ident"/>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var session = new SqlServerMigrationSession(factory);
    /// var repo = new SqlServerMigrationHistoryRepository(session);
    ///
    /// repo.EnsureCreated();
    /// var applied = repo.GetAppliedMigrationIds();
    ///
    /// if (!applied.Contains("20251225_090000"))
    /// {
    ///     // ... ejecutar migración ...
    ///     repo.Insert("20251225_090000", "CreateUsers", "1.0.0", DateTime.UtcNow);
    /// }
    /// </code>
    /// </example>
    public sealed class SqlServerMigrationHistoryRepository : IMigrationHistoryRepository
    {
        private readonly SqlServerMigrationSession _s;
        private readonly string _schema;
        private readonly string _table;

        /// <summary>
        /// Crea el repositorio de historial de migraciones.
        /// </summary>
        /// <param name="session">Sesión usada para ejecutar SQL.</param>
        /// <param name="schema">Schema donde se creará la tabla de historial (default: <c>dbo</c>).</param>
        /// <param name="table">Nombre de la tabla de historial (default: <c>__BareORMMigrationsHistory</c>).</param>
        public SqlServerMigrationHistoryRepository(
            SqlServerMigrationSession session,
            string schema = "dbo",
            string table = "__BareORMMigrationsHistory")
        {
            _s = session;
            _schema = schema;
            _table = table;
        }

        /// <summary>
        /// Asegura que existan el schema y la tabla de historial en la base de datos.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Crea el schema si no existe, y crea la tabla si no existe usando <c>OBJECT_ID</c>.
        /// </para>
        /// <para>
        /// Estructura de tabla:
        /// </para>
        /// <list type="bullet">
        /// <item><description><c>MigrationId</c>: NVARCHAR(64) PK.</description></item>
        /// <item><description><c>Name</c>: NVARCHAR(200).</description></item>
        /// <item><description><c>ProductVersion</c>: NVARCHAR(64).</description></item>
        /// <item><description><c>AppliedAtUtc</c>: DATETIME2.</description></item>
        /// </list>
        /// </remarks>
        public void EnsureCreated()
        {
            // schema
            _s.ExecuteNonQuery($@"
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{Lit(_schema)}')
                EXEC(N'CREATE SCHEMA [{Ident(_schema)}]');
            ");

            // table
            _s.ExecuteNonQuery($@"
            IF OBJECT_ID(N'{Lit(_schema)}.{Lit(_table)}', N'U') IS NULL
            BEGIN
                CREATE TABLE [{Ident(_schema)}].[{Ident(_table)}]
                (
                    MigrationId     NVARCHAR(64)  NOT NULL,
                    Name            NVARCHAR(200) NOT NULL,
                    ProductVersion  NVARCHAR(64)  NOT NULL,
                    AppliedAtUtc    DATETIME2     NOT NULL,
                    CONSTRAINT [PK_{Ident(_table)}] PRIMARY KEY (MigrationId)
                );
            END
            ");
        }

        /// <summary>
        /// Obtiene el conjunto de ids de migraciones ya aplicadas.
        /// </summary>
        /// <returns>
        /// Set de ids (case-sensitive ordinal) listo para comparar contra el catálogo local de migraciones.
        /// </returns>
        public IReadOnlySet<string> GetAppliedMigrationIds()
        {
            var ids = _s.QueryStrings($@"SELECT MigrationId FROM [{Ident(_schema)}].[{Ident(_table)}] ORDER BY MigrationId;");
            return new HashSet<string>(ids, StringComparer.Ordinal);
        }

        /// <summary>
        /// Inserta un registro en la tabla de historial indicando que una migración fue aplicada.
        /// </summary>
        /// <param name="migrationId">Id único/ordenable (p.ej. <c>20251225_090000</c>).</param>
        /// <param name="name">Nombre humano (p.ej. <c>CreateUsers</c>).</param>
        /// <param name="productVersion">Versión del producto/lib usada (p.ej. <c>1.0.0</c>).</param>
        /// <param name="appliedAtUtc">Timestamp UTC de aplicación.</param>
        /// <remarks>
        /// Se persiste <paramref name="appliedAtUtc"/> con formato ISO extendido:
        /// <c>yyyy-MM-ddTHH:mm:ss.fffffff</c>.
        /// </remarks>
        public void Insert(string migrationId, string name, string productVersion, DateTime appliedAtUtc)
        {
            var sql = $@"
                INSERT INTO [{Ident(_schema)}].[{Ident(_table)}]
                (MigrationId, Name, ProductVersion, AppliedAtUtc)
                VALUES
                (N'{Lit(migrationId)}', N'{Lit(name)}', N'{Lit(productVersion)}', '{appliedAtUtc:yyyy-MM-ddTHH:mm:ss.fffffff}');";

            _s.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// Escapa un literal SQL (string) duplicando comillas simples.
        /// </summary>
        private static string Lit(string s) => s.Replace("'", "''");

        /// <summary>
        /// Escapa un identificador SQL Server duplicando corchetes de cierre.
        /// </summary>
        private static string Ident(string s) => s.Replace("]", "]]");
    }
}
