using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

using BareORM.Migrations.Abstractions;
using BareORM.Migrations.DbAssets;
using BareORM.Migrations.Diff;
using BareORM.Migrations.Migrations;
using BareORM.Migrations.Scaffold;
using BareORM.Migrations.Snapshot;
using BareORM.Migrations.Tooling;

using BareORM.SqlServer.Migrations;

/*
 *  
 *  COMANDO DE COMPILACION:
 *      dotnet pack tools\BareORM.Tools.Cli.csproj -c Release -o tools\nupkgs
 *      
 *  
 *  BORRAR CUALQUIER ANTIGUA INSTALACIÓN LOCAL PRIMERO:
 *      dotnet nuget locals all --clear
 *      
 *      
 *  INSTALAR EN LOCAL:
 *          
 *      dotnet tool install --local --add-source tools/nupkgs BareORM.Tools
 *      
 *  VERIFICAR INSTALACIÓN:
 *      dotnet tool list --local
 *  
 *  
 *  VERIFICAR OPCIONES:
 *      bare-tools --help
 * 
 * 
 *  UNINSTALL:
 *      dotnet tool uninstall --local BareORM.Tools
 *      
 *  LIMPIAR COMPILACION:
 *      dotnet clean tools\BareORM.Tools.Cli.csproj -c Release

 * 
 */


namespace BareORM.Tools.Cli
{
    static class Program
    {
    static int Main(string[] args)
        {
            if (args.Length == 0) return Help();

            var cmd = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            try
            {
                return cmd switch
                {
                    "init"  => CmdInit(rest),
                    "prog"  => CmdProg(rest),
                    "db"    => CmdDb(rest),
                    "help" or "--help" or "-h" => Help(),
                    _       => Help($"Comando desconocido: {cmd}")
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        static int CmdInit(string[] args)
        {
            // barerom init --root <path>
            var root = GetOpt(args, "--root") ?? Directory.GetCurrentDirectory();

            var layout = ProjectLayout.Ensure(root);
            Console.WriteLine("✅ BareORM init listo:");
            Console.WriteLine($"   Root:      {layout.ProjectRoot}");
            Console.WriteLine($"   DbAssets:  {layout.AssetsRoot}");
            Console.WriteLine($"   Migrations:{layout.MigrationsRoot}");
            Console.WriteLine($"   Snapshot:  {layout.SnapshotPath}");
            Console.WriteLine($"   Manifest:  {layout.ManifestPath}");
            return 0;
        }

        static int CmdProg(string[] args)
        {
            // barerom prog add --root <path> --name <MigrationName>
            if (args.Length == 0) return Help("Falta subcomando: prog add");

            var sub = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            return sub switch
            {
                "add" => CmdProgAdd(rest),
                _ => Help($"Subcomando prog desconocido: {sub}")
            };
        }

        static int CmdProgAdd(string[] args)
        {
            var root        = GetOpt(args, "--root") ?? Directory.GetCurrentDirectory();
            var name        = GetOpt(args, "--name") ?? "Programmables_Update";

            // Ensure layout (crea carpetas si no existen)
            var layout      = ProjectLayout.Ensure(root);

            // Leer assets reales
            var provider    = new FileSystemAssetProvider(layout.AssetsRoot);
            var assets      = provider.GetAssets().ToList();

            Console.WriteLine($"Assets encontrados: {assets.Count}");
            foreach (var a in assets)
                Console.WriteLine($"- {a.Kind} {a.Schema}.{a.Name}");

            // Load snapshot viejo
            var snapStore = new JsonProgrammablesSnapshotStore();
            var oldSnap = snapStore.Load(layout.SnapshotPath);

            // Diff
            var differ = new ProgrammablesDiffer();
            var ops = differ.Diff(oldSnap, assets);

            if (ops.Count == 0)
            {
                Console.WriteLine("No hay cambios en programmables. (Snapshot = actual)");
                return 0;
            }

            // Scaffold .cs migration
            var scaffolder = new ProgrammablesMigrationScaffolder();
            var filePath = scaffolder.ScaffoldToFile(layout.MigrationsRoot, name, ops);

            // Save snapshot nuevo
            var newSnap = snapStore.BuildFromProviders(new[] { provider });
            snapStore.Save(layout.SnapshotPath, newSnap);

            // Append manifest local (tooling)
            var file = Path.GetFileNameWithoutExtension(filePath);

            var id = file.Split('_').Length >= 2 ? string.Join("_", file.Split('_').Take(2)) : file;

            ManifestWriter.Append(layout.ManifestPath, migrationId: id, migrationName: name, opsCount: ops.Count, filePath: filePath);

            Console.WriteLine($"✅ Migración generada: {filePath}");
            Console.WriteLine($"✅ Snapshot actualizado: {layout.SnapshotPath}");
            Console.WriteLine($"✅ Manifest actualizado: {layout.ManifestPath}");
            Console.WriteLine($"Ops: {ops.Count}");
            Console.WriteLine("⚠️  Ahora: build del proyecto para que Update Database encuentre la migración.");
            return 0;
        }

        static int CmdDb(string[] args)
        {
            // barerom db update --project <csproj> --conn "<cs>" [--config Debug|Release]
            if (args.Length == 0) return Help("Falta subcomando: db update");

            var sub = args[0].ToLowerInvariant();
            var rest = args.Skip(1).ToArray();

            return sub switch
            {
                "update" => CmdDbUpdate(rest),
                _ => Help($"Subcomando db desconocido: {sub}")
            };
        }

        static int CmdDbUpdate(string[] args)
        {
            var project     = GetOpt(args, "--project");
            var conn        = GetOpt(args, "--conn");
            var config      = GetOpt(args, "--config") ?? "Debug";

            if (string.IsNullOrWhiteSpace(project))
                return Help("db update requiere --project <ruta.csproj>");

            if (string.IsNullOrWhiteSpace(conn))
                return Help("db update requiere --conn \"<connectionString>\"");

            project = Path.GetFullPath(project);

        #region 1) Build
            
            Run("dotnet", $"build \"{project}\" -c {config}");
            
        #endregion 


        #region 2) Encontrar dll del proyecto
            
            var dll = ResolveProjectDll(project, config);
            Console.WriteLine($"Assembly: {dll}");
            
        #endregion 
            
            
        #region 3) Bootstrap DB (best effort, no asume master)
            
            var ensure = SqlServerDatabaseBootstrap.TryEnsureDatabaseExists(conn);
            Console.WriteLine($"DB Ensure: {ensure.Status} ({ensure.Database})");
            if (ensure.Status is DatabaseEnsureStatus.SkippedNoMasterAccess or DatabaseEnsureStatus.SkippedNoCreatePermission)
                Console.WriteLine($"⚠️  Nota: no se pudo crear automáticamente. {ensure.Error?.Message}");

        #endregion 

        #region 4) Cargar migraciones desde assembly
            var asm             = Assembly.LoadFrom(dll);
            var migrations      = DiscoverMigrations(asm).ToArray();

            Console.WriteLine($"Migraciones encontradas: {migrations.Length}");
            foreach (var m in migrations)
                Console.WriteLine($"- {m.Id} {m.Name}");
        #endregion
        
            
        #region 5) Apply
            using var session   = new SqlServerMigrationSession(conn);
            var sqlGen          = new SqlServerMigrationSqlGenerator();
            var exec            = new SqlServerMigrationExecutor(session);
            var history         = new SqlServerMigrationHistoryRepository(session);
            var locker          = new SqlServerMigrationLockProvider(session);

            var migrator = new Migrator(sqlGen, history, locker, exec, new MigratorOptions
            {
                Scope = "BareORM.Migrations",
                ProductVersion = "BareORM-Dev",
                CommandTimeoutSeconds = 120
            });

            migrator.Migrate(migrations);
        #endregion 

            Console.WriteLine("✅ Update Database listo.");
            return 0;
        }

        static IEnumerable<Migration> DiscoverMigrations(Assembly asm)
        {
            return asm.GetTypes()
                .Where  ( t => !t.IsAbstract && typeof(Migration).IsAssignableFrom(t) )
                .Select ( t => (Migration)Activator.CreateInstance(t)!                )
                .OrderBy( m => m.Id, StringComparer.Ordinal                           );
        }

        static string ResolveProjectDll(string csprojPath, string config)
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            string? tfm = doc.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(tfm))
            {
                var tfms = doc.Descendants(ns + "TargetFrameworks").FirstOrDefault()?.Value;
                tfm = tfms?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(tfm))
                throw new InvalidOperationException("No se pudo determinar TargetFramework en el .csproj.");

            var asmName = doc.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(asmName))
                asmName = Path.GetFileNameWithoutExtension(csprojPath);

            var dir = Path.GetDirectoryName(csprojPath)!;
            var dll = Path.Combine(dir, "bin", config, tfm!, $"{asmName}.dll");

            if (!File.Exists(dll))
                throw new FileNotFoundException($"No se encontró DLL compilada: {dll}");

            return dll;
        }

        static void Run(string fileName, string args)
        {
            Console.WriteLine($"> {fileName} {args}");
            using var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false
                }
            };
            p.Start();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"{fileName} falló con exitcode {p.ExitCode}");
        }

        static string? GetOpt(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length) return args[i + 1];
                    return "";
                }
            }
            return null;
        }

        static int Help(string? err = null)
        {
            if (!string.IsNullOrWhiteSpace(err))
                Console.Error.WriteLine(err);

            Console.WriteLine(@"
            BareORM CLI (bare-tools)

            Commands:
              bare-tools init --root <path>
                  Crea DbAssets/ + subcarpetas, Migrations/, snapshot y manifest

              bare-tools prog add --root <path> --name <MigrationName>
                  Diffea DbAssets/*.sql vs snapshot y genera Migration .cs

              bare-tools db update --project <path.csproj> --conn ""<cs>"" [--config Debug|Release]
                  Build del proyecto, descubre migraciones del assembly y las aplica en DB
            ");
            return string.IsNullOrWhiteSpace(err) ? 0 : 1;
        }
    }
}