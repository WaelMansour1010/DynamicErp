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
    public class KycCardAcknowledgmentReport : XtraReport
    {
        private readonly Font _titleFont = new Font("Tahoma", 16F, FontStyle.Bold | FontStyle.Underline);
        private readonly Font _lineFont = new Font("Tahoma", 11F, FontStyle.Regular);
        private readonly Font _lineBoldFont = new Font("Tahoma", 11F, FontStyle.Bold);
        private readonly Font _tokenFont = new Font("Tahoma", 12F, FontStyle.Bold);

        public KycCardAcknowledgmentReport(PosCustomerLookupDto customer, DateTime issuedAt)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.No;
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            Landscape = false;
            Margins = new Margins(35, 35, 25, 25);

            var detail = new DetailBand();
            Bands.Add(detail);

            const float a4WidthHundredthInch = 827F;
            float width = a4WidthHundredthInch - Margins.Left - Margins.Right;
            BuildBody(detail, customer, issuedAt, width);
        }

        // Right-hand padding applied to every body line so all right-aligned
        // text shares the exact same right margin. Title and token block
        // intentionally do NOT use this - they remain centered.
        private static readonly PaddingInfo BodyPadding = new PaddingInfo(2, 2, 0, 0);

        private void BuildBody(DetailBand band, PosCustomerLookupDto customer, DateTime issuedAt, float width)
        {
            string customerName = FirstNonEmpty(customer.CustomerName, customer.Name, customer.ArabicName0, "........................");
            string tokenValue = FirstNonEmpty(customer.CardNo, customer.CardId, "........................");
            string date = issuedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            string time = FormatArabicTime(issuedAt);

            DrawHeaderLogos(band, width);

            float y = 78F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0F, y, width, 36F),
                Text = "إقرار استلام بطاقة بنك مصر - Easy Cash ميزة المدفوعة مقدمًا",
                Font = _titleFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                WordWrap = false
            });
            y += 56F;

            const float tokenLabelWidth = 145F;
            const float tokenValueWidth = 300F;
            float tokenBlockWidth = tokenLabelWidth + tokenValueWidth + 8F;
            float tokenX = (width - tokenBlockWidth) / 2F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(tokenX + tokenValueWidth + 8F, y, tokenLabelWidth, 28F),
                Text = "رقم Token :",
                Font = _tokenFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                WordWrap = false
            });

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(tokenX, y, tokenValueWidth, 28F),
                Text = tokenValue,
                Font = _tokenFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderWidth = 0.8F,
                WordWrap = false
            });
            y += 52F;

            const float h = 30F;
            const float pad = 4F;
            // Shift the whole declaration body to the right to match legacy paper alignment.
            const float bodyX = 120F;
            float bodyW = width - bodyX;
            float bodyRight = bodyX + bodyW;

            // سطر 1: [أقر أنا] [اسم العميل تحت خط] [الموقع أدناه بأنني استلمت بطاقة بنك مصر - Easy Cash]
            float rightPrefixW = 75F;
            float leftSuffixW = 360F;
            float nameW = bodyW - rightPrefixW - leftSuffixW - (pad * 2F);
            float nameX = bodyX + leftSuffixW + pad;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(bodyRight - rightPrefixW, y, rightPrefixW, h),
                Text = "أقر أنا",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(nameX, y, nameW, h),
                Text = customerName,
                Font = _lineBoldFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderWidth = 0.7F,
                RightToLeft = RightToLeft.Yes,
                WordWrap = false
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(bodyX, y, leftSuffixW, h),
                Text = "الموقع أدناه بأنني استلمت بطاقة بنك مصر - Easy Cash",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            y += h + 6F;

            // سطر 2: [ميزة ... بالرقم المرجعي لها] [Token تحت خط]
            float line2RightW = 420F;
            float line2TokenW = bodyW - line2RightW - pad;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(bodyRight - line2RightW, y, line2RightW, h),
                Text = "ميزة المدفوعة مقدمًا المذكورة أعلاه بالرقم المرجعي لها (Token)",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(bodyX, y, line2TokenW, h),
                Text = tokenValue,
                Font = _lineBoldFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderWidth = 0.7F,
                WordWrap = false
            });
            y += h + 6F;

            // سطر 3: [في يوم وتاريخ] [date] [الساعة] [time] [من السادة شركة إيزي كاش للدفع الإلكتروني]
            float dateLabelW = 95F;
            float dateW = 120F;
            float timeLabelW = 62F;
            float timeW = 105F;
            float suffixW = bodyW - (dateLabelW + dateW + timeLabelW + timeW) - pad;

            float x = bodyRight;
            x -= dateLabelW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, dateLabelW, h),
                Text = "في يوم وتاريخ",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            x -= dateW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, dateW, h),
                Text = date,
                Font = _lineBoldFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderWidth = 0.7F,
                WordWrap = false
            });
            x -= timeLabelW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, timeLabelW, h),
                Text = "الساعة",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                WordWrap = false
            });
            x -= timeW;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, timeW, h),
                Text = time,
                Font = _lineBoldFont,
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.Bottom,
                BorderWidth = 0.7F,
                WordWrap = false
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(bodyX, y, suffixW, h),
                Text = "من السادة شركة إيزي كاش للدفع الإلكتروني",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            y += h + 6F;

            AddFullLine(band, bodyX, bodyW, ref y, "وأقر بصحة البيانات المدونة، وأتحمل مسؤولية استخدامها وفقًا للشروط المعتمدة.");
            y += 8F;
            AddFullLine(band, bodyX, bodyW, ref y, "المقر بما فيه ،،", true);
            y += 6F;
            AddFullLine(band, bodyX, bodyW, ref y, "توقيع العميل بصحة بياناته المذكورة أعلاه واستلام البطاقة:");
            y += 8F;

            float sigWidth = 420F;
            float sigX = bodyRight - sigWidth;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(sigX, y, sigWidth, 28F),
                Text = "اسم العميل: ...............................................",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            y += 34F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(sigX, y, sigWidth, 28F),
                Text = "التوقيع: ...................................................",
                Font = _lineFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });

            band.HeightF = 1080F;
        }

        private void AddFullLine(DetailBand band, float x, float lineWidth, ref float y, string text, bool bold = false)
        {
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, lineWidth, 35F),
                Text = text,
                Font = bold ? _lineBoldFont : _lineFont,
                // With RightToLeft = Yes the XRLabel treats TextAlignment
                // logically ("start"/"end" of the line, in reading order).
                // MiddleLeft therefore pins the text to the visual RIGHT
                // edge of the bounds, which is what we want for every
                // body paragraph so they all share the page's right
                // margin.
                TextAlignment = TextAlignment.MiddleLeft,
                RightToLeft = RightToLeft.Yes,
                Padding = BodyPadding,
                WordWrap = false
            });
            y += 40F;
        }

        private void DrawHeaderLogos(DetailBand band, float width)
        {
            const float y = 4F;
            const float misrWidth = 175F;
            const float easyWidth = 145F;
            const float logoHeight = 62F;

            Image misrLogo = LoadImage(new[]
            {
                "~/Areas/Pos/Content/images/kyc/banque-misr-logo.png",
                "~/Areas/Pos/Doc/BanqueMisr.png",
                "~/Areas/Pos/Doc/banque_misr.png",
                "~/Areas/Pos/Doc/banque-misr.png",
                "~/assets/images/banque-misr.png"
            });

            Image easyLogo = LoadImage(new[]
            {
                "~/Areas/Pos/Content/images/kyc/easycash-logo.png",
                "~/Areas/Pos/Doc/EasyCashLogo.png",
                "~/Areas/Pos/Doc/easycash-logo.png",
                "~/Areas/Pos/Content/easycash-logo.png",
                "~/assets/images/easycash-logo.png",
                "~/assets/images/logo.PNG"
            });

            if (easyLogo != null)
            {
                band.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF(0F, y, easyWidth, logoHeight),
                    Image = easyLogo,
                    Sizing = ImageSizeMode.ZoomImage,
                    ImageAlignment = ImageAlignment.MiddleCenter
                });
            }
            else
            {
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0F, y + 18F, easyWidth, 24F),
                    Text = "easycash",
                    Font = new Font("Tahoma", 12F, FontStyle.Bold),
                    TextAlignment = TextAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(74, 63, 133)
                });
            }

            if (misrLogo != null)
            {
                band.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF(width - misrWidth, y, misrWidth, logoHeight),
                    Image = misrLogo,
                    Sizing = ImageSizeMode.ZoomImage,
                    ImageAlignment = ImageAlignment.MiddleCenter
                });
            }
            else
            {
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(width - misrWidth, y + 18F, misrWidth, 24F),
                    Text = "بنك مصر / BANQUE MISR",
                    Font = new Font("Tahoma", 11F, FontStyle.Bold),
                    TextAlignment = TextAlignment.MiddleRight,
                    RightToLeft = RightToLeft.Yes
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
                    var image = Image.FromStream(new MemoryStream(bytes));
                    if (image.Width < 40 || image.Height < 20)
                    {
                        image.Dispose();
                        continue;
                    }

                    return image;
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
                return value.ToString("HH:mm:ss", ar);
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
