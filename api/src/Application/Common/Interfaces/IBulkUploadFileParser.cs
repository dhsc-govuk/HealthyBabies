using Microsoft.AspNetCore.Http;

namespace Application.Common.Interfaces;

public interface IBulkUploadFileParser
{
    bool CanParse(IFormFile file);
    Task<BulkUploadParseResult> ParseAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public record BulkUploadParseResult(
    bool IsSuccess,
    List<Dictionary<string, string>> Records,
    string? ErrorMessage = null);