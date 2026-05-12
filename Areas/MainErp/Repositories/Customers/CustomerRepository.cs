using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.Customers;

namespace MyERP.Areas.MainErp.Repositories.Customers
{
    public class CustomerRepository
    {
        private const string CustomerParentAccount = "a1a2a2";
        private const string SupplierParentAccount = "a2a2a1";
        private const string OpeningBalanceAccount = "a5a2a5";
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public CustomerRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public CustomersIndexViewModel LoadIndex(string searchText, int? customerType, int? branchId, int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;
            var model = new CustomersIndexViewModel
            {
                SearchText = searchText,
                CustomerType = customerType,
                BranchId = branchId,
                Page = page,
                PageSize = pageSize
            };

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadLookups(connection, model);
                int totalCount;
                model.Results = Search(connection, searchText, customerType, branchId, page, pageSize, out totalCount);
                model.TotalCount = totalCount;
                model.Selected = NewModel(connection, null);
            }

            return model;
        }

        public CustomerEditViewModel New()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return NewModel(connection, null);
            }
        }

        public CustomerEditViewModel Get(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return Get(connection, null, id);
            }
        }

        public CustomerSaveResult Save(CustomerEditViewModel request, MainErpUserContext user)
        {
            var validation = Validate(request);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                return Fail(validation);
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var isNew = !request.CusId.HasValue || request.CusId.Value <= 0 || !Exists(connection, transaction, request.CusId.Value);

                    if (isNew)
                    {
                        request.CusId = NextId(connection, transaction, "TblCustemers", "CusID");
                        PrepareLegacyCode(request);
                        EnsureUniqueLegacyValues(connection, transaction, request);
                        if (string.IsNullOrWhiteSpace(request.AccountCode))
                        {
                            request.AccountCode = CreateAccount(connection, transaction, request);
                        }

                        InsertCustomer(connection, transaction, request, user);
                    }
                    else
                    {
                        PrepareLegacyCode(request);
                        EnsureUniqueLegacyValues(connection, transaction, request);
                        var current = Get(connection, transaction, request.CusId.Value);
                        if (current == null)
                        {
                            transaction.Rollback();
                            return Fail("لم يتم العثور على العميل المطلوب تعديله.");
                        }

                        if (string.IsNullOrWhiteSpace(request.AccountCode))
                        {
                            request.AccountCode = current.AccountCode;
                        }

                        UpdateCustomer(connection, transaction, request, user);
                    }

                    transaction.Commit();
                    return new CustomerSaveResult
                    {
                        Success = true,
                        Message = "تم حفظ بيانات العميل بنجاح.",
                        CusId = request.CusId,
                        Customer = Get(request.CusId.Value)
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ بيانات العميل: " + ex.Message);
                }
            }
        }

        public CustomerSaveResult Delete(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var current = Get(connection, transaction, id);
                    if (current == null)
                    {
                        return Fail("لم يتم العثور على العميل المطلوب حذفه.");
                    }

                    var usage = ExecuteScalar<int>(connection, transaction, @"
SELECT
    (SELECT COUNT(1) FROM dbo.Transactions WHERE CusID = @Id)
  + (SELECT COUNT(1) FROM dbo.Notes WHERE CusID = @Id)
  + (SELECT COUNT(1) FROM dbo.TblJobOrders WHERE CusID = @Id)
  + (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Account_Code = @AccountCode)
  + (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS1 WHERE Account_Code = @AccountCode)
  + (SELECT COUNT(1) FROM dbo.projects WHERE End_user_id = CONVERT(nvarchar(20), @Id));",
                        new SqlParameter("@Id", SqlDbType.Int) { Value = id },
                        new SqlParameter("@AccountCode", SqlDbType.NVarChar, 50) { Value = DbText(current.AccountCode) });

                    if (usage > 0)
                    {
                        transaction.Rollback();
                        return Fail("لا يمكن حذف هذا العميل لأنه مستخدم في حركات أو سندات قائمة.");
                    }

                    ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.TblCustemers WHERE CusID = @Id",
                        new SqlParameter("@Id", SqlDbType.Int) { Value = id });
                    if (!string.IsNullOrWhiteSpace(current.AccountCode))
                    {
                        ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.ACCOUNTS WHERE Account_Code = @AccountCode",
                            new SqlParameter("@AccountCode", SqlDbType.NVarChar, 50) { Value = current.AccountCode });
                    }
                    transaction.Commit();
                    return new CustomerSaveResult { Success = true, Message = "تم حذف العميل بنجاح.", CusId = id };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف العميل: " + ex.Message);
                }
            }
        }

        public IList<CustomerLookupItemViewModel> LoadBranches()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id");
            }
        }

        private static IList<CustomerListItemViewModel> Search(SqlConnection connection, string searchText, int? customerType, int? branchId, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var items = new List<CustomerListItemViewModel>();
            using (var command = new SqlCommand(@"
WITH CustomerRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY c.CusID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        c.CusID, c.CusName, c.CusNamee, c.Cus_Phone, c.Cus_mobile, c.Type, c.BranchId, c.Fullcode,
        br.branch_name AS BranchName, c.Account_Code, c.OpenBalance, c.RecordDate, c.NationalNo, c.VATNO,
        ISNULL(c.locked, 0) AS locked,
        a.Account_Serial, COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
    FROM dbo.TblCustemers c
    LEFT JOIN dbo.TblBranchesData br ON br.branch_id = c.BranchId
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = c.Account_Code
    WHERE (@SearchText IS NULL
           OR c.CusName LIKE @SearchLike
           OR c.CusNamee LIKE @SearchLike
           OR c.Cus_Phone LIKE @SearchLike
           OR c.Cus_mobile LIKE @SearchLike
           OR c.Fullcode LIKE @SearchLike
           OR CONVERT(nvarchar(50), c.CustGID) LIKE @SearchLike
           OR c.NationalNo LIKE @SearchLike
           OR c.VATNO LIKE @SearchLike
           OR CONVERT(nvarchar(20), c.CusID) = @SearchText)
      AND (@CustomerType IS NULL OR c.Type = @CustomerType)
      AND (@BranchId IS NULL OR c.BranchId = @BranchId)
)
SELECT *
FROM CustomerRows
WHERE RowNo BETWEEN @StartRow AND @EndRow
ORDER BY RowNo;", connection))
            {
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 4000).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                command.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 4000).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText.Trim() + "%";
                command.Parameters.Add("@CustomerType", SqlDbType.Int).Value = (object)customerType ?? DBNull.Value;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@StartRow", SqlDbType.Int).Value = ((page - 1) * pageSize) + 1;
                command.Parameters.Add("@EndRow", SqlDbType.Int).Value = page * pageSize;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (totalCount == 0)
                        {
                            totalCount = ReadInt(reader, "TotalCount").GetValueOrDefault();
                        }

                        items.Add(new CustomerListItemViewModel
                        {
                            CusId = ReadInt(reader, "CusID").GetValueOrDefault(),
                            CusName = ReadString(reader, "CusName"),
                            CusNameEnglish = ReadString(reader, "CusNamee"),
                            Phone = ReadString(reader, "Cus_Phone"),
                            Mobile = ReadString(reader, "Cus_mobile"),
                            Type = ReadInt(reader, "Type"),
                            TypeName = FormatType(ReadInt(reader, "Type")),
                            BranchName = ReadString(reader, "BranchName"),
                            FullCode = ReadString(reader, "Fullcode"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountDisplay = FormatAccount(ReadString(reader, "Account_Serial"), ReadString(reader, "AccountName"), ReadString(reader, "Account_Code")),
                            OpenBalance = ReadDouble(reader, "OpenBalance"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            NationalNo = ReadString(reader, "NationalNo"),
                            VatNo = ReadString(reader, "VATNO"),
                            IsLocked = ReadBool(reader, "locked")
                        });
                    }
                }
            }

            return items;
        }

        private static CustomerEditViewModel NewModel(SqlConnection connection, SqlTransaction transaction)
        {
            return new CustomerEditViewModel
            {
                Type = 1,
                BranchId = FirstInt(connection, transaction, "SELECT TOP (1) branch_id FROM dbo.TblBranchesData ORDER BY branch_id"),
                RecordDate = DateTime.Today,
                OpenBalanceDate = DateTime.Today,
                OpenBalance = 0,
                OpenBalance1 = 0,
                OpenBalance2 = 0,
                CreditLimit = 0,
                CreditLimitCredit = 0,
                DiscountType = 0,
                PurchaseDiscountType = 0,
                PaymentType = 1,
                IdentificationCode = "SA",
                ParentAccount = CustomerParentAccount
            };
        }

        private static CustomerEditViewModel Get(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
    c.*, a.Account_Serial, COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM dbo.TblCustemers c
LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = c.Account_Code
WHERE c.CusID = @Id;", connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new CustomerEditViewModel
                    {
                        CusId = ReadInt(reader, "CusID"),
                        CusName = ReadString(reader, "CusName"),
                        CusNameEnglish = ReadString(reader, "CusNamee"),
                        ResponsibleContact = ReadString(reader, "ResponsibleContact"),
                        Phone = ReadString(reader, "Cus_Phone"),
                        Mobile = ReadString(reader, "Cus_mobile"),
                        FaxNumber = ReadString(reader, "FaxNumber"),
                        Email = ReadString(reader, "E_mail"),
                        Address = ReadString(reader, "Address"),
                        AddressEnglish = ReadString(reader, "AddressE"),
                        Remark = ReadString(reader, "Remark"),
                        Remark2 = ReadString(reader, "Remark2"),
                        Type = ReadInt(reader, "Type").GetValueOrDefault(1),
                        Prefix = ReadString(reader, "prifix"),
                        Code = ReadString(reader, "code"),
                        FullCode = ReadString(reader, "Fullcode"),
                        CustomerAndVendor = ReadBool(reader, "CustomerandVendor"),
                        BranchId = ReadInt(reader, "BranchId"),
                        BranchName = ReadString(reader, "BranchName"),
                        RecordDate = ReadDate(reader, "RecordDate"),
                        OpenBalance = ReadDouble(reader, "OpenBalance"),
                        OpenBalanceType = ReadInt(reader, "OpenBalanceType"),
                        OpenBalance1 = ReadDouble(reader, "OpenBalance1"),
                        OpenBalanceType1 = ReadInt(reader, "OpenBalanceType1"),
                        OpenBalance2 = ReadDouble(reader, "OpenBalance2"),
                        OpenBalanceType2 = ReadInt(reader, "OpenBalanceType2"),
                        OpenBalanceDate = ReadDate(reader, "OpenBalanceDate"),
                        CreditLimit = ReadDouble(reader, "CreditLimit"),
                        CreditLimitCredit = ReadDouble(reader, "CreditlimitCredit"),
                        DebitInterval = ReadInt(reader, "DepitInterval"),
                        CreditInterval = ReadInt(reader, "CreditInterval"),
                        PaymentType = ReadInt(reader, "CPaymentType"),
                        AccountCode = ReadString(reader, "Account_Code"),
                        AccountAsClient = ReadString(reader, "Account_Code_As_Client"),
                        AccountAsSupplier = ReadString(reader, "Account_Code_As_Supplier"),
                        ParentAccount = ReadString(reader, "parent_account"),
                        AccountDisplay = FormatAccount(ReadString(reader, "Account_Serial"), ReadString(reader, "AccountName"), ReadString(reader, "Account_Code")),
                        CustomerTypeId = ReadInt(reader, "CustomerTypeID"),
                        TypeCustomer = ReadInt(reader, "TypeCustomer"),
                        ClassCustomersId = ReadInt(reader, "ClassCustomersId"),
                        GroupsCustomersId = ReadInt(reader, "GroupsCustomersId"),
                        SaleType = ReadInt(reader, "SaleType"),
                        DiscountType = ReadInt(reader, "Trans_DiscountType"),
                        DiscountValue = ReadDouble(reader, "Trans_Discount"),
                        PurchaseDiscountType = ReadInt(reader, "Trans_DiscountTypePur"),
                        PurchaseDiscountValue = ReadDouble(reader, "Trans_DiscountPur"),
                        NationalNo = ReadString(reader, "NationalNo"),
                        VatNo = ReadString(reader, "VATNO"),
                        GeneralTaxNo = ReadString(reader, "txtGeneralTax"),
                        TaxCardNo = ReadString(reader, "txtTaxC"),
                        TaxStampNo = ReadString(reader, "txtTaxStamp"),
                        WorkEarningTaxesNo = ReadString(reader, "txtWorkEarningTaxes"),
                        CommercialRegisterNo = ReadString(reader, "txtTaxNo1"),
                        ImportCardNo = ReadString(reader, "txtCardImportNo"),
                        ExportCardNo = ReadString(reader, "txtCardExportNo"),
                        TaxExempt = ReadBool(reader, "chkTaxExempt"),
                        CountryId = ReadInt(reader, "CountryID"),
                        GovernmentId = ReadInt(reader, "GovernmentID"),
                        CityId = ReadInt(reader, "CityID"),
                        CountryId2 = ReadInt(reader, "CountryID2"),
                        StreetName = ReadString(reader, "StreetName"),
                        AdditionalStreetName = ReadString(reader, "AdditionalStreetName"),
                        BuildingNumber = ReadString(reader, "BuildingNumber"),
                        PlotIdentification = ReadString(reader, "PlotIdentification"),
                        CityName = ReadString(reader, "CityName"),
                        CitySubdivisionName = ReadString(reader, "CitySubdivisionName"),
                        PostalZone = ReadString(reader, "PostalZone"),
                        CountrySubentity = ReadString(reader, "CountrySubentity"),
                        IdentificationCode = ReadString(reader, "IdentificationCode"),
                        Id700 = ReadString(reader, "Id700"),
                        BoxMil = ReadString(reader, "BoxMil"),
                        ZipCode = ReadString(reader, "ZipCode"),
                        BankName = ReadString(reader, "BankName"),
                        BankAccount = ReadString(reader, "BankAccount"),
                        BankCode = ReadString(reader, "BankCode"),
                        BankIban = ReadString(reader, "BankIBAN"),
                        BankAddress = ReadString(reader, "BankAddress"),
                        Iban = ReadString(reader, "IBAN"),
                        IsLocked = ReadBool(reader, "locked"),
                        CreditLocked = ReadBool(reader, "creditlocked"),
                        Export = ReadInt(reader, "export").GetValueOrDefault() == 1
                    };
                }
            }
        }

        private static void LoadLookups(SqlConnection connection, CustomersIndexViewModel model)
        {
            model.Branches = LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id");
            model.Classes = LoadLookup(connection, "SELECT ID, Name FROM dbo.ClassCustomers ORDER BY ID");
            model.Groups = LoadLookup(connection, "SELECT GroupID, GroupName FROM dbo.GroupsCustomers ORDER BY GroupID");
            model.Countries = LoadLookup(connection, "SELECT CountryID, CountryName FROM dbo.TblCountriesData ORDER BY CountryName");
            model.Governments = LoadLookup(connection, "SELECT GovernmentID, GovernmentName FROM dbo.TblCountriesGovernments ORDER BY GovernmentName");
            model.Cities = LoadLookup(connection, "SELECT CityID, CityName FROM dbo.TblCountriesGovernmentsCities ORDER BY CityName");
        }

        private static IList<CustomerLookupItemViewModel> LoadLookup(SqlConnection connection, string sql)
        {
            var items = new List<CustomerLookupItemViewModel>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new CustomerLookupItemViewModel
                    {
                        Id = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture),
                        Text = reader.IsDBNull(1) ? Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) : Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture)
                    });
                }
            }

            return items;
        }

        private static void InsertCustomer(SqlConnection connection, SqlTransaction transaction, CustomerEditViewModel request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(CustomerInsertSql, connection, transaction))
            {
                AddCustomerParameters(command, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateCustomer(SqlConnection connection, SqlTransaction transaction, CustomerEditViewModel request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(CustomerUpdateSql, connection, transaction))
            {
                AddCustomerParameters(command, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static string CreateAccount(SqlConnection connection, SqlTransaction transaction, CustomerEditViewModel request)
        {
            var parent = ResolveParentAccount(connection, transaction, request.Type);
            var accountCode = GenerateAccountCode(connection, transaction, parent);
            var serial = GenerateAccountSerial(connection, transaction, parent);
            var accountTypeValues = LoadParentAccountTypes(connection, transaction, parent);

            using (var command = new SqlCommand(@"
INSERT INTO dbo.ACCOUNTS
(Account_Code, Account_Name, Parent_Account_Code, last_account, cannot_del, Branch, Account_Serial,
 BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna, cost_center, Sum_account,
 cost_center_type, cost_center_id, ActivityTypeId, AccountTypes, AccountTab, DepitOrCredit,
 Differenttype, Authority, UserGroupId, Userid, [Block], [Level])
VALUES
(@Account_Code, @Account_Name, @Parent_Account_Code, 1, 0, '0', @Account_Serial,
 0, GETDATE(), @Account_NameEng, N'1', 0, 0, 0,
 0, NULL, NULL, @AccountTypes, @AccountTab, @DepitOrCredit,
 @Differenttype, @Authority, NULL, NULL, 0, @Level);", connection, transaction))
            {
                command.Parameters.Add("@Account_Code", SqlDbType.NVarChar, 50).Value = accountCode;
                command.Parameters.Add("@Account_Name", SqlDbType.NVarChar, 4000).Value = request.CusName.Trim();
                command.Parameters.Add("@Parent_Account_Code", SqlDbType.NVarChar, 70).Value = parent;
                command.Parameters.Add("@Account_Serial", SqlDbType.NVarChar, 4000).Value = serial;
                command.Parameters.Add("@Account_NameEng", SqlDbType.NVarChar, 4000).Value = string.IsNullOrWhiteSpace(request.CusNameEnglish) ? request.CusName.Trim() : request.CusNameEnglish.Trim();
                command.Parameters.Add("@AccountTypes", SqlDbType.Int).Value = (object)accountTypeValues.AccountTypes ?? DBNull.Value;
                command.Parameters.Add("@AccountTab", SqlDbType.Int).Value = (object)accountTypeValues.AccountTab ?? DBNull.Value;
                command.Parameters.Add("@DepitOrCredit", SqlDbType.Int).Value = (object)accountTypeValues.DepitOrCredit ?? DBNull.Value;
                command.Parameters.Add("@Differenttype", SqlDbType.Int).Value = (object)accountTypeValues.Differenttype ?? DBNull.Value;
                command.Parameters.Add("@Authority", SqlDbType.Int).Value = (object)accountTypeValues.Authority ?? DBNull.Value;
                command.Parameters.Add("@Level", SqlDbType.Int).Value = CountA(accountCode);
                command.ExecuteNonQuery();
            }

            request.ParentAccount = parent;
            return accountCode;
        }

        private static string ResolveParentAccount(SqlConnection connection, SqlTransaction transaction, int type)
        {
            var preferred = type == 2 ? SupplierParentAccount : CustomerParentAccount;
            var exists = ExecuteScalar<int>(connection, transaction,
                "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Code = @Code AND ISNULL(last_account, 0) = 0",
                new SqlParameter("@Code", SqlDbType.NVarChar, 50) { Value = preferred });
            if (exists > 0)
            {
                return preferred;
            }

            throw new InvalidOperationException("الحساب الرئيسي المطلوب لإنشاء حساب العميل غير موجود أو حساب نهائي: " + preferred);
        }

        private static string GenerateAccountCode(SqlConnection connection, SqlTransaction transaction, string parent)
        {
            var maxSuffix = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK) WHERE Parent_Account_Code = @Parent", connection, transaction))
            {
                command.Parameters.Add("@Parent", SqlDbType.NVarChar, 70).Value = parent;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "Account_Code");
                        var lastA = code.LastIndexOf('a');
                        int suffix;
                        if (lastA >= 0 && int.TryParse(code.Substring(lastA + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out suffix) && suffix > maxSuffix)
                        {
                            maxSuffix = suffix;
                        }
                    }
                }
            }

            return parent + "a" + (maxSuffix + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string GenerateAccountSerial(SqlConnection connection, SqlTransaction transaction, string parent)
        {
            var parentSerial = ExecuteScalar<string>(connection, transaction,
                "SELECT TOP (1) Account_Serial FROM dbo.ACCOUNTS WHERE Account_Code = @Code",
                new SqlParameter("@Code", SqlDbType.NVarChar, 50) { Value = parent }) ?? string.Empty;
            var level = CountA(parent) + 1;
            var digits = ExecuteScalar<int>(connection, transaction,
                "SELECT TOP (1) ISNULL(NoOfDigits, 1) FROM dbo.AccountsLevelsDetails WHERE [Level] = @Level ORDER BY id",
                new SqlParameter("@Level", SqlDbType.Int) { Value = level });
            if (digits <= 0)
            {
                digits = 1;
            }

            var maxNumber = 0;
            using (var command = new SqlCommand(@"
SELECT Account_Serial
FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK)
WHERE Parent_Account_Code = @Parent
  AND Account_Serial LIKE @Prefix;", connection, transaction))
            {
                command.Parameters.Add("@Parent", SqlDbType.NVarChar, 70).Value = parent;
                command.Parameters.Add("@Prefix", SqlDbType.NVarChar, 4000).Value = parentSerial + "%";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var serialNumber = ExtractTrailingNumber(ReadString(reader, "Account_Serial"), parentSerial);
                        if (serialNumber > maxNumber)
                        {
                            maxNumber = serialNumber;
                        }
                    }
                }
            }

            var number = maxNumber + 1;
            return parentSerial + number.ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }

        private static AccountTypeValues LoadParentAccountTypes(SqlConnection connection, SqlTransaction transaction, string parent)
        {
            using (var command = new SqlCommand("SELECT TOP (1) AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority FROM dbo.ACCOUNTS WHERE Account_Code = @Code", connection, transaction))
            {
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = parent;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new AccountTypeValues();
                    }

                    return new AccountTypeValues
                    {
                        AccountTypes = ReadInt(reader, "AccountTypes"),
                        AccountTab = ReadInt(reader, "AccountTab"),
                        DepitOrCredit = ReadInt(reader, "DepitOrCredit"),
                        Differenttype = ReadInt(reader, "Differenttype"),
                        Authority = ReadInt(reader, "Authority")
                    };
                }
            }
        }

        private static void PrepareLegacyCode(CustomerEditViewModel request)
        {
            request.Prefix = (request.Prefix ?? string.Empty).Trim();
            request.Code = (request.Code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(request.Code) && request.CusId.HasValue)
            {
                request.Code = request.CusId.Value.ToString(CultureInfo.InvariantCulture);
            }

            request.FullCode = (request.Prefix + request.Code).Trim();
        }

        private static void EnsureUniqueLegacyValues(SqlConnection connection, SqlTransaction transaction, CustomerEditViewModel request)
        {
            EnsureUniqueName(connection, transaction, request.CusId.GetValueOrDefault(), request.CusName);

            if (!string.IsNullOrWhiteSpace(request.FullCode))
            {
                var duplicateCode = ExecuteScalar<int>(connection, transaction,
                    "SELECT COUNT(1) FROM dbo.TblCustemers WHERE Type = 1 AND CusID <> @Id AND LTRIM(RTRIM(Fullcode)) = @FullCode",
                    new SqlParameter("@Id", SqlDbType.Int) { Value = request.CusId.GetValueOrDefault() },
                    new SqlParameter("@FullCode", SqlDbType.NVarChar, 255) { Value = request.FullCode });
                if (duplicateCode > 0)
                {
                    throw new InvalidOperationException("يوجد عميل مسجل مسبقا بهذا الكود. برجاء التأكد من الكود المدخل.");
                }
            }

            double nationalNumber;
            if (!string.IsNullOrWhiteSpace(request.NationalNo) && double.TryParse(request.NationalNo, NumberStyles.Number, CultureInfo.InvariantCulture, out nationalNumber))
            {
                var duplicateNational = ExecuteScalar<int>(connection, transaction,
                    "SELECT COUNT(1) FROM dbo.TblCustemers WHERE Type = 1 AND CusID <> @Id AND CustGID = @CustGID",
                    new SqlParameter("@Id", SqlDbType.Int) { Value = request.CusId.GetValueOrDefault() },
                    new SqlParameter("@CustGID", SqlDbType.Float) { Value = nationalNumber });
                if (duplicateNational > 0)
                {
                    throw new InvalidOperationException("يوجد عميل مسجل مسبقا بنفس رقم السجل.");
                }
            }
        }

        private static void EnsureUniqueName(SqlConnection connection, SqlTransaction transaction, int currentId, string name)
        {
            var duplicate = ExecuteScalar<int>(connection, transaction,
                "SELECT COUNT(1) FROM dbo.TblCustemers WHERE Type = 1 AND CusID <> @Id AND LTRIM(RTRIM(CusName)) = @Name",
                new SqlParameter("@Id", SqlDbType.Int) { Value = currentId },
                new SqlParameter("@Name", SqlDbType.NVarChar, 4000) { Value = name.Trim() });
            if (duplicate > 0)
            {
                throw new InvalidOperationException("يوجد عميل أو مورد مسجل مسبقاً بهذا الاسم. برجاء تمييز الاسم ثم إعادة المحاولة.");
            }
        }

        private static string Validate(CustomerEditViewModel request)
        {
            if (request == null)
            {
                return "لم تصل بيانات العميل.";
            }

            request.CusName = (request.CusName ?? string.Empty).Trim();
            request.CusNameEnglish = (request.CusNameEnglish ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(request.CusName))
            {
                return "يجب إدخال اسم العميل.";
            }

            if (request.Type != 1 && request.Type != 2)
            {
                return "يجب تحديد نوع السجل: عميل أو مورد.";
            }

            if (request.OpenBalanceType.HasValue && request.OpenBalanceType.Value != 0 && request.OpenBalanceType.Value != 1)
            {
                return "نوع الرصيد الافتتاحي يجب أن يكون مدين أو دائن.";
            }

            if (request.OpenBalanceType.HasValue && request.OpenBalance.GetValueOrDefault() <= 0)
            {
                return "يجب كتابة قيمة الرصيد الافتتاحي عند اختيار نوع الرصيد.";
            }

            if (request.OpenBalanceType1.HasValue && request.OpenBalance1.GetValueOrDefault() <= 0)
            {
                return "يجب كتابة قيمة رصيد الشيكات تحت التحصيل.";
            }

            if (request.OpenBalanceType2.HasValue && request.OpenBalance2.GetValueOrDefault() <= 0)
            {
                return "يجب كتابة قيمة رصيد الدفعات المقدمة.";
            }

            if ((request.DiscountType == 1 || request.DiscountType == 2) && request.DiscountValue.GetValueOrDefault() <= 0)
            {
                return "يجب كتابة قيمة الخصم الخاصة بالعميل.";
            }

            if (request.DiscountType == 2 && request.DiscountValue.GetValueOrDefault() > 100)
            {
                return "لا يمكن أن تكون نسبة خصم البيع أكبر من 100.";
            }

            if ((request.PurchaseDiscountType == 1 || request.PurchaseDiscountType == 2) && request.PurchaseDiscountValue.GetValueOrDefault() <= 0)
            {
                return "يجب كتابة قيمة الخصم الخاصة بفواتير الشراء.";
            }

            if (request.PurchaseDiscountType == 2 && request.PurchaseDiscountValue.GetValueOrDefault() > 100)
            {
                return "لا يمكن أن تكون نسبة خصم الشراء أكبر من 100.";
            }

            return null;
        }

        private static CustomerSaveResult Fail(string message)
        {
            return new CustomerSaveResult
            {
                Success = false,
                Message = message
            };
        }

        private static void AddCustomerParameters(SqlCommand command, CustomerEditViewModel request, MainErpUserContext user)
        {
            command.Parameters.Add("@CusID", SqlDbType.Int).Value = request.CusId.Value;
            command.Parameters.Add("@CusName", SqlDbType.NVarChar, 4000).Value = request.CusName.Trim();
            command.Parameters.Add("@CusNamee", SqlDbType.NVarChar, 4000).Value = DbText(request.CusNameEnglish);
            command.Parameters.Add("@ResponsibleContact", SqlDbType.NVarChar, 50).Value = DbText(request.ResponsibleContact);
            command.Parameters.Add("@Cus_Phone", SqlDbType.NVarChar, 50).Value = DbText(request.Phone);
            command.Parameters.Add("@Cus_mobile", SqlDbType.NVarChar, 50).Value = DbText(request.Mobile);
            command.Parameters.Add("@FaxNumber", SqlDbType.NVarChar, 50).Value = DbText(request.FaxNumber);
            command.Parameters.Add("@E_mail", SqlDbType.NVarChar, 100).Value = DbText(request.Email);
            command.Parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = DbText(request.Address);
            command.Parameters.Add("@AddressE", SqlDbType.NVarChar, 400).Value = DbText(request.AddressEnglish);
            command.Parameters.Add("@Remark", SqlDbType.NText).Value = DbText(request.Remark);
            command.Parameters.Add("@Remark2", SqlDbType.VarChar, 255).Value = DbText(request.Remark2);
            command.Parameters.Add("@Type", SqlDbType.Int).Value = request.Type;
            command.Parameters.Add("@prifix", SqlDbType.NVarChar, 255).Value = DbText(request.Prefix);
            command.Parameters.Add("@code", SqlDbType.NVarChar, 255).Value = DbText(request.Code);
            command.Parameters.Add("@Fullcode", SqlDbType.NVarChar, 255).Value = DbText(request.FullCode);
            command.Parameters.Add("@CustomerandVendor", SqlDbType.Bit).Value = request.CustomerAndVendor;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)request.BranchId ?? DBNull.Value;
            command.Parameters.Add("@BranchName", SqlDbType.NVarChar, 255).Value = DbText(request.BranchName);
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = (object)request.RecordDate ?? DateTime.Today;
            command.Parameters.Add("@OpenBalance", SqlDbType.Float).Value = (object)request.OpenBalance ?? 0d;
            command.Parameters.Add("@OpenBalanceType", SqlDbType.Int).Value = (object)request.OpenBalanceType ?? DBNull.Value;
            command.Parameters.Add("@OpenBalance1", SqlDbType.Float).Value = (object)request.OpenBalance1 ?? 0d;
            command.Parameters.Add("@OpenBalanceType1", SqlDbType.Int).Value = (object)request.OpenBalanceType1 ?? DBNull.Value;
            command.Parameters.Add("@OpenBalance2", SqlDbType.Float).Value = (object)request.OpenBalance2 ?? 0d;
            command.Parameters.Add("@OpenBalanceType2", SqlDbType.Int).Value = (object)request.OpenBalanceType2 ?? DBNull.Value;
            command.Parameters.Add("@OpenBalanceDate", SqlDbType.DateTime).Value = (object)request.OpenBalanceDate ?? DBNull.Value;
            command.Parameters.Add("@CreditLimit", SqlDbType.Real).Value = (object)request.CreditLimit ?? 0d;
            command.Parameters.Add("@CreditlimitCredit", SqlDbType.Real).Value = (object)request.CreditLimitCredit ?? 0d;
            command.Parameters.Add("@DepitInterval", SqlDbType.Int).Value = (object)request.DebitInterval ?? 0;
            command.Parameters.Add("@CreditInterval", SqlDbType.Int).Value = (object)request.CreditInterval ?? 0;
            command.Parameters.Add("@CPaymentType", SqlDbType.Int).Value = (object)request.PaymentType ?? 0;
            command.Parameters.Add("@Account_Code", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode);
            command.Parameters.Add("@Account_Code_As_Client", SqlDbType.NVarChar, 50).Value = DbText(request.AccountAsClient);
            command.Parameters.Add("@Account_Code_As_Supplier", SqlDbType.NVarChar, 50).Value = DbText(request.AccountAsSupplier);
            command.Parameters.Add("@parent_account", SqlDbType.VarChar, 500).Value = DbText(string.IsNullOrWhiteSpace(request.ParentAccount) ? (request.Type == 2 ? SupplierParentAccount : CustomerParentAccount) : request.ParentAccount);
            command.Parameters.Add("@CustomerTypeID", SqlDbType.Int).Value = (object)request.CustomerTypeId ?? DBNull.Value;
            command.Parameters.Add("@TypeCustomer", SqlDbType.Int).Value = (object)request.TypeCustomer ?? DBNull.Value;
            command.Parameters.Add("@ClassCustomersId", SqlDbType.Int).Value = (object)request.ClassCustomersId ?? DBNull.Value;
            command.Parameters.Add("@GroupsCustomersId", SqlDbType.Int).Value = (object)request.GroupsCustomersId ?? DBNull.Value;
            command.Parameters.Add("@SaleType", SqlDbType.Int).Value = (object)request.SaleType ?? 0;
            command.Parameters.Add("@Trans_DiscountType", SqlDbType.Int).Value = (object)request.DiscountType ?? 0;
            command.Parameters.Add("@Trans_Discount", SqlDbType.Real).Value = (object)request.DiscountValue ?? 0d;
            command.Parameters.Add("@Trans_DiscountTypePur", SqlDbType.Int).Value = (object)request.PurchaseDiscountType ?? 0;
            command.Parameters.Add("@Trans_DiscountPur", SqlDbType.Real).Value = (object)request.PurchaseDiscountValue ?? 0d;
            command.Parameters.Add("@NationalNo", SqlDbType.NVarChar, 400).Value = DbText(request.NationalNo);
            double custGid;
            command.Parameters.Add("@CustGID", SqlDbType.Float).Value = double.TryParse(request.NationalNo, NumberStyles.Number, CultureInfo.InvariantCulture, out custGid) ? (object)custGid : DBNull.Value;
            command.Parameters.Add("@VATNO", SqlDbType.NVarChar, 255).Value = DbText(request.VatNo);
            command.Parameters.Add("@txtGeneralTax", SqlDbType.NVarChar, 400).Value = DbText(request.GeneralTaxNo);
            command.Parameters.Add("@txtTaxC", SqlDbType.NVarChar, 400).Value = DbText(request.TaxCardNo);
            command.Parameters.Add("@txtTaxStamp", SqlDbType.NVarChar, 400).Value = DbText(request.TaxStampNo);
            command.Parameters.Add("@txtWorkEarningTaxes", SqlDbType.NVarChar, 400).Value = DbText(request.WorkEarningTaxesNo);
            command.Parameters.Add("@txtTaxNo1", SqlDbType.NVarChar, 400).Value = DbText(request.CommercialRegisterNo);
            command.Parameters.Add("@txtCardImportNo", SqlDbType.NVarChar, 400).Value = DbText(request.ImportCardNo);
            command.Parameters.Add("@txtCardExportNo", SqlDbType.NVarChar, 400).Value = DbText(request.ExportCardNo);
            command.Parameters.Add("@chkTaxExempt", SqlDbType.Bit).Value = request.TaxExempt;
            command.Parameters.Add("@CountryID", SqlDbType.Int).Value = (object)request.CountryId ?? DBNull.Value;
            command.Parameters.Add("@GovernmentID", SqlDbType.Int).Value = (object)request.GovernmentId ?? DBNull.Value;
            command.Parameters.Add("@CityID", SqlDbType.Int).Value = (object)request.CityId ?? DBNull.Value;
            command.Parameters.Add("@CountryID2", SqlDbType.Float).Value = (object)request.CountryId2 ?? DBNull.Value;
            command.Parameters.Add("@StreetName", SqlDbType.NVarChar, 255).Value = DbText(request.StreetName);
            command.Parameters.Add("@AdditionalStreetName", SqlDbType.NVarChar, 255).Value = DbText(request.AdditionalStreetName);
            command.Parameters.Add("@BuildingNumber", SqlDbType.NVarChar, 255).Value = DbText(request.BuildingNumber);
            command.Parameters.Add("@PlotIdentification", SqlDbType.NVarChar, 255).Value = DbText(request.PlotIdentification);
            command.Parameters.Add("@CityName", SqlDbType.NVarChar, 255).Value = DbText(request.CityName);
            command.Parameters.Add("@CitySubdivisionName", SqlDbType.NVarChar, 255).Value = DbText(request.CitySubdivisionName);
            command.Parameters.Add("@PostalZone", SqlDbType.NVarChar, 255).Value = DbText(request.PostalZone);
            command.Parameters.Add("@CountrySubentity", SqlDbType.NVarChar, 255).Value = DbText(request.CountrySubentity);
            command.Parameters.Add("@IdentificationCode", SqlDbType.NVarChar, 255).Value = DbText(request.IdentificationCode);
            command.Parameters.Add("@Id700", SqlDbType.NVarChar, 255).Value = DbText(request.Id700);
            command.Parameters.Add("@BoxMil", SqlDbType.NVarChar, 255).Value = DbText(request.BoxMil);
            command.Parameters.Add("@ZipCode", SqlDbType.NVarChar, 255).Value = DbText(request.ZipCode);
            command.Parameters.Add("@BankName", SqlDbType.NVarChar, 50).Value = DbText(request.BankName);
            command.Parameters.Add("@BankAccount", SqlDbType.NVarChar, 50).Value = DbText(request.BankAccount);
            command.Parameters.Add("@BankCode", SqlDbType.NVarChar, 100).Value = DbText(request.BankCode);
            command.Parameters.Add("@BankIBAN", SqlDbType.NVarChar, 100).Value = DbText(request.BankIban);
            command.Parameters.Add("@BankAddress", SqlDbType.NVarChar, 4000).Value = DbText(request.BankAddress);
            command.Parameters.Add("@IBAN", SqlDbType.NVarChar, 400).Value = DbText(request.Iban);
            command.Parameters.Add("@locked", SqlDbType.Bit).Value = request.IsLocked;
            command.Parameters.Add("@creditlocked", SqlDbType.Int).Value = request.CreditLocked ? 1 : 0;
            command.Parameters.Add("@export", SqlDbType.Int).Value = request.Export ? 1 : 0;
            command.Parameters.Add("@Entry", SqlDbType.NVarChar, 255).Value = DbText(user != null ? user.UserName : null);
        }

        private static object DbText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static bool Exists(SqlConnection connection, SqlTransaction transaction, int id)
        {
            return ExecuteScalar<int>(connection, transaction, "SELECT COUNT(1) FROM dbo.TblCustemers WHERE CusID = @Id", new SqlParameter("@Id", SqlDbType.Int) { Value = id }) > 0;
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string table, string column)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(" + column + "), 0) + 1 FROM dbo." + table + " WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static int? FirstInt(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            var value = ExecuteScalar<object>(connection, transaction, sql);
            return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static T ExecuteScalar<T>(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                foreach (var parameter in parameters ?? new SqlParameter[0])
                {
                    command.Parameters.Add(parameter);
                }

                var value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return default(T);
                }

                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                foreach (var parameter in parameters ?? new SqlParameter[0])
                {
                    command.Parameters.Add(parameter);
                }

                command.ExecuteNonQuery();
            }
        }

        private static int CountA(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            var count = 0;
            foreach (var ch in value)
            {
                if (ch == 'a' || ch == 'A')
                {
                    count++;
                }
            }

            return count;
        }

        private static int ExtractTrailingNumber(string serial, string prefix)
        {
            if (string.IsNullOrWhiteSpace(serial) || !serial.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            int number;
            return int.TryParse(serial.Substring((prefix ?? string.Empty).Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? number : 0;
        }

        private static string FormatType(int? type)
        {
            return type == 2 ? "مورد" : "عميل";
        }

        private static string FormatAccount(string serial, string name, string code)
        {
            if (!string.IsNullOrWhiteSpace(serial) || !string.IsNullOrWhiteSpace(name))
            {
                return (serial + " - " + name).Trim(' ', '-');
            }

            return code ?? string.Empty;
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static double? ReadDouble(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (double?)null : Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDate(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private sealed class AccountTypeValues
        {
            public int? AccountTypes { get; set; }
            public int? AccountTab { get; set; }
            public int? DepitOrCredit { get; set; }
            public int? Differenttype { get; set; }
            public int? Authority { get; set; }
        }

private const string CustomerInsertSql = @"
INSERT INTO dbo.TblCustemers
(CusID, CusName, CusNamee, ResponsibleContact, Cus_Phone, Cus_mobile, FaxNumber, E_mail,
 Address, AddressE, Remark, Remark2, Type, prifix, code, Fullcode, CustomerandVendor, BranchId,
 BranchName, RecordDate, OpenBalance, OpenBalanceType, OpenBalance1, OpenBalanceType1,
 OpenBalance2, OpenBalanceType2, OpenBalanceDate, CreditLimit, CreditlimitCredit, DepitInterval,
 CreditInterval, CPaymentType, Account_Code, Account_Code_As_Client,
 Account_Code_As_Supplier, parent_account, CustomerTypeID, TypeCustomer, ClassCustomersId,
 GroupsCustomersId, SaleType, Trans_Discount, Trans_DiscountType, Trans_DiscountPur,
 Trans_DiscountTypePur, NationalNo, CustGID, VATNO, txtGeneralTax, txtTaxC, txtTaxStamp, txtWorkEarningTaxes,
 txtTaxNo1, txtCardImportNo, txtCardExportNo, chkTaxExempt, CountryID, GovernmentID, CityID, CountryID2,
 StreetName, AdditionalStreetName, BuildingNumber, PlotIdentification, CityName, CitySubdivisionName,
 PostalZone, CountrySubentity, IdentificationCode, Id700, BoxMil, ZipCode, BankName, BankAccount,
 BankCode, BankIBAN, BankAddress, IBAN, locked, creditlocked, export, Entry)
VALUES
(@CusID, @CusName, @CusNamee, @ResponsibleContact, @Cus_Phone, @Cus_mobile, @FaxNumber, @E_mail,
 @Address, @AddressE, @Remark, @Remark2, @Type, @prifix, @code, @Fullcode, @CustomerandVendor, @BranchId,
 @BranchName, @RecordDate, @OpenBalance, @OpenBalanceType, @OpenBalance1, @OpenBalanceType1,
 @OpenBalance2, @OpenBalanceType2, @OpenBalanceDate, @CreditLimit, @CreditlimitCredit, @DepitInterval,
 @CreditInterval, @CPaymentType, @Account_Code, @Account_Code_As_Client,
 @Account_Code_As_Supplier, @parent_account, @CustomerTypeID, @TypeCustomer, @ClassCustomersId,
 @GroupsCustomersId, @SaleType, @Trans_Discount, @Trans_DiscountType, @Trans_DiscountPur,
 @Trans_DiscountTypePur, @NationalNo, @CustGID, @VATNO, @txtGeneralTax, @txtTaxC, @txtTaxStamp, @txtWorkEarningTaxes,
 @txtTaxNo1, @txtCardImportNo, @txtCardExportNo, @chkTaxExempt, @CountryID, @GovernmentID, @CityID, @CountryID2,
 @StreetName, @AdditionalStreetName, @BuildingNumber, @PlotIdentification, @CityName, @CitySubdivisionName,
 @PostalZone, @CountrySubentity, @IdentificationCode, @Id700, @BoxMil, @ZipCode, @BankName, @BankAccount,
 @BankCode, @BankIBAN, @BankAddress, @IBAN, @locked, @creditlocked, @export, @Entry);";

        private const string CustomerUpdateSql = @"
UPDATE dbo.TblCustemers
SET CusName = @CusName,
    CusNamee = @CusNamee,
    ResponsibleContact = @ResponsibleContact,
    Cus_Phone = @Cus_Phone,
    Cus_mobile = @Cus_mobile,
    FaxNumber = @FaxNumber,
    E_mail = @E_mail,
    Address = @Address,
    AddressE = @AddressE,
    Remark = @Remark,
    Remark2 = @Remark2,
    Type = @Type,
    prifix = @prifix,
    code = @code,
    Fullcode = @Fullcode,
    CustomerandVendor = @CustomerandVendor,
    BranchId = @BranchId,
    BranchName = @BranchName,
    RecordDate = @RecordDate,
    OpenBalance = @OpenBalance,
    OpenBalanceType = @OpenBalanceType,
    OpenBalance1 = @OpenBalance1,
    OpenBalanceType1 = @OpenBalanceType1,
    OpenBalance2 = @OpenBalance2,
    OpenBalanceType2 = @OpenBalanceType2,
    OpenBalanceDate = @OpenBalanceDate,
    CreditLimit = @CreditLimit,
    CreditlimitCredit = @CreditlimitCredit,
    DepitInterval = @DepitInterval,
    CreditInterval = @CreditInterval,
    CPaymentType = @CPaymentType,
    Account_Code = @Account_Code,
    Account_Code_As_Client = @Account_Code_As_Client,
    Account_Code_As_Supplier = @Account_Code_As_Supplier,
    parent_account = @parent_account,
    CustomerTypeID = @CustomerTypeID,
    TypeCustomer = @TypeCustomer,
    ClassCustomersId = @ClassCustomersId,
    GroupsCustomersId = @GroupsCustomersId,
    SaleType = @SaleType,
    Trans_DiscountType = @Trans_DiscountType,
    Trans_Discount = @Trans_Discount,
    Trans_DiscountTypePur = @Trans_DiscountTypePur,
    Trans_DiscountPur = @Trans_DiscountPur,
    NationalNo = @NationalNo,
    CustGID = @CustGID,
    VATNO = @VATNO,
    txtGeneralTax = @txtGeneralTax,
    txtTaxC = @txtTaxC,
    txtTaxStamp = @txtTaxStamp,
    txtWorkEarningTaxes = @txtWorkEarningTaxes,
    txtTaxNo1 = @txtTaxNo1,
    txtCardImportNo = @txtCardImportNo,
    txtCardExportNo = @txtCardExportNo,
    chkTaxExempt = @chkTaxExempt,
    CountryID = @CountryID,
    GovernmentID = @GovernmentID,
    CityID = @CityID,
    CountryID2 = @CountryID2,
    StreetName = @StreetName,
    AdditionalStreetName = @AdditionalStreetName,
    BuildingNumber = @BuildingNumber,
    PlotIdentification = @PlotIdentification,
    CityName = @CityName,
    CitySubdivisionName = @CitySubdivisionName,
    PostalZone = @PostalZone,
    CountrySubentity = @CountrySubentity,
    IdentificationCode = @IdentificationCode,
    Id700 = @Id700,
    BoxMil = @BoxMil,
    ZipCode = @ZipCode,
    BankName = @BankName,
    BankAccount = @BankAccount,
    BankCode = @BankCode,
    BankIBAN = @BankIBAN,
    BankAddress = @BankAddress,
    IBAN = @IBAN,
    locked = @locked,
    creditlocked = @creditlocked,
    export = @export,
    Entry = @Entry
WHERE CusID = @CusID;";
    }
}
