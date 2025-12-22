using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Abstractions;

namespace BareORM.Advanced.MultiResult
{
    public interface IMultiResult : IDisposable
    {
        IReadOnlyList<T> Read<T>(IEntityMapper<T>? mapper = null) where T : new();
        T? ReadSingle<T>(IEntityMapper<T>? mapper = null) where T : new();
        bool NextResult();
    }

}
