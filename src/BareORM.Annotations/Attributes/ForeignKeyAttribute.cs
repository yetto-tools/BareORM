namespace BareORM.Annotations.Attributes
{


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        public Type RefEntityType { get; }
        public string RefProperty { get; }

        public string? Name { get; set; }
        public ReferentialAction OnDelete { get; set; } = ReferentialAction.NoAction;
        public ReferentialAction OnUpdate { get; set; } = ReferentialAction.NoAction;

        public ForeignKeyAttribute(Type refEntityType, string refProperty)
        {
            RefEntityType = refEntityType;
            RefProperty = refProperty;
        }
    }

}
