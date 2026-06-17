using System.Linq.Expressions;
using Application.Common.Interfaces;

namespace Tests.Common.Services;

public class InMemoryHangfireService : IHangfireService
{
    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return string.Empty;
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return string.Empty;
    }
}