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
    // Coordinate system: hundredths of an inch. A4 portrait = 827 x 1170.
    // Margins are zeroed; positions are absolute against the physical
    // paper corner so they can be tweaked row-by-row.
    //
    // CALIBRATION:
    //   - GLOBAL_Y_SHIFT shifts every printed value vertically. Use it
    //     to match the printer's physical paper feed.
    //   - GLOBAL_X_SHIFT shifts every printed value horizontally.
    //   - Each row has its own constant (TokenCellsY, NationalIdY, etc).
    //     If only one row is off, change that constant.
    //   - Cell widths: TokenCellWidth, NationalIdCellWidth, etc.
    //     Adjust if a digit row drifts across multiple cells.
    public class KycCardReport : XtraReport
    {
        // ----- Tweakable layout constants -----
        private const float GLOBAL_Y_SHIFT = 0F;
        private const float GLOBAL_X_SHIFT = 0F;

        // Top "رقم الـ Token" row - 12 alphanumeric cells.
        private const float TokenCellsX = 75F;
        private const float TokenCellsY = 205F;
        private const float TokenCellWidth = 55F;
        private const float TokenCellHeight = 24F;
        private const int TokenCellCount = 12;

        // Centered Arabic name row.
        private const float ArabicNameY = 265F;
        // Centered English name row.
        private const float EnglishNameY = 310F;
        // Centered address row.
        private const float AddressY = 360F;

        // Right-aligned single fields under "الجنسية" / "تاريخ الميلاد".
        private const float RightFieldX = 580F;
        private const float RightFieldWidth = 200F;
        private const float NationalityY = 450F;
        private const float BirthDateY = 485F;

        // National ID row - 14 cells.
        private const float NationalIdX = 95F;
        private const float NationalIdY = 525F;
        private const float NationalIdCellWidth = 44F;
        private const float NationalIdCellHeight = 22F;
        private const int NationalIdCellCount = 14;

        // Issue date / source / expiry date row.
        private const float IssueRowY = 580F;
        // Issue date (right side).
        private const float IssueDateX = 610F;
        private const float IssueDateWidth = 130F;
        // Issuing entity (middle).
        private const float SourceX = 290F;
        private const float SourceWidth = 290F;
        // Expiry date (left side).
        private const float ExpiryDateX = 45F;
        private const float ExpiryDateWidth = 130F;

        // Mobile phone row - 11 cells.
        private const float PhoneX = 130F;
        private const float PhoneY = 660F;
        private const float PhoneCellWidth = 50F;
        private const float PhoneCellHeight = 22F;
        private const int PhoneCellCount = 11;

        // Bottom "رقم حساب العميل (Token NO.)" row - 12 cells.
        private const float TokenNoX = 75F;
        private const float TokenNoY = 925F;

        // "اسم العميل" signature line in الإقرار section.
        private const float SignatureNameX = 420F;
        private const float SignatureNameY = 1110F;
        private const float SignatureNameWidth = 280F;

        // Footer "Arabic long date".
        private const float FooterDateY = 1150F;

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

            DrawCells(band, token, TokenCellCount,
                TokenCellsX, TokenCellsY, TokenCellWidth, TokenCellHeight);

            DrawCenteredText(band, arabicName, ArabicNameY, _normalBold, RightToLeft.Yes);
            DrawCenteredText(band, englishName, EnglishNameY, _normalBold, RightToLeft.No);
            DrawCenteredText(band, address, AddressY, _smallBold, RightToLeft.No);

            DrawRightField(band, nationality, NationalityY, RightToLeft.Yes);
            DrawRightField(band, birthDate, BirthDateY, RightToLeft.No);

            DrawCells(band, nationalId, NationalIdCellCount,
                NationalIdX, NationalIdY, NationalIdCellWidth, NationalIdCellHeight);

            DrawText(band, cardStart, IssueDateX, IssueRowY, IssueDateWidth, 20F,
                TextAlignment.MiddleRight, RightToLeft.No);
            DrawText(band, source, SourceX, IssueRowY, SourceWidth, 20F,
                TextAlignment.MiddleCenter, RightToLeft.Yes);
            DrawText(band, cardEnd, ExpiryDateX, IssueRowY, ExpiryDateWidth, 20F,
                TextAlignment.MiddleLeft, RightToLeft.No);

            DrawCells(band, phone, PhoneCellCount,
                PhoneX, PhoneY, PhoneCellWidth, PhoneCellHeight);

            DrawCells(band, token, TokenCellCount,
                TokenNoX, TokenNoY, TokenCellWidth, TokenCellHeight);

            DrawText(band, arabicName, SignatureNameX, SignatureNameY,
                SignatureNameWidth, 20F, TextAlignment.MiddleCenter, RightToLeft.Yes);

            DrawText(band, footerDate, 0F, FooterDateY, A4Width, 20F,
                TextAlignment.MiddleCenter, RightToLeft.Yes);
        }

        private void DrawText(DetailBand band, string text, float x, float y,
            float width, float height, TextAlignment alignment, RightToLeft rtl)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x + GLOBAL_X_SHIFT, y + GLOBAL_Y_SHIFT, width, height),
                Text = text,
                Font = _smallBold,
                TextAlignment = alignment,
                RightToLeft = rtl,
                WordWrap = false
            });
        }

        private void DrawCenteredText(DetailBand band, string text, float y,
            Font font, RightToLeft rtl)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(GLOBAL_X_SHIFT, y + GLOBAL_Y_SHIFT, A4Width, 22F),
                Text = text,
                Font = font,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = rtl,
                WordWrap = false
            });
        }

        private void DrawRightField(DetailBand band, string text, float y, RightToLeft rtl)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(RightFieldX + GLOBAL_X_SHIFT, y + GLOBAL_Y_SHIFT,
                    RightFieldWidth, 20F),
                Text = text,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
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
                    BoundsF = new RectangleF(
                        startX + i * cellWidth + GLOBAL_X_SHIFT,
                        y + GLOBAL_Y_SHIFT,
                        cellWidth, cellHeight),
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
