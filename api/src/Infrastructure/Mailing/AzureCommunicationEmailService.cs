using Application.Common.Interfaces;
using Application.Common.Settings;
using Azure;
using Azure.Communication.Email;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Mailing;

public static class EmailTemplates
{
    public const string WelcomeEmail = "App_Data/welcome_email.html";
    public const string TemporaryPasswordEmail = "App_Data/temporary_password_email.html";
}

public class AzureCommunicationEmailService(ApplicationSettings settings, IWebHostEnvironment hostingEnvironment)
    : IEmailNotificationService
{
    private readonly EmailClient _emailClient = new(settings.Smtp.EmailConnection);
    private readonly string _senderAddress = settings.Smtp.SenderEmail;
    private readonly string _clientUrl = settings.Smtp.ClientUrl;

    public async Task<Result<Unit>> SendWelcomeEmail(
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            var htmlTemplate = await File.ReadAllTextAsync(
                Path.Combine(hostingEnvironment.ContentRootPath, EmailTemplates.WelcomeEmail),
                cancellationToken);

            await SendAsync(
                email,
                "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                string.Format(htmlTemplate, _clientUrl),
                cancellationToken);

            return new Result<Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return new Result<Unit>(ex);
        }
    }

    public async Task<Result<Unit>> SendTemporaryPasswordEmail(
        string email,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            var htmlTemplate = await File.ReadAllTextAsync(
                Path.Combine(hostingEnvironment.ContentRootPath, EmailTemplates.TemporaryPasswordEmail),
                cancellationToken);

            await SendAsync(
                email,
                "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                string.Format(htmlTemplate, _clientUrl, temporaryPassword),
                cancellationToken);

            return new Result<Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return new Result<Unit>(ex);
        }
    }

    private async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new EmailMessage(
            senderAddress: _senderAddress,
            content: new EmailContent(subject) { Html = htmlBody },
            recipients: new EmailRecipients([new Azure.Communication.Email.EmailAddress(to)]));

        await _emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);
    }
}