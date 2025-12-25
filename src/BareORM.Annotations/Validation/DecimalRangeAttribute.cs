using System;
using System.Globalization;

namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Validación a nivel de objeto para valores <see cref="decimal"/> dentro de un rango.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se usan <see cref="string"/> en el constructor porque las constantes de atributos en C#
    /// no soportan <see cref="decimal"/> directamente. Además, evita problemas típicos de punto flotante
    /// cuando el origen es copy/paste o configuración.
    /// </para>
    /// <para>
    /// El parseo se realiza con <see cref="CultureInfo.InvariantCulture"/>:
    /// usa punto (<c>.</c>) como separador decimal (p.ej. <c>"10.50"</c>).
    /// </para>
    /// <para>
    /// Esta validación es semántica (runtime): el sistema que consuma esta metadata debe ejecutar la validación.
    /// No genera constraints automáticamente a menos que tu builder lo traduzca.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class Product
    /// {
    ///     [DecimalRange("0.00", "9999999.99")]
    ///     public decimal Price { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class DecimalRangeAttribute : Attribute
    {
        /// <summary>Valor mínimo permitido (inclusive).</summary>
        public decimal Min { get; }

        /// <summary>Valor máximo permitido (inclusive).</summary>
        public decimal Max { get; }

        /// <summary>
        /// Inicializa el rango decimal usando strings en formato invariante.
        /// </summary>
        /// <param name="min">Mínimo permitido (formato invariante, p.ej. <c>"0.00"</c>).</param>
        /// <param name="max">Máximo permitido (formato invariante, p.ej. <c>"999.99"</c>).</param>
        /// <exception cref="FormatException">
        /// Se lanza si <paramref name="min"/> o <paramref name="max"/> no son decimales válidos en formato invariante.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Se lanza si <paramref name="max"/> es menor que <paramref name="min"/>.
        /// </exception>
        public DecimalRangeAttribute(string min, string max)
        {
            if (!decimal.TryParse(min, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMin))
                throw new FormatException($"Rango decimal no válido mínimo(Min): '{min}'. Utilice CultureInfo.InvariantCulture, por ejemplo: \"0.00\".");

            if (!decimal.TryParse(max, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedMax))
                throw new FormatException($"Rango decimal no válido máximo(Max): '{max}'. Utilice CultureInfo.InvariantCulture, por ejemplo: \"999.99\".");

            if (parsedMax < parsedMin)
                throw new ArgumentOutOfRangeException(nameof(max), "Max no puede ser menor que Min.");

            Min = parsedMin;
            Max = parsedMax;
        }
    }
}
