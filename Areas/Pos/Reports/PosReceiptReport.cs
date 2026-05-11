using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.XtraReports.UI;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace MyERP.Areas.Pos.Reports
{
    // Layout target: Areas/Pos/Doc/SaleReportSample.pdf (exported from the legacy
    // Crystal SaleReport.rpt). Width is 80 mm thermal-receipt paper; data is loaded
    // strictly by Transactions.Transaction_ID via PosSqlRepository.GetReceipt.
    public class PosReceiptReport : XtraReport
    {
        private const float PaperWidthMm = 80F;
        private const float SideMarginMm = 2F;

        private readonly Font _microFont = new Font("Tahoma", 5.4F, FontStyle.Regular);
        private readonly Font _smallFont = new Font("Tahoma", 6F, FontStyle.Regular);
        private readonly Font _smallBold = new Font("Tahoma", 6F, FontStyle.Bold);
        private readonly Font _normalFont = new Font("Tahoma", 6.5F, FontStyle.Regular);
        private readonly Font _normalBold = new Font("Tahoma", 6.5F, FontStyle.Bold);
        private readonly Font _sectionFont = new Font("Tahoma", 7F, FontStyle.Bold);
        private readonly Font _companyFont = new Font("Tahoma", 8F, FontStyle.Bold);

        private readonly Color _sectionBack = Color.FromArgb(205, 205, 205);
        private TopMarginBand topMarginBand1;
        private DetailBand detailBand1;
        private BottomMarginBand bottomMarginBand1;
        private readonly Color _lineColor = Color.FromArgb(120, 120, 120);

        public PosReceiptReport(PosReceiptDto receipt)
        {
            if (receipt == null || receipt.Invoice == null)
            {
                throw new ArgumentNullException("receipt");
            }

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.Yes;

            var sideMargin = MmToReportUnit(SideMarginMm);
            PaperKind = DevExpress.Drawing.Printing.DXPaperKind.Custom;
            PageWidth = MmToReportUnit(PaperWidthMm);
            PageHeight = 1600;
            Margins = new Margins(sideMargin, sideMargin, 4, 4);

            var detail = new DetailBand();
            Bands.Add(detail);

            float width = PageWidth - Margins.Left - Margins.Right;
            float consumed = BuildReceipt(detail, receipt, width);
            detail.HeightF = consumed + 4;
        }

        private float BuildReceipt(DetailBand band, PosReceiptDto receipt, float width)
        {
            var invoice = receipt.Invoice;
            float y = 0F;

            string referenceNo = FirstNonEmpty(invoice.ManualNO, invoice.NoteSerial1,
                invoice.Transaction_ID.ToString(CultureInfo.InvariantCulture));
            string barcodeText = FirstNonEmpty(invoice.NoteSerial1,
                invoice.Transaction_ID.ToString(CultureInfo.InvariantCulture));
            DateTime txDate = invoice.TransactionDate.GetValueOrDefault(DateTime.Now);

            decimal feesNet = invoice.Items.Sum(i => i.Price * (i.Quantity <= 0 ? 1m : i.Quantity));
            if (feesNet <= 0)
            {
                feesNet = invoice.NetValue;
            }

            decimal discount = invoice.Items.Sum(i =>
                i.DiscountValue.GetValueOrDefault() + i.TotalDiscountPerLine.GetValueOrDefault());
            decimal vat = invoice.VatValue > 0
                ? invoice.VatValue
                : invoice.Items.Sum(i => i.Vat.GetValueOrDefault());
            decimal vatPercent = ResolveVatPercent(invoice, feesNet, vat);
            decimal feesWithVat = feesNet + vat - discount;
            if (invoice.TotalFees > 0)
            {
                feesWithVat = invoice.TotalFees;
            }

            decimal depositValue = invoice.ViolationsValue.GetValueOrDefault() > 0
                ? invoice.ViolationsValue.GetValueOrDefault()
                : invoice.RechargeValue;
            decimal totalPaid = invoice.PayedValue > 0 ? invoice.PayedValue : depositValue + feesWithVat;

            y = DrawHeader(band, y, width, barcodeText);
            if (invoice.IsCancelled)
            {
                band.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0, y, width, 14),
                    Text = "فاتورة ملغاة",
                    Font = new Font("Tahoma", 8F, FontStyle.Bold),
                    ForeColor = Color.DarkRed,
                    TextAlignment = TextAlignment.MiddleCenter
                });
                y += 16;
            }

            y = DrawSectionHeader(band, "بيانات الإيصال", y, width);
            y = DrawTriRow(band, "REF No.", "الرقم المرجعي", referenceNo, y, width, _smallBold);
            y = DrawTriRow(band, "Date", "تاريخ الإصدار", FormatArabicDate(txDate), y, width, _smallFont);
            y = DrawTriRow(band, string.Empty, "وتاريخ الحق", FormatArabicTime(txDate), y, width, _smallFont);
            y = DrawTriRow(band, "Branch Name", "إسم الفرع", Safe(invoice.BranchName), y, width, _smallFont);
            y = DrawTriRow(band, "Branch No.", "وعنوانة", IntToText(invoice.BranchId), y, width, _smallFont);
            y = DrawTriRow(band, "Branch Address", "رقم الفرع",
                FirstNonEmpty(invoice.BranchCode, invoice.BranchAddress), y, width, _smallFont);
            y = DrawTriRow(band, "Teller Name", "إسم التلر", Safe(invoice.EmpName), y, width, _smallFont);
            y = DrawTriRow(band, "Teller No.", "رقم التلر", IntToText(invoice.Emp_ID), y, width, _smallFont);
            y += 3;

            y = DrawSectionHeader(band, "بيانات العميل", y, width);
            y = DrawTriRow(band, string.Empty, "إسم العميل", Safe(invoice.CashCustomerName), y, width, _smallFont);
            y = DrawTriRow(band, string.Empty, "رقم العميل", Safe(invoice.CashCustomerPhone), y, width, _smallFont);
            y = DrawTriRow(band, string.Empty, "الرقم القومي",
                MaskNationalId(invoice.Tet_NumPoket), y, width, _smallFont);
            y += 3;

            y = DrawSectionHeader(band, "بيانات الإيداع والخدمات", y, width);
            y = DrawTableHeader(band, y, width);
            int idx = 1;
            foreach (var item in invoice.Items)
            {
                decimal qty = item.Quantity <= 0 ? 1m : item.Quantity;
                decimal lineTotal = item.Price * qty;
                y = DrawTableRow(band, IndexedName(idx, item.ItemName), Money(lineTotal),
                    y, width, _smallBold, false);
                idx++;
            }
            y = DrawTableRow(band, "قيمة مصروفات الإيداع والخدمات", Money(feesNet),
                y, width, _smallBold, true);
            y = DrawTableRow(band, "إجمالي الخصم", Money(discount),
                y, width, _smallBold, true);
            y = DrawVatTableRow(band, "ضريبة القيمة المضافة", FormatVatPercent(vatPercent),
                Money(vat), y, width);
            y = DrawTableRow(band, "إجمالي قيمة المصروفات بالضريبة", Money(feesWithVat),
                y, width, _smallBold, true, underline: true);
            y += 3;

            y = DrawSectionHeader(band, "ملخص بيانات الإيداع", y, width);
            y = DrawSummaryRow(band, "مبلغ الإيداع :", Money(depositValue), y, width, _smallBold);
            y = DrawSummaryRow(band, "قيمة الرسوم بالضريبة:", Money(feesWithVat), y, width, _smallBold);
            y = DrawSummaryRow(band, "إجمالي المدفوع :", Money(totalPaid), y, width, _normalBold);
            y = DrawInWordsParagraph(band, AmountInWordsEgp(totalPaid), y, width);
            y += 4;

            y = DrawFooterBox(band, y, width);

            return y;
        }

        private float DrawHeader(XRControl container, float y, float width, string barcodeText)
        {
            Image logo = LoadLogoImage();
            if (logo != null)
            {
                container.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF((width - 110) / 2F, y, 110, 38),
                    Image = logo,
                    Sizing = ImageSizeMode.ZoomImage
                });
                y += 40;
            }
            else
            {
                container.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0, y, width, 18),
                    Text = "easycash",
                    Font = new Font("Tahoma", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(54, 133, 72),
                    TextAlignment = TextAlignment.MiddleCenter
                });
                y += 20;
            }

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, width, 12),
                Text = "شركة إيزي كاش للدفع الإلكتروني",
                Font = _companyFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            y += 14;

            if (!string.IsNullOrEmpty(barcodeText))
            {
                var barcode = new XRBarCode
                {
                    BoundsF = new RectangleF((width - 150) / 2F, y, 150, 22),
                    Text = barcodeText,
                    AutoModule = true,
                    ShowText = false,
                    Symbology = new Code39Generator { WideNarrowRatio = 2F }
                };
                container.Controls.Add(barcode);
                y += 22;

                container.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0, y, width, 10),
                    Text = "*" + barcodeText + "*",
                    Font = _microFont,
                    TextAlignment = TextAlignment.MiddleCenter
                });
                y += 12;
            }

            return y + 2;
        }

        private float DrawSectionHeader(XRControl container, string text, float y, float width)
        {
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, width, 13),
                Text = text,
                Font = _sectionFont,
                BackColor = _sectionBack,
                Borders = BorderSide.All,
                BorderColor = _lineColor,
                BorderWidth = 0.5F,
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(2, 4, 0, 0),
                RightToLeft = RightToLeft.Yes
            });
            return y + 14;
        }

        private float DrawTriRow(XRControl container, string englishLabel, string arabicLabel,
            string value, float y, float width, Font valueFont, float height = 11)
        {
            float englishW = width * 0.22F;
            float valueW = width * 0.50F;
            float arabicW = width * 0.28F;

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, englishW, height),
                Text = englishLabel ?? string.Empty,
                Font = _microFont,
                TextAlignment = TextAlignment.MiddleLeft,
                Padding = new PaddingInfo(2, 1, 0, 0)
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(englishW, y, valueW, height),
                Text = value ?? string.Empty,
                Font = valueFont,
                TextAlignment = TextAlignment.MiddleCenter,
                CanGrow = true,
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(englishW + valueW, y, arabicW, height),
                Text = arabicLabel ?? string.Empty,
                Font = _smallFont,
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(0, 2, 0, 0),
                RightToLeft = RightToLeft.Yes
            });

            return y + height + 1;
        }

        private float DrawTableHeader(XRControl container, float y, float width)
        {
            float valueW = width * 0.30F;
            float descW = width - valueW;

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, valueW, 12),
                Text = "القيمة",
                Font = _smallBold,
                BackColor = _sectionBack,
                Borders = BorderSide.All,
                BorderColor = _lineColor,
                BorderWidth = 0.5F,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(valueW, y, descW, 12),
                Text = "البيان",
                Font = _smallBold,
                BackColor = _sectionBack,
                Borders = BorderSide.All,
                BorderColor = _lineColor,
                BorderWidth = 0.5F,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            return y + 12;
        }

        private float DrawTableRow(XRControl container, string description, string value,
            float y, float width, Font font, bool highlight, bool underline = false, float height = 12)
        {
            float valueW = width * 0.30F;
            float descW = width - valueW;
            BorderSide valueBorders = BorderSide.Left | BorderSide.Right | BorderSide.Bottom;
            BorderSide descBorders = BorderSide.Left | BorderSide.Right | BorderSide.Bottom;

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, valueW, height),
                Text = value ?? string.Empty,
                Font = font,
                Borders = valueBorders,
                BorderColor = _lineColor,
                BorderWidth = underline ? 0.8F : 0.4F,
                TextAlignment = TextAlignment.MiddleCenter,
                Padding = new PaddingInfo(2, 2, 0, 0),
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(valueW, y, descW, height),
                Text = description ?? string.Empty,
                Font = font,
                Borders = descBorders,
                BorderColor = _lineColor,
                BorderWidth = underline ? 0.8F : 0.4F,
                TextAlignment = highlight ? TextAlignment.MiddleRight : TextAlignment.MiddleRight,
                Padding = new PaddingInfo(0, 4, 0, 0),
                RightToLeft = RightToLeft.Yes,
                CanGrow = true
            });

            return y + height;
        }

        private float DrawVatTableRow(XRControl container, string description, string percent,
            string value, float y, float width, float height = 12)
        {
            float valueW = width * 0.30F;
            float percentW = width * 0.20F;
            float descW = width - valueW - percentW;
            BorderSide cellBorders = BorderSide.Left | BorderSide.Right | BorderSide.Bottom;

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, valueW, height),
                Text = value ?? string.Empty,
                Font = _smallBold,
                Borders = cellBorders,
                BorderColor = _lineColor,
                BorderWidth = 0.4F,
                TextAlignment = TextAlignment.MiddleCenter,
                Padding = new PaddingInfo(2, 2, 0, 0),
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(valueW, y, percentW, height),
                Text = percent ?? string.Empty,
                Font = _smallBold,
                Borders = cellBorders,
                BorderColor = _lineColor,
                BorderWidth = 0.4F,
                TextAlignment = TextAlignment.MiddleCenter,
                Padding = new PaddingInfo(2, 2, 0, 0)
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(valueW + percentW, y, descW, height),
                Text = description ?? string.Empty,
                Font = _smallBold,
                Borders = cellBorders,
                BorderColor = _lineColor,
                BorderWidth = 0.4F,
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(0, 4, 0, 0),
                RightToLeft = RightToLeft.Yes
            });

            return y + height;
        }

        private float DrawSummaryRow(XRControl container, string label, string value,
            float y, float width, Font font, float height = 13)
        {
            float valueW = width * 0.40F;
            float labelW = width - valueW;

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, valueW, height),
                Text = value ?? string.Empty,
                Font = font,
                TextAlignment = TextAlignment.MiddleCenter,
                Padding = new PaddingInfo(2, 2, 0, 0),
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(valueW, y, labelW, height),
                Text = label ?? string.Empty,
                Font = font,
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(0, 4, 0, 0),
                RightToLeft = RightToLeft.Yes
            });

            return y + height;
        }

        private float DrawInWordsParagraph(XRControl container, string text, float y, float width)
        {
            var label = new XRLabel
            {
                BoundsF = new RectangleF(0, y, width, 24),
                Text = text ?? string.Empty,
                Font = _smallBold,
                TextAlignment = TextAlignment.MiddleRight,
                Padding = new PaddingInfo(2, 2, 0, 0),
                RightToLeft = RightToLeft.Yes,
                CanGrow = true,
                WordWrap = true
            };
            container.Controls.Add(label);
            return y + label.HeightF + 2;
        }

        private float DrawFooterBox(XRControl container, float y, float width)
        {
            string[] lines = new[]
            {
                "عميلنا العزيز",
                "تم تقديم الطلب بنجاح وفى حاله فشل المدفوعة لأي سبب",
                "سيتم إسترداد المبلغ خلال يومي عمل",
                "نشكركم على إستخدامكم خدمات شركة إيزي كاش وبطاقة كيشني",
                "ونرجو لأي إستفسارات أوشكوى أو فى حالة إكتشاف فقد البطاقة",
                "أو تلفها أو طلب بدل فاقد برجاء التوجة لأقرب فرع بنك مصر",
                "وعدم التردد في الإتصال بمركز",
                "خدمة عملاء بنك مصرعلى الرقم المجاني 19888",
                "وللمزيد زوروا موقعنا الإلكتروني-19926",
                "www.EasyCash-eg.com"
            };

            float top = y;
            float lineHeight = 10F;
            float padding = 2F;
            float boxHeight = padding * 2 + lineHeight * lines.Length + 2;

            container.Controls.Add(new XRPanel
            {
                BoundsF = new RectangleF(0, top, width, boxHeight),
                Borders = BorderSide.All,
                BorderColor = _lineColor,
                BorderWidth = 0.5F
            });

            float lineY = top + padding;
            for (int i = 0; i < lines.Length; i++)
            {
                bool isHeader = i == 0;
                bool isStrong = i == 1 || i == 2;
                bool isWeb = i == lines.Length - 1;
                Font font = isHeader || isStrong ? _smallBold : _microFont;
                if (isWeb)
                {
                    font = _smallFont;
                }

                container.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(2, lineY, width - 4, lineHeight),
                    Text = lines[i],
                    Font = font,
                    TextAlignment = TextAlignment.MiddleCenter,
                    RightToLeft = isWeb ? RightToLeft.No : RightToLeft.Yes
                });
                lineY += lineHeight;
            }

            return top + boxHeight;
        }

        private static decimal ResolveVatPercent(PosInvoiceReviewDto invoice, decimal feesNet, decimal vat)
        {
            decimal fromItems = invoice.Items
                .Select(i => i.Vatyo.GetValueOrDefault())
                .DefaultIfEmpty(0m)
                .Max();
            if (fromItems > 0)
            {
                return fromItems;
            }

            if (feesNet > 0 && vat > 0)
            {
                return Math.Round(vat * 100m / feesNet, 2);
            }

            return 14m;
        }

        private static string FormatVatPercent(decimal percent)
        {
            if (percent <= 0)
            {
                return string.Empty;
            }

            string body = percent == Math.Truncate(percent)
                ? ((long)percent).ToString(CultureInfo.InvariantCulture)
                : percent.ToString("0.##", CultureInfo.InvariantCulture);
            return body + " %";
        }

        private static string IndexedName(int index, string name)
        {
            return index.ToString(CultureInfo.InvariantCulture) + "  " + Safe(name);
        }

        private static string FormatArabicDate(DateTime value)
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

        private static string MaskNationalId(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            if (trimmed.Length <= 4)
            {
                return new string('*', trimmed.Length);
            }

            return trimmed.Substring(0, 3) + new string('*', Math.Min(5, trimmed.Length - 3));
        }

        private static string Money(decimal value)
        {
            return value.ToString("#,##0.00", CultureInfo.InvariantCulture);
        }

        private static string IntToText(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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

        private static int MmToReportUnit(float millimeters)
        {
            return (int)Math.Round(millimeters / 25.4F * 100F, MidpointRounding.AwayFromZero);
        }

        private static Image LoadLogoImage()
        {
            string path = FindLogoPath();
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                return Image.FromStream(new MemoryStream(bytes));
            }
            catch
            {
                return null;
            }
        }

        private static string FindLogoPath()
        {
            var candidates = new List<string>();

            try
            {
                if (HostingEnvironment.IsHosted)
                {
                    candidates.Add(HostingEnvironment.MapPath("~/Areas/Pos/Doc/EasyCashLogo.png"));
                    candidates.Add(HostingEnvironment.MapPath("~/Areas/Pos/Doc/easycash-logo.png"));
                    candidates.Add(HostingEnvironment.MapPath("~/Areas/Pos/Content/easycash-logo.png"));
                    candidates.Add(HostingEnvironment.MapPath("~/assets/images/easycash-logo.png"));
                    candidates.Add(HostingEnvironment.MapPath("~/assets/images/logo.PNG"));
                }
            }
            catch
            {
                // hosting environment may not be available outside IIS
            }

            return candidates
                .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
                .FirstOrDefault();
        }

        private static string AmountInWordsEgp(decimal amount)
        {
            if (amount < 0)
            {
                amount = -amount;
            }

            long pounds = (long)Math.Floor(amount);
            int piasters = (int)Math.Round((amount - pounds) * 100m, MidpointRounding.AwayFromZero);
            if (piasters >= 100)
            {
                pounds += 1;
                piasters = 0;
            }

            var sb = new StringBuilder();
            sb.Append("فقط ");
            sb.Append(pounds == 0 ? "صفر" : NumberToArabicWords(pounds));
            sb.Append(" جنية مصري");
            if (piasters > 0)
            {
                sb.Append(" و ");
                sb.Append(NumberToArabicWords(piasters));
                sb.Append(" قرش");
            }
            sb.Append(" لاغير");
            return sb.ToString();
        }

        private static readonly string[] ArUnits =
        {
            string.Empty, "واحد", "اثنان", "ثلاثة", "اربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
            "عشرة", "احد عشر", "اثنا عشر", "ثلاثة عشر", "اربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر",
            "ثمانية عشر", "تسعة عشر"
        };

        private static readonly string[] ArTens =
        {
            string.Empty, string.Empty, "عشرون", "ثلاثون", "اربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
        };

        private static readonly string[] ArHundreds =
        {
            string.Empty, "مائة", "مئتان", "ثلاثمائة", "اربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
        };

        private static string NumberToArabicWords(long n)
        {
            if (n == 0)
            {
                return "صفر";
            }

            if (n < 0)
            {
                return "سالب " + NumberToArabicWords(-n);
            }

            var parts = new List<string>();
            long billions = n / 1000000000L;
            n %= 1000000000L;
            long millions = n / 1000000L;
            n %= 1000000L;
            long thousands = n / 1000L;
            long rest = n % 1000L;

            if (billions > 0)
            {
                parts.Add(BuildScaleSegment(billions, "مليار", "ملياران", "مليارات", "مليار"));
            }

            if (millions > 0)
            {
                parts.Add(BuildScaleSegment(millions, "مليون", "مليونان", "ملايين", "مليون"));
            }

            if (thousands > 0)
            {
                parts.Add(BuildScaleSegment(thousands, "ألف", "ألفان", "آلاف", "ألف"));
            }

            if (rest > 0)
            {
                parts.Add(SubThousand(rest));
            }

            return string.Join(" و ", parts);
        }

        private static string BuildScaleSegment(long count, string singular, string dual, string plural, string singularBig)
        {
            if (count == 1)
            {
                return singular;
            }

            if (count == 2)
            {
                return dual;
            }

            if (count >= 3 && count <= 10)
            {
                return ArUnits[count] + " " + plural;
            }

            return SubThousand(count) + " " + singularBig;
        }

        private static string SubThousand(long n)
        {
            if (n <= 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            long h = n / 100;
            long r = n % 100;
            if (h > 0)
            {
                parts.Add(ArHundreds[h]);
            }

            if (r > 0)
            {
                if (r < 20)
                {
                    parts.Add(ArUnits[r]);
                }
                else
                {
                    long t = r / 10;
                    long u = r % 10;
                    if (u > 0)
                    {
                        parts.Add(ArUnits[u] + " و " + ArTens[t]);
                    }
                    else
                    {
                        parts.Add(ArTens[t]);
                    }
                }
            }

            return string.Join(" و ", parts);
        }

        private void InitializeComponent()
        {
            this.topMarginBand1 = new DevExpress.XtraReports.UI.TopMarginBand();
            this.detailBand1 = new DevExpress.XtraReports.UI.DetailBand();
            this.bottomMarginBand1 = new DevExpress.XtraReports.UI.BottomMarginBand();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // topMarginBand1
            // 
            this.topMarginBand1.Name = "topMarginBand1";
            // 
            // detailBand1
            // 
            this.detailBand1.Name = "detailBand1";
            // 
            // bottomMarginBand1
            // 
            this.bottomMarginBand1.Name = "bottomMarginBand1";
            // 
            // PosReceiptReport
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.topMarginBand1,
            this.detailBand1,
            this.bottomMarginBand1});
            this.Version = "23.1";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }
    }
}
