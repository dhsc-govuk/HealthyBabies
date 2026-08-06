using System.IO.Compression;
using LanguageExt;

namespace Application.Common.FileValidation;

public sealed record ExtractedZipEntry(Stream Content, string FileName, long Length);

public static class BulkUploadZipExtractor
{
    public static bool IsZip(string fileName) =>
        Path.GetExtension(fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    // The zipped entry is validated against the same profile the caller would otherwise validate an
    // unzipped upload against, so a zipped .xlsx is only accepted where a plain .xlsx would be too.
    public static Either<FileValidationError, ExtractedZipEntry> ExtractSingleEntry(
        Stream zipContent,
        FileUploadProfile profile)
    {
        var profileSettings = FileUploadProfiles.For(profile);
        var maxDecompressedSizeBytes = profileSettings.MaxSizeBytes;

        try
        {
            using var archive = new ZipArchive(zipContent, ZipArchiveMode.Read, leaveOpen: true);
            var entries = archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name)).ToList();

            if (entries.Count != 1)
            {
                return new FileValidationError.UnsafeFilename("zip archive must contain exactly one file");
            }

            var entry = entries[0];
            var extension = Path.GetExtension(entry.Name);

            if (!profileSettings.AllowedExtensions.Contains(extension))
            {
                return new FileValidationError.InvalidExtension(extension, profileSettings.AllowedExtensions);
            }

            if (entry.Length > maxDecompressedSizeBytes)
            {
                return new FileValidationError.TooLarge(entry.Length, maxDecompressedSizeBytes);
            }

            using var entryStream = entry.Open();
            var buffer = new MemoryStream();

            // Bounded copy rather than a plain CopyTo: a zip can under-declare its entry size in the
            // central directory while the actual deflate stream decompresses to far more bytes. Capping
            // the read loop itself (not just checking the size before/after) is what actually stops a
            // zip-bomb from exhausting memory here, regardless of what the archive claims.
            if (!TryBoundedCopy(entryStream, buffer, maxDecompressedSizeBytes))
            {
                return new FileValidationError.TooLarge(maxDecompressedSizeBytes + 1, maxDecompressedSizeBytes);
            }

            buffer.Position = 0;
            return new ExtractedZipEntry(buffer, entry.Name, buffer.Length);
        }
        catch (InvalidDataException)
        {
            return new FileValidationError.SignatureMismatch(".zip");
        }
    }

    private static bool TryBoundedCopy(Stream source, Stream destination, long maxBytes)
    {
        var readBuffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = source.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            totalRead += read;
            if (totalRead > maxBytes)
            {
                return false;
            }

            destination.Write(readBuffer, 0, read);
        }

        return true;
    }
}