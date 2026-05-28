using System.Net;
using System.Net.Mail;
using Application.Common.Interfaces;
using Application.Common.Settings;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Mailing;

public class SmtpEmailNotificationService(ApplicationSettings settings, IWebHostEnvironment hostingEnvironment)
    : ISmtpEmailNotificationService
{
    private readonly SmtpSettings _smtp = settings.Smtp;
    private readonly string _clientUrl = settings.Smtp.ClientUrl;

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

            await SendAsync(
                email,
                "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                string.Format(htmlTemplate, _clientUrl));

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
                Path.Combine(
                    hostingEnvironment.ContentRootPath,
                    EmailTemplates.TemporaryPasswordEmail),
                cancellationToken);

            await SendAsync(
                email,
                "Sign in to Report Data on Best Start Family Hubs and Healthy Babies",
                string.Format(htmlTemplate, _clientUrl, temporaryPassword));

            return new Result<Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return new Result<Unit>(ex);
        }
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.SenderEmail, _smtp.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrEmpty(_smtp.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_smtp.Username, _smtp.Password),
        };

        await client.SendMailAsync(message);
    }
}