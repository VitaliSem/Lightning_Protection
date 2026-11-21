using System.Collections.Generic;
using System.Globalization;

namespace LP.Core.Helpers
{
    public static class AttributeValueHelper
    {
        public static string Clean(string value) => (value ?? string.Empty).Trim();

        public static string GetValue(IDictionary<string, string> values, string tag) =>
            values != null && values.TryGetValue(tag, out var value) && value != null ? value : string.Empty;

        public static bool TryParseNumber(string value, out int number)
        {
            number = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                && number > 0;
        }
    }
}
