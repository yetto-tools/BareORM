namespace BareORM.Migrations.Abstractions
{
    public abstract class Migration
    {
        /// <summary>Ordenable y único. Ej: "20251221_153501"</summary>
        public abstract string Id { get; }

        /// <summary>Nombre humano. Ej: "CreateUsers"</summary>
        public abstract string Name { get; }

        public abstract void Up(MigrationBuilder mb);
        public abstract void Down(MigrationBuilder mb);
    }
}
