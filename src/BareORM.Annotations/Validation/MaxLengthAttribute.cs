using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Object-level validation: maximum string length.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MaxLengthAttribute : Attribute
    {
        /// <summary>
        /// valor de la longitud maxima
        /// </summary>
        public int Value { get; }
        /// <summary>
        /// validacion de longitud maxima
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public MaxLengthAttribute(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
    }
}
