using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyApp.Models;
using StudyApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure global model validation behavior to simplify validation assertions in tests
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .Select(x => $"{x.Key}: {x.Value!.Errors.First().ErrorMessage}")
            .FirstOrDefault();

        return new BadRequestObjectResult(new ErrorResponse(errors ?? "Validation failed"));
    };
});


// DB setup: use SQL Server for persistence, fallback to InMemory for fast development
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (useInMemory || string.IsNullOrEmpty(connectionString))
        options.UseInMemoryDatabase("StudyAppDb");
    else
        options.UseSqlServer(connectionString);
});

// Services
builder.Services.AddScoped<IStudyGroupService, StudyGroupService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

// Initialize DB and seed test users for development and demo purpose
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    await db.Database.EnsureCreatedAsync();
    logger.LogInformation("Database initialization completed successfully.");

    // Seed users
    if (!db.Users.Any())
    {
        db.Seed();
        logger.LogInformation("Database seeded with initial users.");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize database.");
    throw; // Re-throw to prevent app from starting with broken database
}

app.Run();

public partial class Program
{
}
