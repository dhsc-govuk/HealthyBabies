namespace Application.Common.FileValidation;

internal static class FileSignatures
{
    private static readonly Dictionary<string, byte[][]> Signatures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } }, // %PDF-
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },

            // OOXML (xlsx/docx/pptx) and any other zip-based format starts with PK\x03\x04 or PK\x05\x06
            [".xlsx"] = new[]
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                new byte[] { 0x50, 0x4B, 0x07, 0x08 },
            },
            [".docx"] = new[]
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                new byte[] { 0x50, 0x4B, 0x07, 0x08 },
            },
        };

    public static bool HasKnownSignature(string extension) =>
        Signatures.ContainsKey(extension);

    public static bool MatchesExtension(string extension, ReadOnlySpan<byte> header)
    {
        if (!Signatures.TryGetValue(extension, out var candidates))
        {
            // Extensions without a binary signature (e.g. .csv) are validated separately.
            return true;
        }

        foreach (var signature in candidates)
        {
            if (header.Length < signature.Length)
            {
                continue;
            }

            if (header[..signature.Length].SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }

    public static bool LooksLikePlainText(ReadOnlySpan<byte> header)
    {
        // CSV files are text — they should never contain NUL bytes or other
        // binary control characters in their leading bytes.
        foreach (var b in header)
        {
            if (b == 0x00)
            {
                return false;
            }

            // Allow tab (0x09), LF (0x0A), CR (0x0D), and printable ASCII through 0x7E,
            // plus the high range used by UTF-8 continuation bytes.
            var isControl = b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D;
            if (isControl)
            {
                return false;
            }
        }

        return true;
    }
}