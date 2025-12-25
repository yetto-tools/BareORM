using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Define el orden (ordinal) de una propiedad dentro de una composición: columnas, PK compuesta,
    /// UNIQUE/INDEX compuestos, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo es útil cuando el orden importa y no quieres depender del orden de reflexión.
    /// </para>
    /// <para>
    /// Usos típicos:
    /// </para>
    /// <list type="bullet">
    /// <item><description>PK compuesta: define el orden de columnas dentro de la PK.</description></item>
    /// <item><description>UNIQUE/INDEX compuesto: define el orden de columnas dentro del constraint/índice.</description></item>
    /// <item><description>Orden de columnas en el esquema (si tu builder respeta orden).</description></item>
    /// </list>
    /// <para>
    /// Recomendación: usar índices 0..N-1  para composición (más legible en metadata).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class OrderItem
    /// {
    ///     [Ordinal(0)]
    ///     public Guid OrderId { get; set; }
    ///
    ///     [Ordinal(1)]
    ///     public Guid ProductId { get; set; }
    ///
    ///     public int Qty { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class OrdinalAttribute : Attribute
    {
        /// <summary>
        /// Posición ordinal de la propiedad dentro de la composición.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Inicializa el ordinal.
        /// </summary>
        /// <param name="index">Posición ordinal (recomendado 0..N-1).</param>
        public OrdinalAttribute(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
        }
    }
}
