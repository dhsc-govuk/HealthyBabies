using Domain.SiteForms;

namespace Application.SiteForms.Exceptions;

public abstract class SiteFormQuestionException(SiteFormQuestionId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public SiteFormQuestionId SiteFormQuestionId { get; } = id;
}

public class SiteFormQuestionNotFoundException(SiteFormQuestionId id)
    : SiteFormQuestionException(id, $"Site form question with id {id} does not exist");

public class SiteFormQuestionDuplicateCodeException(string code)
    : SiteFormQuestionException(SiteFormQuestionId.Empty(), $"A site form question with code '{code}' already exists");

public class SiteFormQuestionCannotDeletePredefinedException(SiteFormQuestionId id, string code)
    : SiteFormQuestionException(id, $"Cannot delete predefined site form question '{code}'");

public class SiteFormQuestionUnknownException(SiteFormQuestionId id, Exception innerException)
    : SiteFormQuestionException(id, $"Unknown exception for site form question {id}", innerException);