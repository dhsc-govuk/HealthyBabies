using System.Text.Json;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Exceptions;
using Application.SiteForms.Dtos;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;
using LanguageExt;
using MediatR;
using Location = Domain.Locations.Location;

namespace Application.Organisations.Commands;

public record CreateLocationCommand : IRequest<Either<LocationException, Location>>
{
    public Guid OrganisationId { get; init; }
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

public class CreateLocationCommandHandler(
    PermissionsService permissionsService,
    IOrganisationRepository organisationRepository,
    ILocationRepository locationRepository,
    ISiteFormQuestionQueries siteFormQuestionQueries)
    : IRequestHandler<CreateLocationCommand, Either<LocationException, Location>>
{
    public async Task<Either<LocationException, Location>> Handle(
        CreateLocationCommand request,
        CancellationToken cancellationToken)
    {
        var organisationId = new OrganisationId(request.OrganisationId);
        var organisationResult = await GetOrganisation(organisationId, cancellationToken);
        return await organisationResult.BindAsync(_ =>
            CheckIfExist(request, organisationId, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> CheckIfExist(
        CreateLocationCommand request,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var name = request.GetName();
        var locationWithName = await locationRepository.FindByNameAsync(
            name,
            organisationId,
            cancellationToken);

        return await locationWithName.MatchAsync(
            l => new LocationAlreadyExistsException(l.Id, name),
            () => CheckIfReferenceNumberExist(request, organisationId, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> CheckIfReferenceNumberExist(
        CreateLocationCommand request,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var referenceNumber = request.GetReferenceNumber();
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            return await CreateLocation(request, organisationId, cancellationToken);
        }

        var locationWithReferenceNumber = await locationRepository.FindByReferenceNumberAsync(
            referenceNumber,
            organisationId,
            cancellationToken);

        return await locationWithReferenceNumber.MatchAsync(
            l => new LocationReferenceNumberAlreadyExistsException(l.Id, referenceNumber),
            () => CreateLocation(request, organisationId, cancellationToken));
    }

    private async Task<Either<LocationException, Location>> CreateLocation(
        CreateLocationCommand request,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var referenceNumber = request.GetReferenceNumber();
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                referenceNumber = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            }

            var location = Location.New(
                LocationId.New(),
                organisationId,
                request.GetName(),
                request.GetPostCode(),
                referenceNumber,
                request.GetAddressLine1(),
                request.GetAddressLine2(),
                request.GetTownOrCity(),
                request.GetCounty(),
                request.IsActive);

            // Add answers
            await AddAnswersToLocation(location, request.Answers, cancellationToken);

            return await locationRepository.AddAsync(location, cancellationToken);
        }
        catch (Exception exception)
        {
            return new LocationUnknownException(LocationId.Empty(), exception);
        }
    }

    private async Task AddAnswersToLocation(
        Location location,
        IReadOnlyList<SiteAnswerInputDto> answers,
        CancellationToken cancellationToken)
    {
        var questions = await siteFormQuestionQueries.GetAllActive(cancellationToken);

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

    private Task<Either<LocationException, LanguageExt.Unit>> GetOrganisation(
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return permissions.MatchAsync(
            async p => (await organisationRepository.GetOrganisationById(organisationId, p, cancellationToken))
                .Match<Either<LocationException, LanguageExt.Unit>>(
                    _ => LanguageExt.Unit.Default,
                    () => new LocationOrganisationDoesNotExistException(LocationId.Empty(), organisationId)),
            e => new LocationArgumentException(LocationId.Empty(), e.Message));
    }
}