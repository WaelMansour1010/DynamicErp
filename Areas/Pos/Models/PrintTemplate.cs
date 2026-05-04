using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    // Visual layout config for a printable report. Stored as JSON under
    // ~/App_Data/PrintTemplates/{Name}.json. Coordinate units are in
    // hundredths of an inch (matches DevExpress XtraReport units), and
    // A4 portrait is 827 x 1170.
    public class PrintTemplate
    {
        public string Name { get; set; }

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
    }
}
