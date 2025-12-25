using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Estrategia de generación de valores para una columna.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se usa junto con <see cref="ValueGeneratedAttribute"/> para declarar si el valor lo asigna la app
    /// o si lo genera la base de datos (por identity/auto increment, o por una secuencia).
    /// </para>
    /// </remarks>
    public enum ValueGenerationStrategy
    {
        /// <summary>
        /// La aplicación asigna el valor (no hay generación automática).
        /// </summary>
        None,

        /// <summary>
        /// La base de datos genera el valor automáticamente (por ejemplo IDENTITY en SQL Server,
        /// AUTO_INCREMENT en MySQL, serial/identity en Postgres).
        /// </summary>
        Database,

        /// <summary>
        /// El valor se genera usando una secuencia explícita (por ejemplo Oracle SEQUENCE, Postgres SEQUENCE).
        /// </summary>
        Sequence
    }

    /// <summary>
    /// Indica que el valor de la propiedad/columna se genera automáticamente según una estrategia.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Esta anotación es “provider friendly”: cada provider decide cómo materializarla:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ValueGenerationStrategy.Database"/>: identity/auto increment.</description></item>
    /// <item><description><see cref="ValueGenerationStrategy.Sequence"/>: usar <see cref="SequenceName"/> como secuencia explícita.</description></item>
    /// <item><description><see cref="ValueGenerationStrategy.None"/>: el valor lo provee la app.</description></item>
    /// </list>
    /// <para>
    /// <see cref="RetrieveAfterInsert"/> sugiere al executor/bulk/provider si debe intentar
    /// recuperar el valor generado (ej: OUTPUT INSERTED.Id en SQL Server, RETURNING en Postgres).
    /// </para>
    /// </remarks>
    /// <example>
    /// IDENTITY / auto increment:
    /// <code>
    /// public sealed class User
    /// {
    ///     [PrimaryKey]
    ///     [ValueGenerated(ValueGenerationStrategy.Database)]
    ///     public int Id { get; set; }
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// Secuencia explícita:
    /// <code>
    /// public sealed class Invoice
    /// {
    ///     [PrimaryKey]
    ///     [ValueGenerated(ValueGenerationStrategy.Sequence, SequenceName = "dbo.seq_invoice")]
    ///     public long Id { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ValueGeneratedAttribute : Attribute
    {
        /// <summary>
        /// Estrategia de generación seleccionada.
        /// </summary>
        public ValueGenerationStrategy Strategy { get; }

        /// <summary>
        /// Nombre de la secuencia a usar cuando <see cref="Strategy"/> es <see cref="ValueGenerationStrategy.Sequence"/>.
        /// </summary>
        public string? SequenceName { get; set; }

        /// <summary>
        /// Indica si el framework debería intentar recuperar el valor generado después de un INSERT.
        /// </summary>
        /// <remarks>
        /// Default: <c>true</c>. Útil para PKs generadas por DB cuando quieres retornar el Id.
        /// </remarks>
        public bool RetrieveAfterInsert { get; set; } = true;

        /// <summary>
        /// Crea la anotación de valor generado.
        /// </summary>
        /// <param name="strategy">Estrategia (default: <see cref="ValueGenerationStrategy.Database"/>).</param>
        public ValueGeneratedAttribute(ValueGenerationStrategy strategy = ValueGenerationStrategy.Database)
            => Strategy = strategy;
    }
}
