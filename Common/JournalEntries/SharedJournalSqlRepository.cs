using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyERP.Common.JournalEntries
{
    public class SharedJournalSqlRepository
    {
        private readonly Func<SqlConnection> _connectionFactory;

        public SharedJournalSqlRepository(Func<SqlConnection> connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            _connectionFactory = connectionFactory;
        }

        public IList<SharedBranchDto> GetBranches()
        {
            var list = new List<SharedBranchDto>();
            const string sql = @"
SELECT TOP (500)
    branch_id,
    branch_Code,
    branch_name,
    branch_namee
FROM dbo.TblBranchesData WITH (NOLOCK)
ORDER BY branch_id;";

            using (var connection = CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new SharedBranchDto
                    {
                        BranchId = ReadInt(reader, "branch_id").GetValueOrDefault(),
                        BranchCode = ReadString(reader, "branch_Code"),
                        BranchName = FixArabicMojibakeForDisplay(ReadString(reader, "branch_name")),
                        BranchNameEnglish = ReadString(reader, "branch_namee")
                    });
                }
            }

            return list;
        }

        public IList<SharedLookupDto> SearchAccounts(string term)
        {
            var list = new List<SharedLookupDto>();
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

            using (var connection = CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = (term ?? string.Empty).Trim();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SharedLookupDto
                        {
                            Id = ReadString(reader, "Id"),
                            Name = FixArabicMojibakeForDisplay(ReadString(reader, "Name")),
                            Extra = ReadString(reader, "Extra")
                        });
                    }
                }
            }

            return list;
        }

        public bool AccountExists(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return false;
            }

            using (var connection = CreateConnection())
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE Account_Code = @accountCode AND ISNULL(last_account, 0) = 1", connection))
            {
                command.Parameters.Add("@accountCode", SqlDbType.NVarChar, 50).Value = accountCode.Trim();
                return command.ExecuteScalar() != null;
            }
        }

        public bool BranchExists(int branchId)
        {
            using (var connection = CreateConnection())
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.TblBranchesData WITH (NOLOCK) WHERE branch_id = @branchId", connection))
            {
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = branchId;
                return command.ExecuteScalar() != null;
            }
        }

        public bool IsPosted(SharedJournalProfile profile, int noteId)
        {
            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            using (var connection = CreateConnection())
            using (var command = new SqlCommand("SELECT TOP (1) ISNULL(NotePosted, 0) FROM dbo." + profile.HeaderTable + " WITH (NOLOCK) WHERE NoteID = @noteId", connection))
            {
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value && Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
            }
        }

        public IList<SharedAccountTreeDto> GetChartOfAccountsChildren(string parentAccountCode, string term)
        {
            var list = new List<SharedAccountTreeDto>();
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
        (@hasSearch = 0 AND
            (
                (@parentCode = N'' AND
                    (
                        NULLIF(LTRIM(RTRIM(ISNULL(a.Parent_Account_Code, N''))), N'') IS NULL
                        OR NOT EXISTS
                        (
                            SELECT 1
                            FROM dbo.ACCOUNTS p WITH (NOLOCK)
                            WHERE ISNULL(p.Account_Code, N'') = ISNULL(a.Parent_Account_Code, N'')
                        )
                        OR ISNULL(a.Parent_Account_Code, N'') = ISNULL(a.Account_Code, N'')
                    )
                )
                OR (@parentCode <> N'' AND ISNULL(a.Parent_Account_Code, N'') = @parentCode)
            )
        )
  )
ORDER BY a.Account_Serial, a.Account_Code;";

            using (var connection = CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@parentCode", SqlDbType.NVarChar, 50).Value = (parentAccountCode ?? string.Empty).Trim();
                command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = search;
                command.Parameters.Add("@hasSearch", SqlDbType.Bit).Value = hasSearch;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SharedAccountTreeDto
                        {
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountSerial = ReadString(reader, "Account_Serial"),
                            AccountName = FixArabicMojibakeForDisplay(ReadString(reader, "Account_Name")),
                            ParentAccountCode = ReadString(reader, "Parent_Account_Code"),
                            IsLastAccount = ReadBoolean(reader, "IsLastAccount"),
                            HasChildren = ReadBoolean(reader, "HasChildren")
                        });
                    }
                }
            }

            return list;
        }

        public IList<SharedJournalHeaderDto> SearchJournalEntries(SharedJournalProfile profile, SharedJournalSearchRequest request, int userId, bool canChangeDefaults)
        {
            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            var list = new List<SharedJournalHeaderDto>();
            request = request ?? new SharedJournalSearchRequest();
            var openingFilter = profile.IsOpeningBalance ? "  AND ISNULL(n.NoteType, 0) = @manualNoteType\r\n" : string.Empty;
            var autoSourceIdSelect = profile.IsOpeningBalance ? "CAST(NULL AS INT) AS AutoSourceId" : "n.Transaction_ID AS AutoSourceId";
            var groupAutoSource = profile.IsOpeningBalance ? string.Empty : ", n.Transaction_ID";
            var sql = @"
SELECT TOP (200)
    n.NoteID,
    CASE WHEN ISNUMERIC(n.NoteSerial) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial) END AS NoteSerial,
    CASE WHEN ISNUMERIC(n.NoteSerial1) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial1) END AS NoteSerial1,
    n.NoteDate,
    n.branch_no AS BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
    ISNULL(n.Remark, N'') AS Description,
    ISNULL(n.NoteType, 0) AS NoteType,
    COALESCE(NULLIF(nt.NotesTypeName, N''), NULLIF(nt.NotesTypeNamee, N''), CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN @manualNoteTypeName ELSE N'قيد آلي' END) AS NoteTypeName,
    CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN 1 ELSE 0 END AS IsManual,
    " + autoSourceIdSelect + @",
    CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN N'' ELSE COALESCE(NULLIF(nt.NotesTypeName, N''), NULLIF(nt.NotesTypeNamee, N''), N'قيد آلي') END AS AutoSourceName,
    SUM(CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END) AS DebitTotal,
    SUM(CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END) AS CreditTotal
FROM dbo." + profile.HeaderTable + @" n WITH (NOLOCK)
INNER JOIN dbo." + profile.DetailTable + @" d WITH (NOLOCK) ON d.Notes_ID = n.NoteID
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = n.branch_no
LEFT JOIN dbo.TblNotesTypes nt WITH (NOLOCK) ON nt.NotesType = n.NoteType
WHERE (@fromDate IS NULL OR n.NoteDate >= @fromDate)
  AND (@toDate IS NULL OR n.NoteDate < DATEADD(DAY, 1, @toDate))
  AND (@branchId IS NULL OR n.branch_no = @branchId)
  AND (@voucherNo = N'' OR CASE WHEN ISNUMERIC(n.NoteSerial) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial) END LIKE N'%' + @voucherNo + N'%' OR CASE WHEN ISNUMERIC(n.NoteSerial1) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial1) END LIKE N'%' + @voucherNo + N'%')
  AND (@description = N'' OR ISNULL(n.Remark, N'') LIKE N'%' + @description + N'%' OR d.Double_Entry_Vouchers_Description LIKE N'%' + @description + N'%')
  AND (@accountCode = N'' OR d.Account_Code = @accountCode)
  AND (@accountCodes IS NULL OR @accountCodes = N'' OR CHARINDEX(N',' + d.Account_Code + N',', N',' + @accountCodes + N',') > 0)
  AND (@canChangeDefaults = 1 OR n.UserID = @userId OR d.UserID = @userId)
" + openingFilter + @"GROUP BY n.NoteID, n.NoteSerial, n.NoteSerial1, n.NoteDate, n.branch_no, b.branch_name, b.branch_namee, n.Remark, n.NoteType, nt.NotesTypeName, nt.NotesTypeNamee" + groupAutoSource + @"
ORDER BY n.NoteDate DESC, n.NoteID DESC;";

            using (var connection = CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
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
                command.Parameters.Add("@manualNoteType", SqlDbType.Int).Value = profile.ManualNoteType;
                command.Parameters.Add("@manualNoteTypeName", SqlDbType.NVarChar, 100).Value = profile.ManualNoteTypeName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(ReadJournalHeader(reader, profile, false));
                    }
                }
            }

            return list;
        }

        public SharedJournalHeaderDto GetJournalEntryByNoteId(SharedJournalProfile profile, int noteId, int userId, bool canChangeDefaults)
        {
            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            SharedJournalHeaderDto header = null;
            var autoSourceIdSelect = profile.IsOpeningBalance ? "CAST(NULL AS INT) AS AutoSourceId" : "n.Transaction_ID AS AutoSourceId";
            var sql = @"
SELECT TOP (1)
    n.NoteID,
    CASE WHEN ISNUMERIC(n.NoteSerial) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial) END AS NoteSerial,
    CASE WHEN ISNUMERIC(n.NoteSerial1) = 1 THEN CONVERT(NVARCHAR(50), CAST(n.NoteSerial1 AS DECIMAL(38,0))) ELSE CONVERT(NVARCHAR(50), n.NoteSerial1) END AS NoteSerial1,
    n.NoteDate,
    n.branch_no AS BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), n.branch_no)) AS BranchName,
    ISNULL(n.Remark, N'') AS Description,
    ISNULL(n.NoteType, 0) AS NoteType,
    COALESCE(NULLIF(nt.NotesTypeName, N''), NULLIF(nt.NotesTypeNamee, N''), CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN @manualNoteTypeName ELSE N'قيد آلي' END) AS NoteTypeName,
    " + autoSourceIdSelect + @",
    CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN N'' ELSE COALESCE(NULLIF(nt.NotesTypeName, N''), NULLIF(nt.NotesTypeNamee, N''), N'قيد آلي') END AS AutoSourceName,
    CASE WHEN ISNULL(n.NoteType, 0) = @manualNoteType THEN 1 ELSE 0 END AS IsManual
FROM dbo." + profile.HeaderTable + @" n WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = n.branch_no
LEFT JOIN dbo.TblNotesTypes nt WITH (NOLOCK) ON nt.NotesType = n.NoteType
WHERE n.NoteID = @noteId
  AND (@canChangeDefaults = 1 OR n.UserID = @userId);

SELECT
    d.DEV_ID_Line_No,
    d.Account_Code,
    COALESCE(NULLIF(a.Account_Serial, N''), d.Account_Code) AS AccountSerial,
    a.Account_Name,
    d.Double_Entry_Vouchers_Description,
    CASE WHEN d.Credit_Or_Debit = 0 THEN d.Value ELSE 0 END AS Debit,
    CASE WHEN d.Credit_Or_Debit = 1 THEN d.Value ELSE 0 END AS Credit" + (profile.IsOpeningBalance ? @",
    d.opening_balance_voucher_id AS OpeningBalanceVoucherId" : @",
    CAST(NULL AS INT) AS OpeningBalanceVoucherId") + @"
FROM dbo." + profile.DetailTable + @" d WITH (NOLOCK)
LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
WHERE d.Notes_ID = @noteId
ORDER BY d.DEV_ID_Line_No;";

            using (var connection = CreateConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@noteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@canChangeDefaults", SqlDbType.Bit).Value = canChangeDefaults;
                command.Parameters.Add("@manualNoteType", SqlDbType.Int).Value = profile.ManualNoteType;
                command.Parameters.Add("@manualNoteTypeName", SqlDbType.NVarChar, 100).Value = profile.ManualNoteTypeName;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        header = ReadJournalHeader(reader, profile, true);
                    }

                    if (header != null && reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var line = new SharedManualJournalLineDto
                            {
                                AccountCode = ReadString(reader, "Account_Code"),
                                AccountSerial = ReadString(reader, "AccountSerial"),
                                AccountName = FixArabicMojibakeForDisplay(ReadString(reader, "Account_Name")),
                                Description = FixArabicMojibakeForDisplay(ReadString(reader, "Double_Entry_Vouchers_Description")),
                                Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                                Credit = ReadDecimal(reader, "Credit").GetValueOrDefault(),
                                OpeningBalanceVoucherId = ReadInt(reader, "OpeningBalanceVoucherId")
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

        public SharedJournalHeaderDto SaveManualJournalEntry(SharedJournalProfile profile, SharedManualJournalSaveRequest request, int userId, bool canChangeDefaults, bool allowAutomaticOverride)
        {
            if (profile == null)
            {
                throw new ArgumentNullException("profile");
            }

            if (request == null)
            {
                throw new InvalidOperationException("بيانات القيد غير مكتملة");
            }

            using (var connection = CreateConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isUpdate = request.NoteId.HasValue && request.NoteId.Value > 0;
                    if (isUpdate)
                    {
                        var noteType = ExecuteScalarInt(connection, transaction, "SELECT ISNULL(NoteType, 0) FROM dbo." + profile.HeaderTable + " WHERE NoteID = @noteId", new SqlParameter("@noteId", request.NoteId.Value)).GetValueOrDefault();
                        if (noteType != profile.ManualNoteType && (!profile.AllowsAutomaticOverride || !allowAutomaticOverride))
                        {
                            throw new InvalidOperationException(profile.IsOpeningBalance ? "لا يمكن تعديل قيد افتتاحي بنوع مختلف" : "لا يمكن تعديل قيد آلي بدون صلاحية المدير");
                        }
                    }

                    var branchId = request.BranchId.GetValueOrDefault();
                    var noteValue = request.Lines.Sum(x => x.Debit);
                    var noteId = isUpdate ? request.NoteId.Value : GetNextIdFromSequence(connection, transaction, profile.HeaderTable, "NoteID");
                    var voucherId = isUpdate
                        ? GetExistingVoucherId(connection, transaction, profile, noteId)
                        : ExecuteScalarInt(connection, transaction, "SELECT ISNULL(MAX(Double_Entry_Vouchers_ID),0) + 1 FROM dbo." + profile.DetailTable + " WITH (UPDLOCK, HOLDLOCK)", null).GetValueOrDefault();
                    var noteSerial = isUpdate
                        ? ExecuteScalarString(connection, transaction, "SELECT CONVERT(NVARCHAR(50), NoteSerial) FROM dbo." + profile.HeaderTable + " WHERE NoteID = @noteId", new SqlParameter("@noteId", noteId))
                        : GenerateSerial(connection, transaction, profile, branchId, request.NoteDate);
                    decimal noteSerialDecimal;
                    decimal.TryParse(noteSerial, NumberStyles.Any, CultureInfo.InvariantCulture, out noteSerialDecimal);

                    if (isUpdate)
                    {
                        ExecuteNonQuery(connection, transaction, @"
UPDATE dbo." + profile.HeaderTable + @"
SET NoteDate = @noteDate,
    DueDate = @noteDate,
    Note_Value = @noteValue,
    Remark = @description,
    branch_no = @branchId
WHERE NoteID = @noteId;",
                            new SqlParameter("@noteDate", request.NoteDate.Date),
                            new SqlParameter("@noteValue", noteValue),
                            new SqlParameter("@description", request.Description ?? string.Empty),
                            new SqlParameter("@branchId", branchId),
                            new SqlParameter("@noteId", noteId));
                        ExecuteNonQuery(connection, transaction, "DELETE FROM dbo." + profile.DetailTable + " WHERE Notes_ID = @noteId", new SqlParameter("@noteId", noteId));
                    }
                    else if (profile.IsOpeningBalance)
                    {
                        ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.Notes1
(
    NoteID, NoteDate, DueDate, NoteType, NoteSerial, NoteSerial1, Note_Value,
    Double_Entry_Vouchers_ID, UserID, Remark, NotePosted, branch_no, user_name,
    note_value_by_characters
)
VALUES
(
    @noteId, @noteDate, @noteDate, @noteType, @noteSerial, @noteSerial, @noteValue,
    @voucherId, @userId, @description, 0, @branchId, @userName, N''
);",
                            new SqlParameter("@noteId", noteId),
                            new SqlParameter("@noteDate", request.NoteDate.Date),
                            new SqlParameter("@noteType", profile.ManualNoteType),
                            new SqlParameter("@noteSerial", noteSerialDecimal),
                            new SqlParameter("@noteValue", noteValue),
                            new SqlParameter("@voucherId", voucherId),
                            new SqlParameter("@userId", userId),
                            new SqlParameter("@description", request.Description ?? string.Empty),
                            new SqlParameter("@branchId", branchId),
                            new SqlParameter("@userName", string.Empty));
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
    @noteId, @noteDate, @noteType, @noteSerial, @noteSerial, @noteValue, @userId,
    @description, 0, @branchId, @userName, N''
);",
                            new SqlParameter("@noteId", noteId),
                            new SqlParameter("@noteDate", request.NoteDate.Date),
                            new SqlParameter("@noteType", profile.ManualNoteType),
                            new SqlParameter("@noteSerial", noteSerialDecimal),
                            new SqlParameter("@noteValue", noteValue),
                            new SqlParameter("@userId", userId),
                            new SqlParameter("@description", request.Description ?? string.Empty),
                            new SqlParameter("@branchId", branchId),
                            new SqlParameter("@userName", string.Empty));
                    }

                    var lineNo = 1;
                    foreach (var line in request.Lines)
                    {
                        AddJournalLine(connection, transaction, profile, voucherId, lineNo++, line, noteId, request.NoteDate, userId, branchId);
                    }

                    transaction.Commit();
                    return GetJournalEntryByNoteId(profile, noteId, userId, true);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private SqlConnection CreateConnection()
        {
            var connection = _connectionFactory();
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }

        private static SharedJournalHeaderDto ReadJournalHeader(IDataRecord reader, SharedJournalProfile profile, bool includeAutoSource)
        {
            var isManual = ReadInt(reader, "IsManual").GetValueOrDefault() == 1;
            return new SharedJournalHeaderDto
            {
                NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                NoteSerial = ReadString(reader, "NoteSerial"),
                NoteSerial1 = ReadString(reader, "NoteSerial1"),
                NoteDate = ReadDateTime(reader, "NoteDate"),
                BranchId = ReadInt(reader, "BranchId"),
                BranchName = FixArabicMojibakeForDisplay(ReadString(reader, "BranchName")),
                Description = FixArabicMojibakeForDisplay(ReadString(reader, "Description")),
                IsManual = isManual,
                EntryKind = profile.IsOpeningBalance ? "قيد افتتاحي" : (isManual ? "قيد يدوي" : "قيد آلي"),
                NoteType = ReadInt(reader, "NoteType"),
                NoteTypeName = FixArabicMojibakeForDisplay(ReadString(reader, "NoteTypeName")),
                AutoSourceId = includeAutoSource ? ReadInt(reader, "AutoSourceId") : ReadInt(reader, "AutoSourceId"),
                AutoSourceName = FixArabicMojibakeForDisplay(ReadString(reader, "AutoSourceName")),
                AutoSourceUrl = string.Empty,
                DebitTotal = includeAutoSource ? 0 : ReadDecimal(reader, "DebitTotal").GetValueOrDefault(),
                CreditTotal = includeAutoSource ? 0 : ReadDecimal(reader, "CreditTotal").GetValueOrDefault()
            };
        }

        private static int GetExistingVoucherId(SqlConnection connection, SqlTransaction transaction, SharedJournalProfile profile, int noteId)
        {
            if (profile.IsOpeningBalance)
            {
                var headerVoucherId = ExecuteScalarInt(connection, transaction, "SELECT Double_Entry_Vouchers_ID FROM dbo.Notes1 WHERE NoteID = @noteId", new SqlParameter("@noteId", noteId)).GetValueOrDefault();
                if (headerVoucherId > 0)
                {
                    return headerVoucherId;
                }
            }

            return ExecuteScalarInt(connection, transaction, "SELECT TOP (1) Double_Entry_Vouchers_ID FROM dbo." + profile.DetailTable + " WHERE Notes_ID = @noteId ORDER BY Double_Entry_Vouchers_ID", new SqlParameter("@noteId", noteId)).GetValueOrDefault();
        }

        private static string GenerateSerial(SqlConnection connection, SqlTransaction transaction, SharedJournalProfile profile, int branchId, DateTime noteDate)
        {
            if (!profile.IsOpeningBalance)
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

            return ExecuteScalarString(connection, transaction, @"
SELECT CONVERT(NVARCHAR(50), ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CAST(NoteSerial AS DECIMAL(38,0)) ELSE 0 END), 0) + 1)
FROM dbo.Notes1 WITH (UPDLOCK, HOLDLOCK)
WHERE ISNULL(NoteType, 0) = 101
  AND (@branchId = 0 OR branch_no = @branchId);",
                new SqlParameter("@branchId", branchId));
        }

        private static void AddJournalLine(SqlConnection connection, SqlTransaction transaction, SharedJournalProfile profile, int voucherId, int lineNo, SharedManualJournalLineDto line, int noteId, DateTime recordDate, int userId, int branchId)
        {
            var description = line.Description ?? string.Empty;
            if (profile.IsOpeningBalance)
            {
                ExecuteNonQuery(connection, transaction, @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS1
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value,
    Credit_Or_Debit, Double_Entry_Vouchers_Description, RecordDate,
    Notes_ID, UserID, currency, rate, branch_id, DueDate, opening_balance_voucher_id
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value,
    @CreditOrDebit, @Description, @RecordDate,
    @NoteId, @UserId, N'', 1, @BranchId, @RecordDate, @OpeningBalanceVoucherId
);",
                    new SqlParameter("@VoucherId", voucherId),
                    new SqlParameter("@LineNo", lineNo),
                    new SqlParameter("@AccountCode", line.AccountCode ?? string.Empty),
                    new SqlParameter("@Value", line.Debit > 0 ? line.Debit : line.Credit),
                    new SqlParameter("@CreditOrDebit", line.Debit > 0 ? 0 : 1),
                    new SqlParameter("@Description", description),
                    new SqlParameter("@RecordDate", recordDate.Date),
                    new SqlParameter("@NoteId", noteId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@BranchId", branchId),
                    new SqlParameter("@OpeningBalanceVoucherId", line.OpeningBalanceVoucherId.HasValue ? (object)line.OpeningBalanceVoucherId.Value : -1));
                return;
            }

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
                new SqlParameter("@Description", description),
                new SqlParameter("@RecordDate", recordDate.Date),
                new SqlParameter("@NoteId", noteId),
                new SqlParameter("@UserId", userId),
                new SqlParameter("@BranchId", branchId));
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

        private static void ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                command.ExecuteNonQuery();
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
                return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
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

        private static DateTime? ReadDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBoolean(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return false;
            }

            var value = reader.GetValue(ordinal);
            if (value is bool)
            {
                return (bool)value;
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
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
    }
}
