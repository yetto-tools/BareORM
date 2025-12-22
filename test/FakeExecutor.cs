using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.test
{
    public sealed class FakeExecutor : IDbExecutor
    {
        public CommandDefinition? LastCommand { get; private set; }
        public int ExecuteCalls { get; private set; }

        public DataTable DataTableToReturn { get; set; } = new DataTable();
        public object? ScalarToReturn { get; set; }
        public DbResult MetaToReturn { get; set; } = new DbResult(1, new Dictionary<string, object?>());

        public int Execute(CommandDefinition command)
        {
            LastCommand = command;
            ExecuteCalls++;
            return 1;
        }

        public Task<int> ExecuteAsync(CommandDefinition command, CancellationToken ct = default)
            => Task.FromResult(Execute(command));

        public object? ExecuteScalar(CommandDefinition command)
        {
            LastCommand = command;
            return ScalarToReturn;
        }

        public Task<object?> ExecuteScalarAsync(CommandDefinition command, CancellationToken ct = default)
            => Task.FromResult(ExecuteScalar(command));

        public IDataReader ExecuteReader(CommandDefinition command) => throw new NotImplementedException();
        public Task<IDataReader> ExecuteReaderAsync(CommandDefinition command, CancellationToken ct = default) => throw new NotImplementedException();

        public DataTable ExecuteDataTable(CommandDefinition command)
        {
            LastCommand = command;
            return DataTableToReturn;
        }

        public DataSet ExecuteDataSet(CommandDefinition command)
        {
            LastCommand = command;
            return new DataSet();
        }

        public DbResult ExecuteWithMeta(CommandDefinition command) { LastCommand = command; return MetaToReturn; }
        public Task<DbResult> ExecuteWithMetaAsync(CommandDefinition command, CancellationToken ct = default) => Task.FromResult(ExecuteWithMeta(command));

        public DbResult<T?> ExecuteScalarWithMeta<T>(CommandDefinition command)
        {
            LastCommand = command;
            var v = ScalarToReturn;
            var typed = (v == null || v is DBNull) ? default : (T)Convert.ChangeType(v, typeof(T));
            return new DbResult<T?>(typed, 1, MetaToReturn.OutputValues);
        }

        public Task<DbResult<T?>> ExecuteScalarWithMetaAsync<T>(CommandDefinition command, CancellationToken ct = default)
            => Task.FromResult(ExecuteScalarWithMeta<T>(command));

        public DbResult<DataTable> ExecuteDataTableWithMeta(CommandDefinition command)
        {
            LastCommand = command;
            return new DbResult<DataTable>(DataTableToReturn, 1, MetaToReturn.OutputValues);
        }

        public DbResult<DataSet> ExecuteDataSetWithMeta(CommandDefinition command)
        {
            LastCommand = command;
            return new DbResult<DataSet>(new DataSet(), 1, MetaToReturn.OutputValues);
        }

        public DbResult<IDataReader> ExecuteReaderWithMeta(CommandDefinition command) => throw new NotImplementedException();
        public Task<DbResult<IDataReader>> ExecuteReaderWithMetaAsync(CommandDefinition command, CancellationToken ct = default) => throw new NotImplementedException();

        public void Dispose() { }
    }
}
