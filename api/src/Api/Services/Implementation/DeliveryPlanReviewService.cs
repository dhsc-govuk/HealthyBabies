using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

/// <summary>
/// Read model behind the admin review of a submitted Delivery Plan.
///
/// Answer projection, field ordering and snapshot resolution are deliberately not re-implemented
/// here: <see cref="ISubmissionsControllerService"/> already produces them, is parameterised by
/// organisation rather than reading the caller's claims, and is what the CSV export agrees with. This
/// service injects it and adds only what the review screens need on top — navigation between
/// sections and activities, and the activity and delivery statuses, which are answers rather than
/// workflow state. DeliveryPlanReviewTests guards those reused shapes.
/// </summary>
public class DeliveryPlanReviewService(
    IDataCollectionQueries dataCollectionQueries,
    IOrganisationQueries organisationQueries,
    ISubmissionsControllerService submissions,
    IFormSubmissionRepository formSubmissionRepository)
    : IDeliveryPlanReviewService
{
    private const string ActivityEntityType = "Activity";

    private sealed record ReviewModule(
        Guid FormModuleId,
        string Code,
        int SectionNumber,
        string Name,
        string? Description,
        bool AllowsMultipleActivities);

    private sealed record ReviewContext(
        string DataCollectionName,
        string LocalAuthorityName,
        IReadOnlyList<ReviewModule> Modules);

    public async Task<Option<ReviewSectionDetailDto>> GetSection(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var context = await LoadContext(dataCollectionId, localAuthorityId, cancellationToken);
        if (context == null)
        {
            return Option<ReviewSectionDetailDto>.None;
        }

        var index = IndexOfModule(context.Modules, formModuleId);
        if (index < 0)
        {
            return Option<ReviewSectionDetailDto>.None;
        }

        var module = context.Modules[index];
        var navigation = BuildSectionNavigation(context.Modules, index);

        if (module.AllowsMultipleActivities)
        {
            var activities = await BuildActivityRows(dataCollectionId, localAuthorityId, module, cancellationToken);

            return new ReviewSectionDetailDto(
                FormModuleId: module.FormModuleId,
                Code: module.Code,
                SectionNumber: module.SectionNumber,
                Name: module.Name,
                Description: module.Description,
                DataCollectionName: context.DataCollectionName,
                LocalAuthorityName: context.LocalAuthorityName,
                AllowsMultipleActivities: true,
                AnswerGroups: [],
                Activities: activities,
                Navigation: navigation);
        }

        var detail = await submissions.GetFormModuleById(localAuthorityId, dataCollectionId, formModuleId, cancellationToken);

        return detail.Match(
            form => new ReviewSectionDetailDto(
                FormModuleId: module.FormModuleId,
                Code: module.Code,
                SectionNumber: module.SectionNumber,
                Name: module.Name,
                Description: module.Description,
                DataCollectionName: context.DataCollectionName,
                LocalAuthorityName: context.LocalAuthorityName,
                AllowsMultipleActivities: false,
                AnswerGroups: BuildAnswerGroups(form.Sections, form.Fields),
                Activities: [],
                Navigation: navigation),
            () => Option<ReviewSectionDetailDto>.None);
    }

    public async Task<Option<ReviewActivityDetailDto>> GetActivity(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid formModuleId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var context = await LoadContext(dataCollectionId, localAuthorityId, cancellationToken);
        if (context == null)
        {
            return Option<ReviewActivityDetailDto>.None;
        }

        var index = IndexOfModule(context.Modules, formModuleId);
        if (index < 0 || !context.Modules[index].AllowsMultipleActivities)
        {
            return Option<ReviewActivityDetailDto>.None;
        }

        var module = context.Modules[index];

        // GetActivityForm already scopes by organisation, data collection and module, so an activity
        // id belonging to another submission resolves to None rather than leaking across tenants.
        var form = await submissions.GetActivityForm(localAuthorityId, dataCollectionId, formModuleId, activityId, cancellationToken);
        if (form.IsNone)
        {
            return Option<ReviewActivityDetailDto>.None;
        }

        var activities = await GetActivitySubmissions(dataCollectionId, localAuthorityId, formModuleId, cancellationToken);
        var activityIndex = activities.FindIndex(a => a.Id.Value == activityId);
        var navigation = BuildActivityNavigation(activities, activityIndex);

        return form.Match(
            detail => new ReviewActivityDetailDto(
                ActivityId: detail.ActivityId,
                ActivityName: detail.ActivityName,
                FormModuleId: module.FormModuleId,
                ModuleName: module.Name,
                SectionNumber: module.SectionNumber,
                DataCollectionName: context.DataCollectionName,
                LocalAuthorityName: context.LocalAuthorityName,
                AnswerGroups: BuildAnswerGroups(detail.Sections, detail.Fields),
                Navigation: navigation),
            () => Option<ReviewActivityDetailDto>.None);
    }

    /// <summary>
    /// Resolves the collection, the local authority and the ordered section list, returning null when
    /// either is missing or the collection is not a Delivery Plan. Gating here rather than only in the
    /// UI means a hand-typed review URL for a management information collection returns 404.
    /// </summary>
    private async Task<ReviewContext?> LoadContext(
        Guid dataCollectionId,
        Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        var dataCollectionOption = await dataCollectionQueries.GetByIdWithLocalAuthoritiesAsync(
            new DataCollectionId(dataCollectionId),
            cancellationToken);

        if (dataCollectionOption.IsNone)
        {
            return null;
        }

        var dataCollection = dataCollectionOption.Match(dc => dc, () => null!);
        if (dataCollection.Type.Value != DataCollectionTypeConstants.DeliveryPlan)
        {
            return null;
        }

        var organisationOption = await organisationQueries.GetOrganisationById(
            new OrganisationId(localAuthorityId),
            cancellationToken);

        if (organisationOption.IsNone)
        {
            return null;
        }

        var organisation = organisationOption.Match(o => o, () => null!);

        var modules = dataCollection.FormModuleAssignments
            .Where(fm => fm.FormModule is { IsActive: true })
            .OrderBy(fm => fm.FormModule.SectionNumber)
            .Select(fm => new ReviewModule(
                fm.FormModuleId.Value,
                fm.FormModule.Code,
                fm.FormModule.SectionNumber,
                fm.FormModule.Name,
                fm.FormModule.Description,
                fm.FormModule.AllowsMultipleActivities))
            .ToList();

        return new ReviewContext(dataCollection.Name, organisation.Name, modules);
    }

    private static int IndexOfModule(IReadOnlyList<ReviewModule> modules, Guid formModuleId)
    {
        for (var i = 0; i < modules.Count; i++)
        {
            if (modules[i].FormModuleId == formModuleId)
            {
                return i;
            }
        }

        return -1;
    }

    private async Task<List<FormSubmission>> GetActivitySubmissions(
        Guid dataCollectionId,
        Guid localAuthorityId,
        Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var activities = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            new DataCollectionFormModuleId(formModuleId),
            new OrganisationId(localAuthorityId),
            new DataCollectionId(dataCollectionId),
            ActivityEntityType,
            cancellationToken);

        // CreatedAt ordering matches both the CSV export and the local authority's own activity list,
        // so "3 of 4" on screen means the same activity as the third row of the export.
        return activities.OrderBy(a => a.CreatedAt).ToList();
    }

    private async Task<IReadOnlyList<ReviewActivityRowDto>> BuildActivityRows(
        Guid dataCollectionId,
        Guid localAuthorityId,
        ReviewModule module,
        CancellationToken cancellationToken)
    {
        var activities = await GetActivitySubmissions(dataCollectionId, localAuthorityId, module.FormModuleId, cancellationToken);
        var statusFields = DeliveryPlanActivityStatusFields.ForModule(module.Code);

        return activities
            .Select(activity =>
            {
                var activityStatus = FindAnswer(activity, statusFields?.ActivityStatusFieldKey);
                var deliveryStatus = FindAnswer(activity, statusFields?.DeliveryStatusFieldKey);

                return new ReviewActivityRowDto(
                    ActivityId: activity.Id.Value,
                    ActivityName: string.IsNullOrWhiteSpace(activity.Name) ? "(untitled)" : activity.Name,
                    ActivityStatus: AnswerSnapshotResolver.ResolveDisplayValue(activityStatus, null),
                    ActivityStatusValue: activityStatus?.Value,
                    DeliveryStatus: AnswerSnapshotResolver.ResolveDisplayValue(deliveryStatus, null),
                    DeliveryStatusValue: deliveryStatus?.Value);
            })
            .ToList();
    }

    /// <summary>
    /// Looks the answer up by the question code captured on the value at save time, so a later change
    /// to the form definition cannot repoint a status column at a different question.
    /// </summary>
    private static FormFieldValue? FindAnswer(FormSubmission submission, string? questionCode)
        => questionCode == null
            ? null
            : submission.FieldValues.FirstOrDefault(value => value.QuestionCode == questionCode);

    /// <summary>
    /// Groups answers by form section so each group renders as one summary card, keeping the order
    /// GetFormModuleById and GetActivityForm already applied. Fields belonging to no section, and
    /// modules with no sections at all, collapse into a single untitled group.
    /// </summary>
    private static IReadOnlyList<ReviewAnswerGroupDto> BuildAnswerGroups(
        IReadOnlyList<FormModuleSectionDto> sections,
        IReadOnlyList<FormModuleFieldDto> fields)
    {
        var sectionNumbers = sections.Select(section => section.SectionNumber).ToHashSet();

        var groups = sections
            .OrderBy(section => section.SectionNumber)
            .Select(section => new ReviewAnswerGroupDto(
                SectionId: section.Id,
                Title: section.Title,
                Answers: fields.Where(field => field.StepNumber == section.SectionNumber).Select(ToAnswer).ToList()))
            .Where(group => group.Answers.Count > 0)
            .ToList();

        var ungrouped = fields
            .Where(field => field.StepNumber == null || !sectionNumbers.Contains(field.StepNumber.Value))
            .Select(ToAnswer)
            .ToList();

        if (ungrouped.Count > 0)
        {
            groups.Add(new ReviewAnswerGroupDto(SectionId: null, Title: null, Answers: ungrouped));
        }

        return groups;
    }

    // This screen is read-only - the controller exposes no write path - so it always describes an
    // answer as given, never a question about to be answered. The question text therefore comes from
    // the snapshot taken when the answer was saved: a later relabel in Admin Configuration must not
    // rewrite what the reviewer sees. Note this holds whatever the submission's status is; nothing
    // here gates on Submitted, and a reviewer opening a collection still in progress gets the same
    // treatment. QuestionLabel is null only for rows that predate snapshotting, where the live label
    // is all there is.
    private static ReviewAnswerDto ToAnswer(FormModuleFieldDto field)
        => new(
            QuestionCode: field.Code,
            Question: field.QuestionLabel ?? field.Label,
            Value: field.DisplayValue ?? field.Value,
            FieldType: field.FieldType);

    private static ReviewNavigationDto BuildSectionNavigation(IReadOnlyList<ReviewModule> modules, int index)
        => new(
            Position: index + 1,
            Total: modules.Count,
            Previous: index > 0
                ? new ReviewNavigationLinkDto(modules[index - 1].FormModuleId, modules[index - 1].Name, index)
                : null,
            Next: index < modules.Count - 1
                ? new ReviewNavigationLinkDto(modules[index + 1].FormModuleId, modules[index + 1].Name, index + 2)
                : null);

    private static ReviewNavigationDto BuildActivityNavigation(IReadOnlyList<FormSubmission> activities, int index)
    {
        if (index < 0)
        {
            return new ReviewNavigationDto(Position: 0, Total: activities.Count, Previous: null, Next: null);
        }

        return new ReviewNavigationDto(
            Position: index + 1,
            Total: activities.Count,
            Previous: index > 0
                ? new ReviewNavigationLinkDto(activities[index - 1].Id.Value, activities[index - 1].Name ?? "(untitled)", index)
                : null,
            Next: index < activities.Count - 1
                ? new ReviewNavigationLinkDto(activities[index + 1].Id.Value, activities[index + 1].Name ?? "(untitled)", index + 2)
                : null);
    }
}