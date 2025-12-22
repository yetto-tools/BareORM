using BareORM.Migrations.Migrations;
using BareORM.SqlServer;
using BareORM.SqlServer.Migrations;

public static class EjemploMigracionSqlServer
{
    public static void EjemploMigracion()
    {

        // connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=DB_TestMigration;Integrated Security=True;Encrypt=True;Trust Server Certificate=True;";

        var connectionString = "Data Source=(localdb)\\ProjectModels;Initial Catalog=BenchDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False;Command Timeout=30";
        var connFactory = new SqlServerConnectionFactory(connectionString);



        SqlServerDatabaseBootstrap.EnsureDatabaseExists(connectionString);

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

        // Tus migraciones (ejemplo)
        var migrations = new BareORM.Migrations.Abstractions.Migration[]
        {
            new _20251221_000001_CreateUsers(),
            new _20251221_000002_CreateOrders()
        };

        migrator.Migrate(migrations);

        Console.WriteLine("✅ Migraciones aplicadas.");
        Console.ReadLine();


    }
}