using Domain.Systems;

namespace Tests.Data;

public static class GlobalDataData
{
    public static GlobalData CreateGlobalData(
        string entity = GlobalDataEntityTypes.ContactRole,
        string value = "Manager",
        string? description = "Test description")
    {
        return GlobalData.New(entity, value, description);
    }

    public static GlobalData ContactRoleDirector => CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Director", "Director role");
    public static GlobalData ContactRoleManager => CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Manager", "Manager role");
    public static GlobalData ContactRoleSeniorManager => CreateGlobalData(GlobalDataEntityTypes.ContactRole, "Senior Manager", "Senior Manager role");
}