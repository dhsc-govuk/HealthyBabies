using System.Linq.Expressions;
using Application.Common.Interfaces;
using Hangfire;

namespace Infrastructure.Hangfire;

public class HangfireService : IHangfireService
{
    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return BackgroundJob.Enqueue(methodCall);
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return BackgroundJob.Schedule(methodCall, delay);
    }
}