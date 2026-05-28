namespace Application.Common.Settings;

public class ApplicationSettings
{
    public AzureAdSettings AzureAd { get; set; } = new();
    public GraphSettings Graph { get; set; } = new();
    public DefaultAdminUserSettings DefaultAdminUser { get; set; } = new();
    public UserToRemoveSettings UserToRemove { get; set; } = new();
    public AzureBlobSettings AzureBlob { get; set; } = new();
    public SendGridSettings SendGrid { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public HangfireSettings Hangfire { get; set; } = new();
    public MfaSettings Mfa { get; set; } = new();
    public OsPlacesSettings OsPlaces { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
    public SeederSettings Seeder { get; set; } = new();
}

public class AzureAdSettings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ParentDomain { get; set; } = string.Empty;
}

public class GraphSettings
{
    public string ApplicationId { get; set; } = string.Empty;
    public string ApplicationObjectId { get; set; } = string.Empty;
    public string EnterpriseApplicationObjectId { get; set; } = string.Empty;
    public string DefaultUserPassword { get; set; } = string.Empty;
}

public class DefaultAdminUserSettings
{
    public string Email { get; set; } = string.Empty;
    public string SubId { get; set; } = string.Empty;
}

public class UserToRemoveSettings
{
    public string? Email { get; set; }
}

public class AzureBlobSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SendGridSettings
{
    public string EmailApiKey { get; set; } = string.Empty;
    public string ClientUrl { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string ClientUrl { get; set; } = string.Empty;
}

public class HangfireSettings
{
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MfaSettings
{
    public int SessionExpiryHours { get; set; } = 8;
    public bool BypassForTesting { get; set; } = false;
}

public class OsPlacesSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.os.uk/search/places/v1";
}

public class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = [];
}

public class SeederSettings
{
    public bool UseExportedData { get; set; } = false;
    public bool FlushExistingData { get; set; } = false;
}