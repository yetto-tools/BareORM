using System.Data.Common;
using Microsoft.Data.SqlClient;
using BareORM.Abstractions;

namespace BareORM.SqlServer
{
    /// <summary>
    /// Factory para crear conexiones a SQL Server.
    /// </summary>
    /// <remarks>
    /// La clase <c>SqlServerConnectionFactory</c> implementa <see cref="IConnectionFactory"/>
    /// y es responsable de crear instancias de <see cref="SqlConnection"/> configuradas
    /// con la cadena de conexión proporcionada.
    /// 
    /// <strong>Características:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         Proporciona una abstracción para la creación de conexiones a SQL Server.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Sigue el patrón Factory para encapsular la lógica de creación de conexiones.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Valida que la cadena de conexión sea válida en el constructor.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         Crea una nueva conexión independiente en cada llamada a <see cref="CreateConnection"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Uso típico:</strong>
    /// <code>
    /// // Registrar en contenedor de inyección de dependencias
    /// services.AddSingleton&lt;IConnectionFactory&gt;(
    ///     new SqlServerConnectionFactory("Server=localhost;Database=MyDb;User Id=sa;Password=pass"));
    /// 
    /// // Usar a través de la interfaz IConnectionFactory
    /// using (var connection = connectionFactory.CreateConnection())
    /// {
    ///     connection.Open();
    ///     // Usar la conexión...
    /// }
    /// </code>
    /// </remarks>
    /// <seealso cref="IConnectionFactory"/>
    /// <seealso cref="SqlConnection"/>
    public sealed class SqlServerConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="SqlServerConnectionFactory"/>.
        /// <para>
        /// <param name="connectionString">
        /// <strong>El parametro tipo <see href="string"/> <paramref name="connectionString"/> no puede ser una cadena nula, vacía ni contener solo espacios en blanco.</strong>
        /// </param>
        /// </para>
        /// </summary>
        /// <remarks>
        /// El constructor valida que la cadena de conexión no sea nula o vacía.<br/>
        /// La cadena de conexión se almacena internamente y se utiliza para crear<br/>
        /// nuevas instancias de <see cref="SqlConnection"/> en cada llamada a <see cref="CreateConnection"/>.
        /// <example>
        /// <code>
        /// Creación válida:
        ///     var factory = new SqlServerConnectionFactory("Server=localhost;Database=MyDb;User Id=sa;Password=pass");
        /// 
        /// Lanzará ArgumentException
        ///     var invalidFactory = new SqlServerConnectionFactory("");
        /// </code>
        /// </example>
        /// 
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="connectionString"/> es nula, vacía o contiene solo espacios en blanco.
        /// El mensaje de error es "Connection string is required.".
        /// </exception>

        public SqlServerConnectionFactory(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required.", nameof(connectionString));
            _connectionString = connectionString;
        }

        /// <summary>
        /// Crea una nueva conexión a SQL Server.
        /// </summary>
        /// <remarks>
        /// Este método implementa <see cref="IConnectionFactory.CreateConnection"/> y proporciona
        /// una nueva instancia de <see cref="SqlConnection"/> configurada con la cadena de conexión
        /// especificada en el constructor.
        /// 
        /// <strong>Comportamiento:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Crea una nueva instancia de conexión en cada llamada (no reutiliza conexiones).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         La conexión retornada no está abierta. El llamador es responsable de
        ///         llamar a <see cref="DbConnection.Open"/> antes de usarla.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         El llamador debe disponer de la conexión cuando termine de usarla
        ///         (preferiblemente usando una declaración <c>using</c>).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Mejores prácticas:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Siempre use <c>using</c> para asegurar que la conexión se dispose correctamente.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         No mantenga conexiones abiertas más tiempo del necesario.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Considere usar un pool de conexiones (SQL Server lo hace automáticamente
        ///         con cadenas de conexión estándar).
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <returns>
        /// Una nueva instancia de <see cref="SqlConnection"/> configurada con la cadena de conexión.
        /// La conexión no está abierta y debe ser abierta explícitamente antes de usarla.
        /// </returns>
        /// <example>
        /// <code>
        /// var factory = new SqlServerConnectionFactory("Server=localhost;Database=MyDb;User Id=sa;Password=pass");
        /// 
        /// // Patrón recomendado con using
        /// using (var connection = factory.CreateConnection())
        /// {
        ///     connection.Open();
        ///     using (var command = connection.CreateCommand())
        ///     {
        ///         command.CommandText = "SELECT COUNT(*) FROM Users";
        ///         var count = (int)command.ExecuteScalar();
        ///         Console.WriteLine($"Total users: {count}");
        ///     }
        /// }
        /// </code>
        /// </example>
        public DbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}