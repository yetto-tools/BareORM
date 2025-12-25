using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Migrations.Abstractions;

namespace BareORM.Migrations.Scaffold
{
    public sealed class ProgrammablesMigrationScaffolder
    {
        public string ScaffoldToFile(string migrationsFolder, string migrationName, IReadOnlyList<BareORM.Migrations.Abstractions.MigrationOperation> ops)
        {
            Directory.CreateDirectory(migrationsFolder);

            var id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var className = $"_{id}_{Sanitize(migrationName)}";
            var filePath = Path.Combine(migrationsFolder, $"{id}_{Sanitize(migrationName)}.cs");

            var sb = new StringBuilder();
            sb.AppendLine($"//Created at {DateTime.Now}");
            sb.AppendLine("using BareORM.Migrations.Abstractions;");            
            sb.AppendLine();
            sb.AppendLine("namespace BareORM.samples.Migrations;");
            sb.AppendLine();
            sb.AppendLine($"public sealed class {className} : Migration");
            sb.AppendLine("{");
            sb.AppendLine($"    public override string Id => \"{id}\";");
            sb.AppendLine($"    public override string Name => \"{migrationName}\";");
            sb.AppendLine();
            sb.AppendLine("    public override void Up(MigrationBuilder mb)");
            sb.AppendLine("    {");

            foreach (var op in ops)
            {
                switch (op)
                {
                    case CreateOrAlterViewOp v:
                        sb.AppendLine($"        mb.CreateOrAlterView(\"{v.Schema}\", \"{v.Name}\", {ToVerbatim(v.DefinitionSql)});");
                        break;

                    case CreateOrAlterRoutineOp r:
                        if (r.Kind == RoutineKind.Procedure)
                            sb.AppendLine($"        mb.CreateOrAlterProcedure(\"{r.Schema}\", \"{r.Name}\", {ToVerbatim(r.DefinitionSql)});");
                        else if (r.Kind == RoutineKind.ScalarFunction)
                            sb.AppendLine($"        mb.CreateOrAlterScalarFunction(\"{r.Schema}\", \"{r.Name}\", {ToVerbatim(r.DefinitionSql)});");
                        else
                            sb.AppendLine($"        mb.CreateOrAlterTableFunction(\"{r.Schema}\", \"{r.Name}\", {ToVerbatim(r.DefinitionSql)});");
                        break;

                    case CreateOrAlterTriggerOp t:
                        sb.AppendLine($"        mb.CreateOrAlterTrigger(\"{t.Schema}\", \"{t.Name}\", {ToVerbatim(t.DefinitionSql)});");
                        break;

                    // Drops si los habilitas después
                    case DropViewOp dv:
                        sb.AppendLine($"        mb.DropView(\"{dv.Schema}\", \"{dv.Name}\");");
                        break;

                    case DropRoutineOp dr:
                        sb.AppendLine($"        mb.DropRoutine(\"{dr.Schema}\", \"{dr.Name}\", RoutineKind.{dr.Kind});");
                        break;

                    case DropTriggerOp dt:
                        sb.AppendLine($"        mb.DropTrigger(\"{dt.Schema}\", \"{dt.Name}\");");
                        break;
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    public override void Down(MigrationBuilder mb)");
            sb.AppendLine("    {");
            sb.AppendLine("        // MVP: no drops automáticos (seguro).");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        private static string Sanitize(string name)
        {
            var s = new string(name.Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(s) ? "Migration" : s;
        }

        private static string ToVerbatim(string s)
        {
            // @"..." (escapa comillas dobles)
            s = s.Replace("\"", "\"\"");
            return "@\"" + s + "\"";
        }
    }

}
