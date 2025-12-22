using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BareORM.Annotations.Attributes;
using BareORM.Schema.Utilities;

namespace BareORM.Schema.Builders
{
    public sealed class SchemaModelBuilder
    {
        private readonly SchemaModelBuilderOptions _opt;

        public SchemaModelBuilder(SchemaModelBuilderOptions? options = null)
            => _opt = options ?? new SchemaModelBuilderOptions();

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
                var colAttr = p.GetCustomAttribute<ColumnAttribute>();
                var colName = colAttr?.Name ?? p.Name;

                var clrType = p.PropertyType;
                var isNullable = IsNullable(p);

                var colType = _opt.TypeMapper.Map(clrType);

                var inc = p.GetCustomAttribute<IncrementalKeyAttribute>();

                var col = new DbColumn(colName, clrType, colType, p.Name)
                {
                    IsNullable = isNullable,
                    IsIncrementalKey = inc is not null,
                    SequenceName = inc?.SequenceName,
                    StartWith = inc?.StartWith,
                    IncrementBy = inc?.IncrementBy
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

        private static bool IsNullable(PropertyInfo p)
        {
            var t = p.PropertyType;
            if (!t.IsValueType) return true;                 // ref type
            return Nullable.GetUnderlyingType(t) is not null; // Nullable<T>
        }

        private static string GetColumnName(PropertyInfo p)
            => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;
    }
}
