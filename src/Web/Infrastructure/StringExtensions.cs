using System.Text;

namespace YummyZoom.Web.Infrastructure;

public static class StringExtensions
{
    /// <summary>
    /// Converts a PascalCase string to kebab-case.
    /// Example: "TodoLists" becomes "todo-lists".
    /// </summary>
    public static string ToKebabCase(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('-');
            }
            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
} 
