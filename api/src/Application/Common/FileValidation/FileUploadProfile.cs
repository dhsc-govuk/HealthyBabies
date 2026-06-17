namespace Application.Common.FileValidation;

public enum FileUploadProfile
{
    SubmissionAttachment,
    BulkUploadCsv,
    BulkUploadCsvOrExcel,
}

public sealed record FileUploadProfileSettings(
    IReadOnlyCollection<string> AllowedExtensions,
    long MaxSizeBytes,
    string DefaultContentType);

public static class FileUploadProfiles
{
    private const long MegaByte = 1024L * 1024L;

    private static readonly IReadOnlyDictionary<FileUploadProfile, FileUploadProfileSettings> Settings =
        new Dictionary<FileUploadProfile, FileUploadProfileSettings>
        {
            [FileUploadProfile.SubmissionAttachment] = new(
                AllowedExtensions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".pdf", ".csv", ".xlsx", ".docx", ".png", ".jpg", ".jpeg",
                },
                MaxSizeBytes: 25 * MegaByte,
                DefaultContentType: "application/octet-stream"),

            [FileUploadProfile.BulkUploadCsv] = new(
                AllowedExtensions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv" },
                MaxSizeBytes: 5 * MegaByte,
                DefaultContentType: "text/csv"),

            [FileUploadProfile.BulkUploadCsvOrExcel] = new(
                AllowedExtensions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv", ".xlsx" },
                MaxSizeBytes: 5 * MegaByte,
                DefaultContentType: "application/octet-stream"),
        };

    public static FileUploadProfileSettings For(FileUploadProfile profile) => Settings[profile];

    public static readonly IReadOnlyCollection<string> AlwaysBlockedExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".svg", ".html", ".htm", ".xml", ".xhtml",
            ".php", ".phtml", ".pht", ".php3", ".php4", ".php5", ".phps",
            ".asp", ".aspx", ".cer", ".asa",
            ".jsp", ".jspx",
            ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".ps1", ".sh", ".vbs", ".vbe",
            ".js", ".jse", ".wsf", ".wsh",
            ".xlsm", ".docm", ".pptm", ".dotm", ".xltm",
            ".jar", ".class",
            ".lnk", ".scr", ".reg",
        };
}