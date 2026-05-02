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
    // Layout target: Areas/Pos/Doc/repCashCustomer4.pdf, exported from the legacy
    // Crystal report used by FrmCustCash.cmdPrint2_Click. Data is loaded strictly
    // by TblCusCsh.Id through PosSqlRepository.GetKeshniCardCustomerById.
    public class KycCardAcknowledgmentReport : XtraReport
    {
        private readonly Font _bodyFont = new Font("Tahoma", 11F, FontStyle.Regular);
        private readonly Font _bodyBold = new Font("Tahoma", 11F, FontStyle.Bold);
        private readonly Font _titleFont = new Font("Tahoma", 14F, FontStyle.Bold | FontStyle.Underline);
        private readonly Font _tokenLabelFont = new Font("Tahoma", 12F, FontStyle.Bold);
        private readonly Font _tokenValueFont = new Font("Tahoma", 12F, FontStyle.Bold);
        private readonly Font _dynamicFont = new Font("Tahoma", 11F, FontStyle.Bold);

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
            var contentWidth = a4WidthHundredthInch - Margins.Left - Margins.Right;
            BuildBody(detail, customer, issuedAt, contentWidth);
        }

        private void BuildBody(DetailBand band, PosCustomerLookupDto customer, DateTime issuedAt, float width)
        {
            var customerName = FirstNonEmpty(customer.CustomerName, customer.Name, customer.ArabicName0);
            var tokenValue = FirstNonEmpty(customer.VisaNumber, customer.CardSerial);
            var date = issuedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            var time = FormatArabicTime(issuedAt);

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

            const float tokenY = 190F;
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.55F, tokenY, width * 0.18F, 22),
                Text = "Token رقم",
                Font = _tokenLabelFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.No
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.30F, tokenY, width * 0.25F, 22),
                Text = tokenValue,
                Font = _tokenValueFont,
                TextAlignment = TextAlignment.MiddleCenter
            });

            var lineY = 270F;
            const float lineHeight = 30F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, 22),
                Text = "أقر أنا ......................................................... الموقع أدناه بأني إستلمت بطاقة بنك مصر - Easy Cash",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.45F, lineY - 4F, width * 0.30F, 22),
                Text = customerName,
                Font = _dynamicFont,
                BackColor = Color.White,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, 22),
                Text = "ميزة المدفوعة مقدما المذكورة أعلاه بالرقم المرجعي لها",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.05F, lineY, width * 0.40F, 22),
                Text = tokenValue,
                Font = _dynamicFont,
                BackColor = Color.White,
                TextAlignment = TextAlignment.MiddleCenter
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, 22),
                Text = "في يوم وتاريخ ......................... الساعة ............ من السادة شركة أيزي كاش للدفع الإلكتروني",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.55F, lineY - 4F, width * 0.15F, 22),
                Text = date,
                Font = _dynamicFont,
                BackColor = Color.White,
                TextAlignment = TextAlignment.MiddleCenter
            });
            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.32F, lineY - 4F, width * 0.13F, 22),
                Text = time,
                Font = _dynamicFont,
                BackColor = Color.White,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, 22),
                Text = "المقر بما فيه ,,",
                Font = _bodyBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight + 12F;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, lineY, width, 22),
                Text = "توقيع العميل بصحة بياناته المذكورة أعلاه واستلام البطاقة:",
                Font = _bodyBold,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.05F, lineY, width * 0.55F, 22),
                Text = "إسم العميل: ..........................................",
                Font = _bodyFont,
                TextAlignment = TextAlignment.MiddleRight,
                RightToLeft = RightToLeft.Yes
            });
            lineY += lineHeight;

            band.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(width * 0.05F, lineY, width * 0.55F, 22),
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

            var misrLogo = LoadImage(new[]
            {
                "~/Areas/Pos/Doc/BanqueMisr.png",
                "~/Areas/Pos/Doc/banque_misr.png",
                "~/Areas/Pos/Doc/banque-misr.png",
                "~/assets/images/banque-misr.png"
            });

            var easyLogo = LoadImage(new[]
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

            foreach (var virtualPath in virtualPaths)
            {
                string path;
                try
                {
                    path = HostingEnvironment.MapPath(virtualPath);
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
                    return Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
                }
                catch
                {
                    // Try next candidate.
                }
            }

            return null;
        }

        private static string FormatArabicTime(DateTime value)
        {
            try
            {
                return value.ToString("tt hh:mm:ss", CultureInfo.GetCultureInfo("ar-EG"));
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
