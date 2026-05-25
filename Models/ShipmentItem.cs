using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenStock.Models;

[Table("shipment_items")]
public class ShipmentItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("shipment_id")]
    public Guid ShipmentId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    public Shipment Shipment { get; set; } = null!;

    public Product Product { get; set; } = null!;
}