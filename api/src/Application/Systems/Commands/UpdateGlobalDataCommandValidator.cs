using FluentValidation;

namespace Application.Systems.Commands;

public class UpdateGlobalDataCommandValidator : AbstractValidator<UpdateGlobalDataCommand>
{
    public UpdateGlobalDataCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Value).NotEmpty();
        RuleFor(x => x.Value).MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}