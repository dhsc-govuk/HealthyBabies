using FluentValidation;

namespace Application.Users.Commands.OrganisationUsers;

public class UpdateOrganisationUserCommandValidator : AbstractValidator<UpdateOrganisationUserCommand>
{
    public UpdateOrganisationUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Email).MaximumLength(320);
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.FirstName).MaximumLength(255);
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.LastName).MaximumLength(255);
    }
}