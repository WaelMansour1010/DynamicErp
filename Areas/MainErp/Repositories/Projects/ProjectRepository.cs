using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.Projects;

namespace MyERP.Areas.MainErp.Repositories.Projects
{
    public class ProjectRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ProjectRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<ProjectListItemViewModel> Search(string searchText, int? statusId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<ProjectListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
WITH ProjectRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY p.id DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        p.id, p.Fullcode, p.Project_name, p.Project_nameE,
        cust.CusName AS CustomerName,
        st.name AS StatusName,
        br.branch_name AS BranchName,
        p.project_cost, p.net, p.cost_after_discount, p.StartDate, p.EndDate
    FROM dbo.projects p
    LEFT JOIN dbo.TblCustemers cust ON cust.CusID = CASE WHEN ISNUMERIC(p.End_user_id) = 1 THEN CONVERT(int, p.End_user_id) END
    LEFT JOIN dbo.project_status st ON st.id = CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END
    LEFT JOIN dbo.TblBranchesData br ON br.branch_id = p.branch_no
    WHERE (@SearchText IS NULL
           OR p.Fullcode LIKE @SearchLike
           OR p.Code LIKE @SearchLike
           OR p.Project_name LIKE @SearchLike
           OR p.Project_nameE LIKE @SearchLike
           OR cust.CusName LIKE @SearchLike)
      AND (@StatusId IS NULL OR CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END = @StatusId)
      AND (@BranchId IS NULL OR p.branch_no = @BranchId)
)
SELECT * FROM ProjectRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
            {
                command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
                command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
                command.Parameters.AddWithValue("@StatusId", (object)statusId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
                command.Parameters.AddWithValue("@EndRow", page * pageSize);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                        {
                            result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                        }

                        result.Items.Add(new ProjectListItemViewModel
                        {
                            Id = ReadInt(reader, "id").GetValueOrDefault(),
                            FullCode = ReadString(reader, "Fullcode"),
                            ProjectName = ReadString(reader, "Project_name"),
                            ProjectNameEnglish = ReadString(reader, "Project_nameE"),
                            CustomerName = ReadString(reader, "CustomerName"),
                            StatusName = ReadString(reader, "StatusName"),
                            BranchName = ReadString(reader, "BranchName"),
                            ProjectCost = ReadDecimal(reader, "project_cost"),
                            NetCost = ReadDecimal(reader, "net") ?? ReadDecimal(reader, "cost_after_discount"),
                            StartDate = ReadDate(reader, "StartDate"),
                            EndDate = ReadDate(reader, "EndDate")
                        });
                    }
                }
            }

            return result;
        }

        public ProjectEditViewModel New()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var model = new ProjectEditViewModel
                {
                    IsNewProject = true,
                    Id = NextId(connection, null, "projects", "id"),
                    BranchNo = FirstInt(connection, "SELECT TOP 1 branch_id FROM dbo.TblBranchesData ORDER BY branch_id"),
                    StatusId = FirstInt(connection, "SELECT TOP 1 id FROM dbo.project_status ORDER BY id"),
                    CurrencyId = FirstInt(connection, "SELECT TOP 1 id FROM dbo.currency ORDER BY id")
                };
                PopulateLookups(connection, model);
                return model;
            }
        }

        public ProjectEditViewModel Get(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT TOP 1 *
FROM dbo.projects
WHERE id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var model = new ProjectEditViewModel
                    {
                        IsNewProject = false,
                        Id = ReadInt(reader, "id").GetValueOrDefault(),
                        Prefix = ReadString(reader, "prifix"),
                        Code = ReadString(reader, "Code"),
                        FullCode = ReadString(reader, "Fullcode"),
                        ProjectName = ReadString(reader, "Project_name"),
                        ProjectNameEnglish = ReadString(reader, "Project_nameE"),
                        EndUserId = ReadNullableIntFromString(reader, "End_user_id"),
                        EndUserAccount = ReadString(reader, "End_user_Account"),
                        SubContractorId = ReadNullableIntFromString(reader, "sub_contractor_id"),
                        SubContractorAccount = ReadString(reader, "sub_contractor_Account"),
                        BranchNo = ReadInt(reader, "branch_no"),
                        StatusId = ReadNullableIntFromString(reader, "Project_status"),
                        ContractType = ReadString(reader, "Contract_type"),
                        CurrencyId = ReadInt(reader, "CurrencyID"),
                        ProjectCost = ReadDecimal(reader, "project_cost"),
                        GeneralDiscount = ReadDecimal(reader, "general_discount"),
                        DiscountPercentage = ReadDecimal(reader, "DiscountPercentage"),
                        CostAfterDiscount = ReadDecimal(reader, "cost_after_discount"),
                        StartDate = ReadDate(reader, "StartDate"),
                        EndDate = ReadDate(reader, "EndDate"),
                        NearEndDate = ReadDate(reader, "DpNearEndDate"),
                        ManagerEmployeeId = ReadInt(reader, "EmpId"),
                        SalesEmployeeId = ReadInt(reader, "EmpId1"),
                        DepartmentId = ReadInt(reader, "Dept_ID"),
                        PState = ReadInt(reader, "Pstate"),
                        UnderImplementation = ReadInt(reader, "UnderImp"),
                        ContractNo = ReadString(reader, "ContractNo"),
                        Insurance = ReadDouble(reader, "Insurance"),
                        Remarks = ReadString(reader, "Remarkss"),
                        AccountUnderImp = ReadString(reader, "AccountUnderImp")
                    };

                    reader.Close();
                    model.ProjectItems = GetProjectItems(connection, id);
                    PopulateLookups(connection, model);
                    return model;
                }
            }
        }

        public int Save(ProjectEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (model.Id <= 0)
                {
                    model.Id = NextId(connection, transaction, "projects", "id");
                }

                model.Code = string.IsNullOrWhiteSpace(model.Code) ? model.Id.ToString() : model.Code.Trim();
                model.Prefix = string.IsNullOrWhiteSpace(model.Prefix) ? null : model.Prefix.Trim();
                model.FullCode = (model.Prefix ?? string.Empty) + model.Code;
                model.ProjectCost = model.ProjectCost ?? 0m;
                model.GeneralDiscount = model.GeneralDiscount ?? 0m;
                model.CostAfterDiscount = Math.Max(0m, model.ProjectCost.Value - model.GeneralDiscount.Value);

                if (model.UnderImplementation == 2 && string.IsNullOrWhiteSpace(model.AccountUnderImp))
                {
                    model.AccountUnderImp = GenerateProjectAccount(connection, transaction, model.ProjectName, model.ProjectNameEnglish);
                }

                EnsureUniqueCode(connection, transaction, model.Id, model.FullCode);

                var exists = Exists(connection, transaction, model.Id);
                var sql = exists ? UpdateSql : InsertSql;
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    AddParameters(command, model);
                    command.ExecuteNonQuery();
                }

                SaveProjectItems(connection, transaction, model.Id, model.ProjectItems);

                transaction.Commit();
                return model.Id;
            }
        }

        public ProjectExtractCreateViewModel BuildExtractCreateModel(int? projectId = null)
        {
            var model = new ProjectExtractCreateViewModel
            {
                Total = 0m,
                VatValue = 0m,
                NetValue = 0m,
                PerforValue = 0m,
                AdvancedPayment = 0m,
                GeneralDiscount = 0m
            };

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadLookup(connection, "SELECT id, Fullcode + N' - ' + Project_name FROM dbo.projects ORDER BY id DESC", model.Projects);
                
                if (projectId.HasValue && projectId.Value > 0)
                {
                    var project = GetProjectHeader(connection, null, projectId.Value);
                    if (project != null)
                    {
                        model.ProjectId = project.Id;
                        model.ProjectName = project.Name;
                        model.ProjectFullCode = project.FullCode;
                        model.BranchNo = project.BranchNo;
                        model.ExtractItems = GetProjectExtractItems(connection, project.Id);
                    }
                }
            }

            return model;
        }

        private static System.Collections.Generic.IList<ProjectExtractItemViewModel> GetProjectExtractItems(SqlConnection connection, int projectId)
        {
            var items = new System.Collections.Generic.List<ProjectExtractItemViewModel>();
            try
            {
                using (var command = new SqlCommand(@"
SELECT 
    m.ID AS PrMainDesID, 
    m.Name AS item,
    m.FullCode,
    ISNULL(m.Qty, 0) AS ContractQuantity, 
    ISNULL(m.Price, 0) AS Price,
    ISNULL(m.QtyExe, 0) AS PreviousQuantity,
    ISNULL(m.TotalExe, 0) AS PreviousValue
FROM dbo.ProjectMainDes m
WHERE m.ProjectID = @pId", connection))
                {
                    command.Parameters.AddWithValue("@pId", projectId);
                    using (var reader = command.ExecuteReader())
                    {
                        int idx = 1;
                        while (reader.Read())
                        {
                            items.Add(new ProjectExtractItemViewModel
                            {
                                Id = idx++,
                                PrMainDesID = ReadInt(reader, "PrMainDesID").GetValueOrDefault(),
                                Item = ReadString(reader, "item"),
                                FullCode = ReadString(reader, "FullCode"),
                                ContractQuantity = ReadDecimal(reader, "ContractQuantity").GetValueOrDefault(),
                                Price = ReadDecimal(reader, "Price").GetValueOrDefault(),
                                PreviousQuantity = ReadDecimal(reader, "PreviousQuantity").GetValueOrDefault(),
                                PreviousValue = ReadDecimal(reader, "PreviousValue").GetValueOrDefault(),
                                CurrentQuantity = 0m,
                                CurrentValue = 0m
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // جدول ProjectMainDes غير متوفر أو أعمدته مختلفة — نعيد قائمة فارغة بأمان
            }
            return items;
        }

        public int CreateExtract(ProjectExtractCreateViewModel model, int? userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var project = GetProjectHeader(connection, transaction, model.ProjectId.GetValueOrDefault());
                if (project == null)
                {
                    throw new InvalidOperationException("المشروع المحدد غير موجود.");
                }

                var id = NextId(connection, transaction, "project_billl", "id");
                var noteSerial = NextId(connection, transaction, "project_billl", "NoteSerial").ToString();
                var total = model.Total.GetValueOrDefault();
                var vat = model.VatValue.GetValueOrDefault();
                var net = model.NetValue.GetValueOrDefault();
                if (net == 0m)
                {
                    net = total + vat;
                }

                using (var command = new SqlCommand(@"
INSERT INTO dbo.project_billl
(
    id, bill_date, project_no, project_name, total, FATValue, NetValue,
    Branch_NO, ManualNO, NoteSerial, Results, UserID, Remarks, StartDateProje,
    advancedPayment, PerformanceBond, PerforValue, discount
)
VALUES
(
    @Id, @BillDate, @ProjectNo, @ProjectName, @Total, @VatValue, @NetValue,
    @BranchNo, @ManualNo, @NoteSerial, 0, @UserId, @Remarks, @StartDate,
    @AdvancedPayment, @PerformanceBond, @PerforValue, @Discount
);", connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@BillDate", (object)model.BillDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProjectNo", project.Id.ToString());
                    command.Parameters.AddWithValue("@ProjectName", (object)project.Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Total", total);
                    command.Parameters.AddWithValue("@VatValue", vat);
                    command.Parameters.AddWithValue("@NetValue", net);
                    command.Parameters.AddWithValue("@BranchNo", (object)(model.BranchNo ?? project.BranchNo) ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ManualNo", (object)model.ManualNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                    command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@StartDate", (object)project.StartDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@AdvancedPayment", model.AdvancedPayment ?? 0m);
                    command.Parameters.AddWithValue("@PerformanceBond", model.PerforValue ?? 0m);
                    command.Parameters.AddWithValue("@PerforValue", model.PerforValue ?? 0m);
                    command.Parameters.AddWithValue("@Discount", model.GeneralDiscount ?? 0m);
                    command.ExecuteNonQuery();
                }

                // Save detail lines and update executed quantities / values
                if (model.ExtractItems != null && model.ExtractItems.Count > 0)
                {
                    int lineNo = 1;
                    foreach (var item in model.ExtractItems)
                    {
                        if (item.CurrentQuantity <= 0m) continue;

                        var itemCost = item.ContractQuantity * item.Price;
                        var prevQty = item.PreviousQuantity;
                        var prevVal = item.PreviousValue;
                        var currQty = item.CurrentQuantity;
                        var currVal = item.CurrentValue;
                        var totQty = prevQty + currQty;
                        var totVal = prevVal + currVal;

                        var currPercent = item.ContractQuantity > 0m ? (currQty / item.ContractQuantity) * 100m : 0m;
                        var totPercent = item.ContractQuantity > 0m ? (totQty / item.ContractQuantity) * 100m : 0m;

                        // Calculate proportional share based on line current value
                        var ratio = total > 0m ? currVal / total : 0m;
                        var lineDiscount = (model.GeneralDiscount ?? 0m) * ratio;
                        var lineNetAfterMainDiscountBeforeVat = currVal - lineDiscount;
                        var lineVat = (model.VatValue ?? 0m) * ratio;
                        var lineNetAfterMainDiscountWithVat = lineNetAfterMainDiscountBeforeVat + lineVat;
                        var perforVLineDiscount = (model.PerforValue ?? 0m) * ratio;
                        var lineFinal = (model.NetValue ?? 0m) * ratio;

                        using (var command = new SqlCommand(@"
INSERT INTO dbo.project_bill_details
(
    bill_id, project_no, project_id, projectName, FullCode, item,
    Quantity, Price, cost, Pre_Quantity, Pre_Value, Curr_Quantity, Curr_value,
    tot_quantity, tot_value, curr_Percent, tot_percent, line_no,
    LineDiscount, linenetaftermainDiscountBeforevat, LineVat, linenetaftermainDiscountWithvat,
    PerforVLineDiscount, LineFinal, AccountCode
)
VALUES
(
    @BillId, @ProjectNo, @ProjectId, @ProjectName, @FullCode, @Item,
    @Quantity, @Price, @Cost, @PreQuantity, @PreValue, @CurrQuantity, @CurrValue,
    @TotQuantity, @TotValue, @CurrPercent, @TotPercent, @LineNo,
    @LineDiscount, @LineNetAfterMainDiscountBeforeVat, @LineVat, @LineNetAfterMainDiscountWithVat,
    @PerforVLineDiscount, @LineFinal, @AccountCode
);", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BillId", id);
                            command.Parameters.AddWithValue("@ProjectNo", project.Id.ToString());
                            command.Parameters.AddWithValue("@ProjectId", project.Id);
                            command.Parameters.AddWithValue("@ProjectName", (object)project.Name ?? DBNull.Value);
                            command.Parameters.AddWithValue("@FullCode", (object)item.FullCode ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Item", (object)item.Item ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Quantity", item.ContractQuantity);
                            command.Parameters.AddWithValue("@Price", item.Price);
                            command.Parameters.AddWithValue("@Cost", itemCost);
                            command.Parameters.AddWithValue("@PreQuantity", prevQty);
                            command.Parameters.AddWithValue("@PreValue", prevVal);
                            command.Parameters.AddWithValue("@CurrQuantity", currQty);
                            command.Parameters.AddWithValue("@CurrValue", currVal);
                            command.Parameters.AddWithValue("@TotQuantity", totQty);
                            command.Parameters.AddWithValue("@TotValue", totVal);
                            command.Parameters.AddWithValue("@CurrPercent", currPercent);
                            command.Parameters.AddWithValue("@TotPercent", totPercent);
                            command.Parameters.AddWithValue("@LineNo", lineNo++);
                            command.Parameters.AddWithValue("@LineDiscount", lineDiscount);
                            command.Parameters.AddWithValue("@LineNetAfterMainDiscountBeforeVat", lineNetAfterMainDiscountBeforeVat);
                            command.Parameters.AddWithValue("@LineVat", lineVat);
                            command.Parameters.AddWithValue("@LineNetAfterMainDiscountWithVat", lineNetAfterMainDiscountWithVat);
                            command.Parameters.AddWithValue("@PerforVLineDiscount", perforVLineDiscount);
                            command.Parameters.AddWithValue("@LineFinal", lineFinal);
                            command.Parameters.AddWithValue("@AccountCode", (object)project.AccountUnderImp ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }

                        // Update executed quantities / values in dbo.ProjectMainDes
                        using (var command = new SqlCommand(@"
UPDATE dbo.ProjectMainDes
SET QtyExe = ISNULL(QtyExe, 0) + @CurrQuantity,
    TotalExe = ISNULL(TotalExe, 0) + @CurrValue
WHERE ID = @PrMainDesID;", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@CurrQuantity", currQty);
                            command.Parameters.AddWithValue("@CurrValue", currVal);
                            command.Parameters.AddWithValue("@PrMainDesID", item.PrMainDesID);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // ------------------ DOUBLE-ENTRY ACCOUNTING POSTING ENGINE ------------------
                
                // 1. Query Project & Customer Accounts
                int? underImp = null;
                string accountUnderImp = null;
                string revenueAccount = null;
                string endUserId = null;
                string endUserAccount = null;

                using (var command = new SqlCommand(@"
SELECT TOP 1 UnderImp, AccountUnderImp, REVENUE_account, End_user_id, End_user_Account 
FROM dbo.projects 
WHERE id = @ProjectId", connection, transaction))
                {
                    command.Parameters.AddWithValue("@ProjectId", project.Id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            underImp = reader.IsDBNull(0) ? (int?)null : Convert.ToInt32(reader.GetValue(0));
                            accountUnderImp = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1));
                            revenueAccount = reader.IsDBNull(2) ? null : Convert.ToString(reader.GetValue(2));
                            endUserId = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3));
                            endUserAccount = reader.IsDBNull(4) ? null : Convert.ToString(reader.GetValue(4));
                        }
                    }
                }

                string customerAccount = null;
                string customerAccountHi1 = null;
                string customerAccountAss2 = null;
                string customerAccountVat = null;

                if (!string.IsNullOrWhiteSpace(endUserId) && int.TryParse(endUserId, out int cusId))
                {
                    using (var command = new SqlCommand(@"
SELECT TOP 1 Account_Code, Account_CodeHi1, Account_CodeAss2, Account_VAT 
FROM dbo.TblCustemers 
WHERE CusID = @CusId", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@CusId", cusId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customerAccount = reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0));
                                customerAccountHi1 = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1));
                                customerAccountAss2 = reader.IsDBNull(2) ? null : Convert.ToString(reader.GetValue(2));
                                customerAccountVat = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3));
                            }
                        }
                    }
                }

                // 2. Resolve Accounts & Apply Fallbacks (Validation is handled within ResolveLeafAccount)
                string debCustomerAccount = underImp == 2 && !string.IsNullOrWhiteSpace(accountUnderImp) ? accountUnderImp : (!string.IsNullOrWhiteSpace(customerAccount) ? customerAccount : endUserAccount);
                debCustomerAccount = ResolveLeafAccount(connection, transaction, debCustomerAccount, null, null);
                string debGuaranteeAccount = ResolveLeafAccount(connection, transaction, customerAccountAss2, null, null);
                string debAdvancedAccount = ResolveLeafAccount(connection, transaction, customerAccountHi1, null, null);
                string debDiscountAccount = ResolveLeafAccount(connection, transaction, null, "خصم", "Discount");

                string credRevenueAccount = ResolveLeafAccount(connection, transaction, revenueAccount, "إيراد", "Revenue");
                string credVatAccount = ResolveLeafAccount(connection, transaction, customerAccountVat, "ضريب", "VAT");

                // 3. Clean Deletion of any existing ledger records (to prevent duplicates on re-save/edit)
                using (var deleteCmd = new SqlCommand(@"
DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE bill_id = @BillId AND project_id = @ProjectId;
DELETE FROM dbo.Notes WHERE Transaction_ID = @BillId AND NoteType = 5000;", connection, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@BillId", id);
                    deleteCmd.Parameters.AddWithValue("@ProjectId", project.Id);
                    deleteCmd.ExecuteNonQuery();
                }

                // 4. Generate Thread-Safe Manual Serial IDs using AppLock / transaction lock (NextId contains UPDLOCK, HOLDLOCK)
                var noteId = NextId(connection, transaction, "Notes", "NoteID");
                var voucherId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
                double noteSerialValue = (double)noteId;

                // 5. Insert Accounting Note Header (NoteType = 5000)
                using (var command = new SqlCommand(@"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Transaction_ID, UserID, Remark, NotePosted,
    PostedBy, PostDate, ORDER_NO, ManualNo, branch_no, Double_Entry_Vouchers_ID,
    TotalValue, Account_DebitSide, Account_CreditSide, DateRec, project_id
)
VALUES
(
    @NoteId, @NoteDate, 5000, @NoteSerial, @NoteSerial1, @NoteValue, @TransactionId, @UserId, @Remark, 0,
    @UserId, GETDATE(), @OrderNo, @ManualNo, @BranchNo, @VoucherId,
    @TotalValue, @DebitSide, @CreditSide, GETDATE(), @ProjectId
);", connection, transaction))
                {
                    command.Parameters.AddWithValue("@NoteId", noteId);
                    command.Parameters.AddWithValue("@NoteDate", model.BillDate.HasValue ? (object)model.BillDate.Value : DateTime.Now);
                    command.Parameters.AddWithValue("@NoteSerial", noteSerialValue);
                    command.Parameters.AddWithValue("@NoteSerial1", noteSerialValue);
                    command.Parameters.AddWithValue("@NoteValue", (double)net);
                    command.Parameters.AddWithValue("@TransactionId", id);
                    command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Remark", (object)model.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@OrderNo", project.FullCode);
                    command.Parameters.AddWithValue("@ManualNo", (object)model.ManualNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BranchNo", (object)(model.BranchNo ?? project.BranchNo) ?? DBNull.Value);
                    command.Parameters.AddWithValue("@VoucherId", voucherId);
                    command.Parameters.AddWithValue("@TotalValue", (double)net);
                    command.Parameters.AddWithValue("@DebitSide", (object)debCustomerAccount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CreditSide", (object)credRevenueAccount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProjectId", project.Id);
                    command.ExecuteNonQuery();
                }

                // 6. Insert Balanced Voucher Details (DEBITS / CREDITS)
                int devLineNo = 1;
                Action<string, decimal, int, string> addVoucherLine = (accountCode, val, creditOrDebit, desc) =>
                {
                    if (val <= 0m) return;
                    using (var cmd = new SqlCommand(@"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value, Credit_Or_Debit, 
    Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Posted, 
    branch_id, project_bill_no, project_id, bill_id
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value, @CreditOrDebit, 
    @Description, @RecordDate, @NoteId, @UserId, 0, 
    @BranchId, @ProjectBillNo, @ProjectId, @BillId
);", connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@VoucherId", voucherId);
                        cmd.Parameters.AddWithValue("@LineNo", devLineNo++);
                        cmd.Parameters.AddWithValue("@AccountCode", accountCode);
                        cmd.Parameters.AddWithValue("@Value", (double)val);
                        cmd.Parameters.AddWithValue("@CreditOrDebit", creditOrDebit);
                        cmd.Parameters.AddWithValue("@Description", (object)desc ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RecordDate", model.BillDate.HasValue ? (object)model.BillDate.Value : DateTime.Now);
                        cmd.Parameters.AddWithValue("@NoteId", noteId);
                        cmd.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BranchId", (object)(model.BranchNo ?? project.BranchNo) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProjectBillNo", id.ToString());
                        cmd.Parameters.AddWithValue("@ProjectId", project.Id);
                        cmd.Parameters.AddWithValue("@BillId", id);
                        cmd.ExecuteNonQuery();
                    }
                };

                // DEBITS (0)
                addVoucherLine(debCustomerAccount, net, 0, "مستخلص رقم " + id + " للمشروع: " + project.Name);
                addVoucherLine(debGuaranteeAccount, model.PerforValue.GetValueOrDefault(), 0, "ضمان مستخلص رقم " + id + " للمشروع: " + project.Name);
                addVoucherLine(debAdvancedAccount, model.AdvancedPayment.GetValueOrDefault(), 0, "دفعة مقدمة مستردة مستخلص رقم " + id + " للمشروع: " + project.Name);
                addVoucherLine(debDiscountAccount, model.GeneralDiscount.GetValueOrDefault(), 0, "خصم مستخلص رقم " + id + " للمشروع: " + project.Name);

                // CREDITS (1)
                addVoucherLine(credRevenueAccount, total, 1, "إيراد مستخلص رقم " + id + " للمشروع: " + project.Name);
                addVoucherLine(credVatAccount, model.VatValue.GetValueOrDefault(), 1, "ضريبة مستخلص رقم " + id + " للمشروع: " + project.Name);

                // 7. Update Bill Header Linkage to Accounting Note
                using (var updateBillCmd = new SqlCommand(@"
UPDATE dbo.project_billl
SET note_id = @NoteId,
    NoteSerial = @NoteSerial,
    NoteSerial1 = @NoteSerial1
WHERE id = @Id;", connection, transaction))
                {
                    updateBillCmd.Parameters.AddWithValue("@NoteId", noteId);
                    updateBillCmd.Parameters.AddWithValue("@NoteSerial", noteSerialValue.ToString());
                    updateBillCmd.Parameters.AddWithValue("@NoteSerial1", noteSerialValue.ToString());
                    updateBillCmd.Parameters.AddWithValue("@Id", id);
                    updateBillCmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return id;
            }
        }

        public void PopulateLookups(ProjectEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                PopulateLookups(connection, model);
            }
        }

        public void PopulateIndexLookups(ProjectsIndexViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadLookup(connection, "SELECT id, name FROM dbo.project_status ORDER BY id", model.Statuses);
                LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id", model.Branches);
            }
        }

        private void PopulateLookups(SqlConnection connection, ProjectEditViewModel model)
        {
            LoadLookup(connection, "SELECT id, name FROM dbo.project_status ORDER BY id", model.Statuses);
            LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id", model.Branches);
            LoadLookup(connection, "SELECT TOP 200 id, COALESCE(name, nameE, CONVERT(nvarchar(20), id)) FROM dbo.currency ORDER BY id", model.Currencies);
            LoadLookup(connection, "SELECT TOP 200 id, COALESCE(name, namee, CONVERT(nvarchar(20), id)) FROM dbo.contract_type ORDER BY id", model.ContractTypes);
            LoadLookup(connection, "SELECT TOP 300 CusID, COALESCE(CusName, CusNamee, CONVERT(nvarchar(20), CusID)) FROM dbo.TblCustemers ORDER BY CusName", model.Customers);
            LoadLookup(connection, "SELECT TOP 300 Account_Code, COALESCE(Account_Serial + N' - ', N'') + COALESCE(Account_Name, Account_NameEng, Account_Code) FROM dbo.ACCOUNTS WHERE ISNULL(last_account, 0) = 1 ORDER BY Account_Serial", model.Accounts);
            LoadLookup(connection, "SELECT TOP 300 Emp_ID, COALESCE(Emp_Name, Emp_Namee, CONVERT(nvarchar(20), Emp_ID)) FROM dbo.TblEmployee ORDER BY Emp_Name", model.Employees);
        }

        private static void LoadLookup(SqlConnection connection, string sql, System.Collections.Generic.IList<ProjectLookupItem> target)
        {
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    target.Add(new ProjectLookupItem
                    {
                        Value = Convert.ToString(reader.GetValue(0)),
                        Text = reader.IsDBNull(1) ? Convert.ToString(reader.GetValue(0)) : Convert.ToString(reader.GetValue(1))
                    });
                }
            }
        }

        private static bool Exists(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.projects WHERE id = @Id", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string table, string column)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(CASE WHEN ISNUMERIC(" + column + ") = 1 THEN CONVERT(int, " + column + ") END), 0) + 1 FROM dbo." + table + " WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int? FirstInt(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static void EnsureUniqueCode(SqlConnection connection, SqlTransaction transaction, int id, string fullCode)
        {
            if (string.IsNullOrWhiteSpace(fullCode))
            {
                throw new InvalidOperationException("كود المشروع مطلوب.");
            }

            using (var command = new SqlCommand("SELECT TOP 1 Project_name FROM dbo.projects WHERE id <> @Id AND Fullcode = @FullCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@FullCode", fullCode);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    throw new InvalidOperationException("لا يمكن تكرار كود المشروع. الكود مستخدم في مشروع: " + Convert.ToString(existing));
                }
            }
        }

        private static ProjectHeader GetProjectHeader(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand("SELECT TOP 1 id, Project_name, Fullcode, branch_no, StartDate, AccountUnderImp FROM dbo.projects WHERE id = @Id", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new ProjectHeader
                    {
                        Id = ReadInt(reader, "id").GetValueOrDefault(),
                        Name = ReadString(reader, "Project_name"),
                        FullCode = ReadString(reader, "Fullcode"),
                        BranchNo = ReadInt(reader, "branch_no"),
                        StartDate = ReadDate(reader, "StartDate"),
                        AccountUnderImp = ReadString(reader, "AccountUnderImp")
                    };
                }
            }
        }

        private static string GenerateProjectAccount(SqlConnection connection, SqlTransaction transaction, string name, string nameEng)
        {
            var accountCode = NextId(connection, transaction, "ACCOUNTS", "Account_Code").ToString();
            var serial = NextId(connection, transaction, "ACCOUNTS", "Account_Serial").ToString();
            
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ACCOUNTS
(Account_Code, Account_Name, last_account, cannot_del, Branch, Account_Serial,
 BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna, cost_center, Sum_account,
 cost_center_type, AccountTypes, AccountTab, DepitOrCredit, Differenttype, [Block], [Level])
VALUES
(@Code, @Name, 1, 0, '0', @Serial,
 0, GETDATE(), @NameEng, N'1', 0, 0, 0,
 0, 1, 1, 0, 1, 0, 1);", connection, transaction))
            {
                command.Parameters.AddWithValue("@Code", accountCode);
                command.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? "مشروع جديد - تحت التنفيذ" : name + " - تحت التنفيذ");
                command.Parameters.AddWithValue("@NameEng", string.IsNullOrWhiteSpace(nameEng) ? "New Project - Under Implementation" : nameEng + " - Under implementation");
                command.Parameters.AddWithValue("@Serial", serial);
                command.ExecuteNonQuery();
            }
            return accountCode;
        }

        private static System.Collections.Generic.IList<ProjectMainDesViewModel> GetProjectItems(SqlConnection connection, int projectId)
        {
            var items = new System.Collections.Generic.List<ProjectMainDesViewModel>();
            try
            {
                using (var command = new SqlCommand("SELECT ID, ProjectID, Name, Price, Qty, Total FROM dbo.ProjectMainDes WHERE ProjectID = @pId", connection))
                {
                    command.Parameters.AddWithValue("@pId", projectId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new ProjectMainDesViewModel
                            {
                                Id = ReadInt(reader, "ID").GetValueOrDefault(),
                                ProjectId = ReadInt(reader, "ProjectID").GetValueOrDefault(),
                                Item = ReadString(reader, "Name"),
                                Price = ReadDecimal(reader, "Price").GetValueOrDefault(),
                                Quantity = ReadDecimal(reader, "Qty").GetValueOrDefault(),
                                Value = ReadDecimal(reader, "Total").GetValueOrDefault()
                            });
                        }
                    }
                }
            }
            catch (SqlException)
            {
                // جدول ProjectMainDes غير متوفر أو أعمدته مختلفة — نعيد قائمة فارغة بأمان
            }
            return items;
        }

        private static void SaveProjectItems(SqlConnection connection, SqlTransaction transaction, int projectId, System.Collections.Generic.IList<ProjectMainDesViewModel> items)
        {
            try
            {
                using (var command = new SqlCommand("DELETE FROM dbo.ProjectMainDes WHERE ProjectID = @pId", connection, transaction))
                {
                    command.Parameters.AddWithValue("@pId", projectId);
                    command.ExecuteNonQuery();
                }

                if (items == null) return;

                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Item)) continue;
                    using (var command = new SqlCommand("INSERT INTO dbo.ProjectMainDes (ProjectID, Name, Price, Qty, Total) VALUES (@pId, @name, @price, @qty, @total)", connection, transaction))
                    {
                        command.Parameters.AddWithValue("@pId", projectId);
                        command.Parameters.AddWithValue("@name", (object)item.Item ?? DBNull.Value);
                        command.Parameters.AddWithValue("@price", item.Price);
                        command.Parameters.AddWithValue("@qty", item.Quantity);
                        command.Parameters.AddWithValue("@total", item.Value);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("حدث خطأ أثناء حفظ بنود المشروع: " + ex.Message, ex);
            }
        }

        private static void AddParameters(SqlCommand command, ProjectEditViewModel model)
        {
            command.Parameters.AddWithValue("@Id", model.Id);
            command.Parameters.AddWithValue("@EndUserAccount", (object)model.EndUserAccount ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndUserName", DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorAccount", (object)model.SubContractorAccount ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorName", DBNull.Value);
            command.Parameters.AddWithValue("@FullCode", (object)model.FullCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Prefix", (object)model.Prefix ?? DBNull.Value);
            command.Parameters.AddWithValue("@Code", (object)model.Code ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectName", (object)model.ProjectName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectNameEnglish", (object)model.ProjectNameEnglish ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContractType", (object)model.ContractType ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectStatus", (object)model.StatusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectCost", (object)model.ProjectCost ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchNo", (object)model.BranchNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndUserId", (object)model.EndUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorId", (object)model.SubContractorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GeneralDiscount", (object)model.GeneralDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@CostAfterDiscount", (object)model.CostAfterDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@Net", (object)model.CostAfterDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrencyId", (object)model.CurrencyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object)model.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object)model.EndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DiscountPercentage", (object)model.DiscountPercentage ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeptId", (object)model.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);
            command.Parameters.AddWithValue("@NearEndDate", (object)model.NearEndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmpId", (object)model.ManagerEmployeeId ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmpId1", (object)model.SalesEmployeeId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PState", (object)model.PState ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContractNo", (object)model.ContractNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnderImp", (object)model.UnderImplementation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Insurance", (object)model.Insurance ?? DBNull.Value);
            command.Parameters.AddWithValue("@AccountUnderImp", (object)model.AccountUnderImp ?? DBNull.Value);
        }

        private const string InsertSql = @"
INSERT INTO dbo.projects
(
    id, End_user_Account, End_user_name, sub_contractor_Account, sub_contractor_name,
    Fullcode, prifix, Code, Project_name, Project_nameE, Contract_type, Project_status,
    project_cost, branch_no, End_user_id, sub_contractor_id, general_discount,
    cost_after_discount, net, CurrencyID, StartDate, EndDate, DiscountPercentage,
    Dept_ID, Remarkss, DpNearEndDate, EmpId, EmpId1, Pstate, ContractNo, UnderImp, Insurance, AccountUnderImp
)
VALUES
(
    @Id, @EndUserAccount, @EndUserName, @SubContractorAccount, @SubContractorName,
    @FullCode, @Prefix, @Code, @ProjectName, @ProjectNameEnglish, @ContractType, @ProjectStatus,
    @ProjectCost, @BranchNo, @EndUserId, @SubContractorId, @GeneralDiscount,
    @CostAfterDiscount, @Net, @CurrencyId, @StartDate, @EndDate, @DiscountPercentage,
    @DeptId, @Remarks, @NearEndDate, @EmpId, @EmpId1, @PState, @ContractNo, @UnderImp, @Insurance, @AccountUnderImp
);";

        private const string UpdateSql = @"
UPDATE dbo.projects SET
    End_user_Account = @EndUserAccount,
    End_user_name = @EndUserName,
    sub_contractor_Account = @SubContractorAccount,
    sub_contractor_name = @SubContractorName,
    Fullcode = @FullCode,
    prifix = @Prefix,
    Code = @Code,
    Project_name = @ProjectName,
    Project_nameE = @ProjectNameEnglish,
    Contract_type = @ContractType,
    Project_status = @ProjectStatus,
    project_cost = @ProjectCost,
    branch_no = @BranchNo,
    End_user_id = @EndUserId,
    sub_contractor_id = @SubContractorId,
    general_discount = @GeneralDiscount,
    cost_after_discount = @CostAfterDiscount,
    net = @Net,
    CurrencyID = @CurrencyId,
    StartDate = @StartDate,
    EndDate = @EndDate,
    DiscountPercentage = @DiscountPercentage,
    Dept_ID = @DeptId,
    Remarkss = @Remarks,
    DpNearEndDate = @NearEndDate,
    EmpId = @EmpId,
    EmpId1 = @EmpId1,
    Pstate = @PState,
    ContractNo = @ContractNo,
    UnderImp = @UnderImp,
    Insurance = @Insurance,
    AccountUnderImp = @AccountUnderImp
WHERE id = @Id;";

        private static string ReadString(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int? ReadInt(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? ReadNullableIntFromString(IDataRecord reader, string column)
        {
            int value;
            var text = ReadString(reader, column);
            return int.TryParse(text, out value) ? value : (int?)null;
        }

        private static DateTime? ReadDate(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static decimal? ReadDecimal(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static double? ReadDouble(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (double?)null : Convert.ToDouble(reader.GetValue(ordinal));
        }

        private static string ResolveLeafAccount(SqlConnection connection, SqlTransaction transaction, string accountCode, string fallbackSearchTerm, string fallbackSearchTermEng)
        {
            if (!string.IsNullOrWhiteSpace(accountCode))
            {
                using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Code = @Code AND ISNULL(last_account, 0) = 1", connection, transaction))
                {
                    command.Parameters.AddWithValue("@Code", accountCode.Trim());
                    if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                    {
                        return accountCode.Trim();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(fallbackSearchTerm))
            {
                using (var command = new SqlCommand("SELECT TOP 1 Account_Code FROM dbo.ACCOUNTS WHERE ISNULL(last_account, 0) = 1 AND (Account_Name LIKE @Term OR Account_NameEng LIKE @TermEng)", connection, transaction))
                {
                    command.Parameters.AddWithValue("@Term", "%" + fallbackSearchTerm + "%");
                    command.Parameters.AddWithValue("@TermEng", "%" + fallbackSearchTermEng + "%");
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToString(result);
                    }
                }
            }

            using (var command = new SqlCommand("SELECT TOP 1 Account_Code FROM dbo.ACCOUNTS WHERE ISNULL(last_account, 0) = 1 ORDER BY Account_Serial", connection, transaction))
            {
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToString(result);
                }
            }

            return accountCode;
        }

        private class ProjectHeader
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string FullCode { get; set; }
            public int? BranchNo { get; set; }
            public DateTime? StartDate { get; set; }
            public string AccountUnderImp { get; set; }
        }
    }
}
