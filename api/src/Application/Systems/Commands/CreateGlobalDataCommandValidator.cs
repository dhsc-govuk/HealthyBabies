using FluentValidation;

namespace Application.Systems.Commands;

public class CreateGlobalDataCommandValidator : AbstractValidator<CreateGlobalDataCommand>
{
    public CreateGlobalDataCommandValidator()
    {
        RuleFor(x => x.Entity).NotEmpty();
        RuleFor(x => x.Entity).MaximumLength(255);
        RuleFor(x => x.Value).NotEmpty();
        RuleFor(x => x.Value).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}