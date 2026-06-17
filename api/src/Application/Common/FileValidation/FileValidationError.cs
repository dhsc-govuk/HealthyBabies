namespace Application.Common.FileValidation;

public abstract record FileValidationError(string Message)
{
    public sealed record EmptyFile() : FileValidationError("File is empty or missing");

    public sealed record UnsafeFilename(string Reason)
        : FileValidationError($"File name is not allowed: {Reason}");

    public sealed record InvalidExtension(string Extension, IReadOnlyCollection<string> Allowed)
        : FileValidationError(
            $"File type '{Extension}' is not allowed. Allowed types: {string.Join(", ", Allowed)}");

    public sealed record BlockedExtension(string Extension)
        : FileValidationError($"File type '{Extension}' is not allowed for security reasons");

    public sealed record TooLarge(long ActualBytes, long MaxBytes)
        : FileValidationError(
            $"File is too large ({ActualBytes / 1024} KB). Maximum allowed size is {MaxBytes / 1024} KB");

    public sealed record SignatureMismatch(string Extension)
        : FileValidationError(
            $"File contents do not match the declared file type '{Extension}'");

    public sealed record MalwareDetected()
        : FileValidationError("File rejected: malware signature detected");

    public sealed record MacrosDetected()
        : FileValidationError("File rejected: Office documents containing macros are not allowed");

    public sealed record EmbeddedScriptDetected()
        : FileValidationError("File rejected: file contains embedded script content");
}