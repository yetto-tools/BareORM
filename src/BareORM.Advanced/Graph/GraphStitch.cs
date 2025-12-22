using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace BareORM.Advanced.Graph
{
    /// <summary>
    /// Stitching rápido One-to-Many / Many-to-One sin tracking.
    /// Respeta el orden en el que vienen los padres desde SQL.
    /// </summary>
    public static class GraphStitch
    {
        // -------- Key selector cache (compiled expressions) --------
        private static readonly ConcurrentDictionary<string, Delegate> _keyCache = new();

        public static Func<T, TKey> Key<T, TKey>(Expression<Func<T, TKey>> expr)
        {
            var key = $"{typeof(T).FullName}|{typeof(TKey).FullName}|{expr}";
            return (Func<T, TKey>)_keyCache.GetOrAdd(key, _ => expr.Compile());
        }

        /// <summary>
        /// One-to-many: pega hijos en el padre.
        /// Respeta el orden de padres tal como vino en SQL.
        /// </summary>
        public static void OneToMany<TParent, TChild, TKey>(
            IReadOnlyList<TParent> parents,
            IReadOnlyList<TChild> children,
            Expression<Func<TParent, TKey>> parentKey,
            Expression<Func<TChild, TKey>> childForeignKey,
            Action<TParent, List<TChild>> assign)
            where TKey : notnull
        {
            var pk = Key(parentKey);
            var fk = Key(childForeignKey);

            // Agrupar hijos por FK (preserva el orden relativo de hijos según el SELECT)
            var childMap = new Dictionary<TKey, List<TChild>>();
            foreach (var c in children)
            {
                var k = fk(c);
                if (!childMap.TryGetValue(k, out var list))
                    childMap[k] = list = new List<TChild>();
                list.Add(c);
            }

            // Asignar en el orden original de padres
            foreach (var p in parents)
            {
                var k = pk(p);
                assign(p, childMap.TryGetValue(k, out var list) ? list : new List<TChild>());
            }
        }

        public static void ManyToOne<TChild, TParent, TKey>(
            IReadOnlyList<TChild> children,
            IReadOnlyList<TParent> parents,
            Expression<Func<TChild, TKey>> childForeignKey,
            Expression<Func<TParent, TKey>> parentKey,
            Action<TChild, TParent?> assign)
            where TKey : notnull
        {
            var fk = KeySelectorCache.Get(childForeignKey);
            var pk = KeySelectorCache.Get(parentKey);

            var parentMap = new Dictionary<TKey, TParent>();
            foreach (var p in parents)
                parentMap[pk(p)] = p;

            foreach (var c in children)
            {
                var key = fk(c);
                parentMap.TryGetValue(key, out var parent);
                assign(c, parent);
            }
        }
    }
}
