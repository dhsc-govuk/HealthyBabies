using Domain.DataCollections;
using Domain.Organisations;

namespace Application.Organisations;

/// <summary>
/// Maps each delivery-plan strand (form module) to the field keys that hold the
/// financial and delivery figures surfaced on the Local Authority profile.
/// The field numbering differs per strand, so the mapping is explicit.
/// Forecast fields are keyed by financial year range.
/// </summary>
public record DeliveryPlanStrandFields(
    string ModuleCode,
    string DisplayName,
    string? AllocationModule,
    string ActivityNameField,
    string ActivityStatusField,
    string DeliveryStatusField,
    IReadOnlyDictionary<string, string> ForecastFieldsByYear,
    string ActualSpendField,
    string CommittedField,
    string PlannedField)
{
    public const string Year2627 = "2026-27";
    public const string Year2728 = "2027-28";
    public const string Year2829 = "2028-29";
    public const string RolloverYear = "2025-26";

    public static readonly DeliveryPlanStrandFields DeliveryGrant = Allocation(DataCollectionFormModuleCodes.DeliveryGrant, "Delivery Grant", FinancialDataModuleConstants.DeliveryGrant, "DG", 2, 4, 5, 7, 12, 13, 9, 10, 11);

    public static readonly DeliveryPlanStrandFields Parenting = Allocation(DataCollectionFormModuleCodes.Parenting, "Parenting Support", FinancialDataModuleConstants.Parenting, "P", 2, 5, 6, 8, 13, 14, 10, 11, 12);

    public static readonly DeliveryPlanStrandFields Hle = Allocation(DataCollectionFormModuleCodes.Hle, "Home Learning Environment (HLE)", FinancialDataModuleConstants.Hle, "H", 2, 5, 6, 8, 13, 14, 10, 11, 12);

    public static readonly DeliveryPlanStrandFields Send = Allocation(DataCollectionFormModuleCodes.Send, "Special Educational Needs and Disability (SEND)", FinancialDataModuleConstants.Send, "SEND", 2, 5, 6, 8, 13, 14, 10, 11, 12);

    public static readonly DeliveryPlanStrandFields PnmhAndPir = Allocation(DataCollectionFormModuleCodes.PnmhAndPir, "Perinatal Mental Health and Parent-Infant Relationships", FinancialDataModuleConstants.PnmhAndPir, "PN", 1, 9, 10, 12, 17, 18, 14, 15, 16);

    public static readonly DeliveryPlanStrandFields InfantFeeding = Allocation(DataCollectionFormModuleCodes.InfantFeeding, "Infant Feeding", FinancialDataModuleConstants.InfantFeeding, "IF", 1, 9, 10, 12, 17, 18, 14, 15, 16);

    public static readonly DeliveryPlanStrandFields HealthyBabiesOffer = Allocation(DataCollectionFormModuleCodes.HealthyBabiesOffer, "Healthy Babies Offer", FinancialDataModuleConstants.HealthyBabiesOffer, "HBO", 1, 9, 10, 12, 17, 18, 14, 15, 16);

    /// <summary>
    /// Rollover spend only relates to the 2025-26 year and uses a single status field.
    /// </summary>
    public static readonly DeliveryPlanStrandFields RolloverSpend = new(
        DataCollectionFormModuleCodes.RolloverSpend,
        "Rollover spend",
        FinancialDataModuleConstants.RolloverSpend,
        "RO02",
        "RO08",
        "RO08",
        new Dictionary<string, string> { [RolloverYear] = "RO10" },
        "RO11",
        "RO12",
        "RO13");

    /// <summary>The seven allocation strands shown on the Allocations and Financial data tabs.</summary>
    public static readonly IReadOnlyList<DeliveryPlanStrandFields> AllocationStrands =
    [
        DeliveryGrant, Parenting, Hle, Send, PnmhAndPir, InfantFeeding, HealthyBabiesOffer,
    ];

    /// <summary>All delivery-plan strands including rollover spend.</summary>
    public static readonly IReadOnlyList<DeliveryPlanStrandFields> All =
    [
        DeliveryGrant, Parenting, Hle, Send, PnmhAndPir, InfantFeeding, HealthyBabiesOffer, RolloverSpend,
    ];

    private static DeliveryPlanStrandFields Allocation(
        string moduleCode,
        string displayName,
        string? allocationModule,
        string prefix,
        int activityNameNo,
        int activityStatusNo,
        int deliveryStatusNo,
        int forecast2627No,
        int forecast2728No,
        int forecast2829No,
        int actualNo,
        int committedNo,
        int plannedNo) =>
        new(
            moduleCode,
            displayName,
            allocationModule,
            $"{prefix}{activityNameNo:00}",
            $"{prefix}{activityStatusNo:00}",
            $"{prefix}{deliveryStatusNo:00}",
            new Dictionary<string, string>
            {
                [Year2627] = $"{prefix}{forecast2627No:00}",
                [Year2728] = $"{prefix}{forecast2728No:00}",
                [Year2829] = $"{prefix}{forecast2829No:00}",
            },
            $"{prefix}{actualNo:00}",
            $"{prefix}{committedNo:00}",
            $"{prefix}{plannedNo:00}");
}