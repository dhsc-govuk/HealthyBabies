using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Repositories;
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
        CancellationToken cancellationToken)
    {
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

                var fieldErrors = ValidateRowFieldValues(fields, rowData, isOutcomeScores);
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
                        var normalizedValue = NormalizeFieldValue(value, field);
                        formSubmission.SetFieldValue(field, normalizedValue);
                    }
                }

                var hasAllRequiredFields = fields
                    .Where(f => f.IsRequired && !(isOutcomeScores && f.FieldKey == "PPS01"))
                    .Where(f => ConditionalRuleEvaluator.IsFieldVisible(f, rowData, fields.ToList(), null))
                    .All(f => rowData.TryGetValue(f.FieldKey, out var requiredValue) && !string.IsNullOrWhiteSpace(requiredValue));

                if (hasAllRequiredFields)
                {
                    formSubmission.MarkAsComplete();
                }
                else
                {
                    formSubmission.SaveAsDraft();
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

    private static string ComputeRowHash(Dictionary<string, string> rowData, IReadOnlyList<FormField> fields)
    {
        var canonical = string.Join("|", fields
            .Where(f => f.FieldKey != "PPS01")
            .OrderBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(f =>
            {
                var raw = rowData.GetValueOrDefault(f.FieldKey, "").Trim();
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
        bool isOutcomeScores)
    {
        var errors = new List<string>();

        foreach (var field in fields)
        {
            if (isOutcomeScores && field.FieldKey == "PPS01")
            {
                continue;
            }

            if (!ConditionalRuleEvaluator.IsFieldVisible(field, rowData, fields.ToList(), null))
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

        var sumGroupErrors = BulkUploadFieldValueValidator.ValidateSumGroups(rowData, fields);
        errors.AddRange(sumGroupErrors.Select(e => $"{e.FieldKey}: {e.Message}"));

        return errors;
    }

    private static string? NormalizeFieldValue(string value, FormField field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var fieldTypeValue = field.FieldType.ToString();

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