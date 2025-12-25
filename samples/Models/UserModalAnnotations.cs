
using BareORM.Annotations;
using BareORM.Annotations.Attributes;

namespace BareORM.samples.Models
{


    [Table("User", "dbo")]
    [Check("IsActive IN (0,1)", Name = "CK_Users_IsActive")]
    public sealed class UserModelAnnotations
    {
        [PrimaryKey]
        [IncrementalKey]
        public long UserId { get; set; }

        [Unique("UQ_Users_Email")]
        public string Email { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public Guid UserGuid { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; } 

        [ColumnName("settings")]
        [Json]
        public UserSettingsModelAnnotations Settings { get; set; } = new();
    }

    public sealed class UserSettingsModelAnnotations
    {
        public string Theme { get; set; } = "light";
        public bool Notifications { get; set; }
    }

    [Table("Orders", "dbo")]
    public sealed class OrderModelAnnotations
    {
        [PrimaryKey]
        [IncrementalKey]
        public int OrderId { get; set; }

        [ForeignKey(typeof(User), nameof(User.UserId), Name = "FK_Orders_Users",
            OnDelete = ReferentialAction.Cascade)]
        public int UserId { get; set; }

        public string OrderNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

}
