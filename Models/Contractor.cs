using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenStock.Models;

[Table("contractors")]
public class Contractor
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("inn")]
    public string Inn { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("kpp")]
    public string? Kpp { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Не проверен";

    [Column("check_date")]
    public DateTime? CheckDate { get; set; }

    [Column("check_reason")]
    public string? CheckReason { get; set; }

    [Column("checked_by")]
    public string? CheckedBy { get; set; }

   
}