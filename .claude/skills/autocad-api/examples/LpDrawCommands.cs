// LP.Plugin/Commands/LpDrawCommands.cs — end-to-end sample:
// prompt -> LP.Core calculation -> layers -> zone outline -> annotative marker block with attributes -> label.
// C# 12, nullable, implicit usings; compiles for net48 (AutoCAD 2024) and net8.0 (AutoCAD 2025+).
//
// Assumptions: model units are metres, Model tab active; markers/labels annotative in paper mm,
// zone geometry 1:1 and not annotative. Requires a ProjectReference to LP.Core.
using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LP.Core.Enums;
using LP.Core.Models.Base;
using LP.Core.Models.LightningProtectors.DoubleRod;
using LP.Core.Models.LightningProtectors.SingleRod;
using LP.Core.Models.LightningProtectors.SingleWire;
using LP.Core.Services.SN_4_04_03;
using LP.Plugin.Acad;
using LP.Plugin.Drawing;
// Core.Application (AcCoreMgd) exists in both targets; the full Application (AcMgd) is not referenced on net8.0
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcException = Autodesk.AutoCAD.Runtime.Exception;

[assembly: CommandClass(typeof(LP.Plugin.Commands.LpDrawCommands))]

namespace LP.Plugin.Commands;

public class LpDrawCommands
{
    private const string RodMarkBlock = "LP-ROD-MARK";
    private const double LabelPaperHeight = 2.5;            // mm on the sheet
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly LightningProtectionService Service = new();

    [CommandMethod("LP_ROD")]
    public void DrawSingleRod()
    {
        if (!TryGetContext(out Document doc, out Database db, out Editor ed))
            return;

        if (!TryGetPoint(ed, "\nRod position: ", null, out Point3d pos)) return;
        if (!TryGetDouble(ed, "\nRod height h, m", 30.0, false, out double h)) return;
        if (!TryGetDouble(ed, "\nEvaluation height hx, m", 0.0, true, out double hx)) return;
        if (!TryGetReliability(ed, out LightningProtectionReliability reliability)) return;

        var protector = new SingleRodLightningProtector(ToPoint3D(pos), h);
        OperationResult<ProtectionArea> result = Service.GetSingleRodProtectionArea(protector, reliability, hx);
        if (!CheckResult(ed, result))
            return;

        double rx = result.Result.Points[0].Radius ?? 0.0;

        Run(ed, () =>
        {
            using Transaction tr = db.TransactionManager.StartTransaction();
            LayerHelper.EnsureProjectLayers(tr, db);
            string scale = db.Cannoscale.Name;

            ProtectionAreaDrawer.Draw(tr, db, result.Result, LpLayers.Zone);
            InsertRodMark(tr, db, pos, h, rx, scale);

            string label = string.Format(Inv, "h = {0:0.##} m\\Phx = {1:0.##} m\\PRx = {2:0.##} m", h, hx, rx);
            Point3d labelPos = pos + new Vector3d(rx * 0.7071, rx * 0.7071, 0);   // on the zone, at 45°
            TextHelper.CreateAnnotativeMText(tr, db, label, labelPos, LabelPaperHeight, scale,
                AttachmentPoint.BottomLeft, LpLayers.Annotation);

            tr.Commit();
            ed.WriteMessage(string.Format(Inv, "\nProtection zone drawn: Rx = {0:0.###} m at hx = {1:0.##} m.", rx, hx));
        });
    }

    [CommandMethod("LP_WIRE")]
    public void DrawSingleWire()
    {
        if (!TryGetContext(out Document doc, out Database db, out Editor ed))
            return;

        if (!TryGetPoint(ed, "\nFirst support: ", null, out Point3d a)) return;
        if (!TryGetPoint(ed, "\nSecond support: ", a, out Point3d b)) return;
        if (!TryGetDouble(ed, "\nWire height at the first support, m", 20.0, false, out double h1)) return;
        if (!TryGetDouble(ed, "\nWire height at the second support, m", h1, false, out double h2)) return;
        if (!TryGetDouble(ed, "\nEvaluation height hx, m", 0.0, true, out double hx)) return;
        if (!TryGetReliability(ed, out LightningProtectionReliability reliability)) return;

        var protector = new SingleWireLightningProtector(ToPoint3D(a), h1, ToPoint3D(b), h2);
        OperationResult<ProtectionArea> result = Service.GetSingleWireProtectionArea(protector, reliability, hx);
        if (!CheckResult(ed, result))
            return;

        Run(ed, () =>
        {
            using Transaction tr = db.TransactionManager.StartTransaction();
            LayerHelper.EnsureProjectLayers(tr, db);

            ProtectionAreaDrawer.Draw(tr, db, result.Result, LpLayers.Zone);   // two arcs + external tangents

            var wire = new Line(a, b);
            wire.SetDatabaseDefaults(db);
            wire.Layer = LpLayers.Protector;
            PrimitivesHelper.AppendToModelSpace(tr, db, wire);

            tr.Commit();
        });
    }

    [CommandMethod("LP_DROD")]
    public void DrawDoubleRod()
    {
        if (!TryGetContext(out Document doc, out Database db, out Editor ed))
            return;

        if (!TryGetPoint(ed, "\nFirst rod: ", null, out Point3d p1)) return;
        if (!TryGetPoint(ed, "\nSecond rod: ", p1, out Point3d p2)) return;
        if (!TryGetDouble(ed, "\nFirst rod height, m", 30.0, false, out double h1)) return;
        if (!TryGetDouble(ed, "\nSecond rod height, m", h1, false, out double h2)) return;
        if (!TryGetDouble(ed, "\nEvaluation height hx, m", 0.0, true, out double hx)) return;
        if (!TryGetReliability(ed, out LightningProtectionReliability reliability)) return;

        var protector = new DoubleRodLightningProtector(
            new SingleRodLightningProtector(ToPoint3D(p1), h1),
            new SingleRodLightningProtector(ToPoint3D(p2), h2));

        OperationResult<ProtectionArea> result = Service.GetDoubleRodProtectionArea(protector, reliability, hx);
        if (!CheckResult(ed, result))
            return;

        Run(ed, () =>
        {
            using Transaction tr = db.TransactionManager.StartTransaction();
            LayerHelper.EnsureProjectLayers(tr, db);
            string scale = db.Cannoscale.Name;

            // one closed outline (combined zone) or two circles (rods too far apart)
            IReadOnlyList<ObjectId> ids = ProtectionAreaDrawer.Draw(tr, db, result.Result, LpLayers.Zone);
            InsertRodMark(tr, db, p1, h1, null, scale);
            InsertRodMark(tr, db, p2, h2, null, scale);

            tr.Commit();
            ed.WriteMessage(ids.Count > 1
                ? "\nRods are too far apart: two separate protection zones drawn."
                : "\nCombined protection zone drawn.");
        });
    }

    // ---------- Marker block (annotative, geometry in paper mm) ----------

    private static void InsertRodMark(Transaction tr, Database db, Point3d pos, double h, double? rx, string scale)
    {
        var attributes = new Dictionary<string, string> { ["H"] = h.ToString("0.##", Inv) };
        if (rx is double r)
            attributes["RX"] = r.ToString("0.##", Inv);

        BlockHelper.InsertAnnotativeBlock(tr, db, EnsureRodMarkBlock(tr, db), pos, scale, 0.0, LpLayers.Protector, attributes);
    }

    private static ObjectId EnsureRodMarkBlock(Transaction tr, Database db)
    {
        ObjectId id = BlockHelper.GetBlockId(tr, db, RodMarkBlock);
        if (!id.IsNull)
            return id;

        const double r = 1.5;                               // 3 mm circle on the sheet
        List<Entity> geometry =
        [
            new Circle(Point3d.Origin, Vector3d.ZAxis, r),
            new Line(new Point3d(-r, 0, 0), new Point3d(r, 0, 0)),
            new Line(new Point3d(0, -r, 0), new Point3d(0, r, 0)),
            BlockHelper.NewAttributeDefinition(db, "H", "Rod height, m: ", "0", new Point3d(0, r + 1.5, 0), 2.0),
            BlockHelper.NewAttributeDefinition(db, "RX", "Protection radius, m: ", "", new Point3d(0, -r - 1.5, 0), 2.0,
                invisible: true),
        ];

        foreach (Entity e in geometry)
        {
            e.Layer = "0";                                  // take layer/color from the reference
            e.ColorIndex = 0;                               // ByBlock
        }

        return BlockHelper.EnsureBlock(tr, db, RodMarkBlock, Point3d.Origin, geometry, "Lightning rod marker", annotative: true);
    }

    // ---------- Plumbing ----------

    private static bool TryGetContext(out Document doc, out Database db, out Editor ed)
    {
        doc = AcApp.DocumentManager.MdiActiveDocument;
        db = doc?.Database!;
        ed = doc?.Editor!;
        if (doc is null)
            return false;

        if (!db.TileMode)
        {
            ed.WriteMessage("\nSwitch to the Model tab first.");
            return false;
        }
        return true;
    }

    private static bool CheckResult(Editor ed, OperationResult<ProtectionArea> result)
    {
        if (result.IsSuccess)
            return true;

        ed.WriteMessage("\nCalculation failed: " + result.Message);
        return false;
    }

    /// <summary>Runs the drawing step and reports errors instead of letting them escape into AutoCAD.</summary>
    private static void Run(Editor ed, Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException ex)                        // e.g. radius too small for the chord
        {
            ed.WriteMessage("\nCannot build the outline: " + ex.Message);
        }
        catch (NotImplementedException ex)                  // double wire is not implemented in LP.Core yet
        {
            ed.WriteMessage("\n" + ex.Message);
        }
        catch (AcException ex)
        {
            ed.WriteMessage($"\nAutoCAD error: {ex.ErrorStatus} - {ex.Message}");
        }
    }

    private static Point3D ToPoint3D(Point3d p) => new(p.X, p.Y, p.Z);

    /// <summary>Picks a point (UCS) and returns it in WCS. basePoint (WCS) draws a rubber-band line.</summary>
    private static bool TryGetPoint(Editor ed, string message, Point3d? basePoint, out Point3d point)
    {
        var ppo = new PromptPointOptions(message);
        Matrix3d ucs = ed.CurrentUserCoordinateSystem;
        if (basePoint is Point3d bp)
        {
            ppo.UseBasePoint = true;
            ppo.BasePoint = bp.TransformBy(ucs.Inverse());  // BasePoint is in UCS
        }

        PromptPointResult res = ed.GetPoint(ppo);
        point = res.Status == PromptStatus.OK ? res.Value.TransformBy(ucs) : Point3d.Origin;
        return res.Status == PromptStatus.OK;
    }

    private static bool TryGetDouble(Editor ed, string message, double defaultValue, bool allowZero, out double value)
    {
        var pdo = new PromptDoubleOptions(message + ": ")
        {
            AllowNegative = false,
            AllowZero = allowZero,
            DefaultValue = defaultValue,
            UseDefaultValue = true,
        };

        PromptDoubleResult res = ed.GetDouble(pdo);
        value = res.Status == PromptStatus.OK ? res.Value : 0.0;
        return res.Status == PromptStatus.OK;
    }

    private static bool TryGetReliability(Editor ed, out LightningProtectionReliability reliability)
    {
        var pko = new PromptKeywordOptions("\nProtection reliability [P900/P990/P999]: ", "P900 P990 P999")
        {
            AllowNone = true,
        };
        pko.Keywords.Default = "P990";

        PromptResult res = ed.GetKeywords(pko);
        reliability = LightningProtectionReliability.P0_990;
        if (res.Status != PromptStatus.OK && res.Status != PromptStatus.None)
            return false;

        reliability = (res.Status == PromptStatus.None ? "P990" : res.StringResult) switch
        {
            "P900" => LightningProtectionReliability.P0_900,
            "P999" => LightningProtectionReliability.P0_999,
            _ => LightningProtectionReliability.P0_990,
        };
        return true;
    }
}
