using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace BareORM.Abstractions
{
    /// <summary>
    /// Descripción inmutable de un comando de base de datos (Procedimiento Almacenado o texto SQL).
    /// Se comparte entre el núcleo del ORM y los proveedores específicos.
    /// </summary>
    /// <remarks>
    /// Esta clase encapsula toda la información necesaria para ejecutar un comando en la base de datos
    /// de manera agnóstica. Puede representar tanto procedimientos almacenados como consultas SQL
    /// directas. Su naturaleza inmutable garantiza que los parámetros no pueden ser modificados
    /// después de la creación, proporcionando seguridad en ambientes multihilo.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ejemplo con Procedimiento Almacenado
    /// var parametros = new List&lt;DbParameter&gt;
    /// {
    ///     new SqlParameter("@usuarioId", SqlDbType.Int) { Value = 42 },
    ///     new SqlParameter("@estado", SqlDbType.NVarChar) { Value = "Activo" }
    /// };
    /// 
    /// var comando = new CommandDefinition(
    ///     commandText: "sp_ObtenerUsuario",
    ///     commandType: CommandType.StoredProcedure,
    ///     parameters: parametros,
    ///     timeoutSeconds: 30
    /// );
    /// 
    /// // Ejemplo con SQL directo
    /// var comandoSQL = new CommandDefinition(
    ///     commandText: "SELECT * FROM Usuarios WHERE UsuarioId = @id",
    ///     commandType: CommandType.Text,
    ///     parameters: new List&lt;DbParameter&gt;
    ///     {
    ///         new SqlParameter("@id", SqlDbType.Int) { Value = 42 }
    ///     },
    ///     timeoutSeconds: 60
    /// );
    /// </code>
    /// </example>
    public sealed class CommandDefinition
    {
        /// <summary>
        /// Obtiene el nombre del procedimiento almacenado o el texto SQL del comando.
        /// </summary>
        /// <remarks>
        /// Si <see cref="CommandType"/> es <c>StoredProcedure</c>, este valor contiene el nombre
        /// del procedimiento. Si <see cref="CommandType"/> es <c>Text</c>, contiene la consulta SQL completa.
        /// Este valor nunca es nulo ni vacío debido a las validaciones del constructor.
        /// </remarks>
        /// <value>El nombre del procedimiento o texto SQL del comando.</value>
        public string CommandText { get; }

        /// <summary>
        /// Obtiene el tipo de comando: Procedimiento Almacenado o Texto SQL.
        /// </summary>
        /// <remarks>
        /// Determina cómo el proveedor de base de datos interpreta el valor de <see cref="CommandText"/>.
        /// <list type="table">
        ///     <listheader>
        ///         <term>Valor</term>
        ///         <description>Descripción</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>CommandType.StoredProcedure</c></term>
        ///         <description>CommandText es el nombre de un procedimiento almacenado.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>CommandType.Text</c></term>
        ///         <description>CommandText es una consulta SQL directa.</description>
        ///     </item>
        /// </list>
        /// El valor predeterminado es <c>StoredProcedure</c>.
        /// </remarks>
        /// <value>El tipo de comando como <see cref="CommandType"/>.</value>
        public CommandType CommandType { get; }

        /// <summary>
        /// Obtiene la lista de parámetros a pasar al comando, o <c>null</c> si no hay parámetros.
        /// </summary>
        /// <remarks>
        /// Contiene los parámetros necesarios para el comando. Cada parámetro debe tener un nombre,
        /// tipo de dato y valor correspondiente. La lista es de solo lectura, asegurando que
        /// los parámetros no puedan ser modificados después de la creación del comando.
        /// 
        /// Los parámetros son específicos del proveedor (SqlParameter para SQL Server,
        /// NpgsqlParameter para PostgreSQL, etc.).
        /// </remarks>
        /// <value>
        /// Una colección de solo lectura de <see cref="DbParameter"/>, o <c>null</c> si el
        /// comando no requiere parámetros.
        /// </value>
        public IReadOnlyList<DbParameter>? Parameters { get; }

        /// <summary>
        /// Obtiene el tiempo máximo de espera en segundos para la ejecución del comando.
        /// </summary>
        /// <remarks>
        /// Define cuánto tiempo el ORM esperará a que el comando se complete antes de
        /// cancelarlo y lanzar una excepción de timeout. Valores típicos son:
        /// <list type="bullet">
        ///     <item><description>30 segundos: Comandos rápidos (consultas, inserciones simples)</description></item>
        ///     <item><description>60 segundos: Comandos moderados (operaciones en lote, reportes)</description></item>
        ///     <item><description>300+ segundos: Operaciones largas (migraciones, procesamiento masivo)</description></item>
        /// </list>
        /// El valor debe ser mayor que 0. El valor predeterminado es 30 segundos.
        /// </remarks>
        /// <value>El timeout en segundos. Siempre mayor que 0.</value>
        public int TimeoutSeconds { get; }

        /// <summary>
        /// Obtiene el comportamiento del lector de datos para la ejecución del comando.
        /// </summary>
        /// <remarks>
        /// Define cómo el lector de datos se comportará durante la lectura de resultados.
        /// Los valores comunes incluyen:
        /// <list type="table">
        ///     <listheader>
        ///         <term>Valor</term>
        ///         <description>Descripción</description>
        ///     </listheader>
        ///     <item>
        ///         <term><c>CommandBehavior.Default</c></term>
        ///         <description>Comportamiento estándar, la conexión permanece abierta.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>CommandBehavior.SequentialAccess</c></term>
        ///         <description>Permite lectura secuencial de datos grandes, más eficiente en memoria.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>CommandBehavior.SingleRow</c></term>
        ///         <description>Optimización cuando se espera un único resultado.</description>
        ///     </item>
        ///     <item>
        ///         <term><c>CommandBehavior.CloseConnection</c></term>
        ///         <description>Cierra la conexión cuando se cierra el lector.</description>
        ///     </item>
        /// </list>
        /// El valor predeterminado es <c>CommandBehavior.Default</c>.
        /// </remarks>
        /// <value>El comportamiento del lector como <see cref="CommandBehavior"/>.</value>
        public CommandBehavior ReaderBehavior { get; }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CommandDefinition"/>.
        /// </summary>
        /// <param name="commandText">
        /// El nombre del procedimiento almacenado o el texto SQL del comando.
        /// No puede ser nulo ni estar vacío.
        /// </param>
        /// <param name="commandType">
        /// El tipo de comando. El valor predeterminado es <see cref="CommandType.StoredProcedure"/>.
        /// </param>
        /// <param name="parameters">
        /// Una colección de parámetros para el comando, o <c>null</c> si no hay parámetros.
        /// </param>
        /// <param name="timeoutSeconds">
        /// El tiempo máximo de espera en segundos. Debe ser mayor que 0.
        /// El valor predeterminado es 30 segundos.
        /// </param>
        /// <param name="readerBehavior">
        /// El comportamiento del lector de datos. El valor predeterminado es <see cref="CommandBehavior.Default"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Se lanza si <paramref name="commandText"/> es nulo, vacío o solo contiene espacios en blanco.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="timeoutSeconds"/> es menor o igual a 0.
        /// </exception>
        public CommandDefinition(
            string commandText,
            CommandType commandType = CommandType.StoredProcedure,
            IReadOnlyList<DbParameter>? parameters = null,
            int timeoutSeconds = 30,
            CommandBehavior readerBehavior = CommandBehavior.Default)
        {
            if (string.IsNullOrWhiteSpace(commandText))
                throw new ArgumentException("CommandText cannot be null or empty.", nameof(commandText));
            if (timeoutSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "TimeoutSeconds must be greater than 0.");

            CommandText = commandText;
            CommandType = commandType;
            Parameters = parameters;
            TimeoutSeconds = timeoutSeconds;
            ReaderBehavior = readerBehavior;
        }
    }
}