using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace MyERP.Common.StoreData
{
    public class StoreDataRepository
    {
        private readonly Func<SqlConnection> _openConnection;

        public StoreDataRepository(Func<SqlConnection> openConnection)
        {
            if (openConnection == null)
            {
                throw new ArgumentNullException("openConnection");
            }

            _openConnection = openConnection;
        }

        public StoreDataIndexViewModel LoadIndex(StoreDataSearchRequest request, int? selectedId)
        {
            request = request ?? new StoreDataSearchRequest();
            var model = new StoreDataIndexViewModel
            {
                SearchText = request.SearchText,
                BranchId = request.BranchId,
                Mode = request.Mode
            };

            using (var connection = _openConnection())
            {
                model.Branches = LoadBranches(connection);
                model.Employees = LoadEmployees(connection);
                model.Boxes = LoadBoxes(connection);
                model.Accounts = LoadAccounts(connection);
                model.Users = LoadUsers(connection);
                model.Stores = SearchStores(connection, request);
                model.TotalCount = model.Stores.Count;
                model.LinkedCount = model.Stores.Count(x => x.Linked);
                model.UnlinkedCount = model.Stores.Count(x => !x.Linked);
                model.LabCount = model.Stores.Count(x => x.IsLab);
                model.NoEntryCount = model.Stores.Count(x => x.IsNotCreateEntry);

                var resolvedId = selectedId.GetValueOrDefault();
                if (resolvedId <= 0 && model.Stores.Count > 0)
                {
                    resolvedId = model.Stores[0].StoreId;
                }

                model.Selected = resolvedId > 0 ? GetStore(connection, resolvedId) ?? new StoreDataEditViewModel() : NewStore(connection, request.BranchId);
            }

            return model;
        }

        public StoreDataEditViewModel NewStore(int? branchId)
        {
            using (var connection = _openConnection())
            {
                return NewStore(connection, branchId);
            }
        }

        public StoreDataEditViewModel GetStore(int id)
        {
            using (var connection = _openConnection())
            {
                return GetStore(connection, id);
            }
        }

        public StoreDataSaveResult Save(StoreDataEditViewModel model)
        {
            if (model == null)
            {
                return Fail("بيانات المخزن غير مكتملة.");
            }

            model.StoreName = Clean(model.StoreName);
            model.StoreNameEnglish = Clean(model.StoreNameEnglish);
            model.Code = Clean(model.Code);

            if (!model.BranchId.HasValue || model.BranchId.Value <= 0)
            {
                return Fail("يجب تحديد الفرع قبل حفظ المخزن.");
            }

            if (string.IsNullOrWhiteSpace(model.StoreName))
            {
                return Fail("اسم المخزن العربي مطلوب.");
            }

            using (var connection = _openConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (StoreNameExists(connection, transaction, model.StoreName, model.StoreId))
                    {
                        return Fail("يوجد مخزن مسجل مسبقا بهذا الاسم.");
                    }

                    var isNew = model.StoreId <= 0;
                    if (isNew)
                    {
                        model.StoreId = AllocateStoreId(connection, transaction);
                    }

                    ResolveStoreAccounts(connection, transaction, model, isNew);
                    UpsertStore(connection, transaction, model, isNew);
                    ReplaceAssignedUsers(connection, transaction, model.StoreId, model.UserIds);
                    transaction.Commit();

                    return new StoreDataSaveResult
                    {
                        Success = true,
                        StoreId = model.StoreId,
                        Message = isNew ? "تم إنشاء المخزن وربطه تشغيليا." : "تم حفظ تعديلات المخزن."
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ المخزن: " + ex.Message);
                }
            }
        }

        public StoreDataSaveResult Delete(int storeId)
        {
            if (storeId <= 0)
            {
                return Fail("رقم المخزن غير صحيح.");
            }

            using (var connection = _openConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var store = GetStore(connection, transaction, storeId);
                    if (store == null)
                    {
                        return Fail("لم يتم العثور على المخزن.");
                    }

                    if (store.TransactionCount > 0)
                    {
                        return Fail("لا يمكن حذف المخزن لوجود عمليات مسجلة عليه.");
                    }

                    if (store.VoucherAccountUseCount > 0)
                    {
                        return Fail("لا يمكن حذف المخزن لوجود قيود محاسبية مرتبطة بحساباته.");
                    }

                    Execute(connection, transaction, "DELETE FROM dbo.TblUsersStores WHERE storeId = @StoreId", p => p.Add("@StoreId", SqlDbType.Int).Value = storeId);
                    Execute(connection, transaction, "DELETE FROM dbo.TblStore WHERE StoreID = @StoreId", p => p.Add("@StoreId", SqlDbType.Int).Value = storeId);
                    transaction.Commit();

                    return new StoreDataSaveResult { Success = true, Message = "تم حذف المخزن بعد التحقق من عدم وجود حركات.", StoreId = storeId };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف المخزن: " + ex.Message);
                }
            }
        }

        public IList<OperationalStoreViewModel> GetOperationalStores(int? branchId, string searchText)
        {
            using (var connection = _openConnection())
            {
                var rows = new List<OperationalStoreViewModel>();
                var linkedExpression = ColumnExists(connection, null, "TblStore", "linked") ? "ISNULL(s.linked, 0)" : "CONVERT(bit, 1)";
                var isLabExpression = ColumnExists(connection, null, "TblStore", "IsLab") ? "ISNULL(s.IsLab, 0)" : "CONVERT(bit, 0)";
                var noEntryExpression = ColumnExists(connection, null, "TblStore", "IsNotCreateEntry") ? "ISNULL(s.IsNotCreateEntry, 0)" : "CONVERT(bit, 0)";
                var codeExpression = ColumnExists(connection, null, "TblStore", "Code") ? "s.Code" : "CONVERT(NVARCHAR(50), s.StoreID)";
                var phoneExpression = ColumnExists(connection, null, "TblStore", "StorePhone") ? "s.StorePhone" : "CAST(NULL AS NVARCHAR(50))";
                var nameEnglishExpression = ColumnExists(connection, null, "TblStore", "StoreNamee") ? "s.StoreNamee" : "CAST(NULL AS NVARCHAR(255))";
                var sql = @"
SELECT TOP (200)
    s.StoreID,
    {0} AS Code,
    s.StoreName,
    {1} AS StoreNamee,
    s.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), s.BranchId)) AS BranchName,
    {2} AS StorePhone,
    {3} AS linked,
    {4} AS IsLab,
    {5} AS IsNotCreateEntry,
    ISNULL(m.TransactionCount, 0) AS TransactionCount,
    m.LastMovementDate
FROM dbo.TblStore s
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = s.BranchId
OUTER APPLY (
    SELECT COUNT(1) AS TransactionCount, MAX(t.Transaction_Date) AS LastMovementDate
    FROM dbo.Transactions t
    WHERE t.StoreID = s.StoreID
) m
WHERE (@BranchId IS NULL OR s.BranchId = @BranchId)
  AND (
        @SearchText = N''
     OR s.StoreName LIKE N'%' + @SearchText + N'%'
     OR {1} LIKE N'%' + @SearchText + N'%'
     OR {0} LIKE N'%' + @SearchText + N'%'
  )
ORDER BY {3} DESC, s.StoreName, s.StoreID;";
                sql = string.Format(CultureInfo.InvariantCulture, sql, codeExpression, nameEnglishExpression, phoneExpression, linkedExpression, isLabExpression, noEntryExpression);

                using (var command = new SqlCommand(sql, connection))
                {
                    AddNullable(command, "@BranchId", SqlDbType.Int, branchId);
                    command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 255).Value = Clean(searchText);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new OperationalStoreViewModel
                            {
                                StoreId = ReadInt(reader, "StoreID"),
                                Code = ReadString(reader, "Code"),
                                StoreName = ReadString(reader, "StoreName"),
                                StoreNameEnglish = ReadString(reader, "StoreNamee"),
                                BranchId = ReadNullableInt(reader, "BranchId"),
                                BranchName = ReadString(reader, "BranchName"),
                                Phone = ReadString(reader, "StorePhone"),
                                Linked = ReadBool(reader, "linked"),
                                IsLab = ReadBool(reader, "IsLab"),
                                IsNotCreateEntry = ReadBool(reader, "IsNotCreateEntry"),
                                TransactionCount = ReadInt(reader, "TransactionCount"),
                                LastMovementDate = ReadNullableDate(reader, "LastMovementDate")
                            });
                        }
                    }
                }

                return rows;
            }
        }

        private static StoreDataSaveResult Fail(string message)
        {
            return new StoreDataSaveResult { Success = false, Message = message };
        }

        private static StoreDataEditViewModel NewStore(SqlConnection connection, int? branchId)
        {
            var defaults = LoadBranchDefaults(connection, null, branchId);
            var options = LoadOptions(connection, null);
            return new StoreDataEditViewModel
            {
                BranchId = branchId,
                Linked = true,
                InventoryParentAccount = defaults.GetAccount(0),
                SettlementParentAccount = defaults.GetAccount(11),
                LossParentAccount = defaults.GetAccount(options.EachStoreHaveLossAccount ? 10 : 75),
                GiftParentAccount = defaults.GetAccount(options.EachStoreHaveGiftAccount ? 17 : 76)
            };
        }

        private static IList<StoreDataRowViewModel> SearchStores(SqlConnection connection, StoreDataSearchRequest request)
        {
            var rows = new List<StoreDataRowViewModel>();
            var linkedExpression = ColumnExists(connection, null, "TblStore", "linked") ? "ISNULL(s.linked, 0)" : "CONVERT(bit, 1)";
            var isLabExpression = ColumnExists(connection, null, "TblStore", "IsLab") ? "ISNULL(s.IsLab, 0)" : "CONVERT(bit, 0)";
            var noEntryExpression = ColumnExists(connection, null, "TblStore", "IsNotCreateEntry") ? "ISNULL(s.IsNotCreateEntry, 0)" : "CONVERT(bit, 0)";
            var sql = @"
SELECT TOP (300)
    s.StoreID, s.Code, s.StoreName, s.StoreNamee, s.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), s.BranchId)) AS BranchName,
    s.StorePhone,
    COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(e.Emp_Namee, N''), NULLIF(e.Fullcode, N''), CONVERT(NVARCHAR(20), s.Emp_ID)) AS EmployeeName,
    {0} AS linked,
    {1} AS IsLab,
    {2} AS IsNotCreateEntry,
    s.Account_Code,
    s.Account_Code2,
    ISNULL(u.UserCount, 0) AS UserCount,
    ISNULL(t.TransactionCount, 0) AS TransactionCount
FROM dbo.TblStore s
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = s.BranchId
LEFT JOIN dbo.TblEmployee e ON CONVERT(NVARCHAR(50), e.Emp_ID) = CONVERT(NVARCHAR(50), s.Emp_ID)
OUTER APPLY (SELECT COUNT(1) AS UserCount FROM dbo.TblUsersStores us WHERE us.storeId = s.StoreID) u
OUTER APPLY (SELECT COUNT(1) AS TransactionCount FROM dbo.Transactions tr WHERE tr.StoreID = s.StoreID) t
WHERE (@BranchId IS NULL OR s.BranchId = @BranchId)
  AND (
        @SearchText = N''
     OR s.StoreName LIKE N'%' + @SearchText + N'%'
     OR s.StoreNamee LIKE N'%' + @SearchText + N'%'
     OR s.Code LIKE N'%' + @SearchText + N'%'
     OR s.StorePhone LIKE N'%' + @SearchText + N'%'
  )
  AND (@Mode = N'' OR (@Mode = N'linked' AND {0} = 1) OR (@Mode = N'unlinked' AND {0} = 0) OR (@Mode = N'lab' AND {1} = 1) OR (@Mode = N'no-entry' AND {2} = 1))
ORDER BY s.StoreID DESC;";
            sql = string.Format(CultureInfo.InvariantCulture, sql, linkedExpression, isLabExpression, noEntryExpression);

            using (var command = new SqlCommand(sql, connection))
            {
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 255).Value = Clean(request.SearchText);
                command.Parameters.Add("@Mode", SqlDbType.NVarChar, 50).Value = Clean(request.Mode);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new StoreDataRowViewModel
                        {
                            StoreId = ReadInt(reader, "StoreID"),
                            Code = ReadString(reader, "Code"),
                            StoreName = ReadString(reader, "StoreName"),
                            StoreNameEnglish = ReadString(reader, "StoreNamee"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            Phone = ReadString(reader, "StorePhone"),
                            EmployeeName = ReadString(reader, "EmployeeName"),
                            Linked = ReadBool(reader, "linked"),
                            IsLab = ReadBool(reader, "IsLab"),
                            IsNotCreateEntry = ReadBool(reader, "IsNotCreateEntry"),
                            InventoryAccount = ReadString(reader, "Account_Code"),
                            SettlementAccount = ReadString(reader, "Account_Code2"),
                            UserCount = ReadInt(reader, "UserCount"),
                            TransactionCount = ReadInt(reader, "TransactionCount")
                        });
                    }
                }
            }

            return rows;
        }

        private static StoreDataEditViewModel GetStore(SqlConnection connection, int id)
        {
            return GetStore(connection, null, id);
        }

        private static StoreDataEditViewModel GetStore(SqlConnection connection, SqlTransaction transaction, int id)
        {
            var linkedExpression = ColumnExists(connection, transaction, "TblStore", "linked") ? "ISNULL(s.linked, 0)" : "CONVERT(bit, 1)";
            var isLabExpression = ColumnExists(connection, transaction, "TblStore", "IsLab") ? "ISNULL(s.IsLab, 0)" : "CONVERT(bit, 0)";
            var noEntryExpression = ColumnExists(connection, transaction, "TblStore", "IsNotCreateEntry") ? "ISNULL(s.IsNotCreateEntry, 0)" : "CONVERT(bit, 0)";
            var boxExpression = ColumnExists(connection, transaction, "TblStore", "BoxID") ? "s.BoxID" : "CAST(NULL AS INT)";
            var sql = @"
SELECT TOP (1)
    s.StoreID, s.Code, s.StoreName, s.StoreNamee, s.StoreAdress, s.StorePhone, CONVERT(NVARCHAR(MAX), s.Remarks) AS Remarks,
    s.BranchId, s.Emp_ID, s.SalesPersonid, s.PurchasePersonid, {3} AS BoxID,
    {0} AS linked, {1} AS IsLab, {2} AS IsNotCreateEntry,
    s.ParetnAccount, s.Account_Code, s.Account_Code1, s.Account_Code2, s.Account_Code3,
    s.Account_Code0, s.Account_Code11, s.Account_Code22, s.Account_Code33,
    ISNULL(t.TransactionCount, 0) AS TransactionCount
FROM dbo.TblStore s
OUTER APPLY (SELECT COUNT(1) AS TransactionCount FROM dbo.Transactions tr WHERE tr.StoreID = s.StoreID) t
WHERE s.StoreID = @StoreId;";
            sql = string.Format(CultureInfo.InvariantCulture, sql, linkedExpression, isLabExpression, noEntryExpression, boxExpression);

            StoreDataEditViewModel model = null;
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new StoreDataEditViewModel
                        {
                            StoreId = ReadInt(reader, "StoreID"),
                            Code = ReadString(reader, "Code"),
                            StoreName = ReadString(reader, "StoreName"),
                            StoreNameEnglish = ReadString(reader, "StoreNamee"),
                            Address = ReadString(reader, "StoreAdress"),
                            Phone = ReadString(reader, "StorePhone"),
                            Remarks = ReadString(reader, "Remarks"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            EmployeeId = ReadNullableInt(reader, "Emp_ID"),
                            SalesPersonId = ReadNullableInt(reader, "SalesPersonid"),
                            PurchasePersonId = ReadNullableInt(reader, "PurchasePersonid"),
                            BoxId = ReadNullableInt(reader, "BoxID"),
                            Linked = ReadBool(reader, "linked"),
                            IsLab = ReadBool(reader, "IsLab"),
                            IsNotCreateEntry = ReadBool(reader, "IsNotCreateEntry"),
                            ParentAccount = ReadString(reader, "ParetnAccount"),
                            InventoryAccount = ReadString(reader, "Account_Code"),
                            LossAccount = ReadString(reader, "Account_Code1"),
                            SettlementAccount = ReadString(reader, "Account_Code2"),
                            GiftAccount = ReadString(reader, "Account_Code3"),
                            InventoryParentAccount = ReadString(reader, "Account_Code0"),
                            LossParentAccount = ReadString(reader, "Account_Code11"),
                            SettlementParentAccount = ReadString(reader, "Account_Code22"),
                            GiftParentAccount = ReadString(reader, "Account_Code33"),
                            TransactionCount = ReadInt(reader, "TransactionCount")
                        };
                    }
                }
            }

            if (model == null)
            {
                return null;
            }

            model.AssignedUsers = LoadAssignedUsers(connection, transaction, id);
            model.UserIds = model.AssignedUsers.Select(x => x.UserId).ToList();
            model.VoucherAccountUseCount = CountVoucherAccountUse(connection, transaction, model);
            return model;
        }

        private static void ResolveStoreAccounts(SqlConnection connection, SqlTransaction transaction, StoreDataEditViewModel model, bool isNew)
        {
            var options = LoadOptions(connection, transaction);
            var defaults = LoadBranchDefaults(connection, transaction, model.BranchId);
            model.InventoryParentAccount = FirstNonEmpty(model.InventoryParentAccount, defaults.GetAccount(0));
            model.SettlementParentAccount = FirstNonEmpty(model.SettlementParentAccount, defaults.GetAccount(11));
            model.LossParentAccount = FirstNonEmpty(model.LossParentAccount, defaults.GetAccount(options.EachStoreHaveLossAccount ? 10 : 75));
            model.GiftParentAccount = FirstNonEmpty(model.GiftParentAccount, defaults.GetAccount(options.EachStoreHaveGiftAccount ? 17 : 76));

            if (!isNew)
            {
                UpdateAccountLabel(connection, transaction, model.ParentAccount, model.StoreName, model.StoreNameEnglish);
                UpdateAccountLabel(connection, transaction, model.InventoryAccount, model.StoreName + " المخزون السلعي", model.StoreNameEnglish + " - Inventory");
                UpdateAccountLabel(connection, transaction, model.SettlementAccount, model.StoreName + " التسويات الجردية", model.StoreNameEnglish + " - Inventory adjustments");
                UpdateAccountLabel(connection, transaction, model.LossAccount, model.StoreName + " فقد وتلف", model.StoreNameEnglish + " - Loss and damage");
                UpdateAccountLabel(connection, transaction, model.GiftAccount, model.StoreName + " هدايا وعينات", model.StoreNameEnglish + " - Gifts & Samples");
                return;
            }

            if (!model.AutoCreateAccounts)
            {
                model.InventoryAccount = FirstNonEmpty(model.InventoryAccount, model.InventoryParentAccount);
                model.SettlementAccount = FirstNonEmpty(model.SettlementAccount, model.SettlementParentAccount);
                model.LossAccount = FirstNonEmpty(model.LossAccount, model.LossParentAccount);
                model.GiftAccount = FirstNonEmpty(model.GiftAccount, model.GiftParentAccount);
                return;
            }

            if (options.StoreAccountHaveSettlement)
            {
                model.ParentAccount = EnsureChildAccount(connection, transaction, model.InventoryParentAccount, model.StoreName, model.StoreNameEnglish + " - Inventory", false);
                model.InventoryAccount = EnsureChildAccount(connection, transaction, model.ParentAccount, "المخزون السلعي " + model.StoreName, model.StoreNameEnglish + " - Inventory", true);
                model.SettlementAccount = EnsureChildAccount(connection, transaction, model.ParentAccount, "التسويات الجردية " + model.StoreName, model.StoreNameEnglish + " - Inventory adjustments", true);
            }
            else
            {
                model.InventoryAccount = EnsureChildAccount(connection, transaction, model.InventoryParentAccount, "المخزون السلعي " + model.StoreName, model.StoreNameEnglish + " - Inventory", true);
                model.SettlementAccount = EnsureChildAccount(connection, transaction, model.SettlementParentAccount, "التسويات الجردية " + model.StoreName, model.StoreNameEnglish + " - Inventory adjustments", true);
            }

            model.LossAccount = options.EachStoreHaveLossAccount
                ? EnsureChildAccount(connection, transaction, model.LossParentAccount, "فقد وتلف " + model.StoreName, model.StoreNameEnglish + " - Loss and damage", true)
                : model.LossParentAccount;
            model.GiftAccount = options.EachStoreHaveGiftAccount
                ? EnsureChildAccount(connection, transaction, model.GiftParentAccount, "هدايا وعينات " + model.StoreName, model.StoreNameEnglish + " - Gifts and Samples", true)
                : model.GiftParentAccount;
        }

        private static void UpsertStore(SqlConnection connection, SqlTransaction transaction, StoreDataEditViewModel model, bool isNew)
        {
            var sql = isNew ? @"
INSERT INTO dbo.TblStore
(StoreID, Code, StoreName, StoreNamee, StoreAdress, StorePhone, Remarks, BranchId, Emp_ID, SalesPersonid, PurchasePersonid, BoxID,
 linked, IsLab, IsNotCreateEntry, ParetnAccount, Account_Code, Account_Code1, Account_Code2, Account_Code3,
 Account_Code0, Account_Code11, Account_Code22, Account_Code33)
VALUES
(@StoreID, @Code, @StoreName, @StoreNamee, @StoreAdress, @StorePhone, @Remarks, @BranchId, @Emp_ID, @SalesPersonid, @PurchasePersonid, @BoxID,
 @linked, @IsLab, @IsNotCreateEntry, @ParetnAccount, @Account_Code, @Account_Code1, @Account_Code2, @Account_Code3,
 @Account_Code0, @Account_Code11, @Account_Code22, @Account_Code33);" : @"
UPDATE dbo.TblStore
SET Code=@Code, StoreName=@StoreName, StoreNamee=@StoreNamee, StoreAdress=@StoreAdress, StorePhone=@StorePhone, Remarks=@Remarks,
    BranchId=@BranchId, Emp_ID=@Emp_ID, SalesPersonid=@SalesPersonid, PurchasePersonid=@PurchasePersonid, BoxID=@BoxID,
    linked=@linked, IsLab=@IsLab, IsNotCreateEntry=@IsNotCreateEntry, ParetnAccount=@ParetnAccount,
    Account_Code=@Account_Code, Account_Code1=@Account_Code1, Account_Code2=@Account_Code2, Account_Code3=@Account_Code3,
    Account_Code0=@Account_Code0, Account_Code11=@Account_Code11, Account_Code22=@Account_Code22, Account_Code33=@Account_Code33
WHERE StoreID=@StoreID;";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                AddStoreParameters(command, model);
                command.ExecuteNonQuery();
            }
        }

        private static void AddStoreParameters(SqlCommand command, StoreDataEditViewModel model)
        {
            command.Parameters.Add("@StoreID", SqlDbType.Int).Value = model.StoreId;
            AddText(command, "@Code", model.Code, 255);
            AddText(command, "@StoreName", model.StoreName, 50);
            AddText(command, "@StoreNamee", string.IsNullOrWhiteSpace(model.StoreNameEnglish) ? model.StoreName : model.StoreNameEnglish, 255);
            AddText(command, "@StoreAdress", model.Address, 50);
            AddText(command, "@StorePhone", model.Phone, 50);
            AddText(command, "@Remarks", model.Remarks, -1);
            AddNullable(command, "@BranchId", SqlDbType.Int, model.BranchId);
            AddNullable(command, "@Emp_ID", SqlDbType.NVarChar, model.EmployeeId.HasValue ? model.EmployeeId.Value.ToString(CultureInfo.InvariantCulture) : null, 50);
            AddNullable(command, "@SalesPersonid", SqlDbType.Int, model.SalesPersonId);
            AddNullable(command, "@PurchasePersonid", SqlDbType.Int, model.PurchasePersonId);
            AddNullable(command, "@BoxID", SqlDbType.Int, model.BoxId);
            command.Parameters.Add("@linked", SqlDbType.Bit).Value = model.Linked;
            command.Parameters.Add("@IsLab", SqlDbType.Bit).Value = model.IsLab;
            command.Parameters.Add("@IsNotCreateEntry", SqlDbType.Bit).Value = model.IsNotCreateEntry;
            AddText(command, "@ParetnAccount", model.ParentAccount, 255);
            AddText(command, "@Account_Code", model.InventoryAccount, 50);
            AddText(command, "@Account_Code1", model.LossAccount, 50);
            AddText(command, "@Account_Code2", model.SettlementAccount, 50);
            AddText(command, "@Account_Code3", model.GiftAccount, 50);
            AddText(command, "@Account_Code0", model.InventoryParentAccount, 255);
            AddText(command, "@Account_Code11", model.LossParentAccount, 255);
            AddText(command, "@Account_Code22", model.SettlementParentAccount, 255);
            AddText(command, "@Account_Code33", model.GiftParentAccount, 255);
        }

        private static string EnsureChildAccount(SqlConnection connection, SqlTransaction transaction, string parentCode, string name, string nameEnglish, bool isFinal)
        {
            parentCode = Clean(parentCode);
            name = Clean(name);
            if (string.IsNullOrWhiteSpace(parentCode) || string.IsNullOrWhiteSpace(name) || !AccountExists(connection, transaction, parentCode))
            {
                return string.Empty;
            }

            var existing = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM dbo.ACCOUNTS WHERE Parent_Account_Code=@value AND LTRIM(RTRIM(Account_Name))=@name", parentCode, name);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                UpdateAccountLabel(connection, transaction, existing, name, nameEnglish);
                return existing;
            }

            var parent = LoadAccountParentProperties(connection, transaction, parentCode);
            var newCode = GenerateAccountCode(connection, transaction, parentCode);
            var serial = GenerateAccountSerial(connection, transaction, parentCode, newCode);

            Execute(connection, transaction, "UPDATE dbo.ACCOUNTS SET last_account = 0 WHERE Account_Code=@AccountCode", p => p.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = parentCode);
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ACCOUNTS
(AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority, [Block], Account_Code, Account_Name, Parent_Account_Code,
 last_account, cannot_del, Branch, BranchID, Account_Serial, BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna,
 cost_center, Sum_account, cost_center_type, cost_center_id, ActivityTypeId)
VALUES
(@AccountTypes, @AccountTab, @DepitOrCredit, @Differenttype, @Authority, 0, @Account_Code, @Account_Name, @Parent_Account_Code,
 @last_account, 0, @BranchText, @BranchId, @Account_Serial, 0, GETDATE(), @Account_NameEng, @currenct_code, 0,
 0, 0, 0, NULL, 0);", connection, transaction))
            {
                command.Parameters.Add("@AccountTypes", SqlDbType.Int).Value = parent.AccountTypes;
                command.Parameters.Add("@AccountTab", SqlDbType.Int).Value = parent.AccountTab;
                command.Parameters.Add("@DepitOrCredit", SqlDbType.Int).Value = parent.DepitOrCredit;
                command.Parameters.Add("@Differenttype", SqlDbType.Int).Value = parent.DifferentType;
                command.Parameters.Add("@Authority", SqlDbType.Int).Value = parent.Authority;
                command.Parameters.Add("@Account_Code", SqlDbType.NVarChar, 50).Value = newCode;
                command.Parameters.Add("@Account_Name", SqlDbType.NVarChar, 255).Value = Limit(name, 255);
                command.Parameters.Add("@Parent_Account_Code", SqlDbType.NVarChar, 50).Value = parentCode;
                command.Parameters.Add("@last_account", SqlDbType.Bit).Value = isFinal;
                command.Parameters.Add("@BranchText", SqlDbType.NVarChar, 50).Value = parent.BranchId.HasValue ? parent.BranchId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                AddNullable(command, "@BranchId", SqlDbType.Int, parent.BranchId);
                command.Parameters.Add("@Account_Serial", SqlDbType.NVarChar, 50).Value = Limit(serial, 50);
                command.Parameters.Add("@Account_NameEng", SqlDbType.NVarChar, 255).Value = Limit(string.IsNullOrWhiteSpace(nameEnglish) ? name : nameEnglish, 255);
                command.Parameters.Add("@currenct_code", SqlDbType.NVarChar, 50).Value = "1";
                command.ExecuteNonQuery();
            }

            return newCode;
        }

        private static void UpdateAccountLabel(SqlConnection connection, SqlTransaction transaction, string accountCode, string name, string nameEnglish)
        {
            if (string.IsNullOrWhiteSpace(accountCode) || !AccountExists(connection, transaction, accountCode))
            {
                return;
            }

            using (var command = new SqlCommand(@"UPDATE dbo.ACCOUNTS
SET Account_Name = @Name, Account_NameEng = @NameEnglish
WHERE Account_Code = @AccountCode", connection, transaction))
            {
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = Limit(Clean(name), 255);
                command.Parameters.Add("@NameEnglish", SqlDbType.NVarChar, 255).Value = Limit(Clean(nameEnglish), 255);
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                command.ExecuteNonQuery();
            }
        }

        private static AccountSeed LoadAccountParentProperties(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = new SqlCommand(@"SELECT TOP (1) AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority, BranchID
FROM dbo.ACCOUNTS WHERE Account_Code=@AccountCode", connection, transaction))
            {
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new AccountSeed
                        {
                            AccountTypes = ReadInt(reader, "AccountTypes"),
                            AccountTab = ReadInt(reader, "AccountTab"),
                            DepitOrCredit = ReadInt(reader, "DepitOrCredit"),
                            DifferentType = ReadInt(reader, "Differenttype"),
                            Authority = ReadInt(reader, "Authority"),
                            BranchId = ReadNullableInt(reader, "BranchID")
                        };
                    }
                }
            }

            return new AccountSeed();
        }

        private static string GenerateAccountCode(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            var max = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK) WHERE Parent_Account_Code=@Parent", connection, transaction))
            {
                command.Parameters.Add("@Parent", SqlDbType.NVarChar, 50).Value = parentCode;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "Account_Code");
                        if (code.StartsWith(parentCode + "a", StringComparison.OrdinalIgnoreCase))
                        {
                            int value;
                            if (int.TryParse(code.Substring((parentCode + "a").Length), out value) && value > max)
                            {
                                max = value;
                            }
                        }
                    }
                }
            }

            return parentCode + "a" + (max + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string GenerateAccountSerial(SqlConnection connection, SqlTransaction transaction, string parentCode, string accountCode)
        {
            var parentSerial = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Serial FROM dbo.ACCOUNTS WHERE Account_Code=@value", parentCode);
            return string.IsNullOrWhiteSpace(parentSerial) ? accountCode : parentSerial + "." + accountCode.Substring(parentCode.Length).TrimStart('a');
        }

        private static bool StoreNameExists(SqlConnection connection, SqlTransaction transaction, string storeName, int storeId)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.TblStore WHERE StoreName=@StoreName AND StoreID<>@StoreId", connection, transaction))
            {
                command.Parameters.Add("@StoreName", SqlDbType.NVarChar, 50).Value = Limit(storeName, 50);
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = storeId;
                return command.ExecuteScalar() != null;
            }
        }

        private static int AllocateStoreId(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(StoreID), 0) + 1 FROM dbo.TblStore WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void ReplaceAssignedUsers(SqlConnection connection, SqlTransaction transaction, int storeId, IEnumerable<int> userIds)
        {
            Execute(connection, transaction, "DELETE FROM dbo.TblUsersStores WHERE storeId=@StoreId", p => p.Add("@StoreId", SqlDbType.Int).Value = storeId);
            foreach (var userId in (userIds ?? new int[0]).Where(x => x > 0).Distinct())
            {
                Execute(connection, transaction, "INSERT INTO dbo.TblUsersStores (storeId, userid) VALUES (@StoreId, @UserId)", p =>
                {
                    p.Add("@StoreId", SqlDbType.Int).Value = storeId;
                    p.Add("@UserId", SqlDbType.Int).Value = userId;
                });
            }
        }

        private static IList<StoreUserLookupViewModel> LoadAssignedUsers(SqlConnection connection, SqlTransaction transaction, int storeId)
        {
            var users = new List<StoreUserLookupViewModel>();
            const string sql = @"
SELECT us.userid, u.UserName, e.Fullcode
FROM dbo.TblUsersStores us
INNER JOIN dbo.TblUsers u ON u.UserID = us.userid
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
WHERE us.StoreID = @StoreId OR us.storeId = @StoreId
ORDER BY us.id;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = storeId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new StoreUserLookupViewModel
                        {
                            UserId = ReadInt(reader, "userid"),
                            UserName = ReadString(reader, "UserName"),
                            EmployeeCode = ReadString(reader, "Fullcode")
                        });
                    }
                }
            }

            return users;
        }

        private static int CountVoucherAccountUse(SqlConnection connection, SqlTransaction transaction, StoreDataEditViewModel model)
        {
            var accounts = new[] { model.ParentAccount, model.InventoryAccount, model.LossAccount, model.SettlementAccount, model.GiftAccount }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (accounts.Count == 0)
            {
                return 0;
            }

            var filters = string.Join(",", accounts.Select((x, i) => "@a" + i));
            using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Account_Code IN (" + filters + ")", connection, transaction))
            {
                for (var i = 0; i < accounts.Count; i++)
                {
                    command.Parameters.Add("@a" + i, SqlDbType.NVarChar, 50).Value = accounts[i];
                }

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static IList<SelectListItem> LoadBranches(SqlConnection connection)
        {
            return LoadSelectList(connection, @"SELECT branch_id AS Id, COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N''), CONVERT(NVARCHAR(20), branch_id)) AS Name FROM dbo.TblBranchesData ORDER BY branch_id");
        }

        private static IList<SelectListItem> LoadEmployees(SqlConnection connection)
        {
            return LoadSelectList(connection, @"SELECT TOP (500) Emp_ID AS Id, COALESCE(NULLIF(Emp_Name, N''), NULLIF(Emp_Namee, N''), NULLIF(Fullcode, N''), CONVERT(NVARCHAR(20), Emp_ID)) AS Name FROM dbo.TblEmployee ORDER BY Emp_ID");
        }

        private static IList<SelectListItem> LoadAccounts(SqlConnection connection)
        {
            return LoadSelectList(connection, @"SELECT TOP (800) Account_Code AS Id, Account_Code + N' - ' + COALESCE(NULLIF(Account_Name, N''), NULLIF(Account_NameEng, N''), N'') AS Name FROM dbo.ACCOUNTS ORDER BY Account_Code");
        }

        private static IList<SelectListItem> LoadBoxes(SqlConnection connection)
        {
            if (!TableExists(connection, null, "TblBoxesData"))
            {
                return new List<SelectListItem>();
            }

            return LoadSelectList(connection, "SELECT TOP (300) BoxID AS Id, COALESCE(NULLIF(BoxName, N''), NULLIF(BoxNameE, N''), CONVERT(NVARCHAR(20), BoxID)) AS Name FROM dbo.TblBoxesData ORDER BY BoxID");
        }

        private static IList<StoreUserLookupViewModel> LoadUsers(SqlConnection connection)
        {
            var users = new List<StoreUserLookupViewModel>();
            const string sql = @"
SELECT TOP (500) u.UserID, u.UserName, e.Fullcode
FROM dbo.TblUsers u
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
WHERE ISNULL(u.isDeactivated, 0) = 0
ORDER BY u.UserName;";
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new StoreUserLookupViewModel
                    {
                        UserId = ReadInt(reader, "UserID"),
                        UserName = ReadString(reader, "UserName"),
                        EmployeeCode = ReadString(reader, "Fullcode")
                    });
                }
            }

            return users;
        }

        private static IList<SelectListItem> LoadSelectList(SqlConnection connection, string sql)
        {
            var rows = new List<SelectListItem>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new SelectListItem { Value = Convert.ToString(reader["Id"], CultureInfo.InvariantCulture), Text = ReadString(reader, "Name") });
                }
            }

            return rows;
        }

        private static BranchDefaults LoadBranchDefaults(SqlConnection connection, SqlTransaction transaction, int? branchId)
        {
            var branch = new BranchDefaults { BranchId = branchId.GetValueOrDefault() };
            if (!TableExists(connection, transaction, "branches"))
            {
                return branch;
            }

            var sql = branchId.HasValue && branchId.Value > 0
                ? "SELECT TOP 1 * FROM dbo.branches WHERE branch_id=@BranchId"
                : "SELECT TOP 1 * FROM dbo.branches ORDER BY branch_id";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (branchId.HasValue && branchId.Value > 0)
                {
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.Value;
                }

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        branch.BranchId = ReadInt(reader, "branch_id");
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            branch.Values[reader.GetName(i)] = reader.IsDBNull(i) ? string.Empty : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                        }
                    }
                }
            }

            return branch;
        }

        private static StoreOptions LoadOptions(SqlConnection connection, SqlTransaction transaction)
        {
            return new StoreOptions
            {
                StoreAccountHaveSettlement = ReadOptionBool(connection, transaction, "StoreAccountHaveSettelment"),
                EachStoreHaveLossAccount = ReadOptionBool(connection, transaction, "eachStoreHaveLossAccount"),
                EachStoreHaveGiftAccount = ReadOptionBool(connection, transaction, "eachStoreHaveGiftAccount")
            };
        }

        private static bool ReadOptionBool(SqlConnection connection, SqlTransaction transaction, string columnName)
        {
            if (!ColumnExists(connection, transaction, "TblOptions", columnName))
            {
                return false;
            }

            using (var command = new SqlCommand("SELECT TOP 1 [" + columnName + "] FROM dbo.TblOptions", connection, transaction))
            {
                var raw = command.ExecuteScalar();
                return raw != null && raw != DBNull.Value && Convert.ToBoolean(raw);
            }
        }

        private static bool TableExists(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID(N'dbo.' + @TableName, N'U')", connection, transaction))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                var raw = command.ExecuteScalar();
                return raw != null && raw != DBNull.Value;
            }
        }

        private static bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand(@"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME=@TableName AND COLUMN_NAME=@ColumnName", connection, transaction))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                return command.ExecuteScalar() != null;
            }
        }

        private static bool AccountExists(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            return !string.IsNullOrWhiteSpace(accountCode)
                && !string.IsNullOrWhiteSpace(LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM dbo.ACCOUNTS WHERE Account_Code=@value", accountCode));
        }

        private static string LookupScalar(SqlConnection connection, SqlTransaction transaction, string sql, string value)
        {
            return LookupScalar(connection, transaction, sql, value, null);
        }

        private static string LookupScalar(SqlConnection connection, SqlTransaction transaction, string sql, string value, string name)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@value", SqlDbType.NVarChar, 255).Value = value ?? string.Empty;
                if (name != null)
                {
                    command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = name;
                }

                var raw = command.ExecuteScalar();
                return raw == null || raw == DBNull.Value ? string.Empty : Convert.ToString(raw, CultureInfo.InvariantCulture);
            }
        }

        private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, Action<SqlParameterCollection> configure)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (configure != null)
                {
                    configure(command.Parameters);
                }

                command.ExecuteNonQuery();
            }
        }

        private static void AddText(SqlCommand command, string name, string value, int size)
        {
            var parameter = size < 0 ? command.Parameters.Add(name, SqlDbType.NVarChar) : command.Parameters.Add(name, SqlDbType.NVarChar, size);
            parameter.Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : Limit(value, size < 0 ? 4000 : size);
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, int? value)
        {
            command.Parameters.Add(name, type).Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, string value, int size)
        {
            var parameter = command.Parameters.Add(name, type, size);
            parameter.Value = string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value;
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values == null ? string.Empty : values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
        }

        private static string Limit(string value, int max)
        {
            value = Clean(value);
            return max > 0 && value.Length > max ? value.Substring(0, max) : value;
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var value = ReadNullableInt(reader, name);
            return value.GetValueOrDefault();
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            int value;
            return int.TryParse(Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture), out value) ? (int?)value : null;
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
        }

        private static DateTime? ReadNullableDate(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private class StoreOptions
        {
            public bool StoreAccountHaveSettlement { get; set; }
            public bool EachStoreHaveLossAccount { get; set; }
            public bool EachStoreHaveGiftAccount { get; set; }
        }

        private class AccountSeed
        {
            public int AccountTypes { get; set; }
            public int AccountTab { get; set; }
            public int DepitOrCredit { get; set; }
            public int DifferentType { get; set; }
            public int Authority { get; set; }
            public int? BranchId { get; set; }
        }

        private class BranchDefaults
        {
            public BranchDefaults()
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public int BranchId { get; set; }
            public IDictionary<string, string> Values { get; private set; }

            public string GetAccount(int index)
            {
                string value;
                return Values.TryGetValue("a" + index, out value) ? value : string.Empty;
            }
        }
    }
}
