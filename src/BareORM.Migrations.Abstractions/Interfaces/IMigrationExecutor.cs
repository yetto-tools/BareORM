using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Migrations.Abstractions.Interfaces
{
    public interface IMigrationExecutor
    {
        void ExecuteBatch(string sql, int timeoutSeconds = 120);
    }
}
