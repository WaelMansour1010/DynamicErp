using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.FinancialAdministration;

namespace MyERP.Areas.MainErp.Repositories.FinancialAdministration
{
    public class FinancialAdministrationRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public FinancialAdministrationRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public FinancialAdministrationIndexViewModel LoadIndex(FinancialAdministrationSearchViewModel search)
        {
            search = Normalize(search);
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var model = new FinancialAdministrationIndexViewModel
                {
                    Search = search,
                    Summary = LoadSummary(connection),
                    Banks = LoadBanks(connection, search),
                    Boxes = LoadBoxes(connection, search),
                    Branches = LoadLookup(connection, "SELECT TOP (500) CONVERT(NVARCHAR(50), branch_id) Id, COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N''), CONVERT(NVARCHAR(20), branch_id)) Name, branch_Code Code FROM dbo.TblBranchesData WITH (NOLOCK) ORDER BY branch_name", true),
                    Accounts = new List<FinancialLookupViewModel>(),
                    Currencies = LoadLookup(connection, "SELECT TOP (100) CONVERT(NVARCHAR(50), id) Id, COALESCE(NULLIF(code, N''), NULLIF(name, N''), CONVERT(NVARCHAR(20), id)) Name, code Code FROM dbo.currency WITH (NOLOCK) ORDER BY code", true),
                    Employees = LoadLookup(connection, "SELECT TOP (500) CONVERT(NVARCHAR(50), Emp_ID) Id, COALESCE(NULLIF(Emp_Name, N''), CONVERT(NVARCHAR(20), Emp_ID)) Name, Emp_Code Code FROM dbo.TblEmployee WITH (NOLOCK) ORDER BY Emp_Name", true),
                    CurrencyBreakdown = LoadCurrencyBreakdown(connection),
                    BranchBreakdown = LoadBranchBreakdown(connection),
                    RecentBankMovements = LoadRecentBankMovements(connection),
                    RecentBoxMovements = LoadRecentBoxMovements(connection)
                };

                return model;
            }
        }

        public FinancialBankEditViewModel GetBank(int bankId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("SELECT TOP (1) * FROM dbo.BanksData WITH (NOLOCK) WHERE BankID = @Id", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = bankId;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapBankEdit(reader) : null;
                }
            }
        }

        public FinancialBoxEditViewModel GetBox(int boxId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("SELECT TOP (1) * FROM dbo.tblBoxesData WITH (NOLOCK) WHERE BoxID = @Id", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = boxId;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapBoxEdit(reader) : null;
                }
            }
        }

        public IList<FinancialLookupViewModel> SearchAccounts(string term, int limit)
        {
            term = (term ?? string.Empty).Trim();
            limit = limit <= 0 || limit > 50 ? 20 : limit;
            if (term.Length < 2)
            {
                return new List<FinancialLookupViewModel>();
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT TOP (@Limit)
    Account_Code Id,
    COALESCE(NULLIF(Account_Name, N''), Account_Code) Name,
    Account_Code Code
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE ISNULL(Account_Code, N'') <> N''
  AND (
        Account_Code LIKE @Prefix
     OR Account_Code LIKE @Contains
     OR Account_Name LIKE @Contains
     OR Account_NameEng LIKE @Contains
  )
ORDER BY
    CASE WHEN Account_Code LIKE @Prefix THEN 0 ELSE 1 END,
    Account_Code;", connection))
            {
                command.Parameters.Add("@Limit", SqlDbType.Int).Value = limit;
                command.Parameters.Add("@Prefix", SqlDbType.NVarChar, 100).Value = term + "%";
                command.Parameters.Add("@Contains", SqlDbType.NVarChar, 100).Value = "%" + term + "%";
                return LoadLookup(command);
            }
        }

        public FinancialAdministrationSaveResult SaveBank(FinancialBankEditViewModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BankName))
            {
                return Fail("\u0627\u0633\u0645 \u0627\u0644\u0628\u0646\u0643 \u0645\u0637\u0644\u0648\u0628.");
            }

            if (string.IsNullOrWhiteSpace(request.AccountCode))
            {
                return Fail("\u062d\u0633\u0627\u0628 \u0627\u0644\u0628\u0646\u0643 \u0645\u0637\u0644\u0648\u0628.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var id = request.BankId.GetValueOrDefault();
                var referenceValidation = ValidateFinancialReferences(connection, transaction, request.AccountCode, request.BranchId, request.CurrencyId, null);
                if (!string.IsNullOrWhiteSpace(referenceValidation))
                {
                    transaction.Rollback();
                    return Fail(referenceValidation);
                }

                if (DuplicateExists(connection, transaction, "BanksData", "BankID", id, "BankName", request.BankName))
                {
                    return Fail("\u0627\u0633\u0645 \u0627\u0644\u0628\u0646\u0643 \u0645\u0648\u062c\u0648\u062f \u0645\u0646 \u0642\u0628\u0644.");
                }

                if (id <= 0)
                {
                    id = NextId(connection, transaction, "BanksData", "BankID");
                    using (var command = new SqlCommand(@"
INSERT INTO dbo.BanksData
(BankID, BankName, BankNamee, Remarks, Account_Code, Account_Code1, Account_Code2, Account_code3, ParetnAccount, BranchId,
 Commision, OpenBalanceDate, OpenBalanceType, OpenBalance, account_no, IBan, Branch_NO, Tel, Address, Email, Currency_ID, chkapprov, chkLoan, parent_account)
VALUES
(@Id, @Name, @NameE, @Remarks, @AccountCode, @AccountCode1, @AccountCode2, @AccountCode3, @ParentAccount, @BranchId,
 @Commission, GETDATE(), @OpenBalanceType, @OpenBalance, @AccountNo, @Iban, @BranchNo, @Tel, @Address, @Email, @CurrencyId, @Approval, @Loan, @ParentAccount);", connection, transaction))
                    {
                        AddBankParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new SqlCommand(@"
UPDATE dbo.BanksData
SET BankName = @Name,
    BankNamee = @NameE,
    Remarks = @Remarks,
    Account_Code = @AccountCode,
    Account_Code1 = @AccountCode1,
    Account_Code2 = @AccountCode2,
    Account_code3 = @AccountCode3,
    ParetnAccount = @ParentAccount,
    parent_account = @ParentAccount,
    BranchId = @BranchId,
    Commision = @Commission,
    OpenBalanceType = @OpenBalanceType,
    OpenBalance = @OpenBalance,
    account_no = @AccountNo,
    IBan = @Iban,
    Branch_NO = @BranchNo,
    Tel = @Tel,
    Address = @Address,
    Email = @Email,
    Currency_ID = @CurrencyId,
    chkapprov = @Approval,
    chkLoan = @Loan
WHERE BankID = @Id;", connection, transaction))
                    {
                        AddBankParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return new FinancialAdministrationSaveResult { Success = true, Id = id, Message = "\u062a\u0645 \u062d\u0641\u0638 \u0627\u0644\u0628\u0646\u0643 \u0628\u0646\u062c\u0627\u062d." };
            }
        }

        public FinancialAdministrationSaveResult SaveBox(FinancialBoxEditViewModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.BoxName))
            {
                return Fail("\u0627\u0633\u0645 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0645\u0637\u0644\u0648\u0628.");
            }

            if (string.IsNullOrWhiteSpace(request.AccountCode))
            {
                return Fail("\u062d\u0633\u0627\u0628 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0645\u0637\u0644\u0648\u0628.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var id = request.BoxId.GetValueOrDefault();
                var referenceValidation = ValidateFinancialReferences(connection, transaction, request.AccountCode, request.BranchId, null, request.EmployeeId);
                if (!string.IsNullOrWhiteSpace(referenceValidation))
                {
                    transaction.Rollback();
                    return Fail(referenceValidation);
                }

                if (DuplicateExists(connection, transaction, "tblBoxesData", "BoxID", id, "BoxName", request.BoxName))
                {
                    return Fail("\u0627\u0633\u0645 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0645\u0648\u062c\u0648\u062f \u0645\u0646 \u0642\u0628\u0644.");
                }

                if (id <= 0)
                {
                    id = NextId(connection, transaction, "tblBoxesData", "BoxID");
                    using (var command = new SqlCommand(@"
INSERT INTO dbo.tblBoxesData
(BoxID, BoxName, BoxNameE, Comments, Account_Code, Account_Code1, Account_Code2, ParentAccount, parent_account, Type,
 empid, BranchId, ChequeBox, OpenBalanceDate, OpenBalanceType, OpenBalance, DriverId, BTtype, boxValue, Priod, PriodDMY)
VALUES
(@Id, @Name, @NameE, @Comments, @AccountCode, @AccountCode1, @AccountCode2, @ParentAccount, @ParentAccount, @Type,
 @EmployeeId, @BranchId, @ChequeBox, @OpenBalanceDate, @OpenBalanceType, @OpenBalance, @DriverId, @BTtype, @BoxValue, @Period, @PeriodMode);", connection, transaction))
                    {
                        AddBoxParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new SqlCommand(@"
UPDATE dbo.tblBoxesData
SET BoxName = @Name,
    BoxNameE = @NameE,
    Comments = @Comments,
    Account_Code = @AccountCode,
    Account_Code1 = @AccountCode1,
    Account_Code2 = @AccountCode2,
    ParentAccount = @ParentAccount,
    parent_account = @ParentAccount,
    Type = @Type,
    empid = @EmployeeId,
    BranchId = @BranchId,
    ChequeBox = @ChequeBox,
    OpenBalanceDate = @OpenBalanceDate,
    OpenBalanceType = @OpenBalanceType,
    OpenBalance = @OpenBalance,
    DriverId = @DriverId,
    BTtype = @BTtype,
    boxValue = @BoxValue,
    Priod = @Period,
    PriodDMY = @PeriodMode
WHERE BoxID = @Id;", connection, transaction))
                    {
                        AddBoxParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return new FinancialAdministrationSaveResult { Success = true, Id = id, Message = "\u062a\u0645 \u062d\u0641\u0638 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0628\u0646\u062c\u0627\u062d." };
            }
        }

        private FinancialAdministrationSummaryViewModel LoadSummary(SqlConnection connection)
        {
            var hasWallet = ColumnExists(connection, "tblBoxesData", "IsWallet");
            var hasTerminal = ColumnExists(connection, "tblBoxesData", "IsTerminalPOS");
            var sql = @"
SELECT
    BanksCount = (SELECT COUNT(1) FROM dbo.BanksData WITH (NOLOCK)),
    BoxesCount = (SELECT COUNT(1) FROM dbo.tblBoxesData WITH (NOLOCK)),
    LinkedBankAccountsCount = (SELECT COUNT(1) FROM dbo.BanksData WITH (NOLOCK) WHERE ISNULL(Account_Code, N'') <> N''),
    LinkedBoxAccountsCount = (SELECT COUNT(1) FROM dbo.tblBoxesData WITH (NOLOCK) WHERE ISNULL(Account_Code, N'') <> N''),
    BankOpeningBalance = ISNULL((SELECT SUM(ISNULL(OpenBalance, 0)) FROM dbo.BanksData WITH (NOLOCK)), 0),
    BoxOpeningBalance = ISNULL((SELECT SUM(ISNULL(OpenBalance, 0)) FROM dbo.tblBoxesData WITH (NOLOCK)), 0);";

            var summary = new FinancialAdministrationSummaryViewModel();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    summary.BanksCount = ReadInt(reader, "BanksCount");
                    summary.BoxesCount = ReadInt(reader, "BoxesCount");
                    summary.LinkedBankAccountsCount = ReadInt(reader, "LinkedBankAccountsCount");
                    summary.LinkedBoxAccountsCount = ReadInt(reader, "LinkedBoxAccountsCount");
                    summary.BankOpeningBalance = ReadDecimal(reader, "BankOpeningBalance");
                    summary.BoxOpeningBalance = ReadDecimal(reader, "BoxOpeningBalance");
                }
            }

            summary.WalletBoxesCount = hasWallet ? ScalarInt(connection, "SELECT COUNT(1) FROM dbo.tblBoxesData WITH (NOLOCK) WHERE ISNULL(IsWallet, 0) = 1") : 0;
            summary.PosTerminalBoxesCount = hasTerminal ? ScalarInt(connection, "SELECT COUNT(1) FROM dbo.tblBoxesData WITH (NOLOCK) WHERE ISNULL(IsTerminalPOS, 0) = 1") : 0;
            return summary;
        }

        private IList<FinancialBankListItemViewModel> LoadBanks(SqlConnection connection, FinancialAdministrationSearchViewModel search)
        {
            const string sql = @"
SELECT *
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY b.BankID) AS RowNo,
           b.BankID,
           b.BankName,
           b.BankNamee,
           b.Account_Code,
           a.Account_Name,
           br.branch_name,
           c.code AS CurrencyCode,
           b.IBan,
           b.Tel,
           b.OpenBalance,
           b.OpenBalanceType,
           b.chkapprov,
           b.chkLoan,
           LastMovementDate = (SELECT MAX(n.NoteDate) FROM dbo.notes n WITH (NOLOCK) WHERE n.BankID = b.BankID),
           LastMovementValue = ISNULL((SELECT TOP (1) n.Note_Value FROM dbo.notes n WITH (NOLOCK) WHERE n.BankID = b.BankID ORDER BY n.NoteDate DESC, n.NoteID DESC), 0)
    FROM dbo.BanksData b WITH (NOLOCK)
    LEFT JOIN dbo.TblBranchesData br WITH (NOLOCK) ON br.branch_id = b.BranchId
    LEFT JOIN dbo.currency c WITH (NOLOCK) ON c.id = b.Currency_ID
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = b.Account_Code
    WHERE (@Search = N''
       OR ISNULL(b.BankName, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(b.BankNamee, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(b.Account_Code, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(b.IBan, N'') LIKE N'%' + @Search + N'%')
) q
WHERE q.RowNo BETWEEN @StartRow AND @EndRow
ORDER BY q.RowNo;";

            var rows = new List<FinancialBankListItemViewModel>();
            using (var command = new SqlCommand(sql, connection))
            {
                AddPagingParameters(command, search);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new FinancialBankListItemViewModel
                        {
                            BankId = ReadInt(reader, "BankID"),
                            BankName = ReadString(reader, "BankName"),
                            BankNameEnglish = ReadString(reader, "BankNamee"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            BranchName = ReadString(reader, "branch_name"),
                            CurrencyCode = ReadString(reader, "CurrencyCode"),
                            Iban = ReadString(reader, "IBan"),
                            Telephone = ReadString(reader, "Tel"),
                            OpeningBalance = ReadDecimal(reader, "OpenBalance"),
                            OpeningBalanceType = ReadNullableInt(reader, "OpenBalanceType"),
                            ApprovalRequired = ReadBool(reader, "chkapprov"),
                            LoanBank = ReadBool(reader, "chkLoan"),
                            LastMovementDate = ReadNullableDate(reader, "LastMovementDate"),
                            LastMovementValue = ReadDecimal(reader, "LastMovementValue")
                        });
                    }
                }
            }

            return rows;
        }

        private IList<FinancialBoxListItemViewModel> LoadBoxes(SqlConnection connection, FinancialAdministrationSearchViewModel search)
        {
            var hasWallet = ColumnExists(connection, "tblBoxesData", "IsWallet");
            var hasTerminal = ColumnExists(connection, "tblBoxesData", "IsTerminalPOS");
            var sql = @"
SELECT *
FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY bx.BoxID) AS RowNo,
           bx.BoxID,
           bx.BoxName,
           bx.BoxNameE,
           bx.Account_Code,
           a.Account_Name,
           br.branch_name,
           e.Emp_Name,
           bx.Comments,
           bx.Type,
           bx.OpenBalanceType,
           bx.OpenBalance,
           bx.boxValue,
           bx.ChequeBox,
           IsWalletValue = " + (hasWallet ? "ISNULL(bx.IsWallet, 0)" : "CONVERT(bit, 0)") + @",
           IsTerminalValue = " + (hasTerminal ? "ISNULL(bx.IsTerminalPOS, 0)" : "CONVERT(bit, 0)") + @",
           LastMovementDate = (SELECT MAX(n.NoteDate) FROM dbo.notes n WITH (NOLOCK) WHERE n.BoxID = bx.BoxID),
           LastMovementValue = ISNULL((SELECT TOP (1) n.Note_Value FROM dbo.notes n WITH (NOLOCK) WHERE n.BoxID = bx.BoxID ORDER BY n.NoteDate DESC, n.NoteID DESC), 0)
    FROM dbo.tblBoxesData bx WITH (NOLOCK)
    LEFT JOIN dbo.TblBranchesData br WITH (NOLOCK) ON br.branch_id = bx.BranchId
    LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = bx.empid
    LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = bx.Account_Code
    WHERE (@Search = N''
       OR ISNULL(bx.BoxName, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(bx.BoxNameE, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(bx.Account_Code, N'') LIKE N'%' + @Search + N'%'
       OR ISNULL(e.Emp_Name, N'') LIKE N'%' + @Search + N'%')
) q
WHERE q.RowNo BETWEEN @StartRow AND @EndRow
ORDER BY q.RowNo;";

            var rows = new List<FinancialBoxListItemViewModel>();
            using (var command = new SqlCommand(sql, connection))
            {
                AddPagingParameters(command, search);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new FinancialBoxListItemViewModel
                        {
                            BoxId = ReadInt(reader, "BoxID"),
                            BoxName = ReadString(reader, "BoxName"),
                            BoxNameEnglish = ReadString(reader, "BoxNameE"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountName = ReadString(reader, "Account_Name"),
                            BranchName = ReadString(reader, "branch_name"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            Comments = ReadString(reader, "Comments"),
                            Type = ReadNullableInt(reader, "Type"),
                            BalanceType = ReadNullableInt(reader, "OpenBalanceType"),
                            OpeningBalance = ReadDecimal(reader, "OpenBalance"),
                            LimitValue = ReadDecimal(reader, "boxValue"),
                            HasChequeBox = ReadBool(reader, "ChequeBox"),
                            IsWallet = ReadBool(reader, "IsWalletValue"),
                            IsTerminalPos = ReadBool(reader, "IsTerminalValue"),
                            LastMovementDate = ReadNullableDate(reader, "LastMovementDate"),
                            LastMovementValue = ReadDecimal(reader, "LastMovementValue")
                        });
                    }
                }
            }

            return rows;
        }

        private IList<FinancialMetricViewModel> LoadCurrencyBreakdown(SqlConnection connection)
        {
            const string sql = @"
SELECT TOP (12)
    KeyValue = CONVERT(NVARCHAR(50), ISNULL(c.id, 0)),
    NameValue = COALESCE(NULLIF(c.code, N''), NULLIF(c.name, N''), N'Unknown currency'),
    CountValue = COUNT(1),
    SumValue = ISNULL(SUM(ISNULL(b.OpenBalance, 0)), 0)
FROM dbo.BanksData b WITH (NOLOCK)
LEFT JOIN dbo.currency c WITH (NOLOCK) ON c.id = b.Currency_ID
GROUP BY c.id, c.code, c.name
ORDER BY COUNT(1) DESC;";
            return LoadMetrics(connection, sql);
        }

        private IList<FinancialMetricViewModel> LoadBranchBreakdown(SqlConnection connection)
        {
            const string sql = @"
SELECT TOP (12)
    KeyValue = CONVERT(NVARCHAR(50), ISNULL(br.branch_id, 0)),
    NameValue = COALESCE(NULLIF(br.branch_name, N''), NULLIF(br.branch_namee, N''), N'Unknown branch'),
    CountValue = COUNT(1),
    SumValue = ISNULL(SUM(ISNULL(x.OpenBalance, 0)), 0)
FROM (
    SELECT BranchId, OpenBalance FROM dbo.BanksData WITH (NOLOCK)
    UNION ALL
    SELECT BranchId, OpenBalance FROM dbo.tblBoxesData WITH (NOLOCK)
) x
LEFT JOIN dbo.TblBranchesData br WITH (NOLOCK) ON br.branch_id = x.BranchId
GROUP BY br.branch_id, br.branch_name, br.branch_namee
ORDER BY COUNT(1) DESC;";
            return LoadMetrics(connection, sql);
        }

        private IList<FinancialMovementViewModel> LoadRecentBankMovements(SqlConnection connection)
        {
            const string sql = @"
SELECT TOP (8) n.NoteID, n.NoteDate, b.BankName, n.NoteSerial, n.Note_Value, n.Remark
FROM dbo.notes n WITH (NOLOCK)
INNER JOIN dbo.BanksData b WITH (NOLOCK) ON b.BankID = n.BankID
WHERE n.BankID IS NOT NULL
ORDER BY n.NoteDate DESC, n.NoteID DESC;";
            return LoadMovements(connection, sql, "BankName");
        }

        private IList<FinancialMovementViewModel> LoadRecentBoxMovements(SqlConnection connection)
        {
            const string sql = @"
SELECT TOP (8) n.NoteID, n.NoteDate, bx.BoxName, n.NoteSerial, n.Note_Value, n.Remark
FROM dbo.notes n WITH (NOLOCK)
INNER JOIN dbo.tblBoxesData bx WITH (NOLOCK) ON bx.BoxID = n.BoxID
WHERE n.BoxID IS NOT NULL
ORDER BY n.NoteDate DESC, n.NoteID DESC;";
            return LoadMovements(connection, sql, "BoxName");
        }

        private IList<FinancialMetricViewModel> LoadMetrics(SqlConnection connection, string sql)
        {
            var rows = new List<FinancialMetricViewModel>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new FinancialMetricViewModel
                    {
                        Key = ReadString(reader, "KeyValue"),
                        Name = ReadString(reader, "NameValue"),
                        Count = ReadInt(reader, "CountValue"),
                        Value = ReadDecimal(reader, "SumValue")
                    });
                }
            }

            return rows;
        }

        private IList<FinancialMovementViewModel> LoadMovements(SqlConnection connection, string sql, string sourceColumn)
        {
            var rows = new List<FinancialMovementViewModel>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new FinancialMovementViewModel
                    {
                        Id = ReadInt(reader, "NoteID"),
                        MovementDate = ReadNullableDate(reader, "NoteDate"),
                        SourceName = ReadString(reader, sourceColumn),
                        Serial = ReadString(reader, "NoteSerial"),
                        Value = ReadDecimal(reader, "Note_Value"),
                        Remarks = ReadString(reader, "Remark")
                    });
                }
            }

            return rows;
        }

        private IList<FinancialLookupViewModel> LoadLookup(SqlConnection connection, string sql, bool ignoreSqlErrors)
        {
            var rows = new List<FinancialLookupViewModel>();
            try
            {
                using (var command = new SqlCommand(sql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new FinancialLookupViewModel
                        {
                            Id = ReadString(reader, "Id"),
                            Name = ReadString(reader, "Name"),
                            Code = ReadString(reader, "Code")
                        });
                    }
                }
            }
            catch (SqlException)
            {
                if (!ignoreSqlErrors)
                {
                    throw;
                }
            }

            return rows;
        }

        private IList<FinancialLookupViewModel> LoadLookup(SqlCommand command)
        {
            var rows = new List<FinancialLookupViewModel>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new FinancialLookupViewModel
                    {
                        Id = ReadString(reader, "Id"),
                        Name = ReadString(reader, "Name"),
                        Code = ReadString(reader, "Code")
                    });
                }
            }

            return rows;
        }

        private static FinancialBankEditViewModel MapBankEdit(IDataRecord reader)
        {
            return new FinancialBankEditViewModel
            {
                BankId = ReadNullableInt(reader, "BankID"),
                BankName = ReadString(reader, "BankName"),
                BankNameEnglish = ReadString(reader, "BankNamee"),
                Remarks = ReadString(reader, "Remarks"),
                AccountCode = ReadString(reader, "Account_Code"),
                AccountCode1 = ReadString(reader, "Account_Code1"),
                AccountCode2 = ReadString(reader, "Account_Code2"),
                AccountCode3 = ReadString(reader, "Account_code3"),
                ParentAccount = ReadString(reader, "ParetnAccount"),
                BranchId = ReadNullableInt(reader, "BranchId"),
                CurrencyId = ReadNullableInt(reader, "Currency_ID"),
                AccountNo = ReadString(reader, "account_no"),
                Iban = ReadString(reader, "IBan"),
                BranchNo = ReadString(reader, "Branch_NO"),
                Telephone = ReadString(reader, "Tel"),
                Address = ReadString(reader, "Address"),
                Email = ReadString(reader, "Email"),
                OpeningBalance = ReadDecimal(reader, "OpenBalance"),
                OpeningBalanceType = ReadNullableInt(reader, "OpenBalanceType"),
                Commission = ReadDecimal(reader, "Commision"),
                ApprovalRequired = ReadBool(reader, "chkapprov"),
                LoanBank = ReadBool(reader, "chkLoan")
            };
        }

        private static FinancialBoxEditViewModel MapBoxEdit(IDataRecord reader)
        {
            return new FinancialBoxEditViewModel
            {
                BoxId = ReadNullableInt(reader, "BoxID"),
                BoxName = ReadString(reader, "BoxName"),
                BoxNameEnglish = ReadString(reader, "BoxNameE"),
                Comments = ReadString(reader, "Comments"),
                AccountCode = ReadString(reader, "Account_Code"),
                AccountCode1 = ReadString(reader, "Account_Code1"),
                AccountCode2 = ReadString(reader, "Account_Code2"),
                ParentAccount = ReadString(reader, "ParentAccount"),
                Type = ReadNullableInt(reader, "Type"),
                EmployeeId = ReadNullableInt(reader, "empid"),
                BranchId = ReadNullableInt(reader, "BranchId"),
                HasChequeBox = ReadBool(reader, "ChequeBox"),
                OpeningBalanceDate = ReadNullableDate(reader, "OpenBalanceDate"),
                OpeningBalanceType = ReadNullableInt(reader, "OpenBalanceType"),
                OpeningBalance = ReadDecimal(reader, "OpenBalance"),
                DriverId = ReadNullableInt(reader, "DriverId"),
                BalanceType = ReadNullableInt(reader, "BTtype"),
                LimitValue = ReadDecimal(reader, "boxValue"),
                Period = ReadNullableInt(reader, "Priod"),
                PeriodMode = ReadNullableInt(reader, "PriodDMY")
            };
        }

        private static void AddBankParameters(SqlCommand command, FinancialBankEditViewModel request, int id)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = DbText(request.BankName);
            command.Parameters.Add("@NameE", SqlDbType.NVarChar, 255).Value = DbText(request.BankNameEnglish);
            command.Parameters.Add("@Remarks", SqlDbType.NVarChar).Value = DbText(request.Remarks);
            command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode);
            command.Parameters.Add("@AccountCode1", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode1);
            command.Parameters.Add("@AccountCode2", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode2);
            command.Parameters.Add("@AccountCode3", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode3);
            command.Parameters.Add("@ParentAccount", SqlDbType.NVarChar, 50).Value = DbText(request.ParentAccount);
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbInt(request.BranchId);
            command.Parameters.Add("@CurrencyId", SqlDbType.Int).Value = DbInt(request.CurrencyId);
            command.Parameters.Add("@AccountNo", SqlDbType.NVarChar, 100).Value = DbText(request.AccountNo);
            command.Parameters.Add("@Iban", SqlDbType.NVarChar, 100).Value = DbText(request.Iban);
            command.Parameters.Add("@BranchNo", SqlDbType.NVarChar, 100).Value = DbText(request.BranchNo);
            command.Parameters.Add("@Tel", SqlDbType.NVarChar, 100).Value = DbText(request.Telephone);
            command.Parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = DbText(request.Address);
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = DbText(request.Email);
            command.Parameters.Add("@OpenBalanceType", SqlDbType.Int).Value = DbInt(request.OpeningBalanceType);
            command.Parameters.Add("@OpenBalance", SqlDbType.Float).Value = Convert.ToDouble(request.OpeningBalance);
            command.Parameters.Add("@Commission", SqlDbType.Float).Value = Convert.ToDouble(request.Commission);
            command.Parameters.Add("@Approval", SqlDbType.Bit).Value = request.ApprovalRequired;
            command.Parameters.Add("@Loan", SqlDbType.Bit).Value = request.LoanBank;
        }

        private static void AddBoxParameters(SqlCommand command, FinancialBoxEditViewModel request, int id)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = DbText(request.BoxName);
            command.Parameters.Add("@NameE", SqlDbType.NVarChar, 255).Value = DbText(request.BoxNameEnglish);
            command.Parameters.Add("@Comments", SqlDbType.NVarChar, 255).Value = DbText(request.Comments);
            command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode);
            command.Parameters.Add("@AccountCode1", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode1);
            command.Parameters.Add("@AccountCode2", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode2);
            command.Parameters.Add("@ParentAccount", SqlDbType.NVarChar, 50).Value = DbText(request.ParentAccount);
            command.Parameters.Add("@Type", SqlDbType.Int).Value = DbInt(request.Type);
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = DbInt(request.EmployeeId);
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = DbInt(request.BranchId);
            command.Parameters.Add("@ChequeBox", SqlDbType.Bit).Value = request.HasChequeBox;
            command.Parameters.Add("@OpenBalanceDate", SqlDbType.DateTime).Value = (object)request.OpeningBalanceDate ?? DBNull.Value;
            command.Parameters.Add("@OpenBalanceType", SqlDbType.Int).Value = DbInt(request.OpeningBalanceType);
            command.Parameters.Add("@OpenBalance", SqlDbType.Float).Value = Convert.ToDouble(request.OpeningBalance);
            command.Parameters.Add("@DriverId", SqlDbType.Int).Value = DbInt(request.DriverId);
            command.Parameters.Add("@BTtype", SqlDbType.Int).Value = DbInt(request.BalanceType);
            command.Parameters.Add("@BoxValue", SqlDbType.Float).Value = Convert.ToDouble(request.LimitValue);
            command.Parameters.Add("@Period", SqlDbType.Int).Value = DbInt(request.Period);
            command.Parameters.Add("@PeriodMode", SqlDbType.Int).Value = DbInt(request.PeriodMode);
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string tableName, string keyColumn)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(" + Quote(keyColumn) + "), 0) + 1 FROM dbo." + Quote(tableName) + " WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static bool DuplicateExists(SqlConnection connection, SqlTransaction transaction, string tableName, string keyColumn, int currentId, string columnName, string value)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo." + Quote(tableName) + " WHERE LTRIM(RTRIM(ISNULL(" + Quote(columnName) + ", N''))) = @Value AND " + Quote(keyColumn) + " <> @Id", connection, transaction))
            {
                command.Parameters.Add("@Value", SqlDbType.NVarChar, 255).Value = (value ?? string.Empty).Trim();
                command.Parameters.Add("@Id", SqlDbType.Int).Value = currentId;
                return command.ExecuteScalar() != null;
            }
        }

        private static string ValidateFinancialReferences(SqlConnection connection, SqlTransaction transaction, string accountCode, int? branchId, int? currencyId, int? employeeId)
        {
            if (!Exists(connection, transaction, "ACCOUNTS", "Account_Code", accountCode))
            {
                return "\u0627\u0644\u062d\u0633\u0627\u0628 \u0627\u0644\u0645\u0631\u062a\u0628\u0637 \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f \u0641\u064a \u062f\u0644\u064a\u0644 \u0627\u0644\u062d\u0633\u0627\u0628\u0627\u062a.";
            }

            if (branchId.HasValue && branchId.Value > 0 && !Exists(connection, transaction, "TblBranchesData", "branch_id", branchId.Value))
            {
                return "\u0627\u0644\u0641\u0631\u0639 \u0627\u0644\u0645\u0631\u062a\u0628\u0637 \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f.";
            }

            if (currencyId.HasValue && currencyId.Value > 0 && !Exists(connection, transaction, "currency", "id", currencyId.Value))
            {
                return "\u0627\u0644\u0639\u0645\u0644\u0629 \u0627\u0644\u0645\u062d\u062f\u062f\u0629 \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f\u0629.";
            }

            if (employeeId.HasValue && employeeId.Value > 0 && !Exists(connection, transaction, "TblEmployee", "Emp_ID", employeeId.Value))
            {
                return "\u0623\u0645\u064a\u0646 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0627\u0644\u0645\u062d\u062f\u062f \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f.";
            }

            return null;
        }

        private static bool Exists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo." + Quote(tableName) + " WHERE " + Quote(columnName) + " = @Value", connection, transaction))
            {
                command.Parameters.Add("@Value", SqlDbType.NVarChar, 100).Value = value.Trim();
                return command.ExecuteScalar() != null;
            }
        }

        private static bool Exists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, int value)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo." + Quote(tableName) + " WHERE " + Quote(columnName) + " = @Value", connection, transaction))
            {
                command.Parameters.Add("@Value", SqlDbType.Int).Value = value;
                return command.ExecuteScalar() != null;
            }
        }

        private static FinancialAdministrationSaveResult Fail(string message)
        {
            return new FinancialAdministrationSaveResult { Success = false, Message = message };
        }

        private static object DbText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static object DbInt(int? value)
        {
            return value.HasValue && value.Value > 0 ? (object)value.Value : DBNull.Value;
        }

        private static string Quote(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static FinancialAdministrationSearchViewModel Normalize(FinancialAdministrationSearchViewModel search)
        {
            search = search ?? new FinancialAdministrationSearchViewModel();
            search.SearchText = (search.SearchText ?? string.Empty).Trim();
            search.Scope = string.IsNullOrWhiteSpace(search.Scope) ? "all" : search.Scope.Trim().ToLowerInvariant();
            search.Page = search.Page <= 0 ? 1 : search.Page;
            search.PageSize = search.PageSize <= 0 || search.PageSize > 100 ? 40 : search.PageSize;
            return search;
        }

        private static void AddPagingParameters(SqlCommand command, FinancialAdministrationSearchViewModel search)
        {
            var start = ((search.Page - 1) * search.PageSize) + 1;
            var end = search.Page * search.PageSize;
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = search.SearchText;
            command.Parameters.Add("@StartRow", SqlDbType.Int).Value = start;
            command.Parameters.Add("@EndRow", SqlDbType.Int).Value = end;
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand(@"
SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName;", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                return command.ExecuteScalar() != null;
            }
        }

        private static int ScalarInt(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static string ReadString(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int ReadInt(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static int? ReadNullableInt(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static decimal ReadDecimal(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static bool ReadBool(IDataRecord record, string name)
        {
            var value = record[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static DateTime? ReadNullableDate(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }
    }
}
