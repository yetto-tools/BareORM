using System.Data;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Mapea una única fila de IDataRecord a una entidad tipada.
    /// Se utiliza por métodos como Execute&lt;T&gt; para convertir resultados de base de datos
    /// a objetos de dominio de la aplicación.
    /// </summary>
    /// <typeparam name="T">
    /// El tipo de entidad a la que se mapearán los registros de la base de datos.
    /// Puede ser una clase de modelo, DTO, record, o cualquier tipo .NET.
    /// </typeparam>
    /// <remarks>
    /// Esta interfaz implementa el patrón Strategy para la conversión de datos.
    /// Permite que la aplicación defina exactamente cómo convertir filas de datos
    /// en objetos de la aplicación, sin que el ORM asuma una estrategia de mapeo.
    /// 
    /// <strong>Ventajas de usar un mapper explícito:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Control total:</strong> Decide exactamente qué propiedades mapear,
    ///         cómo transformar valores, qué hacer con nulos, etc.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Lógica personalizada:</strong> Puede incluir validaciones, conversiones complejas,
    ///         ensamblaje de objetos relacionados, etc.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Rendimiento:</strong> Puede optimizar el mapeo evitando reflection innecesaria.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Mantenibilidad:</strong> Cambios en la estructura de datos se concentran en el mapper,
    ///         no dispersos por la aplicación.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Testabilidad:</strong> Fácil de testear con IDataRecord mock.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Consultas simples:</strong> Mapear resultados SELECT directamente a DTOs
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Transformaciones de datos:</strong> Convertir valores de BD a tipos aplicación
    ///         (ej: string → Enum, número → booleano)
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Ensamblaje de objetos:</strong> Combinar datos de múltiples columnas
    ///         en un único objeto
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Validación en mapeo:</strong> Validar datos conforme se extraen de la BD
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Objetos complejos:</strong> Mapear a registros anidados, objetos de valor, etc.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Ciclo de vida del mapper:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         El ORM crea una instancia del mapper (típicamente por inyección de dependencias).
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Para cada fila en los resultados, el ORM llama a <see cref="Map(IDataRecord)"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         El mapper transforma el IDataRecord en una instancia de T.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         El ORM colecta todas las instancias en una colección (List, IEnumerable, etc.).
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Definición de la entidad
    /// public class Usuario
    /// {
    ///     public int UsuarioId { get; set; }
    ///     public string Nombre { get; set; } = "";
    ///     public string Email { get; set; } = "";
    ///     public DateTime FechaCreacion { get; set; }
    ///     public bool Activo { get; set; }
    /// }
    /// 
    /// 
    /// // Ejemplo 1: Mapper simple directo
    /// public class UsuarioMapper : IEntityMapper&lt;Usuario&gt;
    /// {
    ///     public Usuario Map(IDataRecord record)
    ///     {
    ///         return new Usuario
    ///         {
    ///             UsuarioId = (int)record["UsuarioId"],
    ///             Nombre = (string)record["Nombre"],
    ///             Email = (string)record["Email"],
    ///             FechaCreacion = (DateTime)record["FechaCreacion"],
    ///             Activo = (bool)record["Activo"]
    ///         };
    ///     }
    /// }
    /// 
    /// // Uso
    /// var mapper = new UsuarioMapper();
    /// var usuarios = await executor.ExecuteAsync&lt;Usuario&gt;(comando, mapper);
    /// 
    /// 
    /// // Ejemplo 2: Mapper con manejo de nulos
    /// public class UsuarioMapperSafe : IEntityMapper&lt;Usuario&gt;
    /// {
    ///     public Usuario Map(IDataRecord record)
    ///     {
    ///         return new Usuario
    ///         {
    ///             UsuarioId = (int)record["UsuarioId"],
    ///             Nombre = record["Nombre"] as string ?? "",
    ///             Email = record["Email"] as string ?? "",
    ///             FechaCreacion = record["FechaCreacion"] is DBNull 
    ///                 ? DateTime.MinValue 
    ///                 : (DateTime)record["FechaCreacion"],
    ///             Activo = record["Activo"] is DBNull ? false : (bool)record["Activo"]
    ///         };
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Mapper con transformaciones complejas
    /// public class UsuarioMapperAdvanced : IEntityMapper&lt;Usuario&gt;
    /// {
    ///     public Usuario Map(IDataRecord record)
    ///     {
    ///         var usuario = new Usuario
    ///         {
    ///             UsuarioId = (int)record["UsuarioId"],
    ///             Nombre = NormalizarNombre((string)record["Nombre"]),
    ///             Email = ((string)record["Email"]).ToLowerInvariant(),
    ///             FechaCreacion = (DateTime)record["FechaCreacion"],
    ///             Activo = Convert.ToBoolean(record["Activo"])
    ///         };
    ///         
    ///         // Validación en el mapeo
    ///         if (string.IsNullOrEmpty(usuario.Email))
    ///             throw new InvalidOperationException("Email no puede estar vacío");
    ///         
    ///         return usuario;
    ///     }
    ///     
    ///     private string NormalizarNombre(string nombre)
    ///     {
    ///         return System.Globalization.CultureInfo.CurrentCulture
    ///             .TextInfo.ToTitleCase(nombre.ToLowerInvariant());
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Mapper genérico con reflection (más flexible pero más lento)
    /// public class ReflectionMapper&lt;T&gt; : IEntityMapper&lt;T&gt; where T : new()
    /// {
    ///     public T Map(IDataRecord record)
    ///     {
    ///         var entity = new T();
    ///         var properties = typeof(T).GetProperties();
    ///         
    ///         foreach (var prop in properties)
    ///         {
    ///             try
    ///             {
    ///                 var value = record[prop.Name];
    ///                 if (value != DBNull.Value)
    ///                 {
    ///                     prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
    ///                 }
    ///             }
    ///             catch
    ///             {
    ///                 // Ignorar si la columna no existe o conversión falla
    ///             }
    ///         }
    ///         
    ///         return entity;
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 5: Mapper con dependencias inyectadas
    /// public class UsuarioMapperWithDependencies : IEntityMapper&lt;Usuario&gt;
    /// {
    ///     private readonly ILogger&lt;UsuarioMapperWithDependencies&gt; _logger;
    ///     private readonly IValidationService _validator;
    ///     
    ///     public UsuarioMapperWithDependencies(
    ///         ILogger&lt;UsuarioMapperWithDependencies&gt; logger,
    ///         IValidationService validator)
    ///     {
    ///         _logger = logger;
    ///         _validator = validator;
    ///     }
    ///     
    ///     public Usuario Map(IDataRecord record)
    ///     {
    ///         var usuario = new Usuario
    ///         {
    ///             UsuarioId = (int)record["UsuarioId"],
    ///             Nombre = (string)record["Nombre"],
    ///             Email = (string)record["Email"],
    ///             FechaCreacion = (DateTime)record["FechaCreacion"],
    ///             Activo = (bool)record["Activo"]
    ///         };
    ///         
    ///         // Usar servicios inyectados
    ///         if (!_validator.IsValidEmail(usuario.Email))
    ///         {
    ///             _logger.LogWarning($"Email inválido para usuario {usuario.UsuarioId}");
    ///             usuario.Activo = false;
    ///         }
    ///         
    ///         return usuario;
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 6: DTO mapper (resultado de consulta diferente de entidad)
    /// public class UsuarioResumeDto
    /// {
    ///     public int Id { get; set; }
    ///     public string NombreCompleto { get; set; } = "";
    ///     public string Estado { get; set; } = "";
    /// }
    /// 
    /// public class UsuarioResumenMapper : IEntityMapper&lt;UsuarioResumeDto&gt;
    /// {
    ///     public UsuarioResumeDto Map(IDataRecord record)
    ///     {
    ///         // Combinar múltiples columnas en una sola propiedad
    ///         var nombre = (string)record["Nombre"];
    ///         var apellido = (string)record["Apellido"];
    ///         
    ///         // Transformar valores booleanos a texto
    ///         bool activo = (bool)record["Activo"];
    ///         
    ///         return new UsuarioResumeDto
    ///         {
    ///             Id = (int)record["UsuarioId"],
    ///             NombreCompleto = $"{nombre} {apellido}".Trim(),
    ///             Estado = activo ? "Activo" : "Inactivo"
    ///         };
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IEntityMapper<T>
    {
        /// <summary>
        /// Mapea una fila de base de datos (IDataRecord) a una instancia de la entidad tipada.
        /// </summary>
        /// <remarks>
        /// Este método es invocado una vez por cada fila en los resultados de la consulta.
        /// 
        /// <strong>Responsabilidades:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Extracción de valores:</strong> Leer valores del IDataRecord por nombre
        ///         o índice de columna.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Conversión de tipos:</strong> Convertir valores SQL a tipos .NET apropiados.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Manejo de nulos:</strong> Manejar valores nulos o DBNull.Value correctamente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Transformaciones:</strong> Aplicar lógica de transformación
        ///         (normalización, conversión de enum, combinación de columnas, etc.).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Construcción:</strong> Crear e inicializar una nueva instancia de T.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Validación (opcional):</strong> Validar datos conforme se mapean.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Buenas prácticas:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Manejo de excepciones:</strong> Considere lanzar excepciones descriptivas
        ///         si el mapeo falla. El ORM propagará la excepción al llamador.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Cheques de nulos:</strong> Siempre cheque DBNull.Value antes de castear
        ///         valores. Use patrones como <c>record[col] is DBNull ? defaultValue : (T)record[col]</c>.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Índices vs nombres:</strong> Usar índices (record[0], record[1]) es más rápido
        ///         que nombres (record["ColumnName"]), pero menos legible. Considere el trade-off.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Caching de información:</strong> Si el mapper se reutiliza, considere cachear
        ///         información de esquema (índices de columnas) en el constructor.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Rendimiento:</strong> Este método se llama para cada fila. Evite operaciones
        ///         costosas (I/O, llamadas a BD, etc.) dentro del mapper.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Patrón seguro de conversión:</strong>
        /// <code>
        /// // ❌ Inseguro - puede lanzar excepción si el valor es nulo
        /// int id = (int)record["Id"];
        /// 
        /// // ✓ Seguro - chequea nulo primero
        /// int id = record["Id"] is DBNull ? 0 : (int)record["Id"];
        /// 
        /// // ✓ Seguro - convierte a nullable
        /// int? id = record["Id"] is DBNull ? null : (int)record["Id"];
        /// 
        /// // ✓ Seguro - usa as para conversión sin excepción
        /// string? nombre = record["Nombre"] as string;
        /// </code>
        /// </remarks>
        /// <param name="record">
        /// El IDataRecord que contiene los datos de una única fila de la consulta.
        /// Proporciona acceso a las columnas por nombre (record["NombreColumna"]) o índice (record[0]).
        /// Nunca es <c>null</c>.
        /// </param>
        /// <returns>
        /// Una nueva instancia de T completamente inicializada con los datos del registro.
        /// Nunca retorna <c>null</c> (lance una excepción si la conversión no es posible).
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// Se lanza si un valor no puede convertirse al tipo esperado.
        /// Ejemplo: intentar castear "ABC" a int.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        /// Se lanza si intenta acceder a una columna que no existe en el registro.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el mapeo falla por motivos de lógica de negocio
        /// (validación, restricción, dependencia no disponible, etc.).
        /// </exception>
        /// <exception cref="Exception">
        /// Cualquier otra excepción que ocurra durante el mapeo será propagada al llamador.
        /// </exception>
        T Map(IDataRecord record);
    }
}