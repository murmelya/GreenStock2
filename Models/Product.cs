using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenStock.Models;


[Table("products")]
public class Product
{
  
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

   
    [Column("article")]
    public string Article { get; set; } = string.Empty;

   
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [Column("purchase_price")]
    public decimal PurchasePrice { get; set; }

    [Column("purchase_currency")]
    public string PurchaseCurrency { get; set; } = "RUB";

    [Column("purchase_rate")]
    public decimal PurchaseRate { get; set; } = 1.0m;

    [Column("selling_price")]
    public decimal SellingPrice { get; set; }

    [Column("stock")]
    public int Stock { get; set; }

    [Column("expiry_date")]
    public DateOnly? ExpiryDate { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<ShipmentItem> ShipmentItems { get; set; } = new List<ShipmentItem>();
}
