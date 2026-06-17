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

                FormSubmission formSubmission;

                if (isOutcomeScores)
                {
                    formSubmission = FormSubmission.Create(modId, orgId, dcId, "OutcomeScore", service.Id.Value);
                    await formSubmissionRepository.AddAsync(formSubmission, cancellationToken);

                    var serviceField = fields.FirstOrDefault(f => f.FieldKey == "PPS01");
                    if (serviceField != null)
                    {
                        formSubmission.SetFieldValue(serviceField.Id, service.Id.Value.ToString());
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
                        formSubmission.SetFieldValue(field.Id, normalizedValue);
                    }
                }

                formSubmission.MarkAsComplete();
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