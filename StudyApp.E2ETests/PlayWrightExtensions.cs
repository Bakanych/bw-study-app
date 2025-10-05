using System.Text.Json;
using Microsoft.Playwright;

namespace StudyApp.E2ETests;

public static class PlayWrightExtensions
{
    public static T? As<T>(this IAPIResponse response)
    {
        return response.JsonAsync<T>(new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        }).GetAwaiter().GetResult();
    } 
    
}