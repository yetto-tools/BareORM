using System.Text.Json;
using BareORM.Abstractions;

namespace BareORM.Mapping
{
    internal static class ValueConverters
    {
        public static object? DbNullToNull(object? v) => v is DBNull ? null : v;

        public static object? ConvertTo(object? value, Type targetType)
        => ConvertTo(value, targetType, isJson: false, serializer: null);

        public static object? ConvertTo(object? value, Type targetType, bool isJson, ISerializer? serializer)
        {
            value = DbNullToNull(value);
            if (value is null)
            {
                // null -> default para value-types no-nullable
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // JSON opt-in
            if (isJson)
            {
                var s = value as string;

                // a veces SQL retorna NVARCHAR, a veces ya viene como JsonDocument en otros providers (no en SQL Server)
                if (s is null) s = value.ToString();

                if (string.IsNullOrWhiteSpace(s))
                    return Activator.CreateInstance(nonNullType);

                // Tipos JSON nativos
                if (nonNullType == typeof(JsonDocument)) return JsonDocument.Parse(s);
                if (nonNullType == typeof(JsonElement)) return JsonDocument.Parse(s).RootElement;
                if (nonNullType == typeof(string)) return s;

                // POCO
                if (serializer is not null)
                {
                    // si ya implementaste Deserialize(string, Type) perfecto:
                    return serializer.Deserialize(s!, nonNullType);
                }

                // fallback sin romper
                return JsonSerializer.Deserialize(s!, nonNullType);
            }

            if (nonNullType.IsInstanceOfType(value))
                return value;

            // Enums
            if (nonNullType.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(nonNullType, s, ignoreCase: true);

                var underlying = Enum.GetUnderlyingType(nonNullType);
                var numeric = Convert.ChangeType(value, underlying);
                return Enum.ToObject(nonNullType, numeric!);
            }

            // Guid
            if (nonNullType == typeof(Guid))
            {
                if (value is Guid g) return g;
                if (value is string gs) return Guid.Parse(gs);
                if (value is byte[] bytes) return new Guid(bytes);
            }

            // DateTimeOffset
            if (nonNullType == typeof(DateTimeOffset))
            {
                if (value is DateTimeOffset dto) return dto;
                if (value is DateTime dt) return new DateTimeOffset(dt);
                if (value is string s) return DateTimeOffset.Parse(s);
            }

            // bool desde 0/1
            if (nonNullType == typeof(bool))
            {
                if (value is bool b) return b;
                if (value is byte by) return by != 0;
                if (value is short sh) return sh != 0;
                if (value is int i) return i != 0;
                if (value is long l) return l != 0;
                if (value is string s) return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return Convert.ChangeType(value, nonNullType);
        }
    }
}
