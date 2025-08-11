using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StudyApp.Models;

namespace StudyApp.IntegrationTests;

public class StudyGroupGetTests : IntegrationFixture
{
    private StudyGroup _existingGroup;

    [SetUp]
    public async Task Setup()
    {
        await CleanupDatabase(x => x.StudyGroupMembers);
        await CleanupDatabase(x => x.StudyGroups);

        var userIds = await GetAvailableUserIds(8);
        if (userIds.Count < 8)
            throw new Exception("Not enough users available for testing. Expected at least 8 users.");

        _existingGroup = await CreateStudyGroup("Math Club", Subject.Math, [userIds[0], userIds[1]]);
        await CreateStudyGroup("Physics Lab", Subject.Physics, [userIds[2], userIds[3]]);
        await CreateStudyGroup("Chemistry Study", Subject.Chemistry, [userIds[4], userIds[5]]);
        await CreateStudyGroup("Advanced Math", Subject.Math, [userIds[6], userIds[7]]);
    }

    [Test]
    public async Task List_ReturnsAllGroups_WhenNoFiltersApplied()
    {
        // Act
        var response = await ApiClient.GetAsync("/api/studygroups");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var groups = await response.Content.ReadFromJsonAsync<List<StudyGroup>>();
        Assert.That(groups!.Count, Is.EqualTo(4));
    }

    [Test]
    public async Task List_ReturnsFilteredGroups_WhenSubjectFilterApplied()
    {
        // Act
        var response = await ApiClient.GetAsync("/api/studygroups?subject=Math");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var groups = await response.Content.ReadFromJsonAsync<List<StudyGroup>>();
        Assert.That(groups!.Count, Is.EqualTo(2));
        Assert.That(groups.All(g => g.Subject == Subject.Math), Is.True);
    }

    [TestCase("Physics")]
    [TestCase("Chemistry")]
    public async Task List_ReturnsFilteredGroups_WhenSpecificSubjectProvided(string subject)
    {
        // Act
        var response = await ApiClient.GetAsync($"/api/studygroups?subject={subject}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var groups = await response.Content.ReadFromJsonAsync<List<StudyGroup>>();
        Assert.That(groups!.Count, Is.EqualTo(1));
        Assert.That(groups.First().Subject, Is.EqualTo(Enum.Parse<Subject>(subject)));
    }

    [Test]
    public async Task List_ReturnsBadRequest_WhenInvalidSubjectProvided()
    {
        // Act
        var response = await ApiClient.GetAsync("/api/studygroups?subject=NonExistentSubject");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoGroupsExist()
    {
        // Arrange - Delete all groups
        await CleanupDatabase(x => x.StudyGroupMembers);
        await CleanupDatabase(x => x.StudyGroups);

        // Act
        var response = await ApiClient.GetAsync("/api/studygroups");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var groups = await response.Content.ReadFromJsonAsync<List<StudyGroup>>();
        Assert.That(groups!.Count, Is.EqualTo(0));
        Assert.That(groups, Is.Not.Null);
    }

    [Test]
    public async Task GetById_ReturnsGroup_WhenGroupExists()
    {
        // Act
        var response = await ApiClient.GetAsync($"/api/studygroups/{_existingGroup.StudyGroupId}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var group = await response.Content.ReadFromJsonAsync<StudyGroup>();
        group.Should().BeEquivalentTo(_existingGroup);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-100)]
    [TestCase(int.MaxValue)]
    public async Task GetById_ReturnsNotFound_WhenGroupDoesNotExist(int id)
    {
        // Act
        var response = await ApiClient.GetAsync($"/api/studygroups/{id}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}