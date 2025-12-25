using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara una longitud fija a nivel de esquema para columnas tipo CHAR/NCHAR.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo se usa para expresar la intención de “longitud fija” (p.ej. códigos, estados, flags),
    /// permitiendo que el provider mapee el tipo físico a <c>CHAR(n)</c>/<c>NCHAR(n)</c> (o equivalente).
    /// </para>
    /// <para>
    /// Reglas recomendadas:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Aplicar únicamente a propiedades <see cref="string"/>.</description></item>
    /// <item><description><c>Value</c> debe ser mayor a 0.</description></item>
    /// <item><description>No combinar con un atributo de longitud variable (p.ej. <c>[ColumnMaxLength]</c>), salvo que tu builder lo permita explícitamente.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     // Ej: "GT", "SV", "HN"
    ///     [ColumnFixedLength(2)]
    ///     public string CountryCode { get; set; } = default!;
    ///
    ///     // Ej: "A", "I"
    ///     [ColumnFixedLength(1)]
    ///     public string Status { get; set; } = default!;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnFixedLengthAttribute : Attribute
    {
        /// <summary>
        /// Longitud fija solicitada para la columna.
        /// </summary>
        /// <remarks>
        /// Debe ser mayor a 0. El provider decide el tipo físico final (p.ej. <c>CHAR(Value)</c>/<c>NCHAR(Value)</c>).
        /// </remarks>
        public int Value { get; }

        /// <summary>
        /// Inicializa el atributo con la longitud fija.
        /// </summary>
        /// <param name="value">Longitud fija requerida (n). Debe ser mayor a 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Se lanza si <paramref name="value"/> es menor o igual a 0.</exception>
        public ColumnFixedLengthAttribute(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
    }
}
