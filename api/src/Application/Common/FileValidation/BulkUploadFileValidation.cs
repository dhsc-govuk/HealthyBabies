using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace Application.Common.FileValidation;

// A zipped upload lets the file bytes reach the API without a WAF/gateway's content-inspection rules
// matching on the raw text - the extracted entry then runs through the exact same profile validation
// as a plain (unzipped) upload would.
public static class BulkUploadFileValidation
{
    public static Task<Either<FileValidationError, ValidatedFile>> ValidateBulkUploadFileAsync(
        this IFileValidationService fileValidationService,
        IFormFile file,
        FileUploadProfile profile,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<Either<FileValidationError, ValidatedFile>>(new FileValidationError.EmptyFile());
        }

        if (!BulkUploadZipExtractor.IsZip(file.FileName))
        {
            return fileValidationService.ValidateAsync(file, profile, cancellationToken);
        }

        return BulkUploadZipExtractor.ExtractSingleEntry(file.OpenReadStream(), profile)
            .MatchAsync(
                async extracted =>
                {
                    await using var extractedContent = extracted.Content;
                    return await fileValidationService.ValidateAsync(
                        extractedContent, extracted.FileName, extracted.Length, profile, cancellationToken);
                },
                error => Task.FromResult<Either<FileValidationError, ValidatedFile>>(error));
    }
}