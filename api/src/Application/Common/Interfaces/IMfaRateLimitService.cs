using Domain.Users;

namespace Application.Common.Interfaces;

public interface IMfaRateLimitService
{
    bool IsRateLimited(UserId userId);
    void RecordAttempt(UserId userId);
    void ResetAttempts(UserId userId);
    int GetRemainingAttempts(UserId userId);
}