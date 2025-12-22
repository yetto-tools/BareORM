using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Annotations.Attributes;


namespace BareORM.samples.Models
{
    public sealed class User
    {
        [PrimaryKey]
        [IncrementalKey]
        public long UserId    { get; set;}
        public string Email { get; set;}
        public string DisplayName   { get; set;}
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
