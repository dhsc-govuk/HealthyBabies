using System.Text.RegularExpressions;
using FluentValidation;

namespace Application.Organisations.Commands;

public class UpdateOrganisationCommandValidator : AbstractValidator<UpdateOrganisationCommand>
{
    private static readonly Regex OnsCodePattern = new(
        @"^[EWSN]\d{8}$",
        RegexOptions.Compiled);

    public UpdateOrganisationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(255);

        RuleFor(x => x.ONSCode)
            .NotEmpty()
            .WithMessage("ONS Code is required.");

        RuleFor(x => x.ONSCode)
            .Must(BeValidOnsCode)
            .When(x => !string.IsNullOrEmpty(x.ONSCode))
            .WithMessage("ONS Code must be in format: letter (E, W, S, or N) followed by 8 digits (e.g., E06000001).");
    }

    private static bool BeValidOnsCode(string onsCode)
    {
        return OnsCodePattern.IsMatch(onsCode);
    }
}