using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara a nivel de esquema que la columna debe ser <c>NOT NULL</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo fuerza la nulabilidad en el modelo de esquema, independientemente de si la propiedad CLR
    /// es nullable o no. Es útil cuando:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Tu código usa <c>string?</c> o tipos nullable por ergonomía, pero en BD quieres <c>NOT NULL</c>.</description></item>
    /// <item><description>Necesitas ser explícito en entidades compartidas o modelos heredados.</description></item>
    /// </list>
    /// <para>
    /// Nota: Para tipos de valor no-nullable (p.ej. <see cref="int"/>, <see cref="Guid"/>), normalmente ya se infiere <c>NOT NULL</c>;
    /// este atributo sigue siendo válido como “override” explícito.
    /// </para>
    /// <para>
    /// Si se combina con un atributo opuesto (p.ej. <c>[ColumnNullable]</c> si existiera), el builder debería definir precedencia
    /// o lanzar una excepción por ambigüedad.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class User
    /// {
    ///     // Aunque el CLR permita null, el schema será NOT NULL.
    ///     [ColumnNotNull]
    ///     public string? Email { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ColumnNotNullAttribute : Attribute { }
}
