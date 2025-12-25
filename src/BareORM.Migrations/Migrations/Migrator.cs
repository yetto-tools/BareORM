using BareORM.Migrations.Abstractions;
using BareORM.Migrations.Abstractions.Interfaces;

namespace BareORM.Migrations.Migrations
{
    /// <summary>
    /// Orquesta la ejecución de migraciones de base de datos.
    /// </summary>
    /// <remarks>
    /// La clase <c>Migrator</c> es responsable de ejecutar un conjunto de migraciones pendientes 
    /// contra una base de datos. Coordina varios componentes para garantizar que las migraciones 
    /// se apliquen de forma segura y ordenada:
    /// 
    /// <strong>Componentes clave:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Generador SQL:</strong> Convierte operaciones de migración en sentencias SQL ejecutables.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Repositorio de historial:</strong> Mantiene el registro de migraciones aplicadas.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Proveedor de bloqueos:</strong> Asegura que solo una instancia de migrador ejecute cambios simultáneamente.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Ejecutor:</strong> Ejecuta las sentencias SQL generadas contra la base de datos.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Proceso de migración:</strong>
    /// <list type="number">
    /// <item>Asegura que la tabla de historial de migraciones existe</item>
    /// <item>Adquiere un bloqueo distribuido para evitar ejecuciones concurrentes</item>
    /// <item>Recupera el conjunto de migraciones ya aplicadas</item>
    /// <item>Para cada migración pendiente (ordenadas por ID):</item>
    /// <list type="bullet">
    /// <item>Ejecuta el método Up() de la migración</item>
    /// <item>Convierte las operaciones a lotes SQL</item>
    /// <item>Ejecuta cada lote contra la base de datos</item>
    /// <item>Registra la migración como aplicada en el historial</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var migrator = new Migrator(
    ///     sqlGenerator,
    ///     historyRepository,
    ///     lockProvider,
    ///     executor,
    ///     new MigratorOptions { CommandTimeoutSeconds = 30 });
    /// 
    /// var migrations = new[]
    /// {
    ///     new Migration("001_CreateUsersTable", "Create users table"),
    ///     new Migration("002_AddEmailColumn", "Add email to users table")
    /// };
    /// 
    /// migrator.Migrate(migrations);
    /// </code>
    /// </example>
    public sealed class Migrator
    {
        private readonly IMigrationSqlGenerator _sql;
        private readonly IMigrationHistoryRepository _history;
        private readonly IMigrationLockProvider _lockProvider;
        private readonly IMigrationExecutor _exec;
        private readonly MigratorOptions _opt;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="Migrator"/>.
        /// </summary>
        /// <remarks>
        /// El constructor inyecta todas las dependencias necesarias para ejecutar migraciones.
        /// Si no se proporcionan opciones, se utilizan los valores por defecto.
        /// </remarks>
        /// <list type="bullet">
        /// <param name="sql">
        /// Generador SQL responsable de convertir operaciones de migración en sentencias SQL ejecutables.
        /// No puede ser null.
        /// </param>
        /// <param name="history">
        /// Repositorio que mantiene el historial de migraciones aplicadas.
        /// No puede ser null.
        /// </param>
        /// <param name="lockProvider">
        /// Proveedor de bloqueos distribuidos para evitar ejecuciones concurrentes de migraciones.
        /// No puede ser null.
        /// </param>
        /// <param name="executor">
        /// Ejecutor responsable de ejecutar las sentencias SQL contra la base de datos.
        /// No puede ser null.
        /// </param>
        /// <param name="options">
        /// Opciones de configuración para el migrador. Si es null, se utilizan opciones por defecto.
        /// Ver <see cref="MigratorOptions"/> para más detalles sobre las opciones disponibles.
        /// </param>
        /// </list>
        public Migrator(
            IMigrationSqlGenerator sql,
            IMigrationHistoryRepository history,
            IMigrationLockProvider lockProvider,
            IMigrationExecutor executor,
            MigratorOptions? options = null)
        {
            _sql = sql;
            _history = history;
            _lockProvider = lockProvider;
            _exec = executor;
            _opt = options ?? new MigratorOptions();
        }

        /// <summary>
        /// Ejecuta un conjunto de migraciones de base de datos pendientes.
        /// </summary>
        /// <remarks>
        /// Este método realiza el proceso completo de migración:
        /// <list type="number">
        /// <item>
        /// <strong>Preparación:</strong>
        /// Asegura que la tabla de historial de migraciones existe en la base de datos.
        /// </item>
        /// 
        /// <item>
        /// <strong>Sincronización:</strong>
        /// Adquiere un bloqueo distribuido basado en el alcance configurado (<see cref="MigratorOptions.Scope"/>).
        /// Esto previene que múltiples instancias ejecuten migraciones simultáneamente.
        /// </item>
        /// 
        /// <item>
        /// <strong>Detección de migraciones pendientes:</strong>
        /// Obtiene la lista de migraciones ya aplicadas del historial y las filtra
        /// para identificar qué migraciones aún no se han ejecutado.
        /// </item>
        ///
        /// <item>
        /// <strong>Ejecución ordenada:</strong>
        /// Procesa las migraciones pendientes en orden alfabético por ID usando comparación ordinal.
        /// </item>
        /// 
        /// <list type="table">
        ///     <item><strong>Para cada migración:</strong></item>
        ///     <item> - Crea un <see cref="MigrationBuilder"/></item>
        ///     <item> - Invoca el método Up() de la migración para definir las operaciones</item>
        ///     <item> - Convierte las operaciones a lotes SQL usando el generador</item>
        ///     <item> - Ejecuta cada lote con el timeout especificado en opciones</item>
        ///     <item> - Registra la migración como aplicada en el historial</item>
        /// </list>
        /// 
        /// <item>
        /// <strong>Liberación del bloqueo:</strong>
        /// El bloqueo se libera automáticamente al finalizar (o si ocurre una excepción).
        /// 
        /// <strong>Consideraciones importantes:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Solo se ejecutan migraciones que no están registradas en el historial.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las migraciones se ejecutan en orden alfabético por ID. Use IDs con prefijo numérico
        ///         (ej: "001_", "002_") para mantener el orden deseado.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si una migración falla durante la ejecución, la excepción se propaga y la
        ///         migración no se registra en el historial.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         El timeout de comando se aplica a cada lote SQL, no a toda la migración.
        ///         </description>
        ///     </item>
        /// </list>
        /// </item>
        /// </list>
        /// 
        /// </remarks>
        /// <param name="migrations">
        /// Colección de migraciones a ejecutar. Las migraciones que ya estén aplicadas
        /// serán omitidas automáticamente.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no se puede crear la tabla de historial de migraciones,
        /// o si no se puede adquirir el bloqueo distribuido.
        /// </exception>
        /// <exception cref="SqlException">
        /// Se lanza si ocurre un error al ejecutar una sentencia SQL durante la migración.
        /// </exception>
        /// <example>
        /// <code>
        /// // Definir migraciones
        /// var migrations = new Migration[]
        /// {
        ///     new Migration("001_InitialSchema", "Create initial database schema")
        ///     {
        ///         Up = (mb) => mb.CreateTable("Users", t => 
        ///         {
        ///             t.Column&lt;int&gt;("Id");
        ///             t.Column&lt;string&gt;("Name");
        ///         })
        ///     },
        ///     new Migration("002_AddUserEmail", "Add email to users")
        ///     {
        ///         Up = (mb) => mb.AddColumn("Users", "Email", "nvarchar(255)")
        ///     }
        /// };
        /// 
        /// // Ejecutar migraciones
        /// try
        /// {
        ///     migrator.Migrate(migrations);
        ///     Console.WriteLine("Migraciones aplicadas exitosamente");
        /// }
        /// catch (Exception ex)
        /// {
        ///     Console.WriteLine($"Error en migración: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public void Migrate(IEnumerable<Migration> migrations)
        {
            _history.EnsureCreated();
            using var l = _lockProvider.Acquire(_opt.Scope);
            var applied = _history.GetAppliedMigrationIds();
            foreach (var m in migrations.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                if (applied.Contains(m.Id)) continue;
                var mb = new MigrationBuilder();
                m.Up(mb);
                var batches = _sql.Generate(mb.Operations);
                foreach (var sql in batches)
                    _exec.ExecuteBatch(sql, _opt.CommandTimeoutSeconds);
                _history.Insert(m.Id, m.Name, _opt.ProductVersion, DateTime.UtcNow);
            }
        }
    }
}