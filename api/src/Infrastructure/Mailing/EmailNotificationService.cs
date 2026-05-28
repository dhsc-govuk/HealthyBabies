using Application.Common.Interfaces;
using Application.Common.Settings;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Hosting;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Infrastructure.Mailing;

public static class EmailTemplates
{
    public const string WelcomeEmail = "App_Data/welcome_email.html";
    public const string TemporaryPasswordEmail = "App_Data/temporary_password_email.html";
}

public class EmailNotificationService(ApplicationSettings settings, IWebHostEnvironment hostingEnvironment)
    : IEmailNotificationService
{
    private readonly SendGridClient _sendGridClient = new(settings.SendGrid.EmailApiKey);

    private readonly string _clientUrl = settings.SendGrid.ClientUrl;
    private readonly string _senderEmail = settings.SendGrid.SenderEmail;

    public async Task<Result<Unit>> SendWelcomeEmail(
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            var htmlTemplate = await File.ReadAllTextAsync(
                Path.Combine(
                    hostingEnvironment.ContentRootPath,
                    EmailTemplates.WelcomeEmail),
                cancellationToken);

            var from = new EmailAddress(_senderEmail);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(
                from: from,
                to: to,
                subject: "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                plainTextContent: null,
                htmlContent: string.Format(htmlTemplate, _clientUrl));

            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

            return response.IsSuccessStatusCode
                ? new Result<Unit>(Unit.Default)
                : new Result<Unit>(new ArgumentException(await response.Body.ReadAsStringAsync(cancellationToken)));
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
                Path.Combine(
                    hostingEnvironment.ContentRootPath,
                    EmailTemplates.TemporaryPasswordEmail),
                cancellationToken);

            var from = new EmailAddress(_senderEmail);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(
                from: from,
                to: to,
                subject: "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                plainTextContent: null,
                htmlContent: string.Format(htmlTemplate, _clientUrl, temporaryPassword));

            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

            return response.IsSuccessStatusCode
                ? new Result<Unit>(Unit.Default)
                : new Result<Unit>(new ArgumentException(await response.Body.ReadAsStringAsync(cancellationToken)));
        }
        catch (Exception ex)
        {
            return new Result<Unit>(ex);
        }
    }
}