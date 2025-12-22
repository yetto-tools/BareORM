using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class UniqueAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; set; } = 0;

        public UniqueAttribute(string name) => Name = name;
    }
}
