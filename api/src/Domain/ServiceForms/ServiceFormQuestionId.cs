namespace Domain.ServiceForms;

public record ServiceFormQuestionId(Guid Value)
{
    public static ServiceFormQuestionId Empty() => new(Guid.Empty);
    public static ServiceFormQuestionId New() => new(Guid.NewGuid());
    public static ServiceFormQuestionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}