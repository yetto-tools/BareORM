using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara una longitud fija a nivel de esquema para columnas <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Es equivalente a <see cref="ColumnFixedLengthAttribute"/>, pero con un nombre más genérico (“Length”).
    /// La intención es mapear a tipos de longitud fija como <c>CHAR(n)</c>/<c>NCHAR(n)</c> (o equivalente)
    /// según el provider.
    /// </para>
    /// <para>
    /// Úsalo cuando el valor siempre tendrá exactamente <c>n</c> caracteres (códigos, estados, banderas).
    /// Para longitud variable, usa un atributo de max length (p.ej. <c>[ColumnMaxLength]</c>) en su lugar.
    /// </para>
    /// <para>
    /// Reglas recomendadas:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Aplicar únicamente a propiedades <see cref="string"/>.</description></item>
    /// <item><description><see cref="Value"/> debe ser mayor a 0.</description></item>
    /// <item><description>No combinar con atributos de longitud variable (p.ej. <c>[ColumnMaxLength]</c>) salvo que tu builder defina precedencia.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     // "GT", "SV", "HN"
    ///     [ColumnLength(2)]
    ///     public string CountryCode { get; set; } = default!;
    ///
    ///     // "A" / "I"
    ///     [ColumnLength(1)]
    ///     public string Status { get; set; } = default!;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ColumnFixedLengthAttribute"/>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnLengthAttribute : Attribute
    {
        /// <summary>
        /// Longitud fija solicitada para la columna.
        /// </summary>
        /// <remarks>
        /// Debe ser mayor a 0. El provider decide el tipo físico final
        /// (p.ej. <c>CHAR(Value)</c>/<c>NCHAR(Value)</c>).
        /// </remarks>
        public int Value { get; }

        /// <summary>
        /// Inicializa el atributo con la longitud fija.
        /// </summary>
        /// <param name="value">Longitud fija requerida (n). Debe ser mayor a 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="value"/> es menor o igual a 0.
        /// </exception>
        public ColumnLengthAttribute(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
    }
}
