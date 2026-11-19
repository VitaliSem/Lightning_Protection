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

    /// <summary>Prompts for optional text, repeating the prompt until the input is valid.</summary>
    public static bool TryGetString(Editor ed, TextPromptDto prompt, out string value)
    {
        var pso = new PromptStringOptions(prompt.Message) { AllowSpaces = true };

        while (true)
        {
            PromptResult res = ed.GetString(pso);

            if (res.Status != PromptStatus.OK)
            {
                value = "";
                return false;
            }

            value = res.StringResult ?? "";
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

    /// <summary>Prompts for a required positive number, repeating the prompt until the value is within the limit.</summary>
    public static bool TryGetPositiveDouble(Editor ed, NumberPromptDto prompt, out double value)
    {
        var pdo = new PromptDoubleOptions(prompt.Message)
        {
            AllowNone = false,
            AllowZero = false,
            AllowNegative = false,
        };

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
