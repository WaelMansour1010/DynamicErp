using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.XtraReports.UI;
using MyERP.Areas.Pos.Models;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MyERP.Areas.Pos.Reports
{
    public class PosReceiptReport : XtraReport
    {
        private const float ReceiptWidthMm = 72F;
        private const float MarginMm = 0.5F;
        private readonly Font _microFont = new Font("Tahoma", 5.4F, FontStyle.Regular);
        private readonly Font _tinyFont = new Font("Tahoma", 5.8F, FontStyle.Regular);
        private readonly Font _normalFont = new Font("Tahoma", 6.3F, FontStyle.Regular);
        private readonly Font _boldFont = new Font("Tahoma", 6.5F, FontStyle.Bold);
        private readonly Font _headerFont = new Font("Tahoma", 6.8F, FontStyle.Bold);
        private readonly Font _logoFont = new Font("Tahoma", 11F, FontStyle.Bold);
        private readonly Color _sectionBackColor = Color.FromArgb(205, 205, 205);
        private readonly Color _lineColor = Color.FromArgb(120, 120, 120);

        public PosReceiptReport(PosReceiptDto receipt)
        {
            if (receipt == null || receipt.Invoice == null)
            {
                throw new ArgumentNullException("receipt");
            }

            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = RightToLeftLayout.Yes;
            PageWidth = MmToReportUnit(ReceiptWidthMm);
            PageHeight = 980;
            Margins = new System.Drawing.Printing.Margins(MmToReportUnit(MarginMm), MmToReportUnit(MarginMm), 2, 2);

            var detail = new DetailBand { HeightF = 930 };
            Bands.Add(detail);
            BuildReceipt(detail, receipt);
        }

        private void BuildReceipt(DetailBand band, PosReceiptDto receipt)
        {
            var invoice = receipt.Invoice;
            var y = 0F;
            var width = PageWidth - Margins.Left - Margins.Right;
            var referenceNo = FirstText(invoice.NoteSerial1, invoice.Transaction_ID.ToString(CultureInfo.InvariantCulture));
            var transactionDate = invoice.TransactionDate.GetValueOrDefault(DateTime.Now);
            var serviceName = invoice.Items.Count > 0 ? invoice.Items[0].ItemName : ServiceTypeName(invoice.TransactionType);
            var serviceFee = invoice.Items.Sum(i => i.Price * (i.Quantity <= 0 ? 1 : i.Quantity));
            if (serviceFee <= 0)
            {
                serviceFee = invoice.NetValue;
            }

            var discount = invoice.Items.Sum(i => i.DiscountValue.GetValueOrDefault() + i.TotalDiscountPerLine.GetValueOrDefault());
            var vat = invoice.VatValue > 0 ? invoice.VatValue : invoice.Items.Sum(i => i.Vat.GetValueOrDefault());
            var feeTotal = serviceFee + vat - discount;
            if (invoice.TotalFees > 0)
            {
                feeTotal = invoice.TotalFees;
            }

            var depositValue = invoice.ViolationsValue.GetValueOrDefault() > 0 ? invoice.ViolationsValue.GetValueOrDefault() : invoice.RechargeValue;
            var paidTotal = invoice.PayedValue > 0 ? invoice.PayedValue : depositValue + feeTotal;

            AddLogo(band, y, width); y += 44;
            AddBarcode(band, referenceNo, y, width); y += 42;

            AddSectionHeader(band, "بيانات الإيصال", y, width); y += 15;
            AddBilingualRow(band, "REF No.", "رقم المرجع", referenceNo, y, width); y += 13;
            AddBilingualRow(band, "Date", "التاريخ", transactionDate.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture), y, width); y += 13;
            AddBilingualRow(band, "Branch Name", "اسم الفرع", invoice.BranchName, y, width); y += 13;
            AddBilingualRow(band, "Branch No.", "رقم الفرع", FirstText(invoice.BranchCode, Number(invoice.BranchId)), y, width); y += 13;
            AddBilingualRow(band, "Branch Address", "عنوان الفرع", invoice.BranchAddress, y, width, 22); y += 22;
            AddBilingualRow(band, "Teller Name", "اسم الصراف", invoice.EmpName, y, width); y += 13;
            AddBilingualRow(band, "Teller No.", "رقم الصراف", Number(invoice.Emp_ID), y, width); y += 15;

            AddSectionHeader(band, "بيانات العميل", y, width); y += 15;
            AddBilingualRow(band, "Customer name", "اسم العميل", invoice.CashCustomerName, y, width); y += 13;
            AddBilingualRow(band, "Customer phone", "رقم العميل", invoice.CashCustomerPhone, y, width); y += 13;
            AddBilingualRow(band, "ID", "ID", invoice.IPN, y, width); y += 13;
            AddBilingualRow(band, "National/IPN", "الرقم القومي / IPN", FirstText(invoice.ManualNO, invoice.Tet_NumPoket), y, width); y += 13;
            if (!string.IsNullOrWhiteSpace(invoice.VisaNumber))
            {
                AddBilingualRow(band, "Card No.", "رقم الكارت", invoice.VisaNumber, y, width); y += 13;
            }

            AddSectionHeader(band, "بيانات الإيداع والخدمات", y, width); y += 15;
            AddBilingualRow(band, "Service name", "اسم الخدمة", serviceName, y, width, 22); y += 22;
            AddBilingualRow(band, "Value", "القيمة", Money(depositValue), y, width); y += 13;
            AddBilingualRow(band, "Net", "الصافي", Money(serviceFee), y, width); y += 13;
            AddBilingualRow(band, "Discount", "الخصم", Money(discount), y, width); y += 13;
            AddBilingualRow(band, "VAT", "ضريبة القيمة المضافة", Money(vat), y, width); y += 13;
            AddBilingualRow(band, "Total/Value", "الإجمالي", Money(feeTotal), y, width); y += 15;

            AddSectionHeader(band, "ملخص بيانات الإيداع", y, width); y += 15;
            AddSummaryRow(band, "DepositValue", "قيمة الإيداع", Money(depositValue), y, width); y += 15;
            AddSummaryRow(band, "Net", "صافي الخدمة", Money(serviceFee), y, width); y += 15;
            AddSummaryRow(band, "Total paid", "إجمالي المدفوع", Money(paidTotal), y, width, true); y += 18;

            AddSeparator(band, y, width); y += 6;
            AddCentered(band, "برجاء مراجعة بيانات الإيصال قبل مغادرة الفرع", y, width, _tinyFont); y += 12;
            AddCentered(band, "يحتفظ العميل بهذا الإيصال كمرجع للعملية", y, width, _tinyFont); y += 12;
            AddCentered(band, "خدمة عملاء كيشني" + FooterPhone(invoice), y, width, _microFont); y += 11;
            AddCentered(band, "شكراً لاستخدامكم خدماتنا", y, width, _normalFont);
        }

        private void AddLogo(XRControl container, float y, float width)
        {
            var logoPath = FindLogoPath();
            if (!string.IsNullOrEmpty(logoPath))
            {
                container.Controls.Add(new XRPictureBox
                {
                    BoundsF = new RectangleF((width - 92) / 2F, y, 92, 26),
                    Image = Image.FromFile(logoPath),
                    Sizing = ImageSizeMode.ZoomImage
                });
            }
            else
            {
                container.Controls.Add(new XRLabel
                {
                    BoundsF = new RectangleF(0, y, width, 20),
                    Text = "EasyCash",
                    Font = _logoFont,
                    ForeColor = Color.FromArgb(54, 133, 72),
                    TextAlignment = TextAlignment.MiddleCenter
                });
            }

            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y + 25, width, 11),
                Text = "كيشني لخدمات الدفع والتحصيل",
                Font = _boldFont,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y + 36, width, 9),
                Text = "Keshni Payment Receipt",
                Font = _microFont,
                TextAlignment = TextAlignment.MiddleCenter
            });
        }

        private void AddBarcode(XRControl container, string text, float y, float width)
        {
            container.Controls.Add(new XRBarCode
            {
                BoundsF = new RectangleF((width - 145) / 2F, y, 145, 25),
                Text = text ?? string.Empty,
                AutoModule = true,
                ShowText = false,
                Symbology = new Code128Generator()
            });
            AddCentered(container, text, y + 25, width, _microFont);
        }

        private void AddSectionHeader(XRControl container, string text, float y, float width)
        {
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, width, 13),
                Text = text,
                Font = _headerFont,
                BackColor = _sectionBackColor,
                Borders = BorderSide.All,
                BorderColor = _lineColor,
                BorderWidth = 0.5F,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
        }

        private void AddBilingualRow(XRControl container, string englishCaption, string arabicCaption, string value, float y, float width, float height = 12)
        {
            AddCell(container, englishCaption, 0, y, width * 0.23F, height, _microFont, TextAlignment.MiddleLeft, false);
            AddValueCell(container, value, width * 0.23F, y, width * 0.49F, height);
            AddCell(container, arabicCaption, width * 0.72F, y, width * 0.28F, height, _microFont, TextAlignment.MiddleRight, true);
        }

        private void AddSummaryRow(XRControl container, string englishCaption, string arabicCaption, string value, float y, float width, bool total = false)
        {
            var font = total ? _boldFont : _normalFont;
            AddCell(container, englishCaption, 0, y, width * 0.25F, 13, font, TextAlignment.MiddleLeft, false, total);
            AddValueCell(container, value, width * 0.25F, y, width * 0.47F, 13, total);
            AddCell(container, arabicCaption, width * 0.72F, y, width * 0.28F, 13, font, TextAlignment.MiddleRight, true, total);
        }

        private void AddCell(XRControl container, string text, float x, float y, float width, float height, Font font, TextAlignment alignment, bool rtl, bool boldBorder = false)
        {
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, width, height),
                Text = text ?? string.Empty,
                Font = font,
                Borders = BorderSide.None,
                BorderColor = _lineColor,
                BorderWidth = boldBorder ? 1F : 0.35F,
                Padding = new PaddingInfo(2, 2, 0, 0),
                TextAlignment = alignment,
                RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No,
                CanGrow = true
            });
        }

        private void AddValueCell(XRControl container, string text, float x, float y, float width, float height, bool bold = false)
        {
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(x, y, width, height),
                Text = text ?? string.Empty,
                Font = bold ? _boldFont : _normalFont,
                Borders = BorderSide.Bottom,
                BorderColor = _lineColor,
                BorderWidth = bold ? 0.8F : 0.3F,
                Padding = new PaddingInfo(1, 1, 0, 0),
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                CanGrow = true
            });
        }

        private void AddCentered(XRControl container, string text, float y, float width, Font font)
        {
            container.Controls.Add(new XRLabel
            {
                BoundsF = new RectangleF(0, y, width, 10),
                Text = text ?? string.Empty,
                Font = font,
                TextAlignment = TextAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes
            });
        }

        private void AddSeparator(XRControl container, float y, float width)
        {
            container.Controls.Add(new XRLine
            {
                BoundsF = new RectangleF(0, y, width, 2),
                LineWidth = 1,
                ForeColor = _lineColor
            });
        }

        private static string FooterPhone(PosInvoiceReviewDto invoice)
        {
            return string.IsNullOrWhiteSpace(invoice.BranchPhone) ? string.Empty : " | " + invoice.BranchPhone.Trim();
        }

        private static string Money(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Number(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string FirstText(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static string ServiceTypeName(string type)
        {
            switch ((type ?? string.Empty).ToLowerInvariant())
            {
                case "cash-out":
                    return "خدمة كاش أوت";
                case "card":
                    return "كارت كيشني";
                case "violations":
                    return "رسوم سداد مخالفات النيابة";
                default:
                    return "خدمة كاش إن";
            }
        }

        private static int MmToReportUnit(float millimeters)
        {
            return (int)Math.Round(millimeters / 25.4F * 100F, MidpointRounding.AwayFromZero);
        }

        private static string FindLogoPath()
        {
            var candidates = new[]
            {
                @"F:\Source Code\SatriahMain\Cayshny\Special\SaleReportLogo.png",
                @"F:\Source Code\SatriahMain\Cayshny\Special\EasyCashLogo.png",
                @"F:\Source Code\SatriahMain\Cayshny\Garphics\Icons\EasyCashLogo.png"
            };

            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
