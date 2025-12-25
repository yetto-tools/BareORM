using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BareORM.Abstractions;
using BareORM.Migrations.Abstractions;
using BareORM.Migrations.Migrations;
using BareORM.SqlServer.Migrations;

namespace BareORM.samples.AddMigration
{
    public static class UpdateDatabase
    {
        public static void Run()
        {
            var connectionString =
              "Data Source=(localdb)\\ProjectModels;Initial Catalog=DB_TestMigration;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;";

            var result =  SqlServerDatabaseBootstrap.TryEnsureDatabaseExists(connectionString);

            if (result.Status is DatabaseEnsureStatus.SkippedNoMasterAccess or DatabaseEnsureStatus.SkippedNoCreatePermission)
            {
                Console.WriteLine($"No pude crear/verificar la DB '{result.Database}'. Status={result.Status}");
                Console.WriteLine(result.Error?.Message);

                // Aquí decides: abortar o continuar (pero abrir destino probablemente fallará)
                // En PROD normalmente abortás con mensaje claro.
                return;
            }

            if (result.Status == DatabaseEnsureStatus.Failed)
            {
                Console.WriteLine($"Bootstrap falló: {result.Error?.Message}");
                throw result.Error!;
            }


            using var session = new SqlServerMigrationSession(connectionString);

            var sqlGen = new SqlServerMigrationSqlGenerator();
            var exec = new SqlServerMigrationExecutor(session);
            var history = new SqlServerMigrationHistoryRepository(session);
            var locker = new SqlServerMigrationLockProvider(session);

            var migrator = new Migrator(sqlGen, history, locker, exec, new MigratorOptions
            {
                Scope = "BareORM.Migrations",
                ProductVersion = "BareORM-Dev",
                CommandTimeoutSeconds = 120
            });

            // Cargar migraciones del assembly del sample (donde se generan .cs)
            var migrations = DiscoverMigrations(Assembly.GetExecutingAssembly());

            migrator.Migrate(migrations);

            Console.WriteLine("Update Database listo.");
        }

        private static IEnumerable<Migration> DiscoverMigrations(Assembly asm)
        {
            return asm.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Migration).IsAssignableFrom(t))
                .Select(t => (Migration)Activator.CreateInstance(t)!)
                .OrderBy(m => m.Id, StringComparer.Ordinal);
        }
    }
}
