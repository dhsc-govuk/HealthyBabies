namespace Domain.Organisations;

public static class FinancialDataModuleConstants
{
    public const string DeliveryGrant = "delivery-grant";
    public const string Parenting = "parenting";
    public const string Hle = "hle";
    public const string Send = "send";
    public const string PnmhAndPir = "pnmh-and-pir";
    public const string InfantFeeding = "infant-feeding";
    public const string HealthyBabiesOffer = "healthy-babies-offer";
    public const string RolloverSpend = "rollover-spend";
}

public class FinancialDataModule
{
    public string Value { get; }

    private FinancialDataModule(string value) => Value = value;

    public static FinancialDataModule From(string value)
    {
        if (All.All(m => m.Value != value))
        {
            throw new ArgumentException($"Invalid financial data module: {value}");
        }

        return new FinancialDataModule(value);
    }

    public static FinancialDataModule DeliveryGrant => new(FinancialDataModuleConstants.DeliveryGrant);
    public static FinancialDataModule Parenting => new(FinancialDataModuleConstants.Parenting);
    public static FinancialDataModule Hle => new(FinancialDataModuleConstants.Hle);
    public static FinancialDataModule Send => new(FinancialDataModuleConstants.Send);
    public static FinancialDataModule PnmhAndPir => new(FinancialDataModuleConstants.PnmhAndPir);
    public static FinancialDataModule InfantFeeding => new(FinancialDataModuleConstants.InfantFeeding);
    public static FinancialDataModule HealthyBabiesOffer => new(FinancialDataModuleConstants.HealthyBabiesOffer);
    public static FinancialDataModule RolloverSpend => new(FinancialDataModuleConstants.RolloverSpend);

    public static IEnumerable<FinancialDataModule> All
    {
        get
        {
            yield return DeliveryGrant;
            yield return Parenting;
            yield return Hle;
            yield return Send;
            yield return PnmhAndPir;
            yield return InfantFeeding;
            yield return HealthyBabiesOffer;
            yield return RolloverSpend;
        }
    }

    public override bool Equals(object? obj) => obj is FinancialDataModule other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}