using System.Net;
using System.Net.Http.Json;
using StudyApp.Models;

namespace StudyApp.IntegrationTests;

public class StudyGroupLeaveTests : IntegrationFixture
{
    private List<int> _availableUserIds = new();
    private int _emptyGroupId;
    private int _mathGroupId;
    private int _physicsGroupId;

    [SetUp]
    public async Task Setup()
    {
        await CleanupDatabase(x => x.StudyGroupMembers);
        await CleanupDatabase(x => x.StudyGroups);

        _availableUserIds = await GetAvailableUserIds(10);

        var mathGroup = await CreateStudyGroup("Math Club", Subject.Math,
            [_availableUserIds[0], _availableUserIds[1], _availableUserIds[2]]);
        _mathGroupId = mathGroup.StudyGroupId;

        var physicsGroup =
            await CreateStudyGroup("Physics Lab", Subject.Physics, [_availableUserIds[3], _availableUserIds[4]]);
        _physicsGroupId = physicsGroup.StudyGroupId;

        var emptyGroup = await CreateStudyGroup("Empty Group", Subject.Chemistry, []);
        _emptyGroupId = emptyGroup.StudyGroupId;
    }

    [Test]
    public async Task Leave_Success_WhenUserIsMemberOfGroup()
    {
        // Arrange - First available user is a member of Math group
        var userId = _availableUserIds[0];

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_mathGroupId}/leave?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify user was removed from the group
        var getResponse = await ApiClient.GetAsync($"/api/studygroups/{_mathGroupId}");
        var group = await getResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(group!.Members.Any(m => m.UserId == userId), Is.False);
        Assert.That(group.Members.Count, Is.EqualTo(2)); // Originally 3, now 2
    }

    [Test]
    public async Task Leave_RemovesOnlySpecifiedUser_WhenMultipleUsersInGroup()
    {
        // Arrange - Second user should be removed, but first and third should remain
        var userToRemove = _availableUserIds[1];

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_mathGroupId}/leave?userId={userToRemove}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify only specified user was removed
        var getResponse = await ApiClient.GetAsync($"/api/studygroups/{_mathGroupId}");
        var group = await getResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(group!.Members.Count, Is.EqualTo(2));
        Assert.That(group.Members.Any(m => m.UserId == userToRemove), Is.False);
        Assert.That(group.Members.Any(m => m.UserId == _availableUserIds[0]), Is.True);
        Assert.That(group.Members.Any(m => m.UserId == _availableUserIds[2]), Is.True);
    }

    [Test]
    public async Task Leave_Success_WhenUserNotMemberOfGroup()
    {
        // Arrange - Use a user that is not a member of Math group (beyond the first 3)
        var userId = _availableUserIds[5];

        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{_mathGroupId}/leave?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify group members count unchanged
        var getResponse = await ApiClient.GetAsync($"/api/studygroups/{_mathGroupId}");
        var group = await getResponse.Content.ReadFromJsonAsync<StudyGroup>();
        Assert.That(group!.Members.Count, Is.EqualTo(3));
    }

    [TestCase(0, 0)]
    [TestCase(-1, -1)]
    [TestCase(null, 1)]
    public async Task Leave_ReturnsBadRequest_WhenUserIdOrGroupIdAreInvalid(int? userId, int? groupId)
    {
        // Act
        var response = await ApiClient.PostAsync($"/api/studygroups/{groupId}/leave?userId={userId}", null);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}