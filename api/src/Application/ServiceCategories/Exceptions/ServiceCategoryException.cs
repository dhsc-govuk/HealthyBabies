using Domain.ServiceCategories;

namespace Application.ServiceCategories.Exceptions;

public abstract class ServiceCategoryException(ServiceCategoryId serviceCategoryId, string message) : Exception(message)
{
    public ServiceCategoryId ServiceCategoryId { get; } = serviceCategoryId;
}

public class ServiceCategoryDoesNotExistException(ServiceCategoryId serviceCategoryId)
    : ServiceCategoryException(serviceCategoryId, $"Service category with id {serviceCategoryId} does not exist");

public class ServiceCategoryAlreadyExistsException(ServiceCategoryId serviceCategoryId, string categoryCode)
    : ServiceCategoryException(serviceCategoryId, $"Service category with code {categoryCode} already exists for this organisation");

public class ServiceCategoryArgumentException(ServiceCategoryId serviceCategoryId, string message)
    : ServiceCategoryException(serviceCategoryId, message);

public class ServiceCategoryUnknownException(ServiceCategoryId serviceCategoryId, Exception innerException)
    : ServiceCategoryException(serviceCategoryId, innerException.Message);