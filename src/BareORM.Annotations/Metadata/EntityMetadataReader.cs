using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BareORM.Annotations.Attributes;

namespace BareORM.Annotations.Metadata
{
    public static class EntityMetadataReader
    {
        private static readonly ConcurrentDictionary<Type, EntityMetadata> _cache = new();

        public static EntityMetadata For<T>() => For(typeof(T));

        public static EntityMetadata For(Type t)
            => _cache.GetOrAdd(t, Build);

        private static EntityMetadata Build(Type t)
        {
            var tableAttr = t.GetCustomAttribute<TableAttribute>();
            var schema = tableAttr?.Schema ?? "dbo";
            var table = tableAttr?.Name ?? t.Name;

            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .ToArray();

            var columns = new List<ColumnMetadata>();
            var fks = new List<ForeignKeyMetadata>();
            var checks = new List<CheckMetadata>();

            foreach (var p in props)
            {
                var colName = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;

                var pk = p.GetCustomAttribute<PrimaryKeyAttribute>();
                var id = p.GetCustomAttribute<IncrementalKeyAttribute>();

                columns.Add(new ColumnMetadata
                {
                    Property = p,
                    ColumnName = colName,
                    IsPrimaryKey = pk is not null,
                    PrimaryKeyOrder = pk?.Order ?? 0,
                    IsIdentity = id is not null,
                    IdentitySeed = id?.Seed ?? 0,
                    IdentityIncrement = id?.Increment ?? 0
                });

                var fk = p.GetCustomAttribute<ForeignKeyAttribute>();
                if (fk is not null)
                {
                    fks.Add(new ForeignKeyMetadata
                    {
                        Property = p,
                        ColumnName = colName,
                        RefEntityType = fk.RefEntityType,
                        RefProperty = fk.RefProperty,
                        Name = fk.Name,
                        OnDelete = fk.OnDelete,
                        OnUpdate = fk.OnUpdate
                    });
                }

                foreach (var chk in p.GetCustomAttributes<CheckAttribute>())
                {
                    checks.Add(new CheckMetadata
                    {
                        Expression = chk.Expression,
                        Name = chk.Name,
                        Property = p
                    });
                }
            }

            // checks a nivel entidad
            foreach (var chk in t.GetCustomAttributes<CheckAttribute>())
            {
                checks.Add(new CheckMetadata
                {
                    Expression = chk.Expression,
                    Name = chk.Name,
                    Property = null
                });
            }

            var pkCols = columns.Where(c => c.IsPrimaryKey)
                .OrderBy(c => c.PrimaryKeyOrder)
                .ToList();

            // Unique groups
            var uniqueGroups = new Dictionary<string, List<(PropertyInfo Prop, string Column, int Order)>>();
            foreach (var p in props)
            {
                var colName = p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name;

                foreach (var uq in p.GetCustomAttributes<UniqueAttribute>())
                {
                    if (!uniqueGroups.TryGetValue(uq.Name, out var list))
                        uniqueGroups[uq.Name] = list = new();
                    list.Add((p, colName, uq.Order));
                }
            }

            var uniques = uniqueGroups
                .Select(g => new UniqueMetadata
                {
                    Name = g.Key,
                    Columns = g.Value.OrderBy(x => x.Order).ToList()
                })
                .ToList();

            return new EntityMetadata
            {
                EntityType = t,
                Schema = schema,
                Table = table,
                Columns = columns,
                PrimaryKeys = pkCols,
                Uniques = uniques,
                ForeignKeys = fks,
                Checks = checks
            };
        }

    }
}
