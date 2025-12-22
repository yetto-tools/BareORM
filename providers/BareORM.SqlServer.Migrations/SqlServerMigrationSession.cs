using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.SqlClient;

namespace BareORM.SqlServer.Migrations
{
    public sealed class SqlServerMigrationSession : IDisposable
    {
        public SqlConnection Connection { get; }
        public SqlTransaction? Transaction { get; private set; }

        public SqlServerMigrationSession(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public void BeginTransaction()
        {
            if (Transaction is not null) return;
            Transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            Transaction?.Commit();
            Transaction?.Dispose();
            Transaction = null;
        }

        public void Rollback()
        {
            try { Transaction?.Rollback(); } catch { /* swallow */ }
            Transaction?.Dispose();
            Transaction = null;
        }

        public int ExecuteNonQuery(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        public object? ExecuteScalar(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        public List<string> QueryStrings(string sql, int timeoutSeconds = 120)
        {
            using var cmd = Connection.CreateCommand();
            cmd.Transaction = Transaction;
            cmd.CommandTimeout = timeoutSeconds;
            cmd.CommandText = sql;

            using var r = cmd.ExecuteReader();
            var list = new List<string>();
            while (r.Read())
                list.Add(r.GetString(0));

            return list;
        }

        public void Dispose()
        {
            Rollback();
            Connection.Dispose();
        }
    }
}
