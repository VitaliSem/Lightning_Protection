using System.Globalization;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using LP.Core.Services;
using LP.Plugin.Models;

namespace LP.Plugin.Acad;

public static class PromptHelper
{
    /// <summary>Prompts for a point and returns it in world coordinates.</summary>
    public static bool TryGetPoint(Editor ed, string message, out Point3d point)
    {
        PromptPointResult res = ed.GetPoint(new PromptPointOptions(message));
        point = res.Status == PromptStatus.OK ? res.Value.TransformBy(ed.CurrentUserCoordinateSystem) : Point3d.Origin;

        return res.Status == PromptStatus.OK;
    }

    public const string ClearTextInput = ".";

    private const string ClearKeyword = "Clear";
    private const string YesKeyword = "Yes";
    private const string NoKeyword = "No";

    /// <summary>
    /// Prompts for optional text, repeating the prompt until the input is valid.
    /// When the prompt has a default value, an empty input keeps it and the clear input empties it.
    /// </summary>
    public static bool TryGetString(Editor ed, TextPromptDto prompt, out string value)
    {
        var pso = new PromptStringOptions(prompt.Message) { AllowSpaces = true };

        if (!string.IsNullOrEmpty(prompt.DefaultValue))
        {
            pso.DefaultValue = prompt.DefaultValue;
            pso.UseDefaultValue = true;
        }

        while (true)
        {
            PromptResult res = ed.GetString(pso);

            if (res.Status != PromptStatus.OK)
            {
                value = "";
                return false;
            }

            value = res.StringResult ?? "";

            if (prompt.DefaultValue is not null && value.Trim() == ClearTextInput)
                value = "";

            string? error = LightningProtectorBlockService.ValidateTextLength(value, prompt.MaxLength, prompt.FieldName);

            if (error is null)
                return true;

            ed.WriteMessage("\n" + error);
        }
    }

    public static bool TryGetOptionalDouble(Editor ed, string message, out double? value)
    {
        var pdo = new PromptDoubleOptions(message)
        {
            AllowNone = true,
            AllowZero = true,
            AllowNegative = true,
        };

        PromptDoubleResult res = ed.GetDouble(pdo);
        value = res.Status == PromptStatus.OK ? res.Value : null;

        return res.Status is PromptStatus.OK or PromptStatus.None;
    }

    /// <summary>
    /// Prompts for an optional number that can be kept with an empty input or removed with a keyword.
    /// </summary>
    public static bool TryGetClearableDouble(Editor ed, NumberPromptDto prompt, out double? value)
    {
        var pdo = new PromptDoubleOptions(prompt.Message)
        {
            AllowNone = true,
            AllowZero = true,
            AllowNegative = true,
        };

        pdo.Keywords.Add(ClearKeyword);

        if (prompt.DefaultValue.HasValue)
        {
            pdo.DefaultValue = prompt.DefaultValue.Value;
            pdo.UseDefaultValue = true;
        }

        PromptDoubleResult res = ed.GetDouble(pdo);

        value = res.Status switch
        {
            PromptStatus.OK => res.Value,
            PromptStatus.None => prompt.DefaultValue,
            _ => null,
        };

        return res.Status is PromptStatus.OK or PromptStatus.None or PromptStatus.Keyword;
    }

    public static bool TryGetPositiveInt(Editor ed, string message, out int value)
    {
        var pio = new PromptIntegerOptions(message)
        {
            AllowNone = false,
            AllowZero = false,
            AllowNegative = false,
        };

        PromptIntegerResult res = ed.GetInteger(pio);
        value = res.Status == PromptStatus.OK ? res.Value : 0;

        return res.Status == PromptStatus.OK;
    }

    /// <summary>Prompts for a Yes/No choice; the incoming value is the default and an empty input keeps it.</summary>
    public static bool TryGetYesNo(Editor ed, string message, ref bool value)
    {
        var pko = new PromptKeywordOptions(message) { AllowNone = true };

        pko.Keywords.Add(YesKeyword);
        pko.Keywords.Add(NoKeyword);
        pko.Keywords.Default = value ? YesKeyword : NoKeyword;

        PromptResult res = ed.GetKeywords(pko);

        if (res.Status == PromptStatus.OK)
            value = string.Equals(res.StringResult, YesKeyword, StringComparison.OrdinalIgnoreCase);

        return res.Status is PromptStatus.OK or PromptStatus.None;
    }

    /// <summary>Prompts for a required positive number, repeating the prompt until the value is within the limit.</summary>
    public static bool TryGetPositiveDouble(Editor ed, NumberPromptDto prompt, out double value)
    {
        var pdo = new PromptDoubleOptions(prompt.Message)
        {
            AllowNone = false,
            AllowZero = false,
            AllowNegative = false,
        };

        if (prompt.DefaultValue.HasValue)
        {
            pdo.DefaultValue = prompt.DefaultValue.Value;
            pdo.UseDefaultValue = true;
        }

        while (true)
        {
            PromptDoubleResult res = ed.GetDouble(pdo);

            if (res.Status != PromptStatus.OK)
            {
                value = 0.0;
                return false;
            }

            value = res.Value;

            if (value <= prompt.MaxValue)
                return true;

            string unit = string.IsNullOrEmpty(prompt.Unit) ? "" : " " + prompt.Unit;

            ed.WriteMessage(string.Format(CultureInfo.InvariantCulture, "\n{0} must not exceed {1}{2}.",
                prompt.FieldName, prompt.MaxValue, unit));
        }
    }
}
