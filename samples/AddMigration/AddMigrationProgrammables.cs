using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Migrations.DbAssets;
using BareORM.Migrations.Diff;
using BareORM.Migrations.Scaffold;
using BareORM.Migrations.Snapshot;

namespace BareORM.samples.AddMigration
{
    public static class AddMigrationProgrammables
    {
        public static void Run()
        {
            var assetsRoot = "DbAssets";
            var migrationsFolder = "Migrations";
            var snapshotPath = Path.Combine(migrationsFolder, "programmables.snapshot.json");

            var provider = new FileSystemAssetProvider(assetsRoot);
            var assets = provider.GetAssets().ToList();

            var snapStore = new JsonProgrammablesSnapshotStore();
            var oldSnap = snapStore.Load(snapshotPath);

            var differ = new ProgrammablesDiffer();
            var ops = differ.Diff(oldSnap, assets);

            if (ops.Count == 0)
            {
                Console.WriteLine("No hay cambios en programmables. (Snapshot = actual)");
                return;
            }

            var scaffolder = new ProgrammablesMigrationScaffolder();
            var file = scaffolder.ScaffoldToFile(migrationsFolder, "Programmables_Update", ops);

            // guardar snapshot nuevo
            var newSnap = snapStore.BuildFromProviders(new[] { provider });
            snapStore.Save(snapshotPath, newSnap);

            Console.WriteLine($"Migración generada: {file}");
            Console.WriteLine($"Snapshot actualizado: {snapshotPath}");
            Console.WriteLine($"Ops generadas: {ops.Count}");
        }
    }
}
