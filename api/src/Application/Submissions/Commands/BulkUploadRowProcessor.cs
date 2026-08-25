using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.Organisations;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;

namespace Application.Submissions.Commands;

internal class BulkUploadRowProcessor(IFormSubmissionRepository formSubmissionRepository)
{
    public async Task<BulkUploadResultDto> ProcessAsync(
        DataCollectionFormModule formModule,
        IReadOnlyList<FormField> fields,
        IReadOnlyDictionary<string, Domain.Services.Service> serviceMap,
        DataCollectionFormModuleId modId,
        OrganisationId orgId,
        DataCollectionId dcId,
        List<string> headers,
        List<List<string>> dataRows,
        int dataStartRowNumber,
        CancellationToken cancellationToken,
        DateTime? asOfUtc = null)
    {
        if (formModule.AllowsMultipleActivities)
        {
            return await ProcessActivityRowsAsync(fields, formModule.Code, serviceMap, modId, orgId, dcId, headers, dataRows, dataStartRowNumber, cancellationToken, asOfUtc);
        }

        var isOutcomeScores = formModule.Code == DataCollectionFormModuleCodes.OutcomeScores;

        var results = new List<BulkUploadRowResultDto>();
        var successCount = 0;
        var errorCount = 0;

        // For outcome scores: cache the content hashes of existing DB records per service so we
        // can skip any CSV row whose hash already exists (cross-DB dedup).  The same set also
        // accumulates hashes for rows processed during this upload, preventing within-CSV
        // duplicates from being inserted twice.
        var existingHashesByService = new Dictionary<Guid, HashSet<string>>();

        for (var i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowNumber = dataStartRowNumber + i;
            var rowData = BulkUploadCsvHelper.MapRowToFields(headers, row);

            var serviceName = rowData.GetValueOrDefault("ServiceName") ?? rowData.GetValueOrDefault("PPS01");

            try
            {
                if (string.IsNullOrWhiteSpace(serviceName))
                {
                    results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, false, "Service name is required"));
                    errorCount++;
                    continue;
                }

                if (!serviceMap.TryGetValue(serviceName.ToLowerInvariant(), out var service))
                {
                    results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, false, $"Service '{serviceName}' not found"));
                    errorCount++;
                    continue;
                }

                // Some questions are gated on the Service's own answers (e.g. SMD02 "is this
                // programme funded?"), and a condition with no answers to read evaluates to hidden -
                // which would quietly skip validating those fields here while ValidateBulkUploadCommand,
                // which does have them, validates them. Both ends must agree on what a row means.
                var serviceAnswers = service.Answers.ToDictionary(
                    a => a.QuestionCode,
                    a => a.Value ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

                var fieldErrors = ValidateRowFieldValues(fields, rowData, isOutcomeScores, serviceAnswers);
                if (fieldErrors.Count > 0)
                {
                    results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, false, string.Join("; ", fieldErrors)));
                    errorCount++;
                    continue;
                }

                FormSubmission formSubmission;

                if (isOutcomeScores)
                {
                    if (!existingHashesByService.TryGetValue(service.Id.Value, out var existingHashes))
                    {
                        // First row for this service: load all existing outcome score records and
                        // pre-compute their content hashes so we can detect duplicates cheaply.
                        var existing = await formSubmissionRepository.GetAllOutcomeScoresByServiceAsync(
                            modId, dcId, service.Id.Value, cancellationToken);
                        existingHashes = existing
                            .Select(s => ComputeSubmissionHash(s, fields))
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                        existingHashesByService[service.Id.Value] = existingHashes;
                    }

                    var rowHash = ComputeRowHash(rowData, fields);
                    if (existingHashes.Contains(rowHash))
                    {
                        // Exact duplicate of an existing record — skip silently.
                        results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, true, null));
                        successCount++;
                        continue;
                    }

                    formSubmission = FormSubmission.Create(modId, orgId, dcId, "OutcomeScore", service.Id.Value);
                    await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);

                    // Add to the seen set so the same row appearing twice in the CSV is also deduped.
                    existingHashes.Add(rowHash);

                    var serviceField = fields.FirstOrDefault(f => f.FieldKey == "PPS01");
                    if (serviceField != null)
                    {
                        formSubmission.SetFieldValue(serviceField, service.Id.Value.ToString());
                    }
                }
                else
                {
                    var submissionOption = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceAsync(
                        modId, dcId, service.Id.Value, cancellationToken);

                    if (submissionOption.IsSome)
                    {
                        formSubmission = submissionOption.Match(s => s, () => null!);
                    }
                    else
                    {
                        formSubmission = FormSubmission.Create(modId, orgId, dcId, "Service", service.Id.Value);
                        await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);
                    }
                }

                foreach (var field in fields)
                {
                    if (isOutcomeScores && field.FieldKey == "PPS01")
                    {
                        continue;
                    }

                    if (rowData.TryGetValue(field.FieldKey, out var value) && !string.IsNullOrEmpty(value))
                    {
                        var normalizedValue = NormalizeFieldValue(value, field, serviceMap);
                        formSubmission.SetFieldValue(field, normalizedValue);
                    }
                }

                var hasAllRequiredFields = fields
                    .Where(f => f.IsRequired && !(isOutcomeScores && f.FieldKey == "PPS01"))
                    .Where(f => ConditionalRuleEvaluator.IsFieldVisible(f, rowData, fields.ToList(), null))
                    .All(f => rowData.TryGetValue(f.FieldKey, out var requiredValue) && !string.IsNullOrWhiteSpace(requiredValue));

                if (hasAllRequiredFields)
                {
                    formSubmission.MarkAsComplete(asOfUtc);
                }
                else
                {
                    formSubmission.SaveAsDraft(asOfUtc);
                }

                await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

                results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, true, null));
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new BulkUploadRowResultDto(rowNumber, serviceName, false, ex.Message));
                errorCount++;
            }
        }

        return new BulkUploadResultDto(
            errorCount == 0,
            dataRows.Count,
            successCount,
            errorCount,
            results);
    }

    // Delivery Plan strand modules (Activities) match a row to an existing Activity by name
    // (case-insensitive) when one was selected for update on the review screen; otherwise a
    // row creates a brand-new Activity, the same as clicking "Add activity" and filling in the
    // form (CreateActivity/SaveActivityForm in SubmissionsControllerService.cs).
    private async Task<BulkUploadResultDto> ProcessActivityRowsAsync(
        IReadOnlyList<FormField> fields,
        string moduleCode,
        IReadOnlyDictionary<string, Domain.Services.Service> serviceMap,
        DataCollectionFormModuleId modId,
        OrganisationId orgId,
        DataCollectionId dcId,
        List<string> headers,
        List<List<string>> dataRows,
        int dataStartRowNumber,
        CancellationToken cancellationToken,
        DateTime? asOfUtc = null)
    {
        var activityIdField = fields.FirstOrDefault(f => f.FieldType.Value == FieldTypeConstants.ReadOnly && f.DisplayOrder == 1);
        var configuredNameFieldKey = DeliveryPlanStrandFields.ResolveActivityNameFieldKey(moduleCode);
        var nameField = (configuredNameFieldKey is not null ? fields.FirstOrDefault(f => f.FieldKey == configuredNameFieldKey) : null)
            ?? fields
                .Where(f => f.FieldType.Value != FieldTypeConstants.ReadOnly && f.FieldType.Value != FieldTypeConstants.Calculation)
                .OrderBy(f => f.FormSection?.SectionNumber ?? 0)
                .ThenBy(f => f.DisplayOrder)
                .FirstOrDefault();

        // A row whose derived name matches an existing Activity (selected by the user to "update
        // from this file" on the review screen) overwrites that submission in place instead of
        // creating a duplicate — mirrors the Service Users update-in-place behaviour above.
        var existingActivities = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            modId, orgId, dcId, "Activity", cancellationToken);
        var existingActivitiesByName = existingActivities
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .GroupBy(s => s.Name!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var results = new List<BulkUploadRowResultDto>();
        var successCount = 0;
        var errorCount = 0;

        for (var i = 0; i < dataRows.Count; i++)
        {
            var row = dataRows[i];
            var rowNumber = dataStartRowNumber + i;
            var rowData = BulkUploadCsvHelper.MapRowToFields(headers, row);
            var rowLabel = nameField != null ? rowData.GetValueOrDefault(nameField.FieldKey) : null;

            try
            {
                var fieldErrors = ValidateRowFieldValues(fields, rowData, isOutcomeScores: false);
                if (fieldErrors.Count > 0)
                {
                    results.Add(new BulkUploadRowResultDto(rowNumber, rowLabel, false, string.Join("; ", fieldErrors)));
                    errorCount++;
                    continue;
                }

                var existingActivity = !string.IsNullOrWhiteSpace(rowLabel) && existingActivitiesByName.TryGetValue(rowLabel.Trim(), out var matched)
                    ? matched
                    : null;

                var formSubmission = existingActivity ?? FormSubmission.Create(modId, orgId, dcId, "Activity", null, null);

                if (existingActivity == null)
                {
                    if (activityIdField != null)
                    {
                        formSubmission.SetFieldValue(activityIdField, "ACT-" + Guid.NewGuid().ToString("N")[..8].ToUpper());
                    }

                    await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);
                }

                foreach (var field in fields)
                {
                    if (rowData.TryGetValue(field.FieldKey, out var value) && !string.IsNullOrEmpty(value))
                    {
                        var normalizedValue = NormalizeFieldValue(value, field, serviceMap);
                        formSubmission.SetFieldValue(field, normalizedValue);
                    }
                }

                // Calculation fields (e.g. "Planned Spend") are never in the CSV — the browser
                // computes them live while a person fills in the form, but bulk-upload never
                // renders that form, so they must be computed here from the values just set above.
                var valuesByFieldKey = fields.ToDictionary(
                    f => f.FieldKey,
                    f => formSubmission.FieldValues.FirstOrDefault(v => v.FormFieldId == f.Id)?.Value,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var calculationField in fields.Where(f => f.FieldType.Value == FieldTypeConstants.Calculation))
                {
                    var computedValue = FormulaEvaluator.Evaluate(calculationField.Configuration, valuesByFieldKey);
                    if (computedValue != null)
                    {
                        formSubmission.SetFieldValue(calculationField, computedValue);
                    }
                }

                if (!string.IsNullOrWhiteSpace(rowLabel))
                {
                    formSubmission.SetName(rowLabel!.Length > 500 ? rowLabel[..500] : rowLabel);
                }

                var hasAllRequiredFields = fields
                    .Where(f => f.IsRequired)
                    .Where(f => ConditionalRuleEvaluator.IsFieldVisible(f, rowData, fields.ToList(), null))
                    .All(f => rowData.TryGetValue(f.FieldKey, out var requiredValue) && !string.IsNullOrWhiteSpace(requiredValue));

                if (hasAllRequiredFields)
                {
                    formSubmission.MarkAsComplete(asOfUtc);
                }
                else
                {
                    formSubmission.SaveAsDraft(asOfUtc);
                }

                await formSubmissionRepository.UpdateAsync(formSubmission, cancellationToken);

                results.Add(new BulkUploadRowResultDto(rowNumber, rowLabel, true, null));
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new BulkUploadRowResultDto(rowNumber, rowLabel, false, ex.Message));
                errorCount++;
            }
        }

        return new BulkUploadResultDto(
            errorCount == 0,
            dataRows.Count,
            successCount,
            errorCount,
            results);
    }

    private static string ComputeRowHash(Dictionary<string, string> rowData, IReadOnlyList<FormField> fields)
    {
        var canonical = string.Join("|", fields
            .Where(f => f.FieldKey != "PPS01")
            .OrderBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(f =>
            {
                var raw = rowData.GetValueOrDefault(f.FieldKey, "").Trim();
                // Hashing the raw CSV value keeps dedup independent of service-name resolution.
                var normalized = NormalizeFieldValue(raw, f) ?? "";
                return $"{f.FieldKey.ToLowerInvariant()}={normalized.ToLowerInvariant()}";
            }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string ComputeSubmissionHash(FormSubmission submission, IReadOnlyList<FormField> fields)
    {
        var canonical = string.Join("|", fields
            .Where(f => f.FieldKey != "PPS01")
            .OrderBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(f =>
            {
                var fieldValue = submission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == f.Id);
                var value = fieldValue?.Value?.Trim() ?? "";
                return $"{f.FieldKey.ToLowerInvariant()}={value.ToLowerInvariant()}";
            }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    // Re-validates every value actually present in the row before it's persisted — closes the gap
    // where an inline-edited cell (or a caller bypassing the UI entirely) could reach this point with
    // an invalid option, non-numeric, or out-of-range value that was never rejected. Required-ness
    // is deliberately not checked here: a missing required field already falls back to SaveAsDraft()
    // below rather than failing the row, and that behaviour is unchanged by this validation pass.
    private static List<string> ValidateRowFieldValues(
        IReadOnlyList<FormField> fields,
        Dictionary<string, string> rowData,
        bool isOutcomeScores,
        Dictionary<string, string>? serviceAnswers = null)
    {
        var errors = new List<string>();

        foreach (var field in fields)
        {
            if (isOutcomeScores && field.FieldKey == "PPS01")
            {
                continue;
            }

            if (!ConditionalRuleEvaluator.IsFieldVisible(field, rowData, fields.ToList(), serviceAnswers))
            {
                continue;
            }

            var value = rowData.GetValueOrDefault(field.FieldKey);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var fieldErrors = BulkUploadFieldValueValidator.ValidateFieldValue(value, field);
            errors.AddRange(fieldErrors.Select(e => $"{field.Label}: {e}"));
        }

        var sumGroupErrors = BulkUploadFieldValueValidator.ValidateSumGroups(rowData, fields, serviceAnswers);
        errors.AddRange(sumGroupErrors.Select(e => $"{e.FieldKey}: {e.Message}"));

        return errors;
    }

    private static bool IsDynamicServicesField(FormField field) =>
        !string.IsNullOrEmpty(field.Configuration)
        && field.Configuration.Contains("dynamicOptions")
        && field.Configuration.Contains("\"services\"");

    private static string? NormalizeFieldValue(
        string value,
        FormField field,
        IReadOnlyDictionary<string, Domain.Services.Service>? serviceMap = null)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var fieldTypeValue = field.FieldType.ToString();

        // Service-backed fields are entered as names in the CSV (that is what the template offers and
        // what a person can reasonably type) but stored as service IDs, so a later rename doesn't
        // orphan the answer. Matches how PPS01 is persisted for Outcome Scores.
        if (serviceMap != null && IsDynamicServicesField(field))
        {
            var names = value.Split(',').Select(v => v.Trim()).Where(v => !string.IsNullOrEmpty(v));
            var resolved = names.Select(n =>
                serviceMap.TryGetValue(n.ToLowerInvariant(), out var service)
                    ? service.Id.Value.ToString()
                    : n);
            return string.Join(",", resolved);
        }

        if (fieldTypeValue == FieldType.Radio.ToString() || fieldTypeValue == FieldType.Select.ToString())
        {
            var matchingOption = field.Options.FirstOrDefault(o =>
                o.Label.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                o.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            return matchingOption?.Value ?? value;
        }

        if (fieldTypeValue == FieldType.Checkbox.ToString())
        {
            var values = value.Split(',').Select(v => v.Trim()).ToList();
            var normalizedValues = values.Select(v =>
            {
                var option = field.Options.FirstOrDefault(o =>
                    o.Label.Equals(v, StringComparison.OrdinalIgnoreCase) ||
                    o.Value.Equals(v, StringComparison.OrdinalIgnoreCase));
                return option?.Value ?? v;
            });
            return string.Join(",", normalizedValues);
        }

        return value;
    }
}

/// <summary>
/// Evaluates a Calculation field's stored "formula" (e.g. "DG07 - (DG09 + DG10)") against a row's
/// other field values, mirroring the client-side computeFormula in FormFieldRenderer/index.tsx —
/// that logic only runs while a person has the form open in a browser, so anything created without
/// one (bulk-upload) needs the same arithmetic run here instead.
/// </summary>
internal static class FormulaEvaluator
{
    private static readonly Regex FieldCodePattern = new(@"[A-Z]{1,4}\d+(?:\.\d+)?[a-z]*", RegexOptions.Compiled);
    private static readonly Regex SafeExpressionPattern = new(@"^[\d\s+\-*/.()]+$", RegexOptions.Compiled);

    public static string? Evaluate(string? configuration, IReadOnlyDictionary<string, string?> valuesByFieldKey)
    {
        var formula = ExtractFormula(configuration);
        if (string.IsNullOrWhiteSpace(formula))
        {
            return null;
        }

        var expression = FieldCodePattern.Replace(formula, match =>
        {
            var raw = valuesByFieldKey.GetValueOrDefault(match.Value);
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value.ToString(CultureInfo.InvariantCulture)
                : "0";
        });

        if (!SafeExpressionPattern.IsMatch(expression))
        {
            return null;
        }

        try
        {
            var result = new DataTable().Compute(expression, null);
            return Convert.ToDecimal(result, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is EvaluateException or InvalidCastException or FormatException or OverflowException)
        {
            return null;
        }
    }

    private static string? ExtractFormula(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.TryGetProperty("formula", out var formulaElement)
                ? formulaElement.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}