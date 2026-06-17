using FluentValidation;

namespace Application.ServiceForms.Commands;

public class ReorderQuestionsCommandValidator : AbstractValidator<ReorderQuestionsCommand>
{
    public ReorderQuestionsCommandValidator()
    {
        RuleFor(x => x.Step)
            .InclusiveBetween(1, 2);

        RuleFor(x => x.Questions)
            .NotEmpty();

        RuleForEach(x => x.Questions).ChildRules(order =>
        {
            order.RuleFor(o => o.Id)
                .NotEmpty();

            order.RuleFor(o => o.DisplayOrder)
                .GreaterThan(0);
        });
    }
}