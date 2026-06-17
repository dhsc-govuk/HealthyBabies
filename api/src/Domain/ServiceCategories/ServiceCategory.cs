using Domain.Organisations;
using Domain.ServiceCategoryForms;

namespace Domain.ServiceCategories;

public class ServiceCategory : AuditableEntity<ServiceCategoryId>
{
    public OrganisationId OrganisationId { get; private set; }
    public Organisation? Organisation { get; private set; }

    public string CategoryCode { get; private set; }
    public string CategoryName { get; private set; }
    public ServiceCategoryStatus Status { get; private set; }
    public int CurrentStep { get; private set; }

    private readonly List<ServiceCategoryAnswer> _answers = [];
    public IReadOnlyCollection<ServiceCategoryAnswer> Answers => _answers.AsReadOnly();

    private ServiceCategory()
    {
    }

    private ServiceCategory(
        ServiceCategoryId id,
        OrganisationId organisationId,
        string categoryCode,
        string categoryName)
    {
        Id = id;
        OrganisationId = organisationId;
        CategoryCode = categoryCode;
        CategoryName = categoryName;
        Status = ServiceCategoryStatus.Draft;
        CurrentStep = 1;
    }

    public static ServiceCategory New(
        ServiceCategoryId id,
        OrganisationId organisationId,
        string categoryCode,
        string categoryName)
        => new(id, organisationId, categoryCode, categoryName);

    public void AddAnswer(
        string questionCode,
        string questionLabel,
        string? questionHint,
        ServiceCategoryFormQuestionType questionType,
        int step,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot)
    {
        var answer = ServiceCategoryAnswer.New(
            ServiceCategoryAnswerId.New(),
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
    }

    public ServiceCategoryAnswer? GetAnswer(string questionCode)
        => _answers.FirstOrDefault(a => a.QuestionCode == questionCode);

    public void AdvanceToStep(int step)
    {
        if (step > CurrentStep)
        {
            CurrentStep = step;
        }
    }

    public void Complete()
    {
        Status = ServiceCategoryStatus.Complete;
    }

    public void RevertToDraft()
    {
        if (Status == ServiceCategoryStatus.Complete)
        {
            Status = ServiceCategoryStatus.Draft;
        }
    }
}