namespace Domain.DataCollections;

/// <summary>
/// Maps each Delivery Plan activity module to the seeded questions that hold its activity status
/// ("Started", "Not started", "Delayed", "Completed") and its delivery status ("On track",
/// "Off track").
///
/// These are ordinary answers, not workflow state: <see cref="Forms.FormSubmission.Status"/> tracks
/// where a form is in its submission lifecycle, whereas the two statuses shown on the review screens
/// are values the local authority selected. The field keys differ per module and the numbering is not
/// derivable from the module code, so the mapping is explicit. Keep it in step with
/// DeliveryPlanFormModuleSeeder — DeliveryPlanActivityStatusFieldsTests fails if a module marked
/// AllowsMultipleActivities is missing here.
/// </summary>
public static class DeliveryPlanActivityStatusFields
{
    /// <param name="ActivityStatusFieldKey">
    /// Null for rollover-spend, which is seeded with a delivery status question only.
    /// </param>
    public readonly record struct StatusFieldKeys(string? ActivityStatusFieldKey, string DeliveryStatusFieldKey);

    public static readonly IReadOnlyDictionary<string, StatusFieldKeys> ByModuleCode =
        new Dictionary<string, StatusFieldKeys>
        {
            [DataCollectionFormModuleCodes.DeliveryGrant] = new("DG06", "DG07"),
            [DataCollectionFormModuleCodes.Parenting] = new("P07", "P08"),
            [DataCollectionFormModuleCodes.Hle] = new("H07", "H08"),
            [DataCollectionFormModuleCodes.Send] = new("SEND07", "SEND08"),
            [DataCollectionFormModuleCodes.PnmhAndPir] = new("PN10", "PN11"),
            [DataCollectionFormModuleCodes.InfantFeeding] = new("IF10", "IF11"),
            [DataCollectionFormModuleCodes.HealthyBabiesOffer] = new("HBO10", "HBO11"),
            [DataCollectionFormModuleCodes.RolloverSpend] = new(null, "RO08"),
        };

    /// <summary>
    /// Returns the status question keys for a module code, or null when the module has no status
    /// questions — which is the case for every non-activity module, such as Focus.
    /// </summary>
    public static StatusFieldKeys? ForModule(string moduleCode)
        => ByModuleCode.TryGetValue(moduleCode, out var keys) ? keys : null;
}