using LanguageExt;
using LanguageExt.Common;

namespace Application.Common.Interfaces;

public interface IEmailNotificationService
{
    Task<Result<Unit>> SendWelcomeEmail(
        string email,
        CancellationToken cancellationToken);

    Task<Result<Unit>> SendTemporaryPasswordEmail(
        string email,
        string temporaryPassword,
        CancellationToken cancellationToken);
}