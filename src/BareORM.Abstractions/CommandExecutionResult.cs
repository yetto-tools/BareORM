namespace BareORM.Abstractions
{
    /// <summary>
    /// Resultado inmutable de la ejecución de un comando de base de datos.
    /// Contiene información sobre el impacto de la operación y los valores de salida.
    /// </summary>
    /// <remarks>
    /// Este record encapsula el resultado de ejecutar un comando (procedimiento almacenado
    /// o SQL directo) en la base de datos. Proporciona dos piezas clave de información:
    /// 
    /// 1. El número de registros afectados por la operación (INSERT, UPDATE, DELETE).
    /// 2. Los valores de salida devueltos por parámetros OUTPUT del procedimiento almacenado.
    /// 
    /// Su naturaleza inmutable garantiza que los datos no puedan ser modificados después
    /// de la ejecución, proporcionando seguridad y predictibilidad en ambientes multihilo.
    /// </remarks>
    /// <param name="RecordsAffected">
    /// El número de registros afectados por la operación. Este valor indica cuántos registros 
    /// fueron impactados:
    /// - Para comandos INSERT: número de filas insertadas
    /// - Para comandos UPDATE: número de filas actualizadas
    /// - Para comandos DELETE: número de filas eliminadas
    /// - Para procedimientos almacenados: puede variar según la lógica interna (puede devolver -1 si no aplica)
    /// - Para comandos SELECT o procedimientos de solo lectura: típicamente 0 o -1
    /// 
    /// Este valor es útil para validar que la operación afectó el número de registros esperado,
    /// como verificación de integridad después de una actualización o eliminación.
    /// Puede ser 0 si ningún registro fue afectado, o -1 si la base de datos no proporciona esta información.
    /// </param>
    /// <param name="OutputValues">
    /// Un diccionario de solo lectura con los valores de salida del comando. Contiene los valores 
    /// devueltos por parámetros OUTPUT en procedimientos almacenados. Las claves del diccionario 
    /// son los nombres de los parámetros (ej: "@resultado", "@mensaje"), y los valores son los datos 
    /// devueltos por el procedimiento.
    /// 
    /// Este diccionario es útil para obtener:
    /// - Identificadores generados por procedimientos (ej: ID de nuevo registro)
    /// - Mensajes de estado o error devueltos por el servidor
    /// - Códigos de resultado o indicadores de éxito/fracaso
    /// - Valores calculados o procesados por el procedimiento almacenado
    /// 
    /// El diccionario estará vacío si el comando no devuelve parámetros de salida.
    /// Los valores pueden ser <c>null</c> si el procedimiento no asignó un valor específico.
    /// Nunca es <c>null</c>, pero puede estar vacío si no hay parámetros de salida.
    /// </param>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Ejecución de procedimiento que actualiza registros
    /// var resultado = await miORM.ExecutarComandoAsync(definicion);
    /// 
    /// if (resultado.RecordsAffected > 0)
    /// {
    ///     Console.WriteLine($"Se actualizaron {resultado.RecordsAffected} registros");
    /// }
    /// 
    /// // Ejemplo 2: Procedimiento con parámetros de salida
    /// var resultado = await miORM.ExecutarComandoAsync(definicion);
    /// 
    /// if (resultado.OutputValues.TryGetValue("@nuevoId", out var id))
    /// {
    ///     Console.WriteLine($"ID generado: {id}");
    /// }
    /// 
    /// if (resultado.OutputValues.TryGetValue("@mensaje", out var mensaje))
    /// {
    ///     Console.WriteLine($"Mensaje del servidor: {mensaje}");
    /// }
    /// 
    /// // Ejemplo 3: Verificar múltiples valores de salida
    /// foreach (var kvp in resultado.OutputValues)
    /// {
    ///     Console.WriteLine($"{kvp.Key} = {kvp.Value}");
    /// }
    /// </code>
    /// </example>
    public sealed record CommandExecutionResult(
        int RecordsAffected,
        IReadOnlyDictionary<string, object?> OutputValues
    );
}