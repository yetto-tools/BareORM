using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Object-level validation: minimum string length.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class MinLengthAttribute : Attribute
    {
        /// <summary>
        /// valor de la longitud minima
        /// </summary>
        public int Value { get; }
        /// <summary>
        /// validacion de longitud minima
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public MinLengthAttribute(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
    }
}
