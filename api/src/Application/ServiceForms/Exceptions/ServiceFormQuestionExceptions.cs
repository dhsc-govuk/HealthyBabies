using Domain.ServiceForms;

namespace Application.ServiceForms.Exceptions;

public abstract class ServiceFormQuestionException(ServiceFormQuestionId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ServiceFormQuestionId ServiceFormQuestionId { get; } = id;
}

public class ServiceFormQuestionNotFoundException(ServiceFormQuestionId id)
    : ServiceFormQuestionException(id, $"Service form question with id {id} does not exist");

public class ServiceFormQuestionDuplicateCodeException(string code)
    : ServiceFormQuestionException(ServiceFormQuestionId.Empty(), $"A service form question with code '{code}' already exists");

public class ServiceFormQuestionCannotDeletePredefinedException(ServiceFormQuestionId id, string code)
    : ServiceFormQuestionException(id, $"Cannot delete predefined service form question '{code}'");

public class ServiceFormQuestionUnknownException(ServiceFormQuestionId id, Exception innerException)
    : ServiceFormQuestionException(id, $"Unknown exception for service form question {id}", innerException);