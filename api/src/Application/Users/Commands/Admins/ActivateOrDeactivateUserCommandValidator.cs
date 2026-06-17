using FluentValidation;

namespace Application.Users.Commands.Admins;

public class ActivateOrDeactivateUserCommandValidator : AbstractValidator<ActivateOrDeactivateUserCommand>
{
    public ActivateOrDeactivateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull();
    }
}