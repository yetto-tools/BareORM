
using System.Reflection;

namespace BareORM.Annotations.Metadata
{
    public sealed class EntityMetadata
    {
        public required Type EntityType { get; init; }
        public required string Schema { get; init; }
        public required string Table { get; init; }

        public required IReadOnlyList<ColumnMetadata> Columns { get; init; }
        public required IReadOnlyList<ColumnMetadata> PrimaryKeys { get; init; }
        public required IReadOnlyList<UniqueMetadata> Uniques { get; init; }
        public required IReadOnlyList<ForeignKeyMetadata> ForeignKeys { get; init; }
        public required IReadOnlyList<CheckMetadata> Checks { get; init; }
    }

    public sealed class ColumnMetadata
    {
        public required PropertyInfo Property { get; init; }
        public required string ColumnName { get; init; }

        public bool IsPrimaryKey { get; init; }
        public long PrimaryKeyOrder { get; init; }

        public bool IsIdentity { get; init; }
        public long IdentitySeed { get; init; }
        public long IdentityIncrement { get; init; }
    }

    public sealed class UniqueMetadata
    {
        public required string Name { get; init; }
        public required IReadOnlyList<(PropertyInfo Prop, string Column, int Order)> Columns { get; init; }
    }

    public sealed class ForeignKeyMetadata
    {
        public required PropertyInfo Property { get; init; }
        public required string ColumnName { get; init; }

        public required Type RefEntityType { get; init; }
        public required string RefProperty { get; init; }

        public string? Name { get; init; }
        public ReferentialAction OnDelete { get; init; }
        public ReferentialAction OnUpdate { get; init; }
    }

    public sealed class CheckMetadata
    {
        public required string Expression { get; init; }
        public string? Name { get; init; }
        public PropertyInfo? Property { get; init; } // null = entity-level
    }
}
