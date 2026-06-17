using System.Net;
using System.Net.Http.Json;
using Application.Common.Interfaces;
using Application.Mfa.Dtos;
using Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Mfa;

public class MfaControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _testUser = AdminsData.MainAdmin;

    [Fact]
    public async Task GetStatus_WhenMfaNotEnabled_ShouldReturnDisabledStatus()
    {
        // Act
        var response = await Client.GetAsync("mfa/status");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var status = await response.ToResponseModel<MfaStatusDto>();
        status.IsEnabled.Should().BeFalse();
        status.EnabledAt.Should().BeNull();
        status.RecoveryCodesRemaining.Should().Be(0);
    }

    [Fact]
    public async Task Setup_WhenMfaNotEnabled_ShouldReturnQrCodeAndSecret()
    {
        // Act
        var response = await Client.PostAsync("mfa/setup", null);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var setupResponse = await response.ToResponseModel<MfaSetupResponseDto>();
        setupResponse.QrCodeUri.Should().NotBeNullOrEmpty();
        setupResponse.QrCodeUri.Should().StartWith("otpauth://totp/");
        setupResponse.ManualEntryKey.Should().NotBeNullOrEmpty();
        setupResponse.ManualEntryKey.Should().HaveLength(32);
    }

    [Fact]
    public async Task VerifySetup_WithInvalidCode_ShouldReturnUnauthorized()
    {
        // Arrange
        await Client.PostAsync("mfa/setup", null);
        var invalidCode = "000000";

        // Act
        var response = await Client.PostAsJsonAsync("mfa/setup/verify", new MfaVerifyRequestDto(invalidCode));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatus_WhenMfaEnabled_ShouldReturnEnabledStatus()
    {
        // Arrange
        await SetupAndEnableMfa();

        // Act
        var response = await Client.GetAsync("mfa/status");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var status = await response.ToResponseModel<MfaStatusDto>();
        status.IsEnabled.Should().BeTrue();
        status.EnabledAt.Should().NotBeNull();
        status.RecoveryCodesRemaining.Should().Be(10);
    }

    [Fact]
    public async Task Verify_WithInvalidCode_ShouldReturnUnauthorized()
    {
        // Arrange
        await SetupAndEnableMfa();
        var invalidCode = "000000";

        // Act
        var response = await Client.PostAsJsonAsync("mfa/verify", new MfaVerifyRequestDto(invalidCode));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disable_WithValidCode_ShouldDisableMfa()
    {
        // Arrange
        var secret = await SetupAndEnableMfa();
        var validCode = GenerateValidTotpCode(secret);

        // Act
        var response = await Client.PostAsJsonAsync("mfa/disable", new MfaVerifyRequestDto(validCode));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var statusResponse = await Client.GetAsync("mfa/status");
        var status = await statusResponse.ToResponseModel<MfaStatusDto>();
        status.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Disable_WithInvalidCode_ShouldReturnUnauthorized()
    {
        // Arrange
        await SetupAndEnableMfa();
        var invalidCode = "000000";

        // Act
        var response = await Client.PostAsJsonAsync("mfa/disable", new MfaVerifyRequestDto(invalidCode));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_WithValidCode_ShouldReturnNewCodes()
    {
        // Arrange
        var secret = await SetupAndEnableMfa();
        var validCode = GenerateValidTotpCode(secret);

        // Act
        var response = await Client.PostAsJsonAsync("mfa/recovery-codes/regenerate", new MfaVerifyRequestDto(validCode));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var codesResponse = await response.ToResponseModel<MfaRecoveryCodesDto>();
        codesResponse.Codes.Should().HaveCount(10);
    }

    [Fact]
    public async Task Setup_WhenMfaAlreadyEnabled_ShouldReturnConflict()
    {
        // Arrange
        await SetupAndEnableMfa();

        // Act
        var response = await Client.PostAsync("mfa/setup", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // BLUM-6354: Traffic alternates between Azure Front Door (X-Azure-ClientIP)
    // and an internal LB (X-Forwarded-For), so the resolved client IP flaps
    // across requests in the same session. ValidateSessionAsync must not fail
    // on IP change as long as the session, expiry, and UserAgent remain valid.
    [Fact]
    public async Task ValidateSessionAsync_WhenClientIpChangesAfterCreation_ShouldStillReturnSession()
    {
        // Arrange
        using var scope = Server.Services.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<IMfaSessionService>();
        const string userAgent = "Mozilla/5.0 (Test) AppleWebKit/537.36";
        const string originalIp = "10.0.0.1";
        const string changedIp = "10.0.0.2";

        var session = await sessionService.CreateSessionAsync(_testUser.Id, originalIp, userAgent);

        // Act
        var validated = await sessionService.ValidateSessionAsync(session.Id, changedIp, userAgent);

        // Assert
        validated.Should().NotBeNull();
        validated!.Id.Should().Be(session.Id);
        validated.UserId.Should().Be(_testUser.Id);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenUserAgentChanges_ShouldReturnNull()
    {
        // Arrange
        using var scope = Server.Services.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<IMfaSessionService>();
        const string originalUserAgent = "Mozilla/5.0 (Test) AppleWebKit/537.36";
        const string differentUserAgent = "curl/8.0";
        const string ipAddress = "10.0.0.1";

        var session = await sessionService.CreateSessionAsync(_testUser.Id, ipAddress, originalUserAgent);

        // Act
        var validated = await sessionService.ValidateSessionAsync(session.Id, ipAddress, differentUserAgent);

        // Assert
        validated.Should().BeNull();
    }

    private static string GenerateValidTotpCode(string secret)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);
        return totp.ComputeTotp();
    }

    private async Task<string> SetupAndEnableMfa()
    {
        var setupResponse = await Client.PostAsync("mfa/setup", null);
        var setup = await setupResponse.ToResponseModel<MfaSetupResponseDto>();
        var validCode = GenerateValidTotpCode(setup.ManualEntryKey);

        await Client.PostAsJsonAsync("mfa/setup/verify", new MfaVerifyRequestDto(validCode));

        return setup.ManualEntryKey;
    }

    public async Task InitializeAsync()
    {
        Context.Users.Add(_testUser);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}