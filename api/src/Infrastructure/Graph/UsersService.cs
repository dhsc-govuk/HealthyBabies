using System.Net;
using Application.Common.Interfaces;
using Application.Common.Settings;
using Application.Users.Interfaces;
using Domain.Common;
using Domain.ValueObjects;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Infrastructure.Graph;

public class UsersService : IUsersService
{
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<UsersService> _logger;
    private readonly string _defaultUserPassword;
    private readonly string _domain;

    public UsersService(
        GraphServiceClient graphServiceClient,
        ApplicationSettings settings,
        IEmailNotificationService emailNotificationService,
        ILogger<UsersService> logger)
    {
        Guard.NotNull(graphServiceClient, nameof(graphServiceClient));
        Guard.NotNull(settings, nameof(settings));
        Guard.NotNullOrEmpty(settings.Graph.DefaultUserPassword, nameof(settings.Graph.DefaultUserPassword));
        Guard.NotNullOrEmpty(settings.AzureAd.Domain, nameof(settings.AzureAd.Domain));

        _graphServiceClient = graphServiceClient;
        _emailNotificationService = emailNotificationService;
        _logger = logger;

        _defaultUserPassword = settings.Graph.DefaultUserPassword;
        _domain = settings.AzureAd.Domain;
    }

    public async Task<Option<string>> FindById(string userId, CancellationToken cancellationToken)
    {
        var user = await _graphServiceClient.Users[userId].GetAsync(cancellationToken: cancellationToken);
        if (user == null)
        {
            return Option<string>.None;
        }

        return user.Id;
    }

    public async Task<Option<string>> FindByEmail(string email, CancellationToken cancellationToken)
    {
        try
        {
            // First, check by federated identity
            var users = await _graphServiceClient.Users.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter
                        = $"identities/any(c:c/issuerAssignedId eq '{email}' and c/issuer eq '{_domain}')";
                    requestConfiguration.QueryParameters.Select = ["id"];
                    requestConfiguration.QueryParameters.Top = 1;
                },
                cancellationToken);

            if (users is { Value: not null } && users.Value.Count != 0)
            {
                return users.Value.First().Id;
            }

            // If not found by identity, check by mail property (for users created via Azure portal or other methods)
            var usersByMail = await _graphServiceClient.Users.GetAsync(
                requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"mail eq '{email}'";
                    requestConfiguration.QueryParameters.Select = ["id"];
                    requestConfiguration.QueryParameters.Top = 1;
                },
                cancellationToken);

            if (usersByMail is { Value: not null } && usersByMail.Value.Count != 0)
            {
                return usersByMail.Value.First().Id;
            }
        }
        catch (ServiceException ex)
        {
            if (ex.Message.ToLower().Contains($"resource '{email}' does not exist"))
            {
                return Option<string>.None;
            }

            throw;
        }

        return Option<string>.None;
    }

    public async Task<Either<Exception, CreatedIdentity>> AddWithPassword(
        Name userName,
        string email,
        bool isActive,
        CancellationToken cancellationToken)
    {
        try
        {
            var password = PasswordGenerator.GenerateRandomPassword();

            // Local member account: user signs in with email + password and is required to change it on first sign-in
            var user = new User
            {
                AccountEnabled = isActive,
                DisplayName = userName.ToString(),
                GivenName = userName.FirstName,
                Surname = userName.LastName,
                Mail = email,
                MailNickname = GenerateMailNickname(email),
                Identities =
                [
                    new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = _domain,
                        IssuerAssignedId = email
                    }
                ],
                PasswordProfile = new PasswordProfile
                {
                    ForceChangePasswordNextSignIn = true,
                    ForceChangePasswordNextSignInWithMfa = true,
                    Password = password
                },
                PasswordPolicies = "DisablePasswordExpiration"
            };

            var createdUser = await _graphServiceClient.Users.PostAsync(user, cancellationToken: cancellationToken);
            _logger.LogInformation("Graph user created with temporary password for {Email}, UserId: {UserId}", email, createdUser!.Id);

            var passwordResult = await _emailNotificationService.SendTemporaryPasswordEmail(email, password, cancellationToken);
            passwordResult.Match(
                _ => { _logger.LogInformation("Temporary password email sent to {Email}", email); return Unit.Default; },
                ex => { _logger.LogError(ex, "Failed to send temporary password email to {Email}", email); return Unit.Default; });

            return new CreatedIdentity(createdUser.Id!);
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Failed to create user with temporary password for {Email}", email);
            return ex;
        }
    }

    private static string GenerateMailNickname(string email)
    {
        var localPart = email.Split('@')[0];

        // Replace characters not allowed in mailNickname
        return localPart.Replace(".", "-").Replace("+", "-").Replace("_", "-");
    }

    public async Task<Either<Exception, Unit>> Update(
       string id,
       Name userName,
       string email,
       bool isActive,
       CancellationToken cancellationToken)
    {
        var user = new User
        {
            AccountEnabled = isActive,
            GivenName = userName.FirstName,
            Surname = userName.LastName,
            DisplayName = userName.ToString()
        };

        try
        {
            await _graphServiceClient.Users[id].PatchAsync(user, cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (ServiceException ex)
        {
            return ex;
        }
    }

    public async Task<Either<Exception, Unit>> Activate(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = new User { AccountEnabled = true };
            await _graphServiceClient.Users[userId].PatchAsync(user, cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (ODataError ex)
        {
            return ex;
        }
        catch (ServiceException ex)
        {
            return ex;
        }
    }

    public async Task<Either<Exception, Unit>> Deactivate(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = new User { AccountEnabled = false };
            await _graphServiceClient.Users[userId].PatchAsync(user, cancellationToken: cancellationToken);
            return Unit.Default;
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            // User already deleted from Entra — treat as success so local delete can proceed.
            return Unit.Default;
        }
        catch (ODataError ex)
        {
            return ex;
        }
        catch (ServiceException ex)
        {
            return ex;
        }
    }

    public async Task<Either<Exception, Unit>> Delete(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _graphServiceClient.Users[userId].DeleteAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted Entra user {UserId}", userId);
            return Unit.Default;
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            // Already gone from Entra — treat as success so local delete can proceed.
            _logger.LogInformation("Entra user {UserId} already deleted; skipping", userId);
            return Unit.Default;
        }
        catch (ODataError ex)
        {
            _logger.LogError(ex, "Failed to delete Entra user {UserId}: {Code} {Message}", userId, ex.Error?.Code, ex.Error?.Message);
            return ex;
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.NotFound)
        {
            // Already gone from Entra — treat as success so local delete can proceed.
            _logger.LogInformation("Entra user {UserId} already deleted; skipping", userId);
            return Unit.Default;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Failed to delete Entra user {UserId}", userId);
            return ex;
        }
    }
}