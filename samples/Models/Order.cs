using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using BareORM.Annotations;
using BareORM.Annotations.Attributes;


namespace BareORM.samples.Models
{
    public sealed class Order
    {
        [PrimaryKey]
        [IncrementalKey]
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = "";

        [ColumnName("TotalAmount")]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey(typeof(User), nameof(User.UserId), Name = "FK_Orders_Users",
            OnDelete = ReferentialAction.Cascade)]
        public long UserId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}
