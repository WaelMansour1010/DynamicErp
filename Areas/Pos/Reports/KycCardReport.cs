using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MyERP.Areas.Pos.Reports
{
    // Layout consumer for repCashCustomer.pdf (the pre-printed card form
    // VB6 FrmCustCash.BtnPrint_Click -> print_report2 prints over).
    //
    // Field positions are now driven by a PrintTemplate JSON config
    // managed visually through /Pos/PrintTemplate. If no config is
    // present, BuildDefaultTemplate() returns the calibrated constants
    // that used to live inline in this class - so existing deployments
    // keep working unchanged.
    //
    // Coordinate units: hundredths of an inch. A4 portrait = 827 x 1170.
    public class KycCardReport : XtraReport
    {
        private const float A4Width = 827F;
        private const float A4Height = 1170F;

        private readonly PrintTemplate _template;
        private readonly PosCustomerLookupDto _customer;
        private readonly DateTime _issuedAt;
        private readonly Dictionary<string, string> _values;

        // Backwards-compatible constructor: callers that don't supply a
        // template fall back to the JSON file (if any) and finally to the
        // default constants via BuildDefaultTemplate().
        public KycCardReport(PosCustomerLookupDto customer, DateTime issuedAt)
            : this(customer, issuedAt, null)
        {
        }

        public KycCardReport(PosCustomerLookupDto customer, DateTime issuedAt,
            PrintTemplate template)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }

            _customer = customer;
            _issuedAt = issuedAt;
            _template = template ?? LoadStoredTemplate("KycCard") ?? BuildDefaultTemplate();
            _values = BuildValues(customer, issuedAt);

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.Yes;
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            Landscape = false;
            Margins = new Margins(0, 0, 0, 0);

            var detail = new DetailBand();
            detail.HeightF = _template.PageHeight > 0 ? _template.PageHeight : A4Height;
            Bands.Add(detail);

            BuildBody(detail);
        }

        // Used by the designer's "New" button when no JSON file exists
        // yet, and as the print-time fallback if /App_Data is empty.
        public static PrintTemplate BuildDefaultTemplate()
        {
            return new PrintTemplate
            {
                Name = "KycCard",
                PrintBackground = false,
                PageWidth = A4Width,
                PageHeight = A4Height,
                ImageWidth = A4Width,
                ImageHeight = A4Height,
                GlobalXShift = 0F,
                GlobalYShift = 0F,
                Fields = new List<PrintTemplateField>
                {
                    Cells("Token", "رقم Token (فوق)", 75F, 205F, 55F, 24F, 12),
                    Text("ArabicName", "الاسم بالعربي", 0F, 265F, A4Width, 22F,
                        true, "Center", "RTL", 10F),
                    Text("EnglishName", "الاسم بالإنجليزي", 0F, 310F, A4Width, 22F,
                        true, "Center", "LTR", 10F),
                    Text("Address", "العنوان", 0F, 360F, A4Width, 22F,
                        true, "Center", "LTR", 9F),
                    Text("Nationality", "الجنسية", 580F, 450F, 200F, 20F,
                        true, "Right", "RTL", 9F),
                    Text("BirthDate", "تاريخ الميلاد", 580F, 485F, 200F, 20F,
                        true, "Right", "LTR", 9F),
                    Cells("NationalId", "الرقم القومي", 95F, 525F, 44F, 22F, 14),
                    Text("IssueDate", "تاريخ الإصدار", 610F, 580F, 130F, 20F,
                        true, "Right", "LTR", 9F),
                    Text("Source", "جهة الإصدار", 290F, 580F, 290F, 20F,
                        true, "Center", "RTL", 9F),
                    Text("ExpiryDate", "تاريخ الانتهاء", 45F, 580F, 130F, 20F,
                        true, "Left", "LTR", 9F),
                    Cells("Phone", "رقم المحمول", 130F, 660F, 50F, 22F, 11),
                    Cells("TokenNo", "Token NO. (تحت)", 75F, 925F, 55F, 24F, 12),
                    Text("SignatureName", "اسم العميل (توقيع)", 420F, 1110F, 280F, 20F,
                        true, "Center", "RTL", 9F),
                    Text("FooterDate", "التاريخ", 0F, 1150F, A4Width, 20F,
                        true, "Center", "RTL", 9F)
                }
            };
        }

        private static PrintTemplateField Cells(string key, string label,
            float x, float y, float cellWidth, float cellHeight, int count)
        {
            return new PrintTemplateField
            {
                FieldKey = key,
                Label = label,
                X = x,
                Y = y,
                Width = cellWidth * count,
                Height = cellHeight,
                FontName = "Tahoma",
                FontSize = 11F,
                Bold = true,
                Alignment = "Center",
                Direction = "LTR",
                IsCellBased = true,
                CellCount = count,
                CellWidth = cellWidth,
                CharacterSpacing = 0F
            };
        }

        private static PrintTemplateField Text(string key, string label,
            float x, float y, float w, float h, bool bold, string alignment,
            string direction, float fontSize)
        {
            return new PrintTemplateField
            {
                FieldKey = key,
                Label = label,
                X = x,
                Y = y,
                Width = w,
                Height = h,
                FontName = "Tahoma",
                FontSize = fontSize,
                Bold = bold,
                Alignment = alignment,
                Direction = direction,
                IsCellBased = false,
                CellCount = 0,
                CellWidth = 0F,
                CharacterSpacing = 0F
            };
        }

        private static PrintTemplate LoadStoredTemplate(string name)
        {
            try
            {
                return new PrintTemplateService().Load(name);
            }
            catch
            {
                return null;
            }
        }

        private void BuildBody(DetailBand band)
        {
            if (_template.PrintBackground && !string.IsNullOrWhiteSpace(_template.BackgroundFileName))
            {
                var bg = LoadBackgroundImage(_template.BackgroundFileName);
                if (bg != null)
                {
                    band.Controls.Add(new XRPictureBox
                    {
                        BoundsF = new RectangleF(0F, 0F,
                            _template.PageWidth > 0 ? _template.PageWidth : A4Width,
                            _template.PageHeight > 0 ? _template.PageHeight : A4Height),
                        Image = bg,
                        Sizing = ImageSizeMode.StretchImage
                    });
                }
            }

            foreach (var field in _template.Fields)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.FieldKey))
                {
                    continue;
                }

                string value;
                _values.TryGetValue(field.FieldKey, out value);
                value = value ?? string.Empty;

                if (field.IsCellBased && field.CellCount > 0 && field.CellWidth > 0)
                {
                    DrawCells(band, value, field);
                }
                else
                {
                    DrawText(band, value, field);
                }
            }
        }

        private void DrawText(DetailBand band, string value, PrintTemplateField field)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(
                    field.X + _template.GlobalXShift,
                    field.Y + _template.GlobalYShift,
                    field.Width,
                    field.Height),
                Text = value,
                Font = ResolveFont(field),
                TextAlignment = ResolveAlignment(field.Alignment),
                RightToLeft = ResolveRtl(field.Direction),
                WordWrap = false
            });
        }

        private void DrawCells(DetailBand band, string value, PrintTemplateField field)
        {
            string content = value ?? string.Empty;
            string padded = content.PadLeft(field.CellCount, ' ');
            if (padded.Length > field.CellCount)
            {
                padded = padded.Substring(padded.Length - field.CellCount);
            }

            float spacing = field.CharacterSpacing;
            for (int i = 0; i < field.CellCount; i++)
            {
                char ch = padded[i];
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(
                        field.X + i * (field.CellWidth + spacing) + _template.GlobalXShift,
                        field.Y + _template.GlobalYShift,
                        field.CellWidth,
                        field.Height),
                    Text = ch == ' ' ? string.Empty : ch.ToString(CultureInfo.InvariantCulture),
                    Font = ResolveFont(field),
                    TextAlignment = TextAlignment.MiddleCenter
                });
            }
        }

        private Font ResolveFont(PrintTemplateField field)
        {
            var name = string.IsNullOrWhiteSpace(field.FontName) ? "Tahoma" : field.FontName;
            var size = field.FontSize > 0 ? field.FontSize : 10F;
            return new Font(name, size, field.Bold ? FontStyle.Bold : FontStyle.Regular);
        }

        private static TextAlignment ResolveAlignment(string alignment)
        {
            switch ((alignment ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "left": return TextAlignment.MiddleLeft;
                case "right": return TextAlignment.MiddleRight;
                default: return TextAlignment.MiddleCenter;
            }
        }

        private static RightToLeft ResolveRtl(string direction)
        {
            return string.Equals(direction, "RTL", StringComparison.OrdinalIgnoreCase)
                ? RightToLeft.Yes
                : RightToLeft.No;
        }

        private static Image LoadBackgroundImage(string fileName)
        {
            try
            {
                var service = new PrintTemplateService();
                var bytes = service.LoadBackground(fileName);
                if (bytes == null)
                {
                    return null;
                }
                using (var ms = new MemoryStream(bytes))
                {
                    return Image.FromStream(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, string> BuildValues(
            PosCustomerLookupDto customer, DateTime issuedAt)
        {
            string token = AlphaNumeric(FirstNonEmpty(
                customer.CardNo, customer.CardId, customer.VisaNumber));
            string nationalId = OnlyDigits(customer.Tet_NumPoket);
            string phone = OnlyDigits(FirstNonEmpty(customer.Phone2, customer.Phone, customer.Tel));

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Token", token },
                { "TokenNo", token },
                { "ArabicName", FirstNonEmpty(JoinArabic(customer), customer.CustomerName, customer.Name) },
                { "EnglishName", JoinEnglish(customer) },
                { "Address", FirstNonEmpty(customer.Address, customer.MailAdress) },
                { "Nationality", ResolveNationality(customer.Nationality) },
                { "BirthDate", FormatDate(customer.BirthDate) },
                { "NationalId", nationalId },
                { "IssueDate", FormatDate(customer.CardDate) },
                { "Source", FirstNonEmpty(customer.CardSource, customer.BranchName) },
                { "ExpiryDate", FormatDate(customer.CardEndDate) },
                { "Phone", phone },
                { "SignatureName", FirstNonEmpty(JoinArabic(customer), customer.CustomerName, customer.Name) },
                { "FooterDate", FormatArabicLongDate(issuedAt) }
            };
        }

        private static string OnlyDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '0' && ch <= '9')
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static string AlphaNumeric(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private static string JoinArabic(PosCustomerLookupDto customer)
        {
            return string.Join(" ", new[]
            {
                customer.ArabicName0,
                customer.ArabicName1,
                customer.ArabicName2,
                customer.ArabicName3
            }.Select(NullToEmpty).Where(s => s.Length > 0));
        }

        private static string JoinEnglish(PosCustomerLookupDto customer)
        {
            return string.Join(" ", new[]
            {
                customer.EnglishName0,
                customer.EnglishName1,
                customer.EnglishName2,
                customer.EnglishName3
            }.Select(NullToEmpty).Where(s => s.Length > 0));
        }

        private static string NullToEmpty(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim();
        }

        private static string ResolveNationality(int? code)
        {
            if (!code.HasValue)
            {
                return string.Empty;
            }
            switch (code.Value)
            {
                case 0:
                case 1: return "مصري";
                default: return code.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue
                ? value.Value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static string FormatArabicLongDate(DateTime value)
        {
            try
            {
                var ar = CultureInfo.GetCultureInfo("ar-EG");
                return value.ToString("dddd, d MMMM، yyyy", ar);
            }
            catch (CultureNotFoundException)
            {
                return value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            }
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}
