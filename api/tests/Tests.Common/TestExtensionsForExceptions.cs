using System.Text.Json;
using FluentAssertions;
using Xunit.Abstractions;

namespace Tests.Common;

/// <summary>
/// Extension methods for better exception handling and debugging in integration tests.
/// </summary>
public static class TestExtensionsForExceptions
{
    /// <summary>
    /// Executes an async action and captures detailed exception information on failure.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task ExecuteWithDetailedExceptionAsync(
        this ITestOutputHelper output,
        Func<Task> action,
        string testContext = "")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(output, ex, testContext);
            throw; // Re-throw to maintain test failure
        }
    }

    /// <summary>
    /// Executes an async function and captures detailed exception information on failure.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task<T> ExecuteWithDetailedExceptionAsync<T>(
        this ITestOutputHelper output,
        Func<Task<T>> func,
        string testContext = "")
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(output, ex, testContext);
            throw; // Re-throw to maintain test failure
        }
    }

    /// <summary>
    /// Enhanced HTTP response assertion with detailed error information.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task ShouldBeSuccessfulWithDetailsAsync(
        this HttpResponseMessage response,
        ITestOutputHelper output)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var errorDetails = $"""
                HTTP Request Failed:
                Status Code: {response.StatusCode} ({(int)response.StatusCode})
                Reason Phrase: {response.ReasonPhrase}
                Response Content: {content}
                Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}
                """;

            output.WriteLine("=== HTTP RESPONSE ERROR ===");
            output.WriteLine(errorDetails);
            output.WriteLine("=== END HTTP RESPONSE ERROR ===");
        }

        response.IsSuccessStatusCode.Should().BeTrue($"Expected successful response but got {response.StatusCode}: {response.ReasonPhrase}");
    }

    /// <summary>
    /// Enhanced database operation with exception details.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task SaveChangesWithDetailsAsync(
        this BaseIntegrationTest test,
        ITestOutputHelper output,
        string operation = "SaveChanges")
    {
        try
        {
            await test.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(output, ex, $"Database {operation}");
            throw;
        }
    }

    /// <summary>
    /// Enhanced JSON deserialization with detailed error information.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public static async Task<T> ToResponseModelWithDetailsAsync<T>(
        this HttpResponseMessage response,
        ITestOutputHelper output)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                output.WriteLine($"Warning: Empty response content when deserializing to {typeof(T).Name}");
                return default(T);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<T>(content, options);
        }
        catch (JsonException jsonEx)
        {
            var content = await response.Content.ReadAsStringAsync();
            var errorMessage = $"""
                JSON Deserialization Failed:
                Target Type: {typeof(T).FullName}
                Response Content: {content}
                Content Length: {content?.Length ?? 0}
                Response Status: {response.StatusCode}
                """;

            output.WriteLine("=== JSON DESERIALIZATION ERROR ===");
            output.WriteLine(errorMessage);
            ExceptionHelper.LogException(output, jsonEx, "JSON Deserialization");
            output.WriteLine("=== END JSON DESERIALIZATION ERROR ===");
            throw;
        }
        catch (Exception ex)
        {
            ExceptionHelper.LogException(output, ex, "Response Model Conversion");
            throw;
        }
    }
}