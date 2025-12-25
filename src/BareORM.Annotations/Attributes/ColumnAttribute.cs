using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Define explícitamente el nombre de la columna en base de datos para una propiedad.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Útil cuando el nombre de la propiedad CLR no coincide con el nombre real en BD
    /// (convenciones, compatibilidad con esquemas existentes, nombres con prefijos, etc.).
    /// </para>
    /// <para>
    /// Si no se especifica este atributo, el nombre de columna se obtiene por convención
    /// (usualmente el nombre de la propiedad).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     // La propiedad se llama "Email", pero la columna en BD es "email_address"
    ///     [ColumnName("email_address")]
    ///     public string Email { get; set; } = default!;
    ///
    ///     // Compatibilidad con un esquema legado
    ///     [ColumnName("USR_ID")]
    ///     public Guid Id { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ColumnNameAttribute : Attribute
    {
        /// <summary>
        /// Nombre de columna en base de datos.
        /// </summary>
        /// <remarks>
        /// Recomendación: evitar espacios y caracteres especiales, y mantener consistencia de casing
        /// según el motor (SQL Server no es case-sensitive por defecto, PostgreSQL sí puede serlo con comillas).
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Inicializa el atributo con el nombre de columna.
        /// </summary>
        /// <param name="name">Nombre exacto de la columna.</param>
        /// <exception cref="ArgumentNullException">
        /// Se recomienda lanzar esta excepción si <paramref name="name"/> es null.
        /// </exception>
        public ColumnNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentOutOfRangeException(nameof(name));
            Name = name;
        }
    }
}
