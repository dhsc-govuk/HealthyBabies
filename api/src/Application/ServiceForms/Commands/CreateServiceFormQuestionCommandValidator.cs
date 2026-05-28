using FluentValidation;

namespace Application.ServiceForms.Commands;

public class CreateServiceFormQuestionCommandValidator : AbstractValidator<CreateServiceFormQuestionCommand>
{
    public CreateServiceFormQuestionCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[A-Za-z0-9]+$").WithMessage("Code must contain only alphanumeric characters");

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Hint)
            .MaximumLength(1000);

        RuleFor(x => x.Placeholder)
            .MaximumLength(255);

        RuleFor(x => x.QuestionType)
            .InclusiveBetween(0, 4);

        RuleFor(x => x.Step)
            .InclusiveBetween(1, 2);

        RuleFor(x => x.HelpText)
            .MaximumLength(2000);

        RuleFor(x => x.ConditionalQuestionCode)
            .MaximumLength(10);

        RuleFor(x => x.ConditionalValue)
            .MaximumLength(255);

        RuleFor(x => x.Options)
            .NotEmpty()
            .When(x => x.QuestionType is 1 or 2 or 3 && x.IsPredefined)
            .WithMessage("Options are required for Radio, Checkbox, and Select question types when using predefined options");

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Value)
                .NotEmpty()
                .MaximumLength(255);

            option.RuleFor(o => o.Label)
                .NotEmpty()
                .MaximumLength(500);
        });
    }
}