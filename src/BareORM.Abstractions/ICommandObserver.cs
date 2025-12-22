namespace BareORM.Abstractions
{
    /// <summary>
    /// Hook ligero de diagnósticos para observar la ejecución de comandos.
    /// El núcleo del ORM y los proveedores pueden llamar a este para rastrear y diagnosticar
    /// sin imponer la dependencia de ILogger.
    /// </summary>
    /// <remarks>
    /// Esta interfaz proporciona un mecanismo de observación de bajo acoplamiento para monitorear
    /// el ciclo de vida completo de la ejecución de comandos en la base de datos.
    /// 
    /// <strong>Ventajas de este enfoque:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Sin dependencias obligatorias:</strong> No requiere que la aplicación
    ///         use un framework de logging específico (log4net, NLog, Serilog, etc.).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Ligero:</strong> Interfaz minimal con solo 3 métodos esenciales.
    ///         Bajo overhead de rendimiento.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Flexible:</strong> Cada aplicación puede implementar su propia estrategia
    ///         de logging, diagnóstico, monitoreo o análisis.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Información completa:</strong> Proporciona acceso al comando completo,
    ///         duración de ejecución e información de excepciones.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Logging:</strong> Registrar en ficheros, bases de datos o servicios de logging.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Monitoreo de rendimiento:</strong> Rastrear comandos lentos, detectar cuellos de botella.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Debugging:</strong> Inspeccionar parámetros y duración en tiempo de desarrollo.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Auditoría:</strong> Registrar quién ejecutó qué comando y cuándo.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Monitoreo de errores:</strong> Capturar y registrar excepciones en comandos.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Métricas:</strong> Colectar estadísticas de comandos para análisis y alertas.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Ciclo de vida de eventos:</strong>
    /// En una ejecución exitosa:
    /// <code>
    /// OnExecuting(command)     → [ejecución en BD] → OnExecuted(command, duration)
    /// </code>
    /// 
    /// Si ocurre un error durante la ejecución:
    /// <code>
    /// OnExecuting(command)     → [error en BD] → OnError(command, exception)
    /// </code>
    /// 
    /// Si el error ocurre antes de ejecutar (ej: validación de parámetros), es posible que
    /// <c>OnExecuting</c> no se haya llamado.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Implementación simple para console logging
    /// public class ConsoleCommandObserver : ICommandObserver
    /// {
    ///     public void OnExecuting(CommandDefinition command)
    ///     {
    ///         Console.WriteLine($"[INICIANDO] {command.CommandText}");
    ///     }
    ///     
    ///     public void OnExecuted(CommandDefinition command, TimeSpan duration)
    ///     {
    ///         Console.WriteLine($"[COMPLETADO] {command.CommandText} en {duration.TotalMilliseconds}ms");
    ///     }
    ///     
    ///     public void OnError(CommandDefinition command, Exception exception)
    ///     {
    ///         Console.WriteLine($"[ERROR] {command.CommandText}: {exception.Message}");
    ///     }
    /// }
    /// 
    /// // Registrar el observer
    /// var observer = new ConsoleCommandObserver();
    /// miORM.SetCommandObserver(observer);
    /// 
    /// 
    /// // Ejemplo 2: Observer que detecta comandos lentos
    /// public class SlowCommandObserver : ICommandObserver
    /// {
    ///     private const long SLOW_THRESHOLD_MS = 1000;
    ///     private readonly ILogger _logger;
    ///     
    ///     public void OnExecuting(CommandDefinition command) { }
    ///     
    ///     public void OnExecuted(CommandDefinition command, TimeSpan duration)
    ///     {
    ///         if (duration.TotalMilliseconds > SLOW_THRESHOLD_MS)
    ///         {
    ///             _logger.LogWarning(
    ///                 "Comando lento detectado: {CommandText} tardó {Duration}ms",
    ///                 command.CommandText,
    ///                 duration.TotalMilliseconds
    ///             );
    ///         }
    ///     }
    ///     
    ///     public void OnError(CommandDefinition command, Exception exception) { }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Observer para recopilar métricas
    /// public class MetricsCommandObserver : ICommandObserver
    /// {
    ///     private readonly Dictionary&lt;string, CommandMetrics&gt; _metrics = new();
    ///     
    ///     public void OnExecuting(CommandDefinition command)
    ///     {
    ///         if (!_metrics.ContainsKey(command.CommandText))
    ///             _metrics[command.CommandText] = new CommandMetrics();
    ///         
    ///         _metrics[command.CommandText].ExecutionCount++;
    ///     }
    ///     
    ///     public void OnExecuted(CommandDefinition command, TimeSpan duration)
    ///     {
    ///         var m = _metrics[command.CommandText];
    ///         m.TotalDuration += duration;
    ///         m.MaxDuration = Math.Max(m.MaxDuration, duration);
    ///         m.MinDuration = Math.Min(m.MinDuration, duration);
    ///     }
    ///     
    ///     public void OnError(CommandDefinition command, Exception exception)
    ///     {
    ///         if (!_metrics.ContainsKey(command.CommandText))
    ///             _metrics[command.CommandText] = new CommandMetrics();
    ///         
    ///         _metrics[command.CommandText].ErrorCount++;
    ///     }
    ///     
    ///     public void PrintReport()
    ///     {
    ///         foreach (var kvp in _metrics)
    ///         {
    ///             var metrics = kvp.Value;
    ///             Console.WriteLine($"Comando: {kvp.Key}");
    ///             Console.WriteLine($"  Ejecuciones: {metrics.ExecutionCount}");
    ///             Console.WriteLine($"  Errores: {metrics.ErrorCount}");
    ///             Console.WriteLine($"  Duración promedio: {metrics.AverageDuration.TotalMilliseconds}ms");
    ///             Console.WriteLine($"  Duración mín/máx: {metrics.MinDuration.TotalMilliseconds}ms / {metrics.MaxDuration.TotalMilliseconds}ms");
    ///         }
    ///     }
    /// }
    /// 
    /// private class CommandMetrics
    /// {
    ///     public int ExecutionCount { get; set; }
    ///     public int ErrorCount { get; set; }
    ///     public TimeSpan TotalDuration { get; set; }
    ///     public TimeSpan MaxDuration { get; set; } = TimeSpan.Zero;
    ///     public TimeSpan MinDuration { get; set; } = TimeSpan.MaxValue;
    ///     public TimeSpan AverageDuration => ExecutionCount > 0 
    ///         ? TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / ExecutionCount) 
    ///         : TimeSpan.Zero;
    /// }
    /// </code>
    /// </example>
    public interface ICommandObserver
    {
        /// <summary>
        /// Se invoca justo antes de ejecutar un comando en la base de datos.
        /// </summary>
        /// <remarks>
        /// Este método se llama cuando el ORM está a punto de enviar el comando a la base de datos,
        /// pero antes de que se ejecute realmente.
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Registrar que un comando va a ejecutarse</description>
        ///     </item>
        ///     <item>
        ///         <description>Iniciar un cronómetro para medir duración</description>
        ///     </item>
        ///     <item>
        ///         <description>Inspeccionar parámetros del comando antes de la ejecución</description>
        ///     </item>
        ///     <item>
        ///         <description>Validar comandos sospechosos</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Garantías:</strong>
        /// - El <see cref="CommandDefinition"/> proporcionado contiene toda la información del comando
        ///   (texto, tipo, parámetros, timeout).
        /// - Este método se llama una sola vez por ejecución de comando.
        /// - Si hay un error durante la ejecución, <see cref="OnError"/> se llama en lugar de <see cref="OnExecuted"/>.
        /// 
        /// <strong>Consideraciones de rendimiento:</strong>
        /// Este método se llama en el camino crítico de ejecución, así que debe ser rápido.
        /// Evite operaciones I/O costosas o computaciones complejas aquí.
        /// </remarks>
        /// <param name="command">
        /// La definición del comando que va a ejecutarse. Contiene texto, tipo, parámetros y timeout.
        /// No es nulo.
        /// </param>
        void OnExecuting(CommandDefinition command);

        /// <summary>
        /// Se invoca después de ejecutar exitosamente un comando en la base de datos.
        /// </summary>
        /// <remarks>
        /// Este método se llama cuando el comando ha completado su ejecución exitosamente
        /// y se han recuperado los resultados (si los hay).
        /// 
        /// <strong>Información proporcionada:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>CommandDefinition:</strong> El comando que se ejecutó (mismo que en OnExecuting).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>TimeSpan duration:</strong> Tiempo transcurrido desde que se envió el comando
        ///         hasta que se completó su ejecución en la BD. Incluye red, procesamiento BD, etc.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Registrar la duración de la ejecución</description>
        ///     </item>
        ///     <item>
        ///         <description>Detectar y alertar sobre comandos lentos (> 1 segundo)</description>
        ///     </item>
        ///     <item>
        ///         <description>Recopilar estadísticas y métricas de rendimiento</description>
        ///     </item>
        ///     <item>
        ///         <description>Enviar datos de telemetría a un servicio de monitoreo</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Garantías:</strong>
        /// - <see cref="OnExecuting"/> fue llamado previamente para este comando.
        /// - La duración es medida por el ORM y es precisa.
        /// - Si ocurrió un error durante la ejecución, se llama a <see cref="OnError"/> en su lugar.
        /// 
        /// <strong>Timing de la duración:</strong>
        /// La duración incluye:
        /// - Tiempo de red (envío del comando, recepción de resultados)
        /// - Tiempo de procesamiento en la BD
        /// - Tiempo de mapeo de resultados (si aplica)
        /// 
        /// NO incluye:
        /// - Tiempo de validación de parámetros (antes de OnExecuting)
        /// - Tiempo de creación de comandos ADO.NET
        /// </remarks>
        /// <param name="command">
        /// La definición del comando que se ejecutó. Es el mismo objeto pasado a OnExecuting.
        /// No es nulo.
        /// </param>
        /// <param name="duration">
        /// El tiempo transcurrido desde la ejecución del comando. Siempre positivo.
        /// Típicamente en el rango de milisegundos a segundos.
        /// </param>
        void OnExecuted(CommandDefinition command, TimeSpan duration);

        /// <summary>
        /// Se invoca si ocurre un error durante la ejecución del comando.
        /// </summary>
        /// <remarks>
        /// Este método se llama cuando la ejecución del comando en la base de datos falla
        /// con una excepción, en lugar de llamar a <see cref="OnExecuted"/>.
        /// 
        /// <strong>Causas comunes de errores:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>La tabla o procedimiento no existe</description>
        ///     </item>
        ///     <item>
        ///         <description>Violación de restricciones (clave única, clave foránea)</description>
        ///     </item>
        ///     <item>
        ///         <description>Permisos insuficientes</description>
        ///     </item>
        ///     <item>
        ///         <description>Timeout de conexión o comando</description>
        ///     </item>
        ///     <item>
        ///         <description>Deadlock en la BD</description>
        ///     </item>
        ///     <item>
        ///         <description>Error de sintaxis SQL</description>
        ///     </item>
        ///     <item>
        ///         <description>Fallo de conexión a la BD</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Casos de uso:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>Registrar errores de BD para auditoría</description>
        ///     </item>
        ///     <item>
        ///         <description>Enviar alertas sobre errores críticos</description>
        ///     </item>
        ///     <item>
        ///         <description>Recopilar estadísticas de fallas</description>
        ///     </item>
        ///     <item>
        ///         <description>Debugging en desarrollo</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Garantías:</strong>
        /// - <see cref="OnExecuting"/> fue llamado previamente para este comando.
        /// - <see cref="OnExecuted"/> NO será llamado (es una alternativa a OnExecuted, no un complemento).
        /// - La excepción contiene información detallada del error de la BD.
        /// - Si ocurre una excepción en este método, puede propagarse o ser ignorada
        ///   dependiendo de la implementación del ORM.
        /// 
        /// <strong>Nota importante sobre excepciones:</strong>
        /// Si este método lanza una excepción, podría interferir con el manejo de errores
        /// normal del ORM. Se recomienda envolver la lógica en try-catch.
        /// </remarks>
        /// <param name="command">
        /// La definición del comando que falló. Es el mismo objeto pasado a OnExecuting.
        /// No es nulo.
        /// </param>
        /// <param name="exception">
        /// La excepción que ocurrió durante la ejecución. Contiene detalles del error
        /// como mensaje, stack trace, e información específica del proveedor BD.
        /// No es nulo.
        /// </param>
        void OnError(CommandDefinition command, Exception exception);
    }
}