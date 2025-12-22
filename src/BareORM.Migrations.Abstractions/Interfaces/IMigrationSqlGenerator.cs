using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Migrations.Abstractions.Interfaces
{
    public interface IMigrationSqlGenerator
    {
        /// <summary>
        /// Convierte operaciones agnósticas en batches SQL ejecutables para el motor actual.
        /// </summary>
        IReadOnlyList<string> Generate(IReadOnlyList<MigrationOperation> operations);
    }
}
