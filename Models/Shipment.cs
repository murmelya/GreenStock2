using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenStock.Models;

[Table("shipments")]
public class Shipment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("recipient")]
    public string Recipient { get; set; } = string.Empty;

    public User CreatedByUser { get; set; } = null!;

    public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
}
