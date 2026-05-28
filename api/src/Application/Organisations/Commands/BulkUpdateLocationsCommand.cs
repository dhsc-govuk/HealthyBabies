using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Exceptions;
using Application.SiteForms.Dtos;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;
using FluentValidation;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using MediatR;

namespace Application.Organisations.Commands;

public record BulkUpdateLocationsCommand : IRequest<Either<LocationException, BulkUpdateLocationsResult>>
{
    public Guid OrganisationId { get; init; }
    public IReadOnlyList<LocationBulkUpdateItem> Locations { get; init; } = [];
}

public record LocationBulkUpdateItem(
    Guid? LocationId,
    string Name,
    IReadOnlyList<SiteAnswerInputDto> Answers);

public record BulkUpdateLocationsResult(
    int TotalCount,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<BulkUpdateLocationResult> Results);

public record BulkUpdateLocationResult(
    Guid? LocationId,
    string SiteName,
    bool IsSuccess,
    bool IsNew,
    string? ErrorMessage = null);

public class BulkUpdateLocationsCommandValidator : AbstractValidator<BulkUpdateLocationsCommand>
{
    public BulkUpdateLocationsCommandValidator()
    {
        RuleFor(x => x.Locations)
            .NotEmpty()
            .WithMessage("At least one location must be provided for bulk update");

        RuleForEach(x => x.Locations)
            .ChildRules(location =>
            {
                location.RuleFor(l => l.Name)
                    .NotEmpty()
                    .WithMessage("Site name is required");

                location.RuleFor(l => l.Answers)
                    .NotEmpty()
                    .WithMessage("At least one answer is required");
            });
    }
}

public class BulkUpdateLocationsCommandHandler(
    PermissionsService permissionsService,
    ILocationRepository locationRepository,
    ISiteFormQuestionQueries questionQueries)
    : IRequestHandler<BulkUpdateLocationsCommand, Either<LocationException, BulkUpdateLocationsResult>>
{
    private const string PredefinedNameCode = "FHS01";
    private const string PredefinedPostCodeCode = "FHS02";
    private const string PredefinedReferenceNumberCode = "FHS03";

    public async Task<Either<LocationException, BulkUpdateLocationsResult>> Handle(
        BulkUpdateLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await ProcessBulkUpdate(p, request, cancellationToken),
            e => new LocationArgumentException(LocationId.Empty(), e.Message));
    }

    private async Task<Either<LocationException, BulkUpdateLocationsResult>> ProcessBulkUpdate(
        Permission permission,
        BulkUpdateLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var results = new List<BulkUpdateLocationResult>();
        var allQuestions = await questionQueries.GetAllActive(cancellationToken);
        var organisationId = new OrganisationId(request.OrganisationId);

        foreach (var item in request.Locations)
        {
            var result = await ProcessSingleLocation(permission, item, allQuestions, organisationId, cancellationToken);
            results.Add(result);
        }

        var successCount = results.Count(r => r.IsSuccess);
        var errorCount = results.Count(r => !r.IsSuccess);

        return new BulkUpdateLocationsResult(
            results.Count,
            successCount,
            errorCount,
            results);
    }

    private async Task<BulkUpdateLocationResult> ProcessSingleLocation(
        Permission permission,
        LocationBulkUpdateItem item,
        IReadOnlyList<SiteFormQuestion> allQuestions,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (item.LocationId.HasValue)
            {
                return await UpdateExistingLocation(permission, item, allQuestions, cancellationToken);
            }
            else
            {
                return await CreateNewLocation(item, allQuestions, organisationId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return new BulkUpdateLocationResult(
                item.LocationId,
                item.Name,
                false,
                false,
                ex.Message);
        }
    }

    private async Task<BulkUpdateLocationResult> UpdateExistingLocation(
        Permission permission,
        LocationBulkUpdateItem item,
        IReadOnlyList<SiteFormQuestion> allQuestions,
        CancellationToken cancellationToken)
    {
        var locationId = new LocationId(item.LocationId!.Value);
        var locationOption = await locationRepository.GetByIdForUpdate(locationId, permission, cancellationToken);

        if (locationOption.IsNone)
        {
            return new BulkUpdateLocationResult(
                item.LocationId,
                item.Name,
                false,
                false,
                "Location not found or access denied");
        }

        var location = locationOption.ValueUnsafe();

        // Update predefined fields from answers
        var nameAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedNameCode);
        if (!string.IsNullOrWhiteSpace(nameAnswer?.Value))
        {
            location.UpdateName(nameAnswer.Value);
        }

        var postCodeAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedPostCodeCode);
        if (postCodeAnswer != null)
        {
            location.UpdatePostCode(postCodeAnswer.Value ?? string.Empty);
        }

        var referenceNumberAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedReferenceNumberCode);
        if (referenceNumberAnswer != null)
        {
            location.UpdateReferenceNumber(referenceNumberAnswer.Value ?? string.Empty);
        }

        // Clear existing answers and add new ones
        location.ClearAnswers();

        foreach (var answerInput in item.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
            if (question == null)
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, answerInput.Value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            location.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.DisplayOrder,
                answerInput.Value,
                displayValue,
                optionsSnapshot);
        }

        await locationRepository.UpdateAsync(location, cancellationToken);

        return new BulkUpdateLocationResult(
            item.LocationId,
            item.Name,
            true,
            false);
    }

    private async Task<BulkUpdateLocationResult> CreateNewLocation(
        LocationBulkUpdateItem item,
        IReadOnlyList<SiteFormQuestion> allQuestions,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var nameAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedNameCode);
        var name = nameAnswer?.Value ?? item.Name;

        var postCodeAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedPostCodeCode);
        var postCode = postCodeAnswer?.Value ?? string.Empty;

        var referenceNumberAnswer = item.Answers.FirstOrDefault(a => a.QuestionCode == PredefinedReferenceNumberCode);
        var referenceNumber = referenceNumberAnswer?.Value;

        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            referenceNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        var location = Location.New(
            LocationId.New(),
            organisationId,
            name,
            postCode,
            referenceNumber,
            isActive: true);

        foreach (var answerInput in item.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
            if (question == null)
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, answerInput.Value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            location.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.DisplayOrder,
                answerInput.Value,
                displayValue,
                optionsSnapshot);
        }

        await locationRepository.AddAsync(location, cancellationToken);

        return new BulkUpdateLocationResult(
            location.Id.Value,
            name,
            true,
            true);
    }

    private static string? GetDisplayValue(SiteFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == SiteFormQuestionType.Checkbox)
        {
            var values = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim());
            var labels = values
                .Select(v => question.Options.FirstOrDefault(o => o.Value.Equals(v, StringComparison.OrdinalIgnoreCase))?.Label ?? v)
                .ToList();
            return string.Join(", ", labels);
        }

        if (question.Options.Any())
        {
            var option = question.Options.FirstOrDefault(o => o.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            return option?.Label ?? value;
        }

        return value;
    }
}