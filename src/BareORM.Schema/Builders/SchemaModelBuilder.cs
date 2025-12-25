
using System.Reflection;
using BareORM.Annotations.Attributes;
using BareORM.Schema.Utilities;

namespace BareORM.Schema.Builders
{
    /// <summary>
    /// SchemaModelBuilder construye un modelo de esquema a partir de tipos de entidad anotados.
    /// </summary>
    public sealed class SchemaModelBuilder
    {
        private readonly SchemaModelBuilderOptions _opt;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public SchemaModelBuilder(SchemaModelBuilderOptions? options = null)
            => _opt = options ?? new SchemaModelBuilderOptions();

        /// <summary>
        /// Construye el modelo de esquema a partir de los tipos de entidad proporcionados.
        /// </summary>
        /// <param name="entityTypes"></param>
        /// <returns></returns>
        public SchemaModel Build(params Type[] entityTypes)
        {
            var model = new SchemaModel();

            foreach (var entityType in entityTypes)
            {
                var tableAttr = entityType.GetCustomAttribute<TableAttribute>();

                if (_opt.RequireTableAttribute && tableAttr is null)
                    continue;

                var schemaName = tableAttr?.Schema ?? _opt.DefaultSchema;
                var tableName = tableAttr?.Name ?? entityType.Name;

                var schema = model.GetOrAddSchema(schemaName);
                var table = schema.GetOrAddTable(tableName, entityType);

                BuildColumns(table, entityType);
                BuildConstraints(table, entityType, model);
            }

            return model;
        }

        private void BuildColumns(DbTable table, Type entityType)
        {
            foreach (var p in ReflectionHelpers.GetMappableProperties(entityType))
            {
                var colAttr = p.GetCustomAttribute<ColumnNameAttribute>();
                var colName = colAttr?.Name ?? p.Name;

                var clrType = p.PropertyType;

                // 1) Leer anotaciones de schema
                var maxLen = p.GetCustomAttribute<ColumnMaxLengthAttribute>()?.Value;

                // Fixed: soporta ColumnFixedLength o ColumnLength (alias)
                var fixedLen =
                    p.GetCustomAttribute<ColumnFixedLengthAttribute>()?.Value
                    ?? p.GetCustomAttribute<ColumnLengthAttribute>()?.Value;

                var prec = p.GetCustomAttribute<ColumnPrecisionAttribute>(); // ✅ schema

                // Validaciones de uso correcto
                var isString = clrType == typeof(string);
                var isDecimal = clrType == typeof(decimal) || clrType == typeof(decimal?);

                if ((maxLen.HasValue || fixedLen.HasValue) && !isString)
                    throw new InvalidOperationException(
                        $"[ColumnMaxLength]/[ColumnFixedLength]/[ColumnLength] only apply to string. {entityType.Name}.{p.Name} is {clrType.Name}.");

                if (maxLen.HasValue && fixedLen.HasValue)
                    throw new InvalidOperationException(
                        $"Use either [ColumnMaxLength] OR [ColumnFixedLength]/[ColumnLength], not both: {entityType.Name}.{p.Name}.");

                if (prec is not null && !isDecimal)
                    throw new InvalidOperationException(
                        $"[ColumnPrecision] only applies to decimal/decimal?. {entityType.Name}.{p.Name} is {clrType.Name}.");

                // 2) Nullability (regla: NULL por defecto)
                var isNullable = true;

                if (p.GetCustomAttribute<ColumnNotNullAttribute>() is not null)
                    isNullable = false;

                if (p.GetCustomAttribute<PrimaryKeyAttribute>() is not null)
                    isNullable = false;

                var inc = p.GetCustomAttribute<IncrementalKeyAttribute>();
                if (inc is not null)
                    isNullable = false;

                var colType = _opt.TypeMapper.Map(clrType);

                // 3) Crear columna con metadata extra
                var col = new DbColumn(colName, clrType, colType, p.Name)
                {
                    IsNullable = isNullable,

                    // Incremental key
                    IsIncrementalKey = inc is not null,
                    SequenceName = inc?.SequenceName,
                    StartWith = inc?.StartWith,
                    IncrementBy = inc?.IncrementBy,

                    // String size
                    MaxLength = maxLen,
                    FixedLength = fixedLen,

                    // Decimal precision/scale
                    Precision = prec?.Precision,
                    Scale = prec?.Scale
                };

                table.Columns.Add(col);
            }
        }


        private void BuildConstraints(DbTable table, Type entityType, SchemaModel model)
        {
            // PK
            var pkProps = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Prop = p, Pk = p.GetCustomAttribute<PrimaryKeyAttribute>() })
                .Where(x => x.Pk is not null)
                .OrderBy(x => x.Pk!.Order)
                .ToList();

            if (pkProps.Count > 0)
            {
                var cols = pkProps.Select(x => GetColumnName(x.Prop)).ToArray();
                var pkName = pkProps.First().Pk!.Name
                             ?? (_opt.UseConventionalConstraintNames ? $"PK_{table.Name}" : "PK");

                table.PrimaryKey = new DbPrimaryKey(pkName, cols);
            }

            // Unique groups (por Name)
            var uniqueGroups = new Dictionary<string, List<(PropertyInfo Prop, int Order)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var uq in p.GetCustomAttributes<UniqueAttribute>())
                {
                    if (!uniqueGroups.TryGetValue(uq.Name, out var list))
                        uniqueGroups[uq.Name] = list = new();
                    list.Add((p, uq.Order));
                }
            }

            foreach (var g in uniqueGroups)
            {
                var cols = g.Value.OrderBy(x => x.Order).Select(x => GetColumnName(x.Prop)).ToArray();
                table.Uniques.Add(new DbUnique(g.Key, cols));
            }

            // Checks (class-level)
            foreach (var chk in entityType.GetCustomAttributes<CheckAttribute>())
            {
                var name = chk.Name ?? (_opt.UseConventionalConstraintNames ? $"CK_{table.Name}_{table.Checks.Count + 1}" : "CK");
                table.Checks.Add(new DbCheck(name, chk.Expression));
            }

            // Checks (property-level)
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var chk in p.GetCustomAttributes<CheckAttribute>())
                {
                    var name = chk.Name ?? (_opt.UseConventionalConstraintNames ? $"CK_{table.Name}_{GetColumnName(p)}" : "CK");
                    table.Checks.Add(new DbCheck(name, chk.Expression));
                }
            }

            // FKs
            foreach (var p in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                if (fk is null) continue;

                var refTableAttr = fk.RefEntityType.GetCustomAttribute<TableAttribute>();
                var refSchema = refTableAttr?.Schema ?? _opt.DefaultSchema;
                var refTable = refTableAttr?.Name ?? fk.RefEntityType.Name;

                // ref column name (si el prop en ref entity tiene [Column], la usamos)
                var refProp = fk.RefEntityType.GetProperty(fk.RefProperty, BindingFlags.Public | BindingFlags.Instance);
                if (refProp is null)
                    throw new InvalidOperationException($"ForeignKey reference property not found: {fk.RefEntityType.Name}.{fk.RefProperty}");

                var fkName = fk.Name
                             ?? (_opt.UseConventionalConstraintNames ? $"FK_{table.Name}_{refTable}_{GetColumnName(p)}" : "FK");

                table.ForeignKeys.Add(new DbForeignKey(
                    fkName,
                    new[] { GetColumnName(p) },
                    refSchema,
                    refTable,
                    new[] { GetColumnName(refProp) },
                    fk.OnDelete,
                    fk.OnUpdate
                ));
            }
        }

        private static string GetColumnName(PropertyInfo p)
            => p.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? p.Name;
    }
}
