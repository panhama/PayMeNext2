namespace PayMeNextApp.Data;

public class Reminder
{
    public int Id { get; set; }
    
    public int SplitEntryId { get; set; }
    
    public DateTime RemindAt { get; set; }
    
    public bool Sent { get; set; } = false;
    
    public DateTime? SentAt { get; set; }
    
    public string Message { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual SplitEntry SplitEntry { get; set; } = null!;
}
