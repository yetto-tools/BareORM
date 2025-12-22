using System.Threading;
using Microsoft.Data.SqlClient;

namespace BareORM.SqlServer.Internal
{
    internal sealed class SqlServerAmbientContext
    {
        public required SqlConnection Connection { get; init; }
        public required SqlTransaction Transaction { get; init; }
    }

    internal static class SqlServerAmbient
    {
        private static readonly AsyncLocal<SqlServerAmbientContext?> _current = new();

        public static SqlServerAmbientContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}