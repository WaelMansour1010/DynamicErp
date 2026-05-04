using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Web.Hosting;

namespace MyERP.Areas.Pos.Reports
{
    // Layout target: Areas/Pos/Doc/repCashCustomer4.pdf - the Crystal
    // acknowledgment form referenced by the legacy Cayshny screen.
    // Customer is loaded strictly by TblCusCsh.Id through
    // PosSqlRepository.GetKeshniCardCustomerById.
    public class KycCardAcknowledgmentReport : XtraReport
    {
        private readonly Font _bodyFont = new Font("Tahoma", 11F, FontStyle.Regular);
        private readonly Font _bodyBold = new Font("Tahoma", 11F, FontStyle.Bold);
        private readonly Font _titleFont = new Font("Tahoma", 14F, FontStyle.Bold | FontStyle.Underline);
        private readonly Font _tokenLabelFont = new Font("Tahoma", 12F, FontStyle.Bold);
        private readonly Font _dynamicFont = new Font("Tahoma", 11F, FontStyle.Bold);

        private readonly Color _underlineColor = Color.Black;

        public KycCardAcknowledgmentReport(PosCustomerLookupDto customer, DateTime issuedAt)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.Yes;
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            Landscape = false;
            Margins = new Margins(70, 70, 60, 60);

            var detail = new DetailBand();
            Bands.Add(detail);

            const float a4WidthHundredthInch = 827F;
            float contentWidth = a4WidthHundredthInch - Margins.Left - Margins.Right;
            BuildBody(detail, customer, issuedAt, contentWidth);
        }

        private void BuildBody(DetailBand band, PosCustomerLookupDto customer, DateTime issuedAt, float width)
        {
            string customerName = FirstNonEmpty(customer.CustomerName, customer.Name, customer.ArabicName0);
            string tokenValue = FirstNonEmpty(customer.CardNo, customer.CardId);
            string date = issuedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            string time = FormatArabicTime(issuedAt);

            DrawHeaderLogos(band, 0F, width);

            const float titleY = 130F;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, titleY, width, 28),
                Text = "إقرار استلام بطاقة بنك مصر - Easy Cash ميزة المدفوعة مقدما",
                Font = _titleFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });

            const float tokenY = 195F;
            const float tokenLineHeight = 22F;
            float tokenLabelWidth = 110F;
            float tokenValueWidth = 240F;
            float tokenBlockWidth = tokenLabelWidth + tokenValueWidth + 8F;
            float tokenStartX = (width - tokenBlockWidth) / 2F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(tokenStartX + tokenValueWidth + 8F, tokenY, tokenLabelWidth, tokenLineHeight),
                Text = "رقم Token :",
                Font = _tokenLabelFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(tokenStartX, tokenY, tokenValueWidth, tokenLineHeight),
                Text = tokenValue,
                Font = _tokenLabelFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderColor = _underlineColor,
                BorderWidth = 0.6F,
                WordWrap = false
            });

            float lineY = 270F;
            const float lineHeight = 30F;
            const float labelHeight = 22F;

            // Line 1 (RTL reading order):
            //   [أقر أنا] [customerName underlined] [الموقع أدناه بأني إستلمت بطاقة بنك مصر - Easy Cash]
            float rightPrefixWidth = 60F;
            float leftSuffixWidth = width * 0.55F;
            float namePartWidth = width - rightPrefixWidth - leftSuffixWidth - 8F;
            float namePartX = leftSuffixWidth + 4F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width - rightPrefixWidth, lineY, rightPrefixWidth, labelHeight),
                Text = "أقر أنا",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(namePartX, lineY, namePartWidth, labelHeight),
                Text = customerName,
                Font = _dynamicFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                Borders = BorderSide.Bottom,
                BorderColor = _underlineColor,
                BorderWidth = 0.6F,
                WordWrap = false
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, leftSuffixWidth, labelHeight),
                Text = "الموقع أدناه بأني إستلمت بطاقة بنك مصر - Easy Cash",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            lineY += lineHeight;

            // Line 2 (RTL):
            //   [ميزة المدفوعة مقدما المذكورة أعلاه بالرقم المرجعي لها] [tokenValue underlined]
            float line2RightWidth = width * 0.55F;
            float line2TokenWidth = width - line2RightWidth - 4F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width - line2RightWidth, lineY, line2RightWidth, labelHeight),
                Text = "ميزة المدفوعة مقدما المذكورة أعلاه بالرقم المرجعي لها",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, line2TokenWidth, labelHeight),
                Text = tokenValue,
                Font = _dynamicFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderColor = _underlineColor,
                BorderWidth = 0.6F,
                WordWrap = false
            });
            lineY += lineHeight;

            // Line 3 (RTL):
            //   [في يوم وتاريخ] [date underlined] [الساعة] [time underlined]
            //   [من السادة شركة أيزي كاش للدفع الإلكتروني]
            float dateLabelW = 95F;
            float dateValueW = 110F;
            float timeLabelW = 55F;
            float timeValueW = 90F;
            float footerSuffixW = width - (dateLabelW + dateValueW + timeLabelW + timeValueW) - 4F;

            float xRight = width;
            xRight -= dateLabelW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(xRight, lineY, dateLabelW, labelHeight),
                Text = "في يوم وتاريخ",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });

            xRight -= dateValueW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(xRight, lineY, dateValueW, labelHeight),
                Text = date,
                Font = _dynamicFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderColor = _underlineColor,
                BorderWidth = 0.6F,
                WordWrap = false
            });

            xRight -= timeLabelW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(xRight, lineY, timeLabelW, labelHeight),
                Text = "الساعة",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });

            xRight -= timeValueW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(xRight, lineY, timeValueW, labelHeight),
                Text = time,
                Font = _dynamicFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderColor = _underlineColor,
                BorderWidth = 0.6F,
                WordWrap = false
            });

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, footerSuffixW, labelHeight),
                Text = "من السادة شركة أيزي كاش للدفع الإلكتروني",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, labelHeight),
                Text = "المقر بما فيه ,,",
                Font = _bodyBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            lineY += lineHeight + 12F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, labelHeight),
                Text = "توقيع العميل بصحة بياناته المذكورة أعلاه واستلام البطاقة:",
                Font = _bodyBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = new PaddingInfo(0, 4, 0, 0)
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.05F, lineY, width * 0.55F, labelHeight),
                Text = "إسم العميل: ..........................................",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.05F, lineY, width * 0.55F, labelHeight),
                Text = "التوقــــــيع: ..........................................",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.HeightF = lineY + 80F;
        }

        private void DrawHeaderLogos(DetailBand band, float y, float width)
        {
            const float logoBoxWidth = 140F;
            const float logoBoxHeight = 80F;

            Image misrLogo = LoadImage(new[]
            {
                "~/Areas/Pos/Doc/BanqueMisr.png",
                "~/Areas/Pos/Doc/banque_misr.png",
                "~/Areas/Pos/Doc/banque-misr.png",
                "~/assets/images/banque-misr.png"
            });

            Image easyLogo = LoadImage(new[]
            {
                "~/Areas/Pos/Doc/EasyCashLogo.png",
                "~/Areas/Pos/Doc/easycash-logo.png",
                "~/Areas/Pos/Content/easycash-logo.png",
                "~/assets/images/easycash-logo.png",
                "~/assets/images/logo.PNG"
            });

            if (misrLogo != null)
            {
                band.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF(0, y, logoBoxWidth, logoBoxHeight),
                    Image = misrLogo,
                    Sizing = ImageSizeMode.ZoomImage
                });
            }
            else
            {
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0, y + 25F, logoBoxWidth, 30F),
                    Text = "بنك مصر",
                    Font = new Font("Tahoma", 14F, FontStyle.Bold),
                    TextAlignment = TextAlignment.MiddleCenter,
                    RightToLeft = RightToLeft.Yes
                });
            }

            if (easyLogo != null)
            {
                band.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF(width - logoBoxWidth, y, logoBoxWidth, logoBoxHeight),
                    Image = easyLogo,
                    Sizing = ImageSizeMode.ZoomImage
                });
            }
            else
            {
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(width - logoBoxWidth, y + 25F, logoBoxWidth, 30F),
                    Text = "easycash",
                    Font = new Font("Tahoma", 14F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(54, 133, 72),
                    TextAlignment = TextAlignment.MiddleCenter
                });
            }
        }

        private static Image LoadImage(IEnumerable<string> virtualPaths)
        {
            try
            {
                if (!HostingEnvironment.IsHosted)
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            foreach (var virt in virtualPaths)
            {
                string path;
                try
                {
                    path = HostingEnvironment.MapPath(virt);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    return Image.FromStream(new MemoryStream(bytes));
                }
                catch
                {
                    // try next candidate
                }
            }

            return null;
        }

        private static string FormatArabicTime(DateTime value)
        {
            try
            {
                var ar = CultureInfo.GetCultureInfo("ar-EG");
                return value.ToString("tt hh:mm:ss", ar);
            }
            catch (CultureNotFoundException)
            {
                return value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
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
