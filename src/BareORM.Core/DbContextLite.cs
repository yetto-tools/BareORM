using System.Data;
using BareORM.Abstractions;


namespace BareORM.Core
{
    public class DbContextLite
    {
        protected readonly IDbExecutor Executor;
        protected readonly ICommandFactory CommandFactory;
        protected readonly ITransactionManager? Tx;
        protected readonly IBulkProvider? Bulk;
        protected readonly ICommandObserver? Observer;

        public DbContextLite(
            IDbExecutor executor,
            ICommandFactory commandFactory,
            ITransactionManager? transactionManager = null,
            IBulkProvider? bulkProvider = null,
            ICommandObserver? observer = null)
        {
            Executor = executor;
            CommandFactory = commandFactory;
            Tx = transactionManager;
            Bulk = bulkProvider;
            Observer = observer;
        }

        // -----------------------
        // Execute (non-query)
        // -----------------------
        public int Execute(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
            => ExecuteInternal(spOrSql, type, parameters, timeoutSeconds);

        public Task<int> ExecuteAsync(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30, CancellationToken ct = default)
            => ExecuteInternalAsync(spOrSql, type, parameters, timeoutSeconds, ct);

        // -----------------------
        // Scalar
        // -----------------------
        public T? ExecuteScalar<T>(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ConvertScalar<T>(Executor.ExecuteScalar(cmd));
        }

        public async Task<T?> ExecuteScalarAsync<T>(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30, CancellationToken ct = default)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            var value = await Executor.ExecuteScalarAsync(cmd, ct);
            return ConvertScalar<T>(value);
        }

        // -----------------------
        // DataTable / DataSet
        // -----------------------
        public DataTable ExecuteDataTable(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.ExecuteDataTable(cmd), cmd);
        }

        public DataSet ExecuteDataSet(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.ExecuteDataSet(cmd), cmd);
        }

        // -----------------------
        // WithMeta (outputs/return-values)
        // -----------------------
        public DbResult ExecuteWithMeta(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.ExecuteWithMeta(cmd), cmd);
        }

        public Task<DbResult> ExecuteWithMetaAsync(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30, CancellationToken ct = default)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObservedAsync(() => Executor.ExecuteWithMetaAsync(cmd, ct), cmd);
        }

        public DbResult<DataTable> ExecuteDataTableWithMeta(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.ExecuteDataTableWithMeta(cmd), cmd);
        }

        public DbResult<DataSet> ExecuteDataSetWithMeta(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.ExecuteDataSetWithMeta(cmd), cmd);
        }

        // -----------------------
        // Reader + Multi-result
        // -----------------------
        public DbResult<IDataReader> ExecuteReaderWithMeta(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30, CommandBehavior behavior = CommandBehavior.Default)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            // Si querés forzar behavior: crea CommandDefinition con ReaderBehavior desde factory (cuando el factory lo soporte).
            return ExecuteObserved(() => Executor.ExecuteReaderWithMeta(cmd), cmd);
        }

        public MultiResultReader ExecuteMultiple(string spOrSql, CommandType type = CommandType.StoredProcedure, object? parameters = null, int timeoutSeconds = 30)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            var result = ExecuteObserved(() => Executor.ExecuteReaderWithMeta(cmd), cmd);
            return new MultiResultReader(result);
        }

        // -----------------------
        // Transaction
        // -----------------------
        public void BeginTransaction(IsolationLevel level = IsolationLevel.ReadCommitted)
        {
            if (Tx == null) throw new InvalidOperationException("Transaction manager is not configured.");
            Tx.Begin(level);
        }

        public void Commit()
        {
            if (Tx == null) throw new InvalidOperationException("Transaction manager is not configured.");
            Tx.Commit();
        }

        public void Rollback()
        {
            if (Tx == null) throw new InvalidOperationException("Transaction manager is not configured.");
            Tx.Rollback();
        }

        // -----------------------
        // Bulk
        // -----------------------
        public void BulkInsert(string table, DataTable data, BulkOptions? options = null)
        {
            if (Bulk == null) throw new InvalidOperationException("Bulk provider is not configured.");
            Bulk.BulkInsert(table, data, options);
        }

        public Task BulkInsertAsync(string table, DataTable data, BulkOptions? options = null, CancellationToken ct = default)
        {
            if (Bulk == null) throw new InvalidOperationException("Bulk provider is not configured.");
            return Bulk.BulkInsertAsync(table, data, options, ct);
        }

        // -----------------------
        // Internals
        // -----------------------
        private int ExecuteInternal(string spOrSql, CommandType type, object? parameters, int timeoutSeconds)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObserved(() => Executor.Execute(cmd), cmd);
        }

        private Task<int> ExecuteInternalAsync(string spOrSql, CommandType type, object? parameters, int timeoutSeconds, CancellationToken ct)
        {
            var cmd = CommandFactory.Create(spOrSql, type, parameters, timeoutSeconds);
            return ExecuteObservedAsync(() => Executor.ExecuteAsync(cmd, ct), cmd);
        }

        private TReturn ExecuteObserved<TReturn>(Func<TReturn> fn, CommandDefinition cmd)
        {
            try
            {
                Observer?.OnExecuting(cmd);
                var start = DateTime.UtcNow;
                var result = fn();
                Observer?.OnExecuted(cmd, DateTime.UtcNow - start);
                return result;
            }
            catch (Exception ex)
            {
                Observer?.OnError(cmd, ex);
                throw;
            }
        }

        private async Task<TReturn> ExecuteObservedAsync<TReturn>(Func<Task<TReturn>> fn, CommandDefinition cmd)
        {
            try
            {
                Observer?.OnExecuting(cmd);
                var start = DateTime.UtcNow;
                var result = await fn();
                Observer?.OnExecuted(cmd, DateTime.UtcNow - start);
                return result;
            }
            catch (Exception ex)
            {
                Observer?.OnError(cmd, ex);
                throw;
            }
        }

        private static T? ConvertScalar<T>(object? value)
        {
            if (value == null || value is DBNull) return default;
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
