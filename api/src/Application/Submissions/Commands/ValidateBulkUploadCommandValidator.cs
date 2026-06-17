using FluentValidation;

namespace Application.Submissions.Commands;

public class ValidateBulkUploadCommandValidator : AbstractValidator<ValidateBulkUploadCommand>
{
    public ValidateBulkUploadCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty()
            .WithMessage("Submission ID is required");

        RuleFor(x => x.ModuleId)
            .NotEmpty()
            .WithMessage("Module ID is required");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File is required");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .Must(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                          name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must be CSV or Excel format");
    }
}