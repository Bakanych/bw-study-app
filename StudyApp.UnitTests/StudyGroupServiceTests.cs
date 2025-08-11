using Microsoft.EntityFrameworkCore;
using StudyApp.Models;
using StudyApp.Services;

namespace StudyApp.UnitTests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Parallelizable(ParallelScope.All)]
public class StudyGroupServiceTests : IDisposable, IAsyncDisposable
{
    private readonly AppDbContext _db;
    private readonly StudyGroupService _svc;


    public StudyGroupServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _svc = new StudyGroupService(_db);
        _db.Seed(3, 0);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Test]
    public async Task CreateGroup_Success_WithValidData()
    {
        var group = await _svc.CreateGroupAsync("Math Club", Subject.Math, [1, 2]);

        Assert.That(group.StudyGroupId, Is.GreaterThan(0));
        Assert.That(group.Name, Is.EqualTo("Math Club"));
        Assert.That(group.Subject, Is.EqualTo(Subject.Math));
        Assert.That(group.Members.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task CreateGroup_Success_WithEmptyUserList()
    {
        var group = await _svc.CreateGroupAsync("Solo Study", Subject.Physics, []);

        Assert.That(group.StudyGroupId, Is.GreaterThan(0));
        Assert.That(group.Members.Count, Is.EqualTo(0));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("This name is way too long for the validation")]
    public void CreateGroup_ThrowsArgumentException_InvalidName(string name)
    {
        var ex = Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.CreateGroupAsync(name, Subject.Math, []));
        Assert.That(ex!.Message, Does.Contain("Invalid name length"));
    }

    [Test]
    public void CreateGroup_ThrowsInvalidOperationException_NonExistentUsers()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.CreateGroupAsync("Group", Subject.Math, [int.MaxValue]));
        Assert.That(ex!.Message, Does.Contain("Some users do not exist"));
    }

    [Test]
    public async Task CreateGroup_ThrowsInvalidOperationException_UserAlreadyInSubject()
    {
        await _svc.CreateGroupAsync("First Math", Subject.Math, [1]);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.CreateGroupAsync("Second Math", Subject.Math, [1]));
        Assert.That(ex!.Message, Does.Contain("already in a group"));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsGroup_WhenExists()
    {
        var created = await _svc.CreateGroupAsync("Test Group", Subject.Math, [1]);
        var retrieved = await _svc.GetByIdAsync(created.StudyGroupId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Test Group"));
        Assert.That(retrieved.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _svc.GetByIdAsync(1);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ListGroupsAsync_ReturnsAllGroups_NoFilter()
    {
        await _svc.CreateGroupAsync("Math 1", Subject.Math, []);
        await _svc.CreateGroupAsync("Physics 1", Subject.Physics, []);

        var groups = await _svc.GetAllAsync();

        Assert.That(groups.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ListGroupsAsync_FiltersBySubject()
    {
        await _svc.CreateGroupAsync("Math 1", Subject.Math, []);
        await _svc.CreateGroupAsync("Math 2", Subject.Math, []);
        await _svc.CreateGroupAsync("Physics 1", Subject.Physics, []);

        var mathGroups = await _svc.SearchAsync(Subject.Math);

        Assert.That(mathGroups.Count, Is.EqualTo(2));
        Assert.That(mathGroups.All(g => g.Subject == Subject.Math), Is.True);
    }

    [Test]
    public async Task JoinGroupAsync_Success_WhenUserCanJoin()
    {
        var group = await _svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var result = await _svc.JoinGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task JoinGroupAsync_ReturnsFalse_WhenUserAlreadyInSubject()
    {
        var group = await _svc.CreateGroupAsync("Math Group", Subject.Math, [1]);
        var result = await _svc.JoinGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.False);
    }

    [Test]
    public void JoinGroupAsync_ThrowsInvalidOperationException_GroupNotExists()
    {
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.JoinGroupAsync(1, 1));
        Assert.That(ex!.Message, Does.Contain("Group does not exist"));
    }

    [Test]
    public async Task JoinGroupAsync_ThrowsInvalidOperationException_UserNotExists()
    {
        var nonExisting = _db.Users.Max(x => x.UserId) + 1;
        var group = await _svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.JoinGroupAsync(group.StudyGroupId, nonExisting));
        Assert.That(ex!.Message, Does.Contain("User does not exist"));
    }

    [Test]
    public async Task LeaveGroupAsync_Success_WhenUserIsMember()
    {
        var group = await _svc.CreateGroupAsync("Test Group", Subject.Math, [1]);
        var result = await _svc.LeaveGroupAsync(group.StudyGroupId, 1);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LeaveGroupAsync_Success_WhenUserNotMember()
    {
        var nonExisting = _db.Users.Max(x => x.UserId) + 1;
        var group = await _svc.CreateGroupAsync("Test Group", Subject.Math, []);
        var result = await _svc.LeaveGroupAsync(group.StudyGroupId, nonExisting);

        Assert.That(result, Is.True);
    }
}