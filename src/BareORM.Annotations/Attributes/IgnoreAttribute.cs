namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Indica que una propiedad debe ser ignorada por el mapeo de esquema y/o persistencia.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Útil para propiedades calculadas, de UI, backing fields o cualquier valor que no deba convertirse
    /// en columna de base de datos.
    /// </para>
    /// <para>
    /// Normalmente el builder/reflection pipeline omite propiedades con este atributo al construir:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Columnas (<c>DbColumn</c> / <c>ColumnMetadata</c>)</description></item>
    /// <item><description>Constraints asociados (PK/Unique/FK/Check si dependieran de esa propiedad)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     public Guid Id { get; set; }
    ///
    ///     // Calculado: no existe en BD
    ///     [Ignore]
    ///     public string DisplayName =&gt; $"{FirstName} {LastName}";
    ///
    ///     public string FirstName { get; set; } = default!;
    ///     public string LastName  { get; set; } = default!;
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IgnoreAttribute : Attribute { }
}
