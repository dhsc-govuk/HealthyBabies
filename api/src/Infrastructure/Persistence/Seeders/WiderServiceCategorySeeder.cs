using Domain.Systems;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class WiderServiceCategorySeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var existingCategories = await context.GlobalData
            .Where(x => x.Entity == GlobalDataEntityTypes.WiderServiceCategory)
            .ToListAsync();

        if (existingCategories.Count > 0)
        {
            return;
        }

        var categories = CreateCategories();
        context.GlobalData.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private static List<GlobalData> CreateCategories()
    {
        return
        [
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "debt_welfare", "Debt and welfare advice"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "domestic_abuse", "Domestic abuse support"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "housing", "Housing"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "mental_health", "Mental health services (beyond Start for Life parent-infant mental health)"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "nutrition_weight", "Nutrition and weight management"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "oral_health", "Oral health improvement"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "stop_smoking", "Stop smoking support"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "substance_misuse", "Substance (alcohol/drug) misuse support"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "youth_justice", "Youth justice services"),
            GlobalData.New(GlobalDataEntityTypes.WiderServiceCategory, "youth_services", "Youth services - universal and targeted"),
        ];
    }
}