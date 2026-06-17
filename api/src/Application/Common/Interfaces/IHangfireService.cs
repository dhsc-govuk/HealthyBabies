using System.Linq.Expressions;

namespace Application.Common.Interfaces;

public interface IHangfireService
{
    string Enqueue(Expression<Func<Task>> methodCall);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
}