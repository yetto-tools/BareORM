using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Migrations.Abstractions.Interfaces
{
    public interface IMigrationHistoryRepository
    {
        void EnsureCreated();
        IReadOnlySet<string> GetAppliedMigrationIds();
        void Insert(string migrationId, string name, string productVersion, DateTime appliedAtUtc);
    }
}
