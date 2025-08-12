using System.Net;
using System.Net.Http.Json;
using StudyApp.Models;

namespace StudyApp.IntegrationTests;

public class StudyGroupCreateTests : IntegrationFixture
{
    [SetUp]
    public async Task DeleteAllGroupsBeforeTest()
    {
        await CleanupDatabase(x => x.StudyGroupMembers);
        await CleanupDatabase(x => x.StudyGroups);
    }

    [TestCase(null)]
    [TestCase(new int[] { })]
    public async Task Create_CreatesEmptyGroup_WhenUserListIsNullOrEmpty(int[]? users)
    {
        // Arrange
        var group = new { Name = "Math Club", Subject = "Math", UserIds = users };
        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", group);
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var createdGroup = await response.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(createdGroup!.Members, Is.Empty);
    }

    [TestCase("Math Club", "Math")]
    [TestCase("Клуб \"Эбонитовая палочка\" ⚡🪄", "Physics")]

    public async Task Create_CreatesGroup_WhenDataIsValid(string name, string subject)
    {
        // Arrange
        var userIds = await GetAvailableUserIds(3);
        var data = new
        {
            Name = name,
            Subject = subject,
            UserIds = userIds.ToArray()
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var group = await response.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(group!.Name, Is.EqualTo(data.Name));
        Assert.That(group.Subject, Is.EqualTo(Enum.Parse<Subject>(subject)));
        Assert.That(group.CreateDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
        Assert.That(group.Members.Select(x => x.UserId).ToList(), Is.EquivalentTo(data.UserIds));
    }

    [Test]
    public async Task Create_Fails_WhenSubjectIsInvalid()
    {
        // Arrange
        var data = new
        {
            Name = "Math Club",
            Subject = "InvalidSubject"
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var responseError = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(responseError!.Error, Does.Contain("Invalid subject"));
    }

    [TestCase("")]
    [TestCase("    ")]
    [TestCase("abcd")]
    [TestCase("1234567890123456789012345678901")]
    public async Task Create_Fails_WhenNameHasInvalidLength(string? name)
    {
        // Arrange
        var data = new
        {
            Name = name,
            Subject = "Math"
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var responseError = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(responseError!.Error, Does.Contain("Invalid name length"));
    }

    [TestCase(null, "Math")]
    [TestCase("Valid group name", null)]
    public async Task Create_Fails_WhenNameOrSubjectIsNull(string? name, string? subject)
    {
        // Arrange
        var data = new
        {
            Name = name,
            Subject = subject
        };

        // Act
        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var responseError = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(responseError!.Error, Does.Contain("required"));
    }

    [Test]
    public async Task Create_Fails_WhenUserIsAlreadyMemberOfTheGroupWithTheSameSubject()
    {
        // Arrange
        var subject = "Math";
        var data = new
        {
            Name = "Math Club",
            Subject = subject,
            UserIds = (await GetAvailableUserIds(2)).ToArray()
        };

        var response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);
        response.EnsureSuccessStatusCode();

        data = new
        {
            Name = "Math Club 2",
            Subject = subject,
            UserIds = (await GetAvailableUserIds(2)).ToArray()
        };

        // Act
        response = await ApiClient.PostAsJsonAsync("/api/studygroups", data);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var responseError = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(responseError!.Error, Does.Contain("already in a group"));
    }
}
