using System.ComponentModel.DataAnnotations;

namespace PayMeNextApp.Data;

public class SplitEntry
{
    public int Id { get; set; }
    
    public int ExpenseId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Participant { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue)]
    public decimal Share { get; set; }
    
    public bool Paid { get; set; } = false;
    
    public DateTime? PaidAt { get; set; }
    
    // Navigation properties
    public virtual Expense Expense { get; set; } = null!;
    public virtual ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
