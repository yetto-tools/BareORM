using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Define precisión y escala a nivel de esquema para columnas DECIMAL/NUMERIC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo se usa para indicar al builder/provider que un valor decimal debe mapearse con
    /// precisión y escala explícitas, por ejemplo: <c>DECIMAL(18,2)</c>.
    /// </para>
    /// <para>
    /// Reglas:
    /// </para>
    /// <list type="bullet">
    /// <item><description><paramref name="precision"/> debe ser &gt; 0.</description></item>
    /// <item><description><paramref name="scale"/> debe ser &gt;= 0 y no puede ser mayor que <paramref name="precision"/>.</description></item>
    /// <item><description>Recomendado usarlo únicamente sobre propiedades <see cref="decimal"/> / <see cref="decimal"/>?.</description></item>
    /// </list>
    /// <para>
    /// Nota: la semántica exacta del tipo físico depende del provider (SQL Server: <c>decimal(p,s)</c>, PostgreSQL: <c>numeric(p,s)</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class Product
    /// {
    ///     // DECIMAL(18,2)
    ///     [Precision(18, 2)]
    ///     public decimal Price { get; set; }
    ///
    ///     // DECIMAL(10,4)
    ///     [Precision(10, 4)]
    ///     public decimal Weight { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class PrecisionAttribute : Attribute
    {
        /// <summary>
        /// Precisión total: número total de dígitos permitidos (enteros + decimales).
        /// </summary>
        public byte Precision { get; }

        /// <summary>
        /// Escala: cantidad de dígitos decimales permitidos.
        /// </summary>
        public byte Scale { get; }

        /// <summary>
        /// Inicializa el atributo con precisión y escala.
        /// </summary>
        /// <param name="precision">Total de dígitos (p.ej. 18). Debe ser mayor a 0.</param>
        /// <param name="scale">Dígitos decimales (p.ej. 2). No puede ser mayor que <paramref name="precision"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="precision"/> es 0, o si <paramref name="scale"/> es mayor que <paramref name="precision"/>.
        /// </exception>
        public PrecisionAttribute(byte precision, byte scale)
        {
            if (precision == 0) throw new ArgumentOutOfRangeException(nameof(precision));
            if (scale > precision) throw new ArgumentOutOfRangeException(nameof(scale), "Scale cannot be greater than Precision.");
            Precision = precision;
            Scale = scale;
        }
    }
}
