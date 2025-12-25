namespace BareORM.Schema.Types
{
    /// <summary>
    /// Tipo de columna agnóstico del provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ColumnType"/> representa el tipo lógico/semántico de una columna (independiente del motor).
    /// Cada provider (SQL Server, PostgreSQL, etc.) decide el tipo físico final (p.ej. <c>nvarchar</c>, <c>jsonb</c>, <c>uniqueidentifier</c>).
    /// </para>
    /// <para>
    /// Recomendación: usa estos tipos en tu capa de schema/migrations y deja el mapeo final al provider.
    /// </para>
    /// </remarks>
    public abstract record ColumnType;

    /// <summary>Entero de 32 bits (signed).</summary>
    /// <remarks>Provider típico: SQL Server <c>int</c>, PostgreSQL <c>integer</c>.</remarks>
    public record Int32Type() : ColumnType;

    /// <summary>Entero de 64 bits (signed).</summary>
    /// <remarks>Provider típico: SQL Server <c>bigint</c>, PostgreSQL <c>bigint</c>.</remarks>
    public record Int64Type() : ColumnType;

    /// <summary>Booleano (true/false).</summary>
    /// <remarks>Provider típico: SQL Server <c>bit</c>, PostgreSQL <c>boolean</c>.</remarks>
    public record BoolType() : ColumnType;

    /// <summary>Fecha y hora (sin offset).</summary>
    /// <remarks>Provider típico: SQL Server <c>datetime2</c>, PostgreSQL <c>timestamp</c>.</remarks>
    public record DateTimeType() : ColumnType;

    /// <summary>Fecha y hora con offset (zona/offset).</summary>
    /// <remarks>Provider típico: SQL Server <c>datetimeoffset</c>, PostgreSQL <c>timestamptz</c>.</remarks>
    public record DateTimeOffsetType() : ColumnType;

    /// <summary>Identificador único (UUID/GUID).</summary>
    /// <remarks>Provider típico: SQL Server <c>uniqueidentifier</c>, PostgreSQL <c>uuid</c>.</remarks>
    public record GuidType() : ColumnType;

    /// <summary>
    /// Decimal con precisión y escala.
    /// </summary>
    /// <param name="Precision">Total de dígitos (p.ej. 18).</param>
    /// <param name="Scale">Dígitos decimales (p.ej. 2).</param>
    /// <remarks>
    /// Provider típico: SQL Server <c>decimal(Precision, Scale)</c>, PostgreSQL <c>numeric(Precision, Scale)</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Precio: decimal(18,2)
    /// var price = new DecimalType(18, 2);
    /// </code>
    /// </example>
    public record DecimalType(byte Precision, byte Scale) : ColumnType;

    /// <summary>Número de punto flotante doble precisión.</summary>
    /// <remarks>Provider típico: SQL Server <c>float</c>, PostgreSQL <c>double precision</c>.</remarks>
    public record DoubleType() : ColumnType;

    /// <summary>
    /// Texto (string) con longitud opcional y configuración Unicode.
    /// </summary>
    /// <param name="MaxLength">
    /// Longitud máxima. Si es <c>null</c>, el provider decide (p.ej. <c>nvarchar(max)</c> / <c>text</c>).
    /// </param>
    /// <param name="Unicode">
    /// Si <c>true</c>, el provider debería usar un tipo Unicode (p.ej. <c>nvarchar</c>).
    /// Si <c>false</c>, uno no-Unicode (p.ej. <c>varchar</c>) cuando aplique.
    /// </param>
    /// <remarks>
    /// SQL Server suele mapear Unicode a <c>nvarchar</c> y no-Unicode a <c>varchar</c>.
    /// En PostgreSQL normalmente se usa <c>text</c>/<c>varchar</c> (Unicode por defecto).
    /// </remarks>
    /// <example>
    /// <code>
    /// new StringType(100);              // hasta 100 chars, Unicode
    /// new StringType(50, Unicode:false); // hasta 50 chars, no Unicode (si el provider lo soporta)
    /// new StringType();                  // longitud "sin límite" (provider decide)
    /// </code>
    /// </example>
    public record StringType(int? MaxLength = null, bool Unicode = true) : ColumnType;

    /// <summary>
    /// Datos binarios (bytes) con longitud opcional.
    /// </summary>
    /// <param name="MaxLength">
    /// Longitud máxima. Si es <c>null</c>, el provider decide (p.ej. <c>varbinary(max)</c> / <c>bytea</c>).
    /// </param>
    /// <remarks>Provider típico: SQL Server <c>varbinary</c>, PostgreSQL <c>bytea</c>.</remarks>
    public record BytesType(int? MaxLength = null) : ColumnType;

    /// <summary>
    /// Tipo JSON “semántico” (el provider decide: nvarchar/jsonb/json/etc).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Este tipo representa intención: “aquí guardo JSON”.
    /// No fuerza un tipo físico específico.
    /// </para>
    /// <para>
    /// Ejemplos de mapeo:
    /// SQL Server: <c>nvarchar(max)</c> (con <c>CHECK(ISJSON(...)=1)</c>) o <c>json</c> si existiera.
    /// PostgreSQL: <c>jsonb</c>.
    /// </para>
    /// </remarks>
    public record JsonType() : ColumnType;
}
