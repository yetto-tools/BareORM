using BareORM.Migrations.Abstractions;
using BareORM.Schema;
using global::BareORM.Schema.Types;

namespace BareORM.samples.TestAlfaMigrations
{

    

    public sealed class _20251221_000001_CreateUsers : Migration
    {
        public override string Id => "20251221_000001";
        public override string Name => "CreateUsers";

        public override void Up(MigrationBuilder mb)
        {
            var t = mb.CreateTable("dbo", "Users");

            t.Columns.Add(new AddColumnOp("dbo", "Users", "UserId", new Int32Type())
            {
                IsNullable = false,
                IsIncrementalKey = true
            });

            t.Columns.Add(new AddColumnOp("dbo", "Users", "Email", new StringType(320))
            {
                IsNullable = false
            });

            t.Columns.Add(new AddColumnOp("dbo", "Users", "DisplayName", new StringType(200))
            {
                IsNullable = false
            });

            t.Columns.Add(new AddColumnOp("dbo", "Users", "IsActive", new BoolType())
            {
                IsNullable = false,
                DefaultValue = true
            });

            t.Columns.Add(new AddColumnOp("dbo", "Users", "CreatedAt", new DateTimeType())
            {
                IsNullable = false,
                // Para SQL Server: el generator actual soporta default literal; luego mejoramos a SQL default (SYSUTCDATETIME)
            });

            t.PrimaryKey = new AddPrimaryKeyOp("dbo", "Users", "PK_Users", new[] { "UserId" });

            // Unique Email
            t.Uniques.Add(new AddUniqueOp("dbo", "Users", "UQ_Users_Email", new[] { "Email" }));
        }

        public override void Down(MigrationBuilder mb)
        {
            mb.DropTable("dbo", "Users");
        }
    }

}
