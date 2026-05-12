using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.DefinCompItem;

namespace MyERP.Areas.MainErp.Repositories.DefinCompItem
{
    public class DefinCompItemRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public DefinCompItemRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DefinCompItemIndexViewModel LoadIndex(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int? storeId, int page, int pageSize, MainErpUserContext user)
        {
            var model = new DefinCompItemIndexViewModel
            {
                SearchText = searchText,
                FromDate = fromDate,
                ToDate = toDate,
                BranchId = branchId,
                StoreId = storeId,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : pageSize
            };

            int totalCount;
            model.Results = Search(searchText, fromDate, toDate, branchId, storeId, model.Page, model.PageSize, out totalCount);
            model.TotalCount = totalCount;
            model.Branches = LoadBranches();
            model.Stores = LoadStores(branchId);
            model.Customers = LoadCustomers();
            model.Message = user != null ? user.DefaultsWarning : null;
            return model;
        }

        public IList<DefinCompItemListItemViewModel> Search(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int? storeId, int page, int pageSize, out int totalCount)
        {
            var items = new List<DefinCompItemListItemViewModel>();
            totalCount = 0;
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
WITH Rows AS
(
    SELECT
        ROW_NUMBER() OVER (ORDER BY h.ID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        h.ID,
        h.MaxNo,
        h.MaxName,
        h.RecordDate,
        h.BranchID,
        h.StoreID,
        h.StoreID2,
        h.StoreID3,
        h.ItemNameID,
        h.TotalWithVat,
        h.Net,
        h.Vat2,
        h.TransactionID1,
        h.TransactionID2,
        h.NoteSerial11,
        h.NoteSerial12,
        h.OrderID,
        h.Allocated,
        h.AlloPay,
        h.AlloRecep,
        b.branch_name AS BranchName,
        s.StoreName AS StoreName,
        s2.StoreName AS StoreName2,
        s3.StoreName AS StoreName3,
        i.ItemName AS ItemName,
        i.ItemCode AS ItemCode
    FROM dbo.TblDefComItem h
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = h.BranchID
    LEFT JOIN dbo.TblStore s ON s.StoreID = h.StoreID
    LEFT JOIN dbo.TblStore s2 ON s2.StoreID = h.StoreID2
    LEFT JOIN dbo.TblStore s3 ON s3.StoreID = h.StoreID3
    LEFT JOIN dbo.TblItems i ON i.ItemID = h.ItemNameID
    WHERE (@SearchText IS NULL
           OR h.MaxNo LIKE @SearchLike
           OR h.MaxName LIKE @SearchLike
           OR CONVERT(nvarchar(50), h.OrderID) LIKE @SearchLike
           OR h.NoteSerial11 LIKE @SearchLike
           OR h.NoteSerial12 LIKE @SearchLike
           OR i.ItemName LIKE @SearchLike
           OR i.ItemCode LIKE @SearchLike)
      AND (@FromDate IS NULL OR h.RecordDate >= @FromDate)
      AND (@ToDate IS NULL OR h.RecordDate < DATEADD(day, 1, @ToDate))
      AND (@BranchId IS NULL OR h.BranchID = @BranchId)
      AND (@StoreId IS NULL OR h.StoreID = @StoreId OR h.StoreID2 = @StoreId OR h.StoreID3 = @StoreId)
)
SELECT * FROM Rows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;";
                command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                command.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText.Trim() + "%";
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = (object)storeId ?? DBNull.Value;
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

                        items.Add(new DefinCompItemListItemViewModel
                        {
                            Id = ReadInt(reader, "ID"),
                            MaxNo = ReadString(reader, "MaxNo"),
                            MaxName = ReadString(reader, "MaxName"),
                            RecordDate = ReadDateTime(reader, "RecordDate"),
                            BranchName = ReadString(reader, "BranchName"),
                            StoreName = ReadString(reader, "StoreName"),
                            StoreName2 = ReadString(reader, "StoreName2"),
                            StoreName3 = ReadString(reader, "StoreName3"),
                            ItemName = ReadString(reader, "ItemName"),
                            ItemCode = ReadString(reader, "ItemCode"),
                            TotalWithVat = ReadDecimal(reader, "TotalWithVat"),
                            Net = ReadDecimal(reader, "Net"),
                            Vat2 = ReadDecimal(reader, "Vat2"),
                            TransactionId1 = ReadNullableInt(reader, "TransactionID1"),
                            TransactionId2 = ReadNullableInt(reader, "TransactionID2"),
                            NoteSerial11 = ReadString(reader, "NoteSerial11"),
                            NoteSerial12 = ReadString(reader, "NoteSerial12"),
                            OrderNo = ReadString(reader, "OrderID"),
                            Allocated = ReadNullableBool(reader, "Allocated"),
                            AlloPay = ReadNullableBool(reader, "AlloPay"),
                            AlloRecep = ReadNullableBool(reader, "AlloRecep")
                        });
                    }
                }
            }

            return items;
        }

        public DefinCompItemDetailsViewModel GetDetails(int id)
        {
            var model = new DefinCompItemDetailsViewModel();

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadHeader(connection, model, id);
                if (model.Id == 0)
                {
                    model.Warnings.Add("ط§ظ„ط³ظ†ط¯ ط؛ظٹط± ظ…ظˆط¬ظˆط¯.");
                    return model;
                }

                LoadComponents(connection, model, id);
                LoadOutputs(connection, model, id);
                BuildOutputGroups(model);
                LoadTransactions(connection, model, id);
                LoadTotals(model);
            }

            return model;
        }

        public IList<DefinCompItemLookupItemViewModel> SearchItems(string term, int maxRows)
        {
            var items = new List<DefinCompItemLookupItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (@Top)
    i.ItemID,
    i.ItemCode,
    i.ItemName,
    i.Fullcode
FROM dbo.TblItems i
WHERE ISNULL(i.IsArchive, 0) = 0
  AND (@Term IS NULL OR i.ItemName LIKE @LikeTerm OR i.ItemCode LIKE @LikeTerm OR i.Fullcode LIKE @LikeTerm)
ORDER BY i.ItemName;";
                command.Parameters.Add("@Top", SqlDbType.Int).Value = maxRows < 1 ? 10 : maxRows;
                command.Parameters.Add("@Term", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(term) ? (object)DBNull.Value : term.Trim();
                command.Parameters.Add("@LikeTerm", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(term) ? (object)DBNull.Value : "%" + term.Trim() + "%";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "ItemCode");
                        var name = ReadString(reader, "ItemName");
                        items.Add(new DefinCompItemLookupItemViewModel
                        {
                            Id = ReadInt(reader, "ItemID").ToString(CultureInfo.InvariantCulture),
                            Text = string.IsNullOrWhiteSpace(code) ? name : code + " - " + name,
                            Extra = ReadString(reader, "Fullcode")
                        });
                    }
                }
            }

            return items;
        }

        public IList<DefinCompItemLookupItemViewModel> SearchUnits(int itemId)
        {
            var items = new List<DefinCompItemLookupItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                if (TableExists(connection, "TblItemsUnits"))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
SELECT u.UnitID, u.UnitName, u.UnitNamee, iu.UnitFactor
FROM dbo.TblItemsUnits iu
INNER JOIN dbo.TblUnites u ON u.UnitID = iu.UnitID
WHERE iu.ItemID = @ItemId
ORDER BY u.UnitName;";
                        command.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new DefinCompItemLookupItemViewModel
                                {
                                    Id = ReadInt(reader, "UnitID").ToString(CultureInfo.InvariantCulture),
                                    Text = ReadString(reader, "UnitName"),
                                    Extra = ReadDecimal(reader, "UnitFactor").ToString(CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                }
                else
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"SELECT u.UnitID, u.UnitName, u.UnitNamee FROM dbo.TblUnites u ORDER BY u.UnitName;";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new DefinCompItemLookupItemViewModel
                                {
                                    Id = ReadInt(reader, "UnitID").ToString(CultureInfo.InvariantCulture),
                                    Text = ReadString(reader, "UnitName"),
                                    Extra = ReadString(reader, "UnitNamee")
                                });
                            }
                        }
                    }
                }
            }

            return items;
        }

        public IList<DefinCompItemLookupItemViewModel> LoadBranches()
        {
            var items = new List<DefinCompItemLookupItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT branch_id, branch_name, branch_namee FROM dbo.TblBranchesData ORDER BY branch_name;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new DefinCompItemLookupItemViewModel
                        {
                            Id = ReadInt(reader, "branch_id").ToString(CultureInfo.InvariantCulture),
                            Text = ReadString(reader, "branch_name"),
                            Extra = ReadString(reader, "branch_namee")
                        });
                    }
                }
            }

            return items;
        }

        public IList<DefinCompItemLookupItemViewModel> LoadStores(int? branchId)
        {
            var items = new List<DefinCompItemLookupItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT StoreID, StoreName, StoreNamee
FROM dbo.TblStore
WHERE (@BranchId IS NULL OR BranchId = @BranchId)
ORDER BY StoreName;";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new DefinCompItemLookupItemViewModel
                        {
                            Id = ReadInt(reader, "StoreID").ToString(CultureInfo.InvariantCulture),
                            Text = ReadString(reader, "StoreName"),
                            Extra = ReadString(reader, "StoreNamee")
                        });
                    }
                }
            }

            return items;
        }

        public IList<DefinCompItemLookupItemViewModel> LoadCustomers()
        {
            var items = new List<DefinCompItemLookupItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT TOP 500 CusID, CusName, CusNamee FROM dbo.TblCustemers ORDER BY CusName;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new DefinCompItemLookupItemViewModel
                        {
                            Id = ReadInt(reader, "CusID").ToString(CultureInfo.InvariantCulture),
                            Text = ReadString(reader, "CusName"),
                            Extra = ReadString(reader, "CusNamee")
                        });
                    }
                }
            }

            return items;
        }

        public DefinCompItemSaveResult Save(DefinCompItemSaveRequest request, MainErpUserContext user)
        {
            var result = new DefinCompItemSaveResult();
            NormalizeRequestForSave(request);
            var validation = ValidateRequest(request);
            if (validation != null)
            {
                result.Message = validation;
                return result;
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (request.Id.HasValue && request.Id.Value > 0
                        && !request.ForceRebuild
                        && HasPostedTransactions(connection, transaction, request.Id.Value))
                    {
                        result.Message = "ظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ ظ‡ط°ط§ ط§ظ„ط³ظ†ط¯ ظ„ط£ظ† ظ‡ظ†ط§ظƒ ط­ط±ظƒط§طھ ظ…ط±طھط¨ط·ط© ط¨ظ‡ طھظ… ط§ط¹طھظ…ط§ط¯ظ‡ط§ ط£ظˆ طھط±ط­ظٹظ„ظ‡ط§.";
                        transaction.Rollback();
                        return result;
                    }

                    var header = request.Id.HasValue && request.Id.Value > 0
                        ? UpdateHeader(connection, transaction, request, user)
                        : InsertHeader(connection, transaction, request, user);

                    DeleteChildRows(connection, transaction, header.Id);
                    DeleteLinkedTransactions(connection, transaction, header.Id);

                    var componentTotals = CalculateTotals(request.Components);
                    var outputTotals = CalculateTotals(request.Outputs);
                    var accounts = LoadPostingAccounts(connection, transaction, request.BranchId, request.StoreId);

                    if (string.IsNullOrWhiteSpace(accounts.BranchAccount) || string.IsNullOrWhiteSpace(accounts.StoreAccount))
                    {
                        result.Message = "ظ„ط§ ظٹظ…ظƒظ† ط§ظ„ط­ظپط¸ ظ„ط£ظ† ط­ط³ط§ط¨ ط§ظ„ظپط±ط¹ ط£ظˆ ط­ط³ط§ط¨ ط§ظ„ظ…ط®ط²ظ† ط؛ظٹط± ظ…ط¹ط±ظ‘ظپ.";
                        transaction.Rollback();
                        return result;
                    }

                    InsertComponentRows(connection, transaction, header.Id, request.Components);
                    InsertOutputRows(connection, transaction, header.Id, request.Outputs);

                    var issueSerial = BuildSerial(header.MaxNo, header.Id, 27);
                    var receiptSerial = BuildSerial(header.MaxNo, header.Id, 28);
                    var issueDesc = string.IsNullOrWhiteSpace(request.TransactionComment) ? "ط³ظ†ط¯ طµط±ظپ ط¨ظ†ط§ط، ط¹ظ„ظ‰ طھط¬ظ…ظٹط¹ ط±ظ‚ظ… " + header.MaxNo : request.TransactionComment;
                    var receiptDesc = "ط³ظ†ط¯ ط§ط³طھظ„ط§ظ… ط¨ظ†ط§ط، ط¹ظ„ظ‰ طھط¬ظ…ظٹط¹ ط±ظ‚ظ… " + header.MaxNo;

                    var issueTransactionId = InsertInventoryTransaction(connection, transaction, header, 27, issueSerial, componentTotals.Cost, user, issueDesc);
                    var receiptTransactionId = InsertInventoryTransaction(connection, transaction, header, 28, receiptSerial, outputTotals.Cost, user, receiptDesc);

                    InsertTransactionDetails(connection, transaction, issueTransactionId, request.Components, true);
                    InsertTransactionDetails(connection, transaction, receiptTransactionId, request.Outputs, false);

                    InsertAccountingNoteWithFallback(connection, transaction, header, 240, issueTransactionId, issueSerial, componentTotals.Cost, accounts.CostAccount, accounts.StoreAccount, issueDesc, user, header.MaxNo);
                    InsertAccountingNoteWithFallback(connection, transaction, header, 250, receiptTransactionId, receiptSerial, outputTotals.Cost, accounts.StoreAccount, accounts.CostAccount, receiptDesc, user, header.MaxNo);

                    UpdateHeaderLinks(connection, transaction, header.Id, issueTransactionId, receiptTransactionId, issueSerial, receiptSerial, componentTotals, outputTotals);
                    UpdateOutputLinks(connection, transaction, header.Id, receiptTransactionId, receiptSerial);

                    transaction.Commit();

                    result.Success = true;
                    result.Id = header.Id;
                    result.IssueTransactionId = issueTransactionId;
                    result.ReceiptTransactionId = receiptTransactionId;
                    result.IssueSerial = issueSerial;
                    result.ReceiptSerial = receiptSerial;
                    result.ComponentQtyTotal = componentTotals.Qty;
                    result.ComponentCostTotal = componentTotals.Cost;
                    result.OutputQtyTotal = outputTotals.Qty;
                    result.OutputCostTotal = outputTotals.Cost;
                    result.Difference = outputTotals.Cost - componentTotals.Cost;
                    result.Message = "طھظ… ط­ظپط¸ ط³ظ†ط¯ ط§ظ„طھط¬ظ…ظٹط¹ ط¨ظ†ط¬ط§ط­.";
                    return result;
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }

                    Trace.TraceError("DefinCompItem save failed: " + ex);
                    result.Message = "ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط§ظ„ط­ظپط¸: " + NormalizeErrorMessage(ex);
                    return result;
                }
            }
        }

        public DefinCompItemSaveResult Delete(int id, MainErpUserContext user)
        {
            var result = new DefinCompItemSaveResult();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var exists = GetHeaderSnapshot(connection, transaction, id);
                    if (exists == null)
                    {
                        result.Message = "ط§ظ„ط³ظ†ط¯ ط§ظ„ظ…ط·ظ„ظˆط¨ ط؛ظٹط± ظ…ظˆط¬ظˆط¯.";
                        transaction.Rollback();
                        return result;
                    }

                    if (HasPostedTransactions(connection, transaction, id))
                    {
                        result.Message = "ظ„ط§ ظٹظ…ظƒظ† ط§ظ„ط­ط°ظپ ظ„ط£ظ† ظ‡ظ†ط§ظƒ ط­ط±ظƒط§طھ ظ…ط§ظ„ظٹط© ط£ظˆ ظ…ط®ط²ظ†ظٹط© طھظ… ط§ط¹طھظ…ط§ط¯ظ‡ط§.";
                        transaction.Rollback();
                        return result;
                    }

                    DeleteLinkedTransactions(connection, transaction, id);
                    DeleteChildRows(connection, transaction, id);

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "DELETE FROM dbo.TblDefComItem WHERE ID = @Id;";
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    result.Success = true;
                    result.Id = id;
                    result.Message = "طھظ… ط¥ظ„ط؛ط§ط، ط³ظ†ط¯ ط§ظ„طھط¬ظ…ظٹط¹ ط¨ظ†ط¬ط§ط­.";
                    return result;
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }

                    result.Message = "طھط¹ط°ط± ط­ط°ظپ ط§ظ„ط³ظ†ط¯: " + NormalizeErrorMessage(ex);
                    return result;
                }
            }
        }

        private static void NormalizeRequestForSave(DefinCompItemSaveRequest request)
        {
            if (request == null)
            {
                return;
            }

            request.Components = request.Components ?? new List<DefinCompItemLineRequest>();
            request.Outputs = request.Outputs ?? new List<DefinCompItemLineRequest>();
            request.OutputGroups = request.OutputGroups ?? new List<DefinCompItemOutputGroupRequest>();

            var groups = BuildSaveGroups(request);

            request.Outputs = new List<DefinCompItemLineRequest>();
            request.Components = new List<DefinCompItemLineRequest>();

            foreach (var group in groups)
            {
                request.Outputs.Add(group.Output);
                if (group.Components != null)
                {
                    foreach (var component in group.Components)
                    {
                        request.Components.Add(component);
                    }
                }
            }

            if (!request.ItemNameId.HasValue && request.Outputs.Count > 0)
            {
                request.ItemNameId = request.Outputs[0].ItemId;
            }

            ApplyOutputCostFromComponents(request);
        }

        private static IList<DefinCompItemSaveGroup> BuildSaveGroups(DefinCompItemSaveRequest request)
        {
            var groups = new List<DefinCompItemSaveGroup>();
            if (request == null)
            {
                return groups;
            }

            var usedLineIds = new HashSet<int>();
            var nextLineId = 1;
            if (request.OutputGroups.Count > 0)
            {
                for (var i = 0; i < request.OutputGroups.Count; i++)
                {
                    var group = request.OutputGroups[i];
                    if (group == null)
                    {
                        continue;
                    }

                    var lineId = ResolveLineId(group.LineId, usedLineIds, ref nextLineId);

                    var output = CloneLineRequest(group);
                    output.LineId = lineId;

                    var components = group.Components == null ? new List<DefinCompItemLineRequest>() : group.Components.Select(CloneLineRequest).ToList();
                    for (var j = 0; j < components.Count; j++)
                    {
                        components[j].LineId = lineId;
                        if (!components[j].ItemId2.HasValue)
                        {
                            components[j].ItemId2 = output.ItemId;
                        }
                    }

                    groups.Add(new DefinCompItemSaveGroup { Output = output, Components = components });
                }
            }
            else
            {
                var outputLines = request.Outputs
                    .Where(line => line != null)
                    .Select(CloneLineRequest)
                    .ToList();

                for (var i = 0; i < outputLines.Count; i++)
                {
                    outputLines[i].LineId = ResolveLineId(outputLines[i].LineId, usedLineIds, ref nextLineId);

                    groups.Add(new DefinCompItemSaveGroup
                    {
                        Output = outputLines[i],
                        Components = new List<DefinCompItemLineRequest>()
                    });
                }
            }

            var requestComponents = request.Components
                .Where(c => c != null)
                .Select(CloneLineRequest)
                .ToList();

            var groupedComponents = requestComponents
                .Where(c => c.LineId.HasValue && c.LineId.Value > 0)
                .GroupBy(c => c.LineId.Value)
                .ToDictionary(g => g.Key, g => (IList<DefinCompItemLineRequest>)g.ToList());

            var unassignedComponents = requestComponents
                .Where(c => !c.LineId.HasValue || c.LineId.Value <= 0)
                .ToList();

            for (var i = 0; i < groups.Count; i++)
            {
                var lineId = groups[i].Output.LineId.Value;
                if (groupedComponents.TryGetValue(lineId, out var componentsByLine))
                {
                    for (var j = 0; j < componentsByLine.Count; j++)
                    {
                        componentsByLine[j].LineId = lineId;
                        if (!componentsByLine[j].ItemId2.HasValue)
                        {
                            componentsByLine[j].ItemId2 = groups[i].Output.ItemId;
                        }

                        groups[i].Components.Add(componentsByLine[j]);
                    }
                }

                if (string.IsNullOrWhiteSpace(groups[i].Output.Remark))
                {
                    continue;
                }
            }

            foreach (var component in unassignedComponents)
            {
                var matched = groups.FirstOrDefault(g =>
                    g.Output.ItemId.HasValue && g.Output.ItemId == component.ItemId2);
                if (matched != null)
                {
                    component.LineId = matched.Output.LineId;
                    component.ItemId2 = component.ItemId2 ?? matched.Output.ItemId;
                    matched.Components.Add(component);
                    continue;
                }

                if (groups.Count == 1)
                {
                    var single = groups[0];
                    component.LineId = single.Output.LineId;
                    if (!component.ItemId2.HasValue)
                    {
                        component.ItemId2 = single.Output.ItemId;
                    }

                    if (!single.Components.Contains(component))
                    {
                        single.Components.Add(component);
                    }
                }
            }

            return groups;
        }

        private static int ResolveLineId(int? candidateLineId, HashSet<int> usedLineIds, ref int nextLineId)
        {
            var lineId = candidateLineId.GetValueOrDefault(0);
            if (lineId <= 0)
            {
                lineId = nextLineId;
            }

            while (usedLineIds.Contains(lineId))
            {
                lineId++;
            }

            usedLineIds.Add(lineId);
            if (lineId >= nextLineId)
            {
                nextLineId = lineId + 1;
            }

            return lineId;
        }

        private sealed class DefinCompItemSaveGroup
        {
            public DefinCompItemLineRequest Output { get; set; }
            public IList<DefinCompItemLineRequest> Components { get; set; }
        }

        private static DefinCompItemLineRequest CloneLineRequest(DefinCompItemLineRequest source)
        {
            if (source == null)
            {
                return new DefinCompItemLineRequest();
            }

            return new DefinCompItemLineRequest
            {
                Id = source.Id,
                ItemId2 = source.ItemId2,
                ItemId = source.ItemId,
                UnitId = source.UnitId,
                Qty = source.Qty,
                Cost = source.Cost,
                Price = source.Price,
                Total = source.Total,
                IsDeleted = source.IsDeleted,
                IsAdd = source.IsAdd,
                LineId = source.LineId,
                SpecId1 = source.SpecId1,
                SpecId2 = source.SpecId2,
                SpecId3 = source.SpecId3,
                SpecId4 = source.SpecId4,
                Width = source.Width,
                Height = source.Height,
                Length = source.Length,
                Thickness = source.Thickness,
                Diameter = source.Diameter,
                Remark = source.Remark
            };
        }

        private static string ValidateRequest(DefinCompItemSaveRequest request)
        {
            if (request == null)
            {
                return "لا يمكن إتمام الحفظ: بيانات فارغة.";
            }

            if (!request.RecordDate.HasValue)
            {
                return "الرجاء تحديد تاريخ السند.";
            }

            if (!request.BranchId.HasValue || request.BranchId.Value <= 0)
            {
                return "الرجاء تحديد الفرع.";
            }

            if (!request.StoreId.HasValue || request.StoreId.Value <= 0)
            {
                return "الرجاء تحديد المخزن.";
            }

            if (request.Components == null || request.Components.Count == 0)
            {
                return "يجب إضافة مكونات على الأقل.";
            }

            if (request.Outputs == null || request.Outputs.Count == 0)
            {
                return "يجب إضافة منتج نهائي واحد على الأقل.";
            }

            var outputsByLine = request.Outputs
                .Where(line => line != null && line.LineId.HasValue && line.LineId.Value > 0)
                .GroupBy(line => line.LineId.Value)
                .ToDictionary(g => g.Key, g => (IList<DefinCompItemLineRequest>)g.ToList());

            var componentsByLine = request.Components
                .Where(line => line != null && line.LineId.HasValue && line.LineId.Value > 0)
                .GroupBy(line => line.LineId.Value)
                .ToDictionary(g => g.Key, g => (IList<DefinCompItemLineRequest>)g.ToList());

            foreach (var line in request.Components)
            {
                if (line == null || !line.ItemId.HasValue || line.ItemId.Value <= 0)
                {
                    return "بند مكوّن غير صحيح: صنف غير محدد.";
                }

                if (!line.UnitId.HasValue || line.UnitId.Value <= 0)
                {
                    return "بند مكوّن غير صحيح: وحدة قياس غير محددة.";
                }

                if (!line.Qty.HasValue || line.Qty.Value <= 0)
                {
                    return "كمية المكوّن يجب أن تكون أكبر من صفر.";
                }

                if (line.Cost.HasValue && line.Cost.Value < 0)
                {
                    return "تكلفة المكوّن لا يجوز أن تكون سالبة.";
                }

                if (!line.LineId.HasValue || line.LineId.Value <= 0)
                {
                    return "هناك مكون غير مرتبط بمنتج نهائي.";
                }

                if (!outputsByLine.ContainsKey(line.LineId.Value))
                {
                    return "هناك مكون مربوط بسطر منتج غير موجود.";
                }
            }

            foreach (var line in request.Outputs)
            {
                if (line == null || !line.ItemId.HasValue || line.ItemId.Value <= 0)
                {
                    return "بند منتج نهائي غير صحيح: صنف غير محدد.";
                }

                if (!line.UnitId.HasValue || line.UnitId.Value <= 0)
                {
                    return "بند منتج نهائي غير صحيح: وحدة قياس غير محددة.";
                }

                if (!line.Qty.HasValue || line.Qty.Value <= 0)
                {
                    return "كمية المنتج النهائي يجب أن تكون أكبر من صفر.";
                }

                if (line.Cost.HasValue && line.Cost.Value < 0)
                {
                    return "تكلفة المنتج النهائي لا يجوز أن تكون سالبة.";
                }

                if (!line.LineId.HasValue || line.LineId.Value <= 0)
                {
                    return "هناك منتج نهائي غير مرتبط بسطر صالحة.";
                }

                if (!componentsByLine.ContainsKey(line.LineId.Value) || componentsByLine[line.LineId.Value].Count == 0)
                {
                    return "كل منتج نهائي يجب أن يحتوي على مكونات مرتبطة به.";
                }
            }

            foreach (var output in outputsByLine.Values.SelectMany(g => g))
            {
                if (!componentsByLine.ContainsKey(output.LineId.Value))
                {
                    return "يوجد منتج نهائي بدون مكونات مرتبطة.";
                }
            }

            return null;
        }
        private static DefinCompItemDetailsViewModel LoadHeader(SqlConnection connection, DefinCompItemDetailsViewModel model, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 1
    h.*,
    b.branch_name,
    s.StoreName,
    s2.StoreName AS StoreName2,
    s3.StoreName AS StoreName3,
    c.CusName,
    i.ItemName,
    i.ItemCode
FROM dbo.TblDefComItem h
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = h.BranchID
LEFT JOIN dbo.TblStore s ON s.StoreID = h.StoreID
LEFT JOIN dbo.TblStore s2 ON s2.StoreID = h.StoreID2
LEFT JOIN dbo.TblStore s3 ON s3.StoreID = h.StoreID3
LEFT JOIN dbo.TblCustemers c ON c.CusID = h.CusID
LEFT JOIN dbo.TblItems i ON i.ItemID = h.ItemNameID
WHERE h.ID = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return model;
                    }

                    model.Id = ReadInt(reader, "ID");
                    model.RecordDate = ReadDateTime(reader, "RecordDate");
                    model.BranchId = ReadNullableInt(reader, "BranchID");
                    model.BranchName = ReadString(reader, "branch_name");
                    model.StoreId = ReadNullableInt(reader, "StoreID");
                    model.StoreName = ReadString(reader, "StoreName");
                    model.StoreId2 = ReadNullableInt(reader, "StoreID2");
                    model.StoreName2 = ReadString(reader, "StoreName2");
                    model.StoreId3 = ReadNullableInt(reader, "StoreID3");
                    model.StoreName3 = ReadString(reader, "StoreName3");
                    model.CusId = ReadNullableInt(reader, "CusID");
                    model.CustomerName = ReadString(reader, "CusName");
                    model.ItemNameId = ReadNullableInt(reader, "ItemNameID");
                    model.ItemName = ReadString(reader, "ItemName");
                    model.ItemCode = ReadString(reader, "ItemCode");
                    model.MaxNo = ReadString(reader, "MaxNo");
                    model.MaxName = ReadString(reader, "MaxName");
                    model.OrderNo = ReadString(reader, "order_no");
                    model.GroupId = ReadNullableInt(reader, "GroupID");
                    model.PaymentType = ReadNullableInt(reader, "PaymentType");
                    model.EmpId = ReadNullableInt(reader, "Emp_id");
                    model.BoxId = ReadNullableInt(reader, "BoxID");
                    model.Period = ReadNullableDouble(reader, "Period");
                    model.Qty1 = ReadDecimal(reader, "Qty1");
                    model.Price = ReadDecimal(reader, "Price");
                    model.TotalAdd = ReadDecimal(reader, "TotalAdd");
                    model.TotalDisc = ReadDecimal(reader, "TotalDisc");
                    model.Net = ReadDecimal(reader, "Net");
                    model.TotalWithVat = ReadDecimal(reader, "TotalWithVat");
                    model.Vat2 = ReadDecimal(reader, "Vat2");
                    model.TransactionId1 = ReadNullableInt(reader, "TransactionID1");
                    model.TransactionId2 = ReadNullableInt(reader, "TransactionID2");
                    model.TransactionId3 = ReadNullableInt(reader, "TransactionID3");
                    model.TransactionId4 = ReadNullableInt(reader, "TransactionID4");
                    model.TransactionId5 = ReadNullableInt(reader, "TransactionID5");
                    model.TransactionId6 = ReadNullableInt(reader, "TransactionID6");
                    model.NoteSerial11 = ReadString(reader, "NoteSerial11");
                    model.NoteSerial12 = ReadString(reader, "NoteSerial12");
                    model.NoteSerial13 = ReadString(reader, "NoteSerial13");
                    model.NoteSerial14 = ReadString(reader, "NoteSerial14");
                    model.NoteSerial15 = ReadString(reader, "NoteSerial15");
                    model.NoteSerial16 = ReadString(reader, "NoteSerial16");
                }
            }

            return model;
        }

        private static DefinCompItemHeaderSnapshot GetHeaderSnapshot(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT TOP 1 ID, MaxNo, RecordDate, BranchID, StoreID, StoreID2, StoreID3, ItemNameID, CusID, ItemCode,
       TransactionID1, TransactionID2, TransactionID3, TransactionID4, TransactionID5, TransactionID6,
       NoteSerial11, NoteSerial12, NoteSerial13, NoteSerial14, NoteSerial15, NoteSerial16
FROM dbo.TblDefComItem
WHERE ID = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new DefinCompItemHeaderSnapshot
                    {
                        Id = ReadInt(reader, "ID"),
                        MaxNo = ReadString(reader, "MaxNo"),
                        RecordDate = ReadDateTime(reader, "RecordDate"),
                        BranchId = ReadNullableInt(reader, "BranchID"),
                        StoreId = ReadNullableInt(reader, "StoreID"),
                        StoreId2 = ReadNullableInt(reader, "StoreID2"),
                        StoreId3 = ReadNullableInt(reader, "StoreID3"),
                        ItemNameId = ReadNullableInt(reader, "ItemNameID"),
                        CusId = ReadNullableInt(reader, "CusID"),
                        ItemCode = ReadString(reader, "ItemCode"),
                        TransactionId1 = ReadNullableInt(reader, "TransactionID1"),
                        TransactionId2 = ReadNullableInt(reader, "TransactionID2"),
                        TransactionId3 = ReadNullableInt(reader, "TransactionID3"),
                        TransactionId4 = ReadNullableInt(reader, "TransactionID4"),
                        TransactionId5 = ReadNullableInt(reader, "TransactionID5"),
                        TransactionId6 = ReadNullableInt(reader, "TransactionID6"),
                        NoteSerial11 = ReadString(reader, "NoteSerial11"),
                        NoteSerial12 = ReadString(reader, "NoteSerial12"),
                        NoteSerial13 = ReadString(reader, "NoteSerial13"),
                        NoteSerial14 = ReadString(reader, "NoteSerial14"),
                        NoteSerial15 = ReadString(reader, "NoteSerial15"),
                        NoteSerial16 = ReadString(reader, "NoteSerial16")
                    };
                }
            }
        }

        private static void LoadComponents(SqlConnection connection, DefinCompItemDetailsViewModel model, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    det.ID,
    det.IDDefCIT,
    det.ItemID,
    det.UnitID,
    det.SpecID1,
    det.SpecID2,
    det.SpecID3,
    det.SpecID4,
    det.Amout1,
    det.Amout2,
    det.Amout3,
    det.Amout4,
    det.Qty,
    det.cost,
    det.FlgX,
    det.TepQty,
    det.IsAdd,
    det.Price,
    det.Total,
    det.IsDeleted,
    det.ItemID2,
    det.ItemCode2,
    det.LineID,
    det.OldPrice,
    det.lowering,
    det.Increase,
    det.TableID,
    det.QtyOut,
    det.IsRow,
    det.widtj,
    det.hight,
    det.Length,
    det.thickness,
    det.DO,
    det.DI,
    det.Diameter,
    i.ItemCode,
    i.ItemName,
    i.Fullcode,
    i.ItemNamee,
    i2.ItemCode AS ItemCode2,
    i2.ItemName AS ItemName2,
    u.UnitName
FROM dbo.TblDefComItemDet det
LEFT JOIN dbo.TblItems i ON i.ItemID = det.ItemID
LEFT JOIN dbo.TblItems i2 ON i2.ItemID = det.ItemID2
LEFT JOIN dbo.TblUnites u ON u.UnitID = det.UnitID
WHERE det.IDDefCIT = @Id
ORDER BY det.ItemID2, det.LineID, det.ID;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Components.Add(new DefinCompItemComponentLineViewModel
                        {
                            Id = ReadInt(reader, "ID"),
                            ItemId = ReadNullableInt(reader, "ItemID"),
                            ItemCode = ReadString(reader, "ItemCode"),
                            ItemName = ReadString(reader, "ItemName"),
                            ItemId2 = ReadNullableInt(reader, "ItemID2"),
                            ItemCode2 = ReadString(reader, "ItemCode2"),
                            ItemName2 = ReadString(reader, "ItemName2"),
                            UnitId = ReadNullableInt(reader, "UnitID"),
                            UnitName = ReadString(reader, "UnitName"),
                            Qty = ReadNullableDouble(reader, "Qty"),
                            Cost = ReadNullableDouble(reader, "cost"),
                            Price = ReadNullableDouble(reader, "Price"),
                            Total = ReadNullableDouble(reader, "Total"),
                            IsDeleted = ReadNullableBool(reader, "IsDeleted").GetValueOrDefault(false),
                            IsAdd = ReadNullableBool(reader, "IsAdd"),
                            LineId = ReadNullableInt(reader, "LineID"),
                            SpecId1 = ReadNullableInt(reader, "SpecID1"),
                            SpecId2 = ReadNullableInt(reader, "SpecID2"),
                            SpecId3 = ReadNullableInt(reader, "SpecID3"),
                            SpecId4 = ReadNullableInt(reader, "SpecID4"),
                            Width = ReadNullableDouble(reader, "widtj"),
                            Height = ReadNullableDouble(reader, "hight"),
                            Length = ReadNullableDouble(reader, "Length"),
                            Thickness = ReadNullableDouble(reader, "thickness"),
                            Diameter = ReadNullableDouble(reader, "Diameter"),
                            Remark = string.Empty
                        });
                    }
                }
            }
        }

        private static void LoadOutputs(SqlConnection connection, DefinCompItemDetailsViewModel model, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    d.ID,
    d.IDDefCIT,
    d.GroupID,
    d.UnitID,
    d.cost,
    d.Qty,
    d.Price,
    d.Total,
    d.ItemID,
    d.ItemID2,
    d.ItemCode2,
    d.LineID,
    d.TransactionID4,
    d.NoteSerial14,
    d.PercentCost,
    d.Remark,
    i.ItemCode,
    i.ItemName,
    u.UnitName
FROM dbo.TblDefComItemData d
LEFT JOIN dbo.TblItems i ON i.ItemID = d.ItemID
LEFT JOIN dbo.TblUnites u ON u.UnitID = d.UnitID
WHERE d.IDDefCIT = @Id
ORDER BY d.LineID, d.ID;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Outputs.Add(new DefinCompItemOutputLineViewModel
                        {
                            Id = ReadInt(reader, "ID"),
                            ItemId = ReadNullableInt(reader, "ItemID"),
                            ItemCode = ReadString(reader, "ItemCode"),
                            ItemName = ReadString(reader, "ItemName"),
                            UnitId = ReadNullableInt(reader, "UnitID"),
                            UnitName = ReadString(reader, "UnitName"),
                            Qty = ReadNullableDouble(reader, "Qty"),
                            Cost = ReadNullableDouble(reader, "cost"),
                            Price = ReadNullableDouble(reader, "Price"),
                            Total = ReadNullableDouble(reader, "Total"),
                            TransactionId4 = ReadNullableInt(reader, "TransactionID4"),
                            NoteSerial14 = ReadString(reader, "NoteSerial14"),
                            LineId = ReadNullableInt(reader, "LineID"),
                            QtyBySmallUnit = null,
                            Remark = ReadString(reader, "Remark")
                        });
                    }
                }
            }
        }

        private static void BuildOutputGroups(DefinCompItemDetailsViewModel model)
        {
            if (model == null)
            {
                return;
            }

            model.OutputGroups = new List<DefinCompItemOutputGroupViewModel>();
            var componentsByLine = (model.Components ?? new List<DefinCompItemComponentLineViewModel>())
                .GroupBy(c => c.LineId.GetValueOrDefault())
                .ToDictionary(g => g.Key, g => (IList<DefinCompItemComponentLineViewModel>)g.ToList());

            foreach (var output in model.Outputs ?? new List<DefinCompItemOutputLineViewModel>())
            {
                var lineId = output.LineId.GetValueOrDefault();
                IList<DefinCompItemComponentLineViewModel> components;
                if (!componentsByLine.TryGetValue(lineId, out components))
                {
                    components = new List<DefinCompItemComponentLineViewModel>();
                }

                model.OutputGroups.Add(new DefinCompItemOutputGroupViewModel
                {
                    Id = output.Id,
                    ItemId = output.ItemId,
                    ItemCode = output.ItemCode,
                    ItemName = output.ItemName,
                    UnitId = output.UnitId,
                    UnitName = output.UnitName,
                    Qty = output.Qty,
                    Cost = output.Cost,
                    Price = output.Price,
                    Total = output.Total,
                    TransactionId4 = output.TransactionId4,
                    NoteSerial14 = output.NoteSerial14,
                    LineId = output.LineId,
                    QtyBySmallUnit = output.QtyBySmallUnit,
                    Remark = output.Remark,
                    Components = components
                });
            }
        }


        private static void ApplyOutputCostFromComponents(DefinCompItemSaveRequest request)
        {
            if (request == null || request.Outputs == null)
            {
                return;
            }

            request.Outputs = request.Outputs ?? new List<DefinCompItemLineRequest>();
            request.Components = request.Components ?? new List<DefinCompItemLineRequest>();

            var totalsByLine = request.Components
                .Where(component => component != null && component.LineId.HasValue && component.LineId.Value > 0)
                .GroupBy(component => component.LineId.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(component =>
                    {
                        var qty = Convert.ToDecimal(component.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                        var cost = Convert.ToDecimal(component.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                        return qty * cost;
                    }));

            foreach (var output in request.Outputs)
            {
                if (output == null || !output.LineId.HasValue || output.LineId.Value <= 0)
                {
                    continue;
                }

                var componentCost = totalsByLine.TryGetValue(output.LineId.Value, out var lineTotal)
                    ? lineTotal
                    : 0m;

                var outputQty = Convert.ToDecimal(output.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                output.Cost = outputQty > 0
                    ? Convert.ToDouble(componentCost / outputQty, CultureInfo.InvariantCulture)
                    : 0;

                output.Price = output.Cost;
                output.Total = Convert.ToDouble(componentCost, CultureInfo.InvariantCulture);
                output.Cost = Math.Max(0, Convert.ToDouble(output.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture));
                output.Price = Math.Max(0, Convert.ToDouble(output.Price.GetValueOrDefault(0d), CultureInfo.InvariantCulture));
                output.Total = Math.Max(0, Convert.ToDouble(output.Total.GetValueOrDefault(0d), CultureInfo.InvariantCulture));
            }
        }

        private static void LoadTransactions(SqlConnection connection, DefinCompItemDetailsViewModel model, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    t.Transaction_ID,
    t.Transaction_Type,
    t.Transaction_Serial,
    t.Transaction_Date,
    t.Total,
    t.NetValue,
    t.VAT,
    t.NoteSerial1,
    t.NoteId,
    t.InvoiceOrderNo,
    t.ManualNO
FROM dbo.Transactions t
WHERE (t.InvoiceOrderNo = @Id OR t.IDDefCIT = @Id)
  AND t.Transaction_Type IN (27, 28)
ORDER BY t.Transaction_Type, t.Transaction_ID;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.LinkedTransactions.Add(new DefinCompItemLinkedTransactionViewModel
                        {
                            TransactionId = ReadInt(reader, "Transaction_ID"),
                            TransactionType = ReadInt(reader, "Transaction_Type"),
                            TransactionSerial = ReadString(reader, "Transaction_Serial"),
                            TransactionDate = ReadDateTime(reader, "Transaction_Date"),
                            Total = ReadDecimal(reader, "Total"),
                            NetValue = ReadDecimal(reader, "NetValue"),
                            Vat = ReadDecimal(reader, "VAT"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            NoteId = ReadNullableInt(reader, "NoteId"),
                            InvoiceOrderNo = ReadNullableInt(reader, "InvoiceOrderNo"),
                            Description = ReadString(reader, "ManualNO")
                        });
                    }
                }
            }
        }

        private static void LoadTotals(DefinCompItemDetailsViewModel model)
        {
            model.ComponentQtyTotal = 0m;
            model.ComponentCostTotal = 0m;
            model.OutputQtyTotal = 0m;
            model.OutputCostTotal = 0m;

            foreach (var line in model.Components)
            {
                var qty = Convert.ToDecimal(line.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                var cost = Convert.ToDecimal(line.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                model.ComponentQtyTotal += qty;
                model.ComponentCostTotal += qty * cost;
            }

            foreach (var line in model.Outputs)
            {
                var qty = Convert.ToDecimal(line.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                var cost = Convert.ToDecimal(line.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                model.OutputQtyTotal += qty;
                model.OutputCostTotal += qty * cost;
            }

            model.Difference = model.OutputCostTotal - model.ComponentCostTotal;
        }

        private static Totals CalculateTotals(IList<DefinCompItemLineRequest> lines)
        {
            var totals = new Totals();
            if (lines == null)
            {
                return totals;
            }

            foreach (var line in lines)
            {
                var qty = Convert.ToDecimal(line.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                var cost = Convert.ToDecimal(line.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                totals.Qty += qty;
                totals.Cost += qty * cost;
            }

            return totals;
        }

        private static DefinCompItemDetailsViewModel InsertHeader(SqlConnection connection, SqlTransaction transaction, DefinCompItemSaveRequest request, MainErpUserContext user)
        {
            var model = new DefinCompItemDetailsViewModel();
            var maxNo = string.IsNullOrWhiteSpace(request.MaxNo) ? BuildDefaultMaxNo(request.RecordDate.GetValueOrDefault(DateTime.Today)) : request.MaxNo.Trim();
            var maxName = string.IsNullOrWhiteSpace(request.MaxName) ? maxNo : request.MaxName.Trim();
            var orderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? maxNo : request.OrderNo.Trim();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.TblDefComItem
(
    RecordDate, BranchID, StoreID, StoreID2, StoreID3, ItemNameID, UserID, UnitID, CusID, MaxNo, MaxName,
    Allocated, AlloPay, AlloRecep, ItemCode, Qty1, TransactionID1, TransactionID2, NoteSerial11, NoteSerial12,
    Price, TotalAdd, GroupID, PaymentType, widtj, hight, TotalDisc, Net, TotalWithVat, Vat2, Emp_id, BoxID,
    Period, RecTime, RecDate, TransactionID3, NoteSerial13, TransactionID4, NoteSerial14, Length,
    TransactionID5, NoteSerial15, Noteid3, MaxNo2, order_no, OrderID, DepandToConv, CBoBasedON, SessionCode, Copied, TransactionID6, NoteSerial16
)
VALUES
(
    @RecordDate, @BranchId, @StoreId, @StoreId2, @StoreId3, @ItemNameId, @UserId, @UnitId, @CusId, @MaxNo, @MaxName,
    0, 0, 0, @ItemCode, @Qty1, 0, 0, N'', N'',
    @Price, @TotalAdd, @GroupId, @PaymentType, @Width, @Height, @TotalDisc, @Net, @TotalWithVat, @Vat2, @EmpId, @BoxId,
    @Period, GETDATE(), @RecordDate, 0, N'', 0, N'', @Length,
    0, N'', 0, @MaxNo, @OrderNo, 0, 0, 0, @SessionCode, 0, 0, N''
);
SELECT CAST(SCOPE_IDENTITY() AS int);";
                BindHeaderParameters(command, request, user, maxNo, maxName, orderNo);
                model.Id = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            model.RecordDate = request.RecordDate;
            model.BranchId = request.BranchId;
            model.StoreId = request.StoreId;
            model.StoreId2 = request.StoreId2;
            model.StoreId3 = request.StoreId3;
            model.CusId = request.CusId;
            model.ItemNameId = request.ItemNameId;
            model.MaxNo = maxNo;
            model.MaxName = maxName;
            model.OrderNo = orderNo;
            model.GroupId = request.GroupId;
            model.PaymentType = request.PaymentType;
            model.EmpId = request.EmpId;
            model.BoxId = request.BoxId;
            model.Period = request.Period;
            model.Qty1 = request.Qty1;
            model.Price = request.Price;
            model.TotalAdd = request.TotalAdd;
            model.TotalDisc = request.TotalDisc;
            model.Net = request.Net;
            model.TotalWithVat = request.TotalWithVat;
            model.Vat2 = request.Vat2;
            return model;
        }

        private static DefinCompItemDetailsViewModel UpdateHeader(SqlConnection connection, SqlTransaction transaction, DefinCompItemSaveRequest request, MainErpUserContext user)
        {
            var model = new DefinCompItemDetailsViewModel();
            var maxNo = string.IsNullOrWhiteSpace(request.MaxNo) ? BuildDefaultMaxNo(request.RecordDate.GetValueOrDefault(DateTime.Today)) : request.MaxNo.Trim();
            var maxName = string.IsNullOrWhiteSpace(request.MaxName) ? maxNo : request.MaxName.Trim();
            var orderNo = string.IsNullOrWhiteSpace(request.OrderNo) ? maxNo : request.OrderNo.Trim();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.TblDefComItem
SET RecordDate = @RecordDate,
    BranchID = @BranchId,
    StoreID = @StoreId,
    StoreID2 = @StoreId2,
    StoreID3 = @StoreId3,
    ItemNameID = @ItemNameId,
    UserID = @UserId,
    UnitID = @UnitId,
    CusID = @CusId,
    MaxNo = @MaxNo,
    MaxName = @MaxName,
    ItemCode = @ItemCode,
    Qty1 = @Qty1,
    Price = @Price,
    TotalAdd = @TotalAdd,
    GroupID = @GroupId,
    PaymentType = @PaymentType,
    widtj = @Width,
    hight = @Height,
    TotalDisc = @TotalDisc,
    Net = @Net,
    TotalWithVat = @TotalWithVat,
    Vat2 = @Vat2,
    Emp_id = @EmpId,
    BoxID = @BoxId,
    Period = @Period,
    RecDate = @RecordDate,
    Length = @Length,
    order_no = @OrderNo,
    SessionCode = @SessionCode
WHERE ID = @Id;";
                BindHeaderParameters(command, request, user, maxNo, maxName, orderNo);
                command.Parameters.Add("@Id", SqlDbType.Int).Value = request.Id.Value;
                command.ExecuteNonQuery();
            }

            model.Id = request.Id.Value;
            model.RecordDate = request.RecordDate;
            model.BranchId = request.BranchId;
            model.StoreId = request.StoreId;
            model.StoreId2 = request.StoreId2;
            model.StoreId3 = request.StoreId3;
            model.CusId = request.CusId;
            model.ItemNameId = request.ItemNameId;
            model.MaxNo = maxNo;
            model.MaxName = maxName;
            model.OrderNo = orderNo;
            model.GroupId = request.GroupId;
            model.PaymentType = request.PaymentType;
            model.EmpId = request.EmpId;
            model.BoxId = request.BoxId;
            model.Period = request.Period;
            model.Qty1 = request.Qty1;
            model.Price = request.Price;
            model.TotalAdd = request.TotalAdd;
            model.TotalDisc = request.TotalDisc;
            model.Net = request.Net;
            model.TotalWithVat = request.TotalWithVat;
            model.Vat2 = request.Vat2;
            return model;
        }

        private static void BindHeaderParameters(SqlCommand command, DefinCompItemSaveRequest request, MainErpUserContext user, string maxNo, string maxName, string orderNo)
        {
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = request.RecordDate.HasValue ? (object)request.RecordDate.Value : DBNull.Value;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)request.BranchId ?? DBNull.Value;
            command.Parameters.Add("@StoreId", SqlDbType.Int).Value = (object)request.StoreId ?? DBNull.Value;
            command.Parameters.Add("@StoreId2", SqlDbType.Int).Value = (object)request.StoreId2 ?? DBNull.Value;
            command.Parameters.Add("@StoreId3", SqlDbType.Int).Value = (object)request.StoreId3 ?? DBNull.Value;
            command.Parameters.Add("@ItemNameId", SqlDbType.Int).Value = (object)request.ItemNameId ?? DBNull.Value;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
            command.Parameters.Add("@UnitId", SqlDbType.Int).Value = DBNull.Value;
            command.Parameters.Add("@CusId", SqlDbType.Int).Value = (object)request.CusId ?? DBNull.Value;
            command.Parameters.Add("@MaxNo", SqlDbType.NVarChar, 800).Value = maxNo;
            command.Parameters.Add("@MaxName", SqlDbType.NVarChar, 800).Value = maxName;
            command.Parameters.Add("@ItemCode", SqlDbType.NVarChar, 800).Value = string.IsNullOrWhiteSpace(request.MaxNo) ? maxNo : request.MaxNo.Trim();
            command.Parameters.Add("@Qty1", SqlDbType.Float).Value = Convert.ToDouble(request.Qty1, CultureInfo.InvariantCulture);
            command.Parameters.Add("@Price", SqlDbType.Money).Value = Convert.ToDecimal(request.Price, CultureInfo.InvariantCulture);
            command.Parameters.Add("@TotalAdd", SqlDbType.Money).Value = Convert.ToDecimal(request.TotalAdd, CultureInfo.InvariantCulture);
            command.Parameters.Add("@GroupId", SqlDbType.Int).Value = (object)request.GroupId ?? DBNull.Value;
            command.Parameters.Add("@PaymentType", SqlDbType.Int).Value = (object)request.PaymentType ?? DBNull.Value;
            command.Parameters.Add("@Width", SqlDbType.Money).Value = 0m;
            command.Parameters.Add("@Height", SqlDbType.Money).Value = 0m;
            command.Parameters.Add("@TotalDisc", SqlDbType.Money).Value = Convert.ToDecimal(request.TotalDisc, CultureInfo.InvariantCulture);
            command.Parameters.Add("@Net", SqlDbType.Money).Value = Convert.ToDecimal(request.Net, CultureInfo.InvariantCulture);
            command.Parameters.Add("@TotalWithVat", SqlDbType.Money).Value = Convert.ToDecimal(request.TotalWithVat, CultureInfo.InvariantCulture);
            command.Parameters.Add("@Vat2", SqlDbType.Money).Value = Convert.ToDecimal(request.Vat2, CultureInfo.InvariantCulture);
            command.Parameters.Add("@EmpId", SqlDbType.Int).Value = (object)request.EmpId ?? DBNull.Value;
            command.Parameters.Add("@BoxId", SqlDbType.Int).Value = (object)request.BoxId ?? DBNull.Value;
            command.Parameters.Add("@Period", SqlDbType.Float).Value = (object)request.Period ?? DBNull.Value;
            command.Parameters.Add("@Length", SqlDbType.Float).Value = 0d;
            command.Parameters.Add("@OrderNo", SqlDbType.NVarChar, 800).Value = orderNo;
            command.Parameters.Add("@SessionCode", SqlDbType.NVarChar, 510).Value = user == null || string.IsNullOrWhiteSpace(user.ConnectionStringName) ? (object)DBNull.Value : user.ConnectionStringName;
        }

        private static void DeleteChildRows(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
DELETE FROM dbo.TblDefComItemDet WHERE IDDefCIT = @Id;
DELETE FROM dbo.TblDefComItemData WHERE IDDefCIT = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteLinkedTransactions(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
DECLARE @TransactionIds TABLE (Transaction_ID int PRIMARY KEY);

INSERT INTO @TransactionIds (Transaction_ID)
SELECT Transaction_ID
FROM dbo.Transactions
WHERE (InvoiceOrderNo = @Id OR IDDefCIT = @Id)
  AND Transaction_Type IN (27, 28);

DELETE dev
FROM dbo.DOUBLE_ENTREY_VOUCHERS dev
WHERE dev.Notes_ID IN (SELECT n.NoteID FROM dbo.Notes n WHERE n.Transaction_ID IN (SELECT Transaction_ID FROM @TransactionIds))
   OR dev.Transaction_ID IN (SELECT Transaction_ID FROM @TransactionIds)
   OR dev.Transaction_ID1 IN (SELECT Transaction_ID FROM @TransactionIds);

DELETE n
FROM dbo.Notes n
WHERE n.Transaction_ID IN (SELECT Transaction_ID FROM @TransactionIds);

DELETE d
FROM dbo.Transaction_Details d
WHERE d.Transaction_ID IN (SELECT Transaction_ID FROM @TransactionIds);

DELETE t
FROM dbo.Transactions t
WHERE t.Transaction_ID IN (SELECT Transaction_ID FROM @TransactionIds);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private static bool HasPostedTransactions(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT TOP 1 1
FROM dbo.Transactions
WHERE (InvoiceOrderNo = @Id OR IDDefCIT = @Id)
  AND Transaction_Type IN (27, 28)
  AND (ISNULL(IsPosted, 0) = 1 OR ISNULL(Posted, 0) = 1);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return command.ExecuteScalar() != null;
            }
        }

        private static void UpdateHeaderLinks(SqlConnection connection, SqlTransaction transaction, int id, int issueTransactionId, int receiptTransactionId, string issueSerial, string receiptSerial, Totals componentTotals, Totals outputTotals)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.TblDefComItem
SET TransactionID1 = @IssueTransactionId,
    TransactionID2 = @ReceiptTransactionId,
    NoteSerial11 = @IssueSerial,
    NoteSerial12 = @ReceiptSerial,
    Qty1 = @ComponentQtyTotal,
    Price = @ComponentCostTotal,
    TotalAdd = @OutputCostTotal,
    Net = @OutputCostTotal,
    TotalWithVat = @OutputCostTotal,
    Vat2 = @Difference,
    Allocated = 1,
    AlloPay = 1,
    AlloRecep = 1
WHERE ID = @Id;";
                command.Parameters.Add("@IssueTransactionId", SqlDbType.Int).Value = issueTransactionId;
                command.Parameters.Add("@ReceiptTransactionId", SqlDbType.Int).Value = receiptTransactionId;
                command.Parameters.Add("@IssueSerial", SqlDbType.NVarChar, 100).Value = issueSerial;
                command.Parameters.Add("@ReceiptSerial", SqlDbType.NVarChar, 100).Value = receiptSerial;
                command.Parameters.Add("@ComponentQtyTotal", SqlDbType.Float).Value = Convert.ToDouble(componentTotals.Qty, CultureInfo.InvariantCulture);
                command.Parameters.Add("@ComponentCostTotal", SqlDbType.Money).Value = componentTotals.Cost;
                command.Parameters.Add("@OutputCostTotal", SqlDbType.Money).Value = outputTotals.Cost;
                command.Parameters.Add("@Difference", SqlDbType.Money).Value = outputTotals.Cost - componentTotals.Cost;
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateOutputLinks(SqlConnection connection, SqlTransaction transaction, int id, int receiptTransactionId, string receiptSerial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.TblDefComItemData
SET TransactionID4 = @ReceiptTransactionId,
    NoteSerial14 = @ReceiptSerial
WHERE IDDefCIT = @Id;";
                command.Parameters.Add("@ReceiptTransactionId", SqlDbType.Int).Value = receiptTransactionId;
                command.Parameters.Add("@ReceiptSerial", SqlDbType.NVarChar, 100).Value = receiptSerial;
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private static int InsertInventoryTransaction(SqlConnection connection, SqlTransaction transaction, DefinCompItemDetailsViewModel header, int transactionType, string serial, decimal total, MainErpUserContext user, string description)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.Transactions
(
    Transaction_ID, Transaction_Serial, Transaction_Date, Transaction_Type, CusID, StoreID, UserID, Total, NoteSerial, NoteSerial1,
    BranchId, PayedValue, NetValue, RemainValue, ManualNO, CBoBasedON, RecTime, VAT, InvoiceOrderNo, IDDefCIT,
    SessionCode, DepandToConv, TypeInvoice, empID, BoxID
)
VALUES
(
    @TransactionId, @TransactionSerial, @TransactionDate, @TransactionType, @CusId, @StoreId, @UserId, @Total, @NoteSerial, @NoteSerial1,
    @BranchId, 0, @Total, 0, @ManualNo, 0, CONVERT(nvarchar(40), GETDATE(), 120), 0, @InvoiceOrderNo, @IdDefCIT,
    @SessionCode, @DepandToConv, 0, @EmpId, @BoxId
);
SELECT CAST(SCOPE_IDENTITY() AS int);";
                var transactionId = NextManualId(connection, transaction, "Transactions", "Transaction_ID");
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@TransactionSerial", SqlDbType.NVarChar, 100).Value = serial;
                command.Parameters.Add("@TransactionDate", SqlDbType.DateTime).Value = header.RecordDate.HasValue ? (object)header.RecordDate.Value : DBNull.Value;
                command.Parameters.Add("@TransactionType", SqlDbType.Int).Value = transactionType;
                command.Parameters.Add("@CusId", SqlDbType.Int).Value = (object)header.CusId ?? DBNull.Value;
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = (object)header.StoreId ?? DBNull.Value;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
                command.Parameters.Add("@Total", SqlDbType.Money).Value = total;
                command.Parameters.Add("@NoteSerial", SqlDbType.NVarChar, 50).Value = serial;
                command.Parameters.Add("@NoteSerial1", SqlDbType.NVarChar, 50).Value = serial;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)header.BranchId ?? DBNull.Value;
                command.Parameters.Add("@ManualNo", SqlDbType.NVarChar, 510).Value = description;
                command.Parameters.Add("@InvoiceOrderNo", SqlDbType.Int).Value = header.Id;
                command.Parameters.Add("@IdDefCIT", SqlDbType.Int).Value = header.Id;
                command.Parameters.Add("@SessionCode", SqlDbType.NVarChar, 510).Value = user == null || string.IsNullOrWhiteSpace(user.ConnectionStringName) ? (object)DBNull.Value : user.ConnectionStringName;
                command.Parameters.Add("@DepandToConv", SqlDbType.Int).Value = header.Id;
                command.Parameters.Add("@EmpId", SqlDbType.Int).Value = (object)header.EmpId ?? DBNull.Value;
                command.Parameters.Add("@BoxId", SqlDbType.Int).Value = (object)header.BoxId ?? DBNull.Value;
                command.ExecuteNonQuery();
                return transactionId;
            }
        }

        private static void InsertTransactionDetails(SqlConnection connection, SqlTransaction transaction, int transactionId, IList<DefinCompItemLineRequest> lines, bool isComponent)
        {
            if (lines == null)
            {
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null || !line.ItemId.HasValue || !line.UnitId.HasValue)
                {
                    continue;
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
INSERT INTO dbo.Transaction_Details
(
    Transaction_ID, Item_ID, ItemCase, ItemSerial, Quantity, Price, ItemDiscountType, ItemDiscount, guaranteeTime,
    CostPrice, ItemProfit, Remarks, UnitId, ShowQty, QtyBySmalltUnit, BranchId, LineID,
    ItemID2, PercentCost, SessionCode, SavedItemType, TotalPrice
)
VALUES
(
    @TransactionId, @ItemId, 1, @ItemSerial, @Quantity, @Price, 0, 0, 0,
    @CostPrice, 0, @Remarks, @UnitId, @ShowQty, @QtyBySmalltUnit, @BranchId, @LineId,
    @ItemId2, @PercentCost, @SessionCode, @SavedItemType, @TotalPrice
);";
                    var qty = Convert.ToDouble(line.Qty.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                    var cost = Convert.ToDecimal(line.Cost.GetValueOrDefault(0d), CultureInfo.InvariantCulture);
                    command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                    command.Parameters.Add("@ItemId", SqlDbType.Int).Value = line.ItemId.Value;
                    command.Parameters.Add("@ItemSerial", SqlDbType.NVarChar, 100).Value = (object)(line.Remark ?? string.Empty);
                    command.Parameters.Add("@Quantity", SqlDbType.Float).Value = qty;
                    command.Parameters.Add("@Price", SqlDbType.Float).Value = Convert.ToDouble(line.Price.GetValueOrDefault(line.Cost.GetValueOrDefault(0d)), CultureInfo.InvariantCulture);
                    command.Parameters.Add("@CostPrice", SqlDbType.Money).Value = cost;
                    command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 510).Value = (object)(line.Remark ?? string.Empty);
                    command.Parameters.Add("@UnitId", SqlDbType.Int).Value = line.UnitId.Value;
                    command.Parameters.Add("@ShowQty", SqlDbType.Float).Value = qty;
                    command.Parameters.Add("@QtyBySmalltUnit", SqlDbType.Float).Value = 1d;
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DBNull.Value;
                    command.Parameters.Add("@LineId", SqlDbType.Int).Value = (object)line.LineId ?? (i + 1);
                    command.Parameters.Add("@ItemId2", SqlDbType.Int).Value = (object)line.ItemId2 ?? DBNull.Value;
                    command.Parameters.Add("@PercentCost", SqlDbType.Float).Value = Convert.ToDouble(cost, CultureInfo.InvariantCulture);
                    command.Parameters.Add("@SessionCode", SqlDbType.NVarChar, 510).Value = DBNull.Value;
                    command.Parameters.Add("@SavedItemType", SqlDbType.Int).Value = isComponent ? 0 : 1;
                    command.Parameters.Add("@TotalPrice", SqlDbType.Float).Value = Convert.ToDouble(cost * (decimal)qty, CultureInfo.InvariantCulture);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void InsertComponentRows(SqlConnection connection, SqlTransaction transaction, int id, IList<DefinCompItemLineRequest> lines)
        {
            if (lines == null)
            {
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null || !line.ItemId.HasValue || !line.UnitId.HasValue)
                {
                    continue;
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
INSERT INTO dbo.TblDefComItemDet
(
    IDDefCIT, ItemID, UnitID, SpecID1, SpecID2, SpecID3, SpecID4, Amout1, Amout2, Amout3, Amout4, Qty, cost,
    FlgX, TepQty, IsAdd, Price, Total, IsDeleted, ItemID2, ItemCode2, isReplaced, LineID, OldPrice, lowering,
    Increase, TableID, QtyOut, IsRow, widtj, hight, Length, thickness, DO, DI, Diameter
)
VALUES
(
    @Id, @ItemId, @UnitId, @SpecId1, @SpecId2, @SpecId3, @SpecId4, 0, 0, 0, 0, @Qty, @Cost,
    @FlgX, @Qty, @IsAdd, @Price, @Total, 0, @ItemId2, 0, 0, @LineId, @OldPrice, 0,
    0, 0, 0, 0, @Width, @Height, @Length, @Thickness, @Diameter, 0, 0
);";
                    var qty = line.Qty.GetValueOrDefault(0d);
                    var cost = line.Cost.GetValueOrDefault(0d);
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@ItemId", SqlDbType.Int).Value = line.ItemId.Value;
                    command.Parameters.Add("@UnitId", SqlDbType.Int).Value = line.UnitId.Value;
                    command.Parameters.Add("@SpecId1", SqlDbType.Int).Value = (object)line.SpecId1 ?? DBNull.Value;
                    command.Parameters.Add("@SpecId2", SqlDbType.Int).Value = (object)line.SpecId2 ?? DBNull.Value;
                    command.Parameters.Add("@SpecId3", SqlDbType.Int).Value = (object)line.SpecId3 ?? DBNull.Value;
                    command.Parameters.Add("@SpecId4", SqlDbType.Int).Value = (object)line.SpecId4 ?? DBNull.Value;
                    command.Parameters.Add("@Qty", SqlDbType.Float).Value = qty;
                    command.Parameters.Add("@Cost", SqlDbType.Float).Value = cost;
                    command.Parameters.Add("@FlgX", SqlDbType.Float).Value = qty;
                    command.Parameters.Add("@IsAdd", SqlDbType.Bit).Value = (object)line.IsAdd ?? DBNull.Value;
                    command.Parameters.Add("@Price", SqlDbType.Money).Value = line.Price.HasValue ? (object)line.Price.Value : cost;
                    command.Parameters.Add("@Total", SqlDbType.Money).Value = line.Total.HasValue ? (object)line.Total.Value : (object)(qty * cost);
                    command.Parameters.Add("@ItemId2", SqlDbType.Int).Value = (object)line.ItemId2 ?? line.ItemId.Value;
                    command.Parameters.Add("@LineId", SqlDbType.Int).Value = (object)line.LineId ?? (i + 1);
                    command.Parameters.Add("@OldPrice", SqlDbType.Float).Value = line.Price.HasValue ? (object)line.Price.Value : cost;
                    command.Parameters.Add("@Width", SqlDbType.Float).Value = (object)line.Width ?? DBNull.Value;
                    command.Parameters.Add("@Height", SqlDbType.Float).Value = (object)line.Height ?? DBNull.Value;
                    command.Parameters.Add("@Length", SqlDbType.Float).Value = (object)line.Length ?? DBNull.Value;
                    command.Parameters.Add("@Thickness", SqlDbType.Float).Value = (object)line.Thickness ?? DBNull.Value;
                    command.Parameters.Add("@Diameter", SqlDbType.Float).Value = (object)line.Diameter ?? DBNull.Value;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void InsertOutputRows(SqlConnection connection, SqlTransaction transaction, int id, IList<DefinCompItemLineRequest> lines)
        {
            if (lines == null)
            {
                return;
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == null || !line.ItemId.HasValue || !line.UnitId.HasValue)
                {
                    continue;
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.TblDefComItemData
(
    IDDefCIT, GroupID, itemcode, UnitID, Special, cost, Qty, Price, widtj, hight, TotalDisc, TotalAdd, Vat2, TotalWithVat,
    ItemID, ItemType, ItemID2, Total, Net, ItemCode2, LineID, Trans_DiscountType, Trans_Discount, Trans_DiscountPercent,
    Remark, GroupIDBuiltin, BuiltinItemID, Length, TransactionID4, NoteSerial14, PercentCost, Diameter, thickness, DO, DI,
    ItemID5, CountItem2, CountItem5, AreaL, NoteSerial15, TransactionID5, Diameter2, thickness2, widtj2, DO2, DI2, hight2, Length2
)
VALUES
(
    @Id, @GroupId, @ItemCode, @UnitId, @Special, @Cost, @Qty, @Price, @Width, @Height, 0, 0, 0, @Total,
    @ItemId, 0, @ItemId2, @Total, @Total, @ItemCode2, @LineId, 0, 0, 0,
    @Remark, 0, 0, @Length, 0, N'', @PercentCost, @Diameter, @Thickness, @DO, @DI,
    0, 0, 0, N'', N'', 0, @Diameter2, @Thickness2, @Width2, @DO2, @DI2, @Height2, @Length2
);";
                    var qty = line.Qty.GetValueOrDefault(0d);
                    var cost = line.Cost.GetValueOrDefault(0d);
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    command.Parameters.Add("@GroupId", SqlDbType.Int).Value = (object)DBNull.Value;
                    command.Parameters.Add("@ItemCode", SqlDbType.Int).Value = line.ItemId.Value;
                    command.Parameters.Add("@UnitId", SqlDbType.Int).Value = line.UnitId.Value;
                    command.Parameters.Add("@Special", SqlDbType.VarChar, 1).Value = DBNull.Value;
                    command.Parameters.Add("@Cost", SqlDbType.Float).Value = cost;
                    command.Parameters.Add("@Qty", SqlDbType.Float).Value = qty;
                    command.Parameters.Add("@Price", SqlDbType.Money).Value = line.Price.HasValue ? (object)line.Price.Value : cost;
                    command.Parameters.Add("@Total", SqlDbType.Money).Value = line.Total.HasValue ? (object)line.Total.Value : (object)(qty * cost);
                    command.Parameters.Add("@ItemId", SqlDbType.Int).Value = line.ItemId.Value;
                    command.Parameters.Add("@ItemId2", SqlDbType.Int).Value = (object)line.ItemId2 ?? line.ItemId.Value;
                    command.Parameters.Add("@ItemCode2", SqlDbType.Int).Value = (object)line.ItemId2 ?? line.ItemId.Value;
                    command.Parameters.Add("@LineId", SqlDbType.Int).Value = (object)line.LineId ?? (i + 1);
                    command.Parameters.Add("@Remark", SqlDbType.NVarChar, 100).Value = (object)(line.Remark ?? string.Empty);
                    command.Parameters.Add("@PercentCost", SqlDbType.Float).Value = Convert.ToDouble(cost, CultureInfo.InvariantCulture);
                    command.Parameters.Add("@Width", SqlDbType.Float).Value = (object)line.Width ?? DBNull.Value;
                    command.Parameters.Add("@Height", SqlDbType.Float).Value = (object)line.Height ?? DBNull.Value;
                    command.Parameters.Add("@Length", SqlDbType.Float).Value = (object)line.Length ?? DBNull.Value;
                    command.Parameters.Add("@Thickness", SqlDbType.Float).Value = (object)line.Thickness ?? DBNull.Value;
                    command.Parameters.Add("@DO", SqlDbType.Float).Value = DBNull.Value;
                    command.Parameters.Add("@DI", SqlDbType.Float).Value = DBNull.Value;
                    command.Parameters.Add("@Diameter", SqlDbType.Float).Value = (object)line.Diameter ?? DBNull.Value;
                    command.Parameters.Add("@Diameter2", SqlDbType.Float).Value = (object)line.Diameter ?? DBNull.Value;
                    command.Parameters.Add("@Thickness2", SqlDbType.Float).Value = (object)line.Thickness ?? DBNull.Value;
                    command.Parameters.Add("@Width2", SqlDbType.Float).Value = (object)line.Width ?? DBNull.Value;
                    command.Parameters.Add("@DO2", SqlDbType.Float).Value = DBNull.Value;
                    command.Parameters.Add("@DI2", SqlDbType.Float).Value = DBNull.Value;
                    command.Parameters.Add("@Height2", SqlDbType.Float).Value = (object)line.Height ?? DBNull.Value;
                    command.Parameters.Add("@Length2", SqlDbType.Float).Value = (object)line.Length ?? DBNull.Value;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void InsertAccountingNote(SqlConnection connection, SqlTransaction transaction, DefinCompItemDetailsViewModel header, int noteType, int transactionId, string serial, decimal value, string debitAccount, string creditAccount, string description, MainErpUserContext user, string orderNo)
        {
            var noteId = NextManualId(connection, transaction, "Notes", "NoteID");
            var voucherId = NextManualId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
            var noteSerialValue = (double)noteId;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, UserID, Remark, NotePosted,
    PostedBy, PostDate, ORDER_NO, ManualNo, branch_no, user_name, person, too, Double_Entry_Vouchers_ID,
    TotalValue, PayDes, PayDes1, Account_DebitSide, Account_CreditSide, DateRec
)
VALUES
(
    @NoteId, @NoteDate, @NoteType, @NoteSerial, @NoteSerial1, @NoteValue, @TransactionId, @UserId, @Remark, 0,
    @UserId, GETDATE(), @OrderNo, @ManualNo, @BranchNo, @UserName, @Person, @Too, @VoucherId,
    @TotalValue, @PayDes, @PayDes1, @DebitSide, @CreditSide, GETDATE()
);

INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value, Credit_Or_Debit, Double_Entry_Vouchers_Description,
    RecordDate, Notes_ID, UserID, Posted, branch_id, DEV_Serial, Transaction_ID
)
VALUES
(
    @VoucherId, 1, @DebitAccount, @Value, 0, @Description, @NoteDate, @NoteId, @UserId, 0, @BranchNo, @Serial, @TransactionId
),
(
    @VoucherId, 2, @CreditAccount, @Value, 1, @Description, @NoteDate, @NoteId, @UserId, 0, @BranchNo, @Serial, @TransactionId
);

UPDATE dbo.Notes
SET Double_Entry_Vouchers_ID = @VoucherId
WHERE NoteID = @NoteId;";
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@VoucherId", SqlDbType.Int).Value = voucherId;
                command.Parameters.Add("@NoteDate", SqlDbType.DateTime).Value = header.RecordDate.HasValue ? (object)header.RecordDate.Value : DateTime.Now;
                command.Parameters.Add("@NoteType", SqlDbType.Int).Value = noteType;
                command.Parameters.Add("@NoteSerial", SqlDbType.Float).Value = noteSerialValue;
                command.Parameters.Add("@NoteSerial1", SqlDbType.Float).Value = noteSerialValue;
                command.Parameters.Add("@NoteValue", SqlDbType.Float).Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
                command.Parameters.Add("@Remark", SqlDbType.NVarChar, 8000).Value = description;
                command.Parameters.Add("@OrderNo", SqlDbType.NVarChar, 100).Value = orderNo;
                command.Parameters.Add("@ManualNo", SqlDbType.NVarChar, 510).Value = serial;
                command.Parameters.Add("@BranchNo", SqlDbType.Int).Value = (object)header.BranchId ?? DBNull.Value;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = user == null ? (object)DBNull.Value : user.UserName;
                command.Parameters.Add("@Person", SqlDbType.NVarChar, 8000).Value = description;
                command.Parameters.Add("@Too", SqlDbType.NVarChar, 2000).Value = description;
                command.Parameters.Add("@TotalValue", SqlDbType.Float).Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                command.Parameters.Add("@PayDes", SqlDbType.NVarChar, 8000).Value = description;
                command.Parameters.Add("@PayDes1", SqlDbType.NVarChar, 8000).Value = description;
                command.Parameters.Add("@DebitSide", SqlDbType.NVarChar, 100).Value = debitAccount;
                command.Parameters.Add("@CreditSide", SqlDbType.NVarChar, 100).Value = creditAccount;
                command.Parameters.Add("@DebitAccount", SqlDbType.NVarChar, 100).Value = debitAccount;
                command.Parameters.Add("@CreditAccount", SqlDbType.NVarChar, 100).Value = creditAccount;
                command.Parameters.Add("@Value", SqlDbType.Money).Value = value;
                command.Parameters.Add("@Description", SqlDbType.NVarChar, 8000).Value = description;
                command.Parameters.Add("@Serial", SqlDbType.NVarChar, 100).Value = serial;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertAccountingNoteWithFallback(SqlConnection connection, SqlTransaction transaction, DefinCompItemDetailsViewModel header, int noteType, int transactionId, string serial, decimal value, string debitAccount, string creditAccount, string description, MainErpUserContext user, string orderNo)
        {
            var savepoint = "DCA" + noteType.ToString(CultureInfo.InvariantCulture) + "_" + transactionId.ToString(CultureInfo.InvariantCulture);
            transaction.Save(savepoint);

            try
            {
                InsertAccountingNote(connection, transaction, header, noteType, transactionId, serial, value, debitAccount, creditAccount, description, user, orderNo);
            }
            catch (SqlException ex) when (ex.Message.IndexOf("FK_DOUBLE_ENTREY_VOUCHERS_ACCOUNTS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                transaction.Rollback(savepoint);
                var fallback = LoadFallbackAccountingAccounts(connection, transaction);
                InsertAccountingNote(connection, transaction, header, noteType, transactionId, serial, value, fallback.DebitAccount, fallback.CreditAccount, description, user, orderNo);
            }
        }

        private static AccountingFallbackAccounts LoadFallbackAccountingAccounts(SqlConnection connection, SqlTransaction transaction)
        {
            var codes = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT TOP (2) Account_Code
FROM dbo.ACCOUNTS
WHERE Account_Code IS NOT NULL AND LTRIM(RTRIM(Account_Code)) <> N''
ORDER BY Account_ID;";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture);
                        code = NormalizeAccountCode(code);
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            codes.Add(code);
                        }
                    }
                }
            }

            var debitAccount = codes.Count > 0 ? codes[0] : null;
            var creditAccount = codes.Count > 1 ? codes[1] : debitAccount;
            if (string.IsNullOrWhiteSpace(debitAccount) || string.IsNullOrWhiteSpace(creditAccount))
            {
                throw new InvalidOperationException("ظ„ط§ ظٹظ…ظƒظ† ط¥ظٹط¬ط§ط¯ ط­ط³ط§ط¨ط§طھ ط§ط­طھظٹط§ط·ظٹط© طµط§ظ„ط­ط© ظ„طھط³ط¬ظٹظ„ ط§ظ„ظ‚ظٹط¯ ط§ظ„ظ…ط­ط§ط³ط¨ظٹ.");
            }

            return new AccountingFallbackAccounts
            {
                DebitAccount = debitAccount,
                CreditAccount = creditAccount
            };
        }

        private sealed class AccountingFallbackAccounts
        {
            public string DebitAccount { get; set; }
            public string CreditAccount { get; set; }
        }

        private static string BuildSerial(string maxNo, int id, int transactionType)
        {
            var root = string.IsNullOrWhiteSpace(maxNo) ? id.ToString(CultureInfo.InvariantCulture) : maxNo.Trim();
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1}", root, transactionType);
        }

        private static string BuildDefaultMaxNo(DateTime date)
        {
            return string.Format(CultureInfo.InvariantCulture, "DEF-{0:yyyyMMdd-HHmmss}", date);
        }

        private static string NormalizeErrorMessage(Exception ex)
        {
            var sql = ex as SqlException;
            if (sql != null)
            {
                return sql.Message;
            }

            if (ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message))
            {
                return ex.InnerException.Message;
            }

            return ex.Message;
        }

        private static PostingAccounts LoadPostingAccounts(SqlConnection connection, SqlTransaction transaction, int? branchId, int? storeId)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
SELECT TOP 1
    b.Account_Code AS BranchAccount,
    COALESCE(s.Account_Code, s.Account_Code0, s.Account_Code1, s.Account_Code2, s.Account_Code3) AS StoreAccount
FROM dbo.TblBranchesData b
LEFT JOIN dbo.TblStore s ON s.StoreID = @StoreId
WHERE b.branch_id = @BranchId;";
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = (object)storeId ?? DBNull.Value;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new PostingAccounts
                        {
                            BranchAccount = NormalizeAccountCode(ReadString(reader, "BranchAccount")),
                            StoreAccount = NormalizeAccountCode(ReadString(reader, "StoreAccount")),
                            CostAccount = NormalizeAccountCode(ReadString(reader, "BranchAccount"))
                        };
                    }
                }
            }

            return new PostingAccounts();
        }

        private static string NormalizeAccountCode(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return null;
            }

            return accountCode.Trim();
        }

        private static int NextManualId(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = string.Format(CultureInfo.InvariantCulture, "SELECT ISNULL(MAX({0}), 0) + 1 FROM dbo.{1} WITH (TABLOCKX, HOLDLOCK);", columnName, tableName);
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }


        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END;";
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = "dbo." + tableName;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
            }
        }

        private static int ReadInt(IDataRecord reader, string columnName)
        {
            return Convert.ToInt32(reader[columnName], CultureInfo.InvariantCulture);
        }

        private static int? ReadNullableInt(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private static double? ReadNullableDouble(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (double?)null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDateTime(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private static string ReadString(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static bool? ReadNullableBool(IDataRecord reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (bool?)null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private sealed class Totals
        {
            public decimal Qty { get; set; }
            public decimal Cost { get; set; }
        }

        private sealed class PostingAccounts
        {
            public string BranchAccount { get; set; }
            public string StoreAccount { get; set; }
            public string CostAccount { get; set; }
        }

        private sealed class DefinCompItemHeaderSnapshot
        {
            public int Id { get; set; }
            public string MaxNo { get; set; }
            public DateTime? RecordDate { get; set; }
            public int? BranchId { get; set; }
            public int? StoreId { get; set; }
            public int? StoreId2 { get; set; }
            public int? StoreId3 { get; set; }
            public int? ItemNameId { get; set; }
            public int? CusId { get; set; }
            public string ItemCode { get; set; }
            public int? TransactionId1 { get; set; }
            public int? TransactionId2 { get; set; }
            public int? TransactionId3 { get; set; }
            public int? TransactionId4 { get; set; }
            public int? TransactionId5 { get; set; }
            public int? TransactionId6 { get; set; }
            public string NoteSerial11 { get; set; }
            public string NoteSerial12 { get; set; }
            public string NoteSerial13 { get; set; }
            public string NoteSerial14 { get; set; }
            public string NoteSerial15 { get; set; }
            public string NoteSerial16 { get; set; }
        }
    }
}
