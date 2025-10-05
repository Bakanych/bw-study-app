using System.Net;
using StudyApp.Models;

namespace StudyApp.IntegrationTests;

public class StudyGroupDeleteTests : IntegrationFixture
{
    [Test]
    public async Task Delete_CascadeDeletesGroupMembers()
    {
        // Arrange
        var userIds = await GetAvailableUserIds(2);
        var group = await CreateStudyGroup("Physics Lab", Subject.Physics, userIds);
        var members = DbContext.StudyGroupMembers
            .Where(x => x.StudyGroupId == group.StudyGroupId);

        Assert.That(members.Count, Is.EqualTo(2));

        // Act
        var response = await ApiClient.DeleteAsync($"/api/studygroups/{group.StudyGroupId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var getResponse = await ApiClient.GetAsync($"/api/studygroups/{group.StudyGroupId}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(members.Count, Is.EqualTo(0));
    }
}