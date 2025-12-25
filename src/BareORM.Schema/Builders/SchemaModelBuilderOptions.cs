using BareORM.Schema.Types;

namespace BareORM.Schema.Builders
{
    /// <summary>
    /// Opciones de configuración para construir un <see cref="BareORM.Schema.SchemaModel"/> a partir de entidades CLR.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controla convenciones (schema por defecto, nombres de constraints) y el mapeo de tipos CLR → <see cref="ColumnType"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new SchemaModelBuilderOptions
    /// {
    ///     DefaultSchema = "dbo",
    ///     RequireTableAttribute = false,
    ///     UseConventionalConstraintNames = true
    /// };
    /// </code>
    /// </example>
    public sealed class SchemaModelBuilderOptions
    {
        /// <summary>
        /// Schema por defecto cuando una entidad no especifica schema explícito.
        /// </summary>
        /// <remarks>
        /// Default: <c>"dbo"</c> (típico en SQL Server).
        /// </remarks>
        public string DefaultSchema { get; init; } = "dbo";

        /// <summary>
        /// Mapper de tipos CLR hacia tipos lógicos (<see cref="ColumnType"/>).
        /// </summary>
        /// <remarks>
        /// Default: <see cref="DefaultClrTypeMapper"/>.
        /// El provider puede mapear posteriormente <see cref="ColumnType"/> a tipos físicos (SQL).
        /// </remarks>
        public IClrTypeMapper TypeMapper { get; init; } = new DefaultClrTypeMapper();

        /// <summary>
        /// Si es <c>true</c>, solo se modelan entidades que tengan el atributo <c>[Table]</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Útil para evitar que clases auxiliares o DTOs entren al modelo por accidente.
        /// </para>
        /// <para>
        /// Si es <c>false</c>, el builder puede aplicar convenciones para incluir entidades sin <c>[Table]</c>.
        /// </para>
        /// </remarks>
        public bool RequireTableAttribute { get; init; } = false;

        /// <summary>
        /// Indica si se deben usar nombres convencionales para constraints.
        /// </summary>
        /// <remarks>
        /// Convención típica:
        /// <list type="bullet">
        /// <item><description><c>PK_{Table}</c></description></item>
        /// <item><description><c>UQ_{Table}_{Columns...}</c></description></item>
        /// <item><description><c>FK_{Table}_{RefTable}_{Columns...}</c></description></item>
        /// <item><description><c>CK_{Table}_{...}</c></description></item>
        /// </list>
        /// Si es <c>false</c>, el builder/provider puede usar nombres explícitos de atributos o dejar que el motor genere nombres.
        /// </remarks>
        public bool UseConventionalConstraintNames { get; init; } = true;
    }
}
