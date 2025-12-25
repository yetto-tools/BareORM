using System.Data;
using BareORM.Abstractions;
using BareORM.Core;
using BareORM.samples.Models;
using BareORM.Schema.Builders;
using BareORM.SqlServer;
using BareORM.SqlServer.Migrations;
using BareORM.SqlServer.Schema;

namespace BareORM.samples
{
    public static class MigrationExample
    {
        public static void Run()
        {
            var connectionString =
              "Data Source=(localdb)\\ProjectModels;Initial Catalog=DB_TestMigration;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;Command Timeout=30";
            
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


            var connFactory = new SqlServerConnectionFactory(connectionString);

            var executor = new SqlServerExecutor(connFactory);
            var cmdFactory = new SqlServerCommandFactory();
            var tx = new SqlServerTransactionManager(connFactory);
            var bulk = new SqlServerBulkProvider(connFactory);

            ICommandObserver observer = new ConsoleObserver();
            var db = new DbContextLite(executor, cmdFactory, tx, bulk, observer);

            // 1) Construir schema model desde entidades
            var builder = new SchemaModelBuilder(new SchemaModelBuilderOptions
            {
                DefaultSchema = "dbo",
                RequireTableAttribute = false
            });

            var model = builder.Build(typeof(User), typeof(Order), typeof(OrderItem));

            // 2) Generar DDL SQL Server
            var ddl = new SqlServerDdlGenerator().Generate(model);

            Console.WriteLine($"DDL batches: {ddl.Count}");

            // 3) Ejecutar DDL en SQL Server (idealmente en transacción)
            db.BeginTransaction();
            try
            {
                foreach (var sql in ddl)
                {
                    // Ejecuta texto
                    db.Execute(sql, CommandType.Text, parameters: null, timeoutSeconds: 120);
                }

                db.Commit();
                Console.WriteLine("Schema aplicado correctamente.");
            }
            catch (Exception ex)
            {
                db.Rollback();
                Console.WriteLine("❌ Error aplicando schema:");
                Console.WriteLine(ex);
            }
        }
    }
}
