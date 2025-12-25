using System.Runtime.InteropServices;
using BareORM.Annotations;
using BareORM.Annotations.Attributes;
using BareORM.Annotations.Validation;

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

        
        public string ProductId { get; set; }

        public string SKU { get; set; } = "";
        public int Qty { get; set; }

        [Precision(18, 4)]
        
        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
