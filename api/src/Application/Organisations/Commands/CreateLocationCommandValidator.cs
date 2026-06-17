using FluentValidation;

namespace Application.Organisations.Commands;

public class CreateLocationCommandValidator
    : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.OrganisationId)
            .NotEmpty();

        RuleFor(x => x.GetName())
            .NotEmpty()
            .MaximumLength(255)
            .WithName("Name");

        RuleFor(x => x.GetPostCode())
            .MaximumLength(20)
            .WithName("PostCode");

        RuleFor(x => x.GetReferenceNumber())
            .MaximumLength(255)
            .WithName("ReferenceNumber");
    }
}