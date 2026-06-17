using System.IO.Compression;
using System.Text;
using Application.Common.FileValidation;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace Api.Tests.Integration.FileValidation;

public class FileValidationServiceTests
{
    private readonly FileValidationService _service = new();

    private static readonly byte[] EicarBytes = Encoding.ASCII.GetBytes(
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    private static readonly byte[] PdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A };
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0 };

    private async Task<Outcome> ValidateAsync(byte[] bytes, string fileName, FileUploadProfile profile)
    {
        using var stream = new MemoryStream(bytes);
        var result = await _service.ValidateAsync(stream, fileName, bytes.Length, profile);
        ValidatedFile? validated = null;
        FileValidationError? error = null;
        result.IfRight(v => validated = v);
        result.IfLeft(e => error = e);
        return new Outcome(validated, error);
    }

    private record Outcome(ValidatedFile? Validated, FileValidationError? Error);

    [Fact]
    public async Task RejectsEicarString_OnAnyProfile()
    {
        var result = await ValidateAsync(EicarBytes, "report.csv", FileUploadProfile.BulkUploadCsv);
        result.Error.Should().BeOfType<FileValidationError.MalwareDetected>();
    }

    [Fact]
    public async Task RejectsPhpExtension_EvenWhenAllowedListEmpty()
    {
        var result = await ValidateAsync(Encoding.ASCII.GetBytes("<?php echo 1; ?>"), "shell.php", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.BlockedExtension>();
    }

    [Theory]
    [InlineData("shell.php.jpg")]
    [InlineData("shell.phtml")]
    [InlineData("shell.PhP")]
    [InlineData("shell.php5")]
    public async Task RejectsDangerousDoubleExtensions(string fileName)
    {
        var bytes = Concat(JpegHeader, Encoding.ASCII.GetBytes("padding"));
        var result = await ValidateAsync(bytes, fileName, FileUploadProfile.SubmissionAttachment);

        result.Error.Should().BeAssignableTo<FileValidationError>()
            .Which.Should().Match<FileValidationError>(e =>
                e is FileValidationError.BlockedExtension
                || e is FileValidationError.InvalidExtension);
    }

    [Theory]
    [InlineData("malicious.svg")]
    [InlineData("malicious.html")]
    [InlineData("malicious.htm")]
    [InlineData("malicious.xml")]
    public async Task RejectsHtmlAndSvg_OnAttachmentProfile(string fileName)
    {
        var bytes = Encoding.ASCII.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"></svg>");
        var result = await ValidateAsync(bytes, fileName, FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.BlockedExtension>();
    }

    [Fact]
    public async Task RejectsRenamedExecutable_WhenSignatureDoesNotMatchExtension()
    {
        var bytes = Encoding.ASCII.GetBytes("MZ\x90\x00 not actually a PDF");
        var result = await ValidateAsync(bytes, "report.pdf", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.SignatureMismatch>();
    }

    [Fact]
    public async Task RejectsBinaryDisguisedAsCsv()
    {
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        var result = await ValidateAsync(bytes, "data.csv", FileUploadProfile.BulkUploadCsv);
        result.Error.Should().BeOfType<FileValidationError.SignatureMismatch>();
    }

    [Fact]
    public async Task RejectsFileExceedingProfileSizeLimit()
    {
        // BulkUploadCsv profile caps at 5 MB.
        var bytes = new byte[6 * 1024 * 1024];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)'a';
        }

        var result = await ValidateAsync(bytes, "huge.csv", FileUploadProfile.BulkUploadCsv);
        result.Error.Should().BeOfType<FileValidationError.TooLarge>();
    }

    [Fact]
    public async Task RejectsFilenameWithPathTraversal()
    {
        var bytes = PdfHeader;
        var result = await ValidateAsync(bytes, "../../../etc/passwd.pdf", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.UnsafeFilename>();
    }

    [Fact]
    public async Task RejectsFilenameWithNullByte()
    {
        var bytes = PdfHeader;
        var result = await ValidateAsync(bytes, "ok.pdf\0.exe", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.UnsafeFilename>();
    }

    [Fact]
    public async Task AcceptsCleanPdf_OnAttachmentProfile()
    {
        var result = await ValidateAsync(PdfHeader, "report.pdf", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeNull();
        result.Validated.Should().NotBeNull();
        result.Validated!.SafeFileName.Should().EndWith(".pdf").And.NotContain("report");
        result.Validated.OriginalFileName.Should().Be("report.pdf");
        result.Validated.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task AcceptsPng_OnAttachmentProfile()
    {
        var result = await ValidateAsync(PngHeader, "logo.png", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeNull();
        result.Validated!.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task AcceptsCleanCsv_OnBulkUploadCsvProfile()
    {
        var bytes = Encoding.UTF8.GetBytes("col1,col2\nvalue1,value2\n");
        var result = await ValidateAsync(bytes, "data.csv", FileUploadProfile.BulkUploadCsv);
        result.Error.Should().BeNull();
        result.Validated!.ContentType.Should().Be("text/csv");
    }

    [Fact]
    public async Task RejectsOoxmlContainingMacros()
    {
        var docxBytes = BuildOoxmlWithMacros();
        var result = await ValidateAsync(docxBytes, "report.docx", FileUploadProfile.SubmissionAttachment);
        result.Error.Should().BeOfType<FileValidationError.MacrosDetected>();
    }

    [Fact]
    public async Task AcceptsCleanOoxml_WithoutMacros()
    {
        var xlsxBytes = BuildOoxmlWithoutMacros();
        var result = await ValidateAsync(xlsxBytes, "data.xlsx", FileUploadProfile.BulkUploadCsvOrExcel);
        result.Error.Should().BeNull();
        result.Validated!.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static byte[] BuildOoxmlWithMacros()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry("word/vbaProject.bin");
        }

        return memory.ToArray();
    }

    private static byte[] BuildOoxmlWithoutMacros()
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("[Content_Types].xml");
            archive.CreateEntry("xl/workbook.xml");
        }

        return memory.ToArray();
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }
}