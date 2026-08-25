using FluentValidation;

namespace Application.DataCollections.Commands;

public class CreateFormModuleCommandValidator : AbstractValidator<CreateFormModuleCommand>
{
    public CreateFormModuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();
    }
}