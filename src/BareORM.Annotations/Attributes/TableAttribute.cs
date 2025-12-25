using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Declara el mapeo de una entidad CLR hacia una tabla física en la base de datos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo se aplica a clases (entidades) y es consumido por componentes como
    /// <c>SchemaModelBuilder</c> para determinar el <b>schema</b> y el <b>nombre</b> de la tabla.
    /// </para>
    /// <para>
    /// Si tu builder está configurado con <c>RequireTableAttribute = true</c>, entonces únicamente las clases
    /// anotadas con <see cref="TableAttribute"/> serán incluidas en el modelo.
    /// </para>
    /// <para>
    /// Convención:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Por defecto el schema es <c>dbo</c>.</description></item>
    /// <item><description>Si no se usa este atributo y <c>RequireTableAttribute = false</c>, el nombre de tabla puede caer por convención al nombre de la clase.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Table("Users", "dbo")]
    /// public sealed class User
    /// {
    ///     [PrimaryKey]
    ///     public int Id { get; set; }
    ///
    ///     [ColumnName("EmailAddress")]
    ///     public string Email { get; set; } = "";
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la tabla (sin schema).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Schema de la tabla (por defecto: <c>dbo</c>).
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Crea el mapeo de entidad → tabla.
        /// </summary>
        /// <param name="name">Nombre de tabla (ej: <c>"Users"</c>).</param>
        /// <param name="schema">Schema (ej: <c>"dbo"</c>). Default: <c>dbo</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Si <paramref name="name"/> es null. (Nota: si quieres, también puedes validar vacío/whitespace).
        /// </exception>
        public TableAttribute(string name, string schema = "dbo")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
    }
}
