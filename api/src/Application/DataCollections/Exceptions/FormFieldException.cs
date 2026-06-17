using Domain.DataCollections.Forms;

namespace Application.DataCollections.Exceptions;

public abstract class FormFieldException(FormFieldId id, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public FormFieldId FormFieldId { get; } = id;
}

public class FormFieldNotFoundException(FormFieldId id)
    : FormFieldException(id, $"Form field with id {id} does not exist");

public class FormFieldDuplicateKeyException(string fieldKey, string formModuleCode)
    : FormFieldException(FormFieldId.Empty(), $"A form field with key '{fieldKey}' already exists in form module '{formModuleCode}'");

public class FormFieldFormModuleNotFoundException(Guid formModuleId)
    : FormFieldException(FormFieldId.Empty(), $"Form module with id {formModuleId} does not exist");

public class FormFieldHasValuesException(FormFieldId id)
    : FormFieldException(id, $"Cannot delete form field with ID '{id.Value}' because it has submitted values. Deactivate the field instead.");

public class FormFieldUnknownException(FormFieldId id, Exception innerException)
    : FormFieldException(id, $"Unknown exception for form field {id}", innerException);