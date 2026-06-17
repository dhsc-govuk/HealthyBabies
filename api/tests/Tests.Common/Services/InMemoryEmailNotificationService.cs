using Application.Common.Interfaces;
using LanguageExt;
using LanguageExt.Common;

namespace Tests.Common.Services;

public class InMemoryEmailNotificationService : IEmailNotificationService
{
    public Task<Result<Unit>> SendTaskReviewReminderEmail(string email, string userName, string taskName, DateTime? completionDate, DateTime reviewDate, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result<Unit>(Unit.Default));
    }

    public Task<Result<Unit>> SendWelcomeEmail(string email, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result<Unit>(Unit.Default));
    }

    public Task<Result<Unit>> SendTemporaryPasswordEmail(string email, string temporaryPassword, CancellationToken cancellationToken)
    {
        return Task.FromResult(new Result<Unit>(Unit.Default));
    }
}