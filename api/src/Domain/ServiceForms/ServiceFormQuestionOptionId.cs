namespace Domain.ServiceForms;

public record ServiceFormQuestionOptionId(Guid Value)
{
    public static ServiceFormQuestionOptionId Empty() => new(Guid.Empty);
    public static ServiceFormQuestionOptionId New() => new(Guid.NewGuid());
    public static ServiceFormQuestionOptionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}