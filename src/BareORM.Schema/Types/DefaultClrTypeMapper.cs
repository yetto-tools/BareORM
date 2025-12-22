using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Schema.Types
{
    public interface IClrTypeMapper
    {
        ColumnType Map(Type clrType);
    }

    public sealed class DefaultClrTypeMapper : IClrTypeMapper
    {
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

            // fallback: string (conservador)
            return new StringType();
        }
    }
}
