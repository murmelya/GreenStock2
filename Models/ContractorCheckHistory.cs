using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenStock.Models;

[Table("contractor_check_history")]
public class ContractorCheckHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("contractor_id")]
    public Guid ContractorId { get; set; }

    [Column("check_date")]
    public DateTime CheckDate { get; set; }

    [Column("inn")]
    public string Inn { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("checked_by")]
    public string? CheckedBy { get; set; }

    [ForeignKey("ContractorId")]
    public Contractor? Contractor { get; set; }
}
