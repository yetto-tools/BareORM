using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Marca una propiedad como parte de la llave primaria (PRIMARY KEY).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se usa para declarar PK simple o compuesta:
    /// </para>
    /// <list type="bullet">
    /// <item><description>PK simple: una sola propiedad con <see cref="PrimaryKeyAttribute"/>.</description></item>
    /// <item><description>PK compuesta: múltiples propiedades con <see cref="PrimaryKeyAttribute"/> y orden definido por <see cref="Order"/>.</description></item>
    /// </list>
    /// <para>
    /// La propiedad <see cref="Name"/> es opcional. Si no se define, el builder/provider puede generar
    /// un nombre por convención (p.ej. <c>PK_{Table}</c>).
    /// </para>
    /// <para>
    /// Importante: <see cref="Order"/> es 0-based y solo se valida que no sea negativo (0..N-1).
    /// Esto calza con la idea de “ordinal” y evita valores inválidos.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     [PrimaryKey(Name = "PK_Users")]
    ///     public Guid Id { get; set; }
    /// }
    ///
    /// public sealed class OrderItem
    /// {
    ///     [PrimaryKey(0, Name = "PK_OrderItems")]
    ///     public Guid OrderId { get; set; }
    ///
    ///     [PrimaryKey(1, Name = "PK_OrderItems")]
    ///     public Guid ProductId { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// Orden 0-based dentro de una PK compuesta.
        /// </summary>
        /// <remarks>
        /// Para PK simple normalmente se deja en 0.
        /// Para PK compuesta se recomienda 0..N-1.
        /// </remarks>
        public int Order { get; }

        /// <summary>
        /// Nombre explícito del constraint PK (opcional).
        /// </summary>
        /// <remarks>
        /// Si es null, el builder/provider puede generar un nombre por convención (p.ej. <c>PK_{Table}</c>).
        /// </remarks>
        public string? Name { get; set; } // PK_Users, etc.

        /// <summary>
        /// Inicializa el atributo de llave primaria.
        /// </summary>
        /// <param name="order">
        /// Orden 0-based dentro de una PK compuesta (0..N-1). Para PK simple, típicamente 0.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Recomendado si <paramref name="order"/> es negativo.
        /// </exception>
        public PrimaryKeyAttribute(int order = 0)
        {
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
        }
    }
}
