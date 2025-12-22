using System.Data;
using System.Data.Common;
using BareORM.Abstractions;

namespace BareORM.test
{


    public sealed class FakeCommandFactory : ICommandFactory
    {
        public CommandDefinition? LastCreated { get; private set; }

        public CommandDefinition Create(string commandText, CommandType commandType = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = new CommandDefinition(commandText, commandType, null, timeoutSeconds);
            LastCreated = cmd;
            return cmd;
        }

        public CommandDefinition Create(string commandText, CommandType commandType, IEnumerable<DbParam> parameters, int timeoutSeconds = 30)
        {
            var cmd = new CommandDefinition(commandText, commandType, null, timeoutSeconds);
            LastCreated = cmd;
            return cmd;
        }

        public IReadOnlyList<DbParameter> CreateParameters(object parameters) => Array.Empty<DbParameter>();
        public IReadOnlyList<DbParameter> CreateParameters(IEnumerable<DbParam> parameters) => Array.Empty<DbParameter>();
        public DbParameter CreateParameter(DbParam param) => throw new NotImplementedException();
    }

}
