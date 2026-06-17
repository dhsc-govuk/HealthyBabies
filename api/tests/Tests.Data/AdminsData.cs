using Domain.Users;
using Domain.ValueObjects;

namespace Tests.Data;

public static class AdminsData
{
    public static readonly User MainAdmin = User.New(
        id: UserId.New(),
        name: new Name("Main", "Admin"),
        email: "main.admin@email.com",
        subId: new SubId(Guid.NewGuid()),
        isActive: true,
        role: UserRole.Admin);
}