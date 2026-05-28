using FluentValidation;

namespace Application.Services.Commands;

public class UpdateServiceStepOneCommandValidator : AbstractValidator<UpdateServiceStepOneCommand>
{
    public UpdateServiceStepOneCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(255).WithMessage("Service name must not exceed 255 characters");

        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers are required");
    }
}