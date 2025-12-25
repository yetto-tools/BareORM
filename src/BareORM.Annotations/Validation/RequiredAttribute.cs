namespace BareORM.Annotations.Validation
{
    /// <summary>
    /// Object-level validation hint. Does NOT imply NOT NULL in DB schema.
    /// </summary>

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public bool AllowEmptyStrings { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="allowEmptyStrings"></param>
        public RequiredAttribute(bool allowEmptyStrings = false)
            => AllowEmptyStrings = allowEmptyStrings;
    }
}
