using System;

namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Validación a nivel de objeto: el string debe tener exactamente N caracteres.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo es de validación (runtime), no de schema. No implica automáticamente
    /// <c>CHAR(n)</c> / <c>NCHAR(n)</c>. Para eso usa <c>[ColumnFixedLength]</c>/<c>[ColumnLength]</c>.
    /// </para>
    /// <para>
    /// El sistema que consuma esta metadata decide si valida en insert/update, o si traduce
    /// la regla a un CHECK opcional.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     // Ej: "GT", "SV"
    ///     [Length(2)]
    ///     public string CountryCode { get; set; } = default!;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class LengthAttribute : Attribute
    {
        /// <summary>
        /// Longitud exacta requerida.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Inicializa la validación de longitud exacta.
        /// </summary>
        /// <param name="value">Longitud exacta requerida.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="value"/> es menor que 0.
        /// </exception>
        public LengthAttribute(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

    }
}
