using Domain.Common;
using FluentValidation;

namespace Application.DataCollections.Commands;

public class ReopenDeliveryPlanSubmissionCommandValidator : AbstractValidator<ReopenDeliveryPlanSubmissionCommand>
{
    private const int MaximumDaysAhead = 90;
    private const int MaximumReasonLength = 500;

    public ReopenDeliveryPlanSubmissionCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A reason is required to reopen a submission")
            .MaximumLength(MaximumReasonLength)
            .WithMessage($"The reason must be {MaximumReasonLength} characters or fewer");

        RuleFor(x => x.ResubmitBy)
            .NotEmpty()
            .WithMessage("Enter the date the local authority should resubmit by")
            .Must(BeInTheFuture)
            .WithMessage("The resubmission date must be in the future")
            .Must(BeWithinNinetyDays)
            .WithMessage($"The resubmission date must be within {MaximumDaysAhead} days of today");
    }

    private static bool BeInTheFuture(DateTime resubmitBy) =>
        UkCalendar.ToUkDate(resubmitBy) > UkCalendar.ToUkDate(DateTime.UtcNow);

    private static bool BeWithinNinetyDays(DateTime resubmitBy) =>
        UkCalendar.ToUkDate(resubmitBy) <= UkCalendar.ToUkDate(DateTime.UtcNow).AddDays(MaximumDaysAhead);
}