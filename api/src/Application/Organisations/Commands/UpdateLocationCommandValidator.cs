using FluentValidation;

namespace Application.Organisations.Commands;

public class UpdateLocationCommandValidator
    : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Id)
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