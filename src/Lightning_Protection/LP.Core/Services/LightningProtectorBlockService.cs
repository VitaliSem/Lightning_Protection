using System;
using System.Collections.Generic;
using System.Globalization;
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

        public const string AttrNumber = "NUMBER";
        public const string AttrName = "NAME";
        public const string AttrTitle = "TITLE";
        public const string AttrType = "TYPE";
        public const string AttrHeightM = "HEIGHT_M";
        public const string AttrBaseHeightM = "BASE_HEIGHT_M";
        public const string AttrDescription = "DESCRIPTION";
        public const string AttrIsActive = "IS_ACTIVE";

        public const double MaxProtectorHeight = 150;

        public const int MaxNameLength = 100;
        public const int MaxTitleLength = 100;
        public const int MaxTypeLength = 100;
        public const int MaxDescriptionLength = 500;

        public const string HeightPrefix = "h=";
        public const string BaseHeightPrefix = "h0=";

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
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > max)
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

                double heightM = dto.HeightM;
                double? baseHeightM = dto.BaseHeightM;

                if (heightM <= 0)
                    return OperationResult<LightningProtectorBlockData>.Fail("Protector height must be greater than zero.");

                if (heightM > MaxProtectorHeight)
                    return OperationResult<LightningProtectorBlockData>.Fail("Protector height exceeds maximum supported value of 150 m.");

                if (baseHeightM.HasValue && (double.IsNaN(baseHeightM.Value) || double.IsInfinity(baseHeightM.Value)))
                    return OperationResult<LightningProtectorBlockData>.Fail("Base height must be a finite number.");

                string error = ValidateTextLength(dto.Name, MaxNameLength, "Name")
                    ?? ValidateTextLength(dto.Title, MaxTitleLength, "Title")
                    ?? ValidateTextLength(dto.Type, MaxTypeLength, "Type")
                    ?? ValidateTextLength(dto.Description, MaxDescriptionLength, "Description");

                if (error != null)
                    return OperationResult<LightningProtectorBlockData>.Fail(error);

                var data = new LightningProtectorBlockData(
                    GetNextNumber(dto.ExistingNumbers),
                    (dto.Name ?? string.Empty).Trim(),
                    (dto.Type ?? string.Empty).Trim(),
                    heightM,
                    true,
                    baseHeightM,
                    (dto.Description ?? string.Empty).Trim(),
                    (dto.Title ?? string.Empty).Trim());

                return OperationResult<LightningProtectorBlockData>.Ok(data);
            }
            catch (Exception ex)
            {
                return OperationResult<LightningProtectorBlockData>.Fail("An unexpected error occurred while creating the protector data.", ex);
            }
        }

        /// <summary>Attribute values of the block reference, keyed by attribute tag.</summary>
        public static IDictionary<string, string> ToAttributeValues(LightningProtectorBlockData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { AttrNumber, data.Number.ToString(CultureInfo.InvariantCulture) },
                { AttrName, data.Name ?? string.Empty },
                { AttrTitle, data.Title ?? string.Empty },
                { AttrType, data.Type ?? string.Empty },
                { AttrHeightM, HeightPrefix + data.HeightM.ToString("0.##", CultureInfo.InvariantCulture) },
                { AttrBaseHeightM, data.BaseHeightM.HasValue
                    ? BaseHeightPrefix + data.BaseHeightM.Value.ToString("0.##", CultureInfo.InvariantCulture)
                    : string.Empty },
                { AttrDescription, data.Description ?? string.Empty },
                { AttrIsActive, data.IsActive ? "true" : "false" }
            };
        }
    }
}
