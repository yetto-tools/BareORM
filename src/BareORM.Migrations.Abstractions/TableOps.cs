using BareORM.Annotations;          // ReferentialAction
using BareORM.Schema.Types;         // ColumnType

namespace BareORM.Migrations.Abstractions
{
    public sealed record CreateTableOp(string Schema, string Name) : MigrationOperation
    {
        public List<AddColumnOp> Columns { get; } = new();
        public AddPrimaryKeyOp? PrimaryKey { get; set; }

        public List<AddUniqueOp> Uniques { get; } = new();
        public List<AddCheckOp> Checks { get; } = new();
        public List<CreateIndexOp> Indexes { get; } = new();
        public List<AddForeignKeyOp> ForeignKeys { get; } = new();
    }

    public sealed record DropTableOp(string Schema, string Name) : MigrationOperation;

    public sealed record AddColumnOp(string Schema, string Table, string Name, ColumnType Type) : MigrationOperation
    {
        public bool IsNullable { get; init; }
        public object? DefaultValue { get; init; }

        public bool IsIncrementalKey { get; init; }
        public string? SequenceName { get; init; }
    }

    public sealed record DropColumnOp(string Schema, string Table, string Name) : MigrationOperation;

    public sealed record AddPrimaryKeyOp(string Schema, string Table, string Name, string[] Columns) : MigrationOperation;
    public sealed record DropPrimaryKeyOp(string Schema, string Table, string Name) : MigrationOperation;

    public sealed record AddUniqueOp(string Schema, string Table, string Name, string[] Columns) : MigrationOperation;
    public sealed record DropUniqueOp(string Schema, string Table, string Name) : MigrationOperation;

    public sealed record AddCheckOp(string Schema, string Table, string Name, string Expression) : MigrationOperation;
    public sealed record DropCheckOp(string Schema, string Table, string Name) : MigrationOperation;

    public sealed record CreateIndexOp(string Schema, string Table, string Name, string[] Columns, bool IsUnique) : MigrationOperation;
    public sealed record DropIndexOp(string Schema, string Table, string Name) : MigrationOperation;

    public sealed record AddForeignKeyOp(
        string Schema, string Table, string Name,
        string[] Columns,
        string RefSchema, string RefTable,
        string[] RefColumns,
        ReferentialAction OnDelete,
        ReferentialAction OnUpdate
    ) : MigrationOperation;

    public sealed record DropForeignKeyOp(string Schema, string Table, string Name) : MigrationOperation;
}