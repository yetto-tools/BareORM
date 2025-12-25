using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara una longitud máxima a nivel de esquema para columnas tipo VARCHAR/NVARCHAR (texto de longitud variable).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo expresa la intención de “longitud variable con límite”.
    /// El provider normalmente lo mapeará a <c>VARCHAR(n)</c>/<c>NVARCHAR(n)</c> (o equivalente).
    /// </para>
    /// <para>
    /// Úsalo cuando el texto puede variar en tamaño pero debe estar limitado (emails, nombres, códigos largos, etc.).
    /// Para longitud fija (exactamente n caracteres) usa <see cref="ColumnFixedLengthAttribute"/> o <see cref="ColumnLengthAttribute"/>.
    /// </para>
    /// <para>
    /// Reglas recomendadas:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Aplicar únicamente a propiedades <see cref="string"/>.</description></item>
    /// <item><description><see cref="Value"/> debe ser mayor a 0.</description></item>
    /// <item><description>No combinar con longitud fija (<c>[ColumnFixedLength]</c>/<c>[ColumnLength]</c>) para la misma propiedad.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     [ColumnMaxLength(320)]
    ///     public string Email { get; set; } = default!;
    ///
    ///     [ColumnMaxLength(100)]
    ///     public string FirstName { get; set; } = default!;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ColumnFixedLengthAttribute"/>
    /// <seealso cref="ColumnLengthAttribute"/>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnMaxLengthAttribute : Attribute
    {
        /// <summary>
        /// Longitud máxima solicitada para la columna.
        /// </summary>
        /// <remarks>
        /// Debe ser mayor a 0. El provider decide el tipo físico final
        /// (p.ej. <c>VARCHAR(Value)</c>/<c>NVARCHAR(Value)</c>, o en algunos motores <c>TEXT</c> si no hay límite).
        /// </remarks>
        public int Value { get; }

        /// <summary>
        /// Inicializa el atributo con la longitud máxima.
        /// </summary>
        /// <param name="value">Longitud máxima requerida (n). Debe ser mayor a 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="value"/> es menor o igual a 0.
        /// </exception>
        public ColumnMaxLengthAttribute(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
    }
}
