namespace Application.DataCollections.Exceptions;

public abstract class FormModuleException(string message) : Exception(message);

public class FormModuleNotFoundException(Guid id)
    : FormModuleException($"Form module with ID '{id}' was not found.");

public class FormModuleDuplicateCodeException(string code)
    : FormModuleException($"A form module with code '{code}' already exists.");

public class FormModuleHasFieldsException(Guid id)
    : FormModuleException($"Cannot delete form module with ID '{id}' because it has fields. Delete the fields first.");

public class FormSectionNotFoundException(Guid id)
    : FormModuleException($"Form section with ID '{id}' was not found.");

public class FormSectionHasFieldsException(Guid id)
    : FormModuleException($"Cannot delete form section with ID '{id}' because it has fields. Delete or reassign the fields first.");