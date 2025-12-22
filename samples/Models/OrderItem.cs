using BareORM.Annotations;
using BareORM.Annotations.Attributes;

namespace BareORM.samples.Models
{
    public sealed class OrderItem
    {
        [PrimaryKey]
        [IncrementalKey]
        public int OrderItemId { get; set; }

        [ForeignKey(typeof(Order), nameof(Order.OrderId), Name = "FK_Orders_OrderItems",
            OnDelete = ReferentialAction.Cascade)]
        public long OrderId { get; set; }

        public string SKU { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
