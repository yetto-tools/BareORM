namespace BareORM.Abstractions
{
    /// <summary>
    /// Lector de múltiples conjuntos de resultados de una sola ejecución de comando.
    /// Permite procesar secuencialmente cada result set devuelto por un procedimiento almacenado
    /// u otras operaciones que retornan múltiples consultas.
    /// </summary>
    /// <remarks>
    /// Esta interfaz facilita el trabajo con procedimientos almacenados que devuelven múltiples
    /// conjuntos de datos (result sets) en una sola ejecución. En lugar de cargar todo en un
    /// DataSet en memoria, permite procesar cada result set secuencialmente sin bloquear recursos.
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Procedimientos complejos:</strong> Procedimientos almacenados que devuelven
    ///         maestros y detalles en múltiples result sets.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Reportes:</strong> Procedimientos que retornan encabezado, detalle y resumen
    ///         en result sets separados.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Consultas unificadas:</strong> Una llamada a procedimiento que obtiene datos
    ///         de varias tablas relacionadas.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Reducción de viajes a BD:</strong> En lugar de múltiples consultas separadas,
    ///         una sola llamada con múltiples SELECT.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Eficiencia en memoria:</strong> Procesar result sets secuencialmente sin cargar
    ///         todo a la vez (especialmente útil con conjuntos grandes).
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Patrón de uso:</strong>
    /// <code>
    /// using var multiReader = await executor.ExecuteMultiResultAsync&lt;T1, T2, T3&gt;(comando, mapper1, mapper2, mapper3);
    /// 
    /// while (multiReader.HasMoreResults)
    /// {
    ///     // Procesar el siguiente result set
    ///     var resultado = multiReader.Read&lt;T&gt;();
    ///     // Hacer algo con el resultado
    /// }
    /// // multiReader se dispone automáticamente
    /// </code>
    /// 
    /// <strong>Implementación típica:</strong>
    /// El ORM (específicamente el IDbExecutor) crea una instancia de IMultiResultReader
    /// que envuelve el IDataReader subyacente. Para cada llamada a Read&lt;T&gt;():
    /// 1. Lee todas las filas del result set actual
    /// 2. Mapea cada fila a instancias de T usando el mapper
    /// 3. Avanza al siguiente result set
    /// 4. Retorna la lista de instancias mapeadas
    /// 
    /// <strong>Garantías de esta interfaz:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         Es secuencial: Solo puede leer un result set a la vez, en orden.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Consume completamente cada result set antes de pasar al siguiente.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Maneja el mapeo automáticamente (no requiere mapeo manual por fila).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Implementa IDisposable para liberar recursos del reader subyacente.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Definición de entidades
    /// public class Cliente
    /// {
    ///     public int ClienteId { get; set; }
    ///     public string Nombre { get; set; } = "";
    ///     public string Email { get; set; } = "";
    /// }
    /// 
    /// public class Pedido
    /// {
    ///     public int PedidoId { get; set; }
    ///     public int ClienteId { get; set; }
    ///     public DateTime Fecha { get; set; }
    ///     public decimal Total { get; set; }
    /// }
    /// 
    /// public class DetalleResumen
    /// {
    ///     public int TotalClientes { get; set; }
    ///     public int TotalPedidos { get; set; }
    ///     public decimal MontoTotal { get; set; }
    /// }
    /// 
    /// 
    /// // Mappers
    /// public class ClienteMapper : IEntityMapper&lt;Cliente&gt;
    /// {
    ///     public Cliente Map(IDataRecord record)
    ///     {
    ///         return new Cliente
    ///         {
    ///             ClienteId = (int)record["ClienteId"],
    ///             Nombre = (string)record["Nombre"],
    ///             Email = (string)record["Email"]
    ///         };
    ///     }
    /// }
    /// 
    /// public class PedidoMapper : IEntityMapper&lt;Pedido&gt;
    /// {
    ///     public Pedido Map(IDataRecord record)
    ///     {
    ///         return new Pedido
    ///         {
    ///             PedidoId = (int)record["PedidoId"],
    ///             ClienteId = (int)record["ClienteId"],
    ///             Fecha = (DateTime)record["Fecha"],
    ///             Total = (decimal)record["Total"]
    ///         };
    ///     }
    /// }
    /// 
    /// public class ResumenMapper : IEntityMapper&lt;DetalleResumen&gt;
    /// {
    ///     public DetalleResumen Map(IDataRecord record)
    ///     {
    ///         return new DetalleResumen
    ///         {
    ///             TotalClientes = (int)record["TotalClientes"],
    ///             TotalPedidos = (int)record["TotalPedidos"],
    ///             MontoTotal = (decimal)record["MontoTotal"]
    ///         };
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 1: Lectura básica de múltiples result sets
    /// public async Task ProcesarMultiplesResultados()
    /// {
    ///     var comando = factory.Create(
    ///         "sp_ObtenerClientesYPedidosYResumen",
    ///         CommandType.StoredProcedure
    ///     );
    ///     
    ///     using var multiReader = executor.ExecuteMultiResult(comando);
    ///     
    ///     int resultSetNum = 1;
    ///     
    ///     // Result set 1: Clientes
    ///     if (multiReader.HasMoreResults)
    ///     {
    ///         var clientes = multiReader.Read&lt;Cliente&gt;();
    ///         Console.WriteLine($"Result set {resultSetNum}: {clientes.Count} clientes");
    ///         foreach (var cliente in clientes)
    ///         {
    ///             Console.WriteLine($"  - {cliente.Nombre} ({cliente.Email})");
    ///         }
    ///         resultSetNum++;
    ///     }
    ///     
    ///     // Result set 2: Pedidos
    ///     if (multiReader.HasMoreResults)
    ///     {
    ///         var pedidos = multiReader.Read&lt;Pedido&gt;();
    ///         Console.WriteLine($"Result set {resultSetNum}: {pedidos.Count} pedidos");
    ///         foreach (var pedido in pedidos)
    ///         {
    ///             Console.WriteLine($"  - Pedido {pedido.PedidoId}: ${pedido.Total}");
    ///         }
    ///         resultSetNum++;
    ///     }
    ///     
    ///     // Result set 3: Resumen
    ///     if (multiReader.HasMoreResults)
    ///     {
    ///         var resumen = multiReader.Read&lt;DetalleResumen&gt;();
    ///         if (resumen.Count > 0)
    ///         {
    ///             var r = resumen[0];
    ///             Console.WriteLine($"Result set {resultSetNum}: Resumen");
    ///             Console.WriteLine($"  - Total clientes: {r.TotalClientes}");
    ///             Console.WriteLine($"  - Total pedidos: {r.TotalPedidos}");
    ///             Console.WriteLine($"  - Monto total: ${r.MontoTotal}");
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 2: Uso de bucle while para procesar unknown número de result sets
    /// public async Task ProcesarResultadosDesconocidos()
    /// {
    ///     var comando = factory.Create(
    ///         "sp_ObtenerDatosVarios",
    ///         CommandType.StoredProcedure
    ///     );
    ///     
    ///     using var multiReader = executor.ExecuteMultiResult(comando);
    ///     
    ///     int setIndex = 0;
    ///     while (multiReader.HasMoreResults)
    ///     {
    ///         setIndex++;
    ///         
    ///         // Procesar según el índice
    ///         if (setIndex == 1)
    ///         {
    ///             var clientes = multiReader.Read&lt;Cliente&gt;();
    ///             ProcesarClientes(clientes);
    ///         }
    ///         else if (setIndex == 2)
    ///         {
    ///             var pedidos = multiReader.Read&lt;Pedido&gt;();
    ///             ProcesarPedidos(pedidos);
    ///         }
    ///         else if (setIndex == 3)
    ///         {
    ///             var resumen = multiReader.Read&lt;DetalleResumen&gt;();
    ///             ProcesarResumen(resumen);
    ///         }
    ///         else
    ///         {
    ///             // Result sets adicionales inesperados
    ///             Console.WriteLine($"Warning: Result set inesperado #{setIndex}");
    ///             multiReader.Read&lt;dynamic&gt;(); // Consumir para avanzar
    ///         }
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Procesamiento con captura de errores
    /// public async Task ProcesarConManejodeErrores()
    /// {
    ///     try
    ///     {
    ///         var comando = factory.Create(
    ///             "sp_ObtenerDatos",
    ///             CommandType.StoredProcedure
    ///         );
    ///         
    ///         using var multiReader = executor.ExecuteMultiResult(comando);
    ///         
    ///         // Leer todos los result sets disponibles
    ///         var todosLosResultados = new List&lt;object&gt;();
    ///         
    ///         while (multiReader.HasMoreResults)
    ///         {
    ///             try
    ///             {
    ///                 var datos = multiReader.Read&lt;dynamic&gt;();
    ///                 todosLosResultados.Add(datos);
    ///                 Console.WriteLine($"Conjunto de datos #{todosLosResultados.Count}: {datos.Count} filas");
    ///             }
    ///             catch (Exception ex)
    ///             {
    ///                 Console.WriteLine($"Error leyendo result set: {ex.Message}");
    ///                 // Continuar con el siguiente result set
    ///             }
    ///         }
    ///         
    ///         Console.WriteLine($"Total de {todosLosResultados.Count} conjuntos leídos exitosamente");
    ///     }
    ///     catch (Exception ex)
    ///     {
    ///         Console.WriteLine($"Error general: {ex.Message}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IMultiResultReader : IDisposable
    {
        /// <summary>
        /// Lee todas las filas del result set actual y las mapea a instancias de tipo T.
        /// Avanza automáticamente al siguiente result set.
        /// </summary>
        /// <typeparam name="T">
        /// El tipo de entidad a la que mapear las filas. Debe ser compatible con un
        /// IEntityMapper&lt;T&gt; que sea capaz de manejar los datos disponibles.
        /// </typeparam>
        /// <remarks>
        /// Este método realiza las siguientes operaciones:
        /// <list type="number">
        ///     <item>
        ///         <description>
        ///         Obtiene el mapper correspondiente para el tipo T (generalmente registrado
        ///         en el ORM o proporcionado previamente).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Lee todas las filas del result set actual.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Mapea cada fila a una instancia de T usando el mapper.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Colecta todas las instancias en una List&lt;T&gt;.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Avanza al siguiente result set (si existe).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Retorna la lista de instancias mapeadas.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Comportamiento secuencial:</strong>
        /// - Cada llamada a Read&lt;T&gt;() consume completamente el result set actual.
        /// - No es posible releer el mismo result set (es secuencial).
        /// - No es posible acceder a result sets anteriores (es unidireccional).
        /// - Debe llamar a Read&lt;T&gt;() en el orden exacto que el procedimiento devuelve los result sets.
        /// 
        /// <strong>Casos de error:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Si los tipos T llamados no coinciden con los result sets devueltos,
        ///         el mapper lanzará una excepción durante el mapeo.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si no hay un mapper registrado para T, lanzará InvalidOperationException.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si el result set está vacío, devuelve una List&lt;T&gt; vacía (no nula).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Ejemplo de uso correcto:</strong>
        /// <code>
        /// // Procedimiento devuelve: Clientes, Pedidos, Resumen
        /// using var reader = executor.ExecuteMultiResult(comando);
        /// 
        /// var clientes = reader.Read&lt;Cliente&gt;();    // Lee result set 1
        /// var pedidos = reader.Read&lt;Pedido&gt;();      // Lee result set 2
        /// var resumen = reader.Read&lt;Resumen&gt;();    // Lee result set 3
        /// </code>
        /// 
        /// <strong>Ejemplo de error (orden incorrecto):</strong>
        /// <code>
        /// // ❌ INCORRECTO - tipos en orden equivocado
        /// var pedidos = reader.Read&lt;Pedido&gt;();      // Espera Clientes, obtiene Clientes
        /// var clientes = reader.Read&lt;Cliente&gt;();    // Espera Pedidos, obtiene Pedidos
        /// // Las propiedades de Pedido no coinciden con columnas de Clientes → Excepción
        /// </code>
        /// </remarks>
        /// <returns>
        /// Una List&lt;T&gt; con todas las filas del result set actual mapeadas a instancias de T.
        /// Nunca retorna null, pero puede retornar una lista vacía si el result set no contiene filas.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no hay un mapper disponible para el tipo T,
        /// o si el reader está en un estado inválido.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Se lanza si los datos del result set no pueden mapearse a tipo T
        /// (típicamente un error de tipos de columnas que no coinciden).
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        /// Se lanza si el mapper intenta acceder a una columna que no existe en el result set.
        /// </exception>
        /// <exception cref="Exception">
        /// Cualquier excepción lanzada por el mapper se propagará.
        /// </exception>
        List<T> Read<T>();

        /// <summary>
        /// Obtiene un valor que indica si hay más result sets disponibles para leer.
        /// </summary>
        /// <remarks>
        /// Este valor se actualiza automáticamente después de cada llamada a Read&lt;T&gt;().
        /// 
        /// <strong>Comportamiento:</strong>
        /// - Inicialmente, si el procedimiento devuelve múltiples result sets, este valor es <c>true</c>.
        /// - Después de cada Read&lt;T&gt;(), avanza al siguiente result set y actualiza este valor.
        /// - Cuando no hay más result sets (hemos leído todos), este valor es <c>false</c>.
        /// 
        /// <strong>Patrón de lectura común:</strong>
        /// <code>
        /// while (multiReader.HasMoreResults)
        /// {
        ///     var datos = multiReader.Read&lt;T&gt;();
        ///     // Procesar datos
        /// }
        /// </code>
        /// 
        /// <strong>Caso especial:</strong>
        /// Si el procedimiento no devuelve ningún result set o está vacío:
        /// - HasMoreResults es <c>false</c> desde el inicio
        /// - El bucle while nunca se ejecuta
        /// </remarks>
        /// <value>
        /// <c>true</c> si hay un result set disponible para leer; <c>false</c> si se han leído
        /// todos los result sets disponibles o si no hay ninguno.
        /// </value>
        bool HasMoreResults { get; }
    }
}