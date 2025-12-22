namespace BareORM.Annotations
{
    public enum ReferentialAction
    {
        NoAction = 0,
        Restrict = 1,
        Cascade = 2,
        SetNull = 3,
        SetDefault = 4
    }
}
