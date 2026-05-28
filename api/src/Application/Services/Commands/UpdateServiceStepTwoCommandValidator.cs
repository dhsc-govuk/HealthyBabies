using FluentValidation;

namespace Application.Services.Commands;

public class UpdateServiceStepTwoCommandValidator : AbstractValidator<UpdateServiceStepTwoCommand>
{
    public UpdateServiceStepTwoCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required");

        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers are required");
    }
}