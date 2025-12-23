using BareORM.Abstractions;
using BareORM.Migrations.Abstractions;
using BareORM.Schema;
using global::BareORM.Annotations;
using global::BareORM.Migrations.Abstractions;
using global::BareORM.Schema.Types;

namespace BareORM.samples.TestAlfaMigrations
{
    

    

    public sealed class _20251221_000002_CreateOrders : Migration
    {
        public override string Id => "20251221_000002";
        public override string Name => "CreateOrders";

        public override void Up(MigrationBuilder mb)
        {
            var t = mb.CreateTable("dbo", "Orders");

            t.Columns.Add(new AddColumnOp("dbo", "Orders", "OrderId", new Int32Type())
            {
                IsNullable = false,
                IsIncrementalKey = true
            });

            t.Columns.Add(new AddColumnOp("dbo", "Orders", "UserId", new Int32Type())
            {
                IsNullable = false
            });

            t.Columns.Add(new AddColumnOp("dbo", "Orders", "Total", new DecimalType(18, 2))
            {
                IsNullable = false,
                DefaultValue = 0m
            });

            t.PrimaryKey = new AddPrimaryKeyOp("dbo", "Orders", "PK_Orders", new[] { "OrderId" });

            // Index por UserId
            t.Indexes.Add(new CreateIndexOp("dbo", "Orders", "IX_Orders_UserId", new[] { "UserId" }, IsUnique: false));

            // FK Orders(UserId) -> Users(UserId)
            t.ForeignKeys.Add(new AddForeignKeyOp(
                "dbo", "Orders", "FK_Orders_Users_UserId",
                new[] { "UserId" },
                "dbo", "Users",
                new[] { "UserId" },
                ReferentialAction.Cascade,
                ReferentialAction.NoAction
            ));
        }

        public override void Down(MigrationBuilder mb)
        {
            mb.DropTable("dbo", "Orders");
        }
    }

}
