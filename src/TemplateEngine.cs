using System.Text;
using System.Text.RegularExpressions;

namespace EzLiveServer;

public static class TemplateEngine
{

    private const string propertyRegexString = @"(?:[^@]|^)(@(\w+))";
    private static readonly Regex propertyRegex = new(propertyRegexString, RegexOptions.Multiline | RegexOptions.Compiled);

    public static string Run(string template, IReadOnlyDictionary<string, object> modelValues)
    {
        var templateProperties = FindProperties(template);
        StringBuilder stringBuilder = new(template.Length);
        int index = 0;

        foreach (var property in templateProperties)
        {
            stringBuilder.Append(template.AsSpan(index, property.Index - index));
            index = property.Index + property.Length;

            string? value = modelValues.TryGetValue(property.Name, out var modelValue) ? modelValue.ToString() : property.Raw;
            stringBuilder.Append(value);
        }
        stringBuilder.Append(template.AsSpan(index));

        return stringBuilder.ToString();
    }

    public static IEnumerable<TemplateProperty> FindProperties(string template)
    {
        var propertyRegexMatches = propertyRegex.Matches(template);
        var properties = new TemplateProperty[propertyRegexMatches.Count];

        for (int i = 0; i < propertyRegexMatches.Count; i++)
        {
            var propertyRegex = propertyRegexMatches[i];

            properties[i] = new TemplateProperty(
                propertyRegex.Groups[1].Value,
                propertyRegex.Groups[2].Value,
                propertyRegex.Groups[1].Index,
                propertyRegex.Groups[1].Length);
        }

        return properties;
    }
}

public record TemplateProperty(string Raw, string Name, int Index, int Length);