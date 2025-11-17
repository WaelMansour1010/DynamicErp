using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.Web;
using MyERP.Models;
using System.Linq;
using MyERP.Controllers;
using MyERP.Utils; // <-- مهم لوجود HelperController
using System.Drawing.Printing;
using System.Text; // <-- مهم لعملية التشفير

namespace MyERP.Reporting.Reports
{
    public partial class PropertyBatchInvoice : DevExpress.XtraReports.UI.XtraReport
    {
        // متغيرات لتخزين بيانات الشركة (الـ "بائع" في الفاتورة)
        private string companyName = "";
        private string arcompanyName = "";
        private string companyVat = "";
        private string regNo= "";
        

        // 1. الكود الإنشائي (Constructor)
        public PropertyBatchInvoice()
        {
            InitializeComponent();

            // 2. ربط حدث اللوجو
            this.xrPictureBox1.BeforePrint += this.xrPictureBox1_BeforePrint;

            // 3. ربط حدث الكيو آر كود
            // !!! تأكد أن اسم الكنترول في المصمم هو "xrBarCodeQRCode" !!!
            this.xrBarCodeQRCode.BeforePrint += this.xrBarCodeQRCode_BeforePrint;
        }

        // 4. دالة اللوجو (معدّلة لجلب بيانات الشركة)
        private void xrPictureBox1_BeforePrint(object sender, CancelEventArgs e)
        {
            int? DepartmentId = null;
            object branchIdValue = this.GetCurrentColumnValue("branch_id");

            if (branchIdValue != null && branchIdValue != DBNull.Value)
            {
                DepartmentId = Convert.ToInt32(branchIdValue);
            }

            var logo = HelperController.GetActivityLogo(DepartmentId);
            xrPictureBox1.ImageUrl = logo;

            // 7. جلب بيانات الشركة (البائع) وتخزينها لاستخدامها في الكيو آر كود
            using (MySoftERPEntity db = new MySoftERPEntity())
            {
                var settings = db.Companies.FirstOrDefault();
                if (settings != null)
                {
                    this.companyName = settings.EnName;
                    // !!! تأكد أن "VATNo" هو اسم الحقل الصحيح للرقم الضريبي في جدول SystemSettings
                    this.companyVat = settings.TaxNumber;
                    this.arcompanyName = settings.ArName;
                    this.regNo= settings.InsuranceNumber;


                }
                CompanyName.Text = this.arcompanyName;
                txtRegNo.Text = this.regNo;
                txtTaxNumber.Text = this.companyVat;
               

            }
        }


        private void Time_BeforePrint(object sender, CancelEventArgs e)
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            ((XRLabel)sender).Text = cTime.ToString("d MMMM yyyy h:mm tt");

        }
        // 5. دالة الكيو آر كود الجديدة (بنفس منطق التقرير الآخر)
        private void xrBarCodeQRCode_BeforePrint(object sender, CancelEventArgs e)
        {
            try
            {
                // --- أ. جلب البيانات المتغيرة من الـ SP ---

                // (تاريخ الإصدار)
                DateTime timeStamp = Convert.ToDateTime(this.GetCurrentColumnValue("IssueDate"));

                // (الإجمالي مع الضريبة)
                // اسم الحقل "PayableAmount" من الـ SP
                decimal invoiceTotal = Convert.ToDecimal(this.GetCurrentColumnValue("PayableAmount"));

                // (إجمالي الضريبة)
                // اسم الحقل "VatValue" من الـ SP
                decimal vatTotal = Convert.ToDecimal(this.GetCurrentColumnValue("VatValue"));


                // --- ب. جلب بيانات البائع (التي خزناها من الدالة السابقة) ---
                string sellerName = this.companyName;
                string vatNumber = this.companyVat;

                // --- ج. استدعاء نفس الدالة المساعدة من نظامك ---
                var qrCodeString = HelperController.EncodeTLV(sellerName, vatNumber, timeStamp.ToString(), invoiceTotal.ToString(), vatTotal.ToString());

                // --- د. وضع القيمة في الكنترول ---
                (sender as XRBarCode).Text = qrCodeString;
            }
            catch (Exception ex)
            {
                // لو حدث خطأ، اعرضه في الكود
                (sender as XRBarCode).Text = $"Error: {ex.Message}";
            }
        }
    }
}