using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Attributes
{

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IgnoreAttribute : Attribute { }
}
