using BareORM.Abstractions;
using BareORM.Schema.Types;

namespace BareORM.Migrations.Abstractions
{
    public abstract record MigrationOperation;
    public record SqlOp(string Sql) : MigrationOperation;

}
