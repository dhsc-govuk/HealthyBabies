using Domain.DataCollections;

namespace Tests.Data;

public static class DataCollectionsData
{
    public static readonly DataCollection MainDataCollection = DataCollection.New(
        id: DataCollectionId.New(),
        name: "Main Data Collection",
        description: "Main data collection description",
        startDate: DateTime.UtcNow.AddDays(1),
        endDate: DateTime.UtcNow.AddMonths(3),
        saveAsDraft: false);

    public static readonly DataCollection SecondDataCollection = DataCollection.New(
        id: DataCollectionId.New(),
        name: "Second Data Collection",
        description: "Second data collection description",
        startDate: DateTime.UtcNow.AddDays(7),
        endDate: DateTime.UtcNow.AddMonths(6),
        saveAsDraft: true);
}