using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.Items;

namespace MyERP.Areas.MainErp.Repositories.Items
{
    public class ItemsRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ItemsRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ItemsIndexViewModel LoadIndex(string searchText, int? groupId, int page, int pageSize, MainErpUserContext user)
        {
            var model = new ItemsIndexViewModel
            {
                SearchText = searchText,
                GroupId = groupId,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : pageSize,
                Groups = LoadGroups(),
                GroupTree = LoadGroupTree(),
                Units = LoadUnits(),
                Message = user != null ? user.DefaultsWarning : null
            };

            int totalCount;
            model.Results = Search(searchText, groupId, model.Page, model.PageSize, out totalCount);
            model.TotalCount = totalCount;
            return model;
        }

        public IList<ItemListItemViewModel> Search(string searchText, int? groupId, int page, int pageSize, out int totalCount)
        {
            var rows = new List<ItemListItemViewModel>();
            totalCount = 0;

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
WITH Rows AS
(
    SELECT
        ROW_NUMBER() OVER (ORDER BY i.ItemID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        i.ItemID,
        i.ItemCode,
        i.ItemName,
        i.ItemNamee,
        i.barCodeNO,
        i.PurchasePrice,
        i.SallingPrice,
        i.IsArchive,
        g.GroupName,
        du.UnitName AS DefaultUnitName
    FROM dbo.TblItems i
    LEFT JOIN dbo.Groups g ON g.GroupID = i.GroupID
    OUTER APPLY
    (
        SELECT TOP (1) u.UnitName
        FROM dbo.TblItemsUnits iu
        LEFT JOIN dbo.TblUnites u ON u.UnitID = iu.UnitID
        WHERE iu.ItemID = i.ItemID
        ORDER BY ISNULL(iu.DefaultUnit, 0) DESC, ISNULL(iu.SecOrder, 0), iu.UnitID
    ) du
    WHERE ISNULL(i.IsArchive, 0) = 0
      AND (@GroupId IS NULL OR i.GroupID = @GroupId)
      AND (@SearchText IS NULL
           OR i.ItemCode LIKE @SearchLike
           OR i.ItemName LIKE @SearchLike
           OR i.ItemNamee LIKE @SearchLike
           OR i.barCodeNO LIKE @SearchLike
           OR i.Fullcode LIKE @SearchLike)
)
SELECT * FROM Rows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;";
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                command.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 240).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText.Trim() + "%";
                command.Parameters.Add("@GroupId", SqlDbType.Int).Value = (object)groupId ?? DBNull.Value;
                command.Parameters.Add("@StartRow", SqlDbType.Int).Value = ((page - 1) * pageSize) + 1;
                command.Parameters.Add("@EndRow", SqlDbType.Int).Value = page * pageSize;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (totalCount == 0 && reader["TotalCount"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"], CultureInfo.InvariantCulture);
                        }

                        rows.Add(new ItemListItemViewModel
                        {
                            Id = ReadInt(reader, "ItemID"),
                            Code = ReadString(reader, "ItemCode"),
                            Name = ReadString(reader, "ItemName"),
                            NameEn = ReadString(reader, "ItemNamee"),
                            GroupName = ReadString(reader, "GroupName"),
                            Barcode = ReadString(reader, "barCodeNO"),
                            PurchasePrice = ReadDecimal(reader, "PurchasePrice"),
                            SalePrice = ReadDecimal(reader, "SallingPrice"),
                            DefaultUnitName = ReadString(reader, "DefaultUnitName"),
                            IsArchived = ReadBool(reader, "IsArchive")
                        });
                    }
                }
            }

            return rows;
        }

        public ItemDetailsViewModel GetDetails(int id)
        {
            var model = new ItemDetailsViewModel();
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT TOP (1)
    ItemID, ItemCode, ItemName, ItemNamee, GroupID, ItemType, PartNo, barCodeNO, CatlogNO, FactoryNO,
    PurchasePrice, SallingPrice, CustomerPrice, DealerPrice, CostPrice, minvalueqty, MaxValueqty,
    RequestLimit, HaveSerial, HaveGuarantee, GuaranteeValue, GuaranteeType, IsArchive, shortName,
    BinLocation, CAST(ItemComment AS nvarchar(max)) AS ItemComment
FROM dbo.TblItems
WHERE ItemID = @Id;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Id = ReadInt(reader, "ItemID");
                            model.Code = ReadString(reader, "ItemCode");
                            model.Name = ReadString(reader, "ItemName");
                            model.NameEn = ReadString(reader, "ItemNamee");
                            model.GroupId = ReadNullableInt(reader, "GroupID");
                            model.ItemType = ReadNullableInt(reader, "ItemType");
                            model.PartNo = ReadString(reader, "PartNo");
                            model.Barcode = ReadString(reader, "barCodeNO");
                            model.CatalogNo = ReadString(reader, "CatlogNO");
                            model.FactoryNo = ReadString(reader, "FactoryNO");
                            model.PurchasePrice = ReadDecimal(reader, "PurchasePrice");
                            model.SalePrice = ReadDecimal(reader, "SallingPrice");
                            model.CustomerPrice = ReadDecimal(reader, "CustomerPrice");
                            model.DealerPrice = ReadDecimal(reader, "DealerPrice");
                            model.CostPrice = ReadDecimal(reader, "CostPrice");
                            model.MinQty = ReadDecimal(reader, "minvalueqty");
                            model.MaxQty = ReadDecimal(reader, "MaxValueqty");
                            model.RequestLimit = ReadNullableInt(reader, "RequestLimit");
                            model.HaveSerial = ReadBool(reader, "HaveSerial");
                            model.HaveGuarantee = ReadBool(reader, "HaveGuarantee");
                            model.GuaranteeValue = ReadNullableInt(reader, "GuaranteeValue");
                            model.GuaranteeType = ReadNullableInt(reader, "GuaranteeType");
                            model.IsArchived = ReadBool(reader, "IsArchive");
                            model.ShortName = ReadString(reader, "shortName");
                            model.BinLocation = ReadString(reader, "BinLocation");
                            model.Notes = ReadString(reader, "ItemComment");
                        }
                    }
                }

                model.Units = LoadItemUnits(connection, id);
            }

            return model;
        }

        public IList<ItemLookupViewModel> LoadGroups()
        {
            var items = new List<ItemLookupViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT GroupID, GroupName, Fullcode FROM dbo.Groups ORDER BY GroupName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemLookupViewModel
                        {
                            Id = ReadInt(reader, "GroupID").ToString(CultureInfo.InvariantCulture),
                            Text = ReadString(reader, "GroupName"),
                            Extra = ReadString(reader, "Fullcode")
                        });
                    }
                }
            }

            return items;
        }

        public IList<ItemLookupViewModel> LoadUnits()
        {
            var items = new List<ItemLookupViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT UnitID, UnitName, UnitNamee FROM dbo.TblUnites ORDER BY UnitName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemLookupViewModel
                        {
                            Id = ReadInt(reader, "UnitID").ToString(CultureInfo.InvariantCulture),
                            Text = ReadString(reader, "UnitName"),
                            Extra = ReadString(reader, "UnitNamee")
                        });
                    }
                }
            }

            return items;
        }

        public IList<GroupListItemViewModel> LoadGroupTree()
        {
            var items = new List<GroupListItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    g.GroupID,
    g.ParentID,
    g.GroupCode,
    g.Fullcode,
    g.GroupName,
    g.GroupNamee,
    g.LastGroup,
    g.IsMaterial,
    g.POSGroup,
    g.IsProducer,
    g.IsAdditions,
    g.HoldingMaterials,
    g.Separate,
    g.IsTransfere,
    g.IsShowCover,
    g.chkTaxExempt,
    p.GroupName AS ParentName,
    (SELECT COUNT(1) FROM dbo.TblItems i WHERE i.GroupID = g.GroupID AND ISNULL(i.IsArchive, 0) = 0) AS ItemsCount,
    (SELECT COUNT(1) FROM dbo.Groups c WHERE c.ParentID = g.GroupID) AS ChildrenCount
FROM dbo.Groups g
LEFT JOIN dbo.Groups p ON p.GroupID = g.ParentID
ORDER BY ISNULL(g.ParentID, 0), g.GroupCode, g.GroupName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(ReadGroup(reader));
                    }
                }
            }

            return items;
        }

        public GroupListItemViewModel GetGroupDetails(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (1)
    g.GroupID,
    g.ParentID,
    g.GroupCode,
    g.Fullcode,
    g.GroupName,
    g.GroupNamee,
    g.LastGroup,
    g.IsMaterial,
    g.POSGroup,
    g.IsProducer,
    g.IsAdditions,
    g.HoldingMaterials,
    g.Separate,
    g.IsTransfere,
    g.IsShowCover,
    g.chkTaxExempt,
    p.GroupName AS ParentName,
    (SELECT COUNT(1) FROM dbo.TblItems i WHERE i.GroupID = g.GroupID AND ISNULL(i.IsArchive, 0) = 0) AS ItemsCount,
    (SELECT COUNT(1) FROM dbo.Groups c WHERE c.ParentID = g.GroupID) AS ChildrenCount
FROM dbo.Groups g
LEFT JOIN dbo.Groups p ON p.GroupID = g.ParentID
WHERE g.GroupID = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadGroup(reader) : null;
                }
            }
        }

        public GroupSaveResult SaveGroup(GroupSaveRequest request, MainErpUserContext user)
        {
            NormalizeGroup(request);
            var validation = ValidateGroup(request);
            if (validation != null)
            {
                return new GroupSaveResult { Success = false, Message = validation };
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    AcquireGroupLock(connection, transaction);
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "Groups", "GroupID") : request.Id.Value;
                    var code = string.IsNullOrWhiteSpace(request.Code)
                        ? NextGroupCode(connection, transaction, request.ParentId.GetValueOrDefault())
                        : request.Code.Trim();

                    var duplicate = FindGroupDuplicate(connection, transaction, id, request.Name);
                    if (duplicate != null)
                    {
                        transaction.Rollback();
                        return new GroupSaveResult { Success = false, Message = duplicate };
                    }

                    if (isNew)
                    {
                        InsertGroup(connection, transaction, id, code, request, user);
                    }
                    else
                    {
                        UpdateGroup(connection, transaction, id, code, request, user);
                    }

                    transaction.Commit();
                    return new GroupSaveResult { Success = true, Id = id, Code = code, Message = "تم حفظ المجموعة." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new GroupSaveResult { Success = false, Message = ex.Message };
                }
            }
        }

        public GroupSaveResult DeleteGroup(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    if (ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.TblItems WHERE GroupID = @Id AND ISNULL(IsArchive, 0) = 0", id) > 0)
                    {
                        transaction.Rollback();
                        return new GroupSaveResult { Success = false, Id = id, Message = "لا يمكن حذف المجموعة لأنها تحتوي على أصناف." };
                    }

                    if (ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.Groups WHERE ParentID = @Id", id) > 0)
                    {
                        transaction.Rollback();
                        return new GroupSaveResult { Success = false, Id = id, Message = "لا يمكن حذف المجموعة لأنها تحتوي على مجموعات فرعية." };
                    }

                    using (var command = new SqlCommand("DELETE FROM dbo.Groups WHERE GroupID = @Id AND GroupID <> 1;", connection, transaction))
                    {
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        var affected = command.ExecuteNonQuery();
                        transaction.Commit();
                        return new GroupSaveResult { Success = affected > 0, Id = id, Message = affected > 0 ? "تم حذف المجموعة." : "المجموعة غير موجودة." };
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new GroupSaveResult { Success = false, Id = id, Message = ex.Message };
                }
            }
        }

        public ItemSaveResult Save(ItemSaveRequest request, MainErpUserContext user)
        {
            Normalize(request);
            var validation = Validate(request);
            if (validation != null)
            {
                return new ItemSaveResult { Success = false, Message = validation };
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    AcquireItemLock(connection, transaction);
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "TblItems", "ItemID") : request.Id.Value;
                    var code = string.IsNullOrWhiteSpace(request.Code) ? NextItemCode(connection, transaction) : request.Code.Trim();

                    var duplicate = FindDuplicate(connection, transaction, id, code, request.Name, request.Barcode, request.Units);
                    if (duplicate != null)
                    {
                        transaction.Rollback();
                        return new ItemSaveResult { Success = false, Message = duplicate };
                    }

                    if (isNew)
                    {
                        InsertItem(connection, transaction, id, code, request, user);
                    }
                    else
                    {
                        UpdateItem(connection, transaction, id, code, request, user);
                    }

                    SaveUnits(connection, transaction, id, request);
                    transaction.Commit();
                    return new ItemSaveResult { Success = true, Id = id, Code = code, Message = "تم الحفظ بنجاح." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new ItemSaveResult { Success = false, Message = ex.Message };
                }
            }
        }

        public ItemSaveResult Delete(int id, MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE dbo.TblItems SET IsArchive = 1, LastUpdate = GETDATE(), UserID = @UserId WHERE ItemID = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user != null ? user.UserId : 0;
                var affected = command.ExecuteNonQuery();
                return new ItemSaveResult { Success = affected > 0, Id = id, Message = affected > 0 ? "تم الحذف." : "الصنف غير موجود." };
            }
        }

        private IList<ItemUnitLineViewModel> LoadItemUnits(SqlConnection connection, int itemId)
        {
            var lines = new List<ItemUnitLineViewModel>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT iu.UnitID, u.UnitName, iu.UnitFactor, iu.UnitSalesPrice, iu.UnitPurPrice, iu.MinSelingPrice,
       iu.MaxSelingPrice, iu.UnitWholeSalePrice, iu.DefaultUnit, iu.barCodeNo2
FROM dbo.TblItemsUnits iu
LEFT JOIN dbo.TblUnites u ON u.UnitID = iu.UnitID
WHERE iu.ItemID = @ItemId
ORDER BY ISNULL(iu.DefaultUnit, 0) DESC, ISNULL(iu.SecOrder, 0), iu.UnitID;";
                command.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lines.Add(new ItemUnitLineViewModel
                        {
                            UnitId = ReadNullableInt(reader, "UnitID"),
                            UnitName = ReadString(reader, "UnitName"),
                            UnitFactor = ReadDecimal(reader, "UnitFactor"),
                            SalePrice = ReadDecimal(reader, "UnitSalesPrice"),
                            PurchasePrice = ReadDecimal(reader, "UnitPurPrice"),
                            MinSellingPrice = ReadDecimal(reader, "MinSelingPrice"),
                            MaxSellingPrice = ReadDecimal(reader, "MaxSelingPrice"),
                            WholeSalePrice = ReadDecimal(reader, "UnitWholeSalePrice"),
                            IsDefault = ReadInt(reader, "DefaultUnit") == 1,
                            Barcode = ReadString(reader, "barCodeNo2")
                        });
                    }
                }
            }

            return lines;
        }

        private static void Normalize(ItemSaveRequest request)
        {
            if (request == null) return;
            request.Code = (request.Code ?? string.Empty).Trim();
            request.Name = (request.Name ?? string.Empty).Trim();
            request.NameEn = (request.NameEn ?? string.Empty).Trim();
            request.Barcode = (request.Barcode ?? string.Empty).Trim();
            request.Units = request.Units ?? new List<ItemUnitLineViewModel>();
            foreach (var unit in request.Units)
            {
                unit.Barcode = (unit.Barcode ?? string.Empty).Trim();
            }
        }

        private static string Validate(ItemSaveRequest request)
        {
            if (request == null) return "بيانات الحفظ غير مكتملة.";
            if (string.IsNullOrWhiteSpace(request.Name)) return "اسم الصنف مطلوب.";
            if (!request.GroupId.HasValue || request.GroupId.Value <= 0) return "مجموعة الصنف مطلوبة.";
            if (!request.ItemType.HasValue || request.ItemType.Value <= 0) return "نوع الصنف مطلوب.";
            if (request.HaveGuarantee && (!request.GuaranteeValue.HasValue || request.GuaranteeValue.Value <= 0)) return "مدة الضمان مطلوبة.";
            if (request.PurchasePrice < 0 || request.SalePrice < 0 || request.CustomerPrice < 0 || request.DealerPrice < 0 || request.CostPrice < 0) return "الأسعار والتكلفة لا تقبل قيما سالبة.";
            if (!request.Units.Any()) return "يجب إدخال وحدة واحدة على الأقل.";
            if (!request.Units.Any(u => u.IsDefault)) return "يجب تحديد وحدة افتراضية.";
            foreach (var unit in request.Units)
            {
                if (!unit.UnitId.HasValue || unit.UnitId.Value <= 0) return "الوحدة مطلوبة.";
                if (unit.UnitFactor <= 0) return "معامل الوحدة يجب أن يكون أكبر من صفر.";
                if (unit.SalePrice < 0 || unit.PurchasePrice < 0 || unit.MinSellingPrice < 0 || unit.MaxSellingPrice < 0 || unit.WholeSalePrice < 0) return "أسعار الوحدات لا تقبل قيما سالبة.";
            }

            var duplicateBarcode = request.Units.Where(u => !string.IsNullOrWhiteSpace(u.Barcode)).GroupBy(u => u.Barcode).FirstOrDefault(g => g.Count() > 1);
            if (duplicateBarcode != null) return "يوجد باركود مكرر داخل وحدات الصنف.";
            return null;
        }

        private static void AcquireItemLock(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("sp_getapplock", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Resource", "MainErp.Legacy.FrmItems");
                command.Parameters.AddWithValue("@LockMode", "Exclusive");
                command.Parameters.AddWithValue("@LockOwner", "Transaction");
                command.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                command.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value, CultureInfo.InvariantCulture) < 0)
                {
                    throw new TimeoutException("تعذر الحصول على قفل حفظ الأصناف.");
                }
            }
        }

        private static void AcquireGroupLock(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("sp_getapplock", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Resource", "MainErp.Legacy.FrmGroups");
                command.Parameters.AddWithValue("@LockMode", "Exclusive");
                command.Parameters.AddWithValue("@LockOwner", "Transaction");
                command.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                command.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value, CultureInfo.InvariantCulture) < 0)
                {
                    throw new TimeoutException("تعذر الحصول على قفل حفظ المجموعات.");
                }
            }
        }

        private static int NextInt(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand(string.Format("SELECT ISNULL(MAX([{0}]), 0) + 1 FROM dbo.[{1}] WITH (UPDLOCK, HOLDLOCK);", columnName, tableName), connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static string NextItemCode(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(MAX(CAST(ItemCode AS int)), 0) + 1
FROM dbo.TblItems WITH (UPDLOCK, HOLDLOCK)
WHERE ISNUMERIC(ItemCode) = 1;", connection, transaction))
            {
                var next = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                return next.ToString("000000000", CultureInfo.InvariantCulture);
            }
        }

        private static string NextGroupCode(SqlConnection connection, SqlTransaction transaction, int parentId)
        {
            var parentCode = string.Empty;
            using (var command = new SqlCommand("SELECT ISNULL(GroupCode, N'') FROM dbo.Groups WHERE GroupID = @ParentId;", connection, transaction))
            {
                command.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;
                var raw = command.ExecuteScalar();
                parentCode = raw == null || raw == DBNull.Value ? string.Empty : raw.ToString();
            }

            using (var command = new SqlCommand(@"
SELECT TOP (1) GroupCode
FROM dbo.Groups WITH (UPDLOCK, HOLDLOCK)
WHERE ParentID = @ParentId AND ISNULL(GroupCode, N'') <> N''
ORDER BY GroupID DESC;", connection, transaction))
            {
                command.Parameters.Add("@ParentId", SqlDbType.Int).Value = parentId;
                var raw = command.ExecuteScalar();
                if (raw == null || raw == DBNull.Value)
                {
                    return parentCode + "1";
                }

                var lastCode = raw.ToString();
                var suffix = lastCode.Length > parentCode.Length ? lastCode.Substring(parentCode.Length) : lastCode;
                int lastNumber;
                if (!int.TryParse(suffix, out lastNumber))
                {
                    lastNumber = 0;
                }

                return parentCode + (lastNumber + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string FindDuplicate(SqlConnection connection, SqlTransaction transaction, int id, string code, string name, string barcode, IList<ItemUnitLineViewModel> units)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
    CASE
        WHEN ItemCode = @Code AND @Code <> '' THEN N'كود الصنف مستخدم من قبل.'
        WHEN ItemName = @Name THEN N'اسم الصنف مستخدم من قبل.'
        WHEN barCodeNO = @Barcode AND @Barcode <> '' THEN N'باركود الصنف مستخدم من قبل.'
    END
FROM dbo.TblItems
WHERE ISNULL(IsArchive, 0) = 0
  AND ItemID <> @Id
  AND ((ItemCode = @Code AND @Code <> '') OR ItemName = @Name OR (barCodeNO = @Barcode AND @Barcode <> ''));", connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = code ?? string.Empty;
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 4000).Value = name ?? string.Empty;
                command.Parameters.Add("@Barcode", SqlDbType.NVarChar, 255).Value = barcode ?? string.Empty;
                var raw = command.ExecuteScalar();
                if (raw != null && raw != DBNull.Value)
                {
                    return raw.ToString();
                }
            }

            var unitBarcodes = units
                .Where(u => !string.IsNullOrWhiteSpace(u.Barcode))
                .Select(u => u.Barcode.Trim())
                .Distinct()
                .ToList();
            foreach (var unitBarcode in unitBarcodes)
            {
                using (var command = new SqlCommand(@"
SELECT TOP (1) 1
FROM dbo.TblItemsUnits iu
INNER JOIN dbo.TblItems i ON i.ItemID = iu.ItemID
WHERE ISNULL(i.IsArchive, 0) = 0
  AND iu.ItemID <> @ItemId
  AND iu.barCodeNo2 = @Barcode;", connection, transaction))
                {
                    command.Parameters.Add("@ItemId", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@Barcode", SqlDbType.NVarChar, 255).Value = unitBarcode;
                    if (command.ExecuteScalar() != null)
                    {
                        return "باركود الوحدة مستخدم من قبل.";
                    }
                }
            }

            return null;
        }

        private static string FindGroupDuplicate(SqlConnection connection, SqlTransaction transaction, int id, string name)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) N'اسم المجموعة مستخدم من قبل.'
FROM dbo.Groups
WHERE GroupID <> @Id AND GroupName = @Name;", connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = name ?? string.Empty;
                var raw = command.ExecuteScalar();
                return raw == null || raw == DBNull.Value ? null : raw.ToString();
            }
        }

        private static void NormalizeGroup(GroupSaveRequest request)
        {
            if (request == null) return;
            request.Code = (request.Code ?? string.Empty).Trim();
            request.FullCode = (request.FullCode ?? string.Empty).Trim();
            request.Name = (request.Name ?? string.Empty).Trim();
            request.NameEn = (request.NameEn ?? string.Empty).Trim();
        }

        private static string ValidateGroup(GroupSaveRequest request)
        {
            if (request == null) return "بيانات المجموعة غير مكتملة.";
            if (string.IsNullOrWhiteSpace(request.Name)) return "اسم المجموعة مطلوب.";
            if (!request.ParentId.HasValue) return "المجموعة الرئيسية مطلوبة.";
            if (request.Id.HasValue && request.Id.Value > 0 && request.ParentId.Value == request.Id.Value) return "لا يمكن اختيار نفس المجموعة كأب.";
            return null;
        }

        private static void InsertGroup(SqlConnection connection, SqlTransaction transaction, int id, string code, GroupSaveRequest request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.Groups
(
    GroupID, GroupName, GroupNamee, ParentID, GroupCode, Fullcode, LastGroup, IsMaterial,
    POSGroup, IsProducer, IsAdditions, HoldingMaterials, Separate, IsTransfere, IsShowCover,
    chkTaxExempt, BranchID
)
VALUES
(
    @GroupID, @GroupName, @GroupNamee, @ParentID, @GroupCode, @Fullcode, @LastGroup, @IsMaterial,
    @POSGroup, @IsProducer, @IsAdditions, @HoldingMaterials, @Separate, @IsTransfere, @IsShowCover,
    @TaxExempt, @BranchID
);", connection, transaction))
            {
                AddGroupParameters(command, id, code, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateGroup(SqlConnection connection, SqlTransaction transaction, int id, string code, GroupSaveRequest request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(@"
UPDATE dbo.Groups
SET GroupName = @GroupName,
    GroupNamee = @GroupNamee,
    ParentID = @ParentID,
    GroupCode = @GroupCode,
    Fullcode = @Fullcode,
    LastGroup = @LastGroup,
    IsMaterial = @IsMaterial,
    POSGroup = @POSGroup,
    IsProducer = @IsProducer,
    IsAdditions = @IsAdditions,
    HoldingMaterials = @HoldingMaterials,
    Separate = @Separate,
    IsTransfere = @IsTransfere,
    IsShowCover = @IsShowCover,
    chkTaxExempt = @TaxExempt,
    BranchID = @BranchID
WHERE GroupID = @GroupID;", connection, transaction))
            {
                AddGroupParameters(command, id, code, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static void AddGroupParameters(SqlCommand command, int id, string code, GroupSaveRequest request, MainErpUserContext user)
        {
            command.Parameters.Add("@GroupID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@GroupName", SqlDbType.NVarChar, 50).Value = request.Name;
            command.Parameters.Add("@GroupNamee", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.NameEn);
            command.Parameters.Add("@ParentID", SqlDbType.Int).Value = request.ParentId.Value;
            command.Parameters.Add("@GroupCode", SqlDbType.NVarChar, 255).Value = code;
            command.Parameters.Add("@Fullcode", SqlDbType.NVarChar, 255).Value = string.IsNullOrWhiteSpace(request.FullCode) ? (object)code : request.FullCode;
            command.Parameters.Add("@LastGroup", SqlDbType.Bit).Value = request.IsLastGroup;
            command.Parameters.Add("@IsMaterial", SqlDbType.Int).Value = request.IsMaterial ? 1 : 0;
            command.Parameters.Add("@POSGroup", SqlDbType.Bit).Value = request.IsPosGroup;
            command.Parameters.Add("@IsProducer", SqlDbType.Bit).Value = request.IsProducer;
            command.Parameters.Add("@IsAdditions", SqlDbType.Bit).Value = request.IsAdditions;
            command.Parameters.Add("@HoldingMaterials", SqlDbType.Bit).Value = request.HoldingMaterials;
            command.Parameters.Add("@Separate", SqlDbType.Int).Value = request.Separate ? 1 : 0;
            command.Parameters.Add("@IsTransfere", SqlDbType.Bit).Value = request.IsTransfere;
            command.Parameters.Add("@IsShowCover", SqlDbType.Bit).Value = request.IsShowCover;
            command.Parameters.Add("@TaxExempt", SqlDbType.Bit).Value = request.TaxExempt;
            command.Parameters.Add("@BranchID", SqlDbType.Int).Value = user != null && user.BranchId.HasValue ? (object)user.BranchId.Value : DBNull.Value;
        }

        private static int ScalarInt(SqlConnection connection, SqlTransaction transaction, string sql, int id)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void InsertItem(SqlConnection connection, SqlTransaction transaction, int id, string code, ItemSaveRequest request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.TblItems
(
    ItemID, ItemCode, ItemName, ItemNamee, GroupID, ItemType, PartNo, barCodeNO, CatlogNO, FactoryNO,
    PurchasePrice, SallingPrice, CustomerPrice, DealerPrice, CostPrice, minvalueqty, MaxValueqty,
    RequestLimit, HaveSerial, HaveGuarantee, GuaranteeValue, GuaranteeType, IsArchive, shortName,
    BinLocation, ItemComment, LastUpdate, UserID, Fullcode
)
VALUES
(
    @ItemID, @ItemCode, @ItemName, @ItemNamee, @GroupID, @ItemType, @PartNo, @Barcode, @CatalogNo, @FactoryNo,
    @PurchasePrice, @SalePrice, @CustomerPrice, @DealerPrice, @CostPrice, @MinQty, @MaxQty,
    @RequestLimit, @HaveSerial, @HaveGuarantee, @GuaranteeValue, @GuaranteeType, @IsArchive, @ShortName,
    @BinLocation, @Notes, GETDATE(), @UserID, @ItemCode
);", connection, transaction))
            {
                AddItemParameters(command, id, code, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateItem(SqlConnection connection, SqlTransaction transaction, int id, string code, ItemSaveRequest request, MainErpUserContext user)
        {
            using (var command = new SqlCommand(@"
UPDATE dbo.TblItems
SET ItemCode = @ItemCode,
    ItemName = @ItemName,
    ItemNamee = @ItemNamee,
    GroupID = @GroupID,
    ItemType = @ItemType,
    PartNo = @PartNo,
    barCodeNO = @Barcode,
    CatlogNO = @CatalogNo,
    FactoryNO = @FactoryNo,
    PurchasePrice = @PurchasePrice,
    SallingPrice = @SalePrice,
    CustomerPrice = @CustomerPrice,
    DealerPrice = @DealerPrice,
    CostPrice = @CostPrice,
    minvalueqty = @MinQty,
    MaxValueqty = @MaxQty,
    RequestLimit = @RequestLimit,
    HaveSerial = @HaveSerial,
    HaveGuarantee = @HaveGuarantee,
    GuaranteeValue = @GuaranteeValue,
    GuaranteeType = @GuaranteeType,
    IsArchive = @IsArchive,
    shortName = @ShortName,
    BinLocation = @BinLocation,
    ItemComment = @Notes,
    LastUpdate = GETDATE(),
    UserID = @UserID,
    Fullcode = @ItemCode
WHERE ItemID = @ItemID;", connection, transaction))
            {
                AddItemParameters(command, id, code, request, user);
                command.ExecuteNonQuery();
            }
        }

        private static void AddItemParameters(SqlCommand command, int id, string code, ItemSaveRequest request, MainErpUserContext user)
        {
            command.Parameters.Add("@ItemID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 50).Value = code;
            command.Parameters.Add("@ItemName", SqlDbType.NVarChar, 4000).Value = request.Name;
            command.Parameters.Add("@ItemNamee", SqlDbType.NVarChar, 4000).Value = (object)request.NameEn ?? DBNull.Value;
            command.Parameters.Add("@GroupID", SqlDbType.Int).Value = (object)request.GroupId ?? DBNull.Value;
            command.Parameters.Add("@ItemType", SqlDbType.Int).Value = (object)request.ItemType ?? DBNull.Value;
            command.Parameters.Add("@PartNo", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.PartNo);
            command.Parameters.Add("@Barcode", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.Barcode);
            command.Parameters.Add("@CatalogNo", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.CatalogNo);
            command.Parameters.Add("@FactoryNo", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.FactoryNo);
            command.Parameters.Add("@PurchasePrice", SqlDbType.Float).Value = Convert.ToDouble(request.PurchasePrice);
            command.Parameters.Add("@SalePrice", SqlDbType.Float).Value = Convert.ToDouble(request.SalePrice);
            command.Parameters.Add("@CustomerPrice", SqlDbType.Float).Value = Convert.ToDouble(request.CustomerPrice);
            command.Parameters.Add("@DealerPrice", SqlDbType.Float).Value = Convert.ToDouble(request.DealerPrice);
            command.Parameters.Add("@CostPrice", SqlDbType.Float).Value = Convert.ToDouble(request.CostPrice);
            command.Parameters.Add("@MinQty", SqlDbType.Float).Value = Convert.ToDouble(request.MinQty);
            command.Parameters.Add("@MaxQty", SqlDbType.Float).Value = Convert.ToDouble(request.MaxQty);
            command.Parameters.Add("@RequestLimit", SqlDbType.Int).Value = (object)request.RequestLimit ?? DBNull.Value;
            command.Parameters.Add("@HaveSerial", SqlDbType.Bit).Value = request.HaveSerial;
            command.Parameters.Add("@HaveGuarantee", SqlDbType.Bit).Value = request.HaveGuarantee;
            command.Parameters.Add("@GuaranteeValue", SqlDbType.Int).Value = (object)request.GuaranteeValue ?? DBNull.Value;
            command.Parameters.Add("@GuaranteeType", SqlDbType.Int).Value = (object)request.GuaranteeType ?? DBNull.Value;
            command.Parameters.Add("@IsArchive", SqlDbType.Bit).Value = request.IsArchived;
            command.Parameters.Add("@ShortName", SqlDbType.NVarChar, 4000).Value = EmptyToDbNull(request.ShortName);
            command.Parameters.Add("@BinLocation", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(request.BinLocation);
            command.Parameters.Add("@Notes", SqlDbType.NVarChar).Value = EmptyToDbNull(request.Notes);
            command.Parameters.Add("@UserID", SqlDbType.Int).Value = user != null ? user.UserId : 0;
        }

        private static void SaveUnits(SqlConnection connection, SqlTransaction transaction, int itemId, ItemSaveRequest request)
        {
            using (var delete = new SqlCommand("DELETE FROM dbo.TblItemsUnits WHERE ItemID = @ItemID;", connection, transaction))
            {
                delete.Parameters.Add("@ItemID", SqlDbType.Int).Value = itemId;
                delete.ExecuteNonQuery();
            }

            var order = 1;
            foreach (var unit in request.Units)
            {
                using (var command = new SqlCommand(@"
INSERT INTO dbo.TblItemsUnits
(
    JunckID, ItemID, UnitID, UnitFactor, SecOrder, DefaultUnit, UnitSalesPrice, UnitPurPrice,
    FactorByDefaultUnit, FactorBySmallUnit, MinSelingPrice, MaxSelingPrice, UnitWholeSalePrice, barCodeNo2
)
VALUES
(
    @JunckID, @ItemID, @UnitID, @UnitFactor, @SecOrder, @DefaultUnit, @UnitSalesPrice, @UnitPurPrice,
    @FactorByDefaultUnit, @FactorBySmallUnit, @MinSelingPrice, @MaxSelingPrice, @UnitWholeSalePrice, @Barcode
);", connection, transaction))
                {
                    command.Parameters.Add("@JunckID", SqlDbType.Int).Value = NextInt(connection, transaction, "TblItemsUnits", "JunckID");
                    command.Parameters.Add("@ItemID", SqlDbType.Int).Value = itemId;
                    command.Parameters.Add("@UnitID", SqlDbType.Int).Value = unit.UnitId.Value;
                    command.Parameters.Add("@UnitFactor", SqlDbType.Decimal).Value = unit.UnitFactor;
                    command.Parameters.Add("@SecOrder", SqlDbType.Int).Value = order++;
                    command.Parameters.Add("@DefaultUnit", SqlDbType.Int).Value = unit.IsDefault ? 1 : 0;
                    command.Parameters.Add("@UnitSalesPrice", SqlDbType.Money).Value = unit.SalePrice;
                    command.Parameters.Add("@UnitPurPrice", SqlDbType.Money).Value = unit.PurchasePrice;
                    command.Parameters.Add("@FactorByDefaultUnit", SqlDbType.Decimal).Value = unit.IsDefault ? 1 : unit.UnitFactor;
                    command.Parameters.Add("@FactorBySmallUnit", SqlDbType.Decimal).Value = unit.UnitFactor;
                    command.Parameters.Add("@MinSelingPrice", SqlDbType.Float).Value = Convert.ToDouble(unit.MinSellingPrice);
                    command.Parameters.Add("@MaxSelingPrice", SqlDbType.Float).Value = Convert.ToDouble(unit.MaxSellingPrice);
                    command.Parameters.Add("@UnitWholeSalePrice", SqlDbType.Float).Value = Convert.ToDouble(unit.WholeSalePrice);
                    command.Parameters.Add("@Barcode", SqlDbType.NVarChar, 255).Value = EmptyToDbNull(unit.Barcode);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static object EmptyToDbNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static GroupListItemViewModel ReadGroup(IDataRecord reader)
        {
            return new GroupListItemViewModel
            {
                Id = ReadInt(reader, "GroupID"),
                ParentId = ReadNullableInt(reader, "ParentID"),
                Code = ReadString(reader, "GroupCode"),
                FullCode = ReadString(reader, "Fullcode"),
                Name = ReadString(reader, "GroupName"),
                NameEn = ReadString(reader, "GroupNamee"),
                IsLastGroup = ReadBool(reader, "LastGroup"),
                IsMaterial = ReadInt(reader, "IsMaterial") == 1,
                IsPosGroup = ReadBool(reader, "POSGroup"),
                IsProducer = ReadBool(reader, "IsProducer"),
                IsAdditions = ReadBool(reader, "IsAdditions"),
                HoldingMaterials = ReadBool(reader, "HoldingMaterials"),
                Separate = ReadInt(reader, "Separate") == 1,
                IsTransfere = ReadBool(reader, "IsTransfere"),
                IsShowCover = ReadBool(reader, "IsShowCover"),
                TaxExempt = ReadBool(reader, "chkTaxExempt"),
                ItemsCount = ReadInt(reader, "ItemsCount"),
                ChildrenCount = ReadInt(reader, "ChildrenCount"),
                ParentName = ReadString(reader, "ParentName")
            };
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
    }
}
