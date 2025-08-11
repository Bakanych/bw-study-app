using System.Net;
using System.Net.Http.Json;
using StudyApp.Models;

namespace StudyApp.IntegrationTests;

public class StudyGroupJoinTests : IntegrationFixture
{
    private List<int> _availableUserIds = new();
    private int _chemistryGroupId;
    private int _mathGroupId;
    private int _physicsGroupId;

    [SetUp]
    public async Task Setup()
    {
        await CleanupDatabase(x => x.StudyGroupMembers);
        await CleanupDatabase(x => x.StudyGroups);

        _availableUserIds = await GetAvailableUserIds(10);

        var mathGroup = await CreateStudyGroup("Math Club", Subject.Math, [_availableUserIds[0], _availableUserIds[1]]);
        _mathGroupId = mathGroup.StudyGroupId;

        var physicsGroup =
            await CreateStudyGroup("Physics Lab", Subject.Physics, [_availableUserIds[2], _availableUserIds[3]]);
        _physicsGroupId = physicsGroup.StudyGroupId;

        var chemistryGroup = await CreateStudyGroup("Chemistry Club", Subject.Chemistry, []);
        _chemistryGroupId = chemistryGroup.StudyGroupId;
    }


    [Test]
    public async Task Join_AllowsMultipleUsersInSameGroup()
    {
        // Arrange - Use existing users who are not in any groups yet
        var userId1 = 5;
        var userId2 = 6;
        var userId3 = 7;

        // Act - Multiple users join the same group
        var response1 = await ApiClient.PostAsync($"/api/studygroups/{_chemistryGroupId}/join?userId={userId1}", null);
        var response2 = await ApiClient.PostAsync($"/api/studygroups/{_chemistryGroupId}/join?userId={userId2}", null);
        var response3 = await ApiClient.PostAsync($"/api/studygroups/{_chemistryGroupId}/join?userId={userId3}", null);

        // Assert
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response3.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify all users were added
        var getResponse = await ApiClient.GetAsync($"/api/studygroups/{_chemistryGroupId}");
        var group = await getResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(group!.Members.Count, Is.EqualTo(3));
        var userIds = group.Members.Select(m => m.UserId).ToList();
        Assert.That(userIds, Contains.Item(userId1));
        Assert.That(userIds, Contains.Item(userId2));
        Assert.That(userIds, Contains.Item(userId3));
    }

    [Test]
    public async Task Join_ReturnsConflict_WhenUserAlreadyInGroupWithSameSubject()
    {
        // Arrange - First available user is already in Math group
        var existingUserId = _availableUserIds[0];

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_mathGroupId}/join?userId={existingUserId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(errorResponse!.Error, Is.EqualTo("User already in a group for this subject"));
    }

    [Test]
    public async Task Join_Success_WhenUserInDifferentSubjectGroup()
    {
        // Arrange - First available user is in Math group, but can join Physics group
        var userId = _availableUserIds[0];

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_physicsGroupId}/join?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify user is now in both groups (different subjects)
        var mathResponse = await ApiClient.GetAsync($"/api/studygroups/{_mathGroupId}");
        var mathGroup = await mathResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(mathGroup!.Members.Any(m => m.UserId == userId), Is.True);

        var physicsResponse = await ApiClient.GetAsync($"/api/studygroups/{_physicsGroupId}");
        var physicsGroup = await physicsResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(physicsGroup!.Members.Any(m => m.UserId == userId), Is.True);
    }

    [Test]
    public async Task Join_ReturnsBadRequest_WhenRequestParamsAreInvalid()
    {
        // Arrange
        var nonExistentGroupId = 99999;
        var userId = 20;

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{nonExistentGroupId}/join?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Error, Is.Not.Empty);
    }

    [TestCase(0, 0)]
    [TestCase(-1, -1)]
    [TestCase(int.MaxValue, int.MaxValue)]
    public async Task Join_ReturnsBadRequest_WhenRequestParamsAreInvalid(int userId, int groupId)
    {
        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{groupId}/join?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.That(errorResponse, Is.Not.Null);
    }

    [Test]
    public async Task Join_ReturnsBadRequest_WhenUserIdParameterIsMissing()
    {
        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_mathGroupId}/join", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}