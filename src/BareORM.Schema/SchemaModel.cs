using BareORM.Annotations;
using BareORM.Schema.Types;


namespace BareORM.Schema
{
    public sealed class SchemaModel
    {
        public Dictionary<string, DbSchema> Schemas { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DbSchema GetOrAddSchema(string name)
        {
            if (!Schemas.TryGetValue(name, out var s))
            {
                s = new DbSchema(name);
                Schemas[name] = s;
            }
            return s;
        }

        public IEnumerable<DbTable> AllTables()
            => Schemas.Values.SelectMany(s => s.Tables.Values);

        public override string ToString()
            => $"Schemas={Schemas.Count}, Tables={AllTables().Count()}";
    }

    public sealed class DbSchema
    {
        public string Name { get; }
        public Dictionary<string, DbTable> Tables { get; } = new(StringComparer.OrdinalIgnoreCase);

        public DbSchema(string name) => Name = name;

        public DbTable GetOrAddTable(string tableName, Type clrType)
        {
            if (!Tables.TryGetValue(tableName, out var t))
            {
                t = new DbTable(Name, tableName, clrType);
                Tables[tableName] = t;
            }
            return t;
        }
    }

    public sealed class DbTable
    {
        public string Schema { get; }
        public string Name { get; }
        public Type ClrType { get; }

        public List<DbColumn> Columns { get; } = new();

        public DbPrimaryKey? PrimaryKey { get; set; }
        public List<DbUnique> Uniques { get; } = new();
        public List<DbForeignKey> ForeignKeys { get; } = new();
        public List<DbCheck> Checks { get; } = new();
        public List<DbIndex> Indexes { get; } = new();

        public DbTable(string schema, string name, Type clrType)
        {
            Schema = schema;
            Name = name;
            ClrType = clrType;
        }

        public DbColumn? FindColumn(string columnName)
            => Columns.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
    }

    public sealed class DbColumn
    {
        public string Name { get; }
        public Type ClrType { get; }
        public ColumnType Type { get; }

        public bool IsNullable { get; set; }
        public object? DefaultValue { get; set; }

        // IncrementalKey (agnóstico)
        public bool IsIncrementalKey { get; set; }
        public string? SequenceName { get; set; }
        public long? StartWith { get; set; }
        public long? IncrementBy { get; set; }

        // Para diff / tracking
        public string ClrPropertyName { get; }

        public DbColumn(string name, Type clrType, ColumnType type, string clrPropertyName)
        {
            Name = name;
            ClrType = clrType;
            Type = type;
            ClrPropertyName = clrPropertyName;
        }
    }

    public sealed class DbPrimaryKey
    {
        public string Name { get; }
        public string[] Columns { get; }

        public DbPrimaryKey(string name, string[] columns)
        {
            Name = name;
            Columns = columns;
        }
    }

    public sealed class DbUnique
    {
        public string Name { get; }
        public string[] Columns { get; }

        public DbUnique(string name, string[] columns)
        {
            Name = name;
            Columns = columns;
        }
    }

    public sealed class DbForeignKey
    {
        public string Name { get; }
        public string[] Columns { get; }

        public string RefSchema { get; }
        public string RefTable { get; }
        public string[] RefColumns { get; }

        public ReferentialAction OnDelete { get; }
        public ReferentialAction OnUpdate { get; }

        public DbForeignKey(
            string name,
            string[] columns,
            string refSchema,
            string refTable,
            string[] refColumns,
            ReferentialAction onDelete,
            ReferentialAction onUpdate)
        {
            Name = name;
            Columns = columns;
            RefSchema = refSchema;
            RefTable = refTable;
            RefColumns = refColumns;
            OnDelete = onDelete;
            OnUpdate = onUpdate;
        }
    }

    public sealed class DbCheck
    {
        public string Name { get; }
        public string Expression { get; }

        public DbCheck(string name, string expression)
        {
            Name = name;
            Expression = expression;
        }
    }

    public sealed class DbIndex
    {
        public string Name { get; }
        public string[] Columns { get; }
        public bool IsUnique { get; }

        public DbIndex(string name, string[] columns, bool isUnique)
        {
            Name = name;
            Columns = columns;
            IsUnique = isUnique;
        }
    }
}
