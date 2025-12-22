
using BareORM.Schema.Types;

namespace BareORM.Migrations.Abstractions
{
    public sealed class MigrationBuilder
    {
        private readonly List<MigrationOperation> _ops = new();

        // ✅ visible desde BareORM.Migrations, pero no editable desde afuera
        public IReadOnlyList<MigrationOperation> Operations => _ops;

        public CreateTableOp CreateTable(string schema, string name)
        {
            var op = new CreateTableOp(schema, name);
            _ops.Add(op);
            return op;
        }

        public void DropTable(string schema, string name)
            => _ops.Add(new DropTableOp(schema, name));

        public void AddColumn(string schema, string table, string name, ColumnType type, bool nullable = true, object? defaultValue = null)
            => _ops.Add(new AddColumnOp(schema, table, name, type) { IsNullable = nullable, DefaultValue = defaultValue });

        public void DropColumn(string schema, string table, string name)
            => _ops.Add(new DropColumnOp(schema, table, name));

        public void Sql(string sql)
            => _ops.Add(new SqlOp(sql));
    }
}
