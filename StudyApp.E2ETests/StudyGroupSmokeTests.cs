using Microsoft.Playwright;
using StudyApp.Models;

namespace StudyApp.E2ETests;

public class StudyGroupSmokeTests : EndToEndTest
{
    private string? _groupName;

    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        await Page.GotoAsync("http://localhost:8080/");
    }

    [Test]
    public async Task CreateStudyGroup()
    {
        int userId;
        _groupName = Random.Shared.Next().ToString();
        const string subject = "Physics";

        await Given("There is a user with no groups", async () =>
        {
            var userIds = Enumerable.Range(1, 10);
            var request = await Api.GetAsync("studygroups");
            var groups = request.As<List<StudyGroup>>();
            var members = groups!.SelectMany(g => g.Members).ToList();
            userId = userIds.First(member => !members.Any(m => m.UserId == member));
        });

        await When($"I create new {subject} study group with name '{_groupName}'", async () =>
        {
            await Page.GetByPlaceholder("Group name").FillAsync(_groupName);
            await Page.Locator("#subject").SelectOptionAsync([subject]);
            await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Create" }).ClickAsync();
        });

        await Then("Group is displayed in the list",
            async () =>
            {
                await Expect(Page.Locator("#groups"))
                    .ToMatchAriaSnapshotAsync($"- cell \"{_groupName}\"");
            });
    }

    [TearDown]
    public async Task TearDown()
    {
        var groupId = (await Api.GetAsync("studygroups"))
            .As<List<StudyGroup>>()!
            .First(x => x.Name == _groupName).StudyGroupId;
        await Api.DeleteAsync($"studygroups/{groupId}");
    }
}