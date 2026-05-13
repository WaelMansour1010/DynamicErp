using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.LegacyOperations;

namespace MyERP.Areas.MainErp.Repositories.LegacyOperations
{
    public class LegacyOperationsRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public LegacyOperationsRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public LegacyOperationsIndexViewModel LoadIndex()
        {
            return new LegacyOperationsIndexViewModel
            {
                Branches = Lookup("SELECT branch_id, branch_name, NULL AccountCode FROM dbo.TblBranchesData ORDER BY branch_id", "branch_id", "branch_name", "AccountCode"),
                Boxes = Lookup("SELECT BoxID, BoxName, Account_Code FROM dbo.TblBoxesData ORDER BY BoxName", "BoxID", "BoxName", "Account_Code"),
                Cashiers = Lookup("SELECT c.EmpID, e.Emp_Name, NULL AccountCode FROM dbo.cachierData c INNER JOIN dbo.TblEmployee e ON e.Emp_ID = c.EmpID WHERE ISNULL(c.Ctype,0)=0 ORDER BY e.Emp_Name", "EmpID", "Emp_Name", "AccountCode"),
                Accounts = Lookup("SELECT TOP 800 Account_Code, Account_Name, Account_Code AccountCode FROM dbo.ACCOUNTS WHERE ISNULL(last_account,0)=0 OR Account_Code = N'r' ORDER BY Account_Code", "Account_Code", "Account_Name", "AccountCode"),
                Customers = Lookup("SELECT TOP 500 CusID, CusName, Account_Code FROM dbo.TblCustemers WHERE ISNULL(Type,0)=1 ORDER BY CusName", "CusID", "CusName", "Account_Code"),
                CarTypes = Lookup("SELECT id, name, NULL AccountCode FROM dbo.TBLCarTypes ORDER BY name", "id", "name", "AccountCode"),
                CarModels = Lookup("SELECT Id, Model, NULL AccountCode FROM dbo.TblCarModels ORDER BY Model", "Id", "Model", "AccountCode"),
                Colors = Lookup("SELECT Id, name, NULL AccountCode FROM dbo.TblColor ORDER BY name", "Id", "name", "AccountCode"),
                MaintenanceWorks = Lookup("SELECT Id, name, NULL AccountCode FROM dbo.TblMaintenanceWork ORDER BY name", "Id", "name", "AccountCode"),
                ExtraExpenses = Lookup("SELECT Id, name, NULL AccountCode FROM dbo.TblExtraExpeneses ORDER BY name", "Id", "name", "AccountCode"),
                PaymentTypes = Lookup("SELECT PaymentID, PaymentName, Account_Code = ISNULL(Accountsus, N'') FROM dbo.TblPaymentType ORDER BY PaymentID", "PaymentID", "PaymentName", "Account_Code"),
                Employees = Lookup("SELECT TOP 1000 Emp_ID, Emp_Name, NULL AccountCode FROM dbo.TblEmployee ORDER BY Emp_Name", "Emp_ID", "Emp_Name", "AccountCode"),
                Departments = Lookup("SELECT DeparmentID, DepartmentName, NULL AccountCode FROM dbo.TblEmpDepartments ORDER BY DepartmentName", "DeparmentID", "DepartmentName", "AccountCode"),
                Items = Lookup("SELECT TOP 1000 ItemID, ItemName, NULL AccountCode FROM dbo.tblItems ORDER BY ItemName", "ItemID", "ItemName", "AccountCode"),
                Cars = Lookup("SELECT TOP 1000 id, COALESCE(NULLIF(BoardNO,N''), NULLIF(Name,N''), Fullcode) DisplayName, NULL AccountCode FROM dbo.TblCarsData ORDER BY id DESC", "id", "DisplayName", "AccountCode"),
                FixedAssets = Lookup("SELECT TOP 1000 id, COALESCE(NULLIF(Name,N''), NULLIF(namee,N''), code) DisplayName, code AccountCode FROM dbo.FixedAssets ORDER BY id DESC", "id", "DisplayName", "AccountCode")
            };
        }

        public IList<CashBoxListItem> SearchBoxes(string search)
        {
            var rows = new List<CashBoxListItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 b.BoxID, b.BoxName, br.branch_name, b.Account_Code, b.OpenBalance
FROM dbo.TblBoxesData b
LEFT JOIN dbo.TblBranchesData br ON br.branch_id = b.BranchId
WHERE @Search IS NULL OR b.BoxName LIKE @Like OR CONVERT(nvarchar(20), b.BoxID)=@Search
ORDER BY b.BoxID DESC;";
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), 100);
                AddNullable(command, "@Like", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new CashBoxListItem
                        {
                            Id = ReadInt(reader, "BoxID"),
                            Name = ReadString(reader, "BoxName"),
                            BranchName = ReadString(reader, "branch_name"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            OpenBalance = ReadDecimal(reader, "OpenBalance")
                        });
                    }
                }
            }

            return rows;
        }

        public CashBoxDetails GetBox(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 * FROM dbo.TblBoxesData WHERE BoxID=@Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new CashBoxDetails
                    {
                        Id = ReadInt(reader, "BoxID"),
                        Name = ReadString(reader, "BoxName"),
                        EnglishName = ReadString(reader, "BoxNameE"),
                        Remarks = ReadString(reader, "Comments"),
                        BranchId = ReadNullableInt(reader, "BranchId"),
                        Type = ReadNullableInt(reader, "Type") ?? 0,
                        EmployeeId = ReadNullableInt(reader, "empid"),
                        ParentAccountCode = ReadString(reader, "parent_account"),
                        AccountCode = ReadString(reader, "Account_Code"),
                        ChequeBox = ReadBool(reader, "ChequeBox"),
                        Period = ReadNullableInt(reader, "Priod"),
                        PeriodType = ReadNullableInt(reader, "PriodDMY"),
                        BoxValue = ReadDecimal(reader, "boxValue"),
                        OpenBalanceDate = ReadDate(reader, "OpenBalanceDate"),
                        OpenBalanceType = ReadNullableInt(reader, "OpenBalanceType"),
                        OpenBalance = ReadDecimal(reader, "OpenBalance")
                    };
                }
            }
        }

        public LegacySaveResult SaveBox(CashBoxDetails request)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (DuplicateBoxName(connection, transaction, request.Name, request.Id))
                    {
                        return Fail("هناك خزنة مسجلة مسبقا بهذا الاسم.");
                    }

                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "TblBoxesData", "BoxID", null) : request.Id.Value;
                    var accountCode = string.IsNullOrWhiteSpace(request.AccountCode)
                        ? CreateAccount(connection, transaction, request.ParentAccountCode, request.Name, request.EnglishName)
                        : request.AccountCode.Trim();

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = isNew
                            ? @"INSERT INTO dbo.TblBoxesData
(BoxID, BoxName, BoxNameE, Comments, Account_Code, Type, empid, BranchId, ParentAccount, parent_account, ChequeBox, OpenBalanceDate, OpenBalanceType, OpenBalance, BTtype, boxValue, Priod, PriodDMY)
VALUES
(@Id, @Name, @NameE, @Remarks, @AccountCode, @Type, @EmpId, @BranchId, @ParentAccount, @ParentAccount, @ChequeBox, @OpenBalanceDate, @OpenBalanceType, @OpenBalance, 0, @BoxValue, @Period, @PeriodType);"
                            : @"UPDATE dbo.TblBoxesData
SET BoxName=@Name, BoxNameE=@NameE, Comments=@Remarks, Type=@Type, empid=@EmpId, BranchId=@BranchId,
    ParentAccount=@ParentAccount, parent_account=@ParentAccount, ChequeBox=@ChequeBox, OpenBalanceDate=@OpenBalanceDate,
    OpenBalanceType=@OpenBalanceType, OpenBalance=@OpenBalance, boxValue=@BoxValue, Priod=@Period, PriodDMY=@PeriodType
WHERE BoxID=@Id;";
                        BoxParameters(command, request, id, accountCode);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Ok("تم حفظ بيانات الخزنة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ الخزنة: " + ex.Message);
                }
            }
        }

        public LegacySaveResult DeleteBox(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var accountCode = ScalarString(connection, transaction, "SELECT Account_Code FROM dbo.TblBoxesData WHERE BoxID=@Id", P("@Id", SqlDbType.Int, id));
                    if (!string.IsNullOrWhiteSpace(accountCode))
                    {
                        var useCount = ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Account_Code=@Code", P("@Code", SqlDbType.NVarChar, accountCode, 100));
                        if (useCount > 0) return Fail("لا يمكن حذف الخزنة لوجود حركات مرتبطة بحسابها.");
                    }

                    Execute(connection, transaction, "DELETE FROM dbo.TblBoxesData WHERE BoxID=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف الخزنة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف الخزنة: " + ex.Message);
                }
            }
        }

        public IList<GeneralCashingDetails> SearchCashing(string search)
        {
            var rows = new List<GeneralCashingDetails>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 id, NoteID, NoteSerial, NoteSerial1, RecordDate, branch_no, GeneralBoxId, SubBoxId, Remarks
FROM dbo.tblGeneralCashing
WHERE @Search IS NULL OR CONVERT(nvarchar(20), id)=@Search OR NoteSerial1 LIKE @Like OR Remarks LIKE @Like
ORDER BY id DESC;";
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), 100);
                AddNullable(command, "@Like", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new GeneralCashingDetails
                        {
                            Id = ReadInt(reader, "id"),
                            NoteId = ReadNullableInt(reader, "NoteID"),
                            JournalSerial = ReadString(reader, "NoteSerial"),
                            VoucherSerial = ReadString(reader, "NoteSerial1"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            BranchId = ReadNullableInt(reader, "branch_no"),
                            GeneralBoxId = ReadNullableInt(reader, "GeneralBoxId"),
                            SubBoxId = ReadNullableInt(reader, "SubBoxId"),
                            Remarks = ReadString(reader, "Remarks")
                        });
                    }
                }
            }
            return rows;
        }

        public GeneralCashingDetails GetCashing(int id)
        {
            GeneralCashingDetails model = null;
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TOP 1 * FROM dbo.tblGeneralCashing WHERE id=@Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new GeneralCashingDetails
                            {
                                Id = ReadInt(reader, "id"),
                                NoteId = ReadNullableInt(reader, "NoteID"),
                                JournalSerial = ReadString(reader, "NoteSerial"),
                                VoucherSerial = ReadString(reader, "NoteSerial1"),
                                RecordDate = ReadDate(reader, "RecordDate"),
                                FromDate = ReadDate(reader, "FromDate"),
                                ToDate = ReadDate(reader, "ToDate"),
                                BranchId = ReadNullableInt(reader, "branch_no"),
                                GeneralBoxId = ReadNullableInt(reader, "GeneralBoxId"),
                                SubBoxId = ReadNullableInt(reader, "SubBoxId"),
                                CashierId = ReadNullableInt(reader, "CashierId"),
                                Remarks = ReadString(reader, "Remarks")
                            };
                        }
                    }
                }

                if (model == null) return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT d.*, p.PaymentName
FROM dbo.tblGeneralCashingDetails d
LEFT JOIN dbo.TblPaymentType p ON p.PaymentID = d.TransType
WHERE d.tblGeneralCashingId=@Id
ORDER BY d.id;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) model.Lines.Add(ReadCashingLine(reader));
                    }
                }
            }
            return model;
        }

        public LegacySaveResult SaveCashing(GeneralCashingDetails request, MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "tblGeneralCashing", "id", null) : request.Id.Value;
                    var noteId = isNew || !request.NoteId.HasValue || request.NoteId <= 0 ? NextInt(connection, transaction, "Notes", "NoteID", null) : request.NoteId.Value;
                    var journal = string.IsNullOrWhiteSpace(request.JournalSerial) ? NextNumber(connection, transaction, "Notes", "NoteSerial", "NoteType=59") : request.JournalSerial.Trim();
                    var voucher = string.IsNullOrWhiteSpace(request.VoucherSerial) ? NextNumber(connection, transaction, "Notes", "NoteSerial1", "NoteType=59") : request.VoucherSerial.Trim();

                    if (!isNew)
                    {
                        Execute(connection, transaction, "DELETE FROM dbo.tblGeneralCashingDetails WHERE tblGeneralCashingId=@Id", P("@Id", SqlDbType.Int, id));
                        Execute(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                        Execute(connection, transaction, "DELETE FROM dbo.Notes WHERE NoteID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = isNew
                            ? @"INSERT INTO dbo.tblGeneralCashing
(id, RecordDate, GeneralBoxId, SubBoxId, FromDate, ToDate, Remarks, OldNoteSerial1, NoteSerial, NoteSerial1, NoteID, branch_no, CashierId)
VALUES (@Id, @Date, @GeneralBox, @SubBox, @FromDate, @ToDate, @Remarks, @Voucher, @Journal, @Voucher, @NoteId, @Branch, @Cashier);"
                            : @"UPDATE dbo.tblGeneralCashing
SET RecordDate=@Date, GeneralBoxId=@GeneralBox, SubBoxId=@SubBox, FromDate=@FromDate, ToDate=@ToDate,
    Remarks=@Remarks, NoteSerial=@Journal, NoteSerial1=@Voucher, NoteID=@NoteId, branch_no=@Branch, CashierId=@Cashier
WHERE id=@Id;";
                        CashingParameters(command, request, id, noteId, journal, voucher);
                        command.ExecuteNonQuery();
                    }

                    foreach (var line in request.Lines.Where(x => x != null && x.CollectedValue != 0))
                    {
                        InsertCashingDetail(connection, transaction, id, line);
                    }

                    var total = request.Lines.Sum(x => x == null ? 0 : x.NetValue);
                    InsertNote(connection, transaction, noteId, 59, request.RecordDate, journal, voucher, total, request.Remarks, request.BranchId, user);
                    InsertCashingVoucher(connection, transaction, request, noteId, user);

                    transaction.Commit();
                    return Ok("تم حفظ سند الإيداع.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ سند الإيداع: " + ex.Message);
                }
            }
        }

        public LegacySaveResult DeleteCashing(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var noteId = ScalarInt(connection, transaction, "SELECT ISNULL(NoteID,0) FROM dbo.tblGeneralCashing WHERE id=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.tblGeneralCashingDetails WHERE tblGeneralCashingId=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                    Execute(connection, transaction, "DELETE FROM dbo.Notes WHERE NoteID=@NoteId AND NoteType=59", P("@NoteId", SqlDbType.Int, noteId));
                    Execute(connection, transaction, "DELETE FROM dbo.tblGeneralCashing WHERE id=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف سند الإيداع.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف سند الإيداع: " + ex.Message);
                }
            }
        }

        public IList<CarMaintenanceDetails> SearchCarMaintenance(string search)
        {
            var rows = new List<CarMaintenanceDetails>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 ID, NoteID, NoteSerial, NoteSerial1, RecordDate, BranchID, ClientName, PlateNo, TotalValue
FROM dbo.TblCarBillMentains
WHERE @Search IS NULL OR CONVERT(nvarchar(20), ID)=@Search OR NoteSerial1 LIKE @Like OR ClientName LIKE @Like OR PlateNo LIKE @Like
ORDER BY ID DESC;";
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), 100);
                AddNullable(command, "@Like", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new CarMaintenanceDetails
                        {
                            Id = ReadInt(reader, "ID"),
                            NoteId = ReadNullableInt(reader, "NoteID"),
                            JournalSerial = ReadString(reader, "NoteSerial"),
                            VoucherSerial = ReadString(reader, "NoteSerial1"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            BranchId = ReadNullableInt(reader, "BranchID"),
                            ClientName = ReadString(reader, "ClientName"),
                            PlateNo = ReadString(reader, "PlateNo"),
                            TotalValue = ReadDecimal(reader, "TotalValue")
                        });
                    }
                }
            }
            return rows;
        }

        public CarMaintenanceDetails GetCarMaintenance(int id)
        {
            CarMaintenanceDetails model = null;
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TOP 1 * FROM dbo.TblCarBillMentains WHERE ID=@Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) model = ReadCarMaintenance(reader);
                    }
                }

                if (model == null) return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT d.*, COALESCE(m.name, e.name) ItemName
FROM dbo.TblCarBillMentainsDetils d
LEFT JOIN dbo.TblMaintenanceWork m ON m.Id=d.Mainte AND ISNULL(d.Type,0)=0
LEFT JOIN dbo.TblExtraExpeneses e ON e.Id=d.Mainte AND ISNULL(d.Type,0)=1
WHERE d.ID=@Id
ORDER BY d.ID2;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) model.Lines.Add(ReadCarLine(reader));
                    }
                }
            }
            return model;
        }

        public LegacySaveResult SaveCarMaintenance(CarMaintenanceDetails request, MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "TblCarBillMentains", "ID", null) : request.Id.Value;
                    var noteId = isNew || !request.NoteId.HasValue || request.NoteId <= 0 ? NextInt(connection, transaction, "Notes", "NoteID", null) : request.NoteId.Value;
                    var voucher = string.IsNullOrWhiteSpace(request.VoucherSerial) ? NextNumber(connection, transaction, "TblCarBillMentains", "NoteSerial1", null) : request.VoucherSerial.Trim();
                    var journal = string.IsNullOrWhiteSpace(request.JournalSerial) ? NextNumber(connection, transaction, "Notes", "NoteSerial", "NoteType=8074") : request.JournalSerial.Trim();
                    request.TotalValue = request.Lines.Sum(x => x == null ? 0 : (x.TotalNet > 0 ? x.TotalNet : x.Value * (x.Count <= 0 ? 1 : x.Count))) - request.Discount + request.VatValue;

                    if (!isNew)
                    {
                        Execute(connection, transaction, "DELETE FROM dbo.TblCarBillMentainsDetils WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                        Execute(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                        Execute(connection, transaction, "DELETE FROM dbo.Notes WHERE NoteID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = isNew ? CarInsertSql : CarUpdateSql;
                        CarParameters(command, request, id, noteId, journal, voucher, user);
                        command.ExecuteNonQuery();
                    }

                    foreach (var line in request.Lines.Where(x => x != null && x.MainteId.HasValue && x.Value > 0))
                    {
                        InsertCarLine(connection, transaction, id, line);
                    }

                    InsertNote(connection, transaction, noteId, 8074, request.RecordDate, journal, voucher, request.TotalValue, "فاتورة صيانة رقم " + voucher, request.BranchId, user);
                    InsertCarVoucher(connection, transaction, request, noteId, user);

                    transaction.Commit();
                    return Ok("تم حفظ فاتورة الصيانة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ فاتورة الصيانة: " + ex.Message);
                }
            }
        }

        public LegacySaveResult DeleteCarMaintenance(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var noteId = ScalarInt(connection, transaction, "SELECT ISNULL(NoteID,0) FROM dbo.TblCarBillMentains WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.TblCarBillMentainsDetils WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID=@NoteId", P("@NoteId", SqlDbType.Int, noteId));
                    Execute(connection, transaction, "DELETE FROM dbo.Notes WHERE NoteID=@NoteId AND NoteType=8074", P("@NoteId", SqlDbType.Int, noteId));
                    Execute(connection, transaction, "DELETE FROM dbo.TblCarBillMentains WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف فاتورة الصيانة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف فاتورة الصيانة: " + ex.Message);
                }
            }
        }

        public IList<CarDataListItem> SearchCars(string search)
        {
            var rows = new List<CarDataListItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 c.id, c.Fullcode, c.Name, c.BoardNO, br.branch_name, ct.name CarTypeName
FROM dbo.TblCarsData c
LEFT JOIN dbo.TblBranchesData br ON br.branch_id = c.Branch_NO
LEFT JOIN dbo.TBLCarTypes ct ON ct.id = c.CarsTypeId
WHERE @Search IS NULL OR CONVERT(nvarchar(20), c.id)=@Search OR c.Fullcode LIKE @Like OR c.Name LIKE @Like OR c.BoardNO LIKE @Like
ORDER BY c.id DESC;";
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), 100);
                AddNullable(command, "@Like", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new CarDataListItem
                        {
                            Id = ReadInt(reader, "id"),
                            FullCode = ReadString(reader, "Fullcode"),
                            Name = ReadString(reader, "Name"),
                            BoardNo = ReadString(reader, "BoardNO"),
                            BranchName = ReadString(reader, "branch_name"),
                            CarTypeName = ReadString(reader, "CarTypeName")
                        });
                    }
                }
            }
            return rows;
        }

        public CarDataDetails GetCarData(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT TOP 1 * FROM dbo.TblCarsData WHERE id=@Id";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var model = ReadCarData(reader);
                    reader.Close();
                    LoadCarParts(connection, model);
                    return model;
                }
            }
        }

        public LegacySaveResult SaveCarData(CarDataDetails request)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (DuplicateCarData(connection, transaction, "Name", request.Name, request.Id)) return Fail("يوجد سيارة/معدة مسجلة مسبقا بهذا الاسم.");
                    if (DuplicateCarData(connection, transaction, "BoardNO", request.BoardNo, request.Id)) return Fail("يوجد سيارة/معدة مسجلة مسبقا بنفس رقم اللوحة.");
                    if (DuplicateCarData(connection, transaction, "Fullcode", request.FullCode, request.Id)) return Fail("يوجد سيارة/معدة مسجلة مسبقا بنفس الكود.");

                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "TblCarsData", "id", null) : request.Id.Value;
                    if (string.IsNullOrWhiteSpace(request.FullCode)) request.FullCode = (request.Prefix ?? "") + (request.Code ?? "");

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = isNew ? CarDataInsertSql : CarDataUpdateSql;
                        CarDataParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Ok("تم حفظ بيانات السيارة/المعدة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ بيانات السيارة/المعدة: " + ex.Message);
                }
            }
        }

        public LegacySaveResult DeleteCarData(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var authCount = ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.TblCardAuthorizationReform WHERE CarID=@Id", P("@Id", SqlDbType.Int, id));
                    if (authCount > 0) return Fail("لا يمكن حذف السيارة لوجود أوامر تفويض/صيانة مرتبطة بها.");
                    Execute(connection, transaction, "DELETE FROM dbo.TblCarsData WHERE id=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف بيانات السيارة/المعدة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف بيانات السيارة/المعدة: " + ex.Message);
                }
            }
        }

        public IList<CarAuthorizationDetails> SearchCarAuthorizations(string search)
        {
            var rows = new List<CarAuthorizationDetails>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 ID, RecordDate, ClientName, Telephone, PlateNo, BranchID, WorkOrder, AuthoOrder, OrderStatus
FROM dbo.TblCardAuthorizationReform
WHERE @Search IS NULL OR CONVERT(nvarchar(20), ID)=@Search OR ClientName LIKE @Like OR PlateNo LIKE @Like OR CONVERT(nvarchar(50), WorkOrder) LIKE @Like OR CONVERT(nvarchar(50), AuthoOrder) LIKE @Like
ORDER BY ID DESC;";
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : search.Trim(), 100);
                AddNullable(command, "@Like", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(search) ? null : "%" + search.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new CarAuthorizationDetails
                        {
                            Id = ReadInt(reader, "ID"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            ClientName = ReadString(reader, "ClientName"),
                            Telephone = ReadString(reader, "Telephone"),
                            PlateNo = ReadString(reader, "PlateNo"),
                            BranchId = ReadNullableInt(reader, "BranchID"),
                            WorkOrder = ReadDecimal(reader, "WorkOrder"),
                            AuthOrder = ReadDecimal(reader, "AuthoOrder"),
                            OrderStatus = ReadNullableInt(reader, "OrderStatus") ?? 0
                        });
                    }
                }
            }
            return rows;
        }

        public CarAuthorizationDetails GetCarAuthorization(int id)
        {
            CarAuthorizationDetails model = null;
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT TOP 1 * FROM dbo.TblCardAuthorizationReform WHERE ID=@Id";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) model = ReadCarAuthorization(reader);
                    }
                }

                if (model == null) return null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT d.*, COALESCE(m.name, e.name) ItemName
FROM dbo.TblCardAuthorizationReformDetails d
LEFT JOIN dbo.TblMaintenanceWork m ON m.Id=d.Mainte AND ISNULL(d.Type,0)=0
LEFT JOIN dbo.TblExtraExpeneses e ON e.Id=d.Mainte AND ISNULL(d.Type,0)=1
WHERE d.ID=@Id
ORDER BY d.ID2;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) model.Lines.Add(ReadCarAuthorizationLine(reader));
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT i.*, t.ItemName
FROM dbo.TblCardAuthorizationReformItems i
LEFT JOIN dbo.tblItems t ON t.ItemID=i.ItemID
WHERE i.ID=@Id
ORDER BY i.ID2;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) model.Items.Add(ReadCarAuthorizationItem(reader));
                    }
                }
            }
            return model;
        }

        public LegacySaveResult SaveCarAuthorization(CarAuthorizationDetails request, MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextInt(connection, transaction, "TblCardAuthorizationReform", "ID", null) : request.Id.Value;
                    if (!isNew)
                    {
                        Execute(connection, transaction, "DELETE FROM dbo.TblCardAuthorizationReformDetails WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                        Execute(connection, transaction, "DELETE FROM dbo.TblCardAuthorizationReformItems WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = isNew ? CarAuthInsertSql : CarAuthUpdateSql;
                        CarAuthorizationParameters(command, request, id, user);
                        command.ExecuteNonQuery();
                    }

                    foreach (var line in request.Lines.Where(x => x != null && x.MainteId.HasValue))
                    {
                        InsertCarAuthorizationLine(connection, transaction, id, line);
                    }
                    foreach (var item in request.Items.Where(x => x != null && (x.ItemId.HasValue || !string.IsNullOrWhiteSpace(x.ItemName)) && x.Qty > 0))
                    {
                        InsertCarAuthorizationItem(connection, transaction, id, item);
                    }

                    transaction.Commit();
                    return Ok("تم حفظ كارت التفويض/أمر الصيانة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ كارت التفويض/أمر الصيانة: " + ex.Message);
                }
            }
        }

        public LegacySaveResult DeleteCarAuthorization(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    Execute(connection, transaction, "DELETE FROM dbo.TblCardAuthorizationReformItems WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.TblCardAuthorizationReformDetails WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    Execute(connection, transaction, "DELETE FROM dbo.TblCardAuthorizationReform WHERE ID=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف كارت التفويض/أمر الصيانة.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف كارت التفويض/أمر الصيانة: " + ex.Message);
                }
            }
        }

        public IList<LegacyAttachmentItem> GetAttachments(string screenName, int recordId)
        {
            var rows = new List<LegacyAttachmentItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT Id, ScreenName, RecordId, FileName, FilePath, ContentType, FileSize, IsPrimary, Caption, CreatedAt
FROM dbo.MainErpLegacyAttachments
WHERE ScreenName=@ScreenName AND RecordId=@RecordId
ORDER BY IsPrimary DESC, CreatedAt DESC, Id DESC;";
                command.Parameters.Add("@ScreenName", SqlDbType.NVarChar, 100).Value = screenName;
                command.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) rows.Add(ReadAttachment(reader));
                }
            }
            return rows;
        }

        public LegacyAttachmentItem AddAttachment(string screenName, int recordId, string fileName, string filePath, string contentType, long fileSize, string caption, int? userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var hasPrimary = ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.MainErpLegacyAttachments WHERE ScreenName=@ScreenName AND RecordId=@RecordId AND IsPrimary=1",
                    P("@ScreenName", SqlDbType.NVarChar, screenName, 100), P("@RecordId", SqlDbType.Int, recordId)) > 0;
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
INSERT INTO dbo.MainErpLegacyAttachments
(ScreenName, RecordId, FileName, FilePath, ContentType, FileSize, IsPrimary, Caption, CreatedAt, CreatedBy)
OUTPUT INSERTED.Id
VALUES (@ScreenName, @RecordId, @FileName, @FilePath, @ContentType, @FileSize, @IsPrimary, @Caption, GETDATE(), @CreatedBy);";
                    command.Parameters.Add("@ScreenName", SqlDbType.NVarChar, 100).Value = screenName;
                    command.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                    command.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = fileName;
                    command.Parameters.Add("@FilePath", SqlDbType.NVarChar, 500).Value = filePath;
                    command.Parameters.Add("@ContentType", SqlDbType.NVarChar, 100).Value = contentType ?? string.Empty;
                    command.Parameters.Add("@FileSize", SqlDbType.BigInt).Value = fileSize;
                    command.Parameters.Add("@IsPrimary", SqlDbType.Bit).Value = !hasPrimary;
                    AddNullable(command, "@Caption", SqlDbType.NVarChar, caption, 300);
                    AddNullable(command, "@CreatedBy", SqlDbType.Int, userId);
                    var id = Convert.ToInt32(command.ExecuteScalar());
                    transaction.Commit();
                    return GetAttachment(id);
                }
            }
        }

        public LegacyAttachmentItem GetAttachment(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT Id, ScreenName, RecordId, FileName, FilePath, ContentType, FileSize, IsPrimary, Caption, CreatedAt
FROM dbo.MainErpLegacyAttachments WHERE Id=@Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadAttachment(reader) : null;
                }
            }
        }

        public LegacySaveResult DeleteAttachment(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    Execute(connection, transaction, "DELETE FROM dbo.MainErpLegacyAttachments WHERE Id=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم حذف المرفق.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف المرفق: " + ex.Message);
                }
            }
        }

        public LegacySaveResult SetPrimaryAttachment(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var item = GetAttachment(id);
                    if (item == null) return Fail("المرفق غير موجود.");
                    Execute(connection, transaction, "UPDATE dbo.MainErpLegacyAttachments SET IsPrimary=0 WHERE ScreenName=@ScreenName AND RecordId=@RecordId",
                        P("@ScreenName", SqlDbType.NVarChar, item.ScreenName, 100), P("@RecordId", SqlDbType.Int, item.RecordId));
                    Execute(connection, transaction, "UPDATE dbo.MainErpLegacyAttachments SET IsPrimary=1 WHERE Id=@Id", P("@Id", SqlDbType.Int, id));
                    transaction.Commit();
                    return Ok("تم تعيين الصورة الرئيسية.", id);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر تعيين الصورة الرئيسية: " + ex.Message);
                }
            }
        }

        private const string CarDataInsertSql = @"
INSERT INTO dbo.TblCarsData
(id, code, Fullcode, prifix, Branch_NO, CarsTypeId, VModel, VColor, Emp_id, fixedAssetid, Name, BoardNO, LicenseNO, Model,
 EqupName, LeaderName, PurchaseDate, LicenseExpireDate, InsuranceExpireDate, TestExpireDate, LastKMCounter, Capacity, Rate,
 Total, Machineno, Chesis, Gearno, Notes, FormOrignal, authorizeLicense, authorizeExamination, SpareTyre, Battery, Guarantee)
VALUES
(@Id, @Code, @FullCode, @Prefix, @BranchId, @CarTypeId, @CarModelId, @ColorId, @EmployeeId, @FixedAssetId, @Name, @BoardNo, @LicenseNo, @Model,
 @EquipmentName, @LeaderName, @PurchaseDate, @LicenseExpireDate, @InsuranceExpireDate, @TestExpireDate, @LastKMCounter, @Capacity, @Rate,
 @Total, @EngineNo, @Chassis, @GearNo, @Notes, @FormOriginal, @AuthorizeLicense, @AuthorizeExamination, @SpareTyre, @Battery, @Guarantee);";

        private const string CarDataUpdateSql = @"
UPDATE dbo.TblCarsData
SET code=@Code, Fullcode=@FullCode, prifix=@Prefix, Branch_NO=@BranchId, CarsTypeId=@CarTypeId, VModel=@CarModelId,
    VColor=@ColorId, Emp_id=@EmployeeId, fixedAssetid=@FixedAssetId, Name=@Name, BoardNO=@BoardNo, LicenseNO=@LicenseNo,
    Model=@Model, EqupName=@EquipmentName, LeaderName=@LeaderName, PurchaseDate=@PurchaseDate,
    LicenseExpireDate=@LicenseExpireDate, InsuranceExpireDate=@InsuranceExpireDate, TestExpireDate=@TestExpireDate,
    LastKMCounter=@LastKMCounter, Capacity=@Capacity, Rate=@Rate, Total=@Total, Machineno=@EngineNo, Chesis=@Chassis,
    Gearno=@GearNo, Notes=@Notes, FormOrignal=@FormOriginal, authorizeLicense=@AuthorizeLicense,
    authorizeExamination=@AuthorizeExamination, SpareTyre=@SpareTyre, Battery=@Battery, Guarantee=@Guarantee
WHERE id=@Id;";

        private const string CarAuthInsertSql = @"
INSERT INTO dbo.TblCardAuthorizationReform
(ID, RecordDate, ClientName, Telephone, UserID, CarTypeID, CarModelID, PlateNo, BranchID, ColorID, YearFact, OrderStatus,
 Accept, EndDate, CarMeter, Complaint, Noteinitial, Shaseh, RecordeTime, ClientCode, mobile, Cash, Accoun, credit,
 WorkOrder, AuthoOrder, CarMetarOut, SparePart, CarID, DiscValue, DiscPercent, TotalAfterDiscount, Vatyo, Vat2,
 finish, wait, CusID, SendSMS, QrCodeData, QrCodeDataPath, subcar1, subcar2, subcar3, subcar4, subcar5, subcar6, subcar7, subcar8, subcar9, subcar10, subcar11, subcar12, subcar13, subcar14)
VALUES
(@Id, @RecordDate, @ClientName, @Telephone, @UserId, @CarTypeId, @CarModelId, @PlateNo, @BranchId, @ColorId, @YearFact, @OrderStatus,
 @Accepted, @EndDate, @CarMeter, @Complaint, @InitialNote, @Chassis, @RecordTime, @ClientCode, @Mobile, @Cash, @Account, @Credit,
 @WorkOrder, @AuthOrder, @CarMeterOut, @SparePart, @CarId, @DiscountValue, @DiscountPercent, @TotalAfterDiscount, @VatPercent, @VatValue,
 @Finished, @Waiting, @CustomerId, @SendSms, @QrCodeData, @QrCodeDataPath, @SubCar1, @SubCar2, @SubCar3, @SubCar4, @SubCar5, @SubCar6, @SubCar7, @SubCar8, @SubCar9, @SubCar10, @SubCar11, @SubCar12, @SubCar13, @SubCar14);";

        private const string CarAuthUpdateSql = @"
UPDATE dbo.TblCardAuthorizationReform
SET RecordDate=@RecordDate, ClientName=@ClientName, Telephone=@Telephone, UserID=@UserId, CarTypeID=@CarTypeId,
    CarModelID=@CarModelId, PlateNo=@PlateNo, BranchID=@BranchId, ColorID=@ColorId, YearFact=@YearFact,
    OrderStatus=@OrderStatus, Accept=@Accepted, EndDate=@EndDate, CarMeter=@CarMeter, Complaint=@Complaint,
    Noteinitial=@InitialNote, Shaseh=@Chassis, RecordeTime=@RecordTime, ClientCode=@ClientCode, mobile=@Mobile,
    Cash=@Cash, Accoun=@Account, credit=@Credit, WorkOrder=@WorkOrder, AuthoOrder=@AuthOrder,
    CarMetarOut=@CarMeterOut, SparePart=@SparePart, CarID=@CarId, DiscValue=@DiscountValue,
    DiscPercent=@DiscountPercent, TotalAfterDiscount=@TotalAfterDiscount, Vatyo=@VatPercent, Vat2=@VatValue,
    finish=@Finished, wait=@Waiting, CusID=@CustomerId, SendSMS=@SendSms, QrCodeData=@QrCodeData, QrCodeDataPath=@QrCodeDataPath,
    subcar1=@SubCar1, subcar2=@SubCar2, subcar3=@SubCar3, subcar4=@SubCar4, subcar5=@SubCar5, subcar6=@SubCar6, subcar7=@SubCar7,
    subcar8=@SubCar8, subcar9=@SubCar9, subcar10=@SubCar10, subcar11=@SubCar11, subcar12=@SubCar12, subcar13=@SubCar13, subcar14=@SubCar14
WHERE ID=@Id;";

        private const string CarInsertSql = @"
INSERT INTO dbo.TblCarBillMentains
(ID, RecordDate, EndDate, ClientName, Telephone, UserID, CarTypeID, CarModelID, PlateNo, BranchID, ColorID, YearFact,
 OrderStatus, Accept, Month_Day, Granty, DateStartG, DateEndG, CarMeter, LongGranty, PayFirst, AmountAccept,
 NoteSerial, NoteSerial1, NoteID, CusID, WorkOrderNO, OverKM, PaymentType, Trans_DiscountType, BoxID, Trans_Discount,
 CarMetarOut, SparePart, NetDiscount, PaymentValue, difdisValue, BasedOn, FATYou, FATValue, TotalValue, AccountCodeVat, CarID, AuthoOrder)
VALUES
(@Id, @Date, @EndDate, @ClientName, @Telephone, @UserId, @CarTypeId, @CarModelId, @PlateNo, @BranchId, @ColorId, @YearFact,
 5, 0, 0, 1, @Date, @EndDate, NULL, NULL, 0, 0,
 @Journal, @Voucher, @NoteId, @CustomerId, @WorkOrderNo, NULL, @PaymentType, @DiscountType, @BoxId, @Discount,
 NULL, @SparePart, @Discount, @PaymentValue, @TotalBeforeVat, 0, @VatPercent, @VatValue, @TotalValue, @VatAccountCode, NULL, NULL);";

        private const string CarUpdateSql = @"
UPDATE dbo.TblCarBillMentains
SET RecordDate=@Date, EndDate=@EndDate, ClientName=@ClientName, Telephone=@Telephone, UserID=@UserId,
    CarTypeID=@CarTypeId, CarModelID=@CarModelId, PlateNo=@PlateNo, BranchID=@BranchId, ColorID=@ColorId, YearFact=@YearFact,
    NoteSerial=@Journal, NoteSerial1=@Voucher, NoteID=@NoteId, CusID=@CustomerId, WorkOrderNO=@WorkOrderNo,
    PaymentType=@PaymentType, Trans_DiscountType=@DiscountType, BoxID=@BoxId, Trans_Discount=@Discount,
    SparePart=@SparePart, NetDiscount=@Discount, PaymentValue=@PaymentValue, difdisValue=@TotalBeforeVat,
    FATYou=@VatPercent, FATValue=@VatValue, TotalValue=@TotalValue, AccountCodeVat=@VatAccountCode
WHERE ID=@Id;";

        private void InsertCashingVoucher(SqlConnection connection, SqlTransaction transaction, GeneralCashingDetails request, int noteId, MainErpUserContext user)
        {
            var line = 1;
            var generalAccount = BoxAccount(connection, transaction, request.GeneralBoxId);
            var subAccount = BoxAccount(connection, transaction, request.SubBoxId);
            foreach (var item in request.Lines.Where(x => x != null && x.NetValue > 0))
            {
                var description = "إيداع عام " + (item.PaymentName ?? item.Remarks);
                InsertDev(connection, transaction, line++, generalAccount, item.NetValue, 0, description, noteId, request.RecordDate, request.BranchId, user);
                InsertDev(connection, transaction, line++, item.PaymentId == 0 ? subAccount : item.AccountsUs, item.CollectedValue > 0 ? item.CollectedValue : item.NetValue, 1, description, noteId, request.RecordDate, request.BranchId, user);
                if (item.CommissionValue > 0) InsertDev(connection, transaction, line++, item.AccountCom, item.CommissionValue, 0, description + " عمولة", noteId, request.RecordDate, request.BranchId, user);
            }
        }

        private void InsertCarVoucher(SqlConnection connection, SqlTransaction transaction, CarMaintenanceDetails request, int noteId, MainErpUserContext user)
        {
            var line = 1;
            var customerAccount = CustomerAccount(connection, transaction, request.CustomerId);
            var boxAccount = BoxAccount(connection, transaction, request.BoxId);
            var salesAccount = request.Lines.Select(x => x.AccountCode).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            var description = "فاتورة صيانة " + request.VoucherSerial;
            if (request.TotalValue > 0) InsertDev(connection, transaction, line++, customerAccount, request.TotalValue, 0, description, noteId, request.RecordDate, request.BranchId, user);
            if (request.PaymentType == 0 && request.PaymentValue > 0)
            {
                InsertDev(connection, transaction, line++, boxAccount, request.PaymentValue, 0, description + " نقدي", noteId, request.RecordDate, request.BranchId, user);
                InsertDev(connection, transaction, line++, customerAccount, request.PaymentValue, 1, description + " نقدي", noteId, request.RecordDate, request.BranchId, user);
            }
            var serviceTotal = request.Lines.Where(x => x.Type == 0).Sum(x => x.TotalNet > 0 ? x.TotalNet : x.Value * (x.Count <= 0 ? 1 : x.Count));
            if (serviceTotal > 0) InsertDev(connection, transaction, line++, salesAccount, serviceTotal, 1, description + " أعمال صيانة", noteId, request.RecordDate, request.BranchId, user);
            var extraTotal = request.Lines.Where(x => x.Type == 1).Sum(x => x.TotalNet > 0 ? x.TotalNet : x.Value * (x.Count <= 0 ? 1 : x.Count));
            if (extraTotal > 0) InsertDev(connection, transaction, line++, salesAccount, extraTotal, 1, description + " مصروفات إضافية", noteId, request.RecordDate, request.BranchId, user);
            if (request.VatValue > 0) InsertDev(connection, transaction, line++, request.VatAccountCode, request.VatValue, 1, description + " ضريبة", noteId, request.RecordDate, request.BranchId, user);
        }

        private void InsertDev(SqlConnection connection, SqlTransaction transaction, int lineNo, string accountCode, decimal value, int debitCredit, string description, int noteId, DateTime? date, int? branchId, MainErpUserContext user)
        {
            if (string.IsNullOrWhiteSpace(accountCode) || value <= 0) return;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(Double_Entry_Vouchers_ID, DEV_ID_Line_No, DEV_ID_Line_No1, Account_Code, Value, Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Account_Interval_ID, branch_id, hideline)
VALUES (@Id, @LineNo, @LineNo, @AccountCode, @Value, @DebitCredit, @Description, @Date, @NoteId, @UserId, NULL, @BranchId, 0);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = NextInt(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID", null);
                command.Parameters.Add("@LineNo", SqlDbType.Int).Value = lineNo;
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 100).Value = accountCode;
                command.Parameters.Add("@Value", SqlDbType.Money).Value = value;
                command.Parameters.Add("@DebitCredit", SqlDbType.SmallInt).Value = debitCredit;
                AddNullable(command, "@Description", SqlDbType.NVarChar, description, 500);
                command.Parameters.Add("@Date", SqlDbType.DateTime).Value = date ?? DateTime.Today;
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                AddNullable(command, "@UserId", SqlDbType.Int, user == null ? null : (object)user.UserId);
                AddNullable(command, "@BranchId", SqlDbType.Int, branchId);
                command.ExecuteNonQuery();
            }
        }

        private void InsertNote(SqlConnection connection, SqlTransaction transaction, int noteId, int noteType, DateTime? date, string journal, string voucher, decimal value, string remark, int? branchId, MainErpUserContext user)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.Notes
(NoteID, NoteType, NoteDate, UserID, NoteSerial, NoteSerial1, Note_Value, remark, Branch_no, numbering_type, numbering_type1, sanad_year, sanad_month)
VALUES (@NoteId, @NoteType, @Date, @UserId, @Journal, @Voucher, @Value, @Remark, @BranchId, 0, @NoteType, YEAR(@Date), MONTH(@Date));";
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@NoteType", SqlDbType.Int).Value = noteType;
                command.Parameters.Add("@Date", SqlDbType.DateTime).Value = date ?? DateTime.Today;
                AddNullable(command, "@UserId", SqlDbType.Int, user == null ? null : (object)user.UserId);
                AddNullable(command, "@Journal", SqlDbType.NVarChar, journal, 100);
                AddNullable(command, "@Voucher", SqlDbType.NVarChar, voucher, 100);
                command.Parameters.Add("@Value", SqlDbType.Float).Value = Convert.ToDouble(value);
                AddNullable(command, "@Remark", SqlDbType.NVarChar, remark, 500);
                AddNullable(command, "@BranchId", SqlDbType.Int, branchId);
                command.ExecuteNonQuery();
            }
        }

        private void InsertCashingDetail(SqlConnection connection, SqlTransaction transaction, int id, GeneralCashingLine line)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.tblGeneralCashingDetails
(tblGeneralCashingId, TransType, value, CollectedValue, CommissionValue, Different, Remarks, Accountsus, Accountcom, Account_Code, CommissionPercentage, NetValue)
VALUES (@Id, @PaymentId, @Balance, @Collected, @Commission, @Different, @Remarks, @AccountsUs, @AccountCom, @AccountCode, @CommissionPercent, @NetValue);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@PaymentId", SqlDbType.Int).Value = line.PaymentId;
                command.Parameters.Add("@Balance", SqlDbType.Money).Value = line.Balance;
                command.Parameters.Add("@Collected", SqlDbType.Money).Value = line.CollectedValue;
                command.Parameters.Add("@Commission", SqlDbType.Money).Value = line.CommissionValue;
                command.Parameters.Add("@Different", SqlDbType.Money).Value = line.Different;
                AddNullable(command, "@Remarks", SqlDbType.NVarChar, line.Remarks, 500);
                AddNullable(command, "@AccountsUs", SqlDbType.NVarChar, line.AccountsUs, 100);
                AddNullable(command, "@AccountCom", SqlDbType.NVarChar, line.AccountCom, 100);
                AddNullable(command, "@AccountCode", SqlDbType.NVarChar, line.AccountCode, 100);
                command.Parameters.Add("@CommissionPercent", SqlDbType.Float).Value = Convert.ToDouble(line.CommissionPercentage);
                command.Parameters.Add("@NetValue", SqlDbType.Float).Value = Convert.ToDouble(line.NetValue);
                command.ExecuteNonQuery();
            }
        }

        private void InsertCarLine(SqlConnection connection, SqlTransaction transaction, int id, CarMaintenanceLine line)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.TblCarBillMentainsDetils
(ID, Type, Mainte, Value, count, comp, bill, ValiueDis, fittervalue, Emp_ID, DiscValue, Percentage, TotalNet, AccountCode, Deptid)
VALUES (@Id, @Type, @Mainte, @Value, @Count, @Company, @Bill, @Value, 0, @EmpId, @Disc, @Percent, @TotalNet, @AccountCode, @DeptId);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@Type", SqlDbType.Int).Value = line.Type;
                AddNullable(command, "@Mainte", SqlDbType.Int, line.MainteId);
                command.Parameters.Add("@Value", SqlDbType.Float).Value = Convert.ToDouble(line.Value);
                command.Parameters.Add("@Count", SqlDbType.Int).Value = line.Count <= 0 ? 1 : line.Count;
                AddNullable(command, "@Company", SqlDbType.NVarChar, line.Company, 200);
                AddNullable(command, "@Bill", SqlDbType.NVarChar, line.BillNo, 100);
                AddNullable(command, "@EmpId", SqlDbType.Int, line.EmployeeId);
                command.Parameters.Add("@Disc", SqlDbType.Float).Value = Convert.ToDouble(line.DiscountValue);
                command.Parameters.Add("@Percent", SqlDbType.Float).Value = Convert.ToDouble(line.Percentage);
                command.Parameters.Add("@TotalNet", SqlDbType.Float).Value = Convert.ToDouble(line.TotalNet > 0 ? line.TotalNet : line.Value * (line.Count <= 0 ? 1 : line.Count));
                AddNullable(command, "@AccountCode", SqlDbType.NVarChar, line.AccountCode, 100);
                AddNullable(command, "@DeptId", SqlDbType.Int, line.DepartmentId);
                command.ExecuteNonQuery();
            }
        }

        private string CreateAccount(SqlConnection connection, SqlTransaction transaction, string parent, string name, string englishName)
        {
            if (string.IsNullOrWhiteSpace(parent)) throw new InvalidOperationException("حساب الأب مطلوب لإنشاء حساب الخزنة.");
            var code = NextAccountCode(connection, transaction, parent.Trim());
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO dbo.ACCOUNTS
(Account_ID, Account_Code, Account_Name, Parent_Account_Code, last_account, cannot_del, BasicAccount, DateCreated, Account_NameEng)
VALUES (@Id, @Code, @Name, @Parent, 1, 0, 0, GETDATE(), @NameE);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = NextInt(connection, transaction, "ACCOUNTS", "Account_ID", null);
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = code;
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 4000).Value = name;
                command.Parameters.Add("@Parent", SqlDbType.NVarChar, 70).Value = parent.Trim();
                AddNullable(command, "@NameE", SqlDbType.NVarChar, englishName, 4000);
                command.ExecuteNonQuery();
            }
            return code;
        }

        private string NextAccountCode(SqlConnection connection, SqlTransaction transaction, string parent)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT Account_Code FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK) WHERE Parent_Account_Code=@Parent";
                command.Parameters.Add("@Parent", SqlDbType.NVarChar, 70).Value = parent;
                var max = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = Convert.ToString(reader[0]);
                        var last = code.LastIndexOf('a');
                        int suffix;
                        if (last >= 0 && int.TryParse(code.Substring(last + 1), out suffix) && suffix > max) max = suffix;
                    }
                }
                return parent + "a" + (max + 1);
            }
        }

        private string BoxAccount(SqlConnection connection, SqlTransaction transaction, int? boxId)
        {
            return boxId.HasValue ? ScalarString(connection, transaction, "SELECT Account_Code FROM dbo.TblBoxesData WHERE BoxID=@Id", P("@Id", SqlDbType.Int, boxId.Value)) : null;
        }

        private string CustomerAccount(SqlConnection connection, SqlTransaction transaction, int? customerId)
        {
            return customerId.HasValue ? ScalarString(connection, transaction, "SELECT Account_Code FROM dbo.TblCustemers WHERE CusID=@Id", P("@Id", SqlDbType.Int, customerId.Value)) : null;
        }

        private IList<LegacyLookupItem> Lookup(string sql, string idColumn, string textColumn, string accountColumn)
        {
            var rows = new List<LegacyLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new LegacyLookupItem { Id = Convert.ToString(reader[idColumn]), Text = ReadString(reader, textColumn), AccountCode = ReadString(reader, accountColumn) });
                    }
                }
            }
            return rows;
        }

        private static void BoxParameters(SqlCommand command, CashBoxDetails request, int id, string accountCode)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            AddNullable(command, "@Name", SqlDbType.NVarChar, request.Name, 200);
            AddNullable(command, "@NameE", SqlDbType.NVarChar, request.EnglishName, 200);
            AddNullable(command, "@Remarks", SqlDbType.NVarChar, request.Remarks, 500);
            command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 100).Value = accountCode;
            command.Parameters.Add("@Type", SqlDbType.Int).Value = request.Type;
            AddNullable(command, "@EmpId", SqlDbType.Int, request.EmployeeId);
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@ParentAccount", SqlDbType.NVarChar, request.ParentAccountCode, 100);
            command.Parameters.Add("@ChequeBox", SqlDbType.Bit).Value = request.ChequeBox;
            AddNullable(command, "@OpenBalanceDate", SqlDbType.DateTime, request.OpenBalanceDate);
            AddNullable(command, "@OpenBalanceType", SqlDbType.Int, request.OpenBalanceType);
            command.Parameters.Add("@OpenBalance", SqlDbType.Float).Value = Convert.ToDouble(request.OpenBalance);
            command.Parameters.Add("@BoxValue", SqlDbType.Float).Value = Convert.ToDouble(request.BoxValue);
            AddNullable(command, "@Period", SqlDbType.Int, request.Period);
            AddNullable(command, "@PeriodType", SqlDbType.Int, request.PeriodType);
        }

        private static void CashingParameters(SqlCommand command, GeneralCashingDetails request, int id, int noteId, string journal, string voucher)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
            command.Parameters.Add("@Date", SqlDbType.DateTime).Value = request.RecordDate ?? DateTime.Today;
            AddNullable(command, "@FromDate", SqlDbType.DateTime, request.FromDate);
            AddNullable(command, "@ToDate", SqlDbType.DateTime, request.ToDate);
            AddNullable(command, "@GeneralBox", SqlDbType.Int, request.GeneralBoxId);
            AddNullable(command, "@SubBox", SqlDbType.Int, request.SubBoxId);
            AddNullable(command, "@Branch", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@Cashier", SqlDbType.Int, request.CashierId);
            AddNullable(command, "@Remarks", SqlDbType.NVarChar, request.Remarks, 500);
            AddNullable(command, "@Journal", SqlDbType.NVarChar, journal, 100);
            AddNullable(command, "@Voucher", SqlDbType.NVarChar, voucher, 100);
        }

        private static void CarDataParameters(SqlCommand command, CarDataDetails request, int id)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            AddNullable(command, "@Code", SqlDbType.NVarChar, request.Code, 100);
            AddNullable(command, "@FullCode", SqlDbType.NVarChar, request.FullCode, 100);
            AddNullable(command, "@Prefix", SqlDbType.NVarChar, request.Prefix, 50);
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@CarTypeId", SqlDbType.Int, request.CarTypeId);
            AddNullable(command, "@CarModelId", SqlDbType.Int, request.CarModelId);
            AddNullable(command, "@ColorId", SqlDbType.Int, request.ColorId);
            AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
            AddNullable(command, "@FixedAssetId", SqlDbType.Int, request.FixedAssetId);
            AddNullable(command, "@Name", SqlDbType.NVarChar, request.Name, 200);
            AddNullable(command, "@BoardNo", SqlDbType.NVarChar, request.BoardNo, 100);
            AddNullable(command, "@LicenseNo", SqlDbType.NVarChar, request.LicenseNo, 100);
            AddNullable(command, "@Model", SqlDbType.NVarChar, request.Model, 100);
            AddNullable(command, "@EquipmentName", SqlDbType.NVarChar, request.EquipmentName, 200);
            AddNullable(command, "@LeaderName", SqlDbType.NVarChar, request.LeaderName, 200);
            AddNullable(command, "@PurchaseDate", SqlDbType.DateTime, request.PurchaseDate);
            AddNullable(command, "@LicenseExpireDate", SqlDbType.DateTime, request.LicenseExpireDate);
            AddNullable(command, "@InsuranceExpireDate", SqlDbType.DateTime, request.InsuranceExpireDate);
            AddNullable(command, "@TestExpireDate", SqlDbType.DateTime, request.TestExpireDate);
            command.Parameters.Add("@LastKMCounter", SqlDbType.Float).Value = Convert.ToDouble(request.LastKMCounter);
            command.Parameters.Add("@Capacity", SqlDbType.Float).Value = Convert.ToDouble(request.Capacity);
            command.Parameters.Add("@Rate", SqlDbType.Float).Value = Convert.ToDouble(request.Rate);
            command.Parameters.Add("@Total", SqlDbType.Float).Value = Convert.ToDouble(request.Total);
            AddNullable(command, "@EngineNo", SqlDbType.NVarChar, request.EngineNo, 100);
            AddNullable(command, "@Chassis", SqlDbType.NVarChar, request.Chassis, 100);
            AddNullable(command, "@GearNo", SqlDbType.NVarChar, request.GearNo, 100);
            AddNullable(command, "@Notes", SqlDbType.NVarChar, request.Notes, 1000);
            command.Parameters.Add("@FormOriginal", SqlDbType.Bit).Value = request.FormOriginal;
            command.Parameters.Add("@AuthorizeLicense", SqlDbType.Bit).Value = request.AuthorizeLicense;
            command.Parameters.Add("@AuthorizeExamination", SqlDbType.Bit).Value = request.AuthorizeExamination;
            command.Parameters.Add("@SpareTyre", SqlDbType.Bit).Value = request.SpareTyre;
            command.Parameters.Add("@Battery", SqlDbType.Bit).Value = request.Battery;
            command.Parameters.Add("@Guarantee", SqlDbType.Bit).Value = request.Guarantee;
        }

        private static void CarAuthorizationParameters(SqlCommand command, CarAuthorizationDetails request, int id, MainErpUserContext user)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = request.RecordDate ?? DateTime.Today;
            AddNullable(command, "@EndDate", SqlDbType.DateTime, request.EndDate);
            AddNullable(command, "@ClientName", SqlDbType.NVarChar, request.ClientName, 300);
            AddNullable(command, "@Telephone", SqlDbType.NVarChar, request.Telephone, 100);
            AddNullable(command, "@Mobile", SqlDbType.NVarChar, request.Mobile, 100);
            AddNullable(command, "@UserId", SqlDbType.Int, request.UserId.HasValue ? (object)request.UserId.Value : user == null ? null : (object)user.UserId);
            AddNullable(command, "@CustomerId", SqlDbType.Int, request.CustomerId);
            command.Parameters.Add("@ClientCode", SqlDbType.NVarChar, 100).Value = request.ClientCode ?? string.Empty;
            AddNullable(command, "@CarId", SqlDbType.Int, request.CarId);
            AddNullable(command, "@CarTypeId", SqlDbType.Int, request.CarTypeId);
            AddNullable(command, "@CarModelId", SqlDbType.Int, request.CarModelId);
            AddNullable(command, "@ColorId", SqlDbType.Int, request.ColorId);
            AddNullable(command, "@PlateNo", SqlDbType.NVarChar, request.PlateNo, 100);
            AddNullable(command, "@YearFact", SqlDbType.Int, request.YearFact);
            command.Parameters.Add("@OrderStatus", SqlDbType.Int).Value = request.OrderStatus;
            command.Parameters.Add("@CarMeter", SqlDbType.Float).Value = Convert.ToDouble(request.CarMeter);
            command.Parameters.Add("@CarMeterOut", SqlDbType.Float).Value = Convert.ToDouble(request.CarMeterOut);
            AddNullable(command, "@Complaint", SqlDbType.NVarChar, request.Complaint, 1000);
            AddNullable(command, "@InitialNote", SqlDbType.NVarChar, request.InitialNote, 1000);
            AddNullable(command, "@Chassis", SqlDbType.NVarChar, request.Chassis, 100);
            command.Parameters.Add("@WorkOrder", SqlDbType.Float).Value = Convert.ToDouble(request.WorkOrder);
            command.Parameters.Add("@AuthOrder", SqlDbType.Float).Value = Convert.ToDouble(request.AuthOrder);
            AddNullable(command, "@SparePart", SqlDbType.NVarChar, request.SparePart, 500);
            command.Parameters.Add("@Cash", SqlDbType.Bit).Value = request.Cash;
            command.Parameters.Add("@Account", SqlDbType.Bit).Value = request.Account;
            command.Parameters.Add("@Credit", SqlDbType.Bit).Value = request.Credit;
            command.Parameters.Add("@Accepted", SqlDbType.Bit).Value = request.Accepted;
            command.Parameters.Add("@Finished", SqlDbType.Bit).Value = request.Finished;
            command.Parameters.Add("@Waiting", SqlDbType.Bit).Value = request.Waiting;
            command.Parameters.Add("@SendSms", SqlDbType.Bit).Value = request.SendSms;
            AddNullable(command, "@QrCodeData", SqlDbType.NVarChar, request.QrCodeData, 255);
            AddNullable(command, "@QrCodeDataPath", SqlDbType.NVarChar, request.QrCodeDataPath, 255);
            command.Parameters.Add("@SubCar1", SqlDbType.Bit).Value = request.SubCar1;
            command.Parameters.Add("@SubCar2", SqlDbType.Bit).Value = request.SubCar2;
            command.Parameters.Add("@SubCar3", SqlDbType.Bit).Value = request.SubCar3;
            command.Parameters.Add("@SubCar4", SqlDbType.Bit).Value = request.SubCar4;
            command.Parameters.Add("@SubCar5", SqlDbType.Bit).Value = request.SubCar5;
            command.Parameters.Add("@SubCar6", SqlDbType.Bit).Value = request.SubCar6;
            command.Parameters.Add("@SubCar7", SqlDbType.Bit).Value = request.SubCar7;
            command.Parameters.Add("@SubCar8", SqlDbType.Bit).Value = request.SubCar8;
            command.Parameters.Add("@SubCar9", SqlDbType.Bit).Value = request.SubCar9;
            command.Parameters.Add("@SubCar10", SqlDbType.Bit).Value = request.SubCar10;
            command.Parameters.Add("@SubCar11", SqlDbType.Bit).Value = request.SubCar11;
            command.Parameters.Add("@SubCar12", SqlDbType.Bit).Value = request.SubCar12;
            command.Parameters.Add("@SubCar13", SqlDbType.Bit).Value = request.SubCar13;
            command.Parameters.Add("@SubCar14", SqlDbType.Bit).Value = request.SubCar14;
            command.Parameters.Add("@DiscountValue", SqlDbType.Float).Value = Convert.ToDouble(request.DiscountValue);
            command.Parameters.Add("@DiscountPercent", SqlDbType.Float).Value = Convert.ToDouble(request.DiscountPercent);
            command.Parameters.Add("@TotalAfterDiscount", SqlDbType.Float).Value = Convert.ToDouble(request.TotalAfterDiscount);
            command.Parameters.Add("@VatPercent", SqlDbType.Float).Value = Convert.ToDouble(request.VatPercent);
            command.Parameters.Add("@VatValue", SqlDbType.Float).Value = Convert.ToDouble(request.VatValue);
            command.Parameters.Add("@RecordTime", SqlDbType.NVarChar, 50).Value = DateTime.Now.ToString("HH:mm");
        }

        private static void CarParameters(SqlCommand command, CarMaintenanceDetails request, int id, int noteId, string journal, string voucher, MainErpUserContext user)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
            AddNullable(command, "@Journal", SqlDbType.NVarChar, journal, 100);
            AddNullable(command, "@Voucher", SqlDbType.NVarChar, voucher, 100);
            command.Parameters.Add("@Date", SqlDbType.DateTime).Value = request.RecordDate ?? DateTime.Today;
            command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = request.EndDate ?? request.RecordDate ?? DateTime.Today;
            AddNullable(command, "@ClientName", SqlDbType.NVarChar, request.ClientName, 300);
            AddNullable(command, "@Telephone", SqlDbType.NVarChar, request.Telephone, 100);
            AddNullable(command, "@UserId", SqlDbType.Int, user == null ? null : (object)user.UserId);
            AddNullable(command, "@CarTypeId", SqlDbType.Int, request.CarTypeId);
            AddNullable(command, "@CarModelId", SqlDbType.Int, request.CarModelId);
            AddNullable(command, "@PlateNo", SqlDbType.NVarChar, request.PlateNo, 100);
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@ColorId", SqlDbType.Int, request.ColorId);
            AddNullable(command, "@YearFact", SqlDbType.Int, request.YearFact);
            AddNullable(command, "@CustomerId", SqlDbType.Int, request.CustomerId);
            AddNullable(command, "@WorkOrderNo", SqlDbType.NVarChar, request.WorkOrderNo, 100);
            command.Parameters.Add("@PaymentType", SqlDbType.Int).Value = request.PaymentType;
            command.Parameters.Add("@DiscountType", SqlDbType.Int).Value = request.DiscountType;
            AddNullable(command, "@BoxId", SqlDbType.Int, request.PaymentType == 0 ? request.BoxId : null);
            command.Parameters.Add("@Discount", SqlDbType.Float).Value = Convert.ToDouble(request.Discount);
            AddNullable(command, "@SparePart", SqlDbType.NVarChar, request.SparePart, 500);
            command.Parameters.Add("@PaymentValue", SqlDbType.Float).Value = Convert.ToDouble(request.PaymentValue);
            command.Parameters.Add("@TotalBeforeVat", SqlDbType.Float).Value = Convert.ToDouble(request.TotalValue - request.VatValue);
            command.Parameters.Add("@VatPercent", SqlDbType.Float).Value = Convert.ToDouble(request.VatPercent);
            command.Parameters.Add("@VatValue", SqlDbType.Float).Value = Convert.ToDouble(request.VatValue);
            command.Parameters.Add("@TotalValue", SqlDbType.Float).Value = Convert.ToDouble(request.TotalValue);
            AddNullable(command, "@VatAccountCode", SqlDbType.NVarChar, request.VatAccountCode, 100);
        }

        private static GeneralCashingLine ReadCashingLine(IDataRecord reader)
        {
            return new GeneralCashingLine
            {
                PaymentId = ReadNullableInt(reader, "TransType") ?? 0,
                PaymentName = ReadString(reader, "PaymentName"),
                Balance = ReadDecimal(reader, "value"),
                CollectedValue = ReadDecimal(reader, "CollectedValue"),
                CommissionPercentage = ReadDecimal(reader, "CommissionPercentage"),
                CommissionValue = ReadDecimal(reader, "CommissionValue"),
                Different = ReadDecimal(reader, "Different"),
                NetValue = ReadDecimal(reader, "NetValue"),
                AccountsUs = ReadString(reader, "Accountsus"),
                AccountCom = ReadString(reader, "Accountcom"),
                AccountCode = ReadString(reader, "Account_Code"),
                Remarks = ReadString(reader, "Remarks")
            };
        }

        private static void InsertCarAuthorizationLine(SqlConnection connection, SqlTransaction transaction, int id, CarAuthorizationLine line)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.TblCardAuthorizationReformDetails
(ID, Type, Mainte, Value, [count], comp, bill, nohours, finish, DateEnter, DateExit, fitter, supervisor, workshop,
 TimeEnter, TimOut, EmpID, PriceFitter, DeptColor, Deptid, empsuper)
VALUES
(@Id, @Type, @Mainte, @Value, @Count, @Company, @Bill, @Hours, @Finish, @DateEnter, @DateExit, @EmployeeId, @SupervisorId, NULL,
 @TimeEnter, @TimeOut, @EmployeeId, @PriceFitter, @DeptColor, @DepartmentId, @SupervisorId);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@Type", SqlDbType.Int).Value = line.Type;
                AddNullable(command, "@Mainte", SqlDbType.Int, line.MainteId);
                command.Parameters.Add("@Value", SqlDbType.Float).Value = Convert.ToDouble(line.Value);
                command.Parameters.Add("@Count", SqlDbType.Int).Value = line.Count <= 0 ? 1 : line.Count;
                AddNullable(command, "@Company", SqlDbType.NVarChar, line.Company, 200);
                AddNullable(command, "@Bill", SqlDbType.NVarChar, line.BillNo, 100);
                command.Parameters.Add("@Hours", SqlDbType.Float).Value = Convert.ToDouble(line.Hours);
                command.Parameters.Add("@Finish", SqlDbType.Bit).Value = line.Finish;
                AddNullable(command, "@DateEnter", SqlDbType.DateTime, line.DateEnter);
                AddNullable(command, "@DateExit", SqlDbType.DateTime, line.DateExit);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, line.EmployeeId);
                AddNullable(command, "@SupervisorId", SqlDbType.Int, line.SupervisorId);
                AddNullable(command, "@TimeEnter", SqlDbType.NVarChar, line.TimeEnter, 50);
                AddNullable(command, "@TimeOut", SqlDbType.NVarChar, line.TimeOut, 50);
                command.Parameters.Add("@PriceFitter", SqlDbType.Float).Value = Convert.ToDouble(line.PriceFitter);
                AddNullable(command, "@DeptColor", SqlDbType.NVarChar, line.DepartmentColor, 50);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, line.DepartmentId);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertCarAuthorizationItem(SqlConnection connection, SqlTransaction transaction, int id, CarAuthorizationItem item)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.TblCardAuthorizationReformItems
(ID, ItemID, Vatyo, Price, Vat2, TotalWithVat, Remark, qty, beforeVat, PriceBDisc, DiscPercent, DiscValue, ItemName2)
VALUES
(@Id, @ItemId, @VatPercent, @Price, @VatValue, @TotalWithVat, @Remark, @Qty, @BeforeVat, @PriceBDisc, @DiscPercent, @DiscValue, @ItemName);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                AddNullable(command, "@ItemId", SqlDbType.Int, item.ItemId);
                command.Parameters.Add("@VatPercent", SqlDbType.Float).Value = Convert.ToDouble(item.VatPercent);
                command.Parameters.Add("@Price", SqlDbType.Float).Value = Convert.ToDouble(item.Price);
                command.Parameters.Add("@VatValue", SqlDbType.Float).Value = Convert.ToDouble(item.VatValue);
                command.Parameters.Add("@TotalWithVat", SqlDbType.Float).Value = Convert.ToDouble(item.TotalWithVat);
                AddNullable(command, "@Remark", SqlDbType.NVarChar, item.Remark, 500);
                command.Parameters.Add("@Qty", SqlDbType.Float).Value = Convert.ToDouble(item.Qty);
                command.Parameters.Add("@BeforeVat", SqlDbType.Float).Value = Convert.ToDouble(item.BeforeVat);
                command.Parameters.Add("@PriceBDisc", SqlDbType.Float).Value = Convert.ToDouble(item.Price);
                command.Parameters.Add("@DiscPercent", SqlDbType.Float).Value = Convert.ToDouble(item.DiscountPercent);
                command.Parameters.Add("@DiscValue", SqlDbType.Float).Value = Convert.ToDouble(item.DiscountValue);
                AddNullable(command, "@ItemName", SqlDbType.NVarChar, item.ItemName, 300);
                command.ExecuteNonQuery();
            }
        }

        private static void LoadCarParts(SqlConnection connection, CarDataDetails model)
        {
            if (model == null || !model.FixedAssetId.HasValue) return;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT d.ID, d.EqupID, d.PartID, f.code PartCode, COALESCE(NULLIF(f.Name,N''), f.namee) PartName
FROM dbo.TblCarsDataDet d
LEFT JOIN dbo.FixedAssets f ON f.id=d.PartID
WHERE d.EqupID=@EquipmentId
ORDER BY d.ID;";
                command.Parameters.Add("@EquipmentId", SqlDbType.Int).Value = model.FixedAssetId.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Parts.Add(new CarPartLine
                        {
                            Id = ReadNullableInt(reader, "ID"),
                            EquipmentId = ReadNullableInt(reader, "EqupID"),
                            PartId = ReadNullableInt(reader, "PartID"),
                            PartCode = ReadString(reader, "PartCode"),
                            PartName = ReadString(reader, "PartName")
                        });
                    }
                }
            }
        }

        private static void SaveCarParts(SqlConnection connection, SqlTransaction transaction, CarDataDetails request)
        {
            if (!request.FixedAssetId.HasValue) return;
            Execute(connection, transaction, "DELETE FROM dbo.TblCarsDataDet WHERE EqupID=@EquipmentId", P("@EquipmentId", SqlDbType.Int, request.FixedAssetId.Value));
            if (request.Parts == null) return;
            foreach (var part in request.Parts.Where(x => x != null && x.PartId.HasValue))
            {
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT INTO dbo.TblCarsDataDet (EqupID, PartID) VALUES (@EquipmentId, @PartId);";
                    command.Parameters.Add("@EquipmentId", SqlDbType.Int).Value = request.FixedAssetId.Value;
                    command.Parameters.Add("@PartId", SqlDbType.Int).Value = part.PartId.Value;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static CarDataDetails ReadCarData(IDataRecord reader)
        {
            return new CarDataDetails
            {
                Id = ReadInt(reader, "id"),
                Code = ReadString(reader, "code"),
                FullCode = ReadString(reader, "Fullcode"),
                Prefix = ReadString(reader, "prifix"),
                BranchId = ReadNullableInt(reader, "Branch_NO"),
                CarTypeId = ReadNullableInt(reader, "CarsTypeId"),
                CarModelId = ReadNullableInt(reader, "VModel"),
                ColorId = ReadNullableInt(reader, "VColor"),
                EmployeeId = ReadNullableInt(reader, "Emp_id"),
                FixedAssetId = ReadNullableInt(reader, "fixedAssetid"),
                Name = ReadString(reader, "Name"),
                BoardNo = ReadString(reader, "BoardNO"),
                LicenseNo = ReadString(reader, "LicenseNO"),
                Model = ReadString(reader, "Model"),
                EquipmentName = ReadString(reader, "EqupName"),
                LeaderName = ReadString(reader, "LeaderName"),
                PurchaseDate = ReadDate(reader, "PurchaseDate"),
                LicenseExpireDate = ReadDate(reader, "LicenseExpireDate"),
                InsuranceExpireDate = ReadDate(reader, "InsuranceExpireDate"),
                TestExpireDate = ReadDate(reader, "TestExpireDate"),
                LastKMCounter = ReadDecimal(reader, "LastKMCounter"),
                Capacity = ReadDecimal(reader, "Capacity"),
                Rate = ReadDecimal(reader, "Rate"),
                Total = ReadDecimal(reader, "Total"),
                EngineNo = ReadString(reader, "Machineno"),
                Chassis = ReadString(reader, "Chesis"),
                GearNo = ReadString(reader, "Gearno"),
                Notes = ReadString(reader, "Notes"),
                FormOriginal = ReadBool(reader, "FormOrignal"),
                AuthorizeLicense = ReadBool(reader, "authorizeLicense"),
                AuthorizeExamination = ReadBool(reader, "authorizeExamination"),
                SpareTyre = ReadBool(reader, "SpareTyre"),
                Battery = ReadBool(reader, "Battery"),
                Guarantee = ReadBool(reader, "Guarantee")
            };
        }

        private static CarAuthorizationDetails ReadCarAuthorization(IDataRecord reader)
        {
            return new CarAuthorizationDetails
            {
                Id = ReadInt(reader, "ID"),
                RecordDate = ReadDate(reader, "RecordDate"),
                EndDate = ReadDate(reader, "EndDate"),
                BranchId = ReadNullableInt(reader, "BranchID"),
                UserId = ReadNullableInt(reader, "UserID"),
                CustomerId = ReadNullableInt(reader, "CusID"),
                ClientCode = ReadString(reader, "ClientCode"),
                ClientName = ReadString(reader, "ClientName"),
                Telephone = ReadString(reader, "Telephone"),
                Mobile = ReadString(reader, "mobile"),
                CarId = ReadNullableInt(reader, "CarID"),
                CarTypeId = ReadNullableInt(reader, "CarTypeID"),
                CarModelId = ReadNullableInt(reader, "CarModelID"),
                ColorId = ReadNullableInt(reader, "ColorID"),
                PlateNo = ReadString(reader, "PlateNo"),
                YearFact = ReadNullableInt(reader, "YearFact"),
                OrderStatus = ReadNullableInt(reader, "OrderStatus") ?? 0,
                CarMeter = ReadDecimal(reader, "CarMeter"),
                CarMeterOut = ReadDecimal(reader, "CarMetarOut"),
                Complaint = ReadString(reader, "Complaint"),
                InitialNote = ReadString(reader, "Noteinitial"),
                Chassis = ReadString(reader, "Shaseh"),
                WorkOrder = ReadDecimal(reader, "WorkOrder"),
                AuthOrder = ReadDecimal(reader, "AuthoOrder"),
                SparePart = ReadString(reader, "SparePart"),
                Cash = ReadBool(reader, "Cash"),
                Account = ReadBool(reader, "Accoun"),
                Credit = ReadBool(reader, "credit"),
                Accepted = ReadBool(reader, "Accept"),
                Finished = ReadBool(reader, "finish"),
                Waiting = ReadBool(reader, "wait"),
                SendSms = ReadBool(reader, "SendSMS"),
                QrCodeData = ReadString(reader, "QrCodeData"),
                QrCodeDataPath = ReadString(reader, "QrCodeDataPath"),
                SubCar1 = ReadBool(reader, "subcar1"),
                SubCar2 = ReadBool(reader, "subcar2"),
                SubCar3 = ReadBool(reader, "subcar3"),
                SubCar4 = ReadBool(reader, "subcar4"),
                SubCar5 = ReadBool(reader, "subcar5"),
                SubCar6 = ReadBool(reader, "subcar6"),
                SubCar7 = ReadBool(reader, "subcar7"),
                SubCar8 = ReadBool(reader, "subcar8"),
                SubCar9 = ReadBool(reader, "subcar9"),
                SubCar10 = ReadBool(reader, "subcar10"),
                SubCar11 = ReadBool(reader, "subcar11"),
                SubCar12 = ReadBool(reader, "subcar12"),
                SubCar13 = ReadBool(reader, "subcar13"),
                SubCar14 = ReadBool(reader, "subcar14"),
                DiscountValue = ReadDecimal(reader, "DiscValue"),
                DiscountPercent = ReadDecimal(reader, "DiscPercent"),
                TotalAfterDiscount = ReadDecimal(reader, "TotalAfterDiscount"),
                VatPercent = ReadDecimal(reader, "Vatyo"),
                VatValue = ReadDecimal(reader, "Vat2")
            };
        }

        private static LegacyAttachmentItem ReadAttachment(IDataRecord reader)
        {
            return new LegacyAttachmentItem
            {
                Id = ReadInt(reader, "Id"),
                ScreenName = ReadString(reader, "ScreenName"),
                RecordId = ReadInt(reader, "RecordId"),
                FileName = ReadString(reader, "FileName"),
                FilePath = ReadString(reader, "FilePath"),
                ContentType = ReadString(reader, "ContentType"),
                FileSize = Convert.ToInt64(reader["FileSize"]),
                IsPrimary = ReadBool(reader, "IsPrimary"),
                Caption = ReadString(reader, "Caption"),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        private static CarAuthorizationLine ReadCarAuthorizationLine(IDataRecord reader)
        {
            return new CarAuthorizationLine
            {
                Type = ReadNullableInt(reader, "Type") ?? 0,
                MainteId = ReadNullableInt(reader, "Mainte"),
                Name = ReadString(reader, "ItemName"),
                Value = ReadDecimal(reader, "Value"),
                Count = ReadNullableInt(reader, "count") ?? 1,
                Company = ReadString(reader, "comp"),
                BillNo = ReadString(reader, "bill"),
                Hours = ReadDecimal(reader, "nohours"),
                Finish = ReadBool(reader, "finish"),
                DateEnter = ReadDate(reader, "DateEnter"),
                DateExit = ReadDate(reader, "DateExit"),
                TimeEnter = ReadString(reader, "TimeEnter"),
                TimeOut = ReadString(reader, "TimOut"),
                EmployeeId = ReadNullableInt(reader, "EmpID"),
                SupervisorId = ReadNullableInt(reader, "empsuper"),
                DepartmentId = ReadNullableInt(reader, "Deptid"),
                PriceFitter = ReadDecimal(reader, "PriceFitter"),
                DepartmentColor = ReadString(reader, "DeptColor")
            };
        }

        private static CarAuthorizationItem ReadCarAuthorizationItem(IDataRecord reader)
        {
            return new CarAuthorizationItem
            {
                ItemId = ReadNullableInt(reader, "ItemID"),
                ItemName = string.IsNullOrWhiteSpace(ReadString(reader, "ItemName")) ? ReadString(reader, "ItemName2") : ReadString(reader, "ItemName"),
                Qty = ReadDecimal(reader, "qty"),
                Price = ReadDecimal(reader, "Price"),
                VatPercent = ReadDecimal(reader, "Vatyo"),
                VatValue = ReadDecimal(reader, "Vat2"),
                TotalWithVat = ReadDecimal(reader, "TotalWithVat"),
                BeforeVat = ReadDecimal(reader, "beforeVat"),
                DiscountPercent = ReadDecimal(reader, "DiscPercent"),
                DiscountValue = ReadDecimal(reader, "DiscValue"),
                Remark = ReadString(reader, "Remark")
            };
        }

        private static CarMaintenanceDetails ReadCarMaintenance(IDataRecord reader)
        {
            return new CarMaintenanceDetails
            {
                Id = ReadInt(reader, "ID"),
                NoteId = ReadNullableInt(reader, "NoteID"),
                JournalSerial = ReadString(reader, "NoteSerial"),
                VoucherSerial = ReadString(reader, "NoteSerial1"),
                RecordDate = ReadDate(reader, "RecordDate"),
                EndDate = ReadDate(reader, "EndDate"),
                BranchId = ReadNullableInt(reader, "BranchID"),
                CustomerId = ReadNullableInt(reader, "CusID"),
                ClientName = ReadString(reader, "ClientName"),
                Telephone = ReadString(reader, "Telephone"),
                CarTypeId = ReadNullableInt(reader, "CarTypeID"),
                CarModelId = ReadNullableInt(reader, "CarModelID"),
                ColorId = ReadNullableInt(reader, "ColorID"),
                PlateNo = ReadString(reader, "PlateNo"),
                YearFact = ReadNullableInt(reader, "YearFact"),
                WorkOrderNo = ReadString(reader, "WorkOrderNO"),
                PaymentType = ReadNullableInt(reader, "PaymentType") ?? 0,
                BoxId = ReadNullableInt(reader, "BoxID"),
                PaymentValue = ReadDecimal(reader, "PaymentValue"),
                Discount = ReadDecimal(reader, "Trans_Discount"),
                DiscountType = ReadNullableInt(reader, "Trans_DiscountType") ?? 0,
                VatPercent = ReadDecimal(reader, "FATYou"),
                VatValue = ReadDecimal(reader, "FATValue"),
                TotalValue = ReadDecimal(reader, "TotalValue"),
                VatAccountCode = ReadString(reader, "AccountCodeVat"),
                SparePart = ReadString(reader, "SparePart")
            };
        }

        private static CarMaintenanceLine ReadCarLine(IDataRecord reader)
        {
            return new CarMaintenanceLine
            {
                Type = ReadNullableInt(reader, "Type") ?? 0,
                MainteId = ReadNullableInt(reader, "Mainte"),
                Name = ReadString(reader, "ItemName"),
                Value = ReadDecimal(reader, "Value"),
                Count = ReadNullableInt(reader, "count") ?? 1,
                DiscountValue = ReadDecimal(reader, "DiscValue"),
                Percentage = ReadDecimal(reader, "Percentage"),
                TotalNet = ReadDecimal(reader, "TotalNet"),
                AccountCode = ReadString(reader, "AccountCode"),
                DepartmentId = ReadNullableInt(reader, "Deptid"),
                EmployeeId = ReadNullableInt(reader, "Emp_ID"),
                Company = ReadString(reader, "comp"),
                BillNo = ReadString(reader, "bill")
            };
        }

        private static bool DuplicateBoxName(SqlConnection connection, SqlTransaction transaction, string name, int? id)
        {
            return ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.TblBoxesData WHERE BoxName=@Name AND (@Id IS NULL OR BoxID<>@Id)",
                P("@Name", SqlDbType.NVarChar, name, 200), P("@Id", SqlDbType.Int, id)) > 0;
        }

        private static bool DuplicateCarData(SqlConnection connection, SqlTransaction transaction, string column, string value, int? id)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return ScalarInt(connection, transaction, "SELECT COUNT(1) FROM dbo.TblCarsData WHERE " + column + "=@Value AND (@Id IS NULL OR id<>@Id)",
                P("@Value", SqlDbType.NVarChar, value.Trim(), 200), P("@Id", SqlDbType.Int, id)) > 0;
        }

        private static int NextInt(SqlConnection connection, SqlTransaction transaction, string table, string column, string where)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT ISNULL(MAX(" + column + "),0)+1 FROM dbo." + table + " WITH (UPDLOCK,HOLDLOCK)" + (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static string NextNumber(SqlConnection connection, SqlTransaction transaction, string table, string column, string where)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT CONVERT(varchar(50), CONVERT(decimal(38,0), ISNULL(MAX(CASE WHEN ISNUMERIC(" + column + ")=1 THEN CONVERT(decimal(38,0)," + column + ") ELSE 0 END),0)+1)) FROM dbo." + table + " WITH (UPDLOCK,HOLDLOCK)" + (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where);
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }

        private static int ScalarInt(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static string ScalarString(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = sql;
                command.Parameters.AddRange(parameters);
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? null : Convert.ToString(value);
            }
        }

        private static SqlParameter P(string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
            parameter.Value = value ?? DBNull.Value;
            return parameter;
        }

        private static LegacySaveResult Ok(string message, int id) { return new LegacySaveResult { Success = true, Message = message, Id = id }; }
        private static LegacySaveResult Fail(string message) { return new LegacySaveResult { Success = false, Message = message }; }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
        {
            var p = size > 0 ? command.Parameters.Add(name, type, size) : command.Parameters.Add(name, type);
            p.Value = value == null || value is string && string.IsNullOrWhiteSpace((string)value) ? DBNull.Value : value;
        }

        private static string ReadString(IDataRecord record, string name) { var value = record[name]; return value == DBNull.Value ? string.Empty : Convert.ToString(value); }
        private static int ReadInt(IDataRecord record, string name) { return Convert.ToInt32(record[name]); }
        private static int? ReadNullableInt(IDataRecord record, string name) { var value = record[name]; return value == DBNull.Value ? (int?)null : Convert.ToInt32(value); }
        private static decimal ReadDecimal(IDataRecord record, string name) { var value = record[name]; return value == DBNull.Value ? 0 : Convert.ToDecimal(value); }
        private static DateTime? ReadDate(IDataRecord record, string name) { var value = record[name]; return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value); }
        private static bool ReadBool(IDataRecord record, string name) { var value = record[name]; return value != DBNull.Value && Convert.ToBoolean(value); }
    }
}
