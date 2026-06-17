using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Exceptions;
using Application.SiteForms.Dtos;
using Domain.Common;
using Domain.Locations;
using Domain.SiteForms;
using LanguageExt;
using MediatR;
using Location = Domain.Locations.Location;

namespace Application.Organisations.Commands;

public record UpdateLocationCommand : IRequest<Either<LocationException, Location>>
{
    public Guid Id { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<SiteAnswerInputDto> Answers { get; init; } = [];

    // Predefined question codes
    private const string PredefinedNameCode = "FHS01";
    private const string PredefinedPostCodeCode = "FHS02";
    private const string PredefinedReferenceNumberCode = "FHS03";

    private const string PredefinedAddressLine1Code = "FHS16";

    private const string PredefinedAddressLine2Code = "FHS13";
    private const string PredefinedTownOrCityCode = "FHS14";
    private const string PredefinedCountyCode = "FHS15";

    public string GetName() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedNameCode)?.Value ?? string.Empty;
    public string GetPostCode() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedPostCodeCode)?.Value ?? string.Empty;
    public string GetReferenceNumber() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedReferenceNumberCode)?.Value ?? string.Empty;
    public string GetAddressLine1() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedAddressLine1Code)?.Value ?? string.Empty;
    public string GetAddressLine2() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedAddressLine2Code)?.Value ?? string.Empty;
    public string GetTownOrCity() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedTownOrCityCode)?.Value ?? string.Empty;
    public string GetCounty() => Answers.FirstOrDefault(a => a.QuestionCode == PredefinedCountyCode)?.Value ?? string.Empty;
}

public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, Either<LocationException, Location>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly PermissionsService _permissionsService;
    private readonly ISiteFormQuestionQueries _siteFormQuestionQueries;

    public UpdateLocationCommandHandler(
        ILocationRepository locationRepository,
        PermissionsService permissionsService,
        ISiteFormQuestionQueries siteFormQuestionQueries)
    {
        Guard.NotNull(locationRepository, nameof(locationRepository));
        Guard.NotNull(permissionsService, nameof(permissionsService));
        Guard.NotNull(siteFormQuestionQueries, nameof(siteFormQuestionQueries));
        _locationRepository = locationRepository;
        _permissionsService = permissionsService;
        _siteFormQuestionQueries = siteFormQuestionQueries;
    }

    public async Task<Either<LocationException, Location>> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken)
    {
        var locationId = new LocationId(request.Id);
        var locationResult = await GetLocation(locationId, cancellationToken);
        return await locationResult.BindAsync(l =>
            FindDuplicate(l, request, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> FindDuplicate(
      Location location,
      UpdateLocationCommand request,
      CancellationToken cancellationToken)
    {
        var name = request.GetName();
        var locationWithName = await _locationRepository.FindDuplicateAsync(
            name,
            location.OrganisationId,
            location.Id,
            cancellationToken);

        return await locationWithName.MatchAsync(
            l => new LocationAlreadyExistsException(l.Id, name),
            () => FindDuplicateByReferenceNumber(location, request, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> FindDuplicateByReferenceNumber(
      Location location,
      UpdateLocationCommand request,
      CancellationToken cancellationToken)
    {
        var referenceNumber = request.GetReferenceNumber();
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            return await UpdateLocation(location, request, cancellationToken);
        }

        var locationWithReferenceNumber = await _locationRepository.FindDuplicateByReferenceNumberAsync(
            referenceNumber,
            location.OrganisationId,
            location.Id,
            cancellationToken);

        return await locationWithReferenceNumber.MatchAsync(
            l => new LocationReferenceNumberAlreadyExistsException(l.Id, referenceNumber),
            () => UpdateLocation(location, request, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> UpdateLocation(
      Location location,
      UpdateLocationCommand request,
      CancellationToken cancellationToken)
    {
        try
        {
            location.UpdateName(request.GetName());
            location.UpdatePostCode(request.GetPostCode());
            location.UpdateReferenceNumber(request.GetReferenceNumber());
            location.UpdateAddressLine1(request.GetAddressLine1());
            location.UpdateAddressLine2(request.GetAddressLine2());
            location.UpdateTownOrCity(request.GetTownOrCity());
            location.UpdateCounty(request.GetCounty());
            location.SetActive(request.IsActive);

            // Update answers
            await UpdateAnswers(location, request.Answers, cancellationToken);

            return await _locationRepository.UpdateAsync(location, cancellationToken);
        }
        catch (Exception exception)
        {
            return new LocationUnknownException(location.Id, exception);
        }
    }

    private async Task UpdateAnswers(
        Location location,
        IReadOnlyList<SiteAnswerInputDto> answers,
        CancellationToken cancellationToken)
    {
        var questions = await _siteFormQuestionQueries.GetAllActive(cancellationToken);

        foreach (var answerInput in answers)
        {
            var question = questions.FirstOrDefault(q => q.Code == answerInput.QuestionCode);
            if (question == null)
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, answerInput.Value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            var existingAnswer = location.GetAnswer(answerInput.QuestionCode);
            if (existingAnswer != null)
            {
                existingAnswer.UpdateAnswer(answerInput.Value, displayValue);
                existingAnswer.UpdateSnapshot(
                    question.Label,
                    question.Hint,
                    question.QuestionType,
                    question.DisplayOrder,
                    optionsSnapshot);
            }
            else
            {
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
        }
    }

    private static string? GetDisplayValue(SiteFormQuestion question, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (question.QuestionType == SiteFormQuestionType.Checkbox)
        {
            var values = value.Split(',');
            var labels = values
                .Select(v => question.Options.FirstOrDefault(o => o.Value == v)?.Label ?? v)
                .ToList();
            return string.Join(", ", labels);
        }

        if (question.Options.Any())
        {
            var option = question.Options.FirstOrDefault(o => o.Value == value);
            return option?.Label ?? value;
        }

        return value;
    }

    private Task<Either<LocationException, Location>> GetLocation(
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        var permissions = _permissionsService.GetLocationPermissions();

        return permissions.MatchAsync(
            async p => (await _locationRepository.GetByIdForUpdate(locationId, p, cancellationToken))
                .Match<Either<LocationException, Location>>(
                    l => l,
                    () => new LocationDoesNotExistException(locationId)),
            e => new LocationArgumentException(locationId, e.Message));
    }
}