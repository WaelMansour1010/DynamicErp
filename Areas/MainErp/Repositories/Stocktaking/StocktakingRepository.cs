using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.Stocktaking;

namespace MyERP.Areas.MainErp.Repositories.Stocktaking
{
    public class StocktakingRepository
    {
        private const int StocktakingTransactionType = 30;
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public StocktakingRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IList<StocktakingListItemViewModel> Search(string searchText, int? storeId, int? branchId)
        {
            var rows = new List<StocktakingListItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100
    t.Transaction_ID,
    t.Transaction_Serial,
    t.Transaction_Date,
    s.StoreName,
    b.branch_name,
    CASE WHEN ISNUMERIC(t.Nots) = 1 THEN CONVERT(int, t.Nots) ELSE NULL END AS Nots,
    CASE WHEN ISNUMERIC(t.Nots2) = 1 THEN CONVERT(int, t.Nots2) ELSE NULL END AS Nots2,
    COUNT(d.ID) AS LinesCount,
    ISNULL(SUM(ISNULL(d.ShowQty, d.Quantity) * ISNULL(d.showPrice, d.Price)), 0) AS TotalValue
FROM dbo.Transactions t
LEFT JOIN dbo.TblStore s ON s.StoreID = t.StoreID
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
LEFT JOIN dbo.Transaction_Details d ON d.Transaction_ID = t.Transaction_ID
WHERE t.Transaction_Type = @Type
  AND (@StoreId IS NULL OR t.StoreID = @StoreId)
  AND (@BranchId IS NULL OR t.BranchId = @BranchId)
  AND (@Search IS NULL OR t.Transaction_Serial LIKE @LikeSearch OR CONVERT(nvarchar(30), t.Transaction_ID) = @Search)
GROUP BY t.Transaction_ID, t.Transaction_Serial, t.Transaction_Date, s.StoreName, b.branch_name, t.Nots, t.Nots2
ORDER BY t.Transaction_ID DESC;";
                command.Parameters.Add("@Type", SqlDbType.Int).Value = StocktakingTransactionType;
                AddNullable(command, "@StoreId", SqlDbType.Int, storeId);
                AddNullable(command, "@BranchId", SqlDbType.Int, branchId);
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim(), 100);
                AddNullable(command, "@LikeSearch", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(searchText) ? null : "%" + searchText.Trim() + "%", 120);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new StocktakingListItemViewModel
                        {
                            Id = GetInt(reader, "Transaction_ID"),
                            Serial = GetString(reader, "Transaction_Serial"),
                            Date = GetDate(reader, "Transaction_Date"),
                            StoreName = GetString(reader, "StoreName"),
                            BranchName = GetString(reader, "branch_name"),
                            LinesCount = GetInt(reader, "LinesCount"),
                            TotalValue = GetDecimal(reader, "TotalValue"),
                            NotS = GetNullableInt(reader, "Nots"),
                            NotS2 = GetNullableInt(reader, "Nots2")
                        });
                    }
                }
            }

            return rows;
        }

        public StocktakingDetailsViewModel GetDetails(int id)
        {
            StocktakingDetailsViewModel model = null;
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT Transaction_ID, Transaction_Serial, Transaction_Date, StoreID, BranchId, GardFromDate, GardTodate, GardEntryType,
       StartGard, StartSetelment, Account1, Account2, chkAutoDetect,
       CASE WHEN ISNUMERIC(Nots) = 1 THEN CONVERT(int, Nots) ELSE NULL END AS Nots,
       CASE WHEN ISNUMERIC(Nots2) = 1 THEN CONVERT(int, Nots2) ELSE NULL END AS Nots2
FROM dbo.Transactions
WHERE Transaction_ID = @Id AND Transaction_Type = @Type;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@Type", SqlDbType.Int).Value = StocktakingTransactionType;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new StocktakingDetailsViewModel
                            {
                                Id = GetInt(reader, "Transaction_ID"),
                                Serial = GetString(reader, "Transaction_Serial"),
                                Date = GetDate(reader, "Transaction_Date"),
                                StoreId = GetNullableInt(reader, "StoreID"),
                                BranchId = GetNullableInt(reader, "BranchId"),
                                FromDate = GetDate(reader, "GardFromDate"),
                                ToDate = GetDate(reader, "GardTodate"),
                                GardEntryType = GetNullableInt(reader, "GardEntryType") ?? 2,
                                StartGard = GetBool(reader, "StartGard"),
                                StartSettlement = GetBool(reader, "StartSetelment"),
                                AutoDetect = GetBool(reader, "chkAutoDetect"),
                                Account1 = GetString(reader, "Account1"),
                                Account2 = GetString(reader, "Account2"),
                                NotS = GetNullableInt(reader, "Nots"),
                                NotS2 = GetNullableInt(reader, "Nots2")
                            };
                        }
                    }
                }

                if (model == null)
                {
                    return null;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT d.Item_ID, i.ItemCode, i.ItemName, d.UnitId, u.UnitName, d.ShowQty, d.Quantity, d.QtyBySmalltUnit,
       d.Price, d.showPrice, d.ItemSerial, d.ItemCase, d.ColorID, d.ItemSize, d.ClassId, d.GardQty,
       d.Gardresult, d.Gardresult1, d.Gardresult2, d.LotNO, d.ProductionDate, d.ExpiryDate,
       d.ParrtNoCode, d.ItemDetailedCode, d.AutoDetect, d.Height, d.Width, d.Length, d.Area
FROM dbo.Transaction_Details d
LEFT JOIN dbo.TblItems i ON i.ItemID = d.Item_ID
LEFT JOIN dbo.TblUnites u ON u.UnitID = d.UnitId
WHERE d.Transaction_ID = @Id
ORDER BY d.ID;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Lines.Add(new StocktakingLineViewModel
                            {
                                ItemId = GetNullableInt(reader, "Item_ID"),
                                ItemCode = GetString(reader, "ItemCode"),
                                ItemName = GetString(reader, "ItemName"),
                                UnitId = GetNullableInt(reader, "UnitId"),
                                UnitName = GetString(reader, "UnitName"),
                                Count = GetDecimal(reader, "ShowQty"),
                                Price = GetDecimal(reader, "showPrice"),
                                Serial = GetString(reader, "ItemSerial"),
                                ItemCase = GetNullableInt(reader, "ItemCase") ?? 0,
                                ColorId = GetNullableInt(reader, "ColorID") ?? 1,
                                ItemSize = GetString(reader, "ItemSize"),
                                ClassId = GetNullableInt(reader, "ClassId") ?? 1,
                                GardQty = GetNullableDecimal(reader, "GardQty"),
                                GardResult = GetNullableDecimal(reader, "Gardresult"),
                                GardResult1 = GetNullableDecimal(reader, "Gardresult1"),
                                GardResult2 = GetNullableDecimal(reader, "Gardresult2"),
                                LotNo = GetString(reader, "LotNO"),
                                ProductionDate = GetDate(reader, "ProductionDate"),
                                ExpiryDate = GetDate(reader, "ExpiryDate"),
                                PartNoCode = GetString(reader, "ParrtNoCode"),
                                ItemDetailedCode = GetString(reader, "ItemDetailedCode"),
                                AutoDetect = GetNullableInt(reader, "AutoDetect") ?? 0,
                                Height = GetNullableDecimal(reader, "Height"),
                                Width = GetNullableDecimal(reader, "Width"),
                                Length = GetNullableDecimal(reader, "Length"),
                                Area = GetNullableDecimal(reader, "Area")
                            });
                        }
                    }
                }
            }

            return model;
        }

        public IList<StocktakingLookupItem> LoadStores()
        {
            return LoadLookup("SELECT StoreID, StoreName FROM dbo.TblStore ORDER BY StoreName", "StoreID", "StoreName");
        }

        public IList<StocktakingLookupItem> LoadBranches()
        {
            return LoadLookup("SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_name", "branch_id", "branch_name");
        }

        public IList<StocktakingLookupItem> LoadUnits()
        {
            return LoadLookup("SELECT UnitID, UnitName FROM dbo.TblUnites ORDER BY UnitName", "UnitID", "UnitName");
        }

        public IList<StocktakingItemLookup> LoadItems()
        {
            var rows = new List<StocktakingItemLookup>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 500 i.ItemID, i.ItemCode, i.ItemName, i.HaveSerial, ISNULL(i.CostPrice, ISNULL(i.PurchasePrice, 0)) AS Price,
       u.UnitID, un.UnitName
FROM dbo.TblItems i
OUTER APPLY (
    SELECT TOP 1 iu.UnitID
    FROM dbo.TblItemsUnits iu
    WHERE iu.ItemID = i.ItemID
    ORDER BY CASE WHEN ISNULL(iu.DefaultUnit, 0) = 1 THEN 0 ELSE 1 END, iu.SecOrder
) u
LEFT JOIN dbo.TblUnites un ON un.UnitID = u.UnitID
WHERE ISNULL(i.IsArchive, 0) = 0 AND ISNULL(i.ItemType, 0) = 0
ORDER BY i.ItemCode, i.ItemName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new StocktakingItemLookup
                        {
                            Id = Convert.ToString(GetInt(reader, "ItemID")),
                            Text = GetString(reader, "ItemName"),
                            Code = GetString(reader, "ItemCode"),
                            HaveSerial = GetBool(reader, "HaveSerial"),
                            Price = GetDecimal(reader, "Price"),
                            DefaultUnitId = GetNullableInt(reader, "UnitID"),
                            DefaultUnitName = GetString(reader, "UnitName")
                        });
                    }
                }
            }

            return rows;
        }

        public StocktakingSaveResult Save(StocktakingSaveRequest request, MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextManualId(connection, transaction, "Transactions", "Transaction_ID", null) : request.Id.Value;
                    var serial = string.IsNullOrWhiteSpace(request.Serial)
                        ? Convert.ToString(NextManualId(connection, transaction, "Transactions", "Transaction_Serial", "Transaction_Type=30"))
                        : request.Serial.Trim();

                    if (isNew)
                    {
                        InsertHeader(connection, transaction, request, user, id, serial);
                    }
                    else
                    {
                        UpdateHeader(connection, transaction, request, user, id, serial);
                        DeleteDetails(connection, transaction, id);
                    }

                    foreach (var line in request.Lines)
                    {
                        if (line == null || !line.ItemId.HasValue || line.ItemId.Value <= 0)
                        {
                            continue;
                        }

                        InsertLine(connection, transaction, id, request.BranchId, line);
                    }

                    transaction.Commit();
                    return new StocktakingSaveResult { Success = true, Id = id, Serial = serial, Message = "تم حفظ الجرد بنجاح." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new StocktakingSaveResult { Success = false, Message = "تعذر حفظ الجرد: " + ex.Message };
                }
            }
        }

        public StocktakingSaveResult Delete(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    DeleteDetails(connection, transaction, id);
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "DELETE FROM dbo.Transactions WHERE Transaction_ID = @Id AND Transaction_Type = @Type";
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        command.Parameters.Add("@Type", SqlDbType.Int).Value = StocktakingTransactionType;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new StocktakingSaveResult { Success = true, Message = "تم حذف الجرد." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new StocktakingSaveResult { Success = false, Message = "تعذر حذف الجرد: " + ex.Message };
                }
            }
        }

        private static void InsertHeader(SqlConnection connection, SqlTransaction transaction, StocktakingSaveRequest request, MainErpUserContext user, int id, string serial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.Transactions
(Transaction_ID, Transaction_Serial, Transaction_Date, GardFromDate, GardTodate, GardEntryType, Transaction_Type, UserID, StoreID, BranchId,
 opening_balance_voucher_id, StartGard, StartSetelment, Account1, Account2, chkAutoDetect, Closed)
VALUES
(@Id, @Serial, @Date, @FromDate, @ToDate, @EntryType, @Type, @UserId, @StoreId, @BranchId,
 0, @StartGard, @StartSettlement, @Account1, @Account2, @AutoDetect, 0);";
                AddHeaderParameters(command, request, user, id, serial);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateHeader(SqlConnection connection, SqlTransaction transaction, StocktakingSaveRequest request, MainErpUserContext user, int id, string serial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.Transactions
SET Transaction_Serial = @Serial,
    Transaction_Date = @Date,
    GardFromDate = @FromDate,
    GardTodate = @ToDate,
    GardEntryType = @EntryType,
    UserID = @UserId,
    StoreID = @StoreId,
    BranchId = @BranchId,
    StartGard = @StartGard,
    StartSetelment = @StartSettlement,
    Account1 = @Account1,
    Account2 = @Account2,
    chkAutoDetect = @AutoDetect
WHERE Transaction_ID = @Id AND Transaction_Type = @Type;";
                AddHeaderParameters(command, request, user, id, serial);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertLine(SqlConnection connection, SqlTransaction transaction, int id, int? branchId, StocktakingLineViewModel line)
        {
            var unitFactor = GetUnitFactor(connection, transaction, line.ItemId.Value, line.UnitId);
            var showQty = line.Count;
            var quantity = showQty * unitFactor;
            var showPrice = line.Price;
            var price = unitFactor == 0 ? 0 : showPrice / unitFactor;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.Transaction_Details
(Transaction_ID, AutoDetect, Item_ID, Quantity, ItemSerial, ParrtNoCode, ItemDetailedCode, ItemCase, Price, ColorID, ItemSize,
 ClassId, Height, Width, Length, Area, BranchId, LotNO, ProductionDate, ExpiryDate, UnitID, ShowQty, QtyBySmalltUnit, showPrice,
 GardQty, Gardresult, Gardresult1, Gardresult2)
VALUES
(@TransactionId, @AutoDetect, @ItemId, @Quantity, @Serial, @PartNoCode, @ItemDetailedCode, @ItemCase, @Price, @ColorId, @ItemSize,
 @ClassId, @Height, @Width, @Length, @Area, @BranchId, @LotNo, @ProductionDate, @ExpiryDate, @UnitId, @ShowQty, @UnitFactor, @ShowPrice,
 @GardQty, @GardResult, @GardResult1, @GardResult2);";
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = id;
                command.Parameters.Add("@AutoDetect", SqlDbType.Int).Value = line.AutoDetect;
                command.Parameters.Add("@ItemId", SqlDbType.Int).Value = line.ItemId.Value;
                command.Parameters.Add("@Quantity", SqlDbType.Float).Value = Convert.ToDouble(quantity);
                AddNullable(command, "@Serial", SqlDbType.NVarChar, line.Serial, 200);
                AddNullable(command, "@PartNoCode", SqlDbType.NVarChar, line.PartNoCode, 200);
                AddNullable(command, "@ItemDetailedCode", SqlDbType.NVarChar, line.ItemDetailedCode, 200);
                command.Parameters.Add("@ItemCase", SqlDbType.Int).Value = line.ItemCase;
                command.Parameters.Add("@Price", SqlDbType.Float).Value = Convert.ToDouble(price);
                command.Parameters.Add("@ColorId", SqlDbType.Int).Value = line.ColorId == 0 ? 1 : line.ColorId;
                AddNullable(command, "@ItemSize", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(line.ItemSize) ? "1" : line.ItemSize, 200);
                command.Parameters.Add("@ClassId", SqlDbType.Int).Value = line.ClassId == 0 ? 1 : line.ClassId;
                AddNullable(command, "@Height", SqlDbType.Float, line.Height);
                AddNullable(command, "@Width", SqlDbType.Float, line.Width);
                AddNullable(command, "@Length", SqlDbType.Float, line.Length);
                AddNullable(command, "@Area", SqlDbType.Float, line.Area);
                AddNullable(command, "@BranchId", SqlDbType.Int, branchId);
                AddNullable(command, "@LotNo", SqlDbType.NVarChar, line.LotNo, 200);
                AddNullable(command, "@ProductionDate", SqlDbType.DateTime, line.ProductionDate);
                AddNullable(command, "@ExpiryDate", SqlDbType.DateTime, line.ExpiryDate);
                AddNullable(command, "@UnitId", SqlDbType.Int, line.UnitId);
                command.Parameters.Add("@ShowQty", SqlDbType.Float).Value = Convert.ToDouble(showQty);
                command.Parameters.Add("@UnitFactor", SqlDbType.Float).Value = Convert.ToDouble(unitFactor);
                command.Parameters.Add("@ShowPrice", SqlDbType.Float).Value = Convert.ToDouble(showPrice);
                AddNullable(command, "@GardQty", SqlDbType.Float, line.GardQty);
                AddNullable(command, "@GardResult", SqlDbType.Float, line.GardResult);
                AddNullable(command, "@GardResult1", SqlDbType.Float, line.GardResult1);
                AddNullable(command, "@GardResult2", SqlDbType.Float, line.GardResult2);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteDetails(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM dbo.Transaction_Details WHERE Transaction_ID = @Id";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private static decimal GetUnitFactor(SqlConnection connection, SqlTransaction transaction, int itemId, int? unitId)
        {
            if (!unitId.HasValue)
            {
                return 1;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT TOP 1 ISNULL(UnitFactor, 1) FROM dbo.TblItemsUnits WHERE ItemID = @ItemId AND UnitID = @UnitId";
                command.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                command.Parameters.Add("@UnitId", SqlDbType.Int).Value = unitId.Value;
                var raw = command.ExecuteScalar();
                return raw == null || raw == DBNull.Value ? 1 : Convert.ToDecimal(raw);
            }
        }

        private static void AddHeaderParameters(SqlCommand command, StocktakingSaveRequest request, MainErpUserContext user, int id, string serial)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Serial", SqlDbType.NVarChar, 100).Value = serial;
            command.Parameters.Add("@Date", SqlDbType.DateTime).Value = request.Date.HasValue ? (object)request.Date.Value : DateTime.Today;
            AddNullable(command, "@FromDate", SqlDbType.DateTime, request.FromDate);
            AddNullable(command, "@ToDate", SqlDbType.DateTime, request.ToDate);
            command.Parameters.Add("@EntryType", SqlDbType.Int).Value = request.GardEntryType;
            command.Parameters.Add("@Type", SqlDbType.Int).Value = StocktakingTransactionType;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
            AddNullable(command, "@StoreId", SqlDbType.Int, request.StoreId);
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            command.Parameters.Add("@StartGard", SqlDbType.Bit).Value = request.StartGard;
            command.Parameters.Add("@StartSettlement", SqlDbType.Bit).Value = request.StartSettlement;
            AddNullable(command, "@Account1", SqlDbType.NVarChar, request.Account1, 200);
            AddNullable(command, "@Account2", SqlDbType.NVarChar, request.Account2, 200);
            command.Parameters.Add("@AutoDetect", SqlDbType.Bit).Value = request.AutoDetect;
        }

        private IList<StocktakingLookupItem> LoadLookup(string sql, string idColumn, string textColumn)
        {
            var rows = new List<StocktakingLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new StocktakingLookupItem
                        {
                            Id = Convert.ToString(reader[idColumn]),
                            Text = GetString(reader, textColumn)
                        });
                    }
                }
            }

            return rows;
        }

        private static int NextManualId(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string whereClause)
        {
            using (var lockCommand = new SqlCommand("sp_getapplock", connection, transaction))
            {
                lockCommand.CommandType = CommandType.StoredProcedure;
                lockCommand.Parameters.AddWithValue("@Resource", "MainErp:" + tableName + ":" + columnName + ":" + (whereClause ?? "all"));
                lockCommand.Parameters.AddWithValue("@LockMode", "Exclusive");
                lockCommand.Parameters.AddWithValue("@LockOwner", "Transaction");
                lockCommand.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = lockCommand.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                lockCommand.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value) < 0)
                {
                    throw new TimeoutException("تعذر حجز رقم جديد للجرد.");
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT ISNULL(MAX(CASE WHEN ISNUMERIC(" + columnName + ") = 1 THEN CONVERT(int, " + columnName + ") ELSE 0 END), 0) + 1 FROM dbo." + tableName + " WITH (UPDLOCK, HOLDLOCK)" + (string.IsNullOrWhiteSpace(whereClause) ? "" : " WHERE " + whereClause);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = size > 0 ? command.Parameters.Add(name, type, size) : command.Parameters.Add(name, type);
            parameter.Value = value == null || (value is string && string.IsNullOrWhiteSpace((string)value)) ? DBNull.Value : value;
        }

        private static string GetString(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int GetInt(IDataRecord record, string name)
        {
            return Convert.ToInt32(record[name]);
        }

        private static int? GetNullableInt(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static DateTime? GetDate(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static bool GetBool(IDataRecord record, string name)
        {
            var value = record[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static decimal GetDecimal(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        private static decimal? GetNullableDecimal(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (decimal?)null : Convert.ToDecimal(value);
        }
    }
}
