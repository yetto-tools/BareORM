using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Define una restricción UNIQUE a nivel de tabla, permitiendo constraints únicos compuestos
    /// agrupando múltiples propiedades por nombre.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo se aplica a propiedades y puede repetirse en la misma propiedad
    /// (<c name="AttributeUsageAttribute.AllowMultiple"/> = true).
    /// El builder (<c name="BareORM.Schema.Builders.SchemaModelBuilder"/>) agrupa las columnas por
    /// <see cref="Name"/> para construir una restricción UNIQUE por cada grupo.
    /// </para>
    /// <para>Soporta UNIQUE compuesto:</para>
    /// <list type="bullet">
    /// <item><description>Varias propiedades con el mismo <see cref="Name"/> forman una sola restricción UNIQUE.</description></item>
    /// <item><description><see cref="Order"/> define el orden de las columnas dentro del UNIQUE (útil para índices compuestos).</description></item>
    /// </list>
    /// <para>
    /// Convención sugerida: usar nombres tipo <c>UQ_{Table}_{Col1}_{Col2}</c> (o similares) para evitar colisiones
    /// y que sea legible en SQL Server.
    /// </para>
    /// </remarks>
    /// <example>
    /// UNIQUE simple (una columna):
    /// <code>
    /// public sealed class User
    /// {
    ///     [Unique("UQ_Users_Email")]
    ///     public string Email { get; set; } = "";
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// UNIQUE compuesto (dos columnas):
    /// <code>
    /// public sealed class Product
    /// {
    ///     [Unique("UQ_Product_SKU_Color", Order = 1)]
    ///     public string Sku { get; set; } = "";
    ///
    ///     [Unique("UQ_Product_SKU_Color", Order = 2)]
    ///     public string Color { get; set; } = "";
    /// }
    /// </code>
    /// </example>

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class UniqueAttribute : Attribute
    {
        /// <summary>
        /// Nombre lógico de la restricción UNIQUE (clave de agrupación).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Orden de la columna dentro del UNIQUE compuesto (default: 0).
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Crea un atributo UNIQUE para agrupar esta propiedad dentro de una restricción por nombre.
        /// </summary>
        /// <param name="name">Nombre de la restricción UNIQUE/grupo.</param>
        /// <exception cref="ArgumentNullException">Si <paramref name="name"/> es null.</exception>
        public UniqueAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
