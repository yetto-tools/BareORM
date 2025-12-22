using System.Data;
using System.Data.Common;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Ejecutor específico del proveedor de base de datos (SQL Server, PostgreSQL, MySQL, etc.).
    /// El núcleo del ORM llama a este sin conocer el proveedor ADO.NET subyacente.
    /// </summary>
    /// <remarks>
    /// Esta interfaz define un contrato para ejecutar comandos en la base de datos de múltiples formas,
    /// ofreciendo tanto métodos simples y rápidos como métodos empresariales que capturan metadatos completos.
    /// 
    /// <strong>Filosofía de diseño:</strong>
    /// La interfaz está dividida en dos grupos de métodos:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Métodos clásicos:</strong> Rápidos y simples, devuelven solo los datos/resultados.
    ///         Ideales para consultas simples sin necesidad de parámetros OUTPUT.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Métodos empresariales (con metadatos):</strong> Devuelven DbResult que encapsula
    ///         datos + metadatos (registros afectados, valores de salida, etc.).
    ///         Necesarios para procedimientos almacenados complejos y operaciones que requieren
    ///         información contextual.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Patrón síncrono/asíncrono:</strong>
    /// Cada operación tiene dos versiones:
    /// - Versión síncrona: Ejecuta y bloquea el hilo.
    /// - Versión asíncrona: Usa async/await, no bloquea el hilo.
    /// 
    /// Se recomienda usar versiones asíncronas en aplicaciones web y servicios que manejan
    /// múltiples solicitudes concurrentes.
    /// 
    /// <strong>Gestión de recursos:</strong>
    /// Esta interfaz implementa <see cref="IDisposable"/> porque mantiene la conexión a la BD
    /// y posiblemente otras transacciones o recursos. Siempre use con «using» o asegúrese de
    /// llamar a <see cref="IDisposable.Dispose"/> cuando termine.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Obtener el ejecutor
    /// using var executor = connectionFactory.CreateExecutor();
    /// 
    /// // Ejemplo 1: Ejecución simple (INSERT, UPDATE, DELETE)
    /// var comando = factory.Create(
    ///     "INSERT INTO Usuarios (Nombre, Email) VALUES (@nombre, @email)",
    ///     CommandType.Text,
    ///     new { nombre = "Juan", email = "juan@example.com" }
    /// );
    /// int registrosInsertados = await executor.ExecuteAsync(comando);
    /// Console.WriteLine($"Insertados: {registrosInsertados}");
    /// 
    /// 
    /// // Ejemplo 2: Consulta escalar
    /// var comandoScalar = factory.Create(
    ///     "SELECT COUNT(*) FROM Usuarios WHERE Estado = @estado",
    ///     CommandType.Text,
    ///     new { estado = "Activo" }
    /// );
    /// object? resultado = await executor.ExecuteScalarAsync(comandoScalar);
    /// int totalActivos = Convert.ToInt32(resultado);
    /// Console.WriteLine($"Usuarios activos: {totalActivos}");
    /// 
    /// 
    /// // Ejemplo 3: Lectura de datos con reader
    /// var comandoReader = factory.Create(
    ///     "SELECT UsuarioId, Nombre, Email FROM Usuarios WHERE Estado = @estado",
    ///     CommandType.Text,
    ///     new { estado = "Activo" }
    /// );
    /// using var reader = await executor.ExecuteReaderAsync(comandoReader);
    /// while (reader.Read())
    /// {
    ///     Console.WriteLine($"{reader["UsuarioId"]}: {reader["Nombre"]}");
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Obtener DataTable
    /// var comandoDT = factory.Create(
    ///     "SELECT * FROM Usuarios WHERE Departamento = @depto",
    ///     CommandType.Text,
    ///     new { depto = "Ventas" }
    /// );
    /// DataTable dt = await executor.ExecuteDataTableAsync(comandoDT);
    /// foreach (DataRow row in dt.Rows)
    /// {
    ///     Console.WriteLine(row["Nombre"]);
    /// }
    /// 
    /// 
    /// // Ejemplo 5: Procedimiento con parámetros OUTPUT
    /// var parametros = new List&lt;DbParam&gt;
    /// {
    ///     new DbParam("@nombre", "Juan Nuevo"),
    ///     new DbParam("@nuevoId", Direction: ParameterDirection.Output, DbType: DbType.Int32)
    /// };
    /// var comandoProc = factory.Create(
    ///     "sp_CrearUsuario",
    ///     CommandType.StoredProcedure,
    ///     parametros
    /// );
    /// var resultado = await executor.ExecuteWithMetaAsync(comandoProc);
    /// int idGenerado = (int)resultado.OutputValues?["@nuevoId"]!;
    /// Console.WriteLine($"ID generado: {idGenerado}");
    /// </code>
    /// </example>
    public interface IDbExecutor : IDisposable
    {
        // ---- Métodos clásicos (rápidos, simples) ----

        /// <summary>
        /// Ejecuta un comando (INSERT, UPDATE, DELETE) y devuelve el número de registros afectados.
        /// </summary>
        /// <remarks>
        /// Este es el método más rápido y simple para operaciones de modificación de datos.
        /// Devuelve solo el número de filas afectadas, sin metadatos adicionales.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item><description>INSERT simple de un registro</description></item>
        ///     <item><description>UPDATE de múltiples registros</description></item>
        ///     <item><description>DELETE condicional</description></item>
        ///     <item><description>Ejecutar procedimientos que no devuelven parámetros OUTPUT</description></item>
        /// </list>
        /// 
        /// <strong>Valor de retorno:</strong>
        /// <list type="bullet">
        ///     <item><description>&gt; 0: Número de filas afectadas</description></item>
        ///     <item><description>0: Ninguna fila fue afectada</description></item>
        ///     <item><description>-1: Comando ejecutado pero BD no reporta número de filas (algunos procedimientos)</description></item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>El número de registros afectados por la operación.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución en la BD.</exception>
        int Execute(CommandDefinition command);

        /// <summary>
        /// Ejecuta un comando (INSERT, UPDATE, DELETE) de forma asíncrona y devuelve el número de registros afectados.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="Execute(CommandDefinition)"/>.
        /// No bloquea el hilo, permitiendo que otros trabajos se ejecuten mientras se espera la respuesta de la BD.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación para permitir cancelar la operación.</param>
        /// <returns>Una tarea que devuelve el número de registros afectados.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución en la BD.</exception>
        Task<int> ExecuteAsync(CommandDefinition command, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una consulta y devuelve el primer valor de la primera fila.
        /// </summary>
        /// <remarks>
        /// Ideal para consultas que devuelven un único valor (ej: COUNT, SUM, MAX, valor único).
        /// 
        /// <strong>Comportamiento:</strong>
        /// - Ejecuta la consulta
        /// - Lee la primera columna de la primera fila
        /// - Descarta el resto de los resultados
        /// - Devuelve el valor o <c>null</c> si el resultado es nulo o la consulta no devuelve filas
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item><description>Obtener contador: SELECT COUNT(*) FROM tabla</description></item>
        ///     <item><description>Obtener suma: SELECT SUM(cantidad) FROM tabla</description></item>
        ///     <item><description>Obtener máximo: SELECT MAX(fecha) FROM tabla</description></item>
        ///     <item><description>Obtener valor único: SELECT Email FROM Usuarios WHERE Id = @id</description></item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>
        /// El valor de la primera columna de la primera fila, o <c>null</c> si no hay resultados o el valor es nulo.
        /// El tipo del valor depende del tipo de dato en la BD.
        /// </returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        object? ExecuteScalar(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta de forma asíncrona y devuelve el primer valor de la primera fila.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="ExecuteScalar(CommandDefinition)"/>.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que devuelve el valor escalar o null.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        Task<object?> ExecuteScalarAsync(CommandDefinition command, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve un IDataReader para lectura secuencial de resultados.
        /// </summary>
        /// <remarks>
        /// Proporciona acceso directo a los datos mediante un lector de datos.
        /// Este método es eficiente en memoria porque lee registros uno a uno sin cargar todos en memoria.
        /// 
        /// <strong>Comportamiento:</strong>
        /// - Ejecuta la consulta
        /// - Devuelve un IDataReader posicionado antes del primer registro
        /// - Debe llamar a <c>Read()</c> para avanzar a cada registro
        /// - Debe cerrar/disponer el reader cuando termine
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Procesar millones de registros sin cargar todo en memoria</description>
        ///     </item>
        ///     <item>
        ///         <description>Lectura secuencial de datos grandes</description>
        ///     </item>
        ///     <item>
        ///         <description>Streaming de datos</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Ejemplo:</strong>
        /// <code>
        /// using var reader = executor.ExecuteReader(command);
        /// while (reader.Read())
        /// {
        ///     string nombre = (string)reader["Nombre"];
        ///     int id = (int)reader["Id"];
        ///     // Procesar cada registro
        /// }
        /// </code>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un IDataReader para leer los resultados secuencialmente.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        IDataReader ExecuteReader(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta SELECT de forma asíncrona y devuelve un IDataReader.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="ExecuteReader(CommandDefinition)"/>.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que devuelve un IDataReader.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        Task<IDataReader> ExecuteReaderAsync(CommandDefinition command, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve los resultados en un DataTable.
        /// </summary>
        /// <remarks>
        /// Carga todos los resultados en memoria en un DataTable, que es una estructura
        /// tabular con acceso aleatorio a filas y columnas.
        /// 
        /// <strong>Ventajas:</strong>
        /// - Acceso aleatorio a cualquier fila/columna
        /// - Metadatos de columnas disponibles
        /// - Fácil de iterar y manipular
        /// 
        /// <strong>Desventajas:</strong>
        /// - Carga todo en memoria (no ideal para millones de registros)
        /// - Mayor uso de memoria que un reader
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item><description>Datos de tamaño moderado que requieren acceso aleatorio</description></item>
        ///     <item><description>Necesita información de esquema/metadatos</description></item>
        ///     <item><description>Requiere manipulación en memoria (filtrado, ordenamiento adicional)</description></item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DataTable con todos los resultados de la consulta.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DataTable ExecuteDataTable(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta y devuelve los resultados en un DataSet (múltiples tablas).
        /// </summary>
        /// <remarks>
        /// Similar a ExecuteDataTable, pero puede devolver múltiples tablas de resultados.
        /// Útil para procedimientos almacenados que devuelven varios conjuntos de datos.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Procedimientos que devuelven múltiples conjuntos de datos</description>
        ///     </item>
        ///     <item>
        ///         <description>Consultas complejas que requieren varios resultados</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DataSet con las tablas de resultados.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DataSet ExecuteDataSet(CommandDefinition command);

        // ---- Métodos empresariales (con metadatos / valores de salida / valores de retorno) ----

        /// <summary>
        /// Ejecuta un comando y devuelve metadatos (registros afectados, valores OUTPUT).
        /// </summary>
        /// <remarks>
        /// Versión de nivel empresarial que captura información adicional junto con los resultados.
        /// 
        /// <strong>Metadatos capturados:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>RecordsAffected:</strong> Número de filas afectadas por la operación
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>OutputValues:</strong> Valores devueltos por parámetros OUTPUT del procedimiento
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Procedimientos almacenados que devuelven parámetros OUTPUT</description>
        ///     </item>
        ///     <item>
        ///         <description>Necesita capturar IDs generados por el procedimiento</description>
        ///     </item>
        ///     <item>
        ///         <description>Requiere metadatos sobre la operación</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DbResult con registros afectados y valores de salida.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DbResult ExecuteWithMeta(CommandDefinition command);

        /// <summary>
        /// Ejecuta un comando de forma asíncrona y devuelve metadatos.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="ExecuteWithMeta(CommandDefinition)"/>.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que devuelve DbResult con metadatos.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        Task<DbResult> ExecuteWithMetaAsync(CommandDefinition command, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una consulta escalar tipada y devuelve el resultado con metadatos.
        /// </summary>
        /// <typeparam name="T">El tipo esperado del resultado (int, string, DateTime, etc.).</typeparam>
        /// <remarks>
        /// Similar a ExecuteScalar, pero devuelve el valor tipado a T y captura metadatos.
        /// El resultado se convierte automáticamente al tipo T.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Obtener un contador tipado: SELECT COUNT(*) as [int]</description>
        ///     </item>
        ///     <item>
        ///         <description>Obtener suma tipada: SELECT SUM(monto) as [decimal]</description>
        ///     </item>
        ///     <item>
        ///         <description>Procedimientos que devuelven valor + parámetros OUTPUT</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DbResult&lt;T&gt; con el valor tipado y metadatos.</returns>
        /// <exception cref="InvalidCastException">Si el resultado no puede convertirse a tipo T.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DbResult<T?> ExecuteScalarWithMeta<T>(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta escalar tipada de forma asíncrona y devuelve el resultado con metadatos.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="ExecuteScalarWithMeta{T}(CommandDefinition)"/>.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que devuelve DbResult&lt;T&gt; con el valor tipado.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="InvalidCastException">Si el resultado no puede convertirse a tipo T.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        Task<DbResult<T?>> ExecuteScalarWithMetaAsync<T>(CommandDefinition command, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve un DataTable con metadatos.
        /// </summary>
        /// <remarks>
        /// Similar a ExecuteDataTable, pero además captura registros afectados y valores OUTPUT.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Procedimientos que devuelven un DataTable + parámetros OUTPUT</description>
        ///     </item>
        ///     <item>
        ///         <description>Necesita metadatos sobre la operación de lectura</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DbResult&lt;DataTable&gt; con los datos y metadatos.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DbResult<DataTable> ExecuteDataTableWithMeta(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta y devuelve un DataSet con metadatos.
        /// </summary>
        /// <remarks>
        /// Similar a ExecuteDataSet, pero además captura valores OUTPUT.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DbResult&lt;DataSet&gt; con los datos y metadatos.</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DbResult<DataSet> ExecuteDataSetWithMeta(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta SELECT y devuelve un IDataReader con metadatos.
        /// </summary>
        /// <remarks>
        /// Combina la eficiencia en memoria del reader con la captura de metadatos.
        /// 
        /// <strong>Comportamiento especial:</strong>
        /// Algunos proveedores de BD no pueden capturar completamente los valores OUTPUT hasta que
        /// el IDataReader se cierre completamente. En estos casos:
        /// - Los OutputValues pueden estar vacíos mientras el reader sigue abierto
        /// - Los OutputValues se rellenan completamente cuando se cierra el reader
        /// 
        /// <strong>Recomendación:</strong>
        /// Cierre el reader (dentro del using) antes de acceder a OutputValues para garantizar
        /// que tengan valores completos.
        /// 
        /// <strong>Ejemplo correcto:</strong>
        /// <code>
        /// var resultado = executor.ExecuteReaderWithMeta(comando);
        /// using (var reader = resultado.Data)
        /// {
        ///     while (reader.Read())
        ///     {
        ///         // Procesar datos del reader
        ///     }
        /// } // Reader cerrado aquí
        /// 
        /// // Ahora OutputValues está completo
        /// if (resultado.OutputValues?.TryGetValue("@codigoError", out var codigo) == true)
        /// {
        ///     Console.WriteLine($"Código: {codigo}");
        /// }
        /// </code>
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <returns>Un DbResult&lt;IDataReader&gt; con el reader y metadatos (valores OUTPUT completos después de cerrar el reader).</returns>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        DbResult<IDataReader> ExecuteReaderWithMeta(CommandDefinition command);

        /// <summary>
        /// Ejecuta una consulta SELECT de forma asíncrona y devuelve un IDataReader con metadatos.
        /// </summary>
        /// <remarks>
        /// Versión asíncrona de <see cref="ExecuteReaderWithMeta(CommandDefinition)"/>.
        /// Tenga en cuenta la misma consideración sobre OutputValues y el cierre del reader.
        /// </remarks>
        /// <param name="command">La definición del comando a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Una tarea que devuelve DbResult&lt;IDataReader&gt; con el reader y metadatos.</returns>
        /// <exception cref="OperationCanceledException">Si la operación es cancelada.</exception>
        /// <exception cref="DbException">Si ocurre un error durante la ejecución.</exception>
        Task<DbResult<IDataReader>> ExecuteReaderWithMetaAsync(CommandDefinition command, CancellationToken ct = default);
    }
}