using System.Collections.Generic;

namespace LP.Core.Constants
{
    public static class LightningProtectorAttributes
    {
        public const string Number = "NUMBER";
        public const string Name = "NAME";
        public const string Title = "TITLE";
        public const string Type = "TYPE";
        public const string HeightM = "HEIGHT_M";
        public const string BaseHeightM = "BASE_HEIGHT_M";
        public const string Description = "DESCRIPTION";
        public const string IsActive = "IS_ACTIVE";

        public const string HeightPrefix = "h=";
        public const string BaseHeightPrefix = "h0=";

        public static readonly IReadOnlyList<string> EditableTags = new[]
        {
            Name, Title, Type, HeightM, BaseHeightM, Description, IsActive
        };
    }
}
