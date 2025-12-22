using System.Data;
using BareORM.Core;
using Xunit;


namespace BareORM.test
{
 
    public class DbContextLiteTests
    {
        [Fact]
        public void ExecuteDataTable_CreatesCommand_WithExpectedValues()
        {
            var exec = new FakeExecutor();
            var factory = new FakeCommandFactory();
            var db = new DbContextLite(exec, factory);

            db.ExecuteDataSetWithMeta("spTest", CommandType.StoredProcedure, new { Id = 1 }, timeoutSeconds: 45);

            Assert.NotNull(factory.LastCreated);
            Assert.Equal("spTest", factory.LastCreated!.CommandText);
            Assert.Equal(CommandType.StoredProcedure, factory.LastCreated!.CommandType);
            Assert.Equal(45, factory.LastCreated!.TimeoutSeconds);
        }
    }
}
