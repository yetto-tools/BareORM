using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Migrations.Abstractions.Interfaces
{

    public interface IMigrationLock : IDisposable { }

    public interface IMigrationLockProvider
    {
        IMigrationLock Acquire(string scope);
    }
}