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
    // SatriahMain Cayshny\New frm\FrmCustCash.frm). Renders dynamic data
    // ON TOP of a pre-printed card body. Customer is loaded strictly by
    // TblCusCsh.Id through PosSqlRepository.GetKeshniCardCustomerById.
    public class KycCardReport : XtraReport
    {
        private readonly Font _smallFont = new Font("Tahoma", 8F, FontStyle.Regular);
        private readonly Font _smallBold = new Font("Tahoma", 8F, FontStyle.Bold);
        private readonly Font _normalBold = new Font("Tahoma", 10F, FontStyle.Bold);
        private readonly Font _cellFont = new Font("Tahoma", 11F, FontStyle.Bold);

        private const float A4WidthHundredths = 827F;

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
            Margins = new Margins(40, 40, 40, 40);

            var detail = new DetailBand();
            detail.HeightF = 1080F;
            Bands.Add(detail);

            float width = A4WidthHundredths - Margins.Left - Margins.Right;
            BuildBody(detail, customer, issuedAt, width);
        }

        private void BuildBody(DetailBand band, PosCustomerLookupDto customer, DateTime issuedAt, float width)
        {
            string visa = OnlyDigits(FirstNonEmpty(customer.VisaNumber, customer.CardNo, customer.CardId));
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

            // Visa number block (top)
            DrawCellDigits(band, visa, 8, width * 0.30F, 200F, 32F, 24F);

            // Names + address (centered text rows)
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, 280F, width, 22F),
                Text = arabicName,
                Font = _normalBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, 312F, width, 22F),
                Text = englishName,
                Font = _normalBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.No
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, 360F, width, 22F),
                Text = address,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.No,
                WordWrap = false
            });

            // Right column: nationality + birth date
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width - 110F, 455F, 100F, 20F),
                Text = nationality,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width - 130F, 478F, 120F, 20F),
                Text = birthDate,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.No
            });

            // National ID cells (14 digits, centered)
            DrawCellDigits(band, nationalId, 14, width * 0.20F, 510F, 28F, 22F);

            // Card start (right) / source (middle) / card end (left)
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width - 130F, 545F, 120F, 20F),
                Text = cardStart,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.No
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width / 2F - 75F, 545F, 150F, 20F),
                Text = source,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(10F, 545F, 120F, 20F),
                Text = cardEnd,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleLeft,
                RightToLeft = RightToLeft.No
            });

            // Phone cells (11 digits, centered)
            DrawCellDigits(band, phone, 11, width * 0.25F, 605F, 30F, 22F);

            // Visa number block (second instance, lower on page)
            DrawCellDigits(band, visa, 8, width * 0.30F, 820F, 32F, 24F);

            // Footer date (Arabic long format)
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, 1040F, width, 20F),
                Text = footerDate,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
        }

        private void DrawCellDigits(DetailBand band, string digits, int cellCount,
            float startX, float y, float cellWidth, float cellHeight)
        {
            string padded = (digits ?? string.Empty).PadLeft(cellCount, ' ');
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
            // Match VB6: nationality 1 (or default) = Egyptian.
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
                return value.ToString("dddd, d MMMM, yyyy", ar);
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
