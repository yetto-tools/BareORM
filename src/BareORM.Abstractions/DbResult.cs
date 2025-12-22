namespace BareORM.Abstractions
{
    /// <summary>
    /// Metadatos de ejecución de un comando sin datos de resultado.
    /// Los valores de salida ya están extraídos en un formato agnóstico del proveedor.
    /// </summary>
    /// <remarks>
    /// Este record encapsula los metadatos generales que devuelve la ejecución de cualquier
    /// comando de base de datos, sin incluir los datos del resultado.
    /// 
    /// Se utiliza típicamente cuando:
    /// - Se ejecutan comandos que no devuelven conjuntos de datos (INSERT, UPDATE, DELETE)
    /// - Solo interesa conocer cuántos registros fueron afectados
    /// - Se necesita acceder a parámetros OUTPUT de procedimientos almacenados
    /// 
    /// Los valores de salida ya vienen en un formato agnóstico, sin referencias específicas
    /// al proveedor de base de datos.
    /// </remarks>
    /// <param name="RecordsAffected">
    /// El número de registros afectados por la operación.
    /// Indica cuántos registros fueron impactados: INSERT (filas insertadas), UPDATE (filas actualizadas),
    /// DELETE (filas eliminadas), o procedimientos (puede variar según lógica interna).
    /// El valor predeterminado es 0 si ningún registro fue afectado.
    /// </param>
    /// <param name="OutputValues">
    /// Un diccionario de solo lectura con los valores de salida del comando, ya extraídos en un formato agnóstico del proveedor.
    /// Contiene los valores devueltos por parámetros OUTPUT de procedimientos almacenados (ej: "@resultado", "@nuevoId").
    /// Es <c>null</c> si no hay parámetros de salida, o vacío si existen parámetros pero no devolvieron valores.
    /// Valor por defecto: <c>null</c>.
    /// </param>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Verificar registros afectados en una actualización
    /// var resultado = await miORM.ActualizarAsync(comando);
    /// 
    /// if (resultado.RecordsAffected > 0)
    /// {
    ///     Console.WriteLine($"Se actualizaron {resultado.RecordsAffected} registros");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Ningún registro fue actualizado");
    /// }
    /// 
    /// // Ejemplo 2: Obtener valores de salida de un procedimiento
    /// var resultado = await miORM.EjecutarProcedimientoAsync(comando);
    /// 
    /// if (resultado.OutputValues?.TryGetValue("@codigoError", out var codigo) == true)
    /// {
    ///     Console.WriteLine($"Código de error: {codigo}");
    /// }
    /// </code>
    /// </example>
    public record DbResult(
        int RecordsAffected = 0,
        IReadOnlyDictionary<string, object?>? OutputValues = null
    );

    /// <summary>
    /// Contenedor de resultado tipado que encapsula datos más metadatos de ejecución.
    /// </summary>
    /// <typeparam name="T">
    /// El tipo de datos que contiene el resultado. Puede ser un modelo de entidad,
    /// una colección, un valor escalar, o cualquier otro tipo .NET.
    /// </typeparam>
    /// <remarks>
    /// Este record genérico combina los datos del resultado con los metadatos de su ejecución
    /// (número de registros afectados, parámetros de salida).
    /// 
    /// Proporciona una forma tipada y segura de devolver datos junto con información
    /// contextual sobre cómo se obtuvieron esos datos.
    /// 
    /// Se utiliza típicamente cuando:
    /// - Se ejecutan consultas SELECT que devuelven datos que deben mapearse a un tipo C#
    /// - Se necesita acceder tanto a los datos como a metadatos de la ejecución
    /// - Se ejecutan procedimientos almacenados que devuelven tanto conjuntos de datos como parámetros OUTPUT
    /// 
    /// Hereda de <see cref="DbResult"/> para proporcionar acceso a los metadatos comunes.
    /// </remarks>
    /// <param name="Data">
    /// Los datos del resultado, mapeados al tipo especificado.
    /// Contiene el resultado de la consulta o procedimiento almacenado, ya mapeado y tipado al tipo genérico T.
    /// Puede ser una entidad única (ej: Usuario), una colección (ej: List&lt;Usuario&gt;, IEnumerable&lt;Usuario&gt;),
    /// un tipo escalar (ej: int, string, decimal), un DTO personalizado, o <c>null</c> si la consulta no devolvió resultados.
    /// </param>
    /// <param name="RecordsAffected">
    /// El número de registros afectados por la operación (heredado de DbResult).
    /// Indica cuántos registros fueron impactados. Valor por defecto: 0.
    /// Ver <see cref="DbResult"/> para más detalles.
    /// </param>
    /// <param name="OutputValues">
    /// Un diccionario de solo lectura con los valores de salida del comando (heredado de DbResult).
    /// Contiene valores devueltos por parámetros OUTPUT. Valor por defecto: <c>null</c>.
    /// Ver <see cref="DbResult"/> para más detalles.
    /// </param>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Obtener una lista tipada de usuarios
    /// var resultado = await miORM.ObtenerTodosAsync&lt;Usuario&gt;();
    /// 
    /// foreach (var usuario in resultado.Data)
    /// {
    ///     Console.WriteLine($"{usuario.Id}: {usuario.Nombre}");
    /// }
    /// 
    /// // Ejemplo 2: Obtener un usuario único con metadatos
    /// var resultado = await miORM.ObtenerPorIdAsync&lt;Usuario&gt;(42);
    /// 
    /// if (resultado.Data != null)
    /// {
    ///     Console.WriteLine($"Usuario encontrado: {resultado.Data.Nombre}");
    ///     Console.WriteLine($"Registros afectados: {resultado.RecordsAffected}");
    /// }
    /// 
    /// // Ejemplo 3: Procedimiento que devuelve datos y parámetros OUTPUT
    /// var resultado = await miORM.EjecutarProcedimientoAsync&lt;ReporteMensual&gt;
    /// (
    ///     new CommandDefinition(/* ... */)
    /// );
    /// 
    /// if (resultado.Data != null)
    /// {
    ///     Console.WriteLine($"Total de registros en reporte: {resultado.Data.Count}");
    /// }
    /// 
    /// if (resultado.OutputValues?.TryGetValue("@totalGranTotal", out var total) == true)
    /// {
    ///     Console.WriteLine($"Gran total: {total}");
    /// }
    /// 
    /// // Ejemplo 4: Trabajar con un resultado tipado como estructura
    /// var (datos, registrosAfectados, salidas) = resultado;
    /// Console.WriteLine($"Se procesaron {registrosAfectados} registros");
    /// </code>
    /// </example>
    public sealed record DbResult<T>(
        T Data,
        int RecordsAffected = 0,
        IReadOnlyDictionary<string, object?>? OutputValues = null
    ) : DbResult(RecordsAffected, OutputValues);
}