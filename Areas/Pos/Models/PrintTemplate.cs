using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    // Visual layout config for a printable report. Stored as JSON under
    // ~/App_Data/PrintTemplates/{Name}.json. Coordinate units are in
    // hundredths of an inch (matches DevExpress XtraReport units), and
    // A4 portrait is 827 x 1170.
    public class PrintTemplate
    {
        // Bumped to 2 when the coordinate system switched from
        // "X measured from the page's right edge" (the old RightToLeftLayout=Yes
        // behaviour) to "X measured from the page's left edge". Templates
        // saved with an older version are auto-migrated by
        // PrintTemplateService.Load before they ever reach the designer
        // or the report.
        public const int CurrentVersion = 2;

        public string Name { get; set; }

        // Layout coordinate-system version. 0/1 = legacy mirrored (X from
        // right). 2 = LTR (X from left of page). Migration is performed on
        // load and persisted on the next save.
        public int TemplateVersion { get; set; }

        // Optional pre-printed scan rendered behind the canvas in the
        // designer. PrintBackground decides if it's also drawn into the
        // exported PDF.
        public string BackgroundFileName { get; set; }
        public bool PrintBackground { get; set; }

        // Page dimensions in template units (1/100 inch).
        public float PageWidth { get; set; } = 827F;
        public float PageHeight { get; set; } = 1170F;

        // Native pixel size of the background image. The designer JS
        // uses this to map between screen pixels and template units.
        public float ImageWidth { get; set; }
        public float ImageHeight { get; set; }

        // Per-printer offset applied to every field at print time.
        public float GlobalXShift { get; set; }
        public float GlobalYShift { get; set; }

        public List<PrintTemplateField> Fields { get; set; } = new List<PrintTemplateField>();
    }

    public class PrintTemplateField
    {
        public string FieldKey { get; set; }
        public string Label { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public string FontName { get; set; } = "Tahoma";
        public float FontSize { get; set; } = 10F;
        public bool Bold { get; set; }

        // "Left" | "Center" | "Right"
        public string Alignment { get; set; } = "Center";

        // "LTR" | "RTL"
        public string Direction { get; set; } = "RTL";

        public bool IsCellBased { get; set; }
        public int CellCount { get; set; }
        public float CellWidth { get; set; }
        public float CharacterSpacing { get; set; }

        // Visual order of characters across cells, independent of the
        // surrounding text direction:
        //   "LTR" = char[0] in the leftmost cell, char[N-1] in the right.
        //          For tokens, national IDs, phone numbers (digit/Latin
        //          content), use LTR.
        //   "RTL" = char[0] in the rightmost cell. Use only for Arabic
        //          cell content where reading order should match.
        // Default is LTR. The RightToLeft setting on the parent report
        // does NOT affect this - cell order is positional, not RTL text.
        public string CellDirection { get; set; } = "LTR";

        // Content source for the rendered field. Controls how the label
        // text is computed at print-time:
        //   "Data"       - lookup value from the customer dictionary
        //                  using FieldKey (the default - backwards
        //                  compatible with existing templates that have
        //                  no FieldType set).
        //   "CheckMark"  - render a fixed check glyph (✓), used as a
        //                  yes/tick marker on the form.
        //   "CrossMark"  - render a fixed cross glyph (✗), used as a
        //                  no/error marker on the form.
        //   "StaticText" - render the verbatim StaticContent below; the
        //                  designer lets the user type whatever caption
        //                  they want.
        public string FieldType { get; set; } = "Data";

        // Verbatim text printed when FieldType == "StaticText". Ignored
        // for every other FieldType.
        public string StaticContent { get; set; }
    }
}
