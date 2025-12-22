using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public int Order { get; }
        public string? Name { get; set; } // PK_Users, etc.

        public PrimaryKeyAttribute(int order = 0) => Order = order;
    }
}
