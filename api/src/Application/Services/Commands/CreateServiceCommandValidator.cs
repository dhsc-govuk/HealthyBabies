using FluentValidation;

namespace Application.Services.Commands;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(255).WithMessage("Service name must not exceed 255 characters");
    }
}