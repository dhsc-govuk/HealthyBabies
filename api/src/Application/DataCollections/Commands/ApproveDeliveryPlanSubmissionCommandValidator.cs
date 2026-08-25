using FluentValidation;

namespace Application.DataCollections.Commands;

public class ApproveDeliveryPlanSubmissionCommandValidator : AbstractValidator<ApproveDeliveryPlanSubmissionCommand>
{
    private const int MaximumCommentLength = 500;

    public ApproveDeliveryPlanSubmissionCommandValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(MaximumCommentLength)
            .WithMessage($"The comment must be {MaximumCommentLength} characters or fewer");
    }
}