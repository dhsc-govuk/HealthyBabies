using Domain.Common;

namespace Domain.DataCollections.Forms.ValueObjects;

public static class FieldTypeConstants
{
    public const string Text = "text";
    public const string Number = "number";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Url = "url";
    public const string Checkbox = "checkbox";
    public const string Radio = "radio";
    public const string Select = "select";
    public const string MultiSelect = "multi_select";
    public const string File = "file";
    public const string Address = "address";
    public const string PostCode = "post_code";
    public const string Textarea = "textarea";
}

/// <summary>
/// Types of form fields supported by the dynamic form system.
/// </summary>
public class FieldType : ValueObject
{
    public string Value { get; private set; }

    private FieldType(string value)
    {
        Value = value;
    }

    public static FieldType From(string value)
    {
        var fieldType = new FieldType(value);

        if (!SupportedFieldTypes.Contains(fieldType))
        {
            throw new UnsupportedFieldTypeException(value);
        }

        return fieldType;
    }

    public static FieldType Text => new(FieldTypeConstants.Text);
    public static FieldType Number => new(FieldTypeConstants.Number);
    public static FieldType Email => new(FieldTypeConstants.Email);
    public static FieldType Phone => new(FieldTypeConstants.Phone);
    public static FieldType Url => new(FieldTypeConstants.Url);
    public static FieldType Checkbox => new(FieldTypeConstants.Checkbox);
    public static FieldType Radio => new(FieldTypeConstants.Radio);
    public static FieldType Select => new(FieldTypeConstants.Select);
    public static FieldType MultiSelect => new(FieldTypeConstants.MultiSelect);
    public static FieldType File => new(FieldTypeConstants.File);
    public static FieldType Address => new(FieldTypeConstants.Address);
    public static FieldType PostCode => new(FieldTypeConstants.PostCode);
    public static FieldType Textarea => new(FieldTypeConstants.Textarea);

    public static implicit operator string(FieldType fieldType)
    {
        return fieldType.ToString();
    }

    public static explicit operator FieldType(string value)
    {
        return From(value);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private static IEnumerable<FieldType> SupportedFieldTypes
    {
        get
        {
            yield return Text;
            yield return Number;
            yield return Email;
            yield return Phone;
            yield return Url;
            yield return Checkbox;
            yield return Radio;
            yield return Select;
            yield return MultiSelect;
            yield return File;
            yield return Address;
            yield return PostCode;
            yield return Textarea;
        }
    }
}

public class UnsupportedFieldTypeException(string value) : Exception($"FieldType \"{value}\" is not supported.");