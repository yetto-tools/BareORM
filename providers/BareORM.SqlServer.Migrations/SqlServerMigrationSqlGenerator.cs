using System.Globalization;
using System.Text;
using BareORM.Annotations;
using BareORM.Migrations.Abstractions;
using BareORM.Migrations.Abstractions.Interfaces;
using BareORM.Schema.Types;

namespace BareORM.SqlServer.Migrations
{
    /// <summary>
    /// Generador de instrucciones SQL para migraciones de bases de datos SQL Server.
    /// </summary>
    /// <remarks>
    /// Esta clase implementa <see cref="IMigrationSqlGenerator"/> y convierte operaciones de migración<br/>
    /// abstractas en instrucciones SQL específicas de SQL Server. Las claves foráneas se generan al final<br/>
    /// para evitar problemas de dependencias entre tablas.
    /// <example>
    /// <code>
    /// var generator = new SqlServerMigrationSqlGenerator();
    /// var operations = new List&lt;MigrationOperation&gt;
    /// {
    ///     new CreateTableOp { Schema = "dbo", Name = "Users", ... }
    /// };
    /// var sqlStatements = generator.Generate(operations);
    /// foreach (var sql in sqlStatements)
    /// {
    ///     // Ejecutar cada instrucción SQL
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class SqlServerMigrationSqlGenerator : IMigrationSqlGenerator
    {
        /// <summary>
        /// Genera una lista de instrucciones SQL a partir de las operaciones de migración proporcionadas.
        /// </summary>
        /// <param name="operations">Lista de operaciones de migración a convertir en SQL.</param>
        /// <returns>
        /// Una lista de cadenas, donde cada cadena es una instrucción SQL completa lista para ejecutar.
        /// Las claves foráneas se agregan al final de la lista para evitar problemas de dependencias.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Se produce cuando se encuentra una operación no soportada.
        /// </exception>
        /// <remarks>
        /// Las operaciones se procesan en orden, excepto las claves foráneas (<see cref="AddForeignKeyOp"/>)
        /// que se difieren hasta el final para garantizar que todas las tablas referenciadas existan.
        /// Los scripts de vistas, rutinas y triggers se dividen en lotes usando <see cref="SqlServerScriptSplitter"/>.
        /// </remarks>
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

                    case CreateOrAlterViewOp v:
                        foreach (var b in SqlServerScriptSplitter.SplitBatches(v.DefinitionSql))
                            batches.Add(b);
                        break;

                    case CreateOrAlterRoutineOp r:
                        foreach (var b in SqlServerScriptSplitter.SplitBatches(r.DefinitionSql))
                            batches.Add(b);
                        break;

                    case DropViewOp dv:
                        batches.Add($"DROP VIEW {Q(dv.Schema)}.{Q(dv.Name)};");
                        break;

                    case DropRoutineOp dr:
                        var kw = dr.Kind switch
                        {
                            RoutineKind.Procedure => "PROCEDURE",
                            RoutineKind.ScalarFunction => "FUNCTION",
                            RoutineKind.TableFunction => "FUNCTION",
                            _ => "PROCEDURE"
                        };
                        batches.Add($"DROP {kw} {Q(dr.Schema)}.{Q(dr.Name)};");
                        break;

                    case CreateOrAlterTriggerOp t:
                        foreach (var b in SqlServerScriptSplitter.SplitBatches(t.DefinitionSql))
                            batches.Add(b);
                        break;

                    case DropTriggerOp dt:
                        batches.Add($"DROP TRIGGER {Q(dt.Schema)}.{Q(dt.Name)};");
                        break;

                    default:
                        throw new NotSupportedException($"Operation not supported: {op.GetType().Name}");
                }
            }

            foreach (var fk in deferredFks)
                batches.Add(BuildAddForeignKey(fk));

            return batches;
        }

        /// <summary>
        /// Construye la instrucción SQL CREATE TABLE a partir de una operación de creación de tabla.
        /// </summary>
        /// <param name="ct">La operación de creación de tabla que contiene la definición completa.</param>
        /// <returns>Una instrucción SQL CREATE TABLE con todas las columnas y la clave primaria si existe.</returns>
        /// <remarks>
        /// Las restricciones UNIQUE, CHECK e índices no se incluyen en esta instrucción; se generan por separado.
        /// Las claves foráneas se difieren y se agregan al final de todas las operaciones.
        /// </remarks>
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

        /// <summary>
        /// Construye la instrucción SQL ALTER TABLE ADD COLUMN a partir de una operación de adición de columna.
        /// </summary>
        /// <param name="ac">La operación de adición de columna que contiene la definición de la nueva columna.</param>
        /// <returns>Una instrucción SQL ALTER TABLE ADD completa.</returns>
        private static string BuildAddColumn(AddColumnOp ac)
            => $"ALTER TABLE {Q(ac.Schema)}.{Q(ac.Table)} ADD {BuildColumn(ac)};";

        /// <summary>
        /// Construye la definición de una columna para usar en CREATE TABLE o ALTER TABLE ADD.
        /// </summary>
        /// <param name="c">La operación que describe la columna a crear.</param>
        /// <returns>
        /// Una cadena con la definición completa de la columna, incluyendo nombre, tipo, IDENTITY (si aplica),
        /// nullabilidad y valor por defecto.
        /// </returns>
        /// <remarks>
        /// Si la columna es una clave incremental (<see cref="AddColumnOp.IsIncrementalKey"/>) y es de tipo
        /// <see cref="Int32Type"/> o <see cref="Int64Type"/>, se agrega IDENTITY(1,1).
        /// </remarks>
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

        /// <summary>
        /// Construye la instrucción SQL ALTER TABLE ADD CONSTRAINT UNIQUE.
        /// </summary>
        /// <param name="uq">La operación que describe la restricción UNIQUE a crear.</param>
        /// <returns>Una instrucción SQL ALTER TABLE ADD CONSTRAINT UNIQUE completa.</returns>
        private static string BuildAddUnique(AddUniqueOp uq)
            => $"ALTER TABLE {Q(uq.Schema)}.{Q(uq.Table)} ADD CONSTRAINT {Q(uq.Name)} UNIQUE ({string.Join(", ", uq.Columns.Select(Q))});";

        /// <summary>
        /// Construye la instrucción SQL ALTER TABLE ADD CONSTRAINT CHECK.
        /// </summary>
        /// <param name="ck">La operación que describe la restricción CHECK a crear.</param>
        /// <returns>Una instrucción SQL ALTER TABLE ADD CONSTRAINT CHECK completa.</returns>
        /// <remarks>
        /// La expresión CHECK se usa tal cual está definida en <see cref="AddCheckOp.Expression"/>,
        /// sin validación ni modificación.
        /// </remarks>
        private static string BuildAddCheck(AddCheckOp ck)
            => $"ALTER TABLE {Q(ck.Schema)}.{Q(ck.Table)} ADD CONSTRAINT {Q(ck.Name)} CHECK ({ck.Expression});";

        /// <summary>
        /// Construye la instrucción SQL CREATE INDEX o CREATE UNIQUE INDEX.
        /// </summary>
        /// <param name="ix">La operación que describe el índice a crear.</param>
        /// <returns>Una instrucción SQL CREATE INDEX completa, con UNIQUE si corresponde.</returns>
        private static string BuildCreateIndex(CreateIndexOp ix)
        {
            var unique = ix.IsUnique ? "UNIQUE " : "";
            return $"CREATE {unique}INDEX {Q(ix.Name)} ON {Q(ix.Schema)}.{Q(ix.Table)} ({string.Join(", ", ix.Columns.Select(Q))});";
        }

        /// <summary>
        /// Construye la instrucción SQL ALTER TABLE ADD CONSTRAINT FOREIGN KEY.
        /// </summary>
        /// <param name="fk">La operación que describe la clave foránea a crear.</param>
        /// <returns>
        /// Una instrucción SQL ALTER TABLE ADD CONSTRAINT FOREIGN KEY completa,
        /// incluyendo las acciones ON DELETE y ON UPDATE si están especificadas.
        /// </returns>
        /// <remarks>
        /// Las acciones referenciales se convierten usando <see cref="ToSqlAction"/>.
        /// Esta instrucción se genera al final del proceso de migración para evitar problemas de dependencias.
        /// </remarks>
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

        /// <summary>
        /// Convierte una acción referencial de <see cref="ReferentialAction"/> a su equivalente SQL Server.
        /// </summary>
        /// <param name="a">La acción referencial a convertir.</param>
        /// <param name="kind">El tipo de acción: "DELETE" o "UPDATE".</param>
        /// <returns>
        /// Una cadena con la cláusula SQL correspondiente (ON DELETE/UPDATE CASCADE, SET NULL, etc.),
        /// o una cadena vacía si la acción es <see cref="ReferentialAction.NoAction"/>.
        /// </returns>
        /// <remarks>
        /// <see cref="ReferentialAction.Restrict"/> se mapea a "NO ACTION" en SQL Server.
        /// </remarks>
        private static string ToSqlAction(ReferentialAction a, string kind) => a switch
        {
            ReferentialAction.Cascade => $"ON {kind} CASCADE",
            ReferentialAction.SetNull => $"ON {kind} SET NULL",
            ReferentialAction.SetDefault => $"ON {kind} SET DEFAULT",
            ReferentialAction.Restrict => $"ON {kind} NO ACTION",
            _ => ""
        };

        /// <summary>
        /// Mapea un tipo de columna abstracto <see cref="ColumnType"/> a su equivalente SQL Server.
        /// </summary>
        /// <param name="t">El tipo de columna a mapear.</param>
        /// <returns>
        /// Una cadena con el tipo de datos SQL Server correspondiente, incluyendo precisión y escala
        /// para tipos como DECIMAL, o longitud máxima para tipos como NVARCHAR.
        /// </returns>
        /// <remarks>
        /// <para>Mapeos principales:</para>
        /// <list type="bullet">
        /// <item><see cref="Int32Type"/> → INT</item>
        /// <item><see cref="Int64Type"/> → BIGINT</item>
        /// <item><see cref="BoolType"/> → BIT</item>
        /// <item><see cref="DateTimeType"/> → DATETIME2</item>
        /// <item><see cref="DateTimeOffsetType"/> → DATETIMEOFFSET</item>
        /// <item><see cref="GuidType"/> → UNIQUEIDENTIFIER</item>
        /// <item><see cref="DecimalType"/> → DECIMAL(precisión, escala)</item>
        /// <item><see cref="DoubleType"/> → FLOAT</item>
        /// <item><see cref="StringType"/> → NVARCHAR(n) o NVARCHAR(MAX)</item>
        /// <item><see cref="BytesType"/> → VARBINARY(n) o VARBINARY(MAX)</item>
        /// <item><see cref="JsonType"/> → NVARCHAR(MAX)</item>
        /// </list>
        /// <para>Los tipos desconocidos se mapean por defecto a NVARCHAR(MAX).</para>
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
            JsonType => "NVARCHAR(MAX)",
            _ => "NVARCHAR(MAX)"
        };

        /// <summary>
        /// Escapa y delimita un identificador SQL (nombre de tabla, columna, esquema, etc.) con corchetes.
        /// </summary>
        /// <param name="name">El nombre del identificador a escapar.</param>
        /// <returns>
        /// El nombre delimitado con corchetes, donde cualquier corchete de cierre dentro del nombre
        /// se duplica para escaparlo correctamente.
        /// </returns>
        /// <example>
        /// <code>
        /// Q("MyTable") → "[MyTable]"
        /// Q("My]Table") → "[My]]Table]"
        /// </code>
        /// </example>
        private static string Q(string name) => $"[{name.Replace("]", "]]")}]";

        /// <summary>
        /// Formatea un valor por defecto de columna para su uso en SQL.
        /// </summary>
        /// <param name="value">El valor por defecto a formatear.</param>
        /// <returns>
        /// Una representación en cadena del valor adecuada para usar en una cláusula DEFAULT de SQL Server.
        /// </returns>
        /// <remarks>
        /// <para>Conversiones de tipo:</para>
        /// <list type="bullet">
        /// <item><c>bool</c> → "1" o "0"</item>
        /// <item><c>string</c> → N'cadena' (con comillas simples escapadas)</item>
        /// <item><c>int</c>, <c>long</c> → representación numérica</item>
        /// <item><c>decimal</c>, <c>double</c> → representación con cultura invariante</item>
        /// <item><c>DateTime</c> → 'yyyy-MM-ddTHH:mm:ss.fffffff'</item>
        /// <item>Otros tipos → N'ToString()' (con comillas simples escapadas)</item>
        /// </list>
        /// </remarks>
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