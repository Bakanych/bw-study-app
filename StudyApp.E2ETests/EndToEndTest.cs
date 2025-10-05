using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using ReportPortal.Shared.Execution.Logging;
using ReportPortalContext = ReportPortal.Shared.Context;

namespace StudyApp.E2ETests;

[TestFixture]
public abstract class EndToEndTest : PageTest
{
    public IAPIRequestContext Api { get; private set; }

    [SetUp]
    public virtual async Task Setup()
    {
        Api = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = "http://localhost:8080/api/"
        });
    }

    private async Task ExecuteStepAsync(string stepName, Func<Task> action, bool captureScreenshot)
    {
        using var logScope = ReportPortalContext.Current.Log.BeginScope(stepName);
        try
        {
            await action();
            if (captureScreenshot)
            {
                var bytes = await Page.ScreenshotAsync();
                logScope.Info("screenshot", "image/png", bytes);
            }
        }
        catch (Exception e)
        {
            logScope.Status = LogScopeStatus.Failed;
            var attempt = TestContext.CurrentContext.CurrentRepeatCount;
            var error = e.Message;
            await TestContext.Out.WriteLineAsync($"({attempt}) {error}");
            Assert.Fail(error);
        }
    }

    private void Step(string name, Action action, [CallerMemberName] string? caller = null)
    {
        var stepName = $"{caller!.ToUpper()} {name}";
        ExecuteStepAsync(stepName, () =>
        {
            action();
            return Task.CompletedTask;
        }, true).GetAwaiter().GetResult();
    }

    private Task StepAsync(string name, Func<Task> action, [CallerMemberName] string? caller = null)
    {
        var stepName = $"{caller!.ToUpper()} {name}";
        return ExecuteStepAsync(stepName, action, true);
    }

    private string GetStepName(Delegate action)
    {
        var description = action.Method.GetCustomAttributesData()
            .FirstOrDefault(x => x.AttributeType == typeof(DescriptionAttribute))?
            .ConstructorArguments[0].Value?.ToString();

        return description ?? action.Method.Name;
    }

    private void Step(Action action)
    {
        Step(GetStepName(action), action);
    }

    private Task StepAsync(Func<Task> action)
    {
        return StepAsync(GetStepName(action), action);
    }

    protected void Given(string name, Action action)
    {
        Step(name, action);
    }

    protected Task Given(string name, Func<Task> action)
    {
        return StepAsync(name, action);
    }

    protected void Given(Action action)
    {
        Step(action);
    }

    protected Task Given(Func<Task> action)
    {
        return StepAsync(action);
    }

    protected void When(string name, Action action)
    {
        Step(name, action);
    }

    protected Task When(string name, Func<Task> action)
    {
        return StepAsync(name, action);
    }

    protected void When(Action action)
    {
        Step(action);
    }

    protected Task When(Func<Task> action)
    {
        return StepAsync(action);
    }

    protected void Then(string name, Action action)
    {
        Step(name, action);
    }

    protected Task Then(string name, Func<Task> action)
    {
        return StepAsync(name, action);
    }

    protected void Then(Action action)
    {
        Step(action);
    }

    protected Task Then(Func<Task> action)
    {
        return StepAsync(action);
    }
}