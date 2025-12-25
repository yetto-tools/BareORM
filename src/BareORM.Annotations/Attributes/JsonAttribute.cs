using System;

namespace BareORM.Annotations.Attributes
{
    /// <summary>
    /// Marca una propiedad como “JSON” a nivel semántico para el esquema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este atributo no fija un tipo físico específico: expresa intención.
    /// El provider decide cómo persistirlo (p.ej. <c>nvarchar(max)</c>, <c>jsonb</c>, <c>json</c>, etc.).
    /// </para>
    /// <para>
    /// Uso típico: propiedades <see cref="string"/> que contienen JSON, o tipos serializables
    /// si tu pipeline soporta serialización/deserialización.
    /// </para>
    /// <para>
    /// Dependiendo del provider, se puede opcionalmente agregar un CHECK (por ejemplo, validar JSON en SQL Server)
    /// o usar un tipo nativo (PostgreSQL <c>jsonb</c>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public sealed class AuditEvent
    /// {
    ///     public Guid Id { get; set; }
    ///
    ///     // JSON semántico (provider decide el tipo físico)
    ///     [Json]
    ///     public string Payload { get; set; } = "{}";
    /// }
    /// </code>
    /// </example>    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonAttribute : Attribute { }
}
