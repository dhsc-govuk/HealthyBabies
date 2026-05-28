using System.Net;
using System.Net.Http.Json;
using Application.Mfa.Dtos;
using Domain.Users;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Mfa;

public class AdminMfaControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;
    private User _targetUser = null!;

    private static string GenerateValidTotpCode(string secret)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);
        return totp.ComputeTotp();
    }

    [Fact]
    public async Task AdminDisableMfa_WhenMfaEnabled_ShouldDisableMfa()
    {
        // Arrange
        await SetupAndEnableMfaForUser(_targetUser.Id);

        // Act
        var response = await Client.PostAsync($"admin/users/{_targetUser.Id.Value}/disable-mfa", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var statusResponse = await GetMfaStatusForUser(_targetUser.Id);
        statusResponse.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AdminDisableMfa_WhenMfaNotEnabled_ShouldReturnBadRequest()
    {
        // Act
        var response = await Client.PostAsync($"admin/users/{_targetUser.Id.Value}/disable-mfa", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AdminResetMfa_WhenMfaEnabled_ShouldDeleteMfaRecord()
    {
        // Arrange
        await SetupAndEnableMfaForUser(_targetUser.Id);

        // Act
        var response = await Client.PostAsync($"admin/users/{_targetUser.Id.Value}/reset-mfa", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var statusResponse = await GetMfaStatusForUser(_targetUser.Id);
        statusResponse.IsEnabled.Should().BeFalse();
        statusResponse.RecoveryCodesRemaining.Should().Be(0);
    }

    [Fact]
    public async Task AdminResetMfa_WhenMfaNotEnabled_ShouldReturnNoContent()
    {
        // Act
        var response = await Client.PostAsync($"admin/users/{_targetUser.Id.Value}/reset-mfa", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdminResetMfa_WhenMfaDisabled_ShouldDeleteMfaRecord()
    {
        // Arrange
        var secret = await SetupAndEnableMfaForUser(_targetUser.Id);
        var validCode = GenerateValidTotpCode(secret);

        // Disable MFA first (record still exists but IsEnabled = false)
        await Client.PostAsJsonAsync("mfa/disable", new MfaVerifyRequestDto(validCode));

        // Act
        var response = await Client.PostAsync($"admin/users/{_targetUser.Id.Value}/reset-mfa", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    public async Task InitializeAsync()
    {
        _targetUser = User.New(
            id: UserId.New(),
            name: new Name("Target", "User"),
            email: "target.user@email.com",
            subId: new SubId(Guid.NewGuid()),
            isActive: true,
            role: UserRole.Admin);

        Context.Users.Add(_adminUser);
        Context.Users.Add(_targetUser);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private async Task<string> SetupAndEnableMfaForUser(UserId userId)
    {
        var userMfa = UserMfa.New(UserMfaId.New(), userId, "encrypted_test_secret");
        var recoveryCodes = Enumerable.Range(1, 10).Select(i => $"code{i}").ToList();
        userMfa.Enable(recoveryCodes);

        Context.UserMfas.Add(userMfa);
        await SaveChangesAsync();

        return "JBSWY3DPEHPK3PXP";
    }

    private async Task<MfaStatusDto> GetMfaStatusForUser(UserId userId)
    {
        Context.ChangeTracker.Clear();
        var userMfa = await Context.UserMfas.FirstOrDefaultAsync(m => m.UserId == userId);
        if (userMfa == null || !userMfa.IsEnabled)
        {
            return new MfaStatusDto(false, null, 0, MfaState.Disabled);
        }

        return new MfaStatusDto(true, userMfa.EnabledAt, userMfa.HashedRecoveryCodes.Count, MfaState.Enabled);
    }
}