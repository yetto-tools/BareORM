using System.Data;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Interfaz para operaciones masivas específicas del proveedor de base de datos.
    /// Cada proveedor implementa sus propias optimizaciones nativas 
    /// (SqlBulkCopy para SQL Server, COPY para PostgreSQL, LOAD DATA para MySQL, etc.).
    /// </summary>
    /// <remarks>
    /// Esta interfaz define un contrato para operaciones de inserción y fusión en lote de alto rendimiento,
    /// aprovechando las características específicas y optimizadas de cada motor de base de datos.
    /// 
    /// A diferencia de INSERT/UPDATE tradicionales, estas operaciones son significativamente más rápidas
    /// cuando se necesita procesar miles o millones de registros porque:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         Minimizan el overhead de logging transaccional.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Utilizan rutas de inserción optimizadas internas del motor SQL.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Reducen el número de viajes cliente-servidor.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Permiten bloqueos a nivel de tabla en lugar de fila para mejor concurrencia.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// - Migraciones de datos de gran volumen
    /// - Importación de archivos CSV/Excel con cientos de miles de registros
    /// - Sincronización de datos entre sistemas
    /// - Cargas de datos iniciales (data seeding)
    /// - ETL (Extract, Transform, Load) masivo
    /// 
    /// <strong>Consideraciones importantes:</strong>
    /// - Las operaciones masivas pueden afectar la concurrencia. Considere ejecutarlas en horarios no pico.
    /// - Los índices y restricciones se validan, pero menos estrictamente que en operaciones normales.
    /// - El bloqueo de tabla impide otros accesos durante la operación (si está habilitado).
    /// - Los triggers pueden ejecutarse o no dependiendo del proveedor y configuración.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Preparar datos en un DataTable
    /// var dt = new DataTable();
    /// dt.Columns.Add("UsuarioId", typeof(int));
    /// dt.Columns.Add("Nombre", typeof(string));
    /// dt.Columns.Add("Email", typeof(string));
    /// dt.Columns.Add("FechaCreacion", typeof(DateTime));
    /// 
    /// // Llenar con datos
    /// for (int i = 0; i &lt; 100000; i++)
    /// {
    ///     dt.Rows.Add(i, $"Usuario{i}", $"user{i}@example.com", DateTime.UtcNow);
    /// }
    /// 
    /// // Configurar opciones de lote
    /// var opciones = new BulkOptions(
    ///     BatchSize: 10000,
    ///     TimeoutSeconds: 300,
    ///     UseInternalTransaction: true,
    ///     TableLock: true
    /// );
    /// 
    /// // Insertar masivamente
    /// var bulkProvider = miORM.GetBulkProvider();
    /// await bulkProvider.BulkInsertAsync("Usuarios", dt, opciones);
    /// 
    /// Console.WriteLine($"Se insertaron {dt.Rows.Count} usuarios exitosamente");
    /// </code>
    /// </example>
    public interface IBulkProvider
    {
        /// <summary>
        /// Inserta masivamente registros en una tabla de forma síncrona.
        /// </summary>
        /// <remarks>
        /// Realiza una inserción en lote de alto rendimiento de todos los registros en el DataTable
        /// en la tabla especificada. Esta operación es mucho más rápida que insertar registros
        /// individuales con INSERT tradicionales.
        /// 
        /// <strong>Comportamiento:</strong>
        /// - Todos los registros del DataTable se insertan en la tabla destino.
        /// - Las columnas del DataTable deben coincidir con las columnas de la tabla (en nombre o índice).
        /// - Se respetan las restricciones de clave primaria, claves foráneas y restricciones CHECK.
        /// - Los índices se actualizan después de la inserción.
        /// - El bloqueo de tabla se aplica si está habilitado en las opciones.
        /// 
        /// <strong>Comportamiento con transacciones:</strong>
        /// Si <c>UseInternalTransaction</c> es <c>true</c> en las opciones, toda la operación se
        /// ejecuta dentro de una transacción única. Si una fila falla, toda la operación se revierte.
        /// 
        /// <strong>Rendimiento:</strong>
        /// Para datos voluminosos (1M+ de registros), considere dividir en múltiples llamadas
        /// con <c>BatchSize</c> apropiad o. Un BatchSize de 5000-10000 es típicamente óptimo.
        /// </remarks>
        /// <param name="tableName">
        /// El nombre completamente calificado de la tabla destino. 
        /// Ejemplo: "dbo.Usuarios" (SQL Server), "public.usuarios" (PostgreSQL).
        /// </param>
        /// <param name="data">
        /// Un DataTable que contiene los registros a insertar. No puede ser <c>null</c>.
        /// Las columnas del DataTable deben existir en la tabla destino.
        /// </param>
        /// <param name="options">
        /// Opciones de configuración para la operación masiva (tamaño de lote, timeout, etc.).
        /// Si es <c>null</c>, se utilizan valores predeterminados.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="tableName"/> es nulo o vacío.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="data"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="DataException">
        /// Se lanza si ocurre un error durante la inserción masiva
        /// (tabla no existe, columnas incompatibles, violaciones de restricciones, etc.).
        /// </exception>
        void BulkInsert(string tableName, DataTable data, BulkOptions? options = null);

        /// <summary>
        /// Inserta masivamente registros en una tabla de forma asíncrona.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="BulkInsert(string, DataTable, BulkOptions?)"/>.
        /// Permite que la operación se ejecute sin bloquear el hilo actual, lo que es especialmente
        /// útil en aplicaciones web y servicios que manejan múltiples solicitudes concurrentes.
        /// 
        /// El <paramref name="ct"/> (CancellationToken) permite cancelar la operación en curso.
        /// Si se cancela, la transacción se revierte si está habilitada.
        /// </remarks>
        /// <param name="tableName">
        /// El nombre completamente calificado de la tabla destino.
        /// Ejemplo: "dbo.Usuarios" (SQL Server), "public.usuarios" (PostgreSQL).
        /// </param>
        /// <param name="data">
        /// Un DataTable que contiene los registros a insertar. No puede ser <c>null</c>.
        /// </param>
        /// <param name="options">
        /// Opciones de configuración para la operación masiva.
        /// Si es <c>null</c>, se utilizan valores predeterminados.
        /// </param>
        /// <param name="ct">
        /// Token de cancelación para permitir la cancelación de la operación.
        /// El valor predeterminado es <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// Una tarea que representa la operación asíncrona.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="tableName"/> es nulo o vacío.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="data"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Se lanza si la operación es cancelada a través del <paramref name="ct"/>.
        /// </exception>
        /// <exception cref="DataException">
        /// Se lanza si ocurre un error durante la inserción masiva.
        /// </exception>
        Task BulkInsertAsync(string tableName, DataTable data, BulkOptions? options = null, CancellationToken ct = default);

        /// <summary>
        /// Realiza una fusión (merge) masiva: inserta registros nuevos y actualiza los existentes.
        /// </summary>
        /// <remarks>
        /// Combina operaciones INSERT y UPDATE en una única operación masiva de alto rendimiento.
        /// Para cada registro en el DataTable:
        /// - Si existe una fila con la misma clave primaria, se actualiza.
        /// - Si no existe, se inserta como nueva.
        /// 
        /// <strong>Comportamiento:</strong>
        /// - Requiere que la tabla tenga una clave primaria definida.
        /// - El DataTable debe incluir todas las columnas de clave primaria.
        /// - Se respetan las restricciones de integridad referencial.
        /// - Los índices se actualizan durante la operación.
        /// 
        /// <strong>Equivalencia SQL:</strong>
        /// Esta operación es similar a:
        /// <code>
        /// MERGE INTO tabla_destino AS dest
        /// USING tabla_origen AS src
        /// ON dest.id = src.id
        /// WHEN MATCHED THEN UPDATE SET ...
        /// WHEN NOT MATCHED THEN INSERT ...
        /// </code>
        /// (la sintaxis exacta varía por proveedor)
        /// 
        /// <strong>Rendimiento:</strong>
        /// Merge es más lento que Insert puro porque debe verificar la existencia de cada registro.
        /// Para operaciones de solo inserción, use <see cref="BulkInsert(string, DataTable, BulkOptions?)"/> en su lugar.
        /// </remarks>
        /// <param name="tableName">
        /// El nombre completamente calificado de la tabla destino.
        /// Ejemplo: "dbo.Usuarios" (SQL Server), "public.usuarios" (PostgreSQL).
        /// </param>
        /// <param name="data">
        /// Un DataTable que contiene los registros a insertar o actualizar.
        /// No puede ser <c>null</c>. Debe incluir las columnas de clave primaria.
        /// </param>
        /// <param name="options">
        /// Opciones de configuración para la operación masiva.
        /// Si es <c>null</c>, se utilizan valores predeterminados.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="tableName"/> es nulo o vacío,
        /// o si la tabla no tiene clave primaria definida.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="data"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="DataException">
        /// Se lanza si ocurre un error durante la fusión masiva.
        /// </exception>
        void BulkMerge(string tableName, DataTable data, BulkOptions? options = null);

        /// <summary>
        /// Realiza una fusión (merge) masiva de forma asíncrona: inserta registros nuevos y actualiza los existentes.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="BulkMerge(string, DataTable, BulkOptions?)"/>.
        /// Permite que la operación se ejecute sin bloquear el hilo actual.
        /// 
        /// El <paramref name="ct"/> (CancellationToken) permite cancelar la operación en curso.
        /// Si se cancela, la transacción se revierte si está habilitada.
        /// </remarks>
        /// <param name="tableName">
        /// El nombre completamente calificado de la tabla destino.
        /// Ejemplo: "dbo.Usuarios" (SQL Server), "public.usuarios" (PostgreSQL).
        /// </param>
        /// <param name="data">
        /// Un DataTable que contiene los registros a insertar o actualizar.
        /// No puede ser <c>null</c>. Debe incluir las columnas de clave primaria.
        /// </param>
        /// <param name="options">
        /// Opciones de configuración para la operación masiva.
        /// Si es <c>null</c>, se utilizan valores predeterminados.
        /// </param>
        /// <param name="ct">
        /// Token de cancelación para permitir la cancelación de la operación.
        /// El valor predeterminado es <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// Una tarea que representa la operación asíncrona.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="tableName"/> es nulo o vacío,
        /// o si la tabla no tiene clave primaria definida.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="data"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Se lanza si la operación es cancelada a través del <paramref name="ct"/>.
        /// </exception>
        /// <exception cref="DataException">
        /// Se lanza si ocurre un error durante la fusión masiva.
        /// </exception>
        Task BulkMergeAsync(string tableName, DataTable data, BulkOptions? options = null, CancellationToken ct = default);
    }
}