using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using BareORM.Abstractions;

using BareORM.Annotations.Attributes;


namespace BareORM.Mapping
{
    public sealed class DefaultEntityMapper<T> : IEntityMapper<T> where T : new()
    {
        private readonly MappingOptions _options;

        // Cache por "firma del record" + "firma de opciones"
        private static readonly ConcurrentDictionary<string, Func<IDataRecord, T>> _mapCache = new();

        public DefaultEntityMapper(MappingOptions? options = null)
            => _options = options ?? new MappingOptions();

        public T Map(IDataRecord record)
        {
            var normalize = _options.NamePolicy ?? NamePolicies.Default;

            // ⚠️ Importante: si no metés opciones en la key, mezclás mappers de configs distintas
            var optionsKey = BuildOptionsKey(_options);
            var sig = RecordSignature.Build(record, normalize);
            var cacheKey = $"{optionsKey}||{sig}";

            var fn = _mapCache.GetOrAdd(cacheKey, _ => BuildMapper(record, _options));
            return fn(record);
        }

        private static string BuildOptionsKey(MappingOptions o)
        {
            // suficiente para distinguir configs
            return $"{o.Mode}|{o.IgnoreCase}|{o.StrictColumnMatch}|{o.StrictOrdinalMatch}|{(o.NamePolicy?.Method.Name ?? "null")}";
        }

        private static Func<IDataRecord, T> BuildMapper(IDataRecord record, MappingOptions options)
        {
            return options.Mode == MappingMode.ByOrdinal
                ? BuildOrdinalMapper(record, options)
                : BuildNameMapper(record, options);
        }

        // ----------------------------
        // ByName (tu implementación)
        // ----------------------------
        private static Func<IDataRecord, T> BuildNameMapper(IDataRecord record, MappingOptions options)
        {
            var normalize = options.NamePolicy ?? NamePolicies.Default;

            var colIndex = new Dictionary<string, int>(options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            for (int i = 0; i < record.FieldCount; i++)
                colIndex[normalize(record.GetName(i))] = i;

            var recordParam = Expression.Parameter(typeof(IDataRecord), "r");
            var entityVar = Expression.Variable(typeof(T), "e");

            var body = new List<Expression>
            {
                Expression.Assign(entityVar, Expression.New(typeof(T)))
            };

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null);

            foreach (var prop in props)
            {
                var colName = prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
                var key = normalize(colName);

                if (!colIndex.TryGetValue(key, out var ordinal))
                {
                    if (options.StrictColumnMatch)
                        throw new InvalidOperationException($"Column not found for property '{typeof(T).Name}.{prop.Name}' (expected '{colName}').");
                    continue;
                }

                // body.Add(AssignPropFromOrdinal(recordParam, entityVar, prop, ordinal));
                body.Add(AssignPropFromOrdinal(recordParam, entityVar, prop, ordinal, options));

            }

            body.Add(entityVar);

            var block = Expression.Block(new[] { entityVar }, body);
            return Expression.Lambda<Func<IDataRecord, T>>(block, recordParam).Compile();
        }

        // ----------------------------
        // ByOrdinal (posicional)
        // ----------------------------
        private static Func<IDataRecord, T> BuildOrdinalMapper(IDataRecord record, MappingOptions options)
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "r");
            var entityVar = Expression.Variable(typeof(T), "e");

            var body = new List<Expression>
            {
                Expression.Assign(entityVar, Expression.New(typeof(T)))
            };

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null)
                .ToList();

            // 1) Si hay [Ordinal], ese manda
            var withOrd = props
                .Select(p => new { Prop = p, Ord = p.GetCustomAttribute<OrdinalAttribute>()?.Index })
                .Where(x => x.Ord.HasValue)
                .OrderBy(x => x.Ord!.Value)
                .ToList();

            if (withOrd.Count > 0)
            {
                var maxOrd = withOrd.Max(x => x.Ord!.Value);
                if (options.StrictOrdinalMatch && record.FieldCount <= maxOrd)
                    throw new InvalidOperationException($"Resultset has {record.FieldCount} columns but requires ordinal {maxOrd} for '{typeof(T).Name}'.");

                foreach (var x in withOrd)
                    body.Add(AssignPropFromOrdinal(recordParam, entityVar, x.Prop, x.Ord!.Value, options));
                    //body.Add(AssignPropFromOrdinal(recordParam, entityVar, prop, ordinal, options));
            }
            else
            {
                // 2) Si no hay [Ordinal], fallback: posicional por “orden de declaración”
                // (MetadataToken es lo más estable que se puede sin atributo)
                var ordered = props.OrderBy(p => p.MetadataToken).ToList();

                if (options.StrictOrdinalMatch && record.FieldCount != ordered.Count)
                    throw new InvalidOperationException(
                        $"Ordinal mapping mismatch for '{typeof(T).Name}'. Resultset columns={record.FieldCount}, properties={ordered.Count}.");

                var count = Math.Min(record.FieldCount, ordered.Count);
                for (int i = 0; i < count; i++)
                    body.Add(AssignPropFromOrdinal(recordParam, entityVar, ordered[i], i, options));
            }

            body.Add(entityVar);

            var block = Expression.Block(new[] { entityVar }, body);
            return Expression.Lambda<Func<IDataRecord, T>>(block, recordParam).Compile();
        }

        private static Expression AssignPropFromOrdinal(ParameterExpression recordParam, ParameterExpression entityVar, PropertyInfo prop, int ordinal, MappingOptions options)
        {
            var getValueCall = Expression.Call(recordParam, nameof(IDataRecord.GetValue), Type.EmptyTypes, Expression.Constant(ordinal));
            var isJson = prop.GetCustomAttribute<JsonAttribute>() is not null;

            var convertCall = Expression.Call(
                typeof(ValueConverters),
                nameof(ValueConverters.ConvertTo),
                Type.EmptyTypes,
                Expression.Convert(getValueCall, typeof(object)),
                Expression.Constant(prop.PropertyType, typeof(Type)),
                Expression.Constant(isJson),
                Expression.Constant(options.Serializer, typeof(ISerializer))
            );

            return Expression.Assign(
                Expression.Property(entityVar, prop),
                Expression.Convert(convertCall, prop.PropertyType)
            );
        }
    }
}
