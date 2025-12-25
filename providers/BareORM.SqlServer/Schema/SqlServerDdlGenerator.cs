using System.Text;
using BareORM.Schema;
using BareORM.Annotations;
using BareORM.Schema.Types;

namespace BareORM.SqlServer.Schema
{
    /// <summary>
    /// Generador de DDL para SQL Server a partir de un <see cref="SchemaModel"/> agnóstico.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Convierte el modelo (<see cref="SchemaModel"/> → schemas/tablas/columnas/constraints) en una lista de
    /// “batches” SQL (strings) que pueden ejecutarse secuencialmente.
    /// </para>
    /// <para>
    /// Orden de generación:
    /// </para>
    /// <list type="number">
    /// <item><description>Crear schemas (si no existen).</description></item>
    /// <item><description>Crear tablas (si no existen), incluyendo PK.</description></item>
    /// <item><description>Agregar UNIQUE, CHECK e INDEX (si no existen).</description></item>
    /// <item><description>Agregar FOREIGN KEYS al final (si no existen).</description></item>
    /// </list>
    /// <para>
    /// Nota importante: el DDL es idempotente “light” (usa <c>IF NOT EXISTS</c> y <c>OBJECT_ID</c>).
    /// No hace ALTER TABLE para columnas existentes ni diff avanzado; su objetivo es bootstrap inicial.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new SchemaModelBuilder();
    /// var model = builder.Build(typeof(User), typeof(Order));
    ///
    /// var ddl = new SqlServerDdlGenerator().Generate(model);
    /// foreach (var batch in ddl)
    ///     Console.WriteLine(batch + "\nGO\n");
    /// </code>
    /// </example>
    public sealed class SqlServerDdlGenerator
    {
        /// <summary>
        /// Genera batches SQL para crear el esquema representado por <paramref name="model"/>.
        /// </summary>
        /// <param name="model">Modelo de esquema.</param>
        /// <returns>Lista de batches SQL listos para ejecutar en orden.</returns>
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

        /// <summary>
        /// Construye el batch para crear una tabla (si no existe) incluyendo columnas y PK.
        /// </summary>
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

        /// <summary>
        /// Construye la definición SQL de una columna dentro de un CREATE TABLE.
        /// </summary>
        /// <param name="table">Tabla que contiene la columna (se usa para naming de defaults).</param>
        /// <param name="c">Columna del modelo.</param>
        /// <returns>Definición tipo: <c>[Col] INT NOT NULL CONSTRAINT [DF_T_Col] DEFAULT 0</c></returns>
        /// <remarks>
        /// <para>
        /// Maneja:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Mapeo de tipos via <see cref="MapType(ColumnType)"/>.</description></item>
        /// <item><description>IDENTITY(1,1) para incremental keys (solo si el tipo es INT/BIGINT).</description></item>
        /// <item><description>NULL/NOT NULL según <see cref="DbColumn.IsNullable"/>.</description></item>
        /// <item><description>DEFAULT constraint si <see cref="DbColumn.DefaultValue"/> no es null.</description></item>
        /// </list>
        /// </remarks>
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
            var def = c.DefaultValue is null
                ? ""
                : $" CONSTRAINT {Q($"DF_{table.Name}_{c.Name}")} DEFAULT {FormatDefault(c.DefaultValue)}";

            return $"{Q(c.Name)} {sqlType}{identity} {nullability}{def}";
        }

        /// <summary>
        /// Construye el batch para agregar un constraint UNIQUE si no existe.
        /// </summary>
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

        /// <summary>
        /// Construye el batch para agregar un constraint CHECK si no existe.
        /// </summary>
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

        /// <summary>
        /// Construye el batch para crear un índice si no existe.
        /// </summary>
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

        /// <summary>
        /// Construye el batch para agregar una FOREIGN KEY si no existe.
        /// </summary>
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

        /// <summary>
        /// Traduce <see cref="ReferentialAction"/> al fragmento SQL Server <c>ON DELETE/ON UPDATE ...</c>.
        /// </summary>
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

        /// <summary>
        /// Mapea un <see cref="ColumnType"/> lógico a su tipo T-SQL equivalente.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Nota: el mapeo actual asume Unicode para strings y usa NVARCHAR.
        /// Para <see cref="JsonType"/> se usa NVARCHAR(MAX) (opcionalmente podrías agregar CHECK con ISJSON()).
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Escapa un identificador para SQL Server usando corchetes <c>[...]</c>.
        /// </summary>
        private static string Q(string name) => $"[{name.Replace("]", "]]")}]";

        /// <summary>
        /// Escapa un literal SQL (string) duplicando comillas simples.
        /// </summary>
        private static string EscapeSqlLiteral(string s) => s.Replace("'", "''");

        /// <summary>
        /// Formatea un valor .NET como literal SQL para <c>DEFAULT</c>.
        /// </summary>
        /// <remarks>
        /// Si el tipo no está contemplado, se serializa usando <c>ToString()</c> como NVARCHAR.
        /// </remarks>
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
