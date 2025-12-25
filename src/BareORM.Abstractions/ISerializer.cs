using System.Text.Json;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Abstracción agnóstica de serialización para uso en cachés, logs y snapshots.
    /// Las implementaciones específicas residen en BareORM.Serialization 
    /// (JSON, BSON, protobuf, etc.).
    /// </summary>
    /// <remarks>
    /// Esta interfaz define un contrato para serializar y deserializar objetos .NET
    /// a/desde representaciones de texto (típicamente JSON), de manera agnóstica respecto
    /// al formato o librería específica de serialización.
    /// 
    /// <strong>Ventajas de usar una abstracción:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Agnóstico de formato:</strong> Cambiar entre JSON, BSON, XML, protobuf, etc.
    ///         sin afectar el código que usa la serialización.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Agnóstico de librería:</strong> Cambiar de System.Text.Json a Newtonsoft.Json
    ///         u otra librería sin cambiar el contrato.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Inyección de dependencias:</strong> Diferentes implementaciones
    ///         pueden ser inyectadas según el contexto (desarrollo, testing, producción).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Testabilidad:</strong> Fácil crear mocks o implementaciones alternativas.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos en BareORM:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Cachés:</strong> Serializar objetos en caché distribuida (Redis, Memcached)
    ///         y deserializar cuando se recuperan.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Logs:</strong> Serializar estados, snapshots y contextos para auditoría
    ///         y debugging.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Snapshots:</strong> Guardar copias de entidades en un punto en el tiempo
    ///         para análisis posterior o auditoría.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Sincronización:</strong> Serializar objetos para envíos entre procesos
    ///         o servicios.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Persistencia temporal:</strong> Guardar estados de sesión, carritos de compra, etc.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Ejemplo de implementaciones esperadas:</strong>
    /// <list type="bullet">
    ///     <item><description>JsonSerializer: Usa System.Text.Json (bajo overhead, built-in)</description></item>
    ///     <item><description>NewtonsoftJsonSerializer: Usa Newtonsoft.Json (más flexible)</description></item>
    ///     <item><description>ProtobufSerializer: Usa Protocol Buffers (muy compacto, binario)</description></item>
    ///     <item><description>MessagePackSerializer: Usa MessagePack (binario, rápido)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Definición de entidad
    /// public class Usuario
    /// {
    ///     public int Id { get; set; }
    ///     public string Nombre { get; set; } = "";
    ///     public string Email { get; set; } = "";
    ///     public DateTime FechaRegistro { get; set; }
    /// }
    /// 
    /// 
    /// // Ejemplo 1: Uso básico de serialización
    /// public class CacheService
    /// {
    ///     private readonly ISerializer _serializer;
    ///     private readonly IDistributedCache _cache;
    ///     
    ///     public CacheService(ISerializer serializer, IDistributedCache cache)
    ///     {
    ///         _serializer = serializer;
    ///         _cache = cache;
    ///     }
    ///     
    ///     public async Task GuardarEnCacheAsync(Usuario usuario)
    ///     {
    ///         // Serializar el usuario a JSON
    ///         string json = _serializer.Serialize(usuario);
    ///         
    ///         // Guardar en Redis o Memcached
    ///         await _cache.SetStringAsync(
    ///             $"usuario:{usuario.Id}",
    ///             json,
    ///             new DistributedCacheEntryOptions 
    ///             { 
    ///                 AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) 
    ///             }
    ///         );
    ///     }
    ///     
    ///     public async Task&lt;Usuario?&gt; ObtenerDelCacheAsync(int usuarioId)
    ///     {
    ///         // Obtener del caché
    ///         string? json = await _cache.GetStringAsync($"usuario:{usuarioId}");
    ///         
    ///         if (json == null)
    ///             return null;
    ///         
    ///         // Deserializar el JSON al objeto Usuario
    ///         return _serializer.Deserialize&lt;Usuario&gt;(json);
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 2: Uso en auditoría/logs
    /// public class AuditLogger
    /// {
    ///     private readonly ISerializer _serializer;
    ///     private readonly ILogger&lt;AuditLogger&gt; _logger;
    ///     
    ///     public void LogCambio&lt;T&gt;(T entityBefore, T entityAfter, string operacion)
    ///     {
    ///         // Serializar estados antes y después
    ///         string jsonBefore = _serializer.Serialize(entityBefore);
    ///         string jsonAfter = _serializer.Serialize(entityAfter);
    ///         
    ///         _logger.LogInformation(
    ///             "Operación {Operacion} - Antes: {Before} - Después: {After}",
    ///             operacion,
    ///             jsonBefore,
    ///             jsonAfter
    ///         );
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Snapshot para auditoría
    /// public class SnapshotStore
    /// {
    ///     private readonly ISerializer _serializer;
    ///     private readonly IRepository&lt;EntitySnapshot&gt; _snapshotRepo;
    ///     
    ///     public async Task CrearSnapshotAsync&lt;T&gt;(T entity, string entidadTipo, int entidadId)
    ///     {
    ///         // Serializar la entidad actual
    ///         string datos = _serializer.Serialize(entity);
    ///         
    ///         var snapshot = new EntitySnapshot
    ///         {
    ///             EntidadTipo = entidadTipo,
    ///             EntidadId = entidadId,
    ///             Datos = datos,
    ///             FechaSnapshot = DateTime.UtcNow
    ///         };
    ///         
    ///         await _snapshotRepo.InsertAsync(snapshot);
    ///     }
    ///     
    ///     public async Task&lt;T?&gt; RecuperarSnapshotAsync&lt;T&gt;(string entidadTipo, int entidadId)
    ///     {
    ///         var snapshot = await _snapshotRepo
    ///             .Where(s => s.EntidadTipo == entidadTipo &amp;&amp; s.EntidadId == entidadId)
    ///             .OrderByDescending(s => s.FechaSnapshot)
    ///             .FirstOrDefaultAsync();
    ///         
    ///         if (snapshot == null)
    ///             return default;
    ///         
    ///         // Deserializar y restaurar
    ///         return _serializer.Deserialize&lt;T&gt;(snapshot.Datos);
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Mapeo dinámico con Type
    /// public class UniversalDeserializer
    /// {
    ///     private readonly ISerializer _serializer;
    ///     private readonly Dictionary&lt;string, Type&gt; _typeRegistry;
    ///     
    ///     public UniversalDeserializer(ISerializer serializer)
    ///     {
    ///         _serializer = serializer;
    ///         _typeRegistry = new Dictionary&lt;string, Type&gt;
    ///         {
    ///             { "usuario", typeof(Usuario) },
    ///             { "pedido", typeof(Pedido) },
    ///             { "producto", typeof(Producto) }
    ///         };
    ///     }
    ///     
    ///     public object? DeserializarPorNombreTipo(string json, string nombreTipo)
    ///     {
    ///         if (!_typeRegistry.TryGetValue(nombreTipo.ToLowerInvariant(), out var type))
    ///         {
    ///             throw new InvalidOperationException($"Tipo '{nombreTipo}' no reconocido");
    ///         }
    ///         
    ///         // Usar sobrecarga Deserialize(string, Type)
    ///         return _serializer.Deserialize(json, type);
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 5: Sincronización entre servicios
    /// public class ServiceBusPublisher
    /// {
    ///     private readonly ISerializer _serializer;
    ///     private readonly IServiceBusClient _serviceBus;
    ///     
    ///     public async Task PublicarEventoAsync&lt;T&gt;(T evento)
    ///     {
    ///         // Serializar el evento
    ///         string json = _serializer.Serialize(evento);
    ///         
    ///         // Enviar a través de Service Bus
    ///         await _serviceBus.SendAsync(
    ///             topicName: typeof(T).Name,
    ///             body: System.Text.Encoding.UTF8.GetBytes(json)
    ///         );
    ///     }
    /// }
    /// 
    /// public class ServiceBusSubscriber
    /// {
    ///     private readonly ISerializer _serializer;
    ///     
    ///     public T DeserializarMensaje&lt;T&gt;(string body)
    ///     {
    ///         // Deserializar el mensaje recibido
    ///         return _serializer.Deserialize&lt;T&gt;(body) 
    ///             ?? throw new InvalidOperationException("No se pudo deserializar el mensaje");
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 6: Conversión genérica entre tipos
    /// public class TypeConverter
    /// {
    ///     private readonly ISerializer _serializer;
    ///     
    ///     public T ConvertirUsandoSerialization&lt;T&gt;(object origen)
    ///     {
    ///         // Serializar el objeto origen
    ///         string json = _serializer.Serialize(origen);
    ///         
    ///         // Deserializar como tipo T
    ///         return _serializer.Deserialize&lt;T&gt;(json) 
    ///             ?? throw new InvalidOperationException("Conversión fallida");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ISerializer
    {
        /// <summary>
        /// Serializa un objeto de tipo T a una representación de texto (típicamente JSON).
        /// </summary>
        /// <typeparam name="T">El tipo del objeto a serializar.</typeparam>
        /// <remarks>
        /// Este método realiza una serialización genérica y fuertemente tipada del objeto.
        /// 
        /// <strong>Proceso:</strong>
        /// 1. Toma el objeto de tipo T
        /// 2. Convierte su estado a una representación de texto
        /// 3. Retorna la representación como string
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         El objeto nunca es modificado (serialización es operación de solo lectura).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades públicas se serializan por defecto.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades privadas, métodos y campos no se serializan automáticamente
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Los valores nulos se incluyen en la serialización (como null en JSON).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Los objetos complejos anidados se serializan recursivamente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las referencias circulares pueden causar StackOverflowException
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Tamaño de resultado:</strong>
        /// El tamaño de la cadena serializada depende del formato y la complejidad del objeto.
        /// Para objetos grandes, considere:
        /// - Compresión (GZIP)
        /// - Almacenamiento en caché
        /// - Paginación
        /// </remarks>
        /// <param name="value">
        /// El objeto a serializar. Puede ser null.
        /// Si es null, típicamente devuelve "null" como string.
        /// </param>
        /// <returns>
        /// Una representación de texto del objeto. Nunca retorna null, pero puede retornar
        /// la cadena "null" si el objeto de entrada es null.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el objeto contiene estructuras no serializables
        /// (ej: referencias circulares, tipos no soportados).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Se lanza si hay problemas con los parámetros de serialización.
        /// </exception>
        string Serialize<T>(T value);

        /// <summary>
        /// Serializa un objeto de tipo T a una representación de texto (típicamente JSON).
        /// </summary>
        /// <typeparam name="T">El tipo del objeto a serializar.</typeparam>
        /// <remarks>
        /// Este método realiza una serialización genérica y fuertemente tipada del objeto
        /// aplicando las opciones de configuración especificadas.
        /// 
        /// <strong>Proceso:</strong>
        /// 1. Toma el objeto de tipo T
        /// 2. Convierte su estado a una representación de texto según las opciones
        /// 3. Retorna la representación como string
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         El objeto nunca es modificado (serialización es operación de solo lectura).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades públicas se serializan por defecto.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades privadas, métodos y campos no se serializan automáticamente
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Los valores nulos se incluyen en la serialización (como null en JSON).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Los objetos complejos anidados se serializan recursivamente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las referencias circulares pueden causar StackOverflowException
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Tamaño de resultado:</strong>
        /// El tamaño de la cadena serializada depende del formato y la complejidad del objeto.
        /// Para objetos grandes, considere:
        /// - Compresión (GZIP)
        /// - Almacenamiento en caché
        /// - Paginación
        /// </remarks>
        /// <param name="value">
        /// El objeto a serializar. Puede ser null.
        /// Si es null, típicamente devuelve "null" como string.
        /// </param>
        /// <param name="options">
        /// Las opciones de configuración para la serialización JSON. 
        /// Incluye configuraciones como nombrado de propiedades, tipos de codificación,
        /// manejo de valores nulos, y comportamiento ante tipos no serializables.
        /// Si es null, se utilizan las opciones por defecto.
        /// <see cref="JsonSerializerOptions"/>
        /// </param>
        /// <returns>
        /// Una representación de texto del objeto serializado. Nunca retorna null, pero puede retornar
        /// la cadena "null" si el objeto de entrada es null.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el objeto contiene estructuras no serializables
        /// (ej: referencias circulares, tipos no soportados).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Se lanza si hay problemas con los parámetros de serialización o las opciones proporcionadas.
        /// </exception>
        string Serialize<T>(T value, JsonSerializerOptions options);

        /// <summary>
        /// Deserializa una cadena de texto a un objeto de tipo T.
        /// </summary>
        /// <typeparam name="T">El tipo esperado del objeto deserializado.</typeparam>
        /// <remarks>
        /// Este método realiza una deserialización genérica y fuertemente tipada.
        /// 
        /// <strong>Proceso:</strong>
        /// 1. Toma una cadena de texto (típicamente JSON)
        /// 2. Analiza la estructura
        /// 3. Crea e inicializa una instancia de T
        /// 4. Retorna la instancia (o null si el input era "null")
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Si el input es la cadena "null", retorna <c>null</c> (no una excepción).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si el input está vacío o es inválido, lanza una excepción.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         El tipo T debe ser compatible con la estructura del texto serializado.
        ///         Si hay desajustes, lanza FormatException.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades faltantes pueden obtener valores predeterminados o ignorarse
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Las propiedades adicionales en el texto pueden ignorarse
        ///         (depende de la implementación).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Patrones de uso seguro:</strong>
        /// <code>
        /// // ✓ Recomendado: Checkear null
        /// var usuario = serializer.Deserialize&lt;Usuario&gt;(json);
        /// if (usuario == null)
        /// {
        ///     // Manejar caso null
        /// }
        /// 
        /// // ✓ Recomendado: Try-catch para JSON inválido
        /// try
        /// {
        ///     var usuario = serializer.Deserialize&lt;Usuario&gt;(json);
        /// }
        /// catch (FormatException ex)
        /// {
        ///     Console.WriteLine($"JSON inválido: {ex.Message}");
        /// }
        /// </code>
        /// </remarks>
        /// <param name="data">
        /// La cadena a deserializar. Puede ser:
        /// - JSON válido que representa una instancia de T: <c>{"id": 1, "nombre": "Juan"}</c>
        /// - La cadena literal "null": Retorna <c>null</c>
        /// - JSON inválido o vacío: Lanza excepción
        /// </param>
        /// <returns>
        /// Una instancia de T completamente inicializada, o <c>null</c> si el input fue "null".
        /// </returns>
        /// <exception cref="FormatException">
        /// Se lanza si el JSON es inválido o no representa un T válido.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ocurre un error durante la deserialización.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Se lanza si el parámetro data es null o vacío (dependiendo de implementación).
        /// </exception>
        T? Deserialize<T>(string data);
        
        /// <summary>
        /// Deserializa una cadena de texto a un objeto del tipo especificado dinámicamente.
        /// </summary>
        /// <remarks>
        /// Esta sobrecarga permite deserialización dinámica cuando el tipo no se conoce
        /// en tiempo de compilación. Es especialmente útil para:
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Mapeo JSON dinámico:</strong> Cuando diferentes tipos pueden venir
        ///         del mismo origen (ej: diferentes eventos, diferentes DTOs).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Registros de tipos:</strong> Cuando el tipo se elige de un registro
        ///         basado en una clave o metadata.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Sistemas de plugins:</strong> Cuando tipos se cargan dinámicamente
        ///         en tiempo de ejecución.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>APIs genéricas:</strong> Cuando la API acepte cualquier tipo compatible.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Diferencia con Deserialize&lt;T&gt;:</strong>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Aspecto</term>
        ///         <description>Deserialize&lt;T&gt;</description>
        ///         <description>Deserialize(string, Type)</description>
        ///     </listheader>
        ///     <item>
        ///         <term>Tipo conocido</term>
        ///         <description>En tiempo de compilación</description>
        ///         <description>En tiempo de ejecución</description>
        ///     </item>
        ///     <item>
        ///         <term>Validación de tipo</term>
        ///         <description>En compilación</description>
        ///         <description>En ejecución</description>
        ///     </item>
        ///     <item>
        ///         <term>Tipo de retorno</term>
        ///         <description>T? (tipado)</description>
        ///         <description>object? (sin tipo)</description>
        ///     </item>
        ///     <item>
        ///         <term>Rendimiento</term>
        ///         <description>Típicamente más rápido</description>
        ///         <description>Puede ser más lento (reflection)</description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Consideraciones:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         El Type debe ser una clase concreta (no abstracta, no interfaz genérica).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si type es null, lanza ArgumentNullException.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         El tipo debe ser deserializable (tener constructor sin parámetros, etc.).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         El resultado debe castearse manualmente al tipo esperado si es necesario.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Si la cadena data es "null", retorna <c>null</c>.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Patrones de uso:</strong>
        /// <code>
        /// // Patrón 1: Mapeo basado en registro de tipos
        /// var registry = new Dictionary&lt;string, Type&gt;
        /// {
        ///     { "usuario", typeof(Usuario) },
        ///     { "pedido", typeof(Pedido) }
        /// };
        /// 
        /// string tipoNombre = "usuario";
        /// var obj = serializer.Deserialize(json, registry[tipoNombre]);
        /// var usuario = (Usuario)obj!;
        /// 
        /// 
        /// // Patrón 2: Usando Type.GetType()
        /// string nombreTipo = "MyApp.Models.Usuario";
        /// var tipo = Type.GetType(nombreTipo);
        /// if (tipo != null)
        /// {
        ///     var obj = serializer.Deserialize(json, tipo);
        /// }
        /// 
        /// 
        /// // Patrón 3: En un switch basado en tipo
        /// public object DeserializarSegunTipo(string json, string discriminador)
        /// {
        ///     var tipo = discriminador switch
        ///     {
        ///         "U" => typeof(Usuario),
        ///         "P" => typeof(Pedido),
        ///         "Q" => typeof(Producto),
        ///         _ => throw new InvalidOperationException($"Tipo desconocido: {discriminador}")
        ///     };
        ///     
        ///     return serializer.Deserialize(json, tipo)!;
        /// }
        /// </code>
        /// </remarks>
        /// <param name="data">
        /// La cadena a deserializar. Puede ser JSON válido o "null".
        /// </param>
        /// <param name="type">
        /// El Type al que deserializar. Debe ser concreto y deserializable.
        /// No puede ser <c>null</c>.
        /// </param>
        /// <returns>
        /// Una instancia del tipo especificado, o <c>null</c> si data fue "null".
        /// El resultado es de tipo object, debe castearse al tipo esperado si es necesario.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="type"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Se lanza si el JSON es inválido o no representa una instancia válida del tipo.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el tipo no es deserializable o si hay un error durante la deserialización.
        /// </exception>
        object? Deserialize(string data, Type type);
    }
}