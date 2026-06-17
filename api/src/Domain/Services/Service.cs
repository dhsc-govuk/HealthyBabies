using Domain.Common;
using Domain.Organisations;
using Domain.ServiceForms;

namespace Domain.Services;

public class Service : SoftDeletableEntity<ServiceId>
{
    private readonly List<ServiceAnswer> _answers = new();

    public OrganisationId OrganisationId { get; private set; }
    public string Name { get; private set; }
    public ServiceStatus Status { get; private set; }
    public int CurrentStep { get; private set; }

    // Navigation properties
    public Organisation? Organisation { get; private set; }
    public IReadOnlyCollection<ServiceAnswer> Answers => _answers.AsReadOnly();

    private Service()
    {
        OrganisationId = OrganisationId.Empty();
        Name = string.Empty;
    }

    private Service(
        ServiceId id,
        OrganisationId organisationId,
        string name,
        ServiceStatus status,
        int currentStep)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        Status = status;
        CurrentStep = currentStep;
    }

    public static Service New(
        ServiceId id,
        OrganisationId organisationId,
        string name)
    {
        return new Service(id, organisationId, name, ServiceStatus.Draft, 1);
    }

    public void UpdateName(string name)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Name = name;
    }

    public void AdvanceToStep(int step)
    {
        Guard.GreaterThan(step, 0, nameof(step));
        Guard.LessThanOrEqual(step, 3, nameof(step));

        if (step > CurrentStep)
        {
            CurrentStep = step;
        }
    }

    public void Complete()
    {
        Status = ServiceStatus.Complete;
    }

    public void RevertToDraft()
    {
        if (Status == ServiceStatus.Complete)
        {
            Status = ServiceStatus.Draft;
            CurrentStep = 1;
        }
    }

    public ServiceAnswer AddAnswer(
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot = null)
    {
        var answer = ServiceAnswer.New(
            ServiceAnswerId.New(),
            Id,
            questionCode,
            questionLabel,
            questionHint,
            questionType,
            step,
            displayOrder,
            value,
            displayValue,
            optionsSnapshot);
        _answers.Add(answer);
        return answer;
    }

    public void UpdateAnswer(string questionCode, string? value, string? displayValue)
    {
        var answer = _answers.FirstOrDefault(a => a.QuestionCode == questionCode);
        if (answer != null)
        {
            answer.UpdateAnswer(value, displayValue);
        }
    }

    public ServiceAnswer? GetAnswer(string questionCode)
    {
        return _answers.FirstOrDefault(a => a.QuestionCode == questionCode);
    }

    public void ClearAnswersForStep(int step)
    {
        _answers.RemoveAll(a => a.Step == step);
    }
}