using System;
using System.Collections.Generic;
using System.Globalization;
using LP.Core.Constants;
using LP.Core.Helpers;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.Block;

namespace LP.Core.Services
{
    /// <summary>
    /// Data rules of the lightning protector block: numbering, height validation and attribute values.
    /// </summary>
    public static class LightningProtectorBlockService
    {
        public const string BlockName = "B_LP_Lightning_Protector";
        public const string LayerName = "L_LP_Lightning_Protectors";

        public const double MaxProtectorHeight = 150;

        public const int MaxNameLength = 100;
        public const int MaxTitleLength = 100;
        public const int MaxTypeLength = 100;
        public const int MaxDescriptionLength = 500;

        /// <summary>
        /// Returns an error message when the trimmed text is longer than maxLength characters, otherwise null.
        /// </summary>
        public static string ValidateTextLength(string value, int maxLength, string fieldName)
        {
            int length = (value ?? string.Empty).Trim().Length;

            return length > maxLength
                ? string.Format(CultureInfo.InvariantCulture, "{0} must not exceed {1} characters (entered {2}).", fieldName, maxLength, length)
                : null;
        }

        /// <summary>
        /// Returns one greater than the maximum positive integer among the existing numbers, or 1 if there is none.
        /// Blank and non-integer values are ignored.
        /// </summary>
        public static int GetNextNumber(IEnumerable<string> existingNumbers)
        {
            if (existingNumbers == null) return 1;

            int max = 0;

            foreach (string value in existingNumbers)
            {
                if (AttributeValueHelper.TryParseNumber(value, out var number) && number > max)
                    max = number;
            }

            return max + 1;
        }

        /// <summary>Validates the input and builds the data of a new lightning protector block.</summary>
        public static OperationResult<LightningProtectorBlockData> Create(LightningProtectorBlockCreateDto dto)
        {
            try
            {
                if (dto == null)
                    return OperationResult<LightningProtectorBlockData>.Fail("Protector data cannot be null.");

                var data = new LightningProtectorBlockData(
                    GetNextNumber(dto.ExistingNumbers),
                    AttributeValueHelper.Clean(dto.Name),
                    AttributeValueHelper.Clean(dto.Type),
                    dto.HeightM,
                    true,
                    dto.BaseHeightM,
                    AttributeValueHelper.Clean(dto.Description),
                    AttributeValueHelper.Clean(dto.Title));

                string error = Validate(data);

                return error == null
                    ? OperationResult<LightningProtectorBlockData>.Ok(data)
                    : OperationResult<LightningProtectorBlockData>.Fail(error);
            }
            catch (Exception ex)
            {
                return OperationResult<LightningProtectorBlockData>.Fail("An unexpected error occurred while creating the protector data.", ex);
            }
        }

        /// <summary>Validates the new values and builds the data of an existing lightning protector block, keeping its number.</summary>
        public static OperationResult<LightningProtectorBlockData> Update(LightningProtectorBlockData current, LightningProtectorBlockUpdateDto dto)
        {
            try
            {
                if (current == null)
                    return OperationResult<LightningProtectorBlockData>.Fail("Current protector data cannot be null.");

                if (dto == null)
                    return OperationResult<LightningProtectorBlockData>.Fail("Protector data cannot be null.");

                var data = new LightningProtectorBlockData(
                    current.Number,
                    AttributeValueHelper.Clean(dto.Name),
                    AttributeValueHelper.Clean(dto.Type),
                    dto.HeightM,
                    dto.IsActive,
                    dto.BaseHeightM,
                    AttributeValueHelper.Clean(dto.Description),
                    AttributeValueHelper.Clean(dto.Title));

                string error = Validate(data);

                return error == null
                    ? OperationResult<LightningProtectorBlockData>.Ok(data)
                    : OperationResult<LightningProtectorBlockData>.Fail(error);
            }
            catch (Exception ex)
            {
                return OperationResult<LightningProtectorBlockData>.Fail("An unexpected error occurred while updating the protector data.", ex);
            }
        }

        /// <summary>Restores the protector data from the attribute values of a block reference.</summary>
        public static OperationResult<LightningProtectorBlockData> FromAttributeValues(IDictionary<string, string> values)
        {
            try
            {
                if (values == null)
                    return OperationResult<LightningProtectorBlockData>.Fail("Attribute values cannot be null.");

                var lookup = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

                if (!AttributeValueHelper.TryParseNumber(AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.Number), out int number))
                    return OperationResult<LightningProtectorBlockData>.Fail("The protector number attribute is missing or invalid.");

                if (!TryParseMeters(AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.HeightM), LightningProtectorAttributes.HeightPrefix, out double? heightM) || !heightM.HasValue)
                    return OperationResult<LightningProtectorBlockData>.Fail(
                        string.Format(CultureInfo.InvariantCulture, "The height attribute of protector #{0} is missing or invalid.", number));

                if (!TryParseMeters(AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.BaseHeightM), LightningProtectorAttributes.BaseHeightPrefix, out double? baseHeightM))
                    return OperationResult<LightningProtectorBlockData>.Fail(
                        string.Format(CultureInfo.InvariantCulture, "The base height attribute of protector #{0} is invalid.", number));

                bool isActive = !string.Equals(AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.IsActive).Trim(), "false", StringComparison.OrdinalIgnoreCase);

                var data = new LightningProtectorBlockData(
                    number,
                    AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.Name),
                    AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.Type),
                    heightM.Value,
                    isActive,
                    baseHeightM,
                    AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.Description),
                    AttributeValueHelper.GetValue(lookup, LightningProtectorAttributes.Title));

                return OperationResult<LightningProtectorBlockData>.Ok(data);
            }
            catch (Exception ex)
            {
                return OperationResult<LightningProtectorBlockData>.Fail("An unexpected error occurred while reading the protector data.", ex);
            }
        }

        private static string Validate(LightningProtectorBlockData data)
        {
            if (data.HeightM <= 0)
                return "Protector height must be greater than zero.";

            if (data.HeightM > MaxProtectorHeight)
                return "Protector height exceeds maximum supported value of 150 m.";

            if (data.BaseHeightM.HasValue && (double.IsNaN(data.BaseHeightM.Value) || double.IsInfinity(data.BaseHeightM.Value)))
                return "Base height must be a finite number.";

            return ValidateTextLength(data.Name, MaxNameLength, "Name")
                ?? ValidateTextLength(data.Title, MaxTitleLength, "Title")
                ?? ValidateTextLength(data.Type, MaxTypeLength, "Type")
                ?? ValidateTextLength(data.Description, MaxDescriptionLength, "Description");
        }

        /// <summary>
        /// Parses a meter value written with an optional prefix; a blank text yields no value.
        /// Returns false when the text is not a finite number.
        /// </summary>
        private static bool TryParseMeters(string text, string prefix, out double? value)
        {
            value = null;

            string trimmed = (text ?? string.Empty).Trim();

            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(prefix.Length).Trim();

            if (trimmed.Length == 0)
                return true;

            if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                || double.IsNaN(parsed) || double.IsInfinity(parsed))
                return false;

            value = parsed;

            return true;
        }

        /// <summary>Attribute values of the block reference, keyed by attribute tag.</summary>
        public static IDictionary<string, string> ToAttributeValues(LightningProtectorBlockData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { LightningProtectorAttributes.Number, data.Number.ToString(CultureInfo.InvariantCulture) },
                { LightningProtectorAttributes.Name, data.Name ?? string.Empty },
                { LightningProtectorAttributes.Title, data.Title ?? string.Empty },
                { LightningProtectorAttributes.Type, data.Type ?? string.Empty },
                { LightningProtectorAttributes.HeightM, LightningProtectorAttributes.HeightPrefix + data.HeightM.ToString("0.##", CultureInfo.InvariantCulture) },
                { LightningProtectorAttributes.BaseHeightM, data.BaseHeightM.HasValue
                    ? LightningProtectorAttributes.BaseHeightPrefix + data.BaseHeightM.Value.ToString("0.##", CultureInfo.InvariantCulture)
                    : string.Empty },
                { LightningProtectorAttributes.Description, data.Description ?? string.Empty },
                { LightningProtectorAttributes.IsActive, data.IsActive ? "true" : "false" }
            };
        }
    }
}
