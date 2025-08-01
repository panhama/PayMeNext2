using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using PayMeNextApp;
using PayMeNextApp.Data;
using PayMeNextApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("PayMeNextDb"));

// Services
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<ReminderService>();

var host = builder.Build();

// Initialize database
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
    await context.SeedDataAsync();
}

await host.RunAsync();