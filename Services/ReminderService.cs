using Microsoft.JSInterop;
using PayMeNextApp.Data;
using Microsoft.EntityFrameworkCore;

namespace PayMeNextApp.Services;

public class ReminderService
{
    private readonly AppDbContext _context;
    private readonly IJSRuntime _jsRuntime;
    private readonly Timer _reminderTimer;

    public ReminderService(AppDbContext context, IJSRuntime jsRuntime)
    {
        _context = context;
        _jsRuntime = jsRuntime;
        
        // Check for pending reminders every minute
        _reminderTimer = new Timer(CheckPendingReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public async Task<Reminder> CreateReminderAsync(int splitEntryId, DateTime remindAt, string? customMessage = null)
    {
        var splitEntry = await _context.SplitEntries
            .Include(s => s.Expense)
            .ThenInclude(e => e.Group)
            .FirstOrDefaultAsync(s => s.Id == splitEntryId);

        if (splitEntry == null)
            throw new ArgumentException("Split entry not found", nameof(splitEntryId));

        var reminder = new Reminder
        {
            SplitEntryId = splitEntryId,
            RemindAt = remindAt,
            Message = customMessage ?? $"Reminder: You owe ${splitEntry.Share:F2} for '{splitEntry.Expense.Description}' in group '{splitEntry.Expense.Group.Name}'"
        };

        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();

        return reminder;
    }

    public async Task<List<Reminder>> GetPendingRemindersAsync()
    {
        return await _context.Reminders
            .Include(r => r.SplitEntry)
            .ThenInclude(s => s.Expense)
            .ThenInclude(e => e.Group)
            .Where(r => !r.Sent && r.RemindAt <= DateTime.UtcNow)
            .OrderBy(r => r.RemindAt)
            .ToListAsync();
    }

    public async Task<bool> SendReminderAsync(int reminderId)
    {
        var reminder = await _context.Reminders
            .Include(r => r.SplitEntry)
            .ThenInclude(s => s.Expense)
            .ThenInclude(e => e.Group)
            .FirstOrDefaultAsync(r => r.Id == reminderId);

        if (reminder == null) return false;

        try
        {
            // Send browser notification
            await _jsRuntime.InvokeVoidAsync("showNotification", 
                "Payment Reminder", 
                reminder.Message,
                "/icon-192.png");

            // Mark as sent
            reminder.Sent = true;
            reminder.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send reminder: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendManualReminderAsync(int splitEntryId, string customMessage)
    {
        var splitEntry = await _context.SplitEntries
            .Include(s => s.Expense)
            .ThenInclude(e => e.Group)
            .FirstOrDefaultAsync(s => s.Id == splitEntryId);

        if (splitEntry == null) return false;

        try
        {
            var message = $"Manual Reminder: {customMessage} - You owe ${splitEntry.Share:F2} for '{splitEntry.Expense.Description}'";
            
            await _jsRuntime.InvokeVoidAsync("showNotification", 
                "Payment Reminder", 
                message,
                "/icon-192.png");

            // Create a manual reminder record
            var reminder = new Reminder
            {
                SplitEntryId = splitEntryId,
                RemindAt = DateTime.UtcNow,
                Message = message,
                Sent = true,
                SentAt = DateTime.UtcNow
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send manual reminder: {ex.Message}");
            return false;
        }
    }

    private async void CheckPendingReminders(object? state)
    {
        try
        {
            var pendingReminders = await GetPendingRemindersAsync();
            
            foreach (var reminder in pendingReminders)
            {
                await SendReminderAsync(reminder.Id);
                
                // Add delay to avoid spam
                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking pending reminders: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _reminderTimer?.Dispose();
    }
}
