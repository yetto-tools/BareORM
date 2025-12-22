using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{
 

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; }
        public string Schema { get; }

        public TableAttribute(string name, string schema = "dbo")
        {
            Name = name;
            Schema = schema;
        }
    }

}
