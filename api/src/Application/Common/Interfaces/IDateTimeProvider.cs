namespace Application.Common.Interfaces;

public interface IDateTimeProvider
{
    System.DateTime GetNow();
    System.DateTime GetUtcNow();
}