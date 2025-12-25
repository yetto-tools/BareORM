using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara una llave foránea (FK) desde una propiedad/columna local hacia otra entidad.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo define la relación “dependiente → principal” indicando:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="RefEntityType"/>: entidad referenciada (tabla principal).</description></item>
    /// <item><description><see cref="RefProperty"/>: propiedad en la entidad principal (normalmente PK o UNIQUE).</description></item>
    /// </list>
    /// <para>
    /// El nombre del constraint (<see cref="Name"/>) es opcional; si no se define, el builder/provider
    /// puede generar uno por convención.
    /// </para>
    /// <para>
    /// Las acciones referenciales (<see cref="OnDelete"/> / <see cref="OnUpdate"/>) se traducen a
    /// reglas ON DELETE / ON UPDATE según el provider.
    /// </para>
    /// <para>
    /// Nota: este atributo describe una FK “por columna”. Para FKs compuestas, normalmente se requiere
    /// una forma adicional (entity-level) para agrupar múltiples columnas (dependiendo de tu diseño).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class Order
    /// {
    ///     public Guid Id { get; set; }
    ///
    ///     // Columna local: CustomerId → Customer.Id
    ///     [ForeignKey(typeof(Customer), nameof(Customer.Id),
    ///         Name = "FK_Orders_Customers_CustomerId",
    ///         OnDelete = ReferentialAction.Cascade)]
    ///     public Guid CustomerId { get; set; }
    /// }
    ///
    /// public sealed class Customer
    /// {
    ///     public Guid Id { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        /// <summary>
        /// Tipo CLR de la entidad referenciada (principal).
        /// </summary>
        public Type RefEntityType { get; }

        /// <summary>
        /// Nombre de la propiedad referenciada en la entidad principal.
        /// </summary>
        /// <remarks>
        /// Usualmente corresponde a una PK o a una propiedad con UNIQUE.
        /// Recomendación: usar <c>nameof(Entidad.Prop)</c> para evitar typos.
        /// </remarks>
        public string RefProperty { get; }

        /// <summary>
        /// Nombre explícito del constraint FK (opcional).
        /// </summary>
        /// <remarks>
        /// Si es null, el builder/provider puede generar un nombre por convención
        /// (p.ej. <c>FK_{Tabla}_{RefTabla}_{Columna}</c>).
        /// </remarks>
        public string? Name { get; set; }

        /// <summary>
        /// Acción referencial al borrar la fila referenciada.
        /// </summary>
        /// <remarks>
        /// Default: <see cref="ReferentialAction.NoAction"/>.
        /// </remarks>
        public ReferentialAction OnDelete { get; set; } = ReferentialAction.NoAction;

        /// <summary>
        /// Acción referencial al actualizar la clave referenciada.
        /// </summary>
        /// <remarks>
        /// Default: <see cref="ReferentialAction.NoAction"/>.
        /// </remarks>
        public ReferentialAction OnUpdate { get; set; } = ReferentialAction.NoAction;

        /// <summary>
        /// Crea una FK desde la propiedad actual hacia <paramref name="refEntityType"/>.<paramref name="refProperty"/>.
        /// </summary>
        /// <param name="refEntityType">Entidad referenciada (principal).</param>
        /// <param name="refProperty">Propiedad referenciada en la entidad principal.</param>
        /// <exception cref="ArgumentNullException">
        /// Recomendado si <paramref name="refEntityType"/> o <paramref name="refProperty"/> son null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="refProperty"/> es vacío o whitespace.
        /// </exception>
        public ForeignKeyAttribute(Type refEntityType, string refProperty)
        {
            RefEntityType = refEntityType ?? throw new ArgumentNullException(nameof(refEntityType));
            if (string.IsNullOrWhiteSpace(refProperty))
                throw new ArgumentOutOfRangeException(nameof(refProperty));

            RefProperty = refProperty;
        }

    }
}
