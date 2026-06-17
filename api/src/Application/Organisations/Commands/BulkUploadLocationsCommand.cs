using System.Text.Json;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Organisations.Dtos;
using Application.Organisations.Exceptions;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Locations.Commands;

public record ValidateBulkUploadLocationsCommand : IRequest<Either<LocationException, BulkUploadLocationsResult>>
{
    public required IFormFile File { get; init; }
    public required OrganisationId OrganisationId { get; init; }
}

public class ValidateBulkUploadLocationsCommandHandler(
    PermissionsService permissionsService,
    ILocationRepository locationRepository,
    ISiteFormQuestionQueries siteFormQuestionQueries,
    IEnumerable<IBulkUploadFileParser> fileParsers)
    : IRequestHandler<ValidateBulkUploadLocationsCommand, Either<LocationException, BulkUploadLocationsResult>>
{
    private const string PredefinedNameCode = "FHS01";
    private const string PredefinedPostCodeCode = "FHS02";
    private const string PredefinedReferenceNumberCode = "FHS03";

    public async Task<Either<LocationException, BulkUploadLocationsResult>> Handle(
        ValidateBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetLocationPermissions();
        return await permissions.MatchAsync(
            async p => await ProcessUpload(request, cancellationToken),
            e => Task.FromResult<Either<LocationException, BulkUploadLocationsResult>>(
                new LocationArgumentException(LocationId.Empty(), e.Message)));
    }

    private async Task<Either<LocationException, BulkUploadLocationsResult>> ProcessUpload(
        ValidateBulkUploadLocationsCommand request,
        CancellationToken cancellationToken)
    {
        var parser = fileParsers.FirstOrDefault(p => p.CanParse(request.File));
        if (parser == null)
        {
            return new LocationArgumentException(
                LocationId.Empty(),
                "Unsupported file format. Please upload a CSV (.csv) or Excel (.xlsx) file.");
        }

        var parseResult = await parser.ParseAsync(request.File, cancellationToken);
        if (!parseResult.IsSuccess)
        {
            return new LocationArgumentException(LocationId.Empty(), parseResult.ErrorMessage ?? "Failed to parse file");
        }

        if (parseResult.Records.Count == 0)
        {
            return new LocationArgumentException(LocationId.Empty(), "The file contains no data rows");
        }

        var questions = await siteFormQuestionQueries.GetAllActive(cancellationToken);
        var results = await ProcessBulkUpload(parseResult.Records, request.OrganisationId, questions, cancellationToken);

        return results;
    }

    private async Task<BulkUploadLocationsResult> ProcessBulkUpload(
        List<Dictionary<string, string>> records,
        OrganisationId organisationId,
        IReadOnlyList<SiteFormQuestion> questions,
        CancellationToken cancellationToken)
    {
        var results = new List<BulkUploadLocationResult>();
        var successCount = 0;
        var errorCount = 0;
        var uploadedNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var rowNumber = int.Parse(record.GetValueOrDefault("_RowNumber", "0"));
            var siteName = record.GetValueOrDefault(PredefinedNameCode, string.Empty);

            var validationErrors = ValidateRecord(record, questions);
            if (validationErrors.Count > 0)
            {
                var errorMessages = string.Join("; ", validationErrors.Select(e => $"{e.QuestionCode}: {e.Message}"));
                results.Add(new BulkUploadLocationResult(rowNumber, siteName, false, errorMessages));
                errorCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(siteName))
            {
                results.Add(new BulkUploadLocationResult(rowNumber, siteName, false, $"{PredefinedNameCode}: Site name is required"));
                errorCount++;
                continue;
            }

            if (uploadedNames.Contains(siteName))
            {
                results.Add(new BulkUploadLocationResult(rowNumber, siteName, false, $"Duplicate site name '{siteName}' found in upload"));
                errorCount++;
                continue;
            }

            var existingLocation = await locationRepository.FindByNameAsync(siteName, organisationId, cancellationToken);
            if (existingLocation.IsSome)
            {
                results.Add(new BulkUploadLocationResult(rowNumber, siteName, false, $"Location with name '{siteName}' already exists"));
                errorCount++;
                continue;
            }

            // Validation passed - mark as valid (will be saved on confirm)
            uploadedNames.Add(siteName);
            results.Add(new BulkUploadLocationResult(rowNumber, siteName, true));
            successCount++;
        }

        return new BulkUploadLocationsResult(records.Count, successCount, errorCount, results);
    }

    private static List<BulkUploadRowValidationError> ValidateRecord(
        Dictionary<string, string> record,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        var errors = new List<BulkUploadRowValidationError>();

        foreach (var question in questions)
        {
            var value = record.GetValueOrDefault(question.Code, string.Empty);

            if (question.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new BulkUploadRowValidationError(question.Code, $"{question.Label} is required"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (question.QuestionType == SiteFormQuestionType.Date)
            {
                if (!DateTime.TryParse(value, out _))
                {
                    errors.Add(new BulkUploadRowValidationError(question.Code, $"Invalid date format. Expected: YYYY-MM-DD"));
                }

                continue;
            }

            if (!question.Options.Any())
            {
                continue;
            }

            var validValues = question.Options.Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (question.QuestionType == SiteFormQuestionType.Checkbox)
            {
                var selectedValues = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();

                foreach (var selectedValue in selectedValues)
                {
                    if (!validValues.Contains(selectedValue))
                    {
                        var validOptions = string.Join(", ", question.Options.Select(o => o.Value));
                        errors.Add(new BulkUploadRowValidationError(
                            question.Code,
                            $"Invalid option '{selectedValue}'. Valid options: {validOptions}"));
                    }
                }
            }
            else if (question.QuestionType is SiteFormQuestionType.Radio or SiteFormQuestionType.Select)
            {
                if (!validValues.Contains(value))
                {
                    var validOptions = string.Join(", ", question.Options.Select(o => o.Value));
                    errors.Add(new BulkUploadRowValidationError(
                        question.Code,
                        $"Invalid option '{value}'. Valid options: {validOptions}"));
                }
            }
        }

        return errors;
    }

    private static Location CreateLocationFromRecord(
        Dictionary<string, string> record,
        OrganisationId organisationId,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        var name = record.GetValueOrDefault(PredefinedNameCode, string.Empty);
        var postCode = record.GetValueOrDefault(PredefinedPostCodeCode, string.Empty);
        var referenceNumber = record.GetValueOrDefault(PredefinedReferenceNumberCode, string.Empty);

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

        foreach (var question in questions)
        {
            var value = record.GetValueOrDefault(question.Code, string.Empty);

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var displayValue = GetDisplayValue(question, value);
            var optionsSnapshot = question.Options.Any()
                ? JsonSerializer.Serialize(question.Options.Select(o => new { o.Value, o.Label }).ToList())
                : null;

            location.AddAnswer(
                question.Code,
                question.Label,
                question.Hint,
                question.QuestionType,
                question.DisplayOrder,
                value,
                displayValue,
                optionsSnapshot);
        }

        return location;
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