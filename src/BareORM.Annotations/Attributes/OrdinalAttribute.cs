using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class OrdinalAttribute : Attribute
    {
        public int Index { get; }
        public OrdinalAttribute(int index) => Index = index;
    }
}
