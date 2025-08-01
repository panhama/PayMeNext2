using Microsoft.EntityFrameworkCore;
using PayMeNextApp.Data;

namespace PayMeNextApp.Services;

public class ExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Expense> CreateExpenseAsync(int groupId, string description, decimal amount, string createdBy, List<string>? selectedParticipants = null)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            throw new ArgumentException("Group not found", nameof(groupId));

        var expense = new Expense
        {
            GroupId = groupId,
            Description = description,
            Amount = amount,
            CreatedBy = createdBy,
            Date = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        // Auto-split logic
        await CreateSplitEntriesAsync(expense, selectedParticipants ?? group.Members);

        return expense;
    }

    private async Task CreateSplitEntriesAsync(Expense expense, List<string> participants)
    {
        var sharePerPerson = expense.Amount / participants.Count;

        foreach (var participant in participants)
        {
            var splitEntry = new SplitEntry
            {
                ExpenseId = expense.Id,
                Participant = participant,
                Share = sharePerPerson,
                Paid = participant == expense.CreatedBy // Creator auto-marked as paid
            };

            if (splitEntry.Paid)
            {
                splitEntry.PaidAt = DateTime.UtcNow;
            }

            _context.SplitEntries.Add(splitEntry);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> TogglePaidStatusAsync(int splitEntryId)
    {
        var splitEntry = await _context.SplitEntries
            .Include(s => s.Reminders)
            .FirstOrDefaultAsync(s => s.Id == splitEntryId);

        if (splitEntry == null) return false;

        splitEntry.Paid = !splitEntry.Paid;
        splitEntry.PaidAt = splitEntry.Paid ? DateTime.UtcNow : null;

        // If marking as paid, remove pending reminders
        if (splitEntry.Paid)
        {
            var pendingReminders = splitEntry.Reminders.Where(r => !r.Sent).ToList();
            _context.Reminders.RemoveRange(pendingReminders);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Expense>> GetGroupExpensesAsync(int groupId)
    {
        return await _context.Expenses
            .Include(e => e.SplitEntries)
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetGroupTotalAsync(int groupId)
    {
        return await _context.Expenses
            .Where(e => e.GroupId == groupId)
            .SumAsync(e => e.Amount);
    }

    public async Task<List<SplitEntry>> GetUnpaidSplitsAsync(string participant)
    {
        return await _context.SplitEntries
            .Include(s => s.Expense)
            .ThenInclude(e => e.Group)
            .Where(s => s.Participant == participant && !s.Paid)
            .OrderByDescending(s => s.Expense.Date)
            .ToListAsync();
    }
}
