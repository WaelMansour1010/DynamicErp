using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyERP.Areas.Pos.Data
{
    public class PosSqlRepository
    {
        public const string WebInvoiceSourceMarker = "WEB_POS";

        private static readonly object PosSystemErrorLogEnsureLock = new object();
        private static bool _posSystemErrorLogEnsured;

        private readonly string _connectionString;

        public PosSqlRepository()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public PosUserContext LoginPosUser(string username, string password)
        {
            const string sql = @"
SELECT TOP (1)
    UserID,
    UserName
FROM dbo.TblUsers
WHERE UserName = @username
  AND [PassWord] = @password
  AND ISNULL(isDeactivated, 0) = 0
ORDER BY UserID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@username", SqlDbType.NVarChar, 255).Value = username ?? string.Empty;
                command.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = password ?? string.Empty;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return GetPosUserDefaults(ReadInt(reader, "UserID").GetValueOrDefault());
                }
            }
        }

        public PosUserContext GetActiveAdminUserContextByUserName(string username)
        {
            const string sql = @"
SELECT TOP (1)
    UserID
FROM dbo.TblUsers
WHERE UserName = @username
  AND ISNULL(isDeactivated, 0) = 0
  AND ISNULL(UserType, -1) = 0
ORDER BY UserID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@username", SqlDbType.NVarChar, 255).Value = username ?? string.Empty;

                connection.Open();
                var userId = command.ExecuteScalar();
                if (userId == null || userId == DBNull.Value)
                {
                    return null;
                }

                return GetPosUserDefaults(Convert.ToInt32(userId, CultureInfo.InvariantCulture));
            }
        }

        public PosUserContext GetPosUserDefaults(int userId)
        {
            EnsurePosUserCategoryColumn();
            const string sql = @"
SELECT TOP (1)
    u.UserID,
    u.UserName,
    u.UserType,
    u.UserCategory,
    u.Empid,
    e.Emp_Name AS EmpName,
    u.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), u.BranchId)) AS BranchName,
    u.StoreID,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), u.StoreID)) AS StoreName,
    u.BoxID,
    COALESCE(NULLIF(box.BoxName, N''), NULLIF(box.BoxNameE, N''), N'خزنة ' + CONVERT(NVARCHAR(20), u.BoxID)) AS BoxName,
    u.FullPremis,
    u.IsReturnAllowed,
    u.CanEditKYC,
    u.CustomerService,
    u.IsFullAccsesCustomerService,
    u.SalemManReport,
    u.CloseReport,
    u.CloseFinanceReport,
    u.DiscountReport,
    u.C360Report,
    u.DailyTransReport,
    u.SalesReportComplete,
    u.DailyTransReport2,
    u.DailyTransReportSectors,
    u.AllReport
FROM dbo.TblUsers u
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = u.BranchId
LEFT JOIN dbo.TblStore s ON s.StoreID = u.StoreID
LEFT JOIN dbo.TblBoxesData box ON box.BoxID = u.BoxID
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
WHERE u.UserID = @userId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var cashPayment = GetDefaultCashPaymentForUser(userId);
                    var context = new PosUserContext
                    {
                        UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                        UserName = ReadString(reader, "UserName"),
                        UserType = ReadInt(reader, "UserType"),
                        UserCategory = ReadString(reader, "UserCategory"),
                        EmpId = ReadInt(reader, "Empid"),
                        EmpName = ReadString(reader, "EmpName"),
                        BranchId = ReadInt(reader, "BranchId"),
                        BranchName = ReadString(reader, "BranchName"),
                        StoreId = ReadInt(reader, "StoreID"),
                        StoreName = ReadString(reader, "StoreName"),
                        BoxId = ReadInt(reader, "BoxID"),
                        BoxName = ReadString(reader, "BoxName"),
                        PaymentNetId = cashPayment != null ? (int?)cashPayment.PaymentID : null,
                        PaymentTypeId = cashPayment != null ? (int?)cashPayment.PaymentID : null,
                        PaymentName = cashPayment != null ? cashPayment.PaymentName : null,
                        BankId = cashPayment != null ? cashPayment.BankId : null,
                        BankName = cashPayment != null ? cashPayment.BankName : null,
                        CanChangeDefaults = ReadInt(reader, "UserType").GetValueOrDefault(-1) == 0,
                        IsFullAccess = ReadInt(reader, "UserType").GetValueOrDefault(-1) == 0,
                        CanReturn = ReadBoolean(reader, "IsReturnAllowed"),
                        CustomerService = ReadBoolean(reader, "CustomerService"),
                        IsFullAccessCustomerService = ReadBoolean(reader, "IsFullAccsesCustomerService"),
                        CanOpenCashCustomer = ReadBoolean(reader, "CanEditKYC") || ReadBoolean(reader, "CustomerService") || ReadBoolean(reader, "IsFullAccsesCustomerService"),
                        CanReportSalesmen = ReadBoolean(reader, "SalemManReport"),
                        CanReportClosings = ReadBoolean(reader, "CloseReport"),
                        CanReportFinanceClosing = ReadBoolean(reader, "CloseFinanceReport"),
                        CanReportDiscounts = ReadBoolean(reader, "DiscountReport"),
                        CanReportIndicators = ReadBoolean(reader, "C360Report"),
                        CanReportDailyTransactions = ReadBoolean(reader, "DailyTransReport"),
                        CanReportSalesComplete = ReadBoolean(reader, "SalesReportComplete"),
                        CanReportDailyTransactions2 = ReadBoolean(reader, "DailyTransReport2"),
                        CanReportDailyTransactionsSectors = ReadBoolean(reader, "DailyTransReportSectors"),
                        CanReportAllSales = ReadBoolean(reader, "AllReport"),
                        SystemOptions = GetPosSystemOptions()
                    };

                    var permissions = GetPosUserPermissions(userId);
                    context.IsFullAccess = context.UserType.GetValueOrDefault(-1) == 0;
                    context.CanChangeDefaults = context.IsFullAccess;
                    context.CanSave = permissions.CanSave;
                    context.CanPrint = permissions.CanPrint;
                    context.CanReturn = context.CanReturn || permissions.CanReturn;
                    context.CanOpenCashCustomer = context.CanOpenCashCustomer || permissions.CanOpenCashCustomer;
                    context.CanManagePrintTemplates = context.IsFullAccess || permissions.CanManagePrintTemplates;
                    ApplyFullAccessReportPermissions(context);
                    ApplyTemporaryPosPermissions(context);
                    if (context.CanTeller)
                    {
                        context.CanSave = true;
                    }
                    context.CanViewReports = HasAnyReportPermission(context);
                    context.CanViewAdminDashboard = context.IsFullAccess;
                    context.CanOpenSales = true;
                    context.CanCancelOrReturn = context.CanReturn;

                    return context;
                }
            }
        }

        public PosUserContext GetPosUserPermissions(int userId)
        {
            const string sql = @"
SELECT
    COUNT(1) AS PermissionRows,
    SUM(CASE WHEN ScreenName = N'FrmSaleBill6' THEN 1 ELSE 0 END) AS SalePermissionRows,
    MAX(CASE WHEN ScreenName = N'FrmSaleBill6' THEN CONVERT(INT, ISNULL(CanAdd, 0)) ELSE 0 END) AS CanSave,
    MAX(CASE WHEN ScreenName = N'FrmSaleBill6' THEN CONVERT(INT, ISNULL(CanPrint, 0)) ELSE 0 END) AS CanPrint,
    MAX(CASE WHEN ScreenName = N'FrmSaleBill6' THEN CONVERT(INT, ISNULL(CanDelete, 0)) ELSE 0 END) AS CanReturn,
    MAX(CASE WHEN ScreenName = N'FrmCustCash' AND (ISNULL(CanAdd, 0) = 1 OR ISNULL(CanEdit, 0) = 1 OR ISNULL(FullAccess, 0) = 1) THEN 1 ELSE 0 END) AS CanOpenCashCustomer,
    MAX(CASE WHEN ScreenName = N'FrmPosPrintTemplate' AND (ISNULL(CanAdd, 0) = 1 OR ISNULL(CanEdit, 0) = 1 OR ISNULL(FullAccess, 0) = 1) THEN 1 ELSE 0 END) AS CanManagePrintTemplates,
    MAX(CONVERT(INT, ISNULL(FullAccess, 0))) AS FullAccess
FROM dbo.ScreenJuncUser
WHERE User_ID = @userId
  AND ScreenName IN (N'FrmSaleBill6', N'FrmCustCash', N'FrmPosPrintTemplate');";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new PosUserContext { CanSave = true, CanPrint = true, CanReturn = false, CanOpenCashCustomer = true };
                    }

                    var hasSalePermissionRows = ReadInt(reader, "SalePermissionRows").GetValueOrDefault() > 0;
                    var fullAccess = ReadInt(reader, "FullAccess").GetValueOrDefault() > 0;
                    return new PosUserContext
                    {
                        IsFullAccess = fullAccess,
                        CanSave = !hasSalePermissionRows || fullAccess || ReadInt(reader, "CanSave").GetValueOrDefault() > 0,
                        CanPrint = fullAccess || ReadInt(reader, "CanPrint").GetValueOrDefault() > 0,
                        CanReturn = fullAccess || ReadInt(reader, "CanReturn").GetValueOrDefault() > 0,
                        CanOpenCashCustomer = fullAccess || ReadInt(reader, "CanOpenCashCustomer").GetValueOrDefault() > 0,
                        CanManagePrintTemplates = fullAccess || ReadInt(reader, "CanManagePrintTemplates").GetValueOrDefault() > 0
                    };
                }
            }
        }

        private static void ApplyFullAccessReportPermissions(PosUserContext context)
        {
            if (context == null || !context.IsFullAccess)
            {
                return;
            }

            context.CanReportSalesmen = true;
            context.CanReportClosings = true;
            context.CanReportFinanceClosing = true;
            context.CanReportDiscounts = true;
            context.CanReportIndicators = true;
            context.CanReportDailyTransactions = true;
            context.CanReportSalesComplete = true;
            context.CanReportDailyTransactions2 = true;
            context.CanReportDailyTransactionsSectors = true;
            context.CanReportAllSales = true;
            context.CanReportSalesComplete2 = true;
            context.CanReportSalesCompleteGovernorates = true;
            context.CanReportSalesCompleteDepartments = true;
            context.CanReportSalesCompleteAnalytical = true;
            context.CanReportStoreSerials = true;
            context.CanViewAccountingReports = true;
            context.CanViewAccountStatement = true;
            context.CanViewTrialBalance = true;
            context.CanViewIncomeStatement = true;
            context.CanViewGeneralLedgerAssistant = true;
            context.CanCreateJournalEntry = true;
            context.CanEditJournalEntry = true;
            context.CanDeleteJournalEntry = true;
        }

        private static bool HasAnyReportPermission(PosUserContext context)
        {
            return context != null && (
                context.CanReportSalesmen ||
                context.CanReportClosings ||
                context.CanReportFinanceClosing ||
                context.CanReportDiscounts ||
                context.CanReportIndicators ||
                context.CanReportDailyTransactions ||
                context.CanReportSalesComplete ||
                context.CanReportDailyTransactions2 ||
                context.CanReportDailyTransactionsSectors ||
                context.CanReportAllSales ||
                context.CanReportSalesComplete2 ||
                context.CanReportSalesCompleteGovernorates ||
                context.CanReportSalesCompleteDepartments ||
                context.CanReportSalesCompleteAnalytical ||
                context.CanReportStoreSerials ||
                context.CanViewAccountingReports ||
                context.CanViewAccountStatement ||
                context.CanViewTrialBalance ||
                context.CanViewIncomeStatement ||
                context.CanViewGeneralLedgerAssistant);
        }

        private void ApplyTemporaryPosPermissions(PosUserContext context)
        {
            if (context == null)
            {
                return;
            }

            if (context.IsFullAccess)
            {
                context.CanViewJournalEntry = true;
                context.CanOpenClosing = true;
                context.CanExecuteClosing = true;
                context.CanOpenSales = true;
                context.CanEditInvoice = true;
                context.CanOpenPayments = true;
                context.CanExecutePayments = true;
                context.CanEditPayments = true;
                context.CanTeller = true;
                context.CanCancelOrReturn = true;
                context.CanEditKyc = true;
                context.CustomerService = true;
                context.IsFullAccessCustomerService = true;
                context.CanPrintKycAcknowledgment = true;
                context.CanPrintKycCard = true;
                context.CanViewAccountingReports = true;
                context.CanViewAccountStatement = true;
                context.CanViewTrialBalance = true;
                context.CanViewIncomeStatement = true;
                context.CanViewGeneralLedgerAssistant = true;
                context.CanCreateJournalEntry = true;
                context.CanEditJournalEntry = true;
                context.CanDeleteJournalEntry = true;
                return;
            }

            var permissions = GetTemporaryPosPermissions(context.UserId);
            var isTeller = IsTellerCategory(context.UserCategory) || IsTemporaryAllowed(permissions, "CanTeller");
            context.CanSave = context.CanSave || isTeller || IsTemporaryAllowed(permissions, "CanSaveInvoice");
            context.CanEditInvoice = context.CanChangeDefaults || IsTemporaryAllowed(permissions, "CanEditInvoice");
            context.CanViewJournalEntry = IsTemporaryAllowed(permissions, "CanViewJournalEntry");
            context.CanOpenClosing = !IsTemporaryDenied(permissions, "CanOpenClosing") && (isTeller || IsTemporaryAllowed(permissions, "CanOpenClosing"));
            context.CanExecuteClosing = !IsTemporaryDenied(permissions, "CanExecuteClosing") && (isTeller || IsTemporaryAllowed(permissions, "CanExecuteClosing"));
            context.CanOpenSales = !permissions.ContainsKey("CanOpenSales") || IsTemporaryAllowed(permissions, "CanOpenSales");
            context.CanOpenPayments = IsTemporaryAllowed(permissions, "CanOpenPayments");
            context.CanExecutePayments = IsTemporaryAllowed(permissions, "CanExecutePayments");
            context.CanEditPayments = IsTemporaryAllowed(permissions, "CanEditPayments");
            context.CanTeller = isTeller;
            context.CanCancelOrReturn = context.CanReturn || IsTemporaryAllowed(permissions, "CanCancelOrReturn");
            context.CustomerService = context.CustomerService || IsTemporaryAllowed(permissions, "CustomerService");
            context.IsFullAccessCustomerService = context.IsFullAccessCustomerService || IsTemporaryAllowed(permissions, "IsFullAccsesCustomerService");
            context.CanOpenCashCustomer = context.CanOpenCashCustomer || context.CustomerService || context.IsFullAccessCustomerService;
            context.CanEditKyc = context.CanOpenCashCustomer || IsTemporaryAllowed(permissions, "CanEditKyc");
            context.CanPrintKycAcknowledgment = context.CanPrint || IsTemporaryAllowed(permissions, "CanPrintKycAcknowledgment");
            context.CanPrintKycCard = context.CanPrint || IsTemporaryAllowed(permissions, "CanPrintKycCard");
            context.CanViewReports = context.CanViewReports || IsTemporaryAllowed(permissions, "CanViewReports");
            context.CanReportSalesmen = context.CanReportSalesmen || IsTemporaryAllowed(permissions, "CanReportSalesmen");
            context.CanReportClosings = context.CanReportClosings || IsTemporaryAllowed(permissions, "CanReportClosings");
            context.CanReportFinanceClosing = context.CanReportFinanceClosing || IsTemporaryAllowed(permissions, "CanReportFinanceClosing");
            context.CanReportDiscounts = context.CanReportDiscounts || IsTemporaryAllowed(permissions, "CanReportDiscounts");
            context.CanReportIndicators = context.CanReportIndicators || IsTemporaryAllowed(permissions, "CanReportIndicators");
            context.CanReportDailyTransactions = context.CanReportDailyTransactions || IsTemporaryAllowed(permissions, "CanReportDailyTransactions");
            context.CanReportSalesComplete = context.CanReportSalesComplete || IsTemporaryAllowed(permissions, "CanReportSalesComplete");
            context.CanReportDailyTransactions2 = context.CanReportDailyTransactions2 || IsTemporaryAllowed(permissions, "CanReportDailyTransactions2");
            context.CanReportDailyTransactionsSectors = context.CanReportDailyTransactionsSectors || IsTemporaryAllowed(permissions, "CanReportDailyTransactionsSectors");
            context.CanReportAllSales = context.CanReportAllSales || IsTemporaryAllowed(permissions, "CanReportAllSales");
            context.CanReportSalesComplete2 = IsTemporaryAllowed(permissions, "CanReportSalesComplete2");
            context.CanReportSalesCompleteGovernorates = IsTemporaryAllowed(permissions, "CanReportSalesCompleteGovernorates");
            context.CanReportSalesCompleteDepartments = IsTemporaryAllowed(permissions, "CanReportSalesCompleteDepartments");
            context.CanReportSalesCompleteAnalytical = IsTemporaryAllowed(permissions, "CanReportSalesCompleteAnalytical");
            context.CanReportStoreSerials = IsTemporaryAllowed(permissions, "CanReportStoreSerials");
            context.CanViewAccountingReports = context.CanViewAccountingReports || IsTemporaryAllowed(permissions, "CanViewAccountingReports");
            context.CanViewAccountStatement = context.CanViewAccountStatement || IsTemporaryAllowed(permissions, "CanViewAccountStatement");
            context.CanViewTrialBalance = context.CanViewTrialBalance || IsTemporaryAllowed(permissions, "CanViewTrialBalance");
            context.CanViewIncomeStatement = context.CanViewIncomeStatement || IsTemporaryAllowed(permissions, "CanViewIncomeStatement");
            context.CanViewGeneralLedgerAssistant = context.CanViewGeneralLedgerAssistant || IsTemporaryAllowed(permissions, "CanViewGeneralLedgerAssistant");
            context.CanCreateJournalEntry = context.CanCreateJournalEntry || IsTemporaryAllowed(permissions, "CanCreateJournalEntry");
            context.CanEditJournalEntry = context.CanEditJournalEntry || IsTemporaryAllowed(permissions, "CanEditJournalEntry");
            context.CanDeleteJournalEntry = context.CanDeleteJournalEntry || IsTemporaryAllowed(permissions, "CanDeleteJournalEntry");
        }

        private static bool IsTemporaryAllowed(IDictionary<string, bool> permissions, string key)
        {
            bool value;
            return permissions != null && permissions.TryGetValue(key, out value) && value;
        }

        private static bool IsTemporaryDenied(IDictionary<string, bool> permissions, string key)
        {
            bool value;
            return permissions != null && permissions.TryGetValue(key, out value) && !value;
        }

        private static bool IsTellerCategory(string category)
        {
            return string.Equals((category ?? string.Empty).Trim(), "تلر", StringComparison.OrdinalIgnoreCase)
                || string.Equals((category ?? string.Empty).Trim(), "Teller", StringComparison.OrdinalIgnoreCase);
        }

        private IDictionary<string, bool> GetTemporaryPosPermissions(int userId)
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NULL
BEGIN
    SELECT CAST(NULL AS NVARCHAR(100)) AS PermissionKey, CAST(0 AS BIT) AS IsAllowed WHERE 1 = 0;
END
ELSE
BEGIN
    SELECT PermissionKey, IsAllowed
    FROM dbo.POS_UserPermissions
    WHERE UserID = @userId;
END";
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = ReadString(reader, "PermissionKey");
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            result[key] = ReadBoolean(reader, "IsAllowed");
                        }
                    }
                }
            }

            return result;
        }

        public PosSystemOptionsDto GetPosSystemOptions()
        {
            const string sql = @"
SELECT TOP (1)
    PercentVisa,
    MinVisa,
    MaxVisa,
    PercentVisaPur,
    MinVisaPur,
    MaxVisaPur
FROM dbo.TblOptions;";

            var options = new PosSystemOptionsDto
            {
                CashCustomerNameMustEnter = true,
                TradingPOS = false,
                PosShape2 = false,
                Todo = "Commission defaults loaded from verified dbo.TblOptions fields. Other POS SystemOptions remain TODO."
            };

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        options.PercentVisa = ReadDecimal(reader, "PercentVisa").GetValueOrDefault();
                        options.MinVisa = ReadDecimal(reader, "MinVisa").GetValueOrDefault();
                        options.MaxVisa = ReadDecimal(reader, "MaxVisa").GetValueOrDefault();
                        options.PercentVisaPur = ReadDecimal(reader, "PercentVisaPur").GetValueOrDefault();
                        options.MinVisaPur = ReadDecimal(reader, "MinVisaPur").GetValueOrDefault();
                        options.MaxVisaPur = ReadDecimal(reader, "MaxVisaPur").GetValueOrDefault();
                    }
                }
            }

            return options;
        }

        public PosCommissionResult CalculateCommission(PosCommissionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var rechargeValue = request.RechargeValue < 0 ? 0 : request.RechargeValue;
            var isCashOut = IsCashOutService(request.ServiceType);
            var isViolations = IsViolationsService(request.ServiceType);
            var isCard = IsCardService(request.ServiceType);
            var item = request.ItemID.HasValue ? GetItemCommissionDetails(request.ItemID.Value) : null;
            var vatPercent = isCashOut || isViolations ? 0m : ResolveVatPercent(request.ItemID, request.Vatyo);

            if (isCard)
            {
                var cardItem = request.ItemID.HasValue ? GetItemById(request.ItemID.Value, request.BranchId) : null;
                var cardFee = cardItem == null ? 0m : cardItem.Price;
                return BuildCommissionResult(request.ItemID, 0m, cardFee, vatPercent, "FrmSaleBill6 Keshni Card UnitSalesPrice", 0, 0, 0, false);
            }

            var systemOptions = GetPosSystemOptions();

            decimal percent;
            decimal min;
            decimal max;
            var source = "SystemOptions";

            if (isViolations)
            {
                var violationFee = request.ItemID.HasValue ? GetViolationServiceFee(request.ItemID.Value) : 0m;
                return BuildCommissionResult(request.ItemID, rechargeValue, violationFee, 0m, "TblItemsUnits.UnitSalesPrice traffic violations", 0, 0, 0, false);
            }

            if (isCashOut)
            {
                var cashRange = request.ItemID.HasValue ? GetItemPriceRangeSalesCommission(rechargeValue, request.ItemID.Value) : null;
                var cashOutMachineCommission = cashRange != null ? cashRange.Cost : 0m;
                if (cashRange != null && cashRange.Price > 0)
                {
                    return BuildCommissionResult(request.ItemID, rechargeValue, cashRange.Price, vatPercent, "CheckPriceRangeSales3/tblItemsCash", 0, 0, 0, true, cashOutMachineCommission);
                }

                if (item != null && HasSalesCommission(item))
                {
                    percent = item.PercentVisa.GetValueOrDefault();
                    min = item.MinVisa.GetValueOrDefault();
                    max = item.MaxVisa.GetValueOrDefault();
                    source = "TblItems cash-out sales commission";
                }
                else
                {
                    percent = systemOptions.PercentVisa;
                    min = systemOptions.MinVisa;
                    max = systemOptions.MaxVisa;
                    source = "TblOptions cash-out sales commission";
                }

                var purchaseCommission = ApplyMinMax(rechargeValue * percent / 100m, min, max);
                return BuildCommissionResult(request.ItemID, rechargeValue, purchaseCommission, vatPercent, source, percent, min, max, true, cashOutMachineCommission);
            }

            var rangeCommission = GetPriceRangeSalesCommission(rechargeValue);
            if (rangeCommission.HasValue && rangeCommission.Value > 0)
            {
                return BuildCommissionResult(request.ItemID, rechargeValue, rangeCommission.Value, vatPercent, "CheckPriceRangeSales2/tblOptionsCash", 0, 0, 0);
            }

            var itemRangeCommission = request.ItemID.HasValue ? GetItemPriceRangeSalesCommission(rechargeValue, request.ItemID.Value) : null;
            if (itemRangeCommission != null && itemRangeCommission.Price > 0)
            {
                return BuildCommissionResult(request.ItemID, rechargeValue, itemRangeCommission.Price, vatPercent, "CheckPriceRangeSales3/tblItemsCash", 0, 0, 0);
            }

            if (item != null && HasSalesCommission(item))
            {
                percent = item.PercentVisa.GetValueOrDefault();
                min = item.MinVisa.GetValueOrDefault();
                max = item.MaxVisa.GetValueOrDefault();
                source = "TblItems sales commission";
            }
            else
            {
                percent = systemOptions.PercentVisa;
                min = systemOptions.MinVisa;
                max = systemOptions.MaxVisa;
                source = "TblOptions sales commission";
            }

            var salesCommission = ApplyMinMax(rechargeValue * percent / 100m, min, max);
            return BuildCommissionResult(request.ItemID, rechargeValue, salesCommission, vatPercent, source, percent, min, max);
        }

        public IList<PosItemLookupDto> GetItems(string term)
        {
            var items = new List<PosItemLookupDto>();

            const string sql = @"
WITH CandidateItems AS
(
    SELECT TOP (20)
        i.ItemID,
        i.ItemName,
        i.ItemNamee,
        i.ItemCode,
        i.SallingPrice,
        i.CostPrice,
        i.ItemCase,
        i.ItemType
    FROM dbo.TblItems i
    WHERE
        (@term = N''
         OR CONVERT(NVARCHAR(50), i.ItemID) LIKE @like
         OR ISNULL(i.ItemCode, N'') LIKE @like
         OR ISNULL(i.ItemName, N'') LIKE @like
         OR ISNULL(i.ItemNamee, N'') LIKE @like)
    ORDER BY i.ItemID
)
SELECT
    i.ItemID AS Item_ID,
    COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), i.ItemID)) AS ItemName,
    i.ItemCode,
    iu.UnitID AS UnitId,
    COALESCE(NULLIF(u.UnitName, N''), NULLIF(u.UnitNamee, N''), CONVERT(NVARCHAR(50), iu.UnitID)) AS UnitName,
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) AS DECIMAL(18, 4)) AS Price,
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) AS DECIMAL(18, 4)) AS ShowPrice,
    CAST(COALESCE(iu.UnitFactor, 1) AS DECIMAL(18, 4)) AS QtyBySmalltUnit,
    CAST(0 AS DECIMAL(18, 4)) AS Vat,
    CAST(0 AS DECIMAL(18, 4)) AS Vatyo,
    CAST(0 AS DECIMAL(18, 4)) AS DiscountValue,
    CAST(0 AS DECIMAL(18, 4)) AS TotalDiscountPerLine,
    CAST(NULL AS INT) AS StoreID2,
    CAST(NULL AS INT) AS BranchId,
    COALESCE(i.ItemCase, 1) AS ItemCase,
    CAST(COALESCE(i.CostPrice, 0) AS DECIMAL(18, 4)) AS CostPrice,
    COALESCE(i.ItemType, 0) AS SavedItemType
FROM CandidateItems i
OUTER APPLY
(
    SELECT TOP (1)
        iu0.UnitID,
        iu0.UnitFactor,
        iu0.UnitSalesPrice
    FROM dbo.TblItemsUnits iu0
    WHERE iu0.ItemID = i.ItemID
    ORDER BY CASE WHEN ISNULL(iu0.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END, iu0.JunckID
) iu
LEFT JOIN dbo.TblUnites u ON u.UnitID = iu.UnitID
ORDER BY i.ItemID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                var searchTerm = term ?? string.Empty;
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = searchTerm;
                command.Parameters.Add("@like", SqlDbType.NVarChar, 110).Value = "%" + searchTerm + "%";

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PosItemLookupDto
                        {
                            Item_ID = ReadInt(reader, "Item_ID").GetValueOrDefault(),
                            ItemName = ReadString(reader, "ItemName"),
                            ItemCode = ReadString(reader, "ItemCode"),
                            UnitId = ReadInt(reader, "UnitId"),
                            UnitName = ReadString(reader, "UnitName"),
                            Price = ReadDecimal(reader, "Price").GetValueOrDefault(),
                            ShowPrice = ReadDecimal(reader, "ShowPrice").GetValueOrDefault(),
                            QtyBySmalltUnit = ReadDecimal(reader, "QtyBySmalltUnit").GetValueOrDefault(1),
                            Vat = ReadDecimal(reader, "Vat").GetValueOrDefault(),
                            Vatyo = ReadDecimal(reader, "Vatyo").GetValueOrDefault(),
                            DiscountValue = ReadDecimal(reader, "DiscountValue").GetValueOrDefault(),
                            TotalDiscountPerLine = ReadDecimal(reader, "TotalDiscountPerLine").GetValueOrDefault(),
                            StoreID2 = ReadInt(reader, "StoreID2"),
                            BranchId = ReadInt(reader, "BranchId"),
                            ItemCase = ReadInt(reader, "ItemCase").GetValueOrDefault(1),
                            CostPrice = ReadDecimal(reader, "CostPrice").GetValueOrDefault(),
                            SavedItemType = ReadInt(reader, "SavedItemType").GetValueOrDefault()
                        });
                    }
                }

            }

            return items;
        }

        public IList<PosPaymentTypeDto> GetPaymentTypes()
        {
            var paymentTypes = new List<PosPaymentTypeDto>();

            const string sql = @"
SELECT TOP (100)
    pt.PaymentID,
    COALESCE(NULLIF(pt.PaymentName, N''), NULLIF(pt.PaymentNamee, N''), CONVERT(NVARCHAR(50), pt.PaymentID)) AS PaymentName,
    pt.BankId,
    COALESCE(NULLIF(b.BankName, N''), NULLIF(b.BankNamee, N''), CASE WHEN ISNULL(pt.BankId, 0) = 0 THEN N'نقدي' ELSE NULL END) AS BankName,
    pt.MaxValue
FROM dbo.TblPaymentType pt
LEFT JOIN dbo.BanksData b ON b.BankID = pt.BankId
ORDER BY pt.PaymentID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        paymentTypes.Add(new PosPaymentTypeDto
                        {
                            PaymentID = ReadInt(reader, "PaymentID").GetValueOrDefault(),
                            PaymentName = ReadString(reader, "PaymentName"),
                            BankId = ReadInt(reader, "BankId"),
                            BankName = ReadString(reader, "BankName"),
                            MaxValue = ReadDecimal(reader, "MaxValue")
                        });
                    }
                }
            }

            return paymentTypes;
        }

        private PosItemLookupDto GetItemById(int itemId, int? branchId = null)
        {
            const string sql = @"
SELECT TOP (1)
    i.ItemID AS Item_ID,
    COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), i.ItemID)) AS ItemName,
    i.ItemCode,
    CAST(1 AS DECIMAL(18, 4)) AS Quantity,
    COALESCE(iu.UnitID, 1) AS UnitId,
    COALESCE(NULLIF(u.UnitName, N''), NULLIF(u.UnitNamee, N''), CONVERT(NVARCHAR(50), COALESCE(iu.UnitID, 1))) AS UnitName,
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) AS DECIMAL(18, 4)) AS Price,
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) AS DECIMAL(18, 4)) AS ShowPrice,
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) + ((COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) * COALESCE(v.Vatyo, 0)) / 100.0) AS DECIMAL(18, 4)) AS TotalPrice,
    CAST(COALESCE(iu.UnitFactor, 1) AS DECIMAL(18, 4)) AS QtyBySmalltUnit,
    CAST((COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) * COALESCE(v.Vatyo, 0)) / 100.0 AS DECIMAL(18, 4)) AS Vat,
    CAST(COALESCE(v.Vatyo, 0) AS DECIMAL(18, 4)) AS Vatyo,
    CAST(0 AS DECIMAL(18, 4)) AS DiscountValue,
    CAST(0 AS DECIMAL(18, 4)) AS TotalDiscountPerLine,
    CAST(NULL AS INT) AS StoreID2,
    CAST(NULL AS INT) AS BranchId,
    COALESCE(i.ItemCase, 1) AS ItemCase,
    CAST(COALESCE(i.CostPrice, 0) AS DECIMAL(18, 4)) AS CostPrice,
    COALESCE(i.ItemType, 0) AS SavedItemType
FROM dbo.TblItems AS i
OUTER APPLY
(
    SELECT TOP (1)
        iu0.UnitID,
        iu0.UnitFactor,
        iu0.UnitSalesPrice
    FROM dbo.TblItemsUnits iu0
    WHERE iu0.ItemID = i.ItemID
      AND (@branchId IS NULL OR ISNULL(iu0.BranchId, 0) = @branchId OR ISNULL(iu0.DefaultUnit, 0) = 1)
    ORDER BY
        CASE WHEN @branchId IS NOT NULL AND ISNULL(iu0.BranchId, 0) = @branchId THEN 0 ELSE 1 END,
        CASE WHEN ISNULL(iu0.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END,
        iu0.JunckID
) iu
OUTER APPLY
(
    SELECT TOP (1)
        td.Vatyo
    FROM dbo.Transaction_Details td
    WHERE td.Item_ID = i.ItemID
      AND ISNULL(td.Vatyo, 0) > 0
    ORDER BY td.Transaction_ID DESC
) v
LEFT JOIN dbo.TblUnites AS u ON u.UnitID = iu.UnitID
WHERE i.ItemID = @itemId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadItemLookup(reader) : null;
                }
            }
        }

        private PosPaymentTypeDto GetDefaultCashPaymentForUser(int userId)
        {
            const string sql = @"
DECLARE @LinkUsersWithPayment BIT;

SELECT TOP (1)
    @LinkUsersWithPayment = ISNULL(LinkUsersWithPayment, 0)
FROM dbo.TblOptions;

SELECT TOP (1)
    pt.PaymentID,
    COALESCE(NULLIF(pt.PaymentName, N''), NULLIF(pt.PaymentNamee, N''), CONVERT(NVARCHAR(50), pt.PaymentID)) AS PaymentName,
    pt.BankId,
    COALESCE(NULLIF(b.BankName, N''), NULLIF(b.BankNamee, N''), CASE WHEN ISNULL(pt.BankId, 0) = 0 THEN N'نقدي' ELSE NULL END) AS BankName,
    pt.MaxValue
FROM dbo.TblPaymentType pt
LEFT JOIN dbo.BanksData b ON b.BankID = pt.BankId
INNER JOIN dbo.TblPaymentUser pu ON pu.PaynetID = pt.PaymentID
WHERE pu.UserID = @userId
  AND (pt.PaymentName = N'نقدي' OR pt.PaymentNamee = N'Cash')
ORDER BY pt.PaymentID;

IF @@ROWCOUNT = 0
BEGIN
    SELECT TOP (1)
        pt.PaymentID,
        COALESCE(NULLIF(pt.PaymentName, N''), NULLIF(pt.PaymentNamee, N''), CONVERT(NVARCHAR(50), pt.PaymentID)) AS PaymentName,
        pt.BankId,
        COALESCE(NULLIF(b.BankName, N''), NULLIF(b.BankNamee, N''), CASE WHEN ISNULL(pt.BankId, 0) = 0 THEN N'نقدي' ELSE NULL END) AS BankName,
        pt.MaxValue
    FROM dbo.TblPaymentType pt
    LEFT JOIN dbo.BanksData b ON b.BankID = pt.BankId
    WHERE (ISNULL(@LinkUsersWithPayment, 0) = 0 OR NOT EXISTS (SELECT 1 FROM dbo.TblPaymentUser WHERE UserID = @userId))
      AND (pt.PaymentName = N'نقدي' OR pt.PaymentNamee = N'Cash')
    ORDER BY pt.PaymentID;
END";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    do
                    {
                        while (reader.Read())
                        {
                            if (reader.FieldCount == 0)
                            {
                                continue;
                            }

                            return new PosPaymentTypeDto
                            {
                                PaymentID = ReadInt(reader, "PaymentID").GetValueOrDefault(),
                                PaymentName = ReadString(reader, "PaymentName"),
                                BankId = ReadInt(reader, "BankId"),
                                BankName = ReadString(reader, "BankName"),
                                MaxValue = ReadDecimal(reader, "MaxValue")
                            };
                        }
                    } while (reader.NextResult());
                }
            }

            return null;
        }

        public IList<PosItemLookupDto> GetDefaultServiceItem(string serviceType, int? itemId = null, int? branchId = null)
        {
            if (itemId.HasValue)
            {
                var selectedItem = GetItemById(itemId.Value, branchId);
                if (selectedItem != null)
                {
                    if (IsViolationsService(serviceType))
                    {
                        NormalizeViolationServiceItem(selectedItem);
                    }

                    return new List<PosItemLookupDto> { selectedItem };
                }
            }

            if (IsViolationsService(serviceType))
            {
                var violationItem = GetItemById(20, branchId);
                if (violationItem != null)
                {
                    NormalizeViolationServiceItem(violationItem);
                    return new List<PosItemLookupDto> { violationItem };
                }
            }

            if (IsCardService(serviceType))
            {
                var cardItem = GetItemById(19, branchId);
                if (cardItem != null)
                {
                    return new List<PosItemLookupDto> { cardItem };
                }
            }

            var primaryService = GetPrimaryServiceItems(serviceType).FirstOrDefault();
            if (primaryService != null)
            {
                var serviceItem = GetItemById(primaryService.Id, branchId);
                if (serviceItem != null)
                {
                    return new List<PosItemLookupDto> { serviceItem };
                }
            }

            var items = new List<PosItemLookupDto>();
            var filter = GetServiceFilter(serviceType);

            var sql = @"
WITH ServiceItems AS
(
    SELECT TOP (1)
        d.Item_ID,
        COUNT(*) AS UseCount,
        MAX(t.Transaction_ID) AS LastTransactionID
    FROM dbo.Transactions t
    INNER JOIN dbo.Transaction_Details d ON d.Transaction_ID = t.Transaction_ID
    LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
    WHERE t.Transaction_Type = 21
      AND " + filter + @"
    GROUP BY d.Item_ID
    ORDER BY COUNT(*) DESC, MAX(t.Transaction_ID) DESC
)
SELECT
    d.Item_ID,
    COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)) AS ItemName,
    i.ItemCode,
    d.Quantity,
    d.UnitId,
    COALESCE(NULLIF(u.UnitName, N''), NULLIF(u.UnitNamee, N''), CONVERT(NVARCHAR(50), d.UnitId)) AS UnitName,
    CAST(COALESCE(d.Price, 0) AS DECIMAL(18, 4)) AS Price,
    CAST(COALESCE(d.showPrice, d.Price, 0) AS DECIMAL(18, 4)) AS ShowPrice,
    CAST(COALESCE(d.TotalPrice, d.Quantity * d.Price, d.Price, 0) AS DECIMAL(18, 4)) AS TotalPrice,
    CAST(COALESCE(d.QtyBySmalltUnit, 1) AS DECIMAL(18, 4)) AS QtyBySmalltUnit,
    CAST(COALESCE(d.Vat, 0) AS DECIMAL(18, 4)) AS Vat,
    CAST(COALESCE(d.Vatyo, 0) AS DECIMAL(18, 4)) AS Vatyo,
    CAST(COALESCE(d.discountvalue, 0) AS DECIMAL(18, 4)) AS DiscountValue,
    CAST(COALESCE(d.TotalDiscountPerLine, 0) AS DECIMAL(18, 4)) AS TotalDiscountPerLine,
    d.StoreID2,
    d.BranchId,
    COALESCE(d.ItemCase, i.ItemCase, 1) AS ItemCase,
    CAST(COALESCE(d.CostPrice, i.CostPrice, 0) AS DECIMAL(18, 4)) AS CostPrice,
    COALESCE(d.SavedItemType, i.ItemType, 0) AS SavedItemType
FROM ServiceItems si
INNER JOIN dbo.Transaction_Details d ON d.Item_ID = si.Item_ID
INNER JOIN dbo.Transactions t ON t.Transaction_ID = d.Transaction_ID
LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
LEFT JOIN dbo.TblUnites u ON u.UnitID = d.UnitId
WHERE t.Transaction_Type = 21
  AND " + filter + @"
ORDER BY t.Transaction_ID DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        items.Add(ReadItemLookup(reader));
                    }
                }
            }

            if (items.Count == 0 && IsViolationsService(serviceType))
            {
                var violationItem = GetItemById(20, branchId);
                if (violationItem != null)
                {
                    NormalizeViolationServiceItem(violationItem);
                    items.Add(violationItem);
                }
            }

            return items;
        }

        public IList<PosServiceOptionDto> GetPrimaryServiceItems(string serviceType)
        {
            var items = new List<PosServiceOptionDto>();
            var where = GetPrimaryServiceWhere(serviceType);
            var sql = @"
SELECT TOP (50)
    ti.ItemID AS Id,
    COALESCE(NULLIF(ti.ItemName, N''), NULLIF(ti.ItemNamee, N''), ti.ItemCode, CONVERT(NVARCHAR(50), ti.ItemID)) AS Name
FROM dbo.TblItems ti
WHERE " + where + @"
ORDER BY ti.ItemID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PosServiceOptionDto
                        {
                            Id = ReadInt(reader, "Id").GetValueOrDefault(),
                            Name = ReadString(reader, "Name")
                        });
                    }
                }
            }

            return items;
        }

        public IList<PosServiceOptionDto> GetSecondaryServiceItems(string serviceType, int itemId)
        {
            var items = new List<PosServiceOptionDto>();
            string sql;

            if (string.Equals((serviceType ?? string.Empty).Trim(), "other-items", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"
SELECT
    u.UnitID AS Id,
    COALESCE(NULLIF(u.UnitName, N''), NULLIF(u.UnitNamee, N''), CONVERT(NVARCHAR(50), u.UnitID)) AS Name
FROM dbo.TblUnites u
INNER JOIN dbo.TblItemsUnits iu ON u.UnitID = iu.UnitID
WHERE iu.ItemID IN
(
    SELECT ItemID
    FROM dbo.TblItems
    WHERE ISNULL(OtherItems, 0) = 1
      AND ItemID = @itemId
)
ORDER BY CASE WHEN ISNULL(iu.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END, iu.JunckID;";
            }
            else
            {
                sql = @"
SELECT
    id AS Id,
    Name
FROM dbo.TblSpecification
WHERE ItemIDService = @itemId
   OR ItemIDService2 = @itemId
ORDER BY id;";
            }

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PosServiceOptionDto
                        {
                            Id = ReadInt(reader, "Id").GetValueOrDefault(),
                            Name = ReadString(reader, "Name")
                        });
                    }
                }
            }

            return items;
        }

        public bool IsServiceItemValidForTransactionType(string serviceType, int itemId)
        {
            if (itemId <= 0)
            {
                return false;
            }

            var normalizedType = (serviceType ?? string.Empty).Trim().ToLowerInvariant();
            string where;
            switch (normalizedType)
            {
                case "cash-out":
                    where = "ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.IsPriceIsPerview, 0) = 1 AND ISNULL(ti.HaveGuarantee, 0) = 0";
                    break;
                case "card":
                    where = "(ti.ItemID IN (1, 19) OR (ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.HaveGuarantee, 0) = 1))";
                    break;
                case "violations":
                    where = "(ti.ItemID = 20 OR ISNULL(ti.TrafficViolations, 0) = 1 OR ti.ItemName LIKE N'%مخالف%')";
                    break;
                case "cash-in":
                    where = "ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.IsPriceIsPerview, 0) = 0 AND ISNULL(ti.HaveGuarantee, 0) = 0 AND ISNULL(ti.TrafficViolations, 0) = 0";
                    break;
                default:
                    return false;
            }

            var sql = @"
SELECT TOP (1) 1
FROM dbo.TblItems ti
WHERE ti.ItemID = @itemId
  AND " + where + ";";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public IList<PosBranchDto> GetBranches()
        {
            var branches = new List<PosBranchDto>();

            const string sql = @"
SELECT TOP (200)
    b.BranchId,
    COALESCE(NULLIF(br.branch_name, N''), NULLIF(br.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), b.BranchId)) AS BranchName
FROM
(
    SELECT branch_id AS BranchId FROM dbo.TblBranchesData WHERE branch_id IS NOT NULL
    UNION
    SELECT DISTINCT BranchId FROM dbo.Transactions WHERE Transaction_Type = 21 AND BranchId IS NOT NULL
) b
LEFT JOIN dbo.TblBranchesData br ON br.branch_id = b.BranchId
ORDER BY b.BranchId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        branches.Add(new PosBranchDto
                        {
                            BranchId = ReadInt(reader, "BranchId").GetValueOrDefault(),
                            BranchName = ReadString(reader, "BranchName")
                        });
                    }
                }
            }

            return branches;
        }

        public IList<PosStoreDto> GetStoresByBranch(int? branchId)
        {
            var stores = new List<PosStoreDto>();

            const string sql = @"
SELECT TOP (100)
    StoreID,
    COALESCE(NULLIF(StoreName, N''), NULLIF(StoreNamee, N''), N'مخزن ' + CONVERT(NVARCHAR(20), StoreID)) AS StoreName,
    BranchId
FROM dbo.TblStore
WHERE @branchId IS NULL OR BranchId = @branchId
ORDER BY StoreID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stores.Add(new PosStoreDto
                        {
                            StoreID = ReadInt(reader, "StoreID").GetValueOrDefault(),
                            StoreName = ReadString(reader, "StoreName"),
                            BranchId = ReadInt(reader, "BranchId")
                        });
                    }
                }
            }

            return stores;
        }

        public IList<PosCashBoxDto> GetCashBoxesByUserOrBranch(int? userId, int? branchId)
        {
            var cashBoxes = new List<PosCashBoxDto>();

            const string sql = @"
SELECT DISTINCT TOP (100)
    b.BoxID,
    COALESCE(NULLIF(b.BoxName, N''), NULLIF(b.BoxNameE, N''), N'خزنة ' + CONVERT(NVARCHAR(20), b.BoxID)) AS BoxName,
    b.BranchId,
    ISNULL(b.IsWallet, 0) AS IsWallet,
    ISNULL(b.IsTerminalPOS, 0) AS IsTerminalPOS
FROM dbo.TblBoxesData b
LEFT JOIN dbo.TblUsersBoxes ub ON ub.BoxId = b.BoxID AND ub.userid = @userId
WHERE (@userId IS NULL OR ub.id IS NOT NULL OR NOT EXISTS (SELECT 1 FROM dbo.TblUsersBoxes WHERE userid = @userId))
  AND (@branchId IS NULL OR b.BranchId = @branchId OR b.BranchId IS NULL)
ORDER BY b.BoxID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cashBoxes.Add(new PosCashBoxDto
                        {
                            BoxID = ReadInt(reader, "BoxID").GetValueOrDefault(),
                            BoxName = ReadString(reader, "BoxName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            IsWallet = ReadBoolean(reader, "IsWallet"),
                            IsTerminalPOS = ReadBoolean(reader, "IsTerminalPOS")
                        });
                    }
                }
            }

            return cashBoxes;
        }

        public PosCustomerLookupDto LookupCustomerByPhone(string phone)
        {
            return LookupKeshniCardCustomer(phone, null, false);
        }

        public PosCustomerLookupDto LookupCashCustomer(string phone)
        {
            return LookupKeshniCardCustomer(phone, null, false);
        }

        public IList<PosCustomerLookupDto> SearchKeshniCardCustomers(string term, int? branchId, bool canChangeDefaults)
        {
            var customers = new List<PosCustomerLookupDto>();
            if (string.IsNullOrWhiteSpace(term))
            {
                return customers;
            }

            const string sql = @"
SELECT TOP (20)
    c.Id,
    c.name,
    c.namee,
    c.ArabicName0,
    c.ArabicName1,
    c.ArabicName2,
    c.ArabicName3,
    c.EnglishName0,
    c.EnglishName1,
    c.EnglishName2,
    c.EnglishName3,
    c.EnglishName5,
    c.EnglishName6,
    c.EnglishName7,
    COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N'')) AS CustomerName,
    COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N'')) AS Phone,
    c.PhoneNo2,
    c.CardId,
    c.CardNo,
    c.card,
    c.CardSource,
    c.tel,
    c.Tet_NumPoket,
    c.Address,
    c.MailAdress,
    c.Nationality,
    c.BirthDate,
    c.CardDate,
    c.CardEndDate,
    c.OrderDate,
    c.EasyCashType,
    c.BranchID,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)) AS BranchName
FROM dbo.TblCusCsh c
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
WHERE ISNULL(c.EasyCashType, 0) = 0
  AND (@branchId IS NULL OR @canChangeDefaults = 1 OR c.BranchID = @branchId)
  AND
    (
      c.PhoneNo2 = @term
      OR c.PhoneNo = @term
      OR c.tel = @term
      OR c.CardNo = @term
      OR c.CardId = @term
      OR c.Tet_NumPoket = @term
      OR c.name LIKE N'%' + @term + N'%'
  )
ORDER BY c.Id DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@term", SqlDbType.NVarChar, 255).Value = term.Trim();
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(MapKeshniCardCustomer(reader));
                    }
                }
            }

            return customers;
        }

        public PosCustomerLookupDto LookupKeshniCardCustomer(string term, int? branchId, bool canChangeDefaults)
        {
            return SearchKeshniCardCustomers(term, branchId, canChangeDefaults).FirstOrDefault();
        }

        public IList<PosCustomerLookupDto> SearchUnusedKeshniCardCustomers(string term, int? branchId, bool canChangeDefaults)
        {
            var customers = new List<PosCustomerLookupDto>();
            if (string.IsNullOrWhiteSpace(term))
            {
                return customers;
            }

            const string sql = @"
SELECT TOP (20)
    c.Id,
    c.name,
    c.namee,
    c.ArabicName0,
    c.ArabicName1,
    c.ArabicName2,
    c.ArabicName3,
    c.EnglishName0,
    c.EnglishName1,
    c.EnglishName2,
    c.EnglishName3,
    c.EnglishName5,
    c.EnglishName6,
    c.EnglishName7,
    COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N'')) AS CustomerName,
    COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N'')) AS Phone,
    c.PhoneNo2,
    c.CardId,
    c.CardNo,
    c.card,
    c.CardSource,
    c.tel,
    c.Tet_NumPoket,
    c.Address,
    c.MailAdress,
    c.Nationality,
    c.BirthDate,
    c.CardDate,
    c.CardEndDate,
    c.OrderDate,
    c.EasyCashType,
    c.BranchID,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)) AS BranchName
FROM dbo.TblCusCsh c
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
WHERE ISNULL(c.EasyCashType, 0) = 0
  AND (@branchId IS NULL OR @canChangeDefaults = 1 OR c.BranchID = @branchId)
  AND
    (
      c.PhoneNo2 = @term
      OR c.PhoneNo = @term
      OR c.CardNo = @term
      OR c.CardId = @term
      OR c.Tet_NumPoket = @term
    )
  AND NOT EXISTS
    (
      SELECT 1
      FROM dbo.Transactions t
      WHERE t.Transaction_Type = 21
        AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL
        AND
          (
            t.VisaNumber = c.CardNo
            OR t.VisaNumber = c.CardId
          )
    )
ORDER BY c.Id DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@term", SqlDbType.NVarChar, 255).Value = term.Trim();
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(MapKeshniCardCustomer(reader));
                    }
                }
            }

            return customers;
        }

        public PosCustomerLookupDto GetKeshniCardCustomerById(int id, int? branchId, bool canChangeDefaults)
        {
            const string sql = @"
SELECT TOP (1)
    c.Id,
    c.name,
    c.namee,
    c.ArabicName0,
    c.ArabicName1,
    c.ArabicName2,
    c.ArabicName3,
    c.EnglishName0,
    c.EnglishName1,
    c.EnglishName2,
    c.EnglishName3,
    c.EnglishName5,
    c.EnglishName6,
    c.EnglishName7,
    COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N'')) AS CustomerName,
    COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N'')) AS Phone,
    c.PhoneNo2,
    c.CardId,
    c.CardNo,
    c.card,
    c.CardSource,
    c.tel,
    c.Tet_NumPoket,
    c.Address,
    c.MailAdress,
    c.Nationality,
    c.BirthDate,
    c.CardDate,
    c.CardEndDate,
    c.OrderDate,
    c.EasyCashType,
    c.BranchID,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)) AS BranchName
FROM dbo.TblCusCsh c
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
WHERE c.Id = @id
  AND ISNULL(c.EasyCashType, 0) = 0
  AND (@branchId IS NULL OR @canChangeDefaults = 1 OR c.BranchID = @branchId);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapKeshniCardCustomer(reader) : null;
                }
            }
        }

        public bool KeshniCardDuplicateExists(string columnName, string value, int? excludeId)
        {
            return FindKeshniCardDuplicateId(columnName, value, excludeId).HasValue;
        }

        public int? FindKeshniCardDuplicateId(string columnName, string value, int? excludeId)
        {
            return FindKeshniCardDuplicateId(columnName, value, excludeId, null);
        }

        public int? FindKeshniCardDuplicateId(string columnName, string value, int? excludeId, int? cardLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Tet_NumPoket",
                "PhoneNo2",
                "CardNo"
            };

            if (!allowedColumns.Contains(columnName))
            {
                throw new ArgumentException("Unsupported Keshni Card duplicate column.", "columnName");
            }

            var sql = @"
SELECT TOP (1) Id
FROM dbo.TblCusCsh
WHERE " + columnName + @" = @value
  AND ISNULL(EasyCashType, 0) = 0
  AND (@cardLength IS NULL OR LEN(LTRIM(RTRIM(ISNULL(CardNo, N'')))) = @cardLength)
  AND (@excludeId IS NULL OR Id <> @excludeId);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                AddString(command, "@value", SqlDbType.NVarChar, 255, value.Trim());
                Add(command, "@excludeId", SqlDbType.Int, excludeId);
                Add(command, "@cardLength", SqlDbType.Int, cardLength);
                connection.Open();
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value
                    ? (int?)null
                    : Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
        }

        public bool KeshniCardSerialExistsInIssuedCards(string cardNo)
        {
            if (string.IsNullOrWhiteSpace(cardNo))
            {
                return false;
            }

            const string sql = @"
SELECT TOP (1) 1
FROM dbo.Transaction_Details TD
INNER JOIN dbo.Transactions T
    ON TD.Transaction_ID = T.Transaction_ID
WHERE T.Transaction_Type = 20
  AND TD.ItemSerial = @cardNo;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                AddString(command, "@cardNo", SqlDbType.NVarChar, 255, cardNo.Trim());
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public PosCustomerLookupDto SaveCashCustomer(PosCashCustomerSaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.CustomerID.HasValue && request.CustomerID.Value > 0)
            {
                const string updateSql = @"
UPDATE dbo.TblCusCsh
SET
    name = @name,
    namee = @namee,
    ArabicName0 = @arabicName0,
    ArabicName1 = @arabicName1,
    ArabicName2 = @arabicName2,
    ArabicName3 = @arabicName3,
    EnglishName0 = @englishName0,
    EnglishName1 = @englishName1,
    EnglishName2 = @englishName2,
    EnglishName3 = @englishName3,
    EnglishName5 = @englishName5,
    EnglishName6 = @englishName6,
    EnglishName7 = @englishName7,
    PhoneNo2 = @phoneNo2,
    PhoneNo = @phoneNo,
    CardNo = @cardNo,
    CardID = @cardId,
    CardSource = @cardSource,
    Tet_NumPoket = @tetNumPoket,
    Address = @address,
    MailAdress = @mailAdress,
    Nationality = @nationality,
    BirthDate = @birthDate,
    CardDate = @cardDate,
    CardEndDate = @cardEndDate,
    OrderDate = @orderDate,
    EasyCashType = 0,
    tel = @tel,
    card = @card,
    BranchID = @branchId,
    UserID = @userId,
    Emp_id = @empId,
    EditDate = GETDATE()
WHERE Id = @id;";

                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand(updateSql, connection))
                {
                    AddCashCustomerParameters(command, request);
                    command.Parameters.Add("@id", SqlDbType.Int).Value = request.CustomerID.Value;
                    connection.Open();
                    var affected = command.ExecuteNonQuery();
                    if (affected <= 0)
                    {
                        throw new InvalidOperationException("لم يتم العثور على سجل KYC المطلوب تحديثه. أعد البحث عن العميل ثم حاول مرة أخرى.");
                    }
                }

                var updated = GetKeshniCardCustomerById(request.CustomerID.Value, request.BranchId, true);
                if (updated == null)
                {
                    throw new InvalidOperationException("تم تحديث بيانات KYC لكن تعذر تحميل نفس العميل بعد الحفظ.");
                }

                return updated;
            }

            const string insertSql = @"
DECLARE @id INT;

SELECT @id = ISNULL(MAX(Id), 0) + 1
FROM dbo.TblCusCsh WITH (TABLOCKX, HOLDLOCK);

INSERT INTO dbo.TblCusCsh
(
    Id,
    name,
    namee,
    ArabicName0,
    ArabicName1,
    ArabicName2,
    ArabicName3,
    EnglishName0,
    EnglishName1,
    EnglishName2,
    EnglishName3,
    EnglishName5,
    EnglishName6,
    EnglishName7,
    PhoneNo2,
    PhoneNo,
    CardNo,
    CardID,
    CardSource,
    Tet_NumPoket,
    Address,
    MailAdress,
    Nationality,
    BirthDate,
    CardDate,
    CardEndDate,
    OrderDate,
    EasyCashType,
    tel,
    card,
    BranchID,
    UserID,
    Emp_id,
    SaveDate
)
VALUES
(
    @id,
    @name,
    @namee,
    @arabicName0,
    @arabicName1,
    @arabicName2,
    @arabicName3,
    @englishName0,
    @englishName1,
    @englishName2,
    @englishName3,
    @englishName5,
    @englishName6,
    @englishName7,
    @phoneNo2,
    @phoneNo,
    @cardNo,
    @cardId,
    @cardSource,
    @tetNumPoket,
    @address,
    @mailAdress,
    @nationality,
    @birthDate,
    @cardDate,
    @cardEndDate,
    @orderDate,
    0,
    @tel,
    @card,
    @branchId,
    @userId,
    @empId,
    GETDATE()
);

SELECT @id AS Id;";

            int newId;
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(insertSql, connection))
            {
                AddCashCustomerParameters(command, request);
                connection.Open();
                newId = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            request.CustomerID = newId;
            var inserted = GetKeshniCardCustomerById(newId, request.BranchId, true);
            if (inserted == null)
            {
                throw new InvalidOperationException("تم حفظ بيانات KYC لكن تعذر تحميل نفس العميل الجديد برقم السجل.");
            }

            return inserted;
        }

        public void SaveKeshniCardAttachment(string subjectNo, string fileName, string title)
        {
            const string sql = @"
INSERT INTO dbo.Subject_doc
(
    subject_no,
    image_NAME,
    image_date,
    DEPARTEMENT,
    image_Title,
    operation_type,
    IsDeleted
)
VALUES
(
    @subjectNo,
    @fileName,
    CONVERT(DATE, GETDATE()),
    NULL,
    @title,
    N'0701201991',
    0
);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                AddString(command, "@subjectNo", SqlDbType.NVarChar, 4000, subjectNo);
                AddString(command, "@fileName", SqlDbType.NVarChar, 4000, fileName);
                AddString(command, "@title", SqlDbType.NVarChar, 4000, title);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public IList<PosKycAttachmentDto> GetKeshniCardAttachments(string subjectNo)
        {
            var attachments = new List<PosKycAttachmentDto>();
            if (string.IsNullOrWhiteSpace(subjectNo))
            {
                return attachments;
            }

            const string sql = @"
SELECT
    COALESCE(operation_no, image_no, 0) AS Id,
    subject_no,
    image_NAME,
    image_date,
    DEPARTEMENT,
    image_Title,
    operation_type
FROM dbo.Subject_doc
WHERE operation_type = N'0701201991'
  AND ISNULL(IsDeleted, 0) = 0
  AND (subject_no = @subjectNo OR LTRIM(RTRIM(subject_no)) = LTRIM(RTRIM(@subjectNo)))
ORDER BY image_date DESC, COALESCE(operation_no, image_no, 0) DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                AddString(command, "@subjectNo", SqlDbType.NVarChar, 4000, subjectNo);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        attachments.Add(MapKeshniAttachment(reader));
                    }
                }
            }

            return attachments;
        }

        public PosKycAttachmentDto GetKeshniCardAttachmentById(int id, int? branchId, bool canChangeDefaults)
        {
            const string sql = @"
SELECT TOP (1)
    COALESCE(d.operation_no, d.image_no, 0) AS Id,
    d.subject_no,
    d.image_NAME,
    d.image_date,
    d.DEPARTEMENT,
    d.image_Title,
    d.operation_type
FROM dbo.Subject_doc d
WHERE d.operation_type = N'0701201991'
  AND ISNULL(d.IsDeleted, 0) = 0
  AND (d.operation_no = @id OR d.image_no = @id)
  AND
  (
      @canChangeDefaults = 1
      OR @branchId IS NULL
      OR EXISTS
      (
          SELECT 1
          FROM dbo.TblCusCsh c
          LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
          WHERE ISNULL(c.EasyCashType, 0) = 0
            AND c.BranchID = @branchId
            AND LTRIM(RTRIM(d.subject_no)) = LTRIM(RTRIM(
                N' كيشني '
                + ISNULL(COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)), N'')
                + N' ' + ISNULL(c.ArabicName0, N'')
                + N' ' + ISNULL(c.ArabicName1, N'')
                + N' ' + ISNULL(c.Tet_NumPoket, N'')
            ))
      )
  );";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                Add(command, "@branchId", SqlDbType.Int, branchId);
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapKeshniAttachment(reader) : null;
                }
            }
        }

        public static string BuildKeshniAttachmentSubject(string branchName, string arabicName0, string arabicName1, string nationalId)
        {
            // VB6 FrmCustCash Cmd(11): " كيشني " & DCboBranch.Text & " " & txtArabicName(0) & " " & txtArabicName(1) & " " & txtTet_NumPoket
            return " كيشني "
                + (branchName ?? string.Empty)
                + " " + (arabicName0 ?? string.Empty)
                + " " + (arabicName1 ?? string.Empty)
                + " " + (nationalId ?? string.Empty);
        }

        public IList<PosCashBoxDto> GetCashBoxes()
        {
            var cashBoxes = new List<PosCashBoxDto>();

            const string sql = @"
SELECT TOP (100)
    BoxID,
    COALESCE(NULLIF(BoxName, N''), NULLIF(BoxNameE, N''), CONVERT(NVARCHAR(50), BoxID)) AS BoxName,
    BranchId,
    ISNULL(IsWallet, 0) AS IsWallet,
    ISNULL(IsTerminalPOS, 0) AS IsTerminalPOS
FROM dbo.TblBoxesData
ORDER BY BoxID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cashBoxes.Add(new PosCashBoxDto
                        {
                            BoxID = ReadInt(reader, "BoxID").GetValueOrDefault(),
                            BoxName = ReadString(reader, "BoxName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            IsWallet = ReadBoolean(reader, "IsWallet"),
                            IsTerminalPOS = ReadBoolean(reader, "IsTerminalPOS")
                        });
                    }
                }
            }

            return cashBoxes;
        }

        public PosEmployeeBalancesDto GetEmployeeBalances(int userId, int? boxId)
        {
            const string sql = @"
DECLARE @EmpID INT;
DECLARE @EmployeeAccount NVARCHAR(255);
DECLARE @BoxAccount NVARCHAR(255);

SELECT TOP (1)
    @EmpID = u.Empid
FROM dbo.TblUsers AS u WITH (NOLOCK)
WHERE u.UserID = @userId;

SELECT TOP (1)
    @EmployeeAccount = e.Account_code
FROM dbo.TblEmployee AS e WITH (NOLOCK)
WHERE e.Emp_ID = @EmpID;

SELECT TOP (1)
    @BoxAccount = b.Account_Code
FROM dbo.TblBoxesData AS b WITH (NOLOCK)
WHERE b.empid = @EmpID
  AND NULLIF(LTRIM(RTRIM(ISNULL(b.Account_Code, N''))), N'') IS NOT NULL
ORDER BY CASE WHEN @boxId IS NOT NULL AND b.BoxID = @boxId THEN 0 ELSE 1 END, b.BoxID;

IF @BoxAccount IS NULL AND @boxId IS NOT NULL
BEGIN
    SELECT TOP (1)
        @BoxAccount = b.Account_Code
    FROM dbo.TblBoxesData AS b WITH (NOLOCK)
    WHERE b.BoxID = @boxId;
END;

SELECT
    @EmployeeAccount AS EmployeeAccountCode,
    @BoxAccount AS BoxAccountCode,
    CAST(ISNULL((
        SELECT SUM(CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN ISNULL(d.Value, 0) ELSE -ISNULL(d.Value, 0) END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d WITH (NOLOCK)
        WHERE d.Account_Code = @EmployeeAccount
    ), 0) AS DECIMAL(18, 2)) AS EmployeeBalance,
    CAST(ISNULL((
        SELECT SUM(CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN ISNULL(d.Value, 0) ELSE -ISNULL(d.Value, 0) END)
        FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d WITH (NOLOCK)
        WHERE d.Account_Code = @BoxAccount
    ), 0) AS DECIMAL(18, 2)) AS BoxBalance;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@boxId", SqlDbType.Int).Value = boxId.HasValue ? (object)boxId.Value : DBNull.Value;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new PosEmployeeBalancesDto
                        {
                            EmployeeBalanceText = "غير محدد",
                            BoxBalanceText = "غير محدد"
                        };
                    }

                    var employeeBalance = ReadDecimal(reader, "EmployeeBalance").GetValueOrDefault();
                    var boxBalance = ReadDecimal(reader, "BoxBalance").GetValueOrDefault();
                    var employeeAccount = ReadString(reader, "EmployeeAccountCode");
                    var boxAccount = ReadString(reader, "BoxAccountCode");

                    return new PosEmployeeBalancesDto
                    {
                        EmployeeAccountCode = employeeAccount,
                        BoxAccountCode = boxAccount,
                        EmployeeBalance = employeeBalance,
                        BoxBalance = boxBalance,
                        EmployeeBalanceText = string.IsNullOrWhiteSpace(employeeAccount) ? "غير محدد" : FormatBalance(employeeBalance),
                        BoxBalanceText = string.IsNullOrWhiteSpace(boxAccount) ? "غير محدد" : FormatBalance(boxBalance)
                    };
                }
            }
        }

        public PosSaveTransactionResult SaveTransaction(PosSaveTransactionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                throw new InvalidOperationException("At least one POS item is required.");
            }

            // Kishny POS uses fixed cash customer CusID=2 for Transactions.CusID; KYC customer remains in TblCusCsh fields.
            if (!request.CustomerID.HasValue || request.CustomerID.Value <= 0)
            {
                request.CustomerID = 2;
            }

            if (!request.DefaultCustomerId.HasValue || request.DefaultCustomerId.Value <= 0)
            {
                request.DefaultCustomerId = 2;
            }

            ApplyServerCalculatedCommission(request);

            if (IsViolationsService(request.TransactionType))
            {
                var violationItem = GetFirstSelectedItem(request);
                request.TrafficViolations = true;
                request.ItemIDService = violationItem != null ? violationItem.Item_ID : request.ItemIDService;
                if (violationItem != null)
                {
                    violationItem.SavedItemType = 1;
                }
                request.RechargeValue = null;
                request.IsRecharg = false;
                request.IsCashOut = false;
                request.IsWallet = false;
                request.PayType = request.ViolationPayType.HasValue ? request.ViolationPayType.Value : 1;
            }

            var itemsTable = BuildItemsTable(request.Items);
            var paymentsTable = BuildPaymentsTable(request);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_SaveTransaction", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 60;

                Add(command, "@TransactionDate", SqlDbType.SmallDateTime, ParseTransactionDate(request.TransactionDate));
                Add(command, "@BranchId", SqlDbType.Int, request.BranchId);
                Add(command, "@StoreID", SqlDbType.Int, request.StoreID);
                Add(command, "@UserID", SqlDbType.Int, request.UserID);
                Add(command, "@Emp_ID", SqlDbType.Int, request.Emp_ID);
                Add(command, "@CustomerID", SqlDbType.Int, request.CustomerID);
                Add(command, "@PaymentType", SqlDbType.Int, request.PaymentType);
                Add(command, "@BoxID", SqlDbType.Int, request.BoxID);
                Add(command, "@PayedValue", SqlDbType.Money, request.PayedValue);
                Add(command, "@NetValue", SqlDbType.Money, request.NetValue);
                Add(command, "@RemainValue", SqlDbType.Money, request.RemainValue);
                Add(command, "@PaymentNetid", SqlDbType.Int, request.PaymentNetid);
                Add(command, "@IsCashOut", SqlDbType.Bit, request.IsCashOut);
                Add(command, "@IsPOS", SqlDbType.Bit, request.IsPOS);
                Add(command, "@OtherItems", SqlDbType.Bit, request.OtherItems);
                Add(command, "@PayType", SqlDbType.Int, request.PayType);
                Add(command, "@POSBillType", SqlDbType.Int, request.POSBillType);
                Add(command, "@STableID", SqlDbType.Int, request.STableID);
                Add(command, "@SessionD", SqlDbType.Int, request.SessionD);
                Add(command, "@BillBasedOn", SqlDbType.Int, request.BillBasedOn);
                AddString(command, "@CashCustomerName", SqlDbType.NVarChar, 255, request.CashCustomerName);
                AddString(command, "@CashCustomerPhone", SqlDbType.NVarChar, 255, request.CashCustomerPhone);
                AddString(command, "@Phone2", SqlDbType.NVarChar, 255, request.Phone2);
                AddString(command, "@IPN", SqlDbType.NVarChar, 255, request.IPN);
                AddString(command, "@ManualNO", SqlDbType.NVarChar, 255, request.ManualNO);
                AddString(command, "@NoID", SqlDbType.VarChar, 255, request.NoID);
                AddString(command, "@ManualNo2", SqlDbType.NVarChar, 1000, request.ManualNo2);
                AddString(command, "@VisaNumber", SqlDbType.NVarChar, 255, request.VisaNumber);
                Add(command, "@RechargeValue", SqlDbType.Float, ToNullableDouble(request.RechargeValue));
                Add(command, "@Tet_NumPoket", SqlDbType.Float, ToNullableDouble(request.Tet_NumPoket));
                Add(command, "@TrafficViolations", SqlDbType.Bit, request.TrafficViolations);
                Add(command, "@ViolationsValue", SqlDbType.Float, ToNullableDouble(request.ViolationsValue));
                Add(command, "@ItemIDService", SqlDbType.Int, request.ItemIDService);
                Add(command, "@ItemIDService2", SqlDbType.Int, request.ItemIDService2);
                Add(command, "@isRecharg", SqlDbType.Bit, request.IsRecharg);
                Add(command, "@IsWallet", SqlDbType.Bit, request.IsWallet);
                Add(command, "@HaveGuarantee", SqlDbType.Bit, request.HaveGuarantee);
                AddString(command, "@CardSerial", SqlDbType.NVarChar, 255, request.CardSerial);
                Add(command, "@ExistingTransactionID", SqlDbType.Int, request.Transaction_ID);

                // VB6 passes DCPreFix.Text; the web service type is not a voucher prefix.
                AddString(command, "@Prefix", SqlDbType.VarChar, 20, null);

                var itemsParameter = command.Parameters.Add("@Items", SqlDbType.Structured);
                itemsParameter.TypeName = "dbo.POS_TransactionItems";
                itemsParameter.Value = itemsTable;

                var paymentsParameter = command.Parameters.Add("@SalesPayments", SqlDbType.Structured);
                paymentsParameter.TypeName = "dbo.POS_SalesPayments";
                paymentsParameter.Value = paymentsTable;

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("usp_POS_SaveTransaction did not return a result row.");
                    }

                    return new PosSaveTransactionResult
                    {
                        Transaction_ID = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                        NoteSerial1 = ReadString(reader, "NoteSerial1")
                    };
                }
            }
        }

        private static DataTable BuildItemsTable(IEnumerable<PosTransactionItemDto> items)
        {
            var table = new DataTable();
            table.Columns.Add("Item_ID", typeof(int));
            table.Columns.Add("Quantity", typeof(double));
            table.Columns.Add("Price", typeof(double));
            table.Columns.Add("UnitId", typeof(int));
            table.Columns.Add("ShowQty", typeof(double));
            table.Columns.Add("QtyBySmalltUnit", typeof(double));
            table.Columns.Add("showPrice", typeof(double));
            table.Columns.Add("TotalPrice", typeof(double));
            table.Columns.Add("StoreID2", typeof(int));
            table.Columns.Add("Vat", typeof(double));
            table.Columns.Add("Vatyo", typeof(double));
            table.Columns.Add("discountvalue", typeof(decimal));
            table.Columns.Add("TotalDiscountPerLine", typeof(decimal));
            table.Columns.Add("ItemCase", typeof(int));
            table.Columns.Add("CostPrice", typeof(decimal));
            table.Columns.Add("SavedItemType", typeof(int));

            foreach (var item in items)
            {
                if (!item.Item_ID.HasValue)
                {
                    continue;
                }

                table.Rows.Add(
                    item.Item_ID.Value,
                    ToDouble(item.Quantity),
                    ToDouble(item.Price),
                    item.UnitId.GetValueOrDefault(1),
                    ToDouble(item.ShowQty.GetValueOrDefault(item.Quantity)),
                    ToDouble(item.QtyBySmalltUnit.GetValueOrDefault(1)),
                    ToDouble(item.ShowPrice.GetValueOrDefault(item.Price)),
                    ToDouble(item.TotalPrice),
                    DbValue(item.StoreID2),
                    ToDouble(item.Vat.GetValueOrDefault()),
                    ToDouble(item.Vatyo.GetValueOrDefault()),
                    item.DiscountValue.GetValueOrDefault(),
                    item.TotalDiscountPerLine.GetValueOrDefault(),
                    item.ItemCase.GetValueOrDefault(1),
                    item.CostPrice.GetValueOrDefault(),
                    item.SavedItemType.GetValueOrDefault());
            }

            if (table.Rows.Count == 0)
            {
                throw new InvalidOperationException("No selected CASH item rows were provided.");
            }

            return table;
        }

        private static DataTable BuildPaymentsTable(PosSaveTransactionRequest request)
        {
            var table = new DataTable();
            table.Columns.Add("PaymentID", typeof(int));
            table.Columns.Add("Value", typeof(double));
            table.Columns.Add("CardNo", typeof(string));
            table.Columns.Add("MaxValue", typeof(double));

            if (request.SalesPayments != null)
            {
                foreach (var payment in request.SalesPayments)
                {
                    if (!payment.PaymentID.HasValue || payment.PaymentID.Value <= 0)
                    {
                        continue;
                    }

                    table.Rows.Add(
                        payment.PaymentID.Value,
                        ToDouble(payment.Value),
                        (object)payment.CardNo ?? DBNull.Value,
                        ToDouble(payment.MaxValue.GetValueOrDefault(payment.Value)));
                }
            }

            if (table.Rows.Count == 0 && request.PaymentType > 0)
            {
                table.Rows.Add(
                    request.PaymentType,
                    ToDouble(request.PayedValue),
                    (object)(request.VisaNumber ?? request.CardSerial) ?? DBNull.Value,
                    ToDouble(request.PayedValue));
            }

            return table;
        }

        private void ApplyServerCalculatedCommission(PosSaveTransactionRequest request)
        {
            var firstItem = GetFirstSelectedItem(request);
            if (firstItem == null || !firstItem.Item_ID.HasValue)
            {
                return;
            }

            var commission = CalculateCommission(new PosCommissionRequest
            {
                ServiceType = request.TransactionType,
                ItemID = firstItem.Item_ID,
                BranchId = request.BranchId,
                RechargeValue = IsCardService(request.TransactionType) ? 0m : (IsViolationsService(request.TransactionType) ? request.ViolationsValue.GetValueOrDefault() : request.RechargeValue.GetValueOrDefault()),
                Vatyo = firstItem.Vatyo,
                IsWallet = request.IsWallet,
                HaveGuarantee = request.HaveGuarantee
            });

            request.CommissionValue = commission.CommissionValue;
            request.VatValue = commission.VatValue;
            request.TotalFees = commission.TotalFees;

            firstItem.Quantity = firstItem.Quantity <= 0 ? 1 : firstItem.Quantity;
            firstItem.ShowQty = firstItem.Quantity;
            firstItem.Price = commission.ServiceFee;
            firstItem.ShowPrice = commission.ServiceFee;
            firstItem.Vat = commission.VatValue;
            firstItem.Vatyo = commission.VatPercent;
            firstItem.TotalPrice = commission.TotalFees;

            if (IsCardService(request.TransactionType))
            {
                request.RechargeValue = 0;
                request.NetValue = commission.TotalFees;
                request.PayedValue = commission.TotalFees;
                request.RemainValue = 0;
                request.ManualNo2 = ToArabicAmountText(commission.TotalFees);
                request.IsRecharg = false;
                request.IsCashOut = false;
                request.IsWallet = false;
                request.ItemIDService = firstItem.Item_ID;
            }
            else
            {
                request.NetValue = commission.ServiceFee;
                request.PayedValue = commission.ServiceFee;
                request.RemainValue = 0;
                request.ManualNo2 = ToArabicAmountText(commission.TotalValue);
            }

            if (request.SalesPayments != null && request.SalesPayments.Count > 0)
            {
                request.SalesPayments[0].Value = request.PayedValue;
                request.SalesPayments[0].MaxValue = request.PayedValue;
            }
        }

        private static string ToArabicAmountText(decimal amount)
        {
            var pounds = (long)Math.Floor(amount);
            var piasters = (int)Math.Round((amount - pounds) * 100m, 0, MidpointRounding.AwayFromZero);
            if (piasters == 100)
            {
                pounds += 1;
                piasters = 0;
            }

            var text = "فقط " + ArabicNumberToWords(pounds) + " جنيه مصري";
            if (piasters > 0)
            {
                text += " و " + ArabicNumberToWords(piasters) + " قرش";
            }

            return text + " لا غير";
        }

        private static string ArabicNumberToWords(long value)
        {
            if (value == 0)
            {
                return "صفر";
            }

            var parts = new List<string>();
            AppendScale(parts, value / 1000000, "مليون", "مليونان", "ملايين");
            value %= 1000000;
            AppendScale(parts, value / 1000, "ألف", "ألفان", "آلاف");
            value %= 1000;
            if (value > 0)
            {
                parts.Add(ArabicUnderThousand((int)value));
            }

            return string.Join(" و ", parts);
        }

        private static void AppendScale(ICollection<string> parts, long count, string singular, string dual, string plural)
        {
            if (count <= 0)
            {
                return;
            }

            if (count == 1)
            {
                parts.Add(singular);
            }
            else if (count == 2)
            {
                parts.Add(dual);
            }
            else if (count <= 10)
            {
                parts.Add(ArabicUnderThousand((int)count) + " " + plural);
            }
            else
            {
                parts.Add(ArabicUnderThousand((int)count) + " " + singular);
            }
        }

        private static string ArabicUnderThousand(int value)
        {
            var ones = new[] { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
            var tens = new[] { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            var hundreds = new[] { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

            var result = new List<string>();
            if (value >= 100)
            {
                result.Add(hundreds[value / 100]);
                value %= 100;
            }

            if (value > 0)
            {
                if (value < 20)
                {
                    result.Add(ones[value]);
                }
                else
                {
                    var unit = value % 10;
                    var ten = value / 10;
                    result.Add(unit > 0 ? ones[unit] + " و " + tens[ten] : tens[ten]);
                }
            }

            return string.Join(" و ", result);
        }

        private static PosTransactionItemDto GetFirstSelectedItem(PosSaveTransactionRequest request)
        {
            if (request.Items == null)
            {
                return null;
            }

            foreach (var item in request.Items)
            {
                if (item != null && item.Item_ID.HasValue)
                {
                    return item;
                }
            }

            return null;
        }

        private PosItemCommissionDto GetItemCommissionDetails(int itemId)
        {
            const string sql = @"
SELECT TOP (1)
    ItemID,
    PercentVisa,
    MinVisa,
    MaxVisa,
    PercentVisaPur,
    MinVisaPur,
    MaxVisaPur
FROM dbo.TblItems
WHERE ItemID = @itemId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new PosItemCommissionDto
                    {
                        ItemID = ReadInt(reader, "ItemID").GetValueOrDefault(),
                        PercentVisa = ReadDecimal(reader, "PercentVisa"),
                        MinVisa = ReadDecimal(reader, "MinVisa"),
                        MaxVisa = ReadDecimal(reader, "MaxVisa"),
                        PercentVisaPur = ReadDecimal(reader, "PercentVisaPur"),
                        MinVisaPur = ReadDecimal(reader, "MinVisaPur"),
                        MaxVisaPur = ReadDecimal(reader, "MaxVisaPur")
                    };
                }
            }
        }

        private decimal? GetPriceRangeSalesCommission(decimal rechargeValue)
        {
            const string sql = "SELECT dbo.CheckPriceRangeSales2(@upperRange, @lowerRange) AS CommissionValue;";
            var roundedValue = Convert.ToInt32(Math.Round(rechargeValue, 0, MidpointRounding.AwayFromZero), CultureInfo.InvariantCulture);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@upperRange", SqlDbType.Int).Value = roundedValue;
                command.Parameters.Add("@lowerRange", SqlDbType.Int).Value = roundedValue;
                connection.Open();
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (decimal?)null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
        }

        private PosItemPriceRangeCommissionDto GetItemPriceRangeSalesCommission(decimal rechargeValue, int itemId)
        {
            const string sql = @"
SELECT TOP (1)
    Price,
    CashBack,
    Cost
FROM dbo.CheckPriceRangeSales3(@upperRange, @lowerRange, @itemId);";
            var roundedValue = Convert.ToInt32(Math.Round(rechargeValue, 0, MidpointRounding.AwayFromZero), CultureInfo.InvariantCulture);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@upperRange", SqlDbType.Int).Value = roundedValue;
                command.Parameters.Add("@lowerRange", SqlDbType.Int).Value = roundedValue;
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var price = ReadDecimal(reader, "Price").GetValueOrDefault();
                    return new PosItemPriceRangeCommissionDto
                    {
                        Price = price > 0 && price < 1 ? rechargeValue * price : price,
                        CashBack = ReadDecimal(reader, "CashBack").GetValueOrDefault(),
                        Cost = ReadDecimal(reader, "Cost").GetValueOrDefault()
                    };
                }
            }
        }

        private decimal GetViolationServiceFee(int itemId)
        {
            const string sql = @"
SELECT TOP (1)
    CAST(COALESCE(iu.UnitSalesPrice, i.SallingPrice, 0) AS DECIMAL(18, 4)) AS ServiceFee
FROM dbo.TblItems AS i
LEFT JOIN dbo.TblItemsUnits AS iu ON iu.ItemID = i.ItemID
WHERE i.ItemID = @itemId
  AND ISNULL(i.TrafficViolations, 0) = 1
ORDER BY CASE WHEN ISNULL(iu.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END, iu.JunckID;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                connection.Open();
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
        }

        private decimal ResolveVatPercent(int? itemId, decimal? postedVatPercent)
        {
            if (postedVatPercent.HasValue && postedVatPercent.Value > 0)
            {
                return postedVatPercent.Value;
            }

            if (!itemId.HasValue)
            {
                return 14m;
            }

            const string sql = @"
SELECT TOP (1)
    Vatyo
FROM dbo.Transaction_Details
WHERE Item_ID = @itemId
  AND ISNULL(Vatyo, 0) > 0
ORDER BY Transaction_ID DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId.Value;
                connection.Open();
                var value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return 14m;
                }

                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
        }

        private static PosCommissionResult BuildCommissionResult(int? itemId, decimal rechargeValue, decimal commissionValue, decimal vatPercent, string source, decimal percent, decimal min, decimal max, bool includeRechargeInTotal = true, decimal bankMachineCommission = 0m)
        {
            var serviceFee = decimal.Round(commissionValue, 2, MidpointRounding.AwayFromZero);
            var vatValue = decimal.Round(serviceFee * vatPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var totalFees = serviceFee + vatValue;
            var totalValue = includeRechargeInTotal ? rechargeValue + totalFees : totalFees;
            bankMachineCommission = decimal.Round(bankMachineCommission, 2, MidpointRounding.AwayFromZero);
            var machineWithdrawalAmount = bankMachineCommission > 0m
                ? decimal.Round(totalValue - bankMachineCommission, 2, MidpointRounding.AwayFromZero)
                : 0m;

            return new PosCommissionResult
            {
                ItemID = itemId,
                RechargeValue = rechargeValue,
                CommissionValue = serviceFee,
                ServiceFee = serviceFee,
                VatValue = vatValue,
                VatPercent = vatPercent,
                TotalFees = totalFees,
                TotalValue = decimal.Round(totalValue, 2, MidpointRounding.AwayFromZero),
                BankMachineCommission = bankMachineCommission,
                CashOutMachineWithdrawalAmount = machineWithdrawalAmount < 0m ? 0m : machineWithdrawalAmount,
                Source = source,
                Percent = percent,
                Min = min,
                Max = max
            };
        }

        public IList<PosTodayInvoiceDto> GetTodayInvoices(int userId, int? branchId, bool canChangeDefaults, bool canEditInvoice, string term, string operationType)
        {
            var invoices = new List<PosTodayInvoiceDto>();
            var searchTerm = (term ?? string.Empty).Trim();
            var operation = NormalizeInvoiceOperationType(operationType);
            var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);
            const string sql = @"
SELECT TOP (100)
    t.Transaction_ID,
    t.NoteSerial1,
    CONVERT(VARCHAR(5), t.Transaction_Date, 108) AS TransactionTime,
    t.CashCustomerName,
    t.CashCustomerPhone,
    CAST(ISNULL(t.PayedValue, 0) AS DECIMAL(18, 2)) AS PayedValue,
    CAST(CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN ISNULL(t.PayedValue, 0)
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(t.PayedValue, 0)
        ELSE ISNULL(t.RechargeValue, 0) + ISNULL(t.VAT, 0) + ISNULL(t.NetValue, 0)
    END AS DECIMAL(18, 2)) AS NetValue,
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'مخالفات'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'كارت كيشني'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'كاش أوت'
        ELSE N'كاش إن'
    END AS ServiceType
FROM dbo.Transactions t
WHERE t.Transaction_Type = 21
  AND (
      @allowExtendedSearch = 1
      OR (
          t.Transaction_Date >= CONVERT(DATE, GETDATE())
          AND t.Transaction_Date < DATEADD(DAY, 1, CONVERT(DATE, GETDATE()))
      )
  )
  AND (
      @allowExtendedSearch = 1
      OR (
          (@branchId IS NULL OR t.BranchId = @branchId)
          AND (@canChangeDefaults = 1 OR t.UserID = @userId)
      )
  )
  AND
  (
      @term = N''
      OR CONVERT(NVARCHAR(50), t.Transaction_ID) LIKE N'%' + @term + N'%'
      OR ISNULL(t.NoteSerial1, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.CashCustomerName, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.CashCustomerPhone, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.Phone2, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.VisaNumber, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.IPN, N'') LIKE N'%' + @term + N'%'
      OR ISNULL(t.ManualNO, N'') LIKE N'%' + @term + N'%'
      OR EXISTS
      (
          SELECT 1
          FROM dbo.Transaction_Details d
          WHERE d.Transaction_ID = t.Transaction_ID
            AND ISNULL(d.ItemSerial, N'') LIKE N'%' + @term + N'%'
      )
  )
  AND
  (
      @operationType = N''
      OR (@operationType = N'cash-in'
          AND ISNULL(t.TrafficViolations, 0) = 0
          AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NULL
          AND ISNULL(t.IsCashOut, 0) = 0)
      OR (@operationType = N'cash-out' AND ISNULL(t.IsCashOut, 0) = 1)
      OR (@operationType = N'card' AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL)
      OR (@operationType = N'violations' AND ISNULL(t.TrafficViolations, 0) = 1)
  )
ORDER BY t.Transaction_ID DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                command.Parameters.Add("@allowExtendedSearch", SqlDbType.Bit).Value = hasSearchTerm && (canChangeDefaults || canEditInvoice);
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = searchTerm;
                command.Parameters.Add("@operationType", SqlDbType.NVarChar, 30).Value = operation;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        invoices.Add(new PosTodayInvoiceDto
                        {
                            Transaction_ID = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            TransactionTime = ReadString(reader, "TransactionTime"),
                            CustomerName = ReadString(reader, "CashCustomerName"),
                            CustomerPhone = ReadString(reader, "CashCustomerPhone"),
                            PayedValue = ReadDecimal(reader, "PayedValue").GetValueOrDefault(),
                            NetValue = ReadDecimal(reader, "NetValue").GetValueOrDefault(),
                            ServiceType = ReadString(reader, "ServiceType")
                        });
                    }
                }
            }

            return invoices;
        }

        public void InsertPosSystemErrorLog(PosSystemErrorLogWriteRequest log)
        {
            EnsurePosSystemErrorLogTable();
            const string sql = @"
INSERT INTO dbo.POS_SystemErrorLog
(
    CreatedAt, Severity, Status, UserId, UserName, BranchId, ScreenName, ActionName,
    OperationType, TransactionId, ErrorMessage, StackTrace, RequestSummary, IpAddress, UserAgent
)
VALUES
(
    GETDATE(), @Severity, @Status, @UserId, @UserName, @BranchId, @ScreenName, @ActionName,
    @OperationType, @TransactionId, @ErrorMessage, @StackTrace, @RequestSummary, @IpAddress, @UserAgent
);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                AddString(command, "@Severity", SqlDbType.NVarChar, 20, log == null ? null : log.Severity);
                AddString(command, "@Status", SqlDbType.NVarChar, 40, log == null ? null : log.Status);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = log == null || !log.UserId.HasValue ? (object)DBNull.Value : log.UserId.Value;
                AddString(command, "@UserName", SqlDbType.NVarChar, 100, log == null ? null : log.UserName);
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = log == null || !log.BranchId.HasValue ? (object)DBNull.Value : log.BranchId.Value;
                AddString(command, "@ScreenName", SqlDbType.NVarChar, 100, log == null ? null : log.ScreenName);
                AddString(command, "@ActionName", SqlDbType.NVarChar, 100, log == null ? null : log.ActionName);
                AddString(command, "@OperationType", SqlDbType.NVarChar, 50, log == null ? null : log.OperationType);
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = log == null || !log.TransactionId.HasValue ? (object)DBNull.Value : log.TransactionId.Value;
                AddString(command, "@ErrorMessage", SqlDbType.NVarChar, 2000, log == null ? null : log.ErrorMessage);
                AddString(command, "@StackTrace", SqlDbType.NVarChar, -1, log == null ? null : log.StackTrace);
                AddString(command, "@RequestSummary", SqlDbType.NVarChar, -1, log == null ? null : log.RequestSummary);
                AddString(command, "@IpAddress", SqlDbType.NVarChar, 64, log == null ? null : log.IpAddress);
                AddString(command, "@UserAgent", SqlDbType.NVarChar, 512, log == null ? null : log.UserAgent);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public PosSystemErrorLogSearchResult SearchPosSystemErrorLogs(PosSystemErrorLogSearchRequest request)
        {
            EnsurePosSystemErrorLogTable();
            request = request ?? new PosSystemErrorLogSearchRequest();
            var pageSize = request.PageSize <= 0 || request.PageSize > 500 ? 200 : request.PageSize;
            var items = new List<PosSystemErrorLogEntry>();
            const string sql = @"
SELECT TOP (@pageSize)
    l.Id,
    l.CreatedAt,
    l.Severity,
    l.Status,
    l.UserId,
    l.UserName,
    l.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), l.BranchId)) AS BranchName,
    l.ScreenName,
    l.ActionName,
    l.OperationType,
    l.TransactionId,
    l.ErrorMessage,
    l.RequestSummary,
    l.IpAddress,
    l.UserAgent
FROM dbo.POS_SystemErrorLog l
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = l.BranchId
WHERE (@fromDate IS NULL OR l.CreatedAt >= @fromDate)
  AND (@toDate IS NULL OR l.CreatedAt < DATEADD(DAY, 1, @toDate))
  AND (@userId IS NULL OR l.UserId = @userId)
  AND (@branchId IS NULL OR l.BranchId = @branchId)
  AND (@userKeyword = N'' OR ISNULL(l.UserName, N'') LIKE N'%' + @userKeyword + N'%' OR CONVERT(NVARCHAR(20), ISNULL(l.UserId, 0)) LIKE N'%' + @userKeyword + N'%')
  AND (@screenAction = N'' OR ISNULL(l.ScreenName, N'') LIKE N'%' + @screenAction + N'%' OR ISNULL(l.ActionName, N'') LIKE N'%' + @screenAction + N'%')
  AND (@operationType = N'' OR ISNULL(l.OperationType, N'') = @operationType)
  AND (@severity = N'' OR ISNULL(l.Severity, N'') = @severity)
  AND (@keyword = N'' OR ISNULL(l.ErrorMessage, N'') LIKE N'%' + @keyword + N'%' OR ISNULL(l.RequestSummary, N'') LIKE N'%' + @keyword + N'%')
ORDER BY l.CreatedAt DESC, l.Id DESC;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@pageSize", SqlDbType.Int).Value = pageSize;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = request.FromDate.HasValue ? (object)request.FromDate.Value.Date : DBNull.Value;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = request.ToDate.HasValue ? (object)request.ToDate.Value.Date : DBNull.Value;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = request.UserId.HasValue ? (object)request.UserId.Value : DBNull.Value;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = request.BranchId.HasValue ? (object)request.BranchId.Value : DBNull.Value;
                command.Parameters.Add("@userKeyword", SqlDbType.NVarChar, 100).Value = (request.UserKeyword ?? string.Empty).Trim();
                command.Parameters.Add("@screenAction", SqlDbType.NVarChar, 100).Value = (request.ScreenAction ?? string.Empty).Trim();
                command.Parameters.Add("@operationType", SqlDbType.NVarChar, 50).Value = NormalizeInvoiceOperationType(request.OperationType);
                command.Parameters.Add("@severity", SqlDbType.NVarChar, 20).Value = (request.Severity ?? string.Empty).Trim();
                command.Parameters.Add("@keyword", SqlDbType.NVarChar, 200).Value = (request.Keyword ?? string.Empty).Trim();
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new PosSystemErrorLogEntry
                        {
                            Id = ReadInt(reader, "Id").GetValueOrDefault(),
                            CreatedAt = ReadDateTime(reader, "CreatedAt").GetValueOrDefault(),
                            Severity = ReadString(reader, "Severity"),
                            Status = ReadString(reader, "Status"),
                            UserId = ReadInt(reader, "UserId"),
                            UserName = ReadString(reader, "UserName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            ScreenName = ReadString(reader, "ScreenName"),
                            ActionName = ReadString(reader, "ActionName"),
                            OperationType = ReadString(reader, "OperationType"),
                            TransactionId = ReadInt(reader, "TransactionId"),
                            ErrorMessage = ReadString(reader, "ErrorMessage"),
                            RequestSummary = ReadString(reader, "RequestSummary"),
                            IpAddress = ReadString(reader, "IpAddress"),
                            UserAgent = ReadString(reader, "UserAgent")
                        });
                    }
                }
            }

            return new PosSystemErrorLogSearchResult { Items = items, Count = items.Count };
        }

        public PosTodaySummaryDto GetTodaySummary(int userId, int? branchId, bool canChangeDefaults)
        {
            var summary = new PosTodaySummaryDto
            {
                GeneratedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture)
            };

            const string sql = @"
SELECT
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
        ELSE N'cash-in'
    END AS ServiceType,
    COUNT(1) AS TransactionCount,
    CAST(SUM(CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN ISNULL(t.PayedValue, 0)
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(t.PayedValue, 0)
        ELSE ISNULL(t.RechargeValue, 0) + ISNULL(t.VAT, 0) + ISNULL(t.NetValue, 0)
    END) AS DECIMAL(18, 2)) AS NetValue,
    CAST(SUM(ISNULL(t.PayedValue, 0)) AS DECIMAL(18, 2)) AS PayedValue
FROM dbo.Transactions t
WHERE t.Transaction_Type = 21
  AND t.Transaction_Date >= CONVERT(DATE, GETDATE())
  AND t.Transaction_Date < DATEADD(DAY, 1, CONVERT(DATE, GETDATE()))
  AND (@branchId IS NULL OR t.BranchId = @branchId)
  AND (@canChangeDefaults = 1 OR t.UserID = @userId)
GROUP BY
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
        ELSE N'cash-in'
    END;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        summary.Items.Add(new PosTodaySummaryItemDto
                        {
                            ServiceType = ReadString(reader, "ServiceType"),
                            Count = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                            NetValue = ReadDecimal(reader, "NetValue").GetValueOrDefault(),
                            PayedValue = ReadDecimal(reader, "PayedValue").GetValueOrDefault()
                        });
                    }
                }
            }

            // The seller's own summary remains user/branch scoped above. The ranking card is
            // motivational and must compare the teller against all sellers with movements today.
            summary.SellerRank = GetTodaySellerRank(userId, null);
            return summary;
        }

        private PosSellerRankDto GetTodaySellerRank(int userId, int? branchId)
        {
            const string sql = @"
;WITH SellerTotals AS
(
    SELECT
        t.UserID AS SellerId,
        CAST(SUM(CASE
            WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN ISNULL(t.PayedValue, 0)
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(t.PayedValue, 0)
            ELSE ISNULL(t.RechargeValue, 0) + ISNULL(t.VAT, 0) + ISNULL(t.NetValue, 0)
        END) AS DECIMAL(18, 2)) AS NetValue
    FROM dbo.Transactions t
    WHERE t.Transaction_Type = 21
      AND t.Transaction_Date >= CONVERT(DATE, GETDATE())
      AND t.Transaction_Date < DATEADD(DAY, 1, CONVERT(DATE, GETDATE()))
      AND (@branchId IS NULL OR t.BranchId = @branchId)
      AND t.UserID IS NOT NULL
    GROUP BY t.UserID
),
Ranked AS
(
    SELECT
        SellerId,
        NetValue,
        RANK() OVER (ORDER BY NetValue DESC) AS RankNo,
        COUNT(1) OVER () AS ActiveSellersCount
    FROM SellerTotals
),
CurrentSeller AS
(
    SELECT SellerId, NetValue, RankNo, ActiveSellersCount
    FROM Ranked
    WHERE SellerId = @userId
)
SELECT
    @userId AS SellerId,
    ISNULL(c.NetValue, 0) AS NetValue,
    c.RankNo,
    ISNULL((SELECT MAX(ActiveSellersCount) FROM Ranked), 0) AS ActiveSellersCount,
    CAST(CASE
        WHEN c.RankNo IS NULL OR ISNULL((SELECT MAX(ActiveSellersCount) FROM Ranked), 0) = 0 THEN N'no-activity'
        WHEN c.RankNo = 1 THEN N'top-10'
        WHEN (CAST(c.RankNo AS DECIMAL(18, 4)) / NULLIF(c.ActiveSellersCount, 0)) <= 0.10 THEN N'top-10'
        WHEN (CAST(c.RankNo AS DECIMAL(18, 4)) / NULLIF(c.ActiveSellersCount, 0)) <= 0.25 THEN N'top-25'
        WHEN (CAST(c.RankNo AS DECIMAL(18, 4)) / NULLIF(c.ActiveSellersCount, 0)) <= 0.50 THEN N'top-50'
        ELSE N'needs-improvement'
    END AS NVARCHAR(30)) AS PercentileBucket,
    CAST(CASE
        WHEN c.RankNo IS NULL OR c.RankNo = 1 THEN 0
        ELSE ISNULL((SELECT MIN(r.NetValue) FROM Ranked r WHERE r.NetValue > c.NetValue), c.NetValue) - c.NetValue
    END AS DECIMAL(18, 2)) AS AmountToNextRank
FROM CurrentSeller c
RIGHT JOIN (SELECT 1 AS Anchor) a ON 1 = 1;";

            var rank = new PosSellerRankDto
            {
                SellerId = userId,
                PercentileBucket = "no-activity",
                Message = "ابدأ أول حركة اليوم وسيظهر ترتيبك هنا."
            };

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rank.SellerId = ReadInt(reader, "SellerId").GetValueOrDefault(userId);
                        rank.NetValue = ReadDecimal(reader, "NetValue").GetValueOrDefault();
                        rank.RankNo = ReadInt(reader, "RankNo");
                        rank.ActiveSellersCount = ReadInt(reader, "ActiveSellersCount").GetValueOrDefault();
                        rank.PercentileBucket = ReadString(reader, "PercentileBucket");
                        rank.AmountToNextRank = ReadDecimal(reader, "AmountToNextRank").GetValueOrDefault();
                    }
                }
            }

            ApplySellerRankPresentation(rank);
            return rank;
        }

        private static void ApplySellerRankPresentation(PosSellerRankDto rank)
        {
            if (!rank.RankNo.HasValue || rank.ActiveSellersCount <= 0)
            {
                rank.RankIcon = "•";
                rank.RankBadgeText = "لم تبدأ بعد";
                rank.RankCssClass = "rank-neutral";
                rank.ProgressPercent = 0;
                rank.Message = "ابدأ أول حركة اليوم وسيظهر ترتيبك هنا.";
                rank.MotivationMessage = "ابدأ أول حركة اليوم وسيظهر ترتيبك هنا.";
                return;
            }

            rank.IsLeading = rank.RankNo.Value == 1;
            rank.ProgressPercent = CalculateSellerRankProgress(rank.RankNo.Value, rank.ActiveSellersCount);

            if (rank.IsLeading)
            {
                rank.RankIcon = "🏆";
                rank.RankBadgeText = "متصدر اليوم";
                rank.RankCssClass = "rank-gold";
                rank.Message = "أنت متصدر اليوم 🏆";
                rank.MotivationMessage = "أداء ممتاز، استمر.";
                return;
            }

            rank.Message = "تحتاج " + rank.AmountToNextRank.ToString("0.00") + " جنيه للوصول للمركز السابق.";

            if (rank.RankNo.Value == 2)
            {
                rank.RankIcon = "🥈";
                rank.RankBadgeText = "المركز الثاني";
                rank.RankCssClass = "rank-silver";
                rank.MotivationMessage = "أداء ممتاز، أنت في المركز الثاني 🥈";
                return;
            }

            if (rank.RankNo.Value == 3)
            {
                rank.RankIcon = "🥉";
                rank.RankBadgeText = "المركز الثالث";
                rank.RankCssClass = "rank-bronze";
                rank.MotivationMessage = "أداء قوي، أنت ضمن الثلاثة الأوائل 🥉";
                return;
            }

            if (string.Equals(rank.PercentileBucket, "top-10", StringComparison.OrdinalIgnoreCase))
            {
                rank.RankIcon = "⭐";
                rank.RankBadgeText = "ضمن أفضل 10%";
                rank.RankCssClass = "rank-top";
                rank.MotivationMessage = "أداء مميز، استمر على نفس المستوى ⭐";
                return;
            }

            if (string.Equals(rank.PercentileBucket, "top-25", StringComparison.OrdinalIgnoreCase))
            {
                rank.RankIcon = "🎖️";
                rank.RankBadgeText = "ضمن أفضل 25%";
                rank.RankCssClass = "rank-achievement";
                rank.MotivationMessage = "أداء قوي وقريب من المراكز الأولى 🎖️";
                return;
            }

            rank.RankIcon = "🚀";
            rank.RankBadgeText = "استمر في التقدم";
            rank.RankCssClass = "rank-progress";
            rank.MotivationMessage = "أمامك فرصة لتحسين ترتيبك اليوم 🚀";
        }

        private static decimal CalculateSellerRankProgress(int rankNo, int activeSellersCount)
        {
            if (activeSellersCount <= 0 || rankNo <= 0)
            {
                return 0;
            }

            if (activeSellersCount == 1)
            {
                return 100;
            }

            var progress = ((decimal)(activeSellersCount - rankNo + 1) / activeSellersCount) * 100;
            if (progress < 0)
            {
                return 0;
            }

            return progress > 100 ? 100 : Math.Round(progress, 2);
        }

        public PosInvoiceReviewDto GetInvoiceForReview(int transactionId, int userId, bool canChangeDefaults)
        {
            PosInvoiceReviewDto invoice = null;
            const string headerSql = @"
SELECT TOP (1)
    t.Transaction_ID,
    t.NoteSerial1,
    t.Transaction_Date,
    t.IPN,
    t.ManualNO,
    t.NoID,
    t.CashCustomerPhone,
    t.CashCustomerName,
    t.Phone2,
    t.VisaNumber,
    t.UserID AS CreatedUserId,
    creator.UserName AS CreatedUserName,
    t.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
    b.branch_Code AS BranchCode,
    b.Remarks AS BranchAddress,
    b.Tel AS BranchPhone,
    t.StoreID,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), t.StoreID)) AS StoreName,
    t.BoxID,
    COALESCE(NULLIF(box.BoxName, N''), NULLIF(box.BoxNameE, N''), CONVERT(NVARCHAR(50), t.BoxID)) AS BoxName,
    t.Emp_ID,
    e.Emp_Name AS EmpName,
    CAST(ISNULL(t.RechargeValue, 0) AS DECIMAL(18, 2)) AS RechargeValue,
    CAST(ISNULL(t.PayedValue, 0) AS DECIMAL(18, 2)) AS PayedValue,
    CAST(ISNULL(t.NetValue, 0) AS DECIMAL(18, 2)) AS NetValue,
    CAST(ISNULL(t.RemainValue, 0) AS DECIMAL(18, 2)) AS RemainValue,
    CAST(ISNULL(t.VAT, 0) AS DECIMAL(18, 2)) AS VatValue,
    t.ItemIDService,
    t.ItemIDService2,
    CAST(t.ViolationsValue AS DECIMAL(18, 2)) AS ViolationsValue,
    CASE
        WHEN t.Tet_NumPoket IS NULL THEN NULL
        ELSE CONVERT(NVARCHAR(100), CONVERT(DECIMAL(38, 0), t.Tet_NumPoket))
    END AS Tet_NumPoket,
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
        ELSE N'cash-in'
    END AS TransactionType
FROM dbo.Transactions t
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblStore s ON s.StoreID = t.StoreID
LEFT JOIN dbo.TblBoxesData box ON box.BoxID = t.BoxID
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = t.Emp_ID
LEFT JOIN dbo.TblUsers creator ON creator.UserID = t.UserID
WHERE t.Transaction_ID = @transactionId
  AND t.Transaction_Type = 21
  AND (@canChangeDefaults = 1 OR t.UserID = @userId);";

            const string detailSql = @"
SELECT
    d.Item_ID,
    COALESCE(NULLIF(i.ItemName, N''), NULLIF(i.ItemNamee, N''), i.ItemCode, CONVERT(NVARCHAR(50), d.Item_ID)) AS ItemName,
    d.UnitId,
    CAST(ISNULL(d.Quantity, 0) AS DECIMAL(18, 4)) AS Quantity,
    CAST(ISNULL(d.ShowQty, d.Quantity) AS DECIMAL(18, 4)) AS ShowQty,
    CAST(ISNULL(d.QtyBySmalltUnit, 1) AS DECIMAL(18, 4)) AS QtyBySmalltUnit,
    CAST(ISNULL(d.Price, 0) AS DECIMAL(18, 4)) AS Price,
    CAST(ISNULL(d.showPrice, d.Price) AS DECIMAL(18, 4)) AS ShowPrice,
    CAST(ISNULL(d.TotalPrice, ISNULL(d.Price, 0) + ISNULL(d.Vat, 0)) AS DECIMAL(18, 4)) AS TotalPrice,
    CAST(ISNULL(d.Vat, 0) AS DECIMAL(18, 4)) AS Vat,
    CAST(ISNULL(d.Vatyo, 0) AS DECIMAL(18, 4)) AS Vatyo,
    CAST(ISNULL(d.discountvalue, 0) AS DECIMAL(18, 4)) AS DiscountValue,
    CAST(ISNULL(d.TotalDiscountPerLine, 0) AS DECIMAL(18, 4)) AS TotalDiscountPerLine,
    d.StoreID2,
    d.ItemCase,
    CAST(ISNULL(d.CostPrice, 0) AS DECIMAL(18, 4)) AS CostPrice,
    d.SavedItemType
FROM dbo.Transaction_Details d
LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
WHERE d.Transaction_ID = @transactionId
ORDER BY d.Item_ID;";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(headerSql, connection))
                {
                    command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        invoice = new PosInvoiceReviewDto
                        {
                            Transaction_ID = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            TransactionDate = ReadDateTime(reader, "Transaction_Date"),
                            TransactionType = ReadString(reader, "TransactionType"),
                            IPN = ReadString(reader, "IPN"),
                            ManualNO = ReadString(reader, "ManualNO"),
                            NoID = ReadString(reader, "NoID"),
                            CashCustomerPhone = ReadString(reader, "CashCustomerPhone"),
                            CashCustomerName = ReadString(reader, "CashCustomerName"),
                            Phone2 = ReadString(reader, "Phone2"),
                            VisaNumber = ReadString(reader, "VisaNumber"),
                            CreatedUserId = ReadInt(reader, "CreatedUserId"),
                            CreatedUserName = ReadString(reader, "CreatedUserName"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            BranchCode = ReadString(reader, "BranchCode"),
                            BranchAddress = ReadString(reader, "BranchAddress"),
                            BranchPhone = ReadString(reader, "BranchPhone"),
                            StoreID = ReadInt(reader, "StoreID"),
                            StoreName = ReadString(reader, "StoreName"),
                            BoxID = ReadInt(reader, "BoxID"),
                            BoxName = ReadString(reader, "BoxName"),
                            Emp_ID = ReadInt(reader, "Emp_ID"),
                            EmpName = ReadString(reader, "EmpName"),
                            RechargeValue = ReadDecimal(reader, "RechargeValue").GetValueOrDefault(),
                            PayedValue = ReadDecimal(reader, "PayedValue").GetValueOrDefault(),
                            NetValue = ReadDecimal(reader, "NetValue").GetValueOrDefault(),
                            RemainValue = ReadDecimal(reader, "RemainValue").GetValueOrDefault(),
                            VatValue = ReadDecimal(reader, "VatValue").GetValueOrDefault(),
                            ItemIDService = ReadInt(reader, "ItemIDService"),
                            ItemIDService2 = ReadInt(reader, "ItemIDService2"),
                            ViolationsValue = ReadDecimal(reader, "ViolationsValue"),
                            Tet_NumPoket = ReadString(reader, "Tet_NumPoket")
                        };
                    }
                }

                using (var command = new SqlCommand(detailSql, connection))
                {
                    command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            invoice.Items.Add(new PosTransactionItemDto
                            {
                                Item_ID = ReadInt(reader, "Item_ID"),
                                ItemName = ReadString(reader, "ItemName"),
                                UnitId = ReadInt(reader, "UnitId"),
                                Quantity = ReadDecimal(reader, "Quantity").GetValueOrDefault(),
                                ShowQty = ReadDecimal(reader, "ShowQty"),
                                QtyBySmalltUnit = ReadDecimal(reader, "QtyBySmalltUnit"),
                                Price = ReadDecimal(reader, "Price").GetValueOrDefault(),
                                ShowPrice = ReadDecimal(reader, "ShowPrice"),
                                TotalPrice = ReadDecimal(reader, "TotalPrice").GetValueOrDefault(),
                                Vat = ReadDecimal(reader, "Vat"),
                                Vatyo = ReadDecimal(reader, "Vatyo"),
                                DiscountValue = ReadDecimal(reader, "DiscountValue"),
                                TotalDiscountPerLine = ReadDecimal(reader, "TotalDiscountPerLine"),
                                StoreID2 = ReadInt(reader, "StoreID2"),
                                ItemCase = ReadInt(reader, "ItemCase"),
                                CostPrice = ReadDecimal(reader, "CostPrice"),
                                SavedItemType = ReadInt(reader, "SavedItemType")
                            });
                        }
                    }
                }
            }

            invoice.TotalFees = invoice.Items.Sum(i => i.Price + i.Vat.GetValueOrDefault());
            if (string.Equals(invoice.TransactionType, "card", StringComparison.OrdinalIgnoreCase))
            {
                invoice.KycCustomer = ResolveInvoiceKeshniCustomer(invoice, canChangeDefaults);
            }

            return invoice;
        }

        private PosCustomerLookupDto ResolveInvoiceKeshniCustomer(PosInvoiceReviewDto invoice, bool canChangeDefaults)
        {
            if (invoice == null)
            {
                return null;
            }

            var lookupValues = new[]
            {
                invoice.VisaNumber,
                invoice.CashCustomerPhone,
                invoice.Tet_NumPoket
            };

            foreach (var value in lookupValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var customer = LookupKeshniCardCustomer(value, invoice.BranchId, canChangeDefaults);
                if (customer != null)
                {
                    return customer;
                }
            }

            return null;
        }

        public PosReceiptDto GetReceipt(int transactionId, int userId, bool canChangeDefaults)
        {
            var invoice = GetInvoiceForReview(transactionId, userId, canChangeDefaults);
            if (invoice == null)
            {
                return null;
            }

            return new PosReceiptDto
            {
                CompanyName = "كيشني",
                Invoice = invoice
            };
        }

        public bool TransactionExists(int transactionId)
        {
            const string sql = @"
SELECT TOP (1) 1
FROM dbo.Transactions
WHERE Transaction_ID = @transactionId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public bool ValidatePosUserPassword(int userId, string password)
        {
            const string sql = @"
SELECT TOP (1) 1
FROM dbo.TblUsers
WHERE UserID = @userId
  AND [PassWord] = @password
  AND ISNULL(isDeactivated, 0) = 0;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@password", SqlDbType.NVarChar, 255).Value = password ?? string.Empty;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public DataTable RunPosReport(string reportKey, DateTime fromDate, DateTime toDate, int branchId, int userId, bool canChangeDefaults)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Report_Run", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@reportKey", SqlDbType.NVarChar, 80).Value = reportKey ?? string.Empty;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                adapter.Fill(table);
            }

            return table;
        }

        public DataTable RunPosStoreSerialsReport(int? storeId, string serialSearch, int branchId, int userId, bool canChangeDefaults)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Report_StoreSerials", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@storeId", SqlDbType.Int).Value = storeId.HasValue ? (object)storeId.Value : DBNull.Value;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                command.Parameters.Add("@serialSearch", SqlDbType.NVarChar, 255).Value = (serialSearch ?? string.Empty).Trim();
                adapter.Fill(table);
            }

            return table;
        }

        public DataTable RunPosWebInvoiceAuditReport(DateTime fromDate, DateTime toDate, int branchId, int userId, bool canChangeDefaults)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Report_WebInvoices", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                adapter.Fill(table);
            }

            return table;
        }

        public DataTable RunAccountingReport(string reportKey, DateTime fromDate, DateTime toDate, int branchId, string accountFrom, string accountTo, string accountCodes, int? costCenterId, int userId, bool canChangeDefaults)
        {
            var table = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Accounting_Report", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;
                command.Parameters.Add("@reportKey", SqlDbType.NVarChar, 80).Value = (reportKey ?? string.Empty).Trim();
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                AddString(command, "@accountFrom", SqlDbType.NVarChar, 50, accountFrom);
                AddString(command, "@accountTo", SqlDbType.NVarChar, 50, accountTo);
                AddString(command, "@accountCodes", SqlDbType.NVarChar, -1, accountCodes);
                Add(command, "@costCenterId", SqlDbType.Int, costCenterId);
                adapter.Fill(table);
            }

            return table;
        }

        public IList<PosLookupDto> SearchAccounts(string term)
        {
            var list = new List<PosLookupDto>();
            const string sql = @"
SELECT TOP (50)
    Account_Code AS Id,
    COALESCE(NULLIF(Account_Serial, N'') + N' - ', N'') + Account_Name AS Name,
    Account_Serial AS Extra
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE ISNULL(last_account, 0) = 1
  AND (
      @term = N''
      OR Account_Code LIKE N'%' + @term + N'%'
      OR Account_Serial LIKE N'%' + @term + N'%'
      OR Account_Name LIKE N'%' + @term + N'%'
  )
ORDER BY Account_Serial, Account_Code;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = (term ?? string.Empty).Trim();
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosLookupDto
                        {
                            Id = ReadString(reader, "Id"),
                            Name = ReadString(reader, "Name"),
                            Extra = ReadString(reader, "Extra")
                        });
                    }
                }
            }

            return list;
        }

        public IList<PosAccountTreeDto> GetChartOfAccountsTree()
        {
            var flat = new List<PosAccountTreeDto>();
            const string sql = @"
SELECT
    Account_Code,
    Account_Name,
    Parent_Account_Code,
    ISNULL(last_account, 0) AS IsLastAccount
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE NULLIF(LTRIM(RTRIM(ISNULL(Account_Code, N''))), N'') IS NOT NULL
ORDER BY Account_Code;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        flat.Add(new PosAccountTreeDto
                        {
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            ParentAccountCode = ReadString(reader, "Parent_Account_Code"),
                            IsLastAccount = ReadBoolean(reader, "IsLastAccount")
                        });
                    }
                }
            }

            var byCode = flat
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode))
                .GroupBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var roots = new List<PosAccountTreeDto>();
            foreach (var account in flat)
            {
                PosAccountTreeDto parent;
                if (!string.IsNullOrWhiteSpace(account.ParentAccountCode) && byCode.TryGetValue(account.ParentAccountCode, out parent) && !string.Equals(parent.AccountCode, account.AccountCode, StringComparison.OrdinalIgnoreCase))
                {
                    parent.Children.Add(account);
                }
                else
                {
                    roots.Add(account);
                }
            }

            return roots;
        }

        public IList<PosAccountTreeDto> GetChartOfAccountsChildren(string parentAccountCode, string term)
        {
            var list = new List<PosAccountTreeDto>();
            var search = (term ?? string.Empty).Trim();
            var hasSearch = search.Length >= 2;
            const string sql = @"
SELECT TOP (80)
    a.Account_Code,
    a.Account_Serial,
    a.Account_Name,
    a.Parent_Account_Code,
    ISNULL(a.last_account, 0) AS IsLastAccount,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.ACCOUNTS c WITH (NOLOCK)
        WHERE ISNULL(c.Parent_Account_Code, N'') = ISNULL(a.Account_Code, N'')
    ) THEN 1 ELSE 0 END AS HasChildren
FROM dbo.ACCOUNTS a WITH (NOLOCK)
WHERE NULLIF(LTRIM(RTRIM(ISNULL(a.Account_Code, N''))), N'') IS NOT NULL
  AND
  (
      (@hasSearch = 1 AND (a.Account_Serial LIKE N'%' + @term + N'%' OR a.Account_Name LIKE N'%' + @term + N'%' OR a.Account_Code LIKE N'%' + @term + N'%'))
      OR
      (@hasSearch = 0 AND ((@parentCode = N'' AND NULLIF(LTRIM(RTRIM(ISNULL(a.Parent_Account_Code, N''))), N'') IS NULL) OR ISNULL(a.Parent_Account_Code, N'') = @parentCode))
  )
ORDER BY a.Account_Serial, a.Account_Code;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@parentCode", SqlDbType.NVarChar, 50).Value = (parentAccountCode ?? string.Empty).Trim();
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = search;
                command.Parameters.Add("@hasSearch", SqlDbType.Bit).Value = hasSearch;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosAccountTreeDto
                        {
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountSerial = ReadString(reader, "Account_Serial"),
                            AccountName = ReadString(reader, "Account_Name"),
                            ParentAccountCode = ReadString(reader, "Parent_Account_Code"),
                            IsLastAccount = ReadBoolean(reader, "IsLastAccount"),
                            HasChildren = ReadBoolean(reader, "HasChildren")
                        });
                    }
                }
            }

            return list;
        }

        public IList<PosJournalHeaderDto> SearchJournalEntries(PosJournalSearchRequest request, int userId, bool canChangeDefaults)
        {
            var list = new List<PosJournalHeaderDto>();
            request = request ?? new PosJournalSearchRequest();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Journal_Search", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 90;
                Add(command, "@fromDate", SqlDbType.DateTime, request.FromDate.HasValue ? (object)request.FromDate.Value.Date : DBNull.Value);
                Add(command, "@toDate", SqlDbType.DateTime, request.ToDate.HasValue ? (object)request.ToDate.Value.Date : DBNull.Value);
                Add(command, "@branchId", SqlDbType.Int, request.BranchId);
                command.Parameters.Add("@voucherNo", SqlDbType.NVarChar, 100).Value = (request.VoucherNo ?? string.Empty).Trim();
                command.Parameters.Add("@description", SqlDbType.NVarChar, 200).Value = (request.Description ?? string.Empty).Trim();
                command.Parameters.Add("@accountCode", SqlDbType.NVarChar, 50).Value = (request.AccountCode ?? string.Empty).Trim();
                AddString(command, "@accountCodes", SqlDbType.NVarChar, -1, request.AccountCodes);
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var isManual = ReadInt(reader, "IsManual").GetValueOrDefault() == 1;
                        list.Add(new PosJournalHeaderDto
                        {
                            NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            NoteDate = ReadDateTime(reader, "NoteDate"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            Description = FixArabicMojibakeForDisplay(ReadString(reader, "Description")),
                            IsManual = isManual,
                            EntryKind = isManual ? "قيد يدوي" : "قيد آلي",
                            DebitTotal = ReadDecimal(reader, "DebitTotal").GetValueOrDefault(),
                            CreditTotal = ReadDecimal(reader, "CreditTotal").GetValueOrDefault()
                        });
                    }
                }
            }

            return list;
        }

        public PosJournalHeaderDto GetJournalEntryByNoteId(int noteId, int userId, bool canChangeDefaults)
        {
            PosJournalHeaderDto header = null;
            const string sql = @"
SELECT TOP (1)
    n.NoteID,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
    n.NoteDate,
    n.branch_no AS BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
    ISNULL(n.Remark, N'') AS Description,
    CASE WHEN ISNULL(n.NoteType, 0) = 57 THEN 1 ELSE 0 END AS IsManual
FROM dbo.Notes n WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = n.branch_no
WHERE n.NoteID = @noteId
  AND (@canChangeDefaults = 1 OR n.UserID = @userId);

SELECT
    d.DEV_ID_Line_No,
    d.Account_Code,
    COALESCE(NULLIF(a.Account_Serial, N''), d.Account_Code) AS AccountSerial,
    a.Account_Name,
    d.Double_Entry_Vouchers_Description,
    CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END AS Debit,
    CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END AS Credit
FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
WHERE d.Notes_ID = @noteId
ORDER BY d.DEV_ID_Line_No;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var isManual = ReadInt(reader, "IsManual").GetValueOrDefault() == 1;
                        header = new PosJournalHeaderDto
                        {
                            NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            NoteDate = ReadDateTime(reader, "NoteDate"),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            Description = FixArabicMojibakeForDisplay(ReadString(reader, "Description")),
                            IsManual = isManual,
                            EntryKind = isManual ? "قيد يدوي" : "قيد آلي"
                        };
                    }

                    if (header != null && reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var line = new PosManualJournalLineDto
                            {
                                AccountCode = ReadString(reader, "Account_Code"),
                                AccountSerial = ReadString(reader, "AccountSerial"),
                                AccountName = ReadString(reader, "Account_Name"),
                                Description = FixArabicMojibakeForDisplay(ReadString(reader, "Double_Entry_Vouchers_Description")),
                                Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                                Credit = ReadDecimal(reader, "Credit").GetValueOrDefault()
                            };
                            header.DebitTotal += line.Debit;
                            header.CreditTotal += line.Credit;
                            header.Lines.Add(line);
                        }
                    }
                }
            }

            return header;
        }

        public PosJournalHeaderDto SaveManualJournalEntry(PosManualJournalSaveRequest request, int userId, bool canChangeDefaults, bool allowAutomaticOverride)
        {
            if (request == null)
            {
                throw new InvalidOperationException("بيانات القيد غير مكتملة");
            }

            EnsureNotesAuditColumns();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var isUpdate = request.NoteId.HasValue && request.NoteId.Value > 0;
                        var noteType = 57;
                        if (isUpdate)
                        {
                            noteType = ExecuteScalarInt(connection, transaction, "SELECT ISNULL(NoteType, 0) FROM dbo.Notes WHERE NoteID = @noteId", new SqlParameter("@noteId", request.NoteId.Value)).GetValueOrDefault();
                            if (noteType != 57 && !allowAutomaticOverride)
                            {
                                throw new InvalidOperationException("لا يمكن تعديل قيد آلي بدون كلمة مرور المدير العام");
                            }
                        }

                        var branchId = request.BranchId.GetValueOrDefault();
                        var noteValue = request.Lines.Sum(x => x.Debit);
                        var noteId = isUpdate ? request.NoteId.Value : GetNextIdFromSequence(connection, transaction, "Notes", "NoteID");
                        var noteSerial = isUpdate
                            ? ExecuteScalarString(connection, transaction, "SELECT CONVERT(NVARCHAR(50), CAST(NoteSerial AS DECIMAL(38,0))) FROM dbo.Notes WHERE NoteID = @noteId", new SqlParameter("@noteId", noteId))
                            : GenerateNotesSerial(connection, transaction, branchId, request.NoteDate);
                        decimal noteSerialDecimal;
                        decimal.TryParse(noteSerial, NumberStyles.Any, CultureInfo.InvariantCulture, out noteSerialDecimal);

                        if (isUpdate)
                        {
                            ExecuteNonQuery(connection, transaction, @"
UPDATE dbo.Notes
SET NoteDate = @noteDate,
    Note_Value = @noteValue,
    Remark = @description,
    branch_no = @branchId,
    LastModifiedByUserId = @userId,
    LastModifiedDate = GETDATE()
WHERE NoteID = @noteId;",
                                new SqlParameter("@noteDate", request.NoteDate.Date),
                                new SqlParameter("@noteValue", noteValue),
                                new SqlParameter("@description", request.Description ?? string.Empty),
                                new SqlParameter("@branchId", branchId),
                                new SqlParameter("@userId", userId),
                                new SqlParameter("@noteId", noteId));
                            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId", new SqlParameter("@noteId", noteId));
                        }
                        else
                        {
                            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, UserID,
    Remark, NotePosted, branch_no, user_name, note_value_by_characters
)
VALUES
(
    @noteId, @noteDate, 57, @noteSerial, @noteSerial, @noteValue, @userId,
    @description, 0, @branchId, @userName, N''
);",
                                new SqlParameter("@noteId", noteId),
                                new SqlParameter("@noteDate", request.NoteDate.Date),
                                new SqlParameter("@noteSerial", noteSerialDecimal),
                                new SqlParameter("@noteValue", noteValue),
                                new SqlParameter("@userId", userId),
                                new SqlParameter("@description", request.Description ?? string.Empty),
                                new SqlParameter("@branchId", branchId),
                                new SqlParameter("@userName", string.Empty));
                        }

                        var voucherId = ExecuteScalarInt(connection, transaction, "SELECT ISNULL(MAX(Double_Entry_Vouchers_ID),0) + 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (UPDLOCK, HOLDLOCK)", null).GetValueOrDefault();
                        var lineNo = 1;
                        foreach (var line in request.Lines)
                        {
                            AddPosPaymentDevLine(connection, transaction, voucherId, lineNo++, new PosPaymentLineDto
                            {
                                AccountCode = line.AccountCode,
                                Description = string.IsNullOrWhiteSpace(line.Description) ? request.Description : line.Description,
                                Debit = line.Debit,
                                Credit = line.Credit
                            }, noteId, request.NoteDate, userId, branchId);
                        }

                        transaction.Commit();
                        return GetJournalEntryByNoteId(noteId, userId, true);
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public DataTable RunKycBankExport(int cardLength, DateTime fromDate, DateTime toDate, int? branchId, bool canChangeDefaults)
        {
            if (cardLength != 8 && cardLength != 18)
            {
                cardLength = 18;
            }

            var table = new DataTable();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_KycBank_Export", connection))
            using (var adapter = new SqlDataAdapter(command))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;
                command.Parameters.Add("@cardLength", SqlDbType.Int).Value = cardLength;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                adapter.Fill(table);
            }

            return table;
        }

        public PosDashboardSummaryDto GetAdminDashboardSummary(DateTime fromDate, DateTime toDate, DateTime previousFromDate, DateTime previousToDate, int? branchId, string operationType, string periodType, bool loadAdvancedInsights = true)
        {
            var summary = new PosDashboardSummaryDto();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Dashboard_Summary", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@previousFromDate", SqlDbType.DateTime).Value = previousFromDate.Date;
                command.Parameters.Add("@previousToDate", SqlDbType.DateTime).Value = previousToDate.Date;
                Add(command, "@branchId", SqlDbType.Int, branchId);
                command.Parameters.Add("@operationType", SqlDbType.NVarChar, 30).Value = (operationType ?? string.Empty).Trim();
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        summary.Kpis.TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault();
                        summary.Kpis.SalesTotal = ReadDecimal(reader, "SalesTotal").GetValueOrDefault();
                        summary.Kpis.FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault();
                        summary.Kpis.VatTotal = ReadDecimal(reader, "VatTotal").GetValueOrDefault();
                        summary.Kpis.NetCollection = ReadDecimal(reader, "NetCollection").GetValueOrDefault();
                        summary.Kpis.ActivatedKycCards = ReadInt(reader, "ActivatedKycCards").GetValueOrDefault();
                        summary.Kpis.CancelledOrReturnedCount = ReadInt(reader, "CancelledOrReturnedCount").GetValueOrDefault();
                    }

                    if (reader.NextResult())
                    {
                        if (reader.Read())
                        {
                            summary.PreviousKpis.TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault();
                            summary.PreviousKpis.SalesTotal = ReadDecimal(reader, "SalesTotal").GetValueOrDefault();
                            summary.PreviousKpis.FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault();
                            summary.PreviousKpis.VatTotal = ReadDecimal(reader, "VatTotal").GetValueOrDefault();
                            summary.PreviousKpis.NetCollection = ReadDecimal(reader, "NetCollection").GetValueOrDefault();
                            summary.PreviousKpis.ActivatedKycCards = ReadInt(reader, "ActivatedKycCards").GetValueOrDefault();
                            summary.PreviousKpis.CancelledOrReturnedCount = ReadInt(reader, "CancelledOrReturnedCount").GetValueOrDefault();
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            summary.BranchComparison.Add(new PosDashboardBranchDto
                            {
                                BranchId = ReadInt(reader, "BranchId"),
                                BranchName = ReadString(reader, "BranchName"),
                                TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                                TotalValue = ReadDecimal(reader, "TotalValue").GetValueOrDefault(),
                                FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            summary.WorstBranches.Add(new PosDashboardBranchDto
                            {
                                BranchId = ReadInt(reader, "BranchId"),
                                BranchName = ReadString(reader, "BranchName"),
                                TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                                TotalValue = ReadDecimal(reader, "TotalValue").GetValueOrDefault(),
                                FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            summary.TopServices.Add(new PosDashboardServiceDto
                            {
                                ItemId = ReadInt(reader, "Item_ID"),
                                ItemName = ReadString(reader, "ItemName"),
                                SaleCount = ReadInt(reader, "SaleCount").GetValueOrDefault(),
                                TotalValue = ReadDecimal(reader, "TotalValue").GetValueOrDefault(),
                                FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            summary.OperationTypeSummary.Add(new PosDashboardOperationDto
                            {
                                OperationType = ReadString(reader, "OperationType"),
                                TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                                RechargeTotal = ReadDecimal(reader, "RechargeTotal").GetValueOrDefault(),
                                FeesTotal = ReadDecimal(reader, "FeesTotal").GetValueOrDefault(),
                                VatTotal = ReadDecimal(reader, "VatTotal").GetValueOrDefault(),
                                NetCollection = ReadDecimal(reader, "NetCollection").GetValueOrDefault()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            summary.DailyTrend.Add(new PosDashboardTrendDto
                            {
                                Day = ReadString(reader, "Day"),
                                TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                                NetCollection = ReadDecimal(reader, "NetCollection").GetValueOrDefault()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var rankNo = ReadInt(reader, "RankNo").GetValueOrDefault();
                            summary.TopSellers.Add(new PosDashboardSellerDto
                            {
                                RankNo = rankNo,
                                RankIcon = DashboardSellerRankIcon(rankNo),
                                SellerId = ReadInt(reader, "SellerId"),
                                SellerName = ReadString(reader, "SellerName"),
                                TransactionCount = ReadInt(reader, "TransactionCount").GetValueOrDefault(),
                                NetValue = ReadDecimal(reader, "NetValue").GetValueOrDefault()
                            });
                        }
                    }
                }
            }

            if (loadAdvancedInsights)
            {
                PopulateDashboardSmartInsights(summary, fromDate, toDate, previousFromDate, previousToDate, branchId, operationType, periodType);
            }
            PopulateDashboardIntelligence(summary, periodType);
            return summary;
        }

        private void PopulateDashboardSmartInsights(PosDashboardSummaryDto summary, DateTime fromDate, DateTime toDate, DateTime previousFromDate, DateTime previousToDate, int? branchId, string operationType, string periodType)
        {
            var branchRows = new List<DashboardSmartMetricRow>();
            var serviceRows = new List<DashboardSmartMetricRow>();
            var sellerRows = new List<DashboardSmartMetricRow>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Dashboard_SmartInsights", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = fromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = toDate.Date;
                command.Parameters.Add("@previousFromDate", SqlDbType.DateTime).Value = previousFromDate.Date;
                command.Parameters.Add("@previousToDate", SqlDbType.DateTime).Value = previousToDate.Date;
                Add(command, "@branchId", SqlDbType.Int, branchId);
                command.Parameters.Add("@operationType", SqlDbType.NVarChar, 30).Value = (operationType ?? string.Empty).Trim();
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    ReadSmartMetricRows(reader, branchRows);
                    if (reader.NextResult()) { ReadSmartMetricRows(reader, serviceRows); }
                    if (reader.NextResult()) { ReadSmartMetricRows(reader, sellerRows); }
                }
            }

            BuildBranchSmartInsights(summary, branchRows, periodType);
            BuildServiceSmartInsights(summary, serviceRows);
            BuildSellerSmartInsights(summary, sellerRows);
            BuildCollectionQualityInsights(summary, periodType);
            BuildEndOfDayProjection(summary, fromDate, toDate, periodType);

            if (!summary.SmartInsights.Any())
            {
                AddSmartInsight(summary, "info", "Low", "💡", "بيانات غير كافية", "لا توجد بيانات كافية لإصدار توصية موثوقة في الفترة المحددة.", "استمر في متابعة البيانات بعد زيادة عدد الحركات.", "جودة البيانات");
            }
        }

        private static void ReadSmartMetricRows(SqlDataReader reader, IList<DashboardSmartMetricRow> rows)
        {
            while (reader.Read())
            {
                rows.Add(new DashboardSmartMetricRow
                {
                    EntityId = ReadInt(reader, "EntityId"),
                    EntityName = ReadString(reader, "EntityName"),
                    CurrentCount = ReadInt(reader, "CurrentCount").GetValueOrDefault(),
                    CurrentValue = ReadDecimal(reader, "CurrentValue").GetValueOrDefault(),
                    CurrentFees = ReadDecimal(reader, "CurrentFees").GetValueOrDefault(),
                    PreviousCount = ReadInt(reader, "PreviousCount").GetValueOrDefault(),
                    PreviousValue = ReadDecimal(reader, "PreviousValue").GetValueOrDefault(),
                    PreviousFees = ReadDecimal(reader, "PreviousFees").GetValueOrDefault()
                });
            }
        }

        private static void BuildBranchSmartInsights(PosDashboardSummaryDto summary, IList<DashboardSmartMetricRow> branches, string periodType)
        {
            var active = branches.Where(x => x.CurrentCount > 0 && x.CurrentValue > 0).ToList();
            var dropped = branches
                .Where(x => x.PreviousValue > 0 && x.CurrentValue > 0 && PercentChange(x.CurrentValue, x.PreviousValue) <= -20)
                .OrderBy(x => PercentChange(x.CurrentValue, x.PreviousValue))
                .Take(3)
                .ToList();

            foreach (var branch in dropped)
            {
                var drop = Math.Abs(PercentChange(branch.CurrentValue, branch.PreviousValue));
                AddSmartInsight(summary, "decline", drop >= 35 ? "High" : "Medium", "📉", "فرع متراجع: " + branch.EntityName,
                    "انخفض صافي التحصيل بنسبة " + drop.ToString("0.#") + "% مقارنة بالفترة السابقة.",
                    "راجع أداء البائعين والخدمات الأكثر تراجعًا داخل الفرع.", "صافي التحصيل - " + DashboardPeriodLabel(periodType));
            }

            var rising = branches
                .Where(x => x.PreviousValue > 0 && x.CurrentValue > 0 && PercentChange(x.CurrentValue, x.PreviousValue) >= 25)
                .OrderByDescending(x => PercentChange(x.CurrentValue, x.PreviousValue))
                .FirstOrDefault();
            if (rising != null)
            {
                var countReason = rising.CurrentCount > rising.PreviousCount ? "زيادة عدد الحركات" : "ارتفاع متوسط قيمة الحركة";
                AddSmartInsight(summary, "growth", "Low", "📈", "فرع صاعد: " + rising.EntityName,
                    "تحسن صافي التحصيل بنسبة " + PercentChange(rising.CurrentValue, rising.PreviousValue).ToString("0.#") + "%، والسبب الأقرب: " + countReason + ".",
                    "احتفظ بنفس نمط التشغيل وراجع إمكانية نقله للفروع المشابهة.", "مقارنة الفروع");
            }

            if (active.Count >= 2)
            {
                var average = active.Average(x => x.CurrentValue);
                var lowActivity = active.Where(x => x.CurrentValue < average * 0.50M).OrderBy(x => x.CurrentValue).FirstOrDefault();
                if (lowActivity != null)
                {
                    AddSmartInsight(summary, "warning", "Medium", "🚨", "إنذار فرع أقل من المتوسط",
                        "فرع " + lowActivity.EntityName + " أقل من متوسط الفروع بأكثر من 50% في صافي التحصيل.",
                        "راجع ساعات الذروة، تدريب التيلرات، ومزيج الخدمات في هذا الفرع.", "إنذار مبكر");
                }
            }
        }

        private static void BuildServiceSmartInsights(PosDashboardSummaryDto summary, IList<DashboardSmartMetricRow> services)
        {
            var active = services.Where(x => x.CurrentCount > 0).ToList();
            var strongest = active.OrderByDescending(x => x.CurrentFees).FirstOrDefault();
            if (strongest != null && strongest.CurrentFees > 0)
            {
                AddSmartInsight(summary, "opportunity", "Low", "🏆", "خدمة تقود الرسوم",
                    "خدمة " + strongest.EntityName + " تحقق أعلى رسوم حالية بإجمالي " + strongest.CurrentFees.ToString("0.00") + ".",
                    "استخدمها كخدمة تركيز في التدريب والتنشيط.", "أفضل الخدمات");
            }

            var stalled = services
                .Where(x => x.PreviousCount > 0 && x.CurrentCount == 0)
                .OrderByDescending(x => x.PreviousValue)
                .FirstOrDefault();
            if (stalled != null)
            {
                AddSmartInsight(summary, "warning", "Medium", "🚨", "خدمة توقفت عن البيع",
                    "خدمة " + stalled.EntityName + " كان لها نشاط في الفترة السابقة ولا توجد لها حركات الآن.",
                    "تحقق من توفر الخدمة وتعريفها وصلاحيات استخدامها.", "إنذار مبكر");
            }

            var serviceDrop = services
                .Where(x => x.PreviousValue > 0 && x.CurrentValue > 0 && PercentChange(x.CurrentValue, x.PreviousValue) <= -30)
                .OrderBy(x => PercentChange(x.CurrentValue, x.PreviousValue))
                .FirstOrDefault();
            if (serviceDrop != null)
            {
                AddSmartInsight(summary, "decline", "Medium", "📉", "خدمة متراجعة",
                    "خدمة " + serviceDrop.EntityName + " تراجعت بنسبة " + Math.Abs(PercentChange(serviceDrop.CurrentValue, serviceDrop.PreviousValue)).ToString("0.#") + "%.",
                    "راجع سبب انخفاض الطلب أو وجود بديل يسحب الحركة منها.", "أفضل وأسوأ الخدمات");
            }
        }

        private static void BuildSellerSmartInsights(PosDashboardSummaryDto summary, IList<DashboardSmartMetricRow> sellers)
        {
            var active = sellers.Where(x => x.CurrentCount > 0 && x.CurrentValue > 0).ToList();
            if (active.Count < 2)
            {
                return;
            }

            var top = active.OrderByDescending(x => x.CurrentValue).ThenByDescending(x => x.CurrentCount).First();
            var excludedEntityIds = new HashSet<int>();
            if (top.EntityId.HasValue) { excludedEntityIds.Add(top.EntityId.Value); }
            AddSmartInsight(summary, "performance", "Low", "🏆", "بائع متميز",
                top.EntityName + " يتصدر أداء البائعين في الفترة الحالية بصافي " + top.CurrentValue.ToString("0.00") + ".",
                "استفد من أسلوبه في توجيه باقي الفريق.", "أداء البائعين");

            var averageTicket = active.Sum(x => x.CurrentValue) / Math.Max(1, active.Sum(x => x.CurrentCount));
            var sellerLowAverageTicket = active
                .Where(x => x.CurrentCount >= 3 && (!x.EntityId.HasValue || !excludedEntityIds.Contains(x.EntityId.Value)) && (x.CurrentValue / Math.Max(1, x.CurrentCount)) < averageTicket * 0.60M)
                .OrderBy(x => x.CurrentValue / Math.Max(1, x.CurrentCount))
                .FirstOrDefault();
            if (sellerLowAverageTicket != null)
            {
                if (sellerLowAverageTicket.EntityId.HasValue) { excludedEntityIds.Add(sellerLowAverageTicket.EntityId.Value); }
                AddSmartInsight(summary, "opportunity", "Medium", "💡", "فرصة تحسين بائع",
                    sellerLowAverageTicket.EntityName + " لديه عدد حركات جيد لكن متوسط الحركة أقل من متوسط الفريق.",
                    "راجع نوع العمليات التي ينفذها وشجعه على الخدمات ذات القيمة الأعلى.", "الفرص الضائعة");
            }

            var decliningSeller = active
                .Where(x => x.PreviousValue > 0 && (!x.EntityId.HasValue || !excludedEntityIds.Contains(x.EntityId.Value)) && PercentChange(x.CurrentValue, x.PreviousValue) <= -30)
                .OrderBy(x => PercentChange(x.CurrentValue, x.PreviousValue))
                .FirstOrDefault();
            if (decliningSeller != null)
            {
                AddSmartInsight(summary, "decline", "Medium", "📉", "بائع متراجع",
                    decliningSeller.EntityName + " تراجع أداؤه بنسبة " + Math.Abs(PercentChange(decliningSeller.CurrentValue, decliningSeller.PreviousValue)).ToString("0.#") + "% عن الفترة السابقة.",
                    "راجع ظروف التشغيل أو احتياجه لدعم في الخدمات الأعلى طلبًا.", "أداء البائعين");
            }
        }

        private static void BuildCollectionQualityInsights(PosDashboardSummaryDto summary, string periodType)
        {
            if (summary.Kpis.TransactionCount <= 0)
            {
                return;
            }

            var feeEfficiency = summary.Kpis.NetCollection > 0 ? summary.Kpis.FeesTotal / summary.Kpis.NetCollection : 0;
            AddSmartInsight(summary, "quality", "Low", "💰", "جودة التحصيل",
                "نسبة الرسوم إلى صافي التحصيل " + (feeEfficiency * 100).ToString("0.#") + "% في الفترة " + DashboardPeriodLabel(periodType) + ".",
                "استخدمها لمراقبة مزيج Cash In / Cash Out / الكروت دون تغيير الأسعار.", "جودة التحصيل");

            if (summary.Kpis.CancelledOrReturnedCount > 0 && summary.Kpis.CancelledOrReturnedCount >= Math.Max(3, summary.Kpis.TransactionCount * 0.05M))
            {
                AddSmartInsight(summary, "warning", "High", "🚨", "ارتفاع مرتجعات/إلغاءات",
                    "عدد الفواتير الملغاة أو المرتجعة مرتفع مقارنة بعدد الحركات.",
                    "راجع أسباب الإلغاء والمرتجعات قبل نهاية اليوم.", "إنذار مبكر");
            }

            if (summary.Kpis.TransactionCount >= 10 && summary.Kpis.ActivatedKycCards <= summary.Kpis.TransactionCount * 0.05M)
            {
                AddSmartInsight(summary, "opportunity", "Medium", "📈", "فرصة KYC غير مستغلة",
                    "عدد كروت KYC المفعلة منخفض مقارنة بحجم الحركة.",
                    "راجع تدريب التفعيل وتوفر المرفقات في الفروع ذات الحركة العالية.", "الفرص الضائعة");
            }
        }

        private static void BuildEndOfDayProjection(PosDashboardSummaryDto summary, DateTime fromDate, DateTime toDate, string periodType)
        {
            if (!string.Equals((periodType ?? string.Empty).Trim(), "daily", StringComparison.OrdinalIgnoreCase) || fromDate.Date != DateTime.Today || toDate.Date != DateTime.Today || summary.Kpis.NetCollection <= 0)
            {
                return;
            }

            const decimal totalWorkingHours = 12M;
            var elapsed = (decimal)Math.Max(1, Math.Min(12, DateTime.Now.Hour - 9 + (DateTime.Now.Minute / 60.0)));
            var projected = summary.Kpis.NetCollection / elapsed * totalWorkingHours;
            AddSmartInsight(summary, "forecast", "Low", "📊", "توقع نهاية اليوم",
                "بناءً على الأداء حتى الآن، من المتوقع أن يصل صافي التحصيل اليوم إلى " + projected.ToString("0.00") + ".",
                "استخدم التوقع لمتابعة الفروع قبل الإغلاق وليس كرقم محاسبي نهائي.", "توقع تشغيلي");
        }

        private static void AddSmartInsight(PosDashboardSummaryDto summary, string type, string severity, string icon, string title, string description, string action, string metric)
        {
            if (summary.SmartInsights.Any(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Description, description, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            summary.SmartInsights.Add(new PosDashboardSmartInsightDto
            {
                Type = type,
                Severity = severity,
                Icon = icon,
                Title = title,
                Description = description,
                Action = action,
                Metric = metric
            });
        }

        private static decimal PercentChange(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return current == 0 ? 0 : 100;
            }

            return ((current - previous) / Math.Abs(previous)) * 100;
        }

        private static void PopulateDashboardIntelligence(PosDashboardSummaryDto summary, string periodType)
        {
            summary.KpiComparisons.Add(BuildDashboardComparison("transactions", "عدد الحركات", summary.Kpis.TransactionCount, summary.PreviousKpis.TransactionCount, "number", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("sales", "إجمالي المبيعات/الشحن", summary.Kpis.SalesTotal, summary.PreviousKpis.SalesTotal, "money", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("fees", "إجمالي الرسوم", summary.Kpis.FeesTotal, summary.PreviousKpis.FeesTotal, "money", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("vat", "إجمالي الضريبة", summary.Kpis.VatTotal, summary.PreviousKpis.VatTotal, "money", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("net", "صافي التحصيل", summary.Kpis.NetCollection, summary.PreviousKpis.NetCollection, "money", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("kyc", "كروت KYC المفعلة", summary.Kpis.ActivatedKycCards, summary.PreviousKpis.ActivatedKycCards, "number", true));
            summary.KpiComparisons.Add(BuildDashboardComparison("returns", "فواتير ملغاة/مرتجعة", summary.Kpis.CancelledOrReturnedCount, summary.PreviousKpis.CancelledOrReturnedCount, "number", false));

            var activeBestBranches = summary.BranchComparison.Where(x => x.TransactionCount > 0 && x.TotalValue > 0).ToList();
            var activeWorstBranches = summary.WorstBranches.Where(x => x.TransactionCount > 0 && x.TotalValue > 0).ToList();
            var bestBranch = activeBestBranches.FirstOrDefault();
            var worstBranch = activeWorstBranches.FirstOrDefault();
            var topService = summary.TopServices.FirstOrDefault();
            var lowService = summary.TopServices.LastOrDefault();
            var highestFeeService = summary.TopServices.OrderByDescending(x => x.FeesTotal).FirstOrDefault();
            var averageTransaction = summary.Kpis.TransactionCount > 0 ? summary.Kpis.NetCollection / summary.Kpis.TransactionCount : 0;
            var netComparison = summary.KpiComparisons.FirstOrDefault(x => x.Key == "net");
            var periodLabel = DashboardPeriodLabel(periodType);

            if (bestBranch != null)
            {
                summary.Insights.Add("أفضل فرع في الفترة: " + bestBranch.BranchName + " بصافي تحصيل " + bestBranch.TotalValue.ToString("0.00"));
            }

            if (worstBranch != null)
            {
                summary.Insights.Add("أقل فرع أداءً يحتاج متابعة: " + worstBranch.BranchName + " بصافي تحصيل " + worstBranch.TotalValue.ToString("0.00"));
            }

            if (topService != null)
            {
                summary.Insights.Add("أكثر خدمة مبيعًا: " + topService.ItemName + " بعدد " + topService.SaleCount.ToString("0") + " حركة");
            }

            if (lowService != null && !string.Equals(lowService.ItemName, topService != null ? topService.ItemName : null, StringComparison.OrdinalIgnoreCase))
            {
                summary.Insights.Add("أقل خدمة ضمن القائمة: " + lowService.ItemName + " بعدد " + lowService.SaleCount.ToString("0") + " حركة");
            }

            if (highestFeeService != null)
            {
                summary.Insights.Add("الخدمة الأعلى رسومًا: " + highestFeeService.ItemName + " بإجمالي رسوم " + highestFeeService.FeesTotal.ToString("0.00"));
            }

            summary.Insights.Add("متوسط قيمة العملية: " + averageTransaction.ToString("0.00"));
            summary.Insights.Add("عدد الفواتير الملغاة أو المرتجعة: " + summary.Kpis.CancelledOrReturnedCount.ToString("0"));

            if (netComparison != null)
            {
                summary.Insights.Add("نسبة نمو التحصيل مقارنة بالفترة السابقة: " + netComparison.ChangePercent.ToString("0.##") + "%");
                if (netComparison.ChangePercent < -10)
                {
                    summary.Recommendations.Add("في انخفاض واضح في التحصيل مقارنة بالفترة السابقة، راجع أداء الفروع والخدمات الأعلى تأثيرًا.");
                }
                else if (netComparison.ChangePercent > 10)
                {
                    summary.Recommendations.Add("التحصيل زاد مقارنة بالفترة السابقة، حافظ على نفس تشغيل الفروع والخدمات الأعلى أداءً.");
                }
            }

            if (activeWorstBranches.Count == 0)
            {
                summary.Recommendations.Add("لا توجد بيانات كافية لتحديد أقل الفروع أداءً في الفترة المحددة");
            }
            else if (worstBranch != null && activeBestBranches.Count > 1)
            {
                var averageBranchNet = activeBestBranches.Concat(activeWorstBranches).GroupBy(x => x.BranchId).Select(g => g.First()).Average(x => x.TotalValue);
                if (averageBranchNet > 0 && worstBranch.TotalValue < averageBranchNet * 0.65M)
                {
                    summary.Recommendations.Add("فرع " + worstBranch.BranchName + " أقل من متوسط الفروع بأكثر من 35%، راجع التيلرات أو حركة العملاء.");
                }
                var names = string.Join("، ", activeWorstBranches.Select(x => x.BranchName).ToArray());
                summary.Recommendations.Add("أقل " + activeWorstBranches.Count.ToString("0") + " فروع أداءً حسب صافي التحصيل في الفترة " + periodLabel + ": " + names);
            }
            else
            {
                var names = string.Join("، ", activeWorstBranches.Select(x => x.BranchName).ToArray());
                summary.Recommendations.Add("أقل الفروع أداءً حسب صافي التحصيل في الفترة " + periodLabel + ": " + names);
            }

            if (topService != null && summary.Kpis.FeesTotal > 0 && topService.FeesTotal >= summary.Kpis.FeesTotal * 0.60M)
            {
                summary.Recommendations.Add("خدمة " + topService.ItemName + " تمثل نسبة كبيرة من الرسوم، ركز عليها في التسويق والتشغيل.");
            }

            var highTransactionsLowFees = summary.OperationTypeSummary.OrderByDescending(x => x.TransactionCount).FirstOrDefault();
            if (highTransactionsLowFees != null && highTransactionsLowFees.TransactionCount > 0 && highTransactionsLowFees.NetCollection > 0)
            {
                var feeRatio = highTransactionsLowFees.FeesTotal / highTransactionsLowFees.NetCollection;
                if (feeRatio < 0.02M)
                {
                    summary.Recommendations.Add("هناك نوع عملية بعدد حركات عالي ورسوم منخفضة، راجع مزيج العمليات والتسعير.");
                }
            }
        }

        private static string DashboardPeriodLabel(string periodType)
        {
            var value = (periodType ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "weekly") { return "الأسبوعية"; }
            if (value == "monthly") { return "الشهرية"; }
            if (value == "yearly") { return "السنوية"; }
            if (value == "custom") { return "المخصصة"; }
            return "اليومية";
        }

        private static string DashboardSellerRankIcon(int rankNo)
        {
            if (rankNo == 1) { return "🥇"; }
            if (rankNo == 2) { return "🥈"; }
            if (rankNo == 3) { return "🥉"; }
            return "⭐";
        }

        private static PosDashboardKpiComparisonDto BuildDashboardComparison(string key, string title, decimal current, decimal previous, string format, bool higherIsPositive)
        {
            var change = previous == 0
                ? (current == 0 ? 0 : 100)
                : ((current - previous) / Math.Abs(previous)) * 100;
            return new PosDashboardKpiComparisonDto
            {
                Key = key,
                Title = title,
                CurrentValue = current,
                PreviousValue = previous,
                ChangePercent = change,
                IsPositive = higherIsPositive ? change >= 0 : change <= 0,
                Format = format
            };
        }

        private sealed class DashboardSmartMetricRow
        {
            public int? EntityId { get; set; }
            public string EntityName { get; set; }
            public int CurrentCount { get; set; }
            public decimal CurrentValue { get; set; }
            public decimal CurrentFees { get; set; }
            public int PreviousCount { get; set; }
            public decimal PreviousValue { get; set; }
            public decimal PreviousFees { get; set; }
        }

        private sealed class PosItemPriceRangeCommissionDto
        {
            public decimal Price { get; set; }
            public decimal CashBack { get; set; }
            public decimal Cost { get; set; }
        }

        public IList<PosJournalEntryDto> GetJournalEntriesForTransaction(int transactionId, int userId, bool canChangeDefaults)
        {
            var entries = new List<PosJournalEntryDto>();
            const string sql = @"
IF OBJECT_ID('tempdb..#PosJournalNoteIds') IS NOT NULL DROP TABLE #PosJournalNoteIds;
CREATE TABLE #PosJournalNoteIds (NoteID INT NOT NULL PRIMARY KEY);

IF COL_LENGTH('dbo.Notes', 'Transaction_ID') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        INSERT INTO #PosJournalNoteIds(NoteID)
        SELECT DISTINCT n.NoteID
        FROM dbo.Notes n
        INNER JOIN dbo.Transactions t ON t.Transaction_ID = n.Transaction_ID
        WHERE t.Transaction_ID = @transactionId
          AND t.Transaction_Type = 21
          AND (@canChangeDefaults = 1 OR t.UserID = @userId);',
        N'@transactionId INT, @userId INT, @canChangeDefaults BIT',
        @transactionId, @userId, @canChangeDefaults;
END;

INSERT INTO #PosJournalNoteIds(NoteID)
SELECT DISTINCT n.NoteID
FROM dbo.Notes n
INNER JOIN dbo.Transactions t ON CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) = NULLIF(t.NoteSerial, N'')
WHERE t.Transaction_ID = @transactionId
  AND t.Transaction_Type = 21
  AND (@canChangeDefaults = 1 OR t.UserID = @userId)
  AND NOT EXISTS (SELECT 1 FROM #PosJournalNoteIds x WHERE x.NoteID = n.NoteID);

SELECT
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    dev.RecordDate,
    dev.Account_Code,
    acc.Account_Name,
    acc.Account_Serial,
    dev.Double_Entry_Vouchers_Description,
    CASE WHEN dev.Credit_Or_Debit = 0 THEN dev.Value ELSE 0 END AS Debit,
    CASE WHEN dev.Credit_Or_Debit = 1 THEN dev.Value ELSE 0 END AS Credit
FROM dbo.DOUBLE_ENTREY_VOUCHERS dev
INNER JOIN dbo.Notes n ON n.NoteID = dev.Notes_ID
LEFT JOIN dbo.ACCOUNTS acc ON acc.Account_Code = dev.Account_Code
WHERE dev.Notes_ID IN (SELECT NoteID FROM #PosJournalNoteIds)
ORDER BY dev.Notes_ID, dev.DEV_ID_Line_No;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new PosJournalEntryDto
                        {
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            RecordDate = ReadDateTime(reader, "RecordDate"),
                            AccountSerial = ReadString(reader, "Account_Serial"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            Description = FixArabicMojibakeForDisplay(ReadString(reader, "Double_Entry_Vouchers_Description")),
                            Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                            Credit = ReadDecimal(reader, "Credit").GetValueOrDefault()
                        });
                    }
                }
            }

            return entries;
        }

        public IList<PosLookupDto> GetPosPaymentBoxes(int? branchId, int? boxType)
        {
            var list = new List<PosLookupDto>();
            var sql = @"
SELECT TOP (300)
    CONVERT(NVARCHAR(50), BoxID) AS Id,
    COALESCE(NULLIF(BoxName, N''), NULLIF(BoxNameE, N''), N'خزنة ' + CONVERT(NVARCHAR(20), BoxID)) AS Name,
    Account_Code AS Extra
FROM dbo.TblBoxesData
WHERE (@branchId IS NULL OR BranchId = @branchId)
  AND (@boxType IS NULL OR [Type] = @boxType)
ORDER BY BoxName;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                Add(command, "@branchId", SqlDbType.Int, branchId);
                Add(command, "@boxType", SqlDbType.Int, boxType);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosLookupDto { Id = ReadString(reader, "Id"), Name = ReadString(reader, "Name"), Extra = ReadString(reader, "Extra") });
                    }
                }
            }

            return list;
        }

        public IList<PosLookupDto> GetPosPaymentBanks()
        {
            var list = new List<PosLookupDto>();
            const string sql = @"
SELECT TOP (300)
    CONVERT(NVARCHAR(50), BankID) AS Id,
    COALESCE(NULLIF(BankName, N''), NULLIF(BankNameE, N''), N'بنك ' + CONVERT(NVARCHAR(20), BankID)) AS Name,
    Account_Code AS Extra
FROM dbo.BanksData
ORDER BY BankName;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosLookupDto { Id = ReadString(reader, "Id"), Name = ReadString(reader, "Name"), Extra = ReadString(reader, "Extra") });
                    }
                }
            }

            return list;
        }

        public IList<PosLookupDto> GetPosPaymentEmployees()
        {
            var list = new List<PosLookupDto>();
            const string sql = @"
SELECT TOP (500)
    CONVERT(NVARCHAR(50), Emp_ID) AS Id,
    Emp_Name AS Name,
    Account_Code AS Extra
FROM dbo.TblEmployee
WHERE Emp_ID IS NOT NULL
ORDER BY Emp_Name;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosLookupDto { Id = ReadString(reader, "Id"), Name = ReadString(reader, "Name"), Extra = ReadString(reader, "Extra") });
                    }
                }
            }

            return list;
        }

        public IList<PosPaymentLineDto> PreviewPosPayment(PosPaymentRequestDto request)
        {
            request = request ?? new PosPaymentRequestDto();
            var lines = new List<PosPaymentLineDto>();
            var description = string.IsNullOrWhiteSpace(request.Remarks) ? "تمويل واستعاضة الخزن والعهد" : request.Remarks.Trim();
            lines.Add(new PosPaymentLineDto
            {
                AccountCode = request.NameAccountCode,
                AccountName = GetAccountName(request.NameAccountCode),
                Debit = request.Value,
                Credit = 0,
                Description = description
            });

            if (request.BoxValue == 0 && request.EmpValue == 0)
            {
                var credit = ResolvePaymentCreditAccount(request);
                lines.Add(new PosPaymentLineDto { AccountCode = credit, AccountName = GetAccountName(credit), Debit = 0, Credit = request.Value, Description = description });
            }
            else
            {
                if (request.BoxValue > 0)
                {
                    var credit = ResolvePaymentCreditAccount(request);
                    lines.Add(new PosPaymentLineDto { AccountCode = credit, AccountName = GetAccountName(credit), Debit = 0, Credit = request.BoxValue, Description = description });
                }

                if (request.EmpValue > 0)
                {
                    lines.Add(new PosPaymentLineDto { AccountCode = request.EmpAccountCode, AccountName = GetAccountName(request.EmpAccountCode), Debit = 0, Credit = request.EmpValue, Description = description });
                }
            }

            return lines;
        }

        public IList<PosPaymentMovementDto> SearchPosPayments(PosPaymentSearchRequestDto request, int userId, bool canChangeDefaults)
        {
            EnsurePosPaymentAuditColumns();
            request = request ?? new PosPaymentSearchRequestDto();
            var list = new List<PosPaymentMovementDto>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_POS_Payments_Search", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 90;
                command.Parameters.Add("@searchText", SqlDbType.NVarChar, 100).Value = (request.SearchText ?? string.Empty).Trim();
                Add(command, "@fromDate", SqlDbType.DateTime, request.FromDate.HasValue ? (object)request.FromDate.Value.Date : DBNull.Value);
                Add(command, "@toDate", SqlDbType.DateTime, request.ToDate.HasValue ? (object)request.ToDate.Value.Date : DBNull.Value);
                Add(command, "@branchId", SqlDbType.Int, request.BranchId);
                Add(command, "@empId", SqlDbType.Int, request.EmpId);
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@contextBranchId", SqlDbType.Int).Value = request.BranchId.GetValueOrDefault(0);
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PosPaymentMovementDto
                        {
                            NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            NoteDate = ReadDateTime(reader, "NoteDate"),
                            BranchId = ReadInt(reader, "BranchId").GetValueOrDefault(),
                            BranchName = ReadString(reader, "BranchName"),
                            CashingType = ReadInt(reader, "CashingType").GetValueOrDefault(),
                            CashingTypeName = ReadString(reader, "CashingTypeName"),
                            NameAccountCode = ReadString(reader, "NameAccountCode"),
                            NameText = ReadString(reader, "NameText"),
                            PaymentMethod = ReadInt(reader, "PaymentMethod").GetValueOrDefault(),
                            Value = ReadDecimal(reader, "Value").GetValueOrDefault(),
                            EmpId = ReadInt(reader, "EmpId"),
                            CreatedUserId = ReadInt(reader, "CreatedUserId"),
                            CreatedUserName = ReadString(reader, "CreatedUserName"),
                            LastModifiedByUserId = ReadInt(reader, "LastModifiedByUserId"),
                            LastModifiedByUserName = ReadString(reader, "LastModifiedByUserName"),
                            LastModifiedDate = ReadDateTime(reader, "LastModifiedDate")
                        });
                    }
                }
            }

            return list;
        }

        public PosPaymentMovementDto GetPosPayment(int noteId, int userId, bool canChangeDefaults, int? branchId)
        {
            EnsurePosPaymentAuditColumns();
            PosPaymentMovementDto item = null;
            const string sql = @"
SELECT TOP (1)
    n.NoteID,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) AS NoteSerial,
    CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) AS NoteSerial1,
    n.NoteDate,
    ISNULL(n.branch_no, 0) AS BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
    ISNULL(n.CashingType, 0) AS CashingType,
    ISNULL(n.BTCashAccountcode, N'') AS NameAccountCode,
    ISNULL(n.person, N'') AS NameText,
    ISNULL(n.NoteCashingType, 0) AS PaymentMethod,
    CAST(ISNULL(n.Note_Value, 0) AS DECIMAL(18,2)) AS Value,
    n.BoxID,
    n.BankID,
    n.ChqueNum AS ReferenceNo,
    n.DueDate AS ReferenceDate,
    n.Emp_ID AS EmpId,
    n.AccountEmpCode AS EmpAccountCode,
    CAST(ISNULL(n.boxValue, 0) AS DECIMAL(18,2)) AS BoxValue,
    CAST(ISNULL(n.EmpValue, 0) AS DECIMAL(18,2)) AS EmpValue,
    ISNULL(n.Remark, N'') AS Remarks,
    ISNULL(n.general_des_notes, N'') AS GeneralDescription,
    n.UserID AS CreatedUserId,
    u.UserName AS CreatedUserName,
    n.LastModifiedByUserId,
    mu.UserName AS LastModifiedByUserName,
    n.LastModifiedDate
FROM dbo.Notes n
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = n.branch_no
LEFT JOIN dbo.TblUsers u ON u.UserID = n.UserID
LEFT JOIN dbo.TblUsers mu ON mu.UserID = n.LastModifiedByUserId
WHERE n.NoteID = @noteId
  AND n.NoteType = 50
  AND (@canChangeDefaults = 1 OR n.UserID = @userId OR n.branch_no = @branchId);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId.GetValueOrDefault(0);
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        item = new PosPaymentMovementDto
                        {
                            NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            NoteDate = ReadDateTime(reader, "NoteDate"),
                            BranchId = ReadInt(reader, "BranchId").GetValueOrDefault(),
                            BranchName = ReadString(reader, "BranchName"),
                            CashingType = ReadInt(reader, "CashingType").GetValueOrDefault(),
                            NameAccountCode = ReadString(reader, "NameAccountCode"),
                            NameText = ReadString(reader, "NameText"),
                            PaymentMethod = ReadInt(reader, "PaymentMethod").GetValueOrDefault(),
                            Value = ReadDecimal(reader, "Value").GetValueOrDefault(),
                            BoxId = ReadInt(reader, "BoxID"),
                            BankId = ReadInt(reader, "BankID"),
                            ReferenceNo = ReadString(reader, "ReferenceNo"),
                            ReferenceDate = ReadDateTime(reader, "ReferenceDate"),
                            EmpId = ReadInt(reader, "EmpId"),
                            EmpAccountCode = ReadString(reader, "EmpAccountCode"),
                            BoxValue = ReadDecimal(reader, "BoxValue").GetValueOrDefault(),
                            EmpValue = ReadDecimal(reader, "EmpValue").GetValueOrDefault(),
                            Remarks = ReadString(reader, "Remarks"),
                            GeneralDescription = ReadString(reader, "GeneralDescription"),
                            CreatedUserId = ReadInt(reader, "CreatedUserId"),
                            CreatedUserName = ReadString(reader, "CreatedUserName"),
                            LastModifiedByUserId = ReadInt(reader, "LastModifiedByUserId"),
                            LastModifiedByUserName = ReadString(reader, "LastModifiedByUserName"),
                            LastModifiedDate = ReadDateTime(reader, "LastModifiedDate")
                        };
                    }
                }
            }

            if (item != null)
            {
                item.Lines = GetPosPaymentLines(item.NoteId);
            }

            return item;
        }

        public PosPaymentResultDto SavePosPayment(PosPaymentRequestDto request, int userId, bool canExecute)
        {
            if (!canExecute)
            {
                throw new UnauthorizedAccessException("ليست لديك صلاحية تنفيذ التمويل والاستعاضة");
            }

            ValidatePosPayment(request);
            var lines = PreviewPosPayment(request);
            var debit = lines.Sum(l => l.Debit);
            var credit = lines.Sum(l => l.Credit);
            if (Math.Abs(debit - credit) > 0.01m)
            {
                throw new InvalidOperationException("القيد غير متوازن. راجع قيم الخزنة والعهدة.");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        EnsurePosPaymentAuditColumns(connection, transaction);
                        var isUpdate = request.NoteId.HasValue && request.NoteId.Value > 0;
                        var noteId = isUpdate ? request.NoteId.Value : GetNextIdFromSequence(connection, transaction, "Notes", "NoteID");
                        var noteSerial = isUpdate
                            ? ExecuteScalarString(connection, transaction, "SELECT CONVERT(NVARCHAR(50), CAST(NoteSerial AS DECIMAL(38,0))) FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @NoteID AND NoteType = 50", new SqlParameter("@NoteID", noteId))
                            : GenerateNotesSerial(connection, transaction, request.BranchId, request.PaymentDate);
                        var noteSerial1 = isUpdate
                            ? ExecuteScalarString(connection, transaction, "SELECT CONVERT(NVARCHAR(50), CAST(NoteSerial1 AS DECIMAL(38,0))) FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @NoteID AND NoteType = 50", new SqlParameter("@NoteID", noteId))
                            : ExecuteScalarString(connection, transaction, "SELECT CONVERT(NVARCHAR(50), ISNULL(MAX(CAST(NoteSerial1 AS BIGINT)), 0) + 1) FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteType = 50 AND ISNUMERIC(NoteSerial1) = 1", null);

                        if (isUpdate && string.IsNullOrWhiteSpace(noteSerial1))
                        {
                            throw new InvalidOperationException("الحركة غير موجودة أو ليست من نوع التمويل والاستعاضة");
                        }

                        if (isUpdate)
                        {
                            var originalUserId = ExecuteScalarInt(connection, transaction, "SELECT UserID FROM dbo.Notes WHERE NoteID = @NoteID AND NoteType = 50", new SqlParameter("@NoteID", noteId));
                            ExecuteNonQuery(connection, transaction, @"
UPDATE dbo.Notes
SET
    branch_no = @BranchId,
    Note_Value = @Value,
    Remark = @Remarks,
    general_des_notes = @GeneralDescription,
    person = @Person,
    NoteDate = @NoteDate,
    CashingType = @CashingType,
    BTCashAccountcode = @BtCashAccount,
    BoxID = @BoxID,
    BankID = @BankID,
    ChqueNum = @ReferenceNo,
    DueDate = @ReferenceDate,
    NoteCashingType = @PaymentMethod,
    Emp_ID = @EmpID,
    AccountEmpCode = @EmpAccountCode,
    boxValue = @BoxValue,
    EmpValue = @EmpValue,
    UserID = @OriginalUserID,
    sanad_year = YEAR(@NoteDate),
    sanad_month = MONTH(@NoteDate),
    note_value_by_characters = CONVERT(NVARCHAR(50), @Value),
    LastModifiedByUserId = @UserID,
    LastModifiedDate = GETDATE()
WHERE NoteID = @NoteID
  AND NoteType = 50;",
                                new SqlParameter("@NoteID", noteId),
                                new SqlParameter("@BranchId", request.BranchId),
                                new SqlParameter("@Value", request.Value),
                                new SqlParameter("@Remarks", request.Remarks ?? string.Empty),
                                new SqlParameter("@GeneralDescription", request.GeneralDescription ?? string.Empty),
                                new SqlParameter("@Person", request.NameText ?? string.Empty),
                                new SqlParameter("@NoteDate", request.PaymentDate.Date),
                                new SqlParameter("@CashingType", request.CashingType),
                                new SqlParameter("@BtCashAccount", request.NameAccountCode ?? string.Empty),
                                new SqlParameter("@BoxID", request.PaymentMethod == 0 && request.BoxId.HasValue ? (object)request.BoxId.Value : DBNull.Value),
                                new SqlParameter("@BankID", request.PaymentMethod == 0 || !request.BankId.HasValue ? (object)DBNull.Value : request.BankId.Value),
                                new SqlParameter("@ReferenceNo", request.ReferenceNo ?? string.Empty),
                                new SqlParameter("@ReferenceDate", request.ReferenceDate.HasValue ? (object)request.ReferenceDate.Value.Date : DBNull.Value),
                                new SqlParameter("@PaymentMethod", request.PaymentMethod),
                                new SqlParameter("@EmpID", request.EmpId.HasValue ? (object)request.EmpId.Value : DBNull.Value),
                                new SqlParameter("@EmpAccountCode", request.EmpAccountCode ?? string.Empty),
                                new SqlParameter("@BoxValue", request.BoxValue),
                                new SqlParameter("@EmpValue", request.EmpValue),
                                new SqlParameter("@OriginalUserID", originalUserId.HasValue ? (object)originalUserId.Value : userId),
                                new SqlParameter("@UserID", userId));

                            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @NoteID", new SqlParameter("@NoteID", noteId));
                        }
                        else
                        {
                            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.Notes
(
    NoteID, branch_no, NoteSerial, NoteSerial1, OldNoteSerial1, Note_Value,
    Remark, general_des_notes, person, NoteType, NoteDate, CashingType,
    BTCashAccountcode, BoxID, BankID, ChqueNum, DueDate, NoteCashingType,
    Emp_ID, AccountEmpCode, boxValue, EmpValue, UserID, numbering_type,
    numbering_type1, sanad_year, sanad_month, note_value_by_characters
)
VALUES
(
    @NoteID, @BranchId, @NoteSerial, @NoteSerial1, @NoteSerial1, @Value,
    @Remarks, @GeneralDescription, @Person, 50, @NoteDate, @CashingType,
    @BtCashAccount, @BoxID, @BankID, @ReferenceNo, @ReferenceDate, @PaymentMethod,
    @EmpID, @EmpAccountCode, @BoxValue, @EmpValue, @UserID, 0,
    0, YEAR(@NoteDate), MONTH(@NoteDate), CONVERT(NVARCHAR(50), @Value)
);",
                            new SqlParameter("@NoteID", noteId),
                            new SqlParameter("@BranchId", request.BranchId),
                            new SqlParameter("@NoteSerial", ParseNullableDecimal(noteSerial).HasValue ? (object)ParseNullableDecimal(noteSerial).Value : DBNull.Value),
                            new SqlParameter("@NoteSerial1", noteSerial1 ?? string.Empty),
                            new SqlParameter("@Value", request.Value),
                            new SqlParameter("@Remarks", request.Remarks ?? string.Empty),
                            new SqlParameter("@GeneralDescription", request.GeneralDescription ?? string.Empty),
                            new SqlParameter("@Person", request.NameText ?? string.Empty),
                            new SqlParameter("@NoteDate", request.PaymentDate.Date),
                            new SqlParameter("@CashingType", request.CashingType),
                            new SqlParameter("@BtCashAccount", request.NameAccountCode ?? string.Empty),
                            new SqlParameter("@BoxID", request.PaymentMethod == 0 && request.BoxId.HasValue ? (object)request.BoxId.Value : DBNull.Value),
                            new SqlParameter("@BankID", request.PaymentMethod == 0 || !request.BankId.HasValue ? (object)DBNull.Value : request.BankId.Value),
                            new SqlParameter("@ReferenceNo", request.ReferenceNo ?? string.Empty),
                            new SqlParameter("@ReferenceDate", request.ReferenceDate.HasValue ? (object)request.ReferenceDate.Value.Date : DBNull.Value),
                            new SqlParameter("@PaymentMethod", request.PaymentMethod),
                            new SqlParameter("@EmpID", request.EmpId.HasValue ? (object)request.EmpId.Value : DBNull.Value),
                            new SqlParameter("@EmpAccountCode", request.EmpAccountCode ?? string.Empty),
                            new SqlParameter("@BoxValue", request.BoxValue),
                            new SqlParameter("@EmpValue", request.EmpValue),
                            new SqlParameter("@UserID", userId));
                        }

                        var voucherId = ExecuteScalarInt(connection, transaction, "SELECT ISNULL(MAX(Double_Entry_Vouchers_ID),0) + 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (UPDLOCK, HOLDLOCK)", null).GetValueOrDefault();
                        var lineNo = 1;
                        foreach (var line in lines)
                        {
                            AddPosPaymentDevLine(connection, transaction, voucherId, lineNo++, line, noteId, request.PaymentDate, userId, request.BranchId);
                        }

                        transaction.Commit();
                        return new PosPaymentResultDto { NoteId = noteId, NoteSerial = noteSerial, NoteSerial1 = noteSerial1, Lines = lines };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void ValidatePosPayment(PosPaymentRequestDto request)
        {
            if (request == null) { throw new InvalidOperationException("بيانات العملية غير مكتملة"); }
            if (request.BranchId <= 0) { throw new InvalidOperationException("الفرع مطلوب"); }
            if (request.PaymentDate == DateTime.MinValue) { request.PaymentDate = DateTime.Today; }
            if (request.CashingType != 5 && request.CashingType != 6) { throw new InvalidOperationException("نوع المدفوعات مطلوب"); }
            if (string.IsNullOrWhiteSpace(request.NameAccountCode)) { throw new InvalidOperationException("اسم/حساب العهدة أو الخزنة مطلوب"); }
            if (request.Value <= 0) { throw new InvalidOperationException("قيمة الدفع يجب أن تكون أكبر من صفر"); }
            if (request.PaymentMethod == 0 && !request.BoxId.HasValue) { throw new InvalidOperationException("الخزنة مطلوبة في الدفع النقدي"); }
            if (request.PaymentMethod != 0 && !request.BankId.HasValue) { throw new InvalidOperationException("البنك مطلوب"); }
            if ((request.PaymentMethod == 1 || request.PaymentMethod == 2 || request.PaymentMethod == 3) && string.IsNullOrWhiteSpace(request.ReferenceNo)) { throw new InvalidOperationException("رقم الشيك/الحوالة مطلوب"); }
            if (request.BoxValue + request.EmpValue > 0 && Math.Abs((request.BoxValue + request.EmpValue) - request.Value) > 0.01m) { throw new InvalidOperationException("مجموع قيمة الخزنة والعهدة يجب أن يساوي قيمة الدفع"); }
            if (request.EmpValue > 0 && string.IsNullOrWhiteSpace(request.EmpAccountCode)) { throw new InvalidOperationException("حساب عهدة الموظف مطلوب"); }
        }

        private IList<PosPaymentLineDto> GetPosPaymentLines(int noteId)
        {
            var lines = new List<PosPaymentLineDto>();
            const string sql = @"
SELECT
    d.Account_Code,
    acc.Account_Name,
    d.Double_Entry_Vouchers_Description,
    CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN CAST(ISNULL(d.Value, 0) AS DECIMAL(18,2)) ELSE 0 END AS Debit,
    CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 1 THEN CAST(ISNULL(d.Value, 0) AS DECIMAL(18,2)) ELSE 0 END AS Credit
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
LEFT JOIN dbo.ACCOUNTS acc ON acc.Account_Code = d.Account_Code
WHERE d.Notes_ID = @noteId
ORDER BY d.DEV_ID_Line_No;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lines.Add(new PosPaymentLineDto
                        {
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            Description = FixArabicMojibakeForDisplay(ReadString(reader, "Double_Entry_Vouchers_Description")),
                            Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                            Credit = ReadDecimal(reader, "Credit").GetValueOrDefault()
                        });
                    }
                }
            }

            return lines;
        }

        private void EnsurePosPaymentAuditColumns()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    EnsurePosPaymentAuditColumns(connection, transaction);
                    transaction.Commit();
                }
            }
        }

        private static void EnsurePosPaymentAuditColumns(SqlConnection connection, SqlTransaction transaction)
        {
            ExecuteNonQuery(connection, transaction, @"
IF COL_LENGTH('dbo.Notes', 'LastModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedByUserId INT NULL;
END;

IF COL_LENGTH('dbo.Notes', 'LastModifiedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedDate DATETIME NULL;
END;");
        }

        private string ResolvePaymentCreditAccount(PosPaymentRequestDto request)
        {
            if (request.PaymentMethod == 0)
            {
                return ExecuteScalarString("SELECT TOP (1) Account_Code FROM dbo.TblBoxesData WHERE BoxID = @id", new SqlParameter("@id", request.BoxId.GetValueOrDefault()));
            }

            return ExecuteScalarString("SELECT TOP (1) Account_Code FROM dbo.BanksData WHERE BankID = @id", new SqlParameter("@id", request.BankId.GetValueOrDefault()));
        }

        private string GetAccountName(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return string.Empty;
            }

            return ExecuteScalarString("SELECT TOP (1) Account_Name FROM dbo.ACCOUNTS WHERE Account_Code = @accountCode", new SqlParameter("@accountCode", accountCode)) ?? accountCode;
        }

        public IList<PosPermissionUserDto> GetPosPermissionUsers()
        {
            EnsurePosUserCategoryColumn();
            var users = new List<PosPermissionUserDto>();
            const string sql = @"
SELECT TOP (500)
    u.UserID,
    u.UserName,
    u.UserCategory,
    e.Emp_Name
FROM dbo.TblUsers u
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
WHERE ISNULL(u.isDeactivated, 0) = 0
ORDER BY u.UserName;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new PosPermissionUserDto
                        {
                            UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                            UserName = ReadString(reader, "UserName"),
                            EmpName = ReadString(reader, "Emp_Name"),
                            UserCategory = ReadString(reader, "UserCategory")
                        });
                    }
                }
            }

            return users;
        }

        public void SavePosUserCategory(int userId, string userCategory)
        {
            EnsurePosUserCategoryColumn();
            const string sql = @"
UPDATE dbo.TblUsers
SET UserCategory = NULLIF(LTRIM(RTRIM(@userCategory)), N'')
WHERE UserID = @userId;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@userCategory", SqlDbType.NVarChar, 50).Value = (object)(userCategory ?? string.Empty);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public IList<PosPermissionItemDto> GetPosUserTemporaryPermissionItems(int userId)
        {
            var saved = GetTemporaryPosPermissions(userId);
            saved["CustomerService"] = GetLegacyCustomerServicePermission(userId);
            saved["IsFullAccsesCustomerService"] = GetLegacyBooleanUserPermission(userId, "IsFullAccsesCustomerService");
            return BuildPosPermissionItems(saved);
        }

        public void SavePosUserTemporaryPermissions(int userId, IEnumerable<PosPermissionItemDto> permissions)
        {
            EnsurePosUserPermissionsTable();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        bool? customerService = null;
                        bool? fullAccessCustomerService = null;
                        ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.POS_UserPermissions WHERE UserID = @userId", new SqlParameter("@userId", userId));
                        foreach (var permission in permissions ?? new List<PosPermissionItemDto>())
                        {
                            if (permission == null || string.IsNullOrWhiteSpace(permission.Key))
                            {
                                continue;
                            }

                            if (string.Equals(permission.Key.Trim(), "CustomerService", StringComparison.OrdinalIgnoreCase))
                            {
                                customerService = permission.IsAllowed;
                                continue;
                            }

                            if (string.Equals(permission.Key.Trim(), "IsFullAccsesCustomerService", StringComparison.OrdinalIgnoreCase))
                            {
                                fullAccessCustomerService = permission.IsAllowed;
                                continue;
                            }

                            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
VALUES(@userId, @permissionKey, @isAllowed, GETDATE());",
                                new SqlParameter("@userId", userId),
                                new SqlParameter("@permissionKey", permission.Key.Trim()),
                                new SqlParameter("@isAllowed", permission.IsAllowed));
                        }

                        if (customerService.HasValue)
                        {
                            ExecuteNonQuery(connection, transaction, @"
UPDATE dbo.TblUsers
SET CustomerService = @customerService
WHERE UserID = @userId;",
                                new SqlParameter("@userId", userId),
                                new SqlParameter("@customerService", customerService.Value));
                        }

                        if (fullAccessCustomerService.HasValue)
                        {
                            ExecuteNonQuery(connection, transaction, @"
UPDATE dbo.TblUsers
SET IsFullAccsesCustomerService = @fullAccessCustomerService
WHERE UserID = @userId;",
                                new SqlParameter("@userId", userId),
                                new SqlParameter("@fullAccessCustomerService", fullAccessCustomerService.Value));
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private bool GetLegacyCustomerServicePermission(int userId)
        {
            const string sql = @"
SELECT TOP (1) ISNULL(CustomerService, 0) AS CustomerService
FROM dbo.TblUsers
WHERE UserID = @userId;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value && Convert.ToBoolean(value);
            }
        }

        private bool GetLegacyBooleanUserPermission(int userId, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || columnName.IndexOfAny(new[] { '[', ']', ';', '-', '\'' }) >= 0)
            {
                return false;
            }

            var sql = @"
SELECT TOP (1) ISNULL(" + columnName + @", 0) AS IsAllowed
FROM dbo.TblUsers
WHERE UserID = @userId;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                connection.Open();
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value && Convert.ToBoolean(value);
            }
        }

        private Tuple<int, int> ApplyLegacyCustomerServiceToCategory(string userCategory, bool isAllowed)
        {
            const string sql = @"
DECLARE @AffectedUsers INT;
DECLARE @UpdatedRows INT;

SELECT @AffectedUsers = COUNT(1)
FROM dbo.TblUsers
WHERE ISNULL(isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(UserCategory, N''))) = LTRIM(RTRIM(@userCategory));

UPDATE dbo.TblUsers
SET CustomerService = @isAllowed
WHERE ISNULL(isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(UserCategory, N''))) = LTRIM(RTRIM(@userCategory));

SET @UpdatedRows = @@ROWCOUNT;

DELETE p
FROM dbo.POS_UserPermissions p
INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
WHERE ISNULL(u.isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) = LTRIM(RTRIM(@userCategory))
  AND p.PermissionKey = N'CustomerService';

SELECT ISNULL(@AffectedUsers, 0) AS AffectedUsers, ISNULL(@UpdatedRows, 0) AS UpdatedRows;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userCategory", SqlDbType.NVarChar, 50).Value = userCategory ?? string.Empty;
                command.Parameters.Add("@isAllowed", SqlDbType.Bit).Value = isAllowed;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Tuple.Create(ReadInt(reader, "AffectedUsers").GetValueOrDefault(), ReadInt(reader, "UpdatedRows").GetValueOrDefault());
                    }
                }
            }

            return Tuple.Create(0, 0);
        }

        private Tuple<int, int> ApplyLegacyFullAccessCustomerServiceToCategory(string userCategory, bool isAllowed)
        {
            const string sql = @"
DECLARE @AffectedUsers INT;
DECLARE @UpdatedRows INT;

SELECT @AffectedUsers = COUNT(1)
FROM dbo.TblUsers
WHERE ISNULL(isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(UserCategory, N''))) = LTRIM(RTRIM(@userCategory));

UPDATE dbo.TblUsers
SET IsFullAccsesCustomerService = @isAllowed
WHERE ISNULL(isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(UserCategory, N''))) = LTRIM(RTRIM(@userCategory));

SET @UpdatedRows = @@ROWCOUNT;

DELETE p
FROM dbo.POS_UserPermissions p
INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
WHERE ISNULL(u.isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) = LTRIM(RTRIM(@userCategory))
  AND p.PermissionKey = N'IsFullAccsesCustomerService';

SELECT ISNULL(@AffectedUsers, 0) AS AffectedUsers, ISNULL(@UpdatedRows, 0) AS UpdatedRows;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userCategory", SqlDbType.NVarChar, 50).Value = userCategory ?? string.Empty;
                command.Parameters.Add("@isAllowed", SqlDbType.Bit).Value = isAllowed;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Tuple.Create(ReadInt(reader, "AffectedUsers").GetValueOrDefault(), ReadInt(reader, "UpdatedRows").GetValueOrDefault());
                    }
                }
            }

            return Tuple.Create(0, 0);
        }

        public Tuple<int, int> ApplyPosPermissionToCategory(string userCategory, string permissionKey, bool isAllowed)
        {
            EnsurePosUserCategoryColumn();
            EnsurePosUserPermissionsTable();
            if (string.Equals(permissionKey, "IsFullAccsesCustomerService", StringComparison.OrdinalIgnoreCase))
            {
                return ApplyLegacyFullAccessCustomerServiceToCategory(userCategory, isAllowed);
            }

            if (string.Equals(permissionKey, "CustomerService", StringComparison.OrdinalIgnoreCase))
            {
                return ApplyLegacyCustomerServiceToCategory(userCategory, isAllowed);
            }

            const string sql = @"
DECLARE @AffectedUsers INT;
DECLARE @UpdatedRows INT;

SELECT @AffectedUsers = COUNT(1)
FROM dbo.TblUsers
WHERE ISNULL(isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(UserCategory, N''))) = LTRIM(RTRIM(@userCategory));

UPDATE p
SET IsAllowed = @isAllowed,
    UpdatedAt = GETDATE()
FROM dbo.POS_UserPermissions p
INNER JOIN dbo.TblUsers u ON u.UserID = p.UserID
WHERE ISNULL(u.isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) = LTRIM(RTRIM(@userCategory))
  AND p.PermissionKey = @permissionKey;

SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO dbo.POS_UserPermissions(UserID, PermissionKey, IsAllowed, UpdatedAt)
SELECT u.UserID, @permissionKey, @isAllowed, GETDATE()
FROM dbo.TblUsers u
WHERE ISNULL(u.isDeactivated, 0) = 0
  AND LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) = LTRIM(RTRIM(@userCategory))
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.POS_UserPermissions p
      WHERE p.UserID = u.UserID
        AND p.PermissionKey = @permissionKey
  );

SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;

SELECT ISNULL(@AffectedUsers, 0) AS AffectedUsers, ISNULL(@UpdatedRows, 0) AS UpdatedRows;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@userCategory", SqlDbType.NVarChar, 50).Value = userCategory ?? string.Empty;
                command.Parameters.Add("@permissionKey", SqlDbType.NVarChar, 100).Value = permissionKey ?? string.Empty;
                command.Parameters.Add("@isAllowed", SqlDbType.Bit).Value = isAllowed;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Tuple.Create(ReadInt(reader, "AffectedUsers").GetValueOrDefault(), ReadInt(reader, "UpdatedRows").GetValueOrDefault());
                    }
                }
            }

            return Tuple.Create(0, 0);
        }

        public static IList<PosPermissionItemDto> BuildPosPermissionItems(IDictionary<string, bool> saved)
        {
            var definitions = new[]
            {
                new PosPermissionItemDto { Key = "CanViewReports", Title = "عرض شاشة التقارير" },
                new PosPermissionItemDto { Key = "CanReportSalesmen", Title = "تقرير المناديب" },
                new PosPermissionItemDto { Key = "CanReportClosings", Title = "تقرير الإغلاقات" },
                new PosPermissionItemDto { Key = "CanReportFinanceClosing", Title = "تقرير الإغلاق المالي" },
                new PosPermissionItemDto { Key = "CanReportDiscounts", Title = "تقرير الخصومات" },
                new PosPermissionItemDto { Key = "CanReportIndicators", Title = "تقرير المؤشرات العامة" },
                new PosPermissionItemDto { Key = "CanReportDailyTransactions", Title = "تقرير يومي بالحركات" },
                new PosPermissionItemDto { Key = "CanReportSalesComplete", Title = "تقرير المبيعات الشامل 1" },
                new PosPermissionItemDto { Key = "CanReportDailyTransactions2", Title = "تقرير يومي بالحركات 2" },
                new PosPermissionItemDto { Key = "CanReportDailyTransactionsSectors", Title = "تقرير المبيعات الشامل بالقطاعات" },
                new PosPermissionItemDto { Key = "CanReportAllSales", Title = "تقارير المبيعات العامة" },
                new PosPermissionItemDto { Key = "CanReportSalesComplete2", Title = "تقرير المبيعات الشامل 2" },
                new PosPermissionItemDto { Key = "CanReportSalesCompleteGovernorates", Title = "تقرير المبيعات الشامل بالمحافظات" },
                new PosPermissionItemDto { Key = "CanReportSalesCompleteDepartments", Title = "تقرير المبيعات الشامل بالإدارات" },
                new PosPermissionItemDto { Key = "CanReportSalesCompleteAnalytical", Title = "تقرير المبيعات تحليلي" },
                new PosPermissionItemDto { Key = "CanReportStoreSerials", Title = "تقرير سيريالات المخزن" },
                new PosPermissionItemDto { Key = "CanViewAccountingReports", Title = "عرض تقارير الحسابات" },
                new PosPermissionItemDto { Key = "CanViewTrialBalance", Title = "عرض ميزان مراجعة" },
                new PosPermissionItemDto { Key = "CanViewIncomeStatement", Title = "عرض قائمة الدخل" },
                new PosPermissionItemDto { Key = "CanViewAccountStatement", Title = "عرض كشف حساب" },
                new PosPermissionItemDto { Key = "CanViewGeneralLedgerAssistant", Title = "عرض الأستاذ العام المساعد" },
                new PosPermissionItemDto { Key = "CustomerService", Title = "CustomerService / خدمة العملاء" },
                new PosPermissionItemDto { Key = "IsFullAccsesCustomerService", Title = "صلاحية متابعة KYC والبنك" },
                new PosPermissionItemDto { Key = "CanEditKyc", Title = "التعديل في KYC" },
                new PosPermissionItemDto { Key = "CanPrintKycAcknowledgment", Title = "طباعة الإقرار" },
                new PosPermissionItemDto { Key = "CanPrintKycCard", Title = "طباعة الكارت" },
                new PosPermissionItemDto { Key = "CanViewJournalEntry", Title = "استعراض القيد المحاسبي" },
                new PosPermissionItemDto { Key = "CanCreateJournalEntry", Title = "إدخال قيد يومية" },
                new PosPermissionItemDto { Key = "CanEditJournalEntry", Title = "تعديل قيد يومية" },
                new PosPermissionItemDto { Key = "CanDeleteJournalEntry", Title = "حذف قيد يومية" },
                new PosPermissionItemDto { Key = "CanOpenClosing", Title = "فتح شاشة الإغلاق" },
                new PosPermissionItemDto { Key = "CanExecuteClosing", Title = "عمل الإغلاق" },
                new PosPermissionItemDto { Key = "CanOpenSales", Title = "فتح شاشة المبيعات" },
                new PosPermissionItemDto { Key = "CanSaveInvoice", Title = "حفظ فواتير البيع" },
                new PosPermissionItemDto { Key = "CanEditInvoice", Title = "تعديل فواتير البيع السابقة" },
                new PosPermissionItemDto { Key = "CanOpenPayments", Title = "فتح شاشة التمويل والاستعاضة" },
                new PosPermissionItemDto { Key = "CanExecutePayments", Title = "تنفيذ التمويل والاستعاضة" },
                new PosPermissionItemDto { Key = "CanEditPayments", Title = "تعديل حركات التمويل والاستعاضة السابقة" },
                new PosPermissionItemDto { Key = "CanTeller", Title = "Teller / بياع" },
                new PosPermissionItemDto { Key = "CanCancelOrReturn", Title = "عمل مرتجع/إلغاء" }
            };

            foreach (var item in definitions)
            {
                item.IsAllowed = IsTemporaryAllowed(saved, item.Key);
            }

            return definitions;
        }
        private void EnsurePosUserPermissionsTable()
        {
            const string sql = @"
IF OBJECT_ID(N'dbo.POS_UserPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_UserPermissions
    (
        UserID INT NOT NULL,
        PermissionKey NVARCHAR(100) NOT NULL,
        IsAllowed BIT NOT NULL CONSTRAINT DF_POS_UserPermissions_IsAllowed DEFAULT(0),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_UserPermissions_UpdatedAt DEFAULT(GETDATE()),
        CONSTRAINT PK_POS_UserPermissions PRIMARY KEY(UserID, PermissionKey)
    );
END";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void EnsurePosSystemErrorLogTable()
        {
            if (_posSystemErrorLogEnsured)
            {
                return;
            }

            lock (PosSystemErrorLogEnsureLock)
            {
                if (_posSystemErrorLogEnsured)
                {
                    return;
                }

            const string sql = @"
IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SystemErrorLog
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SystemErrorLog PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_SystemErrorLog_CreatedAt DEFAULT (GETDATE()),
        Severity NVARCHAR(20) NULL,
        Status NVARCHAR(40) NULL,
        UserId INT NULL,
        UserName NVARCHAR(100) NULL,
        BranchId INT NULL,
        ScreenName NVARCHAR(100) NULL,
        ActionName NVARCHAR(100) NULL,
        OperationType NVARCHAR(50) NULL,
        TransactionId INT NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        StackTrace NVARCHAR(MAX) NULL,
        RequestSummary NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(64) NULL,
        UserAgent NVARCHAR(512) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SystemErrorLog_CreatedAt' AND object_id = OBJECT_ID(N'dbo.POS_SystemErrorLog'))
BEGIN
    CREATE INDEX IX_POS_SystemErrorLog_CreatedAt ON dbo.POS_SystemErrorLog(CreatedAt DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_POS_SystemErrorLog_UserBranch' AND object_id = OBJECT_ID(N'dbo.POS_SystemErrorLog'))
BEGIN
    CREATE INDEX IX_POS_SystemErrorLog_UserBranch ON dbo.POS_SystemErrorLog(UserId, BranchId, CreatedAt DESC);
END;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }

                _posSystemErrorLogEnsured = true;
            }
        }

        private void EnsurePosUserCategoryColumn()
        {
            const string sql = @"
IF COL_LENGTH('dbo.TblUsers', 'UserCategory') IS NULL
BEGIN
    ALTER TABLE dbo.TblUsers ADD UserCategory NVARCHAR(50) NULL;
END";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void EnsureNotesAuditColumns()
        {
            const string sql = @"
IF COL_LENGTH('dbo.Notes', 'LastModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedByUserId INT NULL;
END;

IF COL_LENGTH('dbo.Notes', 'LastModifiedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedDate DATETIME NULL;
END;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public bool CanPrintTransaction(int transactionId, int userId, bool canChangeDefaults)
        {
            const string sql = @"
SELECT TOP (1) 1
FROM dbo.Transactions
WHERE Transaction_ID = @transactionId
  AND Transaction_Type = 21
  AND (@canChangeDefaults = 1 OR UserID = @userId);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        public bool TransactionHasDetails(int transactionId)
        {
            const string sql = @"
SELECT TOP (1) 1
FROM dbo.Transaction_Details
WHERE Transaction_ID = @transactionId;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@transactionId", SqlDbType.Int).Value = transactionId;
                connection.Open();
                return command.ExecuteScalar() != null;
            }
        }

        private static bool IsCashOutService(string serviceType)
        {
            return string.Equals((serviceType ?? string.Empty).Trim(), "cash-out", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCardService(string serviceType)
        {
            return string.Equals((serviceType ?? string.Empty).Trim(), "card", StringComparison.OrdinalIgnoreCase);
        }

        private static void NormalizeViolationServiceItem(PosItemLookupDto item)
        {
            item.Quantity = 1;
            item.Price = item.Price <= 0 ? 50 : item.Price;
            item.ShowPrice = item.Price;
            item.TotalPrice = item.Price;
            item.Vat = 0;
            item.Vatyo = 0;
            item.DiscountValue = 0;
            item.TotalDiscountPerLine = 0;
        }

        private static bool IsViolationsService(string serviceType)
        {
            return string.Equals((serviceType ?? string.Empty).Trim(), "violations", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSalesCommission(PosItemCommissionDto item)
        {
            return item.PercentVisa.HasValue || item.MinVisa.HasValue || item.MaxVisa.HasValue;
        }

        private static bool HasPurchaseCommission(PosItemCommissionDto item)
        {
            return item.PercentVisaPur.HasValue || item.MinVisaPur.HasValue || item.MaxVisaPur.HasValue;
        }

        private static decimal ApplyMinMax(decimal value, decimal min, decimal max)
        {
            if (min > 0 && value < min)
            {
                value = min;
            }

            if (max > 0 && value > max)
            {
                value = max;
            }

            return value;
        }

        private static string GetServiceFilter(string serviceType)
        {
            switch ((serviceType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "cash-out":
                    return "ISNULL(t.IsCashOut, 0) = 1";
                case "card":
                    return "NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL AND d.Item_ID <> 2";
                case "violations":
                    return "(ISNULL(i.TrafficViolations, 0) = 1 OR i.ItemName LIKE N'%مخالف%')";
                case "cash-in":
                default:
                    return "ISNULL(t.IsCashOut, 0) = 0 AND (ISNULL(t.isRecharg, 0) = 1 OR ISNULL(t.RechargeValue, 0) > 0) AND NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NULL AND ISNULL(i.TrafficViolations, 0) = 0";
            }
        }

        private static string NormalizeInvoiceOperationType(string operationType)
        {
            switch ((operationType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "cash-in":
                case "cash-out":
                case "card":
                case "violations":
                    return operationType.Trim().ToLowerInvariant();
                default:
                    return string.Empty;
            }
        }

        private static string GetPrimaryServiceWhere(string serviceType)
        {
            switch ((serviceType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "cash-out":
                    return "ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.IsPriceIsPerview, 0) = 1 AND ISNULL(ti.HaveGuarantee, 0) = 0";
                case "card":
                    return "(ti.ItemID IN (1, 19) OR (ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.HaveGuarantee, 0) = 1))";
                case "violations":
                    return "ISNULL(ti.TrafficViolations, 0) = 1";
                case "other-items":
                    return "ISNULL(ti.OtherItems, 0) = 1";
                case "cash-in":
                default:
                    return "ti.ItemType = 1 AND ti.ChkLot = 1 AND ISNULL(ti.IsPriceIsPerview, 0) = 0";
            }
        }

        private static PosItemLookupDto ReadItemLookup(IDataRecord reader)
        {
            return new PosItemLookupDto
            {
                Item_ID = ReadInt(reader, "Item_ID").GetValueOrDefault(),
                ItemName = ReadString(reader, "ItemName"),
                ItemCode = ReadString(reader, "ItemCode"),
                Quantity = ReadDecimal(reader, "Quantity").GetValueOrDefault(1),
                UnitId = ReadInt(reader, "UnitId"),
                UnitName = ReadString(reader, "UnitName"),
                Price = ReadDecimal(reader, "Price").GetValueOrDefault(),
                ShowPrice = ReadDecimal(reader, "ShowPrice").GetValueOrDefault(),
                TotalPrice = ReadDecimal(reader, "TotalPrice").GetValueOrDefault(),
                QtyBySmalltUnit = ReadDecimal(reader, "QtyBySmalltUnit").GetValueOrDefault(1),
                Vat = ReadDecimal(reader, "Vat").GetValueOrDefault(),
                Vatyo = ReadDecimal(reader, "Vatyo").GetValueOrDefault(),
                DiscountValue = ReadDecimal(reader, "DiscountValue").GetValueOrDefault(),
                TotalDiscountPerLine = ReadDecimal(reader, "TotalDiscountPerLine").GetValueOrDefault(),
                StoreID2 = ReadInt(reader, "StoreID2"),
                BranchId = ReadInt(reader, "BranchId"),
                ItemCase = ReadInt(reader, "ItemCase").GetValueOrDefault(1),
                CostPrice = ReadDecimal(reader, "CostPrice").GetValueOrDefault(),
                SavedItemType = ReadInt(reader, "SavedItemType").GetValueOrDefault()
            };
        }

        private static DateTime ParseTransactionDate(string value)
        {
            DateTime parsed;
            if (!string.IsNullOrWhiteSpace(value) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
            {
                return parsed;
            }

            return DateTime.Now;
        }

        private static void Add(SqlCommand command, string name, SqlDbType type, object value)
        {
            var parameter = command.Parameters.Add(name, type);
            parameter.Value = value ?? DBNull.Value;
        }

        private static void AddString(SqlCommand command, string name, SqlDbType type, int size, string value)
        {
            var parameter = command.Parameters.Add(name, type, size);
            parameter.Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private static void AddCashCustomerParameters(SqlCommand command, PosCashCustomerSaveRequest request)
        {
            var arabicName = string.IsNullOrWhiteSpace(request.Name)
                ? string.Join(" ", new[] { request.ArabicName0, request.ArabicName1, request.ArabicName2 }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim()
                : request.Name;
            var englishName = string.IsNullOrWhiteSpace(request.NameE)
                ? string.Join(" ", new[] { request.EnglishName0, request.EnglishName1, request.EnglishName2, request.EnglishName3 }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim()
                : request.NameE;

            AddString(command, "@name", SqlDbType.NVarChar, 255, arabicName);
            AddString(command, "@namee", SqlDbType.NVarChar, 255, englishName);
            AddString(command, "@arabicName0", SqlDbType.NVarChar, 255, request.ArabicName0);
            AddString(command, "@arabicName1", SqlDbType.NVarChar, 255, request.ArabicName1);
            AddString(command, "@arabicName2", SqlDbType.NVarChar, 255, request.ArabicName2);
            AddString(command, "@arabicName3", SqlDbType.NVarChar, 255, request.ArabicName3);
            AddString(command, "@englishName0", SqlDbType.NVarChar, 255, request.EnglishName0);
            AddString(command, "@englishName1", SqlDbType.NVarChar, 255, request.EnglishName1);
            AddString(command, "@englishName2", SqlDbType.NVarChar, 255, request.EnglishName2);
            AddString(command, "@englishName3", SqlDbType.NVarChar, 255, request.EnglishName3);
            AddString(command, "@englishName5", SqlDbType.NVarChar, 255, request.EnglishName5);
            AddString(command, "@englishName6", SqlDbType.NVarChar, 255, request.EnglishName6);
            AddString(command, "@englishName7", SqlDbType.NVarChar, 255, request.EnglishName7);
            AddString(command, "@phoneNo2", SqlDbType.NVarChar, 50, request.PhoneNo2);
            AddString(command, "@phoneNo", SqlDbType.NVarChar, 50, request.PhoneNo);
            AddString(command, "@cardNo", SqlDbType.NVarChar, 255, request.CardNo);
            AddString(command, "@cardId", SqlDbType.NVarChar, 255, string.IsNullOrWhiteSpace(request.CardId) ? request.CardNo : request.CardId);
            AddString(command, "@cardSource", SqlDbType.NVarChar, 255, request.CardSource);
            AddString(command, "@tetNumPoket", SqlDbType.NVarChar, 255, request.Tet_NumPoket);
            AddString(command, "@address", SqlDbType.NVarChar, 255, request.Address);
            AddString(command, "@mailAdress", SqlDbType.NVarChar, 255, request.MailAdress);
            Add(command, "@nationality", SqlDbType.Int, request.Nationality);
            Add(command, "@birthDate", SqlDbType.DateTime, request.BirthDate);
            Add(command, "@cardDate", SqlDbType.DateTime, request.CardDate);
            Add(command, "@cardEndDate", SqlDbType.DateTime, request.CardEndDate);
            Add(command, "@orderDate", SqlDbType.DateTime, request.OrderDate ?? DateTime.Today);
            AddString(command, "@tel", SqlDbType.NVarChar, 50, request.Tel);
            AddString(command, "@card", SqlDbType.NVarChar, 255, request.Card);
            Add(command, "@branchId", SqlDbType.Int, request.BranchId);
            Add(command, "@userId", SqlDbType.Int, request.UserId);
            Add(command, "@empId", SqlDbType.Int, request.EmpId);
        }

        private static PosCustomerLookupDto MapKeshniCardCustomer(IDataRecord reader)
        {
            var cardNo = ReadString(reader, "CardNo");
            var cardId = ReadString(reader, "CardId");

            return new PosCustomerLookupDto
            {
                CustomerID = ReadInt(reader, "Id").GetValueOrDefault(),
                Name = ReadString(reader, "name"),
                NameE = ReadString(reader, "namee"),
                ArabicName0 = ReadString(reader, "ArabicName0"),
                ArabicName1 = ReadString(reader, "ArabicName1"),
                ArabicName2 = ReadString(reader, "ArabicName2"),
                ArabicName3 = ReadString(reader, "ArabicName3"),
                EnglishName0 = ReadString(reader, "EnglishName0"),
                EnglishName1 = ReadString(reader, "EnglishName1"),
                EnglishName2 = ReadString(reader, "EnglishName2"),
                EnglishName3 = ReadString(reader, "EnglishName3"),
                EnglishName5 = ReadString(reader, "EnglishName5"),
                EnglishName6 = ReadString(reader, "EnglishName6"),
                EnglishName7 = ReadString(reader, "EnglishName7"),
                CustomerName = ReadString(reader, "CustomerName"),
                Phone = ReadString(reader, "Phone"),
                Phone2 = ReadString(reader, "PhoneNo2"),
                // TblCusCsh.CardId/CardNo are Keshni card identifiers, not the POS screen ID.
                IPN = null,
                VisaNumber = string.IsNullOrWhiteSpace(cardNo) ? cardId : cardNo,
                CardSerial = ReadString(reader, "card"),
                CardNo = cardNo,
                CardId = cardId,
                CardSource = ReadString(reader, "CardSource"),
                Tel = ReadString(reader, "tel"),
                Tet_NumPoket = ReadString(reader, "Tet_NumPoket"),
                Address = ReadString(reader, "Address"),
                MailAdress = ReadString(reader, "MailAdress"),
                Nationality = ReadInt(reader, "Nationality"),
                BirthDate = ReadDateTime(reader, "BirthDate"),
                CardDate = ReadDateTime(reader, "CardDate"),
                CardEndDate = ReadDateTime(reader, "CardEndDate"),
                OrderDate = ReadDateTime(reader, "OrderDate"),
                EasyCashType = ReadInt(reader, "EasyCashType"),
                BranchId = ReadInt(reader, "BranchID"),
                BranchName = ReadString(reader, "BranchName")
            };
        }

        private static PosKycAttachmentDto MapKeshniAttachment(IDataRecord reader)
        {
            return new PosKycAttachmentDto
            {
                Id = ReadInt(reader, "Id").GetValueOrDefault(),
                SubjectNo = ReadString(reader, "subject_no"),
                FileName = ReadString(reader, "image_NAME"),
                ImageDate = ReadDateTime(reader, "image_date"),
                Department = ReadString(reader, "DEPARTEMENT"),
                ImageTitle = ReadString(reader, "image_Title"),
                OperationType = ReadString(reader, "operation_type")
            };
        }

        private static object DbValue(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static double ToDouble(decimal value)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static double? ToNullableDouble(decimal? value)
        {
            return value.HasValue ? (double?)ToDouble(value.Value) : null;
        }

        private static double? ToNullableDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            double parsed;
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ? (double?)parsed : null;
        }

        private static string FormatBalance(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        private static int ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteNonQuery();
            }
        }

        private string ExecuteScalarString(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                connection.Open();
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string ExecuteScalarString(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static int? ExecuteScalarInt(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static int GetNextIdFromSequence(SqlConnection connection, SqlTransaction transaction, string tableName, string fieldName)
        {
            using (var command = new SqlCommand("dbo.GetNextID_FromSequence", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 100).Value = tableName;
                command.Parameters.Add("@FieldName", SqlDbType.NVarChar, 100).Value = fieldName;
                var nextValue = command.Parameters.Add("@NextValue", SqlDbType.BigInt);
                nextValue.Direction = ParameterDirection.Output;
                var error = command.Parameters.Add("@ErrorMsg", SqlDbType.NVarChar, 500);
                error.Direction = ParameterDirection.Output;
                command.ExecuteNonQuery();
                if (error.Value != DBNull.Value && !string.IsNullOrWhiteSpace(Convert.ToString(error.Value, CultureInfo.InvariantCulture)))
                {
                    throw new InvalidOperationException(Convert.ToString(error.Value, CultureInfo.InvariantCulture));
                }

                return Convert.ToInt32(nextValue.Value, CultureInfo.InvariantCulture);
            }
        }

        private static string GenerateNotesSerial(SqlConnection connection, SqlTransaction transaction, int branchId, DateTime noteDate)
        {
            using (var command = new SqlCommand("dbo.usp_Notes_coding_V2", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@my_branch", SqlDbType.Int).Value = branchId;
                command.Parameters.Add("@date1", SqlDbType.Date).Value = noteDate.Date;
                command.Parameters.Add("@departement_name", SqlDbType.Int).Value = 0;
                var result = command.Parameters.Add("@Result", SqlDbType.VarChar, 50);
                result.Direction = ParameterDirection.InputOutput;
                result.Value = string.Empty;
                command.ExecuteNonQuery();
                return Convert.ToString(result.Value, CultureInfo.InvariantCulture);
            }
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            decimal parsed;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ? (decimal?)parsed : null;
        }

        private static void AddPosPaymentDevLine(SqlConnection connection, SqlTransaction transaction, int voucherId, int lineNo, PosPaymentLineDto line, int noteId, DateTime recordDate, int userId, int branchId)
        {
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
    Notes_ID, UserID, currency, rate, branch_id, DueDate
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value,
    @CreditOrDebit, @Description, @RecordDate,
    @NoteId, @UserId, N'', 1, @BranchId, @RecordDate
);",
                new SqlParameter("@VoucherId", voucherId),
                new SqlParameter("@LineNo", lineNo),
                new SqlParameter("@AccountCode", line.AccountCode ?? string.Empty),
                new SqlParameter("@Value", line.Debit > 0 ? line.Debit : line.Credit),
                new SqlParameter("@CreditOrDebit", line.Debit > 0 ? 0 : 1),
                new SqlParameter("@Description", line.Description ?? string.Empty),
                new SqlParameter("@RecordDate", recordDate.Date),
                new SqlParameter("@NoteId", noteId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@BranchId", branchId));
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string FixArabicMojibakeForDisplay(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var fixedValue = value;
            for (var i = 0; i < 2 && LooksLikeArabicMojibake(fixedValue); i++)
            {
                try
                {
                    fixedValue = Encoding.UTF8.GetString(Encoding.GetEncoding(1256).GetBytes(fixedValue));
                }
                catch
                {
                    return value;
                }
            }

            return string.IsNullOrWhiteSpace(fixedValue) ? value : fixedValue;
        }

        private static bool LooksLikeArabicMojibake(string value)
        {
            return value.IndexOf("ط¸", StringComparison.Ordinal) >= 0
                || value.IndexOf("ط·", StringComparison.Ordinal) >= 0
                || value.IndexOf("ظ¾", StringComparison.Ordinal) >= 0
                || value.IndexOf("ظپ", StringComparison.Ordinal) >= 0
                || value.IndexOf("آ", StringComparison.Ordinal) >= 0
                || value.IndexOf("€", StringComparison.Ordinal) >= 0;
        }

        private static DateTime? ReadDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBoolean(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }
    }
}


