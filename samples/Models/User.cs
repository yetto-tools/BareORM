using BareORM.Annotations.Attributes;
using BareORM.Annotations.Validation;


namespace BareORM.samples.Models
{
    public sealed class User
    {
        [PrimaryKey]
        [IncrementalKey]
        public long UserId    { get; set;}
        
        [Required, ColumnNotNull]
        public string Email { get; set;} = string.Empty;

        [Required, ColumnNotNull]
        public string DisplayName   { get; set;} = string.Empty;
        public bool IsActive  { get; set;}
        public DateTime CreatedAt { get; set; }

        [Json] // <-- el mapper sabe que esto viene en una columna JSON (string)
        public UserSettings Settings { get; set; } = new();

        public List<Order> Orders { get; set; } = new();
    }
    public sealed class UserSettings
    {
        public string Theme { get; set; } = "light";
        public long Notifications { get; set; }
    }
}
