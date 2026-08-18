using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Pachimon.Data;

namespace Pachimon.UI
{
    public sealed class DescriptionTemplateContext
    {
        private readonly Dictionary<string, string> _values =
            new(StringComparer.OrdinalIgnoreCase);

        public DescriptionTemplateContext Set(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A template value key is required.", nameof(key));
            }

            _values[key] = Convert.ToString(
                value,
                CultureInfo.InvariantCulture) ?? string.Empty;
            return this;
        }

        public bool TryGetValue(string key, out string value) =>
            _values.TryGetValue(key, out value);
    }

    public static class DescriptionTemplateFormatter
    {
        private static readonly Regex TokenPattern = new(
            @"\{(?<token>[^{}]+)\}",
            RegexOptions.CultureInvariant);

        public static string Format(
            string template,
            DescriptionTemplateContext context = null)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            return TokenPattern.Replace(
                template,
                match => ResolveToken(match, context));
        }

        private static string ResolveToken(
            Match match,
            DescriptionTemplateContext context)
        {
            var token = match.Groups["token"].Value.Trim();
            if (token.Equals("/color", StringComparison.OrdinalIgnoreCase))
            {
                return "</color>";
            }

            if (token.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                return "\n";
            }

            if (TryGetArgument(token, "icon", out var iconName)
                && TryGetAllocationType(iconName, out var iconType))
            {
                return AttributeRichText.GetIcon(iconType);
            }

            if (TryGetArgument(token, "color", out var colorName)
                && TryGetAllocationType(colorName, out var colorType))
            {
                return AttributeRichText.Colorize(colorType, string.Empty)
                    .Replace("</color>", string.Empty);
            }

            if (TryGetArgument(token, "value", out var valueKey)
                && context != null
                && context.TryGetValue(valueKey, out var value))
            {
                return value;
            }

            if (TryGetArgument(token, "term", out var term))
            {
                var separator = term.IndexOf('|');
                var termId = separator >= 0 ? term[..separator] : term;
                var label = separator >= 0 ? term[(separator + 1)..] : term;
                if (!string.IsNullOrWhiteSpace(termId)
                    && !string.IsNullOrWhiteSpace(label))
                {
                    return $"<link=\"term:{termId.Trim()}\"><u>{label.Trim()}</u></link>";
                }
            }

            return match.Value;
        }

        private static bool TryGetArgument(
            string token,
            string name,
            out string argument)
        {
            var prefix = name + ":";
            if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                argument = token[prefix.Length..].Trim();
                return argument.Length > 0;
            }

            argument = string.Empty;
            return false;
        }

        private static bool TryGetAllocationType(
            string name,
            out AllocationType type)
        {
            return Enum.TryParse(name, true, out type)
                && type >= AllocationType.Fire
                && type <= AllocationType.Dragon;
        }
    }
}
