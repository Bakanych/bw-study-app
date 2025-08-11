using Bogus;
using Microsoft.EntityFrameworkCore;

namespace StudyApp.Models;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<StudyGroupMember> StudyGroupMembers => Set<StudyGroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StudyGroupMember>()
            .HasKey(m => new { m.StudyGroupId, m.UserId });

        modelBuilder.Entity<StudyGroupMember>()
            .HasOne(m => m.StudyGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.StudyGroupId);

        modelBuilder.Entity<StudyGroupMember>()
            .HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId);

        modelBuilder.Entity<StudyGroup>()
            .Property(e => e.Subject)
            .HasConversion<string>();
    }

    public void Seed(int targetUserCount = 20, int targetGroupCount = 5)
    {
        Database.EnsureCreated();

        var currentUserCount = Users.Count();

        if (currentUserCount >= targetUserCount) return;
        var usersToGenerate = targetUserCount - currentUserCount;

        var userFaker = new Faker<User>()
            .RuleFor(u => u.Name, f => $"{f.Person.FirstName} {f.Person.LastName}");

        var users = userFaker.Generate(usersToGenerate);
        Users.AddRange(users);

        var groupFaker = new Faker<StudyGroup>()
            .RuleFor(g => g.Subject, f => f.PickRandom<Subject>())
            .RuleFor(g => g.Name, (f, g) =>
            {
                var raw = $"{g.Subject} {f.Hacker.Adjective()} {f.Hacker.Noun()}";
                var trimmed = raw.Length > 30 ? raw[..30] : raw;
                if (trimmed.Length < 5) trimmed = (trimmed + " group").PadRight(5);
                return trimmed.Trim();
            });

        var groups = groupFaker.Generate(targetGroupCount);
        StudyGroups.AddRange(groups);

        SaveChanges();
    }
}