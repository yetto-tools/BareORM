using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BareORM.Schema.Utilities
{
    internal static class ReflectionHelpers
    {
        public static IEnumerable<PropertyInfo> GetMappableProperties(Type t)
            => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => p.GetIndexParameters().Length == 0);
    }
}
