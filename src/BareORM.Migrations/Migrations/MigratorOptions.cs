

namespace BareORM.Migrations.Migrations
{
    public sealed class MigratorOptions
    {
        public string Scope { get; init; } = "BareORM.Migrations";
        public string ProductVersion { get; init; } = "BareORM";
        public int CommandTimeoutSeconds { get; init; } = 120;
    }
}
