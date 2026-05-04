using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using MyERP.Areas.Pos.Models;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;

namespace MyERP.Areas.Pos.Reports
{
    // Layout target: Areas/Pos/Doc/repCashCustomer.pdf - the Crystal report
    // opened by VB6 FrmCustCash.BtnPrint_Click -> print_report2 (
    // SatriahMain Cayshny\New frm\FrmCustCash.frm). Prints dynamic data
    // ON TOP of a pre-printed card application form. Customer is loaded
    // strictly by TblCusCsh.Id.
    //
    // Positions below are calibrated against repCashCustomer.pdf assuming
    // A4 portrait at the standard print scaling. They may need 5-15 unit
    // tweaks per cell row when running against a specific physical
    // printer/paper batch (printers and pre-printed runs vary). To
    // recalibrate a row, change only its y coordinate or the cell start
    // X. Cell widths are derived once and reused.
    public class KycCardReport : XtraReport
    {
        private readonly Font _smallBold = new Font("Tahoma", 9F, FontStyle.Bold);
        private readonly Font _normalBold = new Font("Tahoma", 10F, FontStyle.Bold);
        private readonly Font _cellFont = new Font("Tahoma", 11F, FontStyle.Bold);

        private const float A4Width = 827F;
        private const float A4Height = 1170F;

        public KycCardReport(PosCustomerLookupDto customer, DateTime issuedAt)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.Yes;
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            Landscape = false;
            // Margins zeroed: positions below are absolute and assume the
            // page origin is the physical paper corner.
            Margins = new Margins(0, 0, 0, 0);

            var detail = new DetailBand();
            detail.HeightF = A4Height;
            Bands.Add(detail);

            BuildBody(detail, customer, issuedAt);
        }

        private void BuildBody(DetailBand band, PosCustomerLookupDto customer, DateTime issuedAt)
        {
            string token = AlphaNumeric(FirstNonEmpty(customer.CardNo, customer.CardId, customer.VisaNumber));
            string nationalId = OnlyDigits(customer.Tet_NumPoket);
            string phone = OnlyDigits(FirstNonEmpty(customer.Phone2, customer.Phone, customer.Tel));
            string birthDate = FormatDate(customer.BirthDate);
            string cardStart = FormatDate(customer.CardDate);
            string cardEnd = FormatDate(customer.CardEndDate);
            string arabicName = FirstNonEmpty(JoinArabic(customer), customer.CustomerName, customer.Name);
            string englishName = JoinEnglish(customer);
            string address = FirstNonEmpty(customer.Address, customer.MailAdress);
            string nationality = ResolveNationality(customer.Nationality);
            string source = FirstNonEmpty(customer.CardSource, customer.BranchName);
            string footerDate = FormatArabicLongDate(issuedAt);

            // ---- Top "رقم الـ Token" cells (12 alphanumeric cells) ----
            DrawCells(band, token, 12, 75F, 250F, 55F, 26F);

            // ---- Arabic / English / Address rows (centered text) ----
            DrawCenteredText(band, arabicName, 295F, _normalBold, RightToLeft.Yes);
            DrawCenteredText(band, englishName, 332F, _normalBold, RightToLeft.No);
            DrawCenteredText(band, address, 378F, _smallBold, RightToLeft.No);

            // ---- Right column: nationality + birth date ----
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(580F, 470F, 200F, 20F),
                Text = nationality,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(580F, 505F, 200F, 20F),
                Text = birthDate,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.No
            });

            // ---- National ID cells (14 digits) ----
            DrawCells(band, nationalId, 14, 95F, 545F, 44F, 24F);

            // ---- Issue date (right) | Source (middle) | Expiry date (left) ----
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(610F, 593F, 130F, 20F),
                Text = cardStart,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.No
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(290F, 593F, 290F, 20F),
                Text = source,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(45F, 593F, 130F, 20F),
                Text = cardEnd,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleLeft,
                RightToLeft = RightToLeft.No
            });

            // ---- Mobile phone cells (11 digits) ----
            DrawCells(band, phone, 11, 130F, 663F, 50F, 24F);

            // ---- Bottom "رقم حساب العميل (Token NO.)" cells (12 alphanumeric) ----
            DrawCells(band, token, 12, 75F, 905F, 55F, 26F);

            // ---- "اسم العميل" signature line (الإقرار section) ----
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(420F, 1075F, 280F, 20F),
                Text = arabicName,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });

            // ---- Footer date (Arabic long form) ----
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0F, 1140F, A4Width, 20F),
                Text = footerDate,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
        }

        private void DrawCenteredText(DetailBand band, string text, float y, Font font, RightToLeft rtl)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0F, y, A4Width, 22F),
                Text = text,
                Font = font,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = rtl,
                WordWrap = false
            });
        }

        private void DrawCells(DetailBand band, string value, int cellCount,
            float startX, float y, float cellWidth, float cellHeight)
        {
            string padded = (value ?? string.Empty).PadLeft(cellCount, ' ');
            if (padded.Length > cellCount)
            {
                padded = padded.Substring(padded.Length - cellCount);
            }

            for (int i = 0; i < cellCount; i++)
            {
                char ch = padded[i];
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(startX + i * cellWidth, y, cellWidth, cellHeight),
                    Text = ch == ' ' ? string.Empty : ch.ToString(CultureInfo.InvariantCulture),
                    Font = _cellFont,
                    TextAlignment = TextAlignment.MiddleCenter
                });
            }
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
