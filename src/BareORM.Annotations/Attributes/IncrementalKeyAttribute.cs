namespace BareORM.Annotations.Attributes
{


    /// <summary>
    /// Marca una propiedad como clave incremental generada automáticamente por la base de datos.
    /// El motor SQL lo asigna a los mecanismos nativos correspondientes: IDENTITY (SQL Server),
    /// AUTO_INCREMENT (MySQL), SERIAL (PostgreSQL), SEQUENCE (Oracle) o ROWID (SQLite).
    /// </summary>
    /// <remarks>
    /// Este atributo se utiliza en modelos de datos para indicar que un campo es una clave primaria
    /// autoincrementable cuyo valor es generado y gestionado completamente por la base de datos.
    /// El ORM es responsable de recuperar el valor generado después de una inserción si es necesario.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Usuario
    /// {
    ///     [IncrementalKey(RetrieveAfterInsert = true)]
    ///     public long UsuarioId { get; set; }
    ///     
    ///     public string Nombre { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IncrementalKeyAttribute : Attribute
    {
        /// <summary>
        /// Obtiene o establece el nombre opcional de la secuencia personalizada para bases de datos
        /// como Oracle o PostgreSQL, cuando se desea un control explícito sobre la secuencia.
        /// </summary>
        /// <remarks>
        /// Si este valor es <c>null</c>, el proveedor de la base de datos utilizará su mecanismo
        /// de generación de claves predeterminado (IDENTITY, AUTO_INCREMENT, SERIAL, etc.).
        /// </remarks>
        /// <value>
        /// El nombre de la secuencia personalizada, o <c>null</c> para usar el comportamiento predeterminado.
        /// </value>
        public string? SequenceName { get; init; }

        /// <summary>
        /// Obtiene o establece un valor que indica si el ORM debe recuperar el valor de clave
        /// generado inmediatamente después de ejecutar la inserción en la base de datos.
        /// </summary>
        /// <remarks>
        /// Cuando este valor es <c>true</c>, el ORM ejecutará una consulta adicional (como
        /// SCOPE_IDENTITY(), LAST_INSERT_ID(), o similar) para obtener el valor asignado.
        /// Establecer este valor en <c>false</c> puede mejorar el rendimiento si el valor generado
        /// no es necesario inmediatamente.
        /// </remarks>
        /// <value>
        /// <c>true</c> si se debe recuperar el valor generado; de lo contrario, <c>false</c>.
        /// El valor predeterminado es <c>true</c>.
        /// </value>
        public bool RetrieveAfterInsert { get; init; } = true;

        /// <summary>
        /// Obtiene o establece una sugerencia opcional del valor inicial de la secuencia.
        /// </summary>
        /// <remarks>
        /// Este valor es meramente informativo y el proveedor de la base de datos puede ignorarlo.
        /// Se utiliza para dar una indicación de desde qué valor debe comenzar la generación de claves.
        /// </remarks>
        /// <value>
        /// El número desde el cual la secuencia debe comenzar, o <c>null</c> para dejar que
        /// la base de datos use su valor predeterminado.
        /// </value>
        public long? StartWith { get; init; }

        /// <summary>
        /// Obtiene o establece una sugerencia opcional del incremento entre valores generados sucesivamente.
        /// </summary>
        /// <remarks>
        /// Este valor es meramente informativo y el proveedor de la base de datos puede ignorarlo.
        /// Se utiliza para dar una indicación de cuánto debe incrementarse la secuencia en cada generación.
        /// Por ejemplo, un valor de 2 genera: 1, 3, 5, 7, etc.
        /// </remarks>
        /// <value>
        /// El valor de incremento de la secuencia, o <c>null</c> para usar el incremento
        /// predeterminado (generalmente 1).
        /// </value>
        public long? IncrementBy { get; init; }
        
        
        public long? Seed { get; internal set; }
        
        public long? Increment { get; internal set; }
    }
}
