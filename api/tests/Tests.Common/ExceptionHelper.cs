using System.Text;
using Xunit.Abstractions;

namespace Tests.Common;

/// <summary>
/// Helper class for displaying detailed exception information in tests.
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// Gets a detailed string representation of an exception including all inner exceptions.
    /// </summary>
    /// <returns></returns>
    public static string GetDetailedExceptionMessage(Exception exception)
    {
        var sb = new StringBuilder();
        var currentException = exception;
        var level = 0;

        while (currentException != null)
        {
            var indent = new string(' ', level * 2);

            sb.AppendLine($"{indent}Exception Level {level}:");
            sb.AppendLine($"{indent}  Type: {currentException.GetType().FullName}");
            sb.AppendLine($"{indent}  Message: {currentException.Message}");

            if (!string.IsNullOrEmpty(currentException.StackTrace))
            {
                sb.AppendLine($"{indent}  Stack Trace:");
                var stackLines = currentException.StackTrace.Split('\n');
                foreach (var line in stackLines.Take(10)) // Limit to first 10 lines
                {
                    sb.AppendLine($"{indent}    {line.Trim()}");
                }

                if (stackLines.Length > 10)
                {
                    sb.AppendLine($"{indent}    ... ({stackLines.Length - 10} more lines)");
                }
            }

            if (currentException.Data.Count > 0)
            {
                sb.AppendLine($"{indent}  Data:");
                foreach (var key in currentException.Data.Keys)
                {
                    sb.AppendLine($"{indent}    {key}: {currentException.Data[key]}");
                }
            }

            currentException = currentException.InnerException;
            level++;

            if (currentException != null)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Logs detailed exception information to test output.
    /// </summary>
    public static void LogException(ITestOutputHelper? output, Exception exception, string context = "")
    {
        if (output is null)
        {
            return;
        }

        var contextMessage = string.IsNullOrEmpty(context) ? string.Empty : $" in {context}";
        output.WriteLine($"=== EXCEPTION DETAILS{contextMessage} ===");
        output.WriteLine(GetDetailedExceptionMessage(exception));
        output.WriteLine("=== END EXCEPTION DETAILS ===");
    }

    /// <summary>
    /// Creates a detailed assertion failure message with exception details.
    /// </summary>
    /// <returns></returns>
    public static string CreateAssertionMessage(string testContext, Exception exception)
    {
        return $"Test failed in {testContext}:\n{GetDetailedExceptionMessage(exception)}";
    }
}