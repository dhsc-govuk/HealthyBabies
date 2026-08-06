using System.Text.Json;
using Application.Organisations.Dtos;
using Domain.Locations;
using Domain.Organisations;
using Domain.SiteForms;

namespace Application.Organisations.Helpers;

public static class LocationBulkUploadRecordHelper
{
    public const string PredefinedNameCode = "FHS01";
    public const string PredefinedPostCodeCode = "FHS02";
    public const string PredefinedReferenceNumberCode = "FHS03";
    public const string PredefinedAddressLine1Code = "FHS16";
    public const string PredefinedAddressLine2Code = "FHS13";
    public const string PredefinedTownOrCityCode = "FHS14";
    public const string PredefinedCountyCode = "FHS15";

    public static List<BulkUploadRowValidationError> ValidateRecord(
        Dictionary<string, string> record,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        var errors = new List<BulkUploadRowValidationError>();

        foreach (var question in questions)
        {
            if (!string.IsNullOrEmpty(question.ConditionalQuestionCode))
            {
                var parentValue = record.GetValueOrDefault(question.ConditionalQuestionCode, string.Empty);
                if (parentValue != question.ConditionalValue)
                {
                    continue;
                }
            }

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
                if (!DateTime.TryParse(value, out var parsedDate))
                {
                    errors.Add(new BulkUploadRowValidationError(question.Code, "Invalid date format. Expected: YYYY-MM-DD"));
                }
                else
                {
                    var dateError = SiteFormDateAnswerValidator.ValidateDateRule(question, parsedDate);
                    if (dateError != null)
                    {
                        errors.Add(new BulkUploadRowValidationError(question.Code, dateError));
                    }
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

    public static Location BuildNewLocation(
        Dictionary<string, string> record,
        OrganisationId organisationId,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        var name = record.GetValueOrDefault(PredefinedNameCode, string.Empty);
        var postCode = record.GetValueOrDefault(PredefinedPostCodeCode, string.Empty);
        var referenceNumber = record.GetValueOrDefault(PredefinedReferenceNumberCode, string.Empty);
        var addressLine1 = record.GetValueOrDefault(PredefinedAddressLine1Code, string.Empty);
        var addressLine2 = record.GetValueOrDefault(PredefinedAddressLine2Code, string.Empty);
        var townOrCity = record.GetValueOrDefault(PredefinedTownOrCityCode, string.Empty);
        var county = record.GetValueOrDefault(PredefinedCountyCode, string.Empty);

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
            addressLine1,
            addressLine2,
            townOrCity,
            county,
            isActive: true);

        ApplyAnswers(location, record, questions);
        return location;
    }

    public static void UpdateExistingLocation(
        Location location,
        Dictionary<string, string> record,
        IReadOnlyList<SiteFormQuestion> questions)
    {
        location.UpdatePostCode(record.GetValueOrDefault(PredefinedPostCodeCode, string.Empty));

        var referenceNumber = record.GetValueOrDefault(PredefinedReferenceNumberCode, string.Empty);
        if (!string.IsNullOrWhiteSpace(referenceNumber))
        {
            location.UpdateReferenceNumber(referenceNumber);
        }

        location.UpdateAddressLine1(record.GetValueOrDefault(PredefinedAddressLine1Code, string.Empty));
        location.UpdateAddressLine2(record.GetValueOrDefault(PredefinedAddressLine2Code, string.Empty));
        location.UpdateTownOrCity(record.GetValueOrDefault(PredefinedTownOrCityCode, string.Empty));
        location.UpdateCounty(record.GetValueOrDefault(PredefinedCountyCode, string.Empty));

        ApplyAnswers(location, record, questions);
    }

    private static void ApplyAnswers(
        Location location,
        Dictionary<string, string> record,
        IReadOnlyList<SiteFormQuestion> questions)
    {
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

            var existingAnswer = location.GetAnswer(question.Code);
            if (existingAnswer != null)
            {
                existingAnswer.UpdateAnswer(value, displayValue);
                existingAnswer.UpdateSnapshot(question.Label, question.Hint, question.QuestionType, question.DisplayOrder, optionsSnapshot);
            }
            else
            {
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
        }
    }

    public static string? GetDisplayValue(SiteFormQuestion question, string? value)
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