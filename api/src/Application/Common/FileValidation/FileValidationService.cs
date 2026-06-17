using System.IO.Compression;
using System.Text;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace Application.Common.FileValidation;

public sealed class FileValidationService : IFileValidationService
{
    private const int HeaderSampleSize = 512;
    private const int FilenameMaxLength = 200;

    // The standardised EICAR anti-malware test signature. See https://www.eicar.org/.
    private static readonly byte[] EicarSignature = Encoding.ASCII.GetBytes(
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    private static readonly string[] ScriptNeedles =
    {
        "<script", "javascript:", "<?php", "<%", "<jsp:", "<svg", "onload=", "onerror=",
    };

    private static readonly string[] OoxmlMacroParts =
    {
        "vbaProject.bin",
    };

    public Task<Either<FileValidationError, ValidatedFile>> ValidateAsync(
        IFormFile file,
        FileUploadProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return Task.FromResult<Either<FileValidationError, ValidatedFile>>(
                new FileValidationError.EmptyFile());
        }

        return ValidateAsync(file.OpenReadStream(), file.FileName, file.Length, profile, cancellationToken);
    }

    public async Task<Either<FileValidationError, ValidatedFile>> ValidateAsync(
        Stream content,
        string fileName,
        long length,
        FileUploadProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (content is null || length <= 0)
        {
            return new FileValidationError.EmptyFile();
        }

        var settings = FileUploadProfiles.For(profile);

        if (length > settings.MaxSizeBytes)
        {
            return new FileValidationError.TooLarge(length, settings.MaxSizeBytes);
        }

        var sanitised = SanitiseFilename(fileName);
        if (sanitised is null)
        {
            return new FileValidationError.UnsafeFilename("contains illegal characters or path segments");
        }

        var extension = Path.GetExtension(sanitised).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return new FileValidationError.UnsafeFilename("missing file extension");
        }

        if (FileUploadProfiles.AlwaysBlockedExtensions.Contains(extension))
        {
            return new FileValidationError.BlockedExtension(extension);
        }

        if (!settings.AllowedExtensions.Contains(extension))
        {
            return new FileValidationError.InvalidExtension(extension, settings.AllowedExtensions);
        }

        // Defence against double-extension bypass: shell.php.jpg → bare name "shell.php"
        // still has a dangerous inner extension.
        var bareName = Path.GetFileNameWithoutExtension(sanitised);
        var innerExtension = Path.GetExtension(bareName).ToLowerInvariant();
        if (!string.IsNullOrEmpty(innerExtension)
            && FileUploadProfiles.AlwaysBlockedExtensions.Contains(innerExtension))
        {
            return new FileValidationError.BlockedExtension(innerExtension);
        }

        var buffer = await ReadHeaderAsync(content, cancellationToken);

        if (ContainsEicar(buffer))
        {
            return new FileValidationError.MalwareDetected();
        }

        if (FileSignatures.HasKnownSignature(extension))
        {
            if (!FileSignatures.MatchesExtension(extension, buffer))
            {
                return new FileValidationError.SignatureMismatch(extension);
            }
        }
        else
        {
            // Text-typed (CSV) files: ensure the leading bytes don't look binary.
            if (!FileSignatures.LooksLikePlainText(buffer))
            {
                return new FileValidationError.SignatureMismatch(extension);
            }
        }

        if (ContainsScriptContent(buffer))
        {
            return new FileValidationError.EmbeddedScriptDetected();
        }

        // Rewind so callers can re-read the full stream from the start.
        var fullStream = await BufferFullStreamAsync(content, buffer, length, cancellationToken);

        if (IsOoxml(extension) && ContainsMacros(fullStream))
        {
            return new FileValidationError.MacrosDetected();
        }

        fullStream.Position = 0;

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var contentType = ResolveContentType(extension, settings.DefaultContentType);

        return new ValidatedFile(
            OriginalFileName: sanitised,
            SafeFileName: safeFileName,
            SafeExtension: extension,
            ContentType: contentType,
            Length: length,
            Content: fullStream);
    }

    private static string? SanitiseFilename(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Reject path separators and parent-directory tokens before any processing.
        if (input.Contains('\0') || input.Contains('/') || input.Contains('\\') || input.Contains(".."))
        {
            return null;
        }

        var trimmed = Path.GetFileName(input).Trim();
        if (trimmed.Length == 0 || trimmed.Length > FilenameMaxLength)
        {
            return null;
        }

        foreach (var c in trimmed)
        {
            if (c < 0x20)
            {
                return null;
            }
        }

        var invalid = Path.GetInvalidFileNameChars();
        if (trimmed.IndexOfAny(invalid) >= 0)
        {
            return null;
        }

        return trimmed;
    }

    private static async Task<byte[]> ReadHeaderAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var buffer = new byte[HeaderSampleSize];
        var read = await content.ReadAsync(buffer.AsMemory(0, HeaderSampleSize), cancellationToken);
        if (read < HeaderSampleSize)
        {
            Array.Resize(ref buffer, read);
        }

        return buffer;
    }

    private static async Task<Stream> BufferFullStreamAsync(
        Stream content,
        byte[] header,
        long length,
        CancellationToken cancellationToken)
    {
        var memory = new MemoryStream(capacity: (int)Math.Min(length, int.MaxValue));
        memory.Write(header, 0, header.Length);
        await content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    private static bool ContainsEicar(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < EicarSignature.Length)
        {
            return false;
        }

        for (var i = 0; i <= buffer.Length - EicarSignature.Length; i++)
        {
            if (buffer.Slice(i, EicarSignature.Length).SequenceEqual(EicarSignature))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsScriptContent(ReadOnlySpan<byte> buffer)
    {
        // Cheap ASCII scan against well-known script markers. We only check the
        // header window — enough to catch SVG/HTML/PHP polyglots smuggled into
        // permitted file types without the cost of decoding the whole file.
        var text = Encoding.ASCII.GetString(buffer).ToLowerInvariant();
        foreach (var needle in ScriptNeedles)
        {
            if (text.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOoxml(string extension) =>
        extension is ".xlsx" or ".docx" or ".pptx";

    private static bool ContainsMacros(Stream fullStream)
    {
        var originalPosition = fullStream.Position;
        try
        {
            fullStream.Position = 0;
            using var archive = new ZipArchive(fullStream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                foreach (var part in OoxmlMacroParts)
                {
                    if (entry.FullName.EndsWith(part, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (InvalidDataException)
        {
            // Not a valid zip — signature check would already have failed.
            return false;
        }
        finally
        {
            fullStream.Position = originalPosition;
        }
    }

    private static string ResolveContentType(string extension, string fallback) =>
        extension switch
        {
            ".pdf" => "application/pdf",
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => fallback,
        };
}