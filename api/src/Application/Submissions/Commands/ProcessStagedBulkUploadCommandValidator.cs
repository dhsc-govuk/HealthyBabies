using FluentValidation;

namespace Application.Submissions.Commands;

public class ProcessStagedBulkUploadCommandValidator : AbstractValidator<ProcessStagedBulkUploadCommand>
{
    public ProcessStagedBulkUploadCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty()
            .WithMessage("Submission ID is required");

        RuleFor(x => x.ModuleId)
            .NotEmpty()
            .WithMessage("Module ID is required");

        RuleFor(x => x.StagingId)
            .NotEmpty()
            .WithMessage("Staging ID is required");

        RuleFor(x => x.SelectedServiceNames)
            .NotEmpty()
            .WithMessage("At least one service must be selected");

        RuleForEach(x => x.CellEdits).ChildRules(edit =>
        {
            edit.RuleFor(e => e.RowIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Row index must be non-negative");

            edit.RuleFor(e => e.ColumnIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Column index must be non-negative");
        });
    }
}