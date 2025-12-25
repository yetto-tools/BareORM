using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BareORM.Annotations.Attributes;

namespace BareORM.Annotations.Metadata
{
    /// <summary>
    /// Reads and caches metadata for entity CLR types based on BareORM annotations.
    /// </summary>
    /// <remarks>
    /// This reader is intended for high-performance scenarios:
    /// metadata is computed once per entity type and then cached in-memory.
    ///
    /// <para>
    /// The metadata produced here is typically used by:
    /// <list type="bullet">
    ///   <item><description>Schema building (tables/columns/constraints)</description></item>
    ///   <item><description>Migrations scaffolding and diffing</description></item>
    ///   <item><description>Runtime mapping strategies (PK/FK/uniques for stitching graphs)</description></item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Default conventions:
    /// <list type="bullet">
    ///   <item><description>Schema defaults to <c>dbo</c> if not specified in <see cref="TableAttribute"/>.</description></item>
    ///   <item><description>Table name defaults to CLR type name if not specified in <see cref="TableAttribute"/>.</description></item>
    ///   <item><description>Column name defaults to property name if not specified in <see cref="ColumnNameAttribute"/>.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class EntityMetadataReader
    {
        private static readonly ConcurrentDictionary<Type, EntityMetadata> _cache = new();

        /// <summary>
        /// Gets metadata for the given entity type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Entity CLR type.</typeparam>
        /// <returns>An <see cref="EntityMetadata"/> instance for <typeparamref name="T"/>.</returns>
        public static EntityMetadata For<T>() => For(typeof(T));

        /// <summary>
        /// Gets metadata for the given entity CLR <see cref="Type"/>.
        /// </summary>
        /// <param name="t">Entity CLR type.</param>
        /// <returns>An <see cref="EntityMetadata"/> instance for <paramref name="t"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="t"/> is null.</exception>
        public static EntityMetadata For(Type t)
        {
            if (t is null) throw new ArgumentNullException(nameof(t));
            return _cache.GetOrAdd(t, Build);
        }

        /// <summary>
        /// Builds the metadata for an entity type by scanning its annotations.
        /// This method is invoked only on cache misses.
        /// </summary>
        /// <param name="t">Entity CLR type.</param>
        /// <returns>Computed metadata for the entity.</returns>
        private static EntityMetadata Build(Type t)
        {
            var tableAttr = t.GetCustomAttribute<TableAttribute>();
            var schema = tableAttr?.Schema ?? "dbo";
            var table = tableAttr?.Name ?? t.Name;

            // Candidate properties: public instance, readable/writable, non-indexers.
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .ToArray();

            var columns = new List<ColumnMetadata>();
            var fks = new List<ForeignKeyMetadata>();
            var checks = new List<CheckMetadata>();

            foreach (var p in props)
            {
                var colName = p.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? p.Name;

                var pk = p.GetCustomAttribute<PrimaryKeyAttribute>();
                var id = p.GetCustomAttribute<IncrementalKeyAttribute>();

                // Column metadata (PK/identity info)
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

                // FK metadata (single-column FK per property)
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

                // Property-level checks
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

            // Entity-level checks
            foreach (var chk in t.GetCustomAttributes<CheckAttribute>())
            {
                checks.Add(new CheckMetadata
                {
                    Expression = chk.Expression,
                    Name = chk.Name,
                    Property = null
                });
            }

            // PK columns ordered by attribute order
            var pkCols = columns
                .Where(c => c.IsPrimaryKey)
                .OrderBy(c => c.PrimaryKeyOrder)
                .ToList();

            // Unique constraint groups
            // Grouping key: UniqueAttribute.Name
            var uniqueGroups = new Dictionary<string, List<(PropertyInfo Prop, string Column, int Order)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in props)
            {
                var colName = p.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? p.Name;

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
