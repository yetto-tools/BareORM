

using System.Data.Common;
using System.Text;
using BareORM.Schema;
using BareORM.Annotations;
using BareORM.Schema.Types;
using BareORM.Annotations.Attributes;

namespace BareORM.SqlServer.Schema
{
    public sealed class SqlServerDdlGenerator
    {
        public IReadOnlyList<string> Generate(SchemaModel model)
        {
            var batches = new List<string>();

            // 1) Schemas
            foreach (var s in model.Schemas.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                batches.Add(
                $@"IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{EscapeSqlLiteral(s.Name)}') 
                    EXEC(N'CREATE SCHEMA {Q(s.Name)}');
                ".Trim()
                );
            }

            // 2) Create tables (PK incluido)
            foreach (var t in model.AllTables().OrderBy(x => $"{x.Schema}.{x.Name}", StringComparer.OrdinalIgnoreCase))
            {
                batches.Add(BuildCreateTable(t));
            }

            // 3) Unique + Checks + Indexes
            foreach (var t in model.AllTables().OrderBy(x => $"{x.Schema}.{x.Name}", StringComparer.OrdinalIgnoreCase))
            {
                foreach (var uq in t.Uniques)
                    batches.Add(BuildAddUnique(t, uq));

                foreach (var ck in t.Checks)
                    batches.Add(BuildAddCheck(t, ck));

                foreach (var ix in t.Indexes)
                    batches.Add(BuildCreateIndex(t, ix));
            }

            // 4) Foreign Keys (al final)
            foreach (var t in model.AllTables().OrderBy(x => $"{x.Schema}.{x.Name}", StringComparer.OrdinalIgnoreCase))
            {
                foreach (var fk in t.ForeignKeys)
                    batches.Add(BuildAddForeignKey(t, fk));
            }

            return batches;
        }

        private static string BuildCreateTable(DbTable t)
        {
            var sb = new StringBuilder();

            sb.AppendLine($@"
IF OBJECT_ID(N'{EscapeSqlLiteral(t.Schema)}.{EscapeSqlLiteral(t.Name)}', N'U') IS NULL
BEGIN
    CREATE TABLE {Q(t.Schema)}.{Q(t.Name)}
    (");

            for (int i = 0; i < t.Columns.Count; i++)
            {
                var c = t.Columns[i];
                sb.Append("        ").Append(BuildColumnDefinition(t, c));

                if (i < t.Columns.Count - 1 || t.PrimaryKey is not null)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
            }

            if (t.PrimaryKey is not null)
            {
                var pkCols = string.Join(", ", t.PrimaryKey.Columns.Select(Q));
                sb.AppendLine($"        CONSTRAINT {Q(t.PrimaryKey.Name)} PRIMARY KEY ({pkCols})");
            }

            sb.AppendLine(@"    );
END
".Trim());

            return sb.ToString();
        }

        private static string BuildColumnDefinition(DbTable table, BareORM.Schema.DbColumn c)
        {
            var sqlType = MapType(c.Type);

            var identity = "";
            if (c.IsIncrementalKey)
            {
                // Para SQL Server solo aplica bien a int/bigint típicamente
                if (c.Type is BareORM.Schema.Types.Int32Type or BareORM.Schema.Types.Int64Type)
                    identity = " IDENTITY(1,1)";
            }

            var nullability = c.IsNullable ? "NULL" : "NOT NULL";
            var def = c.DefaultValue is null ? "" : $" CONSTRAINT {Q($"DF_{table.Name}_{c.Name}")} DEFAULT {FormatDefault(c.DefaultValue)}";

            return $"{Q(c.Name)} {sqlType}{identity} {nullability}{def}";
        }

        private static string BuildAddUnique(DbTable t, DbUnique uq)
        {
            var cols = string.Join(", ", uq.Columns.Select(Q));
            return $@"
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'{EscapeSqlLiteral(uq.Name)}')
BEGIN
    ALTER TABLE {Q(t.Schema)}.{Q(t.Name)}
    ADD CONSTRAINT {Q(uq.Name)} UNIQUE ({cols});
END
".Trim();
        }

        private static string BuildAddCheck(DbTable t, DbCheck ck)
        {
            return $@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'{EscapeSqlLiteral(ck.Name)}')
BEGIN
    ALTER TABLE {Q(t.Schema)}.{Q(t.Name)}
    ADD CONSTRAINT {Q(ck.Name)} CHECK ({ck.Expression});
END
".Trim();
        }

        private static string BuildCreateIndex(DbTable t, DbIndex ix)
        {
            var unique = ix.IsUnique ? "UNIQUE " : "";
            var cols = string.Join(", ", ix.Columns.Select(Q));

            return $@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{EscapeSqlLiteral(ix.Name)}' AND object_id = OBJECT_ID(N'{EscapeSqlLiteral(t.Schema)}.{EscapeSqlLiteral(t.Name)}'))
BEGIN
    CREATE {unique}INDEX {Q(ix.Name)} ON {Q(t.Schema)}.{Q(t.Name)} ({cols});
END
".Trim();
        }

        private static string BuildAddForeignKey(DbTable t, DbForeignKey fk)
        {
            var cols = string.Join(", ", fk.Columns.Select(Q));
            var refCols = string.Join(", ", fk.RefColumns.Select(Q));

            var onDelete = ToSqlAction(fk.OnDelete, "DELETE");
            var onUpdate = ToSqlAction(fk.OnUpdate, "UPDATE");

            return $@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'{EscapeSqlLiteral(fk.Name)}')
BEGIN
    ALTER TABLE {Q(t.Schema)}.{Q(t.Name)}
    ADD CONSTRAINT {Q(fk.Name)}
    FOREIGN KEY ({cols})
    REFERENCES {Q(fk.RefSchema)}.{Q(fk.RefTable)} ({refCols})
    {onDelete}
    {onUpdate};
END
".Trim();
        }

        private static string ToSqlAction(ReferentialAction a, string kind)
        {
            return a switch
            {
                ReferentialAction.Cascade => $"ON {kind} CASCADE",
                ReferentialAction.SetNull => $"ON {kind} SET NULL",
                ReferentialAction.SetDefault => $"ON {kind} SET DEFAULT",
                ReferentialAction.Restrict => $"ON {kind} NO ACTION",
                _ => "" // NoAction
            };
        }

        private static string MapType(ColumnType t) => t switch
        {
            Int32Type => "INT",
            Int64Type => "BIGINT",
            BoolType => "BIT",
            DateTimeType => "DATETIME2",
            DateTimeOffsetType => "DATETIMEOFFSET",
            GuidType => "UNIQUEIDENTIFIER",
            DecimalType d => $"DECIMAL({d.Precision},{d.Scale})",
            DoubleType => "FLOAT",
            StringType s => s.MaxLength is null ? "NVARCHAR(MAX)" : $"NVARCHAR({s.MaxLength.Value})",
            BytesType b => b.MaxLength is null ? "VARBINARY(MAX)" : $"VARBINARY({b.MaxLength.Value})",
            JsonType => "NVARCHAR(MAX)", // o podrías agregar CHECK(ISJSON(col)=1)
            _ => "NVARCHAR(MAX)"
        };

        private static string Q(string name) => $"[{name.Replace("]", "]]")}]";

        private static string EscapeSqlLiteral(string s) => s.Replace("'", "''");

        private static string FormatDefault(object value)
        {
            return value switch
            {
                bool b => b ? "1" : "0",
                string s => $"N'{EscapeSqlLiteral(s)}'",
                int i => i.ToString(),
                long l => l.ToString(),
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                DateTime dt => $"'{dt:yyyy-MM-ddTHH:mm:ss.fffffff}'",
                _ => $"N'{EscapeSqlLiteral(value.ToString() ?? "")}'"
            };
        }
    }
}
