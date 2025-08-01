using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace PayMeNextApp.Data;

    public class AppDbContext : DbContext
    {
        // Constructor required for AddDbContext configuration
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<SplitEntry> SplitEntries { get; set; }
    public DbSet<Reminder> Reminders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use in-memory database for Blazor WebAssembly
        optionsBuilder.UseInMemoryDatabase("PayMeNextDb");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Group entity
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Members)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                );
        });

        // Configure Expense entity
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Expenses)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SplitEntry entity
        modelBuilder.Entity<SplitEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Participant).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Share).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Expense)
                .WithMany(ex => ex.SplitEntries)
                .HasForeignKey(e => e.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Reminder entity
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SplitEntry)
                .WithMany(s => s.Reminders)
                .HasForeignKey(e => e.SplitEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public async Task SeedDataAsync()
    {
        if (!Groups.Any())
        {
            var sampleGroup = new Group
            {
                Name = "Florida Destin Trip",
                Members = new List<string> { "Alice", "Bob", "Charlie", "Diana" },
                Created = DateTime.UtcNow.AddDays(-7)
            };

            Groups.Add(sampleGroup);
            await SaveChangesAsync();

            var sampleExpense = new Expense
            {
                GroupId = sampleGroup.Id,
                Description = "Beach House Rental",
                Amount = 800.00m,
                Date = DateTime.UtcNow.AddDays(-5),
                CreatedBy = "Alice"
            };

            Expenses.Add(sampleExpense);
            await SaveChangesAsync();

            var splitAmount = sampleExpense.Amount / sampleGroup.Members.Count;
            foreach (var member in sampleGroup.Members)
            {
                var splitEntry = new SplitEntry
                {
                    ExpenseId = sampleExpense.Id,
                    Participant = member,
                    Share = splitAmount,
                    Paid = member == "Alice" // Creator auto-paid
                };

                SplitEntries.Add(splitEntry);
            }

            await SaveChangesAsync();
        }
    }
}
