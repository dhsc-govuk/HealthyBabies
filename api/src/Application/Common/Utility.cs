using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common;

public static class Utility
{
    public static string ReplaceSpaceWithDash(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input, @"\s+", "-");
    }

    private const string Base64FieldPrefix = "b64:";

    public static string HtmlDecodeField(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Base64FieldPrefix, StringComparison.Ordinal))
        {
            return value;
        }

        var payload = value.Substring(Base64FieldPrefix.Length);
        return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
    }

    public static string? HtmlDecodeNullableField(string? value)
        => value is null ? null : HtmlDecodeField(value);

    public static string GetDescription(this Enum value)
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null)
        {
            FieldInfo field = type.GetField(name);
            if (field != null && Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute descriptionAttribute)
            {
                return descriptionAttribute.Description ?? name;
            }
        }

        return null;
    }
}