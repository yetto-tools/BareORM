using System;

namespace BareORM.Schema.Types
{
    /// <summary>
    /// Maps CLR types (<see cref="Type"/>) to BareORM relational column types (<see cref="ColumnType"/>).
    /// </summary>
    /// <remarks>
    /// This mapper is used by schema-building components (e.g. <c>SchemaModelBuilder</c>) to determine the
    /// baseline database type for each property.
    ///
    /// <para>
    /// <b>Important:</b> this contract maps only the "type family" (e.g. string, int, decimal). Extra
    /// schema metadata like max length, fixed length, precision/scale, nullability, etc. should be
    /// captured separately (typically from annotations) and applied later by SQL generators.
    /// </para>
    /// </remarks>
    public interface IClrTypeMapper
    {
        /// <summary>
        /// Maps a CLR type to a BareORM <see cref="ColumnType"/>.
        /// </summary>
        /// <param name="clrType">
        /// The CLR type to map. Nullable value-types (e.g. <c>int?</c>) are supported.
        /// </param>
        /// <returns>
        /// A <see cref="ColumnType"/> representing the baseline relational type.
        /// </returns>
        ColumnType Map(Type clrType);
    }

    /// <summary>
    /// Default CLR-to-column type mapper used by BareORM.
    /// </summary>
    /// <remarks>
    /// The default mapping is intentionally conservative and portable:
    /// <list type="bullet">
    ///   <item><description><see cref="decimal"/> maps to <c>DECIMAL(18,2)</c> via <see cref="DecimalType"/>.</description></item>
    ///   <item><description><see cref="string"/> maps to <see cref="StringType"/> (length is decided later).</description></item>
    ///   <item><description>Unknown types fall back to <see cref="StringType"/> to avoid breaking schema builds.</description></item>
    /// </list>
    ///
    /// <para>
    /// Providers (SQL Server, PostgreSQL, MySQL, etc.) should interpret the resulting <see cref="ColumnType"/>
    /// and apply additional metadata such as max length or precision/scale gathered by the schema builder.
    /// </para>
    /// </remarks>
    public sealed class DefaultClrTypeMapper : IClrTypeMapper
    {
        /// <summary>
        /// Maps a CLR type to a default BareORM <see cref="ColumnType"/>.
        /// </summary>
        /// <param name="clrType">
        /// The CLR type. If it is a nullable value-type (e.g. <c>int?</c>) the underlying type is used.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="ColumnType"/>. Unknown types return <see cref="StringType"/>.
        /// </returns>
        public ColumnType Map(Type clrType)
        {
            var t = Nullable.GetUnderlyingType(clrType) ?? clrType;

            if (t == typeof(int)) return new Int32Type();
            if (t == typeof(long)) return new Int64Type();
            if (t == typeof(bool)) return new BoolType();
            if (t == typeof(DateTime)) return new DateTimeType();
            if (t == typeof(DateTimeOffset)) return new DateTimeOffsetType();
            if (t == typeof(Guid)) return new GuidType();
            if (t == typeof(decimal)) return new DecimalType(18, 2);
            if (t == typeof(double) || t == typeof(float)) return new DoubleType();
            if (t == typeof(byte[])) return new BytesType();
            if (t == typeof(string)) return new StringType();

            // fallback: string (conservative)
            return new StringType();
        }
    }
}
