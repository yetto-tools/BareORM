using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Object-level validation for numeric/date types.
    /// (Strings use MinLength/MaxLength/Length)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RangeAttribute : Attribute
    {
        /// <summary>
        /// valor minimo
        /// </summary>
        public double Min { get; }
        /// <summary>
        /// valor maximo
        /// </summary>
        public double Max { get; }
        /// <summary>
        ///     
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public RangeAttribute(double min, double max)
        {
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));
            Min = min;
            Max = max;
        }
    }
}
