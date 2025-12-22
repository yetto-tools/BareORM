using System.Globalization;
using System.Text;
using BareORM.Annotations;
using BareORM.Migrations.Abstractions;
using BareORM.Migrations.Abstractions.Interfaces;
using BareORM.Schema.Types;



namespace BareORM.SqlServer.Migrations
{
    public sealed class SqlServerMigrationSqlGenerator : IMigrationSqlGenerator
    {
        public IReadOnlyList<string> Generate(IReadOnlyList<MigrationOperation> operations)
        {
            var batches = new List<string>();
            var deferredFks = new List<AddForeignKeyOp>();

            foreach (var op in operations)
            {
                switch (op)
                {
                    case SqlOp s:
                        batches.Add(s.Sql);
                        break;

                    case CreateTableOp ct:
                        batches.Add(BuildCreateTable(ct));
                        // uniques/checks/indexes del create-table
                        foreach (var uq in ct.Uniques) batches.Add(BuildAddUnique(uq));
                        foreach (var ck in ct.Checks) batches.Add(BuildAddCheck(ck));
                        foreach (var ix in ct.Indexes) batches.Add(BuildCreateIndex(ix));

                        // FKs al final
                        deferredFks.AddRange(ct.ForeignKeys);
                        break;

                    case DropTableOp dt:
                        batches.Add($"DROP TABLE {Q(dt.Schema)}.{Q(dt.Name)};");
                        break;

                    case AddColumnOp ac:
                        batches.Add(BuildAddColumn(ac));
                        break;

                    case DropColumnOp dc:
                        batches.Add($"ALTER TABLE {Q(dc.Schema)}.{Q(dc.Table)} DROP COLUMN {Q(dc.Name)};");
                        break;

                    case AddPrimaryKeyOp pk:
                        batches.Add($"ALTER TABLE {Q(pk.Schema)}.{Q(pk.Table)} ADD CONSTRAINT {Q(pk.Name)} PRIMARY KEY ({string.Join(", ", pk.Columns.Select(Q))});");
                        break;

                    case DropPrimaryKeyOp dpk:
                        batches.Add($"ALTER TABLE {Q(dpk.Schema)}.{Q(dpk.Table)} DROP CONSTRAINT {Q(dpk.Name)};");
                        break;

                    case AddUniqueOp au:
                        batches.Add(BuildAddUnique(au));
                        break;

                    case DropUniqueOp du:
                        batches.Add($"ALTER TABLE {Q(du.Schema)}.{Q(du.Table)} DROP CONSTRAINT {Q(du.Name)};");
                        break;

                    case AddCheckOp ack:
                        batches.Add(BuildAddCheck(ack));
                        break;

                    case DropCheckOp dck:
                        batches.Add($"ALTER TABLE {Q(dck.Schema)}.{Q(dck.Table)} DROP CONSTRAINT {Q(dck.Name)};");
                        break;

                    case CreateIndexOp cix:
                        batches.Add(BuildCreateIndex(cix));
                        break;

                    case DropIndexOp dix:
                        batches.Add($"DROP INDEX {Q(dix.Name)} ON {Q(dix.Schema)}.{Q(dix.Table)};");
                        break;

                    case AddForeignKeyOp fk:
                        deferredFks.Add(fk);
                        break;

                    case DropForeignKeyOp dfk:
                        batches.Add($"ALTER TABLE {Q(dfk.Schema)}.{Q(dfk.Table)} DROP CONSTRAINT {Q(dfk.Name)};");
                        break;

                    default:
                        throw new NotSupportedException($"Operation not supported: {op.GetType().Name}");
                }
            }

            foreach (var fk in deferredFks)
                batches.Add(BuildAddForeignKey(fk));

            return batches;
        }

        private static string BuildCreateTable(CreateTableOp ct)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {Q(ct.Schema)}.{Q(ct.Name)}");
            sb.AppendLine("(");

            for (int i = 0; i < ct.Columns.Count; i++)
            {
                var c = ct.Columns[i];
                sb.Append("    ").Append(BuildColumn(c));

                var hasMore = i < ct.Columns.Count - 1 || ct.PrimaryKey is not null;
                sb.AppendLine(hasMore ? "," : "");
            }

            if (ct.PrimaryKey is not null)
            {
                var cols = string.Join(", ", ct.PrimaryKey.Columns.Select(Q));
                sb.AppendLine($"    CONSTRAINT {Q(ct.PrimaryKey.Name)} PRIMARY KEY ({cols})");
            }

            sb.AppendLine(");");
            return sb.ToString();
        }

        private static string BuildAddColumn(AddColumnOp ac)
            => $"ALTER TABLE {Q(ac.Schema)}.{Q(ac.Table)} ADD {BuildColumn(ac)};";

        private static string BuildColumn(AddColumnOp c)
        {
            var sqlType = MapType(c.Type);

            var identity = "";
            if (c.IsIncrementalKey && (c.Type is Int32Type or Int64Type))
                identity = " IDENTITY(1,1)";

            var nullability = c.IsNullable ? "NULL" : "NOT NULL";
            var def = c.DefaultValue is null ? "" : $" DEFAULT {FormatDefault(c.DefaultValue)}";

            return $"{Q(c.Name)} {sqlType}{identity} {nullability}{def}";
        }

        private static string BuildAddUnique(AddUniqueOp uq)
            => $"ALTER TABLE {Q(uq.Schema)}.{Q(uq.Table)} ADD CONSTRAINT {Q(uq.Name)} UNIQUE ({string.Join(", ", uq.Columns.Select(Q))});";

        private static string BuildAddCheck(AddCheckOp ck)
            => $"ALTER TABLE {Q(ck.Schema)}.{Q(ck.Table)} ADD CONSTRAINT {Q(ck.Name)} CHECK ({ck.Expression});";

        private static string BuildCreateIndex(CreateIndexOp ix)
        {
            var unique = ix.IsUnique ? "UNIQUE " : "";
            return $"CREATE {unique}INDEX {Q(ix.Name)} ON {Q(ix.Schema)}.{Q(ix.Table)} ({string.Join(", ", ix.Columns.Select(Q))});";
        }

        private static string BuildAddForeignKey(AddForeignKeyOp fk)
        {
            var cols = string.Join(", ", fk.Columns.Select(Q));
            var refCols = string.Join(", ", fk.RefColumns.Select(Q));

            var onDelete = ToSqlAction(fk.OnDelete, "DELETE");
            var onUpdate = ToSqlAction(fk.OnUpdate, "UPDATE");

            return $@"
                ALTER TABLE {Q(fk.Schema)}.{Q(fk.Table)}
                ADD CONSTRAINT {Q(fk.Name)}
                FOREIGN KEY ({cols})
                REFERENCES {Q(fk.RefSchema)}.{Q(fk.RefTable)} ({refCols})
                {onDelete}
                {onUpdate};".Trim();
        }

        private static string ToSqlAction(ReferentialAction a, string kind) => a switch
        {
            ReferentialAction.Cascade => $"ON {kind} CASCADE",
            ReferentialAction.SetNull => $"ON {kind} SET NULL",
            ReferentialAction.SetDefault => $"ON {kind} SET DEFAULT",
            ReferentialAction.Restrict => $"ON {kind} NO ACTION",
            _ => ""
        };

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
            JsonType => "NVARCHAR(MAX)",
            _ => "NVARCHAR(MAX)"
        };

        private static string Q(string name) => $"[{name.Replace("]", "]]")}]";

        private static string FormatDefault(object value) => value switch
        {
            bool b => b ? "1" : "0",
            string s => $"N'{s.Replace("'", "''")}'",
            int i => i.ToString(),
            long l => l.ToString(),
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            DateTime dt => $"'{dt:yyyy-MM-ddTHH:mm:ss.fffffff}'",
            _ => $"N'{(value.ToString() ?? "").Replace("'", "''")}'"
        };
    }
}
