using System.ComponentModel.DataAnnotations;

namespace PayMeNextApp.Data;

public class Expense
{
    public int Id { get; set; }
    
    public int GroupId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public string CreatedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Group Group { get; set; } = null!;
    public virtual ICollection<SplitEntry> SplitEntries { get; set; } = new List<SplitEntry>();
}
