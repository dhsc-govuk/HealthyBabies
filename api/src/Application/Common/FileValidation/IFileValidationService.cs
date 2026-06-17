using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace Application.Common.FileValidation;

public sealed record ValidatedFile(
    string OriginalFileName,
    string SafeFileName,
    string SafeExtension,
    string ContentType,
    long Length,
    Stream Content);

public interface IFileValidationService
{
    Task<Either<FileValidationError, ValidatedFile>> ValidateAsync(
        IFormFile file,
        FileUploadProfile profile,
        CancellationToken cancellationToken = default);

    Task<Either<FileValidationError, ValidatedFile>> ValidateAsync(
        Stream content,
        string fileName,
        long length,
        FileUploadProfile profile,
        CancellationToken cancellationToken = default);
}