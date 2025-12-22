using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace BareORM.Advanced.Graph
{
    internal static class KeySelectorCache
    {
        private static readonly ConcurrentDictionary<string, Delegate> _cache = new();

        public static Func<T, TKey> Get<T, TKey>(Expression<Func<T, TKey>> expr)
        {
            var key = $"{typeof(T).FullName}|{typeof(TKey).FullName}|{expr}";
            return (Func<T, TKey>)_cache.GetOrAdd(key, _ => expr.Compile());
        }
    }
}
