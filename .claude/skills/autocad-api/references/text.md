# Text: `DBText`, `MText`, text styles

Source: `text-guide.md`. Full code: `../examples/TextHelper.cs`.

| | `DBText` (TEXT) | `MText` (MTEXT) |
| --- | --- | --- |
| Insertion point | `Position` | `Location` |
| Content | `TextString` (no line breaks) | `Contents` (with codes), `Text` (plain, read-only) |
| Height | `Height` | `TextHeight` |
| Box width | — (`WidthFactor`, Fit/Align) | `Width` (wrap width, 0 = no wrap) |
| Real size | `GeometricExtents` | `ActualWidth`, `ActualHeight` |
| Justification | `HorizontalMode`, `VerticalMode`, `AlignmentPoint` | `Attachment` |
| Rotation | `Rotation` (rad) | `Rotation` (rad) |

## Rules

- Call `SetDatabaseDefaults(db)` on every new text object (current style, layer, color, linetype).
- Height must be `> 0`. For annotative text it is the **paper** height.
- Angles (`Rotation`, `Oblique`) in radians.

## DBText justification

For anything other than Left/Baseline set `HorizontalMode` / `VerticalMode`, set **`AlignmentPoint`**,
then call `t.AdjustAlignment(db)`; `Position` is then recalculated (it "jumps" — expected).
Horizontal: `TextLeft`, `TextCenter`, `TextRight`, `TextAlign`, `TextMid`, `TextFit`.
Vertical: `TextBase`, `TextBottom`, `TextVerticalMid`, `TextTop`.
Fit = squeeze between `Position` and `AlignmentPoint`, height kept; Align = same, height changes.

## MText contents codes

C# strings need doubled backslashes (`"\\P"`) or verbatim (`@"\P"`).

| Code | Effect |
| --- | --- |
| `\P` | new paragraph |
| `\H2x;` / `\H5;` | relative / absolute height |
| `\C1;` | ACI color |
| `\Farial\|b1\|i0;` | font, bold, italic |
| `\L…\l`, `\O…\o`, `\K…\k` | underline, overline, strike |
| `\S1/2;` | stacked fraction |
| `\W0.8;`, `\Q15;`, `\T2;` | width factor, oblique°, tracking |
| `{ … }` | scope of a format change |
| `%%d` `%%p` `%%c`, `\U+00B0` | °, ±, ⌀, Unicode |

Assigning plain text to `Contents` wipes inline formatting; replacing inside `Contents` will not find phrases
interrupted by codes. Unbalanced `{`/`}` breaks formatting.

Other MText props: `LineSpacingStyle`/`LineSpacingFactor`, `BackgroundFill` + `BackgroundFillColor` +
`BackgroundScaleFactor` (mask — useful for labels placed over zone hatching/lines).

## Size of the text field

- `MText.Width = 0` → natural width; `FitMTextWidthToContent` sets `Width = ActualWidth`.
- `ActualWidth` / `ActualHeight` can be stale inside the same transaction — read after commit.
- Proportional resize of text + box: `TransformBy(Matrix3d.Scaling(f, basePt))`.

## Text styles (`TextStyleTableRecord`)

- TrueType: `tsr.Font = new FontDescriptor("Arial", bold, italic, 0, 0)`; SHX: `tsr.FileName = "simplex.shx"`.
  `FontDescriptor` is in `Autodesk.AutoCAD.GraphicsInterface` — import it with an alias
  (`using FontDescriptor = Autodesk.AutoCAD.GraphicsInterface.FontDescriptor;`), because a full
  `using ...GraphicsInterface;` makes `Polyline` and others ambiguous.
- `TextSize = 0` → variable height (recommended). `XScale` = width factor, `ObliquingAngle` (rad).
- Make current: `db.Textstyle = styleId`.
- Font/orientation change affects all existing text of the style; height/width/oblique only new text. `ed.Regen()`.
- Assigning a style to MText resets inline fonts.
- Annotative style: `tsr.Annotative = AnnotativeStates.True` — text created with it becomes annotative.

## Annotative text (project use: labels with fixed sheet height)

Combine with the scales reference: set `ent.Annotative = AnnotativeStates.True` (or use an annotative style),
then add at least one scale context: `ent.AddContext(occ.GetContext(scaleName))`. `Height` = paper height
(e.g. 2.5 mm). See `TextHelper.CreateAnnotativeMText`.

## Bulk

- Select: `DxfCode.Start = "TEXT,MTEXT"` (+ `DxfCode.LayerName`).
- Apply: open each `ForWrite` in one transaction, skip `ErrorStatus.OnLockedLayer`.
- `ConvertTextToMText` copies content/height/rotation/style/layer/color, `Attachment = BottomLeft`.

## Errors

zero height · `\n` in DBText · alignment ignored (no `AlignmentPoint`/`AdjustAlignment`) · `eKeyNotFound`
(layer/style) · `eOnLockedLayer` · `ActualWidth == 0` before regen · missing font substitution ·
mirrored text (`MIRRTEXT = 0`) · annotative text size depends on current scale.
