using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyApp.Controllers;
using StudyApp.Models;
using Testcontainers.MsSql;

namespace StudyApp.IntegrationTests;

[Parallelizable(ParallelScope.Fixtures)]
public abstract class IntegrationFixture
{
    private WebApplicationFactory<Program> _app;
    private MsSqlContainer _sqlContainer;
    protected HttpClient ApiClient;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        // Start SQL Server container
        _sqlContainer = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .Build();

        await _sqlContainer.StartAsync();

        // Create a web application factory with SQL Server in a container
        _app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTesting");
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext registrations with container DB
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseSqlServer(_sqlContainer.GetConnectionString());
                    });
                });
            });

        ApiClient = _app.CreateClient();

        // Initialize database and seed test data
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Seed();
    }


    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        ApiClient.Dispose();
        await _app.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    protected async Task CleanupDatabase<T>(Func<AppDbContext, DbSet<T>> entitySelector) where T : class
    {
        using var scope = _app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entities = await entitySelector(dbContext).ToListAsync();

        if (entities.Any())
        {
            entitySelector(dbContext).RemoveRange(entities);
            await dbContext.SaveChangesAsync();
        }
    }

    protected async Task<StudyGroup> CreateStudyGroup(string name, Subject subject, IEnumerable<int> userIds)
    {
        var data = new CreateGroupRequest(name, subject.ToString(), userIds);
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);
        return await response.Content.ReadFromJsonAsync<StudyGroup>() ?? throw new Exception("Failed to create group");
    }

    protected async Task<List<int>> GetAvailableUserIds(int count)
    {
        using var scope = _app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Users.Take(count).Select(u => u.UserId).ToListAsync();
    }
}
