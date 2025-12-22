using System.Data.Common;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Factory para crear instancias de DbConnection.
    /// Habilita arquitecturas multi-inquilino (multi-tenant), connection strings por solicitud,
    /// políticas de reintentos, y cambio de proveedores de BD sin modificar el núcleo del ORM.
    /// </summary>
    /// <remarks>
    /// Esta interfaz es el punto de entrada para toda la creación de conexiones en BareORM.
    /// Al delegar la creación de conexiones a una factory, se obtienen múltiples beneficios:
    /// 
    /// <strong>Beneficios clave:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Flexibilidad de configuración:</strong> La lógica de creación de conexiones
    ///         puede cambiar en tiempo de ejecución sin afectar el resto del código.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Multi-inquilino (Multi-Tenant):</strong> Diferentes inquilinos pueden usar
    ///         diferentes bases de datos o servidores, determinados en tiempo de ejecución.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Inyección de dependencias:</strong> La factory puede ser inyectada y
    ///         reemplazada para testing, alternativas de desarrollo, etc.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Políticas de reintentos:</strong> La factory puede aplicar reintentos
    ///         automáticos en caso de fallos de conexión transitorios.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Cambio de proveedores:</strong> Cambiar entre SQL Server, PostgreSQL, MySQL, etc.
    ///         sin modificar el código principal.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Connection pooling:</strong> Delega la administración de pools de conexiones
    ///         a la factory, que puede optimizarlos según la estrategia.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Aplicaciones multi-inquilino (SaaS):</strong> Cada inquilino se conecta a su propia
    ///         base de datos o esquema.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Sharding:</strong> Distribuir datos entre múltiples servidores basándose
    ///         en una clave de partición.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Lectura/Escritura dividida (Read/Write Splitting):</strong> Usar diferentes
    ///         servidores para lecturas y escrituras.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Failover automático:</strong> Cambiar de servidor en caso de fallo.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Testing e inyección:</strong> Usar bases de datos en memoria, mock o alternativas
    ///         en pruebas unitarias e integración.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Ambientes dinámicos:</strong> Cambiar entre desarrollo, staging y producción
    ///         sin recompilar.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Responsabilidades típicas de una implementación:</strong>
    /// <list type="bullet">
    ///     <item><description>Seleccionar la cadena de conexión apropiada</description></item>
    ///     <item><description>Crear la instancia de DbConnection correcta (SqlConnection, NpgsqlConnection, etc.)</description></item>
    ///     <item><description>Configurar opciones de conexión (timeout, enumeración, compresión, etc.)</description></item>
    ///     <item><description>Manejar reintentos y fallbacks</description></item>
    ///     <item><description>Registrar la creación de conexiones para auditoría/diagnóstico</description></item>
    ///     <item><description>Aplicar políticas de seguridad (credenciales, encriptación)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ejemplo 1: Factory simple estática
    /// public class SimpleConnectionFactory : IConnectionFactory
    /// {
    ///     private readonly string _connectionString;
    ///     
    ///     public SimpleConnectionFactory(string connectionString)
    ///     {
    ///         _connectionString = connectionString;
    ///     }
    ///     
    ///     public DbConnection CreateConnection()
    ///     {
    ///         var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
    ///         connection.Open();
    ///         return connection;
    ///     }
    /// }
    /// 
    /// // Usar en la aplicación
    /// var factory = new SimpleConnectionFactory("Server=localhost;Database=MiDB;");
    /// var miORM = new BareORM(factory);
    /// 
    /// 
    /// // Ejemplo 2: Factory multi-inquilino (Multi-Tenant)
    /// public class MultiTenantConnectionFactory : IConnectionFactory
    /// {
    ///     private readonly IHttpContextAccessor _httpContextAccessor;
    ///     private readonly Dictionary&lt;string, string&gt; _tenantConnections;
    ///     
    ///     public MultiTenantConnectionFactory(
    ///         IHttpContextAccessor httpContextAccessor,
    ///         Dictionary&lt;string, string&gt; tenantConnections)
    ///     {
    ///         _httpContextAccessor = httpContextAccessor;
    ///         _tenantConnections = tenantConnections;
    ///     }
    ///     
    ///     public DbConnection CreateConnection()
    ///     {
    ///         // Obtener el inquilino actual de la solicitud HTTP
    ///         var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant-id")?.Value
    ///             ?? throw new InvalidOperationException("Inquilino no identificado");
    ///         
    ///         if (!_tenantConnections.TryGetValue(tenantId, out var connectionString))
    ///             throw new InvalidOperationException($"Inquilino '{tenantId}' no encontrado");
    ///         
    ///         var connection = new System.Data.SqlClient.SqlConnection(connectionString);
    ///         connection.Open();
    ///         return connection;
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 3: Factory con reintentos automáticos
    /// public class RetryConnectionFactory : IConnectionFactory
    /// {
    ///     private readonly string _connectionString;
    ///     private const int MaxRetries = 3;
    ///     private const int RetryDelayMs = 100;
    ///     
    ///     public RetryConnectionFactory(string connectionString)
    ///     {
    ///         _connectionString = connectionString;
    ///     }
    ///     
    ///     public DbConnection CreateConnection()
    ///     {
    ///         int attempt = 0;
    ///         while (attempt &lt; MaxRetries)
    ///         {
    ///             try
    ///             {
    ///                 var connection = new System.Data.SqlClient.SqlConnection(_connectionString);
    ///                 connection.Open();
    ///                 return connection;
    ///             }
    ///             catch (Exception ex) when (IsTransient(ex) &amp;&amp; attempt &lt; MaxRetries - 1)
    ///             {
    ///                 attempt++;
    ///                 System.Threading.Thread.Sleep(RetryDelayMs * attempt);
    ///                 continue;
    ///             }
    ///         }
    ///         
    ///         throw new InvalidOperationException("No se pudo establecer conexión después de reintentos");
    ///     }
    ///     
    ///     private bool IsTransient(Exception ex)
    ///     {
    ///         // Detectar errores transitorios comunes
    ///         return ex.Message.Contains("timeout") 
    ///             || ex.Message.Contains("connection lost")
    ///             || ex.InnerException?.Message.Contains("A network-related") == true;
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 4: Factory con Read/Write Splitting
    /// public class ReadWriteSplitConnectionFactory : ICommandObserver
    /// {
    ///     private readonly string _writeConnectionString;
    ///     private readonly string _readConnectionString;
    ///     private CommandType _lastCommandType;
    ///     
    ///     public ReadWriteSplitConnectionFactory(
    ///         string writeConnectionString,
    ///         string readConnectionString)
    ///     {
    ///         _writeConnectionString = writeConnectionString;
    ///         _readConnectionString = readConnectionString;
    ///     }
    ///     
    ///     public DbConnection CreateConnection()
    ///     {
    ///         // Determinar si es lectura o escritura basándose en el comando anterior
    ///         bool isRead = _lastCommandType == CommandType.Text &amp;&amp; 
    ///                      _lastCommandType.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
    ///         
    ///         var connectionString = isRead ? _readConnectionString : _writeConnectionString;
    ///         var connection = new System.Data.SqlClient.SqlConnection(connectionString);
    ///         connection.Open();
    ///         return connection;
    ///     }
    /// }
    /// 
    /// 
    /// // Ejemplo 5: Factory por contexto (para testing)
    /// public class ContextualConnectionFactory : IConnectionFactory
    /// {
    ///     private static readonly ThreadLocal&lt;string?&gt; _contextConnectionString = 
    ///         new ThreadLocal&lt;string?&gt;();
    ///     private readonly string _defaultConnectionString;
    ///     
    ///     public ContextualConnectionFactory(string defaultConnectionString)
    ///     {
    ///         _defaultConnectionString = defaultConnectionString;
    ///     }
    ///     
    ///     public DbConnection CreateConnection()
    ///     {
    ///         var connectionString = _contextConnectionString.Value ?? _defaultConnectionString;
    ///         var connection = new System.Data.SqlClient.SqlConnection(connectionString);
    ///         connection.Open();
    ///         return connection;
    ///     }
    ///     
    ///     /// &lt;summary&gt;
    ///     /// Para testing: establecer temporalmente una conexión diferente en el hilo actual
    ///     /// &lt;/summary&gt;
    ///     public static void SetContextConnection(string connectionString)
    ///     {
    ///         _contextConnectionString.Value = connectionString;
    ///     }
    ///     
    ///     /// &lt;summary&gt;
    ///     /// Para testing: limpiar la conexión contextual
    ///     /// &lt;/summary&gt;
    ///     public static void ClearContextConnection()
    ///     {
    ///         _contextConnectionString.Value = null;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Crea y devuelve una nueva instancia de DbConnection lista para usar.
        /// </summary>
        /// <remarks>
        /// Este método es responsable de:
        /// 
        /// <list type="number">
        ///     <item>
        ///         <description>
        ///         <strong>Seleccionar la cadena de conexión:</strong> Basándose en el contexto actual
        ///         (inquilino, usuario, tipo de operación, etc.), seleccionar la cadena de conexión apropiada.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Crear la conexión:</strong> Instanciar el tipo correcto de DbConnection
        ///         (SqlConnection, NpgsqlConnection, MySqlConnection, etc.).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Configurar la conexión:</strong> Establecer opciones como timeout, collation,
        ///         enumeración, compresión, etc.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Abrir la conexión:</strong> Establecer la conexión física con la base de datos.
        ///         La conexión devuelta debe estar abierta y lista para usar.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Manejar reintentos:</strong> Si es necesario, reintentar en caso de fallos transitorios.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Registrar:</strong> Opcionalmente, registrar la creación de la conexión para
        ///         auditoría o diagnóstico.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Garantías esperadas:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Conexión abierta:</strong> La DbConnection devuelta está abierta (<c>State == ConnectionState.Open</c>)
        ///         y lista para ejecutar comandos.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Proveedor correcto:</strong> El tipo de DbConnection corresponde al proveedor
        ///         esperado (la factory debe garantizar coherencia).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Contexto aplicado:</strong> Cualquier lógica de contexto (multi-tenant, usuario actual, etc.)
        ///         ha sido aplicada correctamente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Seguridad:</strong> Credenciales y encriptación se han configurado según las políticas.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Responsabilidad del llamador (ORM):</strong>
        /// El ORM es responsable de cerrar/disponer la conexión después de usarla. Por lo tanto,
        /// la factory no debe intentar gestionar el ciclo de vida de la conexión devuelta.
        /// 
        /// <strong>Excepciones esperadas:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>InvalidOperationException:</strong> Cuando no se puede determinar el contexto
        ///         (ej: no se puede identificar el inquilino actual).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>DbException / SqlException:</strong> Cuando falla la conexión a la base de datos
        ///         (servidor no disponible, credenciales inválidas, timeout, etc.).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>ArgumentException:</strong> Cuando la cadena de conexión es inválida o incompleta.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Consideraciones de rendimiento:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Esta factory se llama para cada operación de base de datos. Debe ser eficiente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Se recomienda confiar en ADO.NET connection pooling para minimizar el overhead
        ///         de crear conexiones físicas reales.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         La lógica de contexto (ej: buscar el inquilino) debe ser rápida o cacheada.
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// Una instancia de <see cref="DbConnection"/> totalmente configurada, abierta y lista para ejecutar comandos.
        /// Nunca retorna <c>null</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza cuando la factory no puede determinar el contexto necesario
        /// (ej: inquilino no identificado, contexto de usuario no disponible).
        /// </exception>
        /// <exception cref="DbException">
        /// Se lanza cuando falla la conexión a la base de datos después de reintentos
        /// (servidor no disponible, credenciales inválidas, timeout, etc.).
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Se lanza cuando la cadena de conexión es inválida, incompleta, o no compatible
        /// con el proveedor.
        /// </exception>
        DbConnection CreateConnection();
    }
}