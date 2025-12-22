using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Schema.Types
{
    public abstract record ColumnType;

    public record Int32Type() : ColumnType;
    public record Int64Type() : ColumnType;
    public record BoolType() : ColumnType;
    public record DateTimeType() : ColumnType;
    public record DateTimeOffsetType() : ColumnType;
    public record GuidType() : ColumnType;
    public record DecimalType(byte Precision, byte Scale) : ColumnType;
    public record DoubleType() : ColumnType;
    public record StringType(int? MaxLength = null, bool Unicode = true) : ColumnType;
    public record BytesType(int? MaxLength = null) : ColumnType;

    /// <summary>
    /// “Semántico” (provider decide: nvarchar/jsonb/json/etc)
    /// </summary>
    public record JsonType() : ColumnType;
}
