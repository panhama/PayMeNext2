using System.ComponentModel.DataAnnotations;

namespace PayMeNextApp.Data;

public class Group
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public List<string> Members { get; set; } = new();
    
    // Navigation properties
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
