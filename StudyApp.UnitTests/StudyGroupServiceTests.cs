using Microsoft.EntityFrameworkCore;
using StudyApp.Models;
using StudyApp.Services;

namespace StudyApp.UnitTests;

public class StudyGroupServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { UserId = 1, Name = "Mary" },
                new User { UserId = 2, Name = "Bob" },
                new User { UserId = 3, Name = "Alice" }
            );
            db.SaveChanges();
        }

        return db;
    }

    [Test]
    public async Task CreateGroup_Success_WithValidData()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var group = await svc.CreateGroupAsync("Math Club", Subject.Math, [1, 2]);

        Assert.That(group.StudyGroupId, Is.GreaterThan(0));
        Assert.That(group.Name, Is.EqualTo("Math Club"));
        Assert.That(group.Subject, Is.EqualTo(Subject.Math));
        Assert.That(group.Members.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateGroup_Success_WithEmptyUserList()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var group = await svc.CreateGroupAsync("Solo Study", Subject.Physics, []);

        Assert.That(group.StudyGroupId, Is.GreaterThan(0));
        Assert.That(group.Members.Count, Is.EqualTo(0));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("This name is way too long for the validation")]
    public void CreateGroup_ThrowsArgumentException_InvalidName(string name)
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateGroupAsync(name, Subject.Math, []));
        Assert.That(ex!.Message, Does.Contain("Invalid name length"));
    }

    [Test]
    public void CreateGroup_ThrowsInvalidOperationException_NonExistentUsers()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateGroupAsync("Group", Subject.Math, [int.MaxValue]));
        Assert.That(ex!.Message, Does.Contain("Some users do not exist"));
    }

    [Test]
    public async Task CreateGroup_ThrowsInvalidOperationException_UserAlreadyInSubject()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        await svc.CreateGroupAsync("First Math", Subject.Math, [1]);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateGroupAsync("Second Math", Subject.Math, [1]));
        Assert.That(ex!.Message, Does.Contain("already in a group"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsGroup_WhenExists()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var created = await svc.CreateGroupAsync("Test Group", Subject.Math, [1]);
        var retrieved = await svc.GetByIdAsync(created.StudyGroupId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Test Group"));
        Assert.That(retrieved.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var result = await svc.GetByIdAsync(1);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ListGroupsAsync_ReturnsAllGroups_NoFilter()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        await svc.CreateGroupAsync("Math 1", Subject.Math, []);
        await svc.CreateGroupAsync("Physics 1", Subject.Physics, []);

        var groups = await svc.GetAllAsync();

        Assert.That(groups.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ListGroupsAsync_FiltersBySubject()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        await svc.CreateGroupAsync("Math 1", Subject.Math, []);
        await svc.CreateGroupAsync("Math 2", Subject.Math, []);
        await svc.CreateGroupAsync("Physics 1", Subject.Physics, []);

        var mathGroups = await svc.SearchAsync(Subject.Math);

        Assert.That(mathGroups.Count, Is.EqualTo(2));
        Assert.That(mathGroups.All(g => g.Subject == Subject.Math), Is.True);
    }

    [Test]
    public async Task JoinGroupAsync_Success_WhenUserCanJoin()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var group = await svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var result = await svc.JoinGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task JoinGroupAsync_ReturnsFalse_WhenUserAlreadyInSubject()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var group = await svc.CreateGroupAsync("Math Group", Subject.Math, [1]);
        var result = await svc.JoinGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.False);
    }

    [Test]
    public void JoinGroupAsync_ThrowsInvalidOperationException_GroupNotExists()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.JoinGroupAsync(1, 1));
        Assert.That(ex!.Message, Does.Contain("Group does not exist"));
    }

    [Test]
    public async Task JoinGroupAsync_ThrowsInvalidOperationException_UserNotExists()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);
        var nonExisting = db.Users.Max(x => x.UserId) + 1;
        var group = await svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.JoinGroupAsync(group.StudyGroupId, nonExisting));
        Assert.That(ex!.Message, Does.Contain("User does not exist"));
    }

    [Test]
    public async Task LeaveGroupAsync_Success_WhenUserIsMember()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);

        var group = await svc.CreateGroupAsync("Test Group", Subject.Math, [1]);
        var result = await svc.LeaveGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LeaveGroupAsync_Success_WhenUserNotMember()
    {
        using var db = CreateDb();
        var svc = new StudyGroupService(db);
        var nonExisting = db.Users.Max(x => x.UserId) + 1;
        var group = await svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var result = await svc.LeaveGroupAsync(group.StudyGroupId, nonExisting);

        Assert.That(result, Is.True);
    }
}
