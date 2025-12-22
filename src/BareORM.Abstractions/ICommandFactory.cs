using System.Data;
using System.Data.Common;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Factory específica del proveedor que construye CommandDefinition y DbParameter
    /// a partir de entradas amigables del usuario (objetos anónimos, diccionarios, descriptores DbParam, etc.).
    /// </summary>
    /// <remarks>
    /// Esta interfaz actúa como un puente de conversión entre las abstracciones agnósticas del ORM
    /// y los tipos específicos del proveedor de base de datos. Traduce descripciones de parámetros
    /// de alto nivel a parámetros nativos tipados (SqlParameter, NpgsqlParameter, MySqlParameter, etc.).
    /// 
    /// <strong>Beneficios de usar una factory:</strong>
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         <strong>Abstracción agnóstica:</strong> El código cliente no necesita conocer detalles
    ///         específicos de SQL Server, PostgreSQL, MySQL, etc.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Flexibilidad de entrada:</strong> Acepta múltiples formatos (objetos anónimos,
    ///         diccionarios, DbParam, etc.) para máxima comodidad del usuario.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Validación centralizada:</strong> Valida parámetros, tipos y restricciones
    ///         en un único lugar.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///         <strong>Mapeo de tipos inteligente:</strong> Convierte tipos .NET a tipos SQL automáticamente.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <strong>Casos de uso típicos:</strong>
    /// - Crear comandos simples con parámetros usando sintaxis amigable
    /// - Convertir objetos POCO a parámetros de base de datos
    /// - Construir procedimientos almacenados con parámetros OUTPUT
    /// - Trabajar con diccionarios dinámicos de parámetros
    /// </remarks>
    /// <example>
    /// <code>
    /// // Obtener la factory del ORM
    /// var factory = miORM.GetCommandFactory();
    /// 
    /// // Ejemplo 1: Crear comando con objeto anónimo
    /// var cmd1 = factory.Create(
    ///     commandText: "sp_ActualizarUsuario",
    ///     commandType: CommandType.StoredProcedure,
    ///     parameters: new { usuarioId = 42, nombre = "Juan" }
    /// );
    /// 
    /// // Ejemplo 2: Crear comando con diccionario
    /// var parametros = new Dictionary&lt;string, object&gt;
    /// {
    ///     { "@id", 42 },
    ///     { "@estado", "Activo" }
    /// };
    /// var cmd2 = factory.Create(
    ///     "SELECT * FROM Usuarios WHERE Id = @id AND Estado = @estado",
    ///     CommandType.Text,
    ///     parametros
    /// );
    /// 
    /// // Ejemplo 3: Crear comando con descriptores DbParam (mayor control)
    /// var descriptores = new List&lt;DbParam&gt;
    /// {
    ///     new DbParam("@nuevoId", DbType: System.Data.DbType.Int32, Direction: ParameterDirection.Output),
    ///     new DbParam("@nombre", Value: "Juan", DbType: System.Data.DbType.String, Size: 100),
    ///     new DbParam("@activo", Value: true, DbType: System.Data.DbType.Boolean)
    /// };
    /// var cmd3 = factory.Create(
    ///     "sp_CrearUsuario",
    ///     CommandType.StoredProcedure,
    ///     descriptores
    /// );
    /// 
    /// // Ejemplo 4: Crear parámetros individuales desde DbParam
    /// var param = factory.CreateParameter(
    ///     new DbParam("@precio", 99.99m, DbType.Decimal, Precision: 10, Scale: 2)
    /// );
    /// </code>
    /// </example>
    public interface ICommandFactory
    {
        /// <summary>
        /// Crea una definición de comando a partir de texto/nombre y parámetros de objeto flexible.
        /// </summary>
        /// <remarks>
        /// Esta sobrecarga acepta parámetros como un objeto (típicamente anónimo o diccionario)
        /// y los convierte automáticamente a parámetros nativos del proveedor.
        /// 
        /// <strong>Formatos de parámetros soportados:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Objeto anónimo:</strong> <c>new { usuarioId = 42, nombre = "Juan" }</c>
        ///         Los nombres de propiedades se convierten a parámetros (con @ si es necesario).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Diccionario:</strong> <c>new Dictionary&lt;string, object&gt; { { "@id", 42 } }</c>
        ///         Las claves se usan como nombres de parámetros.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>POCO (Plain Old CLR Object):</strong> Un objeto con propiedades públicas.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong><c>null</c>:</strong> Comando sin parámetros.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// <strong>Mapeo de tipos:</strong>
        /// El factory intenta mapear automáticamente tipos .NET a tipos SQL:
        /// - int/long → Int32/Int64
        /// - string → String/NVarChar
        /// - DateTime → DateTime
        /// - decimal → Decimal
        /// - bool → Boolean
        /// - byte[] → Binary
        /// - etc.
        /// 
        /// Para mayor control sobre tipos, use la sobrecarga con <see cref="IEnumerable{DbParam}"/>.
        /// </remarks>
        /// <param name="commandText">
        /// El nombre del procedimiento almacenado o el texto SQL.
        /// No puede ser nulo ni vacío.
        /// </param>
        /// <param name="commandType">
        /// El tipo de comando. El valor predeterminado es <c>CommandType.StoredProcedure</c>.
        /// </param>
        /// <param name="parameters">
        /// Objeto que contiene los parámetros (anónimo, diccionario, POCO, etc.).
        /// Puede ser <c>null</c> si no hay parámetros.
        /// </param>
        /// <param name="timeoutSeconds">
        /// Timeout en segundos para la ejecución. El valor predeterminado es 30.
        /// Debe ser mayor que 0.
        /// </param>
        /// <returns>
        /// Una instancia de <see cref="CommandDefinition"/> lista para ejecutarse.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="commandText"/> es nulo o vacío.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="timeoutSeconds"/> es menor o igual a 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si el mapeo de tipos falla o si hay tipos no soportados en los parámetros.
        /// </exception>
        CommandDefinition Create(
            string commandText,
            CommandType commandType = CommandType.StoredProcedure,
            object? parameters = null,
            int timeoutSeconds = 30);

        /// <summary>
        /// Crea una definición de comando a partir de descriptores DbParam tipados.
        /// </summary>
        /// <remarks>
        /// Esta sobrecarga acepta una colección de <see cref="DbParam"/>, que proporciona
        /// control total sobre cada parámetro: nombre, valor, tipo, dirección, tamaño,
        /// precisión, escala, etc.
        /// 
        /// Use esta sobrecarga cuando necesite:
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         Parámetros OUTPUT que devuelven valores desde el servidor.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Control explícito de tipos SQL (ej: especificar Decimal con precisión/escala).
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Parámetros de valor de tabla (TVP) o tipos definidos por usuario.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         Parámetros con restricciones de tamaño específicas.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// El factory valida que cada descriptor sea válido antes de crear el comando.
        /// </remarks>
        /// <param name="commandText">
        /// El nombre del procedimiento almacenado o el texto SQL.
        /// No puede ser nulo ni vacío.
        /// </param>
        /// <param name="commandType">
        /// El tipo de comando (StoredProcedure o Text).
        /// </param>
        /// <param name="parameters">
        /// Colección de descriptores <see cref="DbParam"/> que describen cada parámetro.
        /// No puede ser <c>null</c>, pero puede estar vacía.
        /// </param>
        /// <param name="timeoutSeconds">
        /// Timeout en segundos para la ejecución. El valor predeterminado es 30.
        /// Debe ser mayor que 0.
        /// </param>
        /// <returns>
        /// Una instancia de <see cref="CommandDefinition"/> lista para ejecutarse.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="commandText"/> es nulo o vacío,
        /// o si algún parámetro en la colección es inválido.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="parameters"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="timeoutSeconds"/> es menor o igual a 0.
        /// </exception>
        CommandDefinition Create(
            string commandText,
            CommandType commandType,
            IEnumerable<DbParam> parameters,
            int timeoutSeconds = 30);

        /// <summary>
        /// Convierte un objeto flexible (anónimo, diccionario, POCO) a una lista de parámetros
        /// específicos del proveedor.
        /// </summary>
        /// <remarks>
        /// Este método extrae parámetros de una fuente flexible y los traduce a parámetros
        /// nativos del proveedor de base de datos.
        /// 
        /// <strong>Comportamiento por tipo de entrada:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Objeto anónimo:</strong> Usa reflexión para extraer propiedades públicas.
        ///         Cada propiedad se convierte en un parámetro.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>IDictionary:</strong> Itera las claves y valores del diccionario.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>POCO:</strong> Usa reflexión similar al objeto anónimo.
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// Los nombres de parámetros se normalizan automáticamente (se añade @ si falta).
        /// </remarks>
        /// <param name="parameters">
        /// Objeto que contiene los parámetros. Puede ser anónimo, diccionario, POCO, etc.
        /// </param>
        /// <returns>
        /// Una colección de solo lectura de parámetros específicos del proveedor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="parameters"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ocurre un error durante la conversión de tipos.
        /// </exception>
        IReadOnlyList<DbParameter> CreateParameters(object parameters);

        /// <summary>
        /// Convierte una colección de descriptores DbParam a parámetros específicos del proveedor.
        /// </summary>
        /// <remarks>
        /// Este método traduce cada <see cref="DbParam"/> (formato agnóstico) a su equivalente
        /// específico del proveedor (SqlParameter, NpgsqlParameter, MySqlParameter, etc.).
        /// 
        /// Valida cada descriptor antes de la conversión para asegurar que todos los
        /// parámetros sean válidos y consistentes.
        /// 
        /// El orden de los parámetros en la colección devuelta coincide con el orden
        /// de entrada.
        /// </remarks>
        /// <param name="parameters">
        /// Colección de descriptores DbParam que describen cada parámetro.
        /// No puede ser <c>null</c>.
        /// </param>
        /// <returns>
        /// Una colección de solo lectura de parámetros específicos del proveedor.
        /// Si la entrada está vacía, devuelve una colección vacía.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="parameters"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ocurre un error durante la conversión de algún descriptor.
        /// </exception>
        IReadOnlyList<DbParameter> CreateParameters(IEnumerable<DbParam> parameters);

        /// <summary>
        /// Crea un único parámetro específico del proveedor a partir de un descriptor DbParam.
        /// </summary>
        /// <remarks>
        /// Este método realiza la conversión de un descriptor agnóstico (<see cref="DbParam"/>)
        /// a un parámetro nativo específico del proveedor.
        /// 
        /// <strong>Comportamiento por propiedad del descriptor:</strong>
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///         <strong>Name:</strong> Se normaliza y se añade @ si es necesario.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Value:</strong> Se convierte según el tipo especificado o inferido.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>DbType:</strong> Si se proporciona, se usa para crear el parámetro tipado.
        ///         Si es null, el factory intenta inferir el tipo del valor.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>Direction, Size, Precision, Scale:</strong> Se asignan directamente
        ///         al parámetro nativo si es soportado.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <description>
        ///         <strong>TypeName:</strong> Se usa para tipos especializados (TVP, UDT, etc.).
        ///         </description>
        ///     </item>
        /// </list>
        /// 
        /// Este método es útil cuando se necesita crear un parámetro individual de forma
        /// programática, sin construir un comando completo.
        /// </remarks>
        /// <param name="param">
        /// El descriptor del parámetro que contiene toda la información necesaria.
        /// No puede ser <c>null</c>.
        /// </param>
        /// <returns>
        /// Un parámetro específico del proveedor totalmente configurado.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Se lanza si <paramref name="param"/> es <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si ocurre un error durante la conversión o si el descriptor contiene
        /// valores inválidos.
        /// </exception>
        DbParameter CreateParameter(DbParam param);
    }
}