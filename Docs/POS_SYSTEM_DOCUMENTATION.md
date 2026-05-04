# POS System Documentation

This document is the handoff file for the DynamicErp POS/Keshni work. It is intentionally written as a practical map for any new developer or AI agent that needs to continue the project without rediscovering the same context.

Project root:

```text
F:\Source Code\DynamicErp
```

Main POS area:

```text
F:\Source Code\DynamicErp\Areas\Pos
```

Do not put POS database scripts in the old project path:

```text
F:\Source Code\SatriahMain\Main Script\AllScripts.sql
```

POS scripts belong under:

```text
F:\Source Code\DynamicErp\Areas\Pos\Sql
```

---

## 1. System Overview

The POS system is a web implementation of the Keshni/CASH workflow inside the ASP.NET MVC ERP project. It supports daily teller sales, KYC/card activation, operational reports, accounting reports, dashboard analytics, session recovery, system health monitoring, and configurable print templates.

Main modules:

| Module | Purpose |
|---|---|
| Sales / POS | Create and review Cash In, Cash Out, Keshni Card, and Violations transactions. |
| KYC | Save customer/card KYC data, upload/open attachments, print KYC card and acknowledgment. |
| Reports | Operational POS reports and lightweight Razor/HTML reports with Excel/print support. |
| Accounting | Trial balance, income statement, account statement, assistant ledger, and manual journal entry screen. |
| Dashboard | Admin/executive KPIs, branch/service/seller summaries, smart rule-based insights. |
| System Health | Admin-only monitoring for app, POS, session restore, SQL/server health. |
| Print Template Designer | Visual JSON-driven print layout editor for KYC card templates. |
| Payments/Custody | Web version of funding/reimbursement workflow inspired by VB6 `FrmPayments1`. |

The system is high-volume POS, not a small back-office tool. Local audit showed millions of transaction/journal rows, and production is expected to support roughly 120+ concurrent users and thousands of daily transactions.

---

## 2. Architecture

### ASP.NET MVC Structure

The POS implementation lives under:

```text
Areas/Pos
```

Important folders:

```text
Areas/Pos/Controllers
Areas/Pos/Data
Areas/Pos/Models
Areas/Pos/Views
Areas/Pos/Scripts
Areas/Pos/Reports
Areas/Pos/Services
Areas/Pos/Sql
Areas/Pos/Tools
Areas/Pos/assets
```

### Main Controllers

| Controller | Responsibility |
|---|---|
| `PosLoginController` | POS login, emergency admin login, `POSCTX` cookie, session restore, logout. |
| `PosDashboardController` | POS shell, admin dashboard visibility, menu routing, sales defaults checks. |
| `PosTransactionController` | Main sales screen, invoice save/load/search, commission preview, KYC save, lookups. |
| `PosClosingController` | POS closing screen and close execution. |
| `KycAttachmentController` | KYC attachments and acknowledgment PDF action. |
| `KycBankFollowUpController` | KYC/bank follow-up exports and copy workflows ported from VB6 reference logic. |
| `PosReportsController` | Operational reports screen and report execution. |
| `HtmlReportsController` | Lightweight Razor/HTML POS reports. |
| `AccountingReportsController` | Accounting HTML reports. |
| `JournalEntriesController` | Manual journal entry create/search/view/edit. |
| `PaymentsController` | Funding/reimbursement screen. |
| `PosPermissionsController` | Temporary POS permissions UI and bulk/category permission management. |
| `PosSystemHealthController` | Admin-only system health dashboard. |
| `PrintTemplateController` | Visual print template designer. |

### Data/Services

| Class | Responsibility |
|---|---|
| `PosSqlRepository` | Main SQL gateway for POS login, permissions, invoices, reports, accounting, KYC, dashboard data. |
| `PosClosingSqlRepository` | Closing-specific persistence and validation. |
| `PosSystemHealthRepository` | System health metrics from logs/DMVs/stored procedures. |
| `PrintTemplateService` | Load/save JSON templates and backgrounds from `App_Data/PrintTemplates`. |
| `PosSystemHealthMonitor` | Lightweight in-process monitoring for active users/session restores/request indicators. |

### Client Scripts

| Script | Responsibility |
|---|---|
| `pos-transaction.js` | Sales UI, keyboard flow, save/load, KYC modal, commission preload/cache, invoice list, dashboard card snippets. |
| `pos-closing.js` | Closing screen interactions. |
| `account-selector.js` | Reusable account selector modal/search/tree behavior. |
| `print-template-designer.js` | Visual template editor behavior. |

---

## 3. POS Workflow

### 1. Login

The user logs into the POS module through `PosLoginController`. The backend validates credentials through `PosSqlRepository.LoginPosUser`.

Important behavior:

- Login validates identity/password.
- Sales defaults are not required for every user at login.
- Sales defaults are required when opening/using the sales screen.
- Emergency/admin login exists for the configured master flow; treat it as privileged and protect it operationally.

### 2. Session Handling

After successful login:

- `Session["PosUserContext"]` is set.
- A protected `POSCTX` cookie is issued.
- The context contains user, branch, store, box, payment defaults, and permissions.

### 3. Creating Invoice

The sales screen supports:

- Cash In
- Cash Out
- Keshni Card
- Violations

The UI applies transaction-type specific visibility/validation. Hidden fields must not be validated or carried into mismatched transaction types.

Important safety rules:

- Switching top transaction tabs resets stale state.
- Existing review/edit mode is cleared when switching tabs.
- Branch/store/customer/card state is reset unless intentionally loading an existing invoice.
- Keshni Card uses grid + card summary, not recharge/wallet fields.

### 4. Commission/Payment Calculation

Commission settings are preloaded/cached client-side on screen initialization through the commission bootstrap endpoint. The save button is guarded until commission settings are ready.

Key frontend flags in `pos-transaction.js`:

```js
commissionCache
commissionsReady
commissionCalculationPending
commissionPreviewSequence
lastCashOutBankMachineCommission
```

Cash Out has special bank/machine commission handling. The UI can show the teller the true amount to withdraw from the machine:

```text
اسحب من الماكينة
عمولة الماكينة
```

The VB6 reference logic for machine commission uses:

```vb
SELECT Price, CashBack, Cost
FROM dbo.CheckPriceRangeSales3(amount, amount, itemId)
```

`Cost` is the machine/bank commission source.

### 5. Save

Save is handled by `PosTransactionController.Save`, which ultimately calls `dbo.usp_POS_SaveTransaction`.

Important rules:

- Normal tellers must be able to create new invoices with their normal permissions.
- Password confirmation is required only when editing an existing invoice created by another user.
- Existing invoice branch must be loaded from the invoice itself, not the current user default.
- Existing invoice creator/user must remain unchanged.
- Last modifier fields are used where available.
- Save must preserve current accounting/journal behavior.

### 6. Journal Creation

Journal/accounting entries are created through the existing save/update flow. Do not manually invent debit/credit logic in UI code.

Known tables involved:

```text
Transactions
Transaction_Details
Notes
DOUBLE_ENTREY_VOUCHERS
TransactionValueAdded
```

### 7. Printing

Printing includes:

- POS receipt reports.
- KYC card report.
- KYC acknowledgment report.
- Print template designer for KYC card layout.

Important: Do not modify `PosReceiptReport.cs` unless a task explicitly targets POS receipts.

### Keyboard Flow

The sales screen includes keyboard-oriented UX in `pos-transaction.js`:

- `Enter` moves through the active workflow instead of causing accidental submit.
- `F9` and `Ctrl+S` are save shortcuts.
- Save is blocked during loading, pending commission calculation, or invalid state.

### Loading Protection

The UI uses loading helpers:

```js
uxBeginLoading()
uxEndLoading()
uxIsBusy()
```

Do not allow duplicate saves while `uxSaving` or loading flags are active.

---

## 4. Session Handling

### Keys

Defined in `PosLoginController`:

```csharp
public const string PosContextSessionKey = "PosUserContext";
public const string PosContextCookieName = "POSCTX";
```

### Session

Primary source:

```csharp
Session["PosUserContext"]
```

### Cookie Restore

If the ASP.NET session is lost, controllers use `PosLoginController.RestorePosContext(...)` to rebuild context from the protected `POSCTX` cookie.

The intended behavior:

1. Try session.
2. If missing, try protected cookie.
3. Rebuild `PosUserContext` from database defaults/permissions.
4. Refresh cookie if needed.
5. Log restore safely.

### Security Considerations

The POS cookie must be:

- `HttpOnly = true`
- `Secure = true` on HTTPS
- `SameSite = Lax` or stricter if compatible
- Encrypted/protected using fixed `machineKey`
- Reasonable expiration, around 12 hours

Production must not use `AutoGenerate` machineKey. All app instances must share the same fixed key.

### Logout Behavior

Logout must:

- Clear ASP.NET Session.
- Expire/remove `POSCTX`.
- Prevent context restore after logout.

### Restore Logging

Restore logs are written under:

```text
App_Data/Logs/pos-session-restore-yyyyMMdd.log
```

Log only safe fields:

- UserId
- BranchId
- timestamp
- controller/action
- IP

Never log cookie payload, password, token, national ID, or sensitive KYC values.

### Failure Handling

If cookie is missing/invalid/expired:

```text
انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى
```

Anti-loop behavior should stop repeated restore attempts after 2-3 failures for the same request and redirect cleanly to login.

---

## 5. Database & Performance

### Main Tables

High-volume tables observed in local `Cash` audit:

| Table | Role |
|---|---|
| `Transactions` | Invoice/movement header. |
| `Transaction_Details` | Invoice/service/item details. |
| `Notes` | Vouchers/closing/journal note headers. |
| `DOUBLE_ENTREY_VOUCHERS` | Accounting journal lines. |
| `TblCusCsh` | Keshni card/KYC customer data. |
| `TblBranchesData` | Branch lookup/security. |
| `TblUsers` | Users and admin flag (`UserType = 0`). |
| `TblEmployee` | Employee/seller names linked to users. |

### Main Stored Procedure

Sales save uses:

```text
dbo.usp_POS_SaveTransaction
```

Sequence/concurrency work involved:

```text
dbo.GetNextID_FromSequence
```

The ID allocation bug under concurrency was handled separately by the POS sequence script.

### Performance Findings

Local audit on `Cash` showed:

- Database size around `35 GB`.
- `Transactions` has millions of rows.
- `Transaction_Details` has millions of rows.
- `DOUBLE_ENTREY_VOUCHERS` is very large.
- POS save creates/updates multiple child/journal rows.

Mixed 120-worker load test for 10 minutes:

| State | Save Count | Failures | Avg Save | Max Save | P95 Save |
|---|---:|---:|---:|---:|---:|
| Before experimental indexes | 7,554 | 0 | 2,170 ms | 4,104 ms | 2,701 ms |
| After experimental indexes | 5,914 | 0 | 3,047 ms | 5,387 ms | 3,510 ms |

Conclusion:

- Stored procedure conversion is approved.
- Experimental reporting indexes worsened invoice save performance.
- Do not deploy untested indexes blindly.

Final deployment package:

```text
Areas/Pos/Sql/PerformanceDeployment
```

Important files:

```text
01_Apply_Final_Pos_Performance_Procedures.sql
02_Rollback_Experimental_Indexes.sql
03_Optional_SQL_Server_Memory_Settings.sql
04_WebConfig_ConnectionString_Recommendation.txt
05_Test_Commands.ps1
README_POS_Performance_Deployment.md
```

### Connection Pool Recommendation

Current default pool size is usually 100. For 120+ users, recommended:

```text
Max Pool Size=200;Connect Timeout=30;Pooling=True;
```

Do not copy developer credentials to production. Only append the tuning options to the correct environment connection string.

### SQL Memory Recommendation

If IIS and SQL Server run on the same 34 GB server:

```sql
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'max server memory (MB)', 24576;
RECONFIGURE;
```

If SQL Server is dedicated on a similar 34 GB server, `28672 MB` may be considered.

Do not change `MAXDOP` or `cost threshold for parallelism` without clean before/after wait-stat evidence.

---

## 6. Reports System

### Report Types

There are two report areas:

1. Operational POS reports.
2. Lightweight Razor/HTML reports.

The lightweight report system avoids DevExpress designer and uses:

- Stored procedures or optimized SQL views.
- MVC controller actions.
- Strongly typed models.
- Razor views.
- HTML tables.
- Print-friendly CSS.
- Excel export.

### Controllers

```text
PosReportsController
HtmlReportsController
AccountingReportsController
```

### Operational Reports

Examples include:

- تقرير يومي بالحركات
- تقرير يومي بالحركات 2
- تقرير المبيعات الشامل 1
- تقرير المبيعات الشامل 2
- تقرير المبيعات العام
- تقرير الإغلاقات
- تقرير الإغلاق المالي
- تقرير الإغلاق المالي والخصومات
- تقرير الإيرادات
- تقرير سيريالات المخزن

Reports must not auto-load heavy data on page open. The user selects a report and presses the search/report button.

### Accounting Reports

Available accounting reports:

- ميزان مراجعة
- قائمة الدخل
- كشف حساب
- أستاذ عام مساعد

These use `AccountingReportsController` and stored procedure-backed data loading.

### Filters

Common filters:

- From date
- To date
- Branch
- Account selector where applicable
- Store where applicable

### Account Tree Selector

The account selector is a reusable modal component, not an always-visible inline tree.

Key rules:

- Display `Account_Serial` to users.
- Hide `Account_Code` from UI; use it internally only.
- Search by account serial or account name.
- Support multi-select.
- Selecting a parent can include children.
- Server-side/lazy search is preferred for large charts of accounts.

Script:

```text
Areas/Pos/Scripts/account-selector.js
```

---

## 7. Print Template Designer

### URL

```text
/Pos/PrintTemplate?name=KycCard
```

### Purpose

The print template designer allows visual editing of KYC card print fields without a DevExpress designer.

It is currently used by:

```text
Areas/Pos/Reports/KycCardReport.cs
```

### Storage

Templates are stored as JSON:

```text
App_Data/PrintTemplates/{Name}.json
```

Background images are stored under:

```text
App_Data/PrintTemplates/Backgrounds
```

### Service

```text
Areas/Pos/Services/PrintTemplateService.cs
```

### Template Model

```text
Areas/Pos/Models/PrintTemplate.cs
```

Template fields contain coordinates, sizing, font, alignment, label, key, and field type.

### Fallback Behavior

If no JSON template exists, `KycCardReport.BuildDefaultTemplate()` supplies default field positions/constants.

### Permission

The link appears only when:

```text
CanManagePrintTemplates = true
```

This comes from admin/full access or `FrmPosPrintTemplate` permission.

---

## 8. Permissions Model

### Source

POS permissions are read mainly from:

```text
dbo.ScreenJuncUser
```

Admin detection:

```sql
TblUsers.UserType = 0
```

### Important Permission Screens/Flags

Examples:

| Permission / Screen | Use |
|---|---|
| `FrmSaleBill6` | Sales save/print/edit/return style permissions. |
| `FrmCustCash` | KYC/customer card permissions. |
| `FrmPosPrintTemplate` | Print template designer access. |
| `CustomerService` / `IsFullAccessCustomerService` | KYC/bank follow-up access rules. |
| POS report permissions | Per-report visibility/execution. |
| Accounting report permissions | Accounting report access. |
| Journal entry permissions | Manual journal create/edit/delete. |

### Context Flags

`PosUserContext` includes many boolean flags:

```text
CanSave
CanPrint
CanReturn
CanOpenCashCustomer
CanViewReports
CanViewAdminDashboard
CanViewJournalEntry
CanOpenClosing
CanExecuteClosing
CanOpenSales
CanEditInvoice
CanCancelOrReturn
CanEditKyc
CustomerService
IsFullAccessCustomerService
CanPrintKycAcknowledgment
CanPrintKycCard
CanTeller
CanOpenPayments
CanExecutePayments
CanEditPayments
CanViewAccountingReports
CanCreateJournalEntry
CanEditJournalEntry
CanDeleteJournalEntry
CanManagePrintTemplates
```

### Granting Permission

Use the POS permissions screen where possible. For direct DB updates, follow existing `ScreenJuncUser` patterns and avoid duplicate permission rows.

Admin/full access should bypass normal menu restrictions only where explicitly intended.

---

## 9. System Health Dashboard

### URL/Menu

Admin-only menu:

```text
مراقبة النظام
```

Controller:

```text
PosSystemHealthController
```

Repository:

```text
PosSystemHealthRepository
```

### Metrics

The dashboard is designed to show:

- Active users.
- Requests per minute.
- Average response time.
- Error rate.
- POS session restores.
- Invoice save performance.
- Failed saves.
- SQL/database health.
- Blocking/slow queries where permission allows.
- Alerts.

### SQL Permission

Some server/DMV metrics require:

```sql
GRANT VIEW SERVER STATE TO [AppLoginName];
```

Script:

```text
Areas/Pos/Sql/35_POS_SystemHealthPermissions.sql
```

### Fallback Behavior

If permission is missing, the dashboard must not crash. It should show:

```text
لا توجد صلاحية كافية لقراءة مؤشرات الخادم. يتطلب هذا الجزء صلاحية VIEW SERVER STATE.
```

and continue showing available metrics.

### Performance Rule

Do not run heavy health queries automatically. Use:

```text
تحديث مؤشرات النظام
```

and cache briefly where possible.

---

## 10. UX Improvements

### Keyboard Mode

The sales screen is optimized for teller speed:

- Enter-based movement.
- F9/Ctrl+S save shortcuts.
- Save blocked during busy/loading state.

### Step Locking

The UI prevents saving until required steps are complete:

- Required sales defaults.
- Valid branch/store/box.
- Required service/item.
- Valid customer/card data where applicable.
- Commission settings loaded.
- No pending commission calculation.

### Debounce / Race Protection

Commission preview uses:

```js
commissionPreviewSequence
commissionTimer
commissionKey()
```

Old async responses must be ignored if the user changed inputs before the response returned.

### Save Protection

Before save:

- Validate permissions.
- Validate sales defaults.
- Validate current transaction type.
- Validate commission readiness.
- If editing another user's invoice, require current user's password.
- Disable duplicate save clicks while saving.

### Loading Overlay

`pos-transaction.js` manages loading state with:

```js
uxBeginLoading()
uxEndLoading()
```

Keep UI responsive and avoid blocking the browser with synchronous heavy work.

---

## 11. Known Issues / Constraints

### Save Time

Controlled local mixed-load baseline:

```text
Average save time around 2.17 seconds
P95 around 2.7 seconds
```

This is stable but not extremely fast. Further work should target `usp_POS_SaveTransaction`, transaction scope, journal overhead, and lock duration.

### Large Tables

Important large tables:

```text
Transactions
Transaction_Details
Notes
DOUBLE_ENTREY_VOUCHERS
```

Reports over these tables must use stored procedures/pagination/date filters.

### Indexes

Experimental reporting indexes made save performance worse. Do not add indexes blindly. Any new index must be tested against both:

- Read/report performance.
- Write/save performance.

### Dashboard Lazy Loading

Heavy dashboard insights must be loaded on demand:

```text
تحميل مؤشرات الأداء
```

Do not auto-load expensive dashboard/report queries on initial page load.

### Encoding

Arabic encoding has been a recurring issue. Ensure:

- Files are UTF-8.
- Razor layouts include `<meta charset="utf-8">`.
- SQL Arabic literals use `N'...'`.
- Database Arabic columns are NVARCHAR where Arabic is stored.
- Do not reintroduce mojibake strings.

### Server Session

If production app pool recycles, save should still work by restoring context from `POSCTX`. Fixed machineKey is mandatory.

---

## 12. How To Continue Development

### Add a New POS Feature

1. Start in `Areas/Pos/Controllers`.
2. Use `PosSqlRepository` or a focused repository method.
3. Add or reuse a view under `Areas/Pos/Views`.
4. Add focused JS under `Areas/Pos/Scripts` if needed.
5. Reuse `PosUserContext`.
6. Protect backend endpoints with permissions, not UI hiding only.
7. Keep changes scoped.

### Add a New Report

1. Decide if it belongs to operational reports or accounting reports.
2. Create/extend a stored procedure under `Areas/Pos/Sql`.
3. Add a report definition in the appropriate controller.
4. Use the existing HTML report view model/table rendering.
5. Add Excel/print support through the existing report pattern.
6. Do not load data on page open.
7. Test with large date ranges and branch filters.

### Add a New Accounting Feature

1. Inspect existing accounting tables and VB6 reference logic first.
2. Do not guess debit/credit behavior.
3. Manual journals use:

```text
NoteType = 57
```

4. Automatic journal entries should be view-only for normal users.
5. Editing automatic entries requires general admin password and journal edit permission.

### Modify POS Save Safely

Do not change:

- Accounting debit/credit logic.
- Journal creation behavior.
- Tax/VAT logic.
- Stock/serial behavior.
- Original invoice branch/creator during edits.

Before changing save:

1. Reproduce with one invoice.
2. Test new invoice save.
3. Test own invoice edit.
4. Test other-user invoice edit with password.
5. Test branch preservation.
6. Run at least a small load test.

### Test Commands

Tools:

```text
Areas/Pos/Tools/Invoke-PosPerformanceAudit.ps1
Areas/Pos/Tools/Invoke-PosLoadScenario.ps1
```

Final runbook:

```text
Areas/Pos/Sql/PerformanceDeployment/README_POS_Performance_Deployment.md
```

---

## 13. Important Rules

1. No heavy inline queries for dashboards, reports, accounting reports, or large searches.
2. Prefer stored procedures or optimized views for heavy data.
3. SQL Server compatibility target: SQL Server 2012 for deployment scripts unless otherwise confirmed.
4. Stored procedures should use `DROP` then `CREATE`, not `CREATE OR ALTER`.
5. Avoid `SELECT *` in reporting/analytics procedures.
6. Use date/branch/user/type filters.
7. Do not load full datasets into C# and aggregate in memory.
8. Use pagination for large grids.
9. Do not auto-run heavy reports on page open.
10. Do not add indexes without before/after write and read tests.
11. Do not break teller keyboard flow.
12. Do not block UI with long synchronous operations.
13. Do not log sensitive values.
14. Backend permission checks are mandatory.
15. Preserve original invoice branch and creator during edits.
16. Password confirmation for invoice edit applies only to existing invoices created by another user.
17. Arabic text must remain Unicode-safe end to end.
18. POS work must not be mirrored into the old SatriahMain `AllScripts.sql`.

---

## Quick Reference

### Important URLs

```text
/Pos
/Pos/PosTransaction/Index
/Pos/PosClosing
/Pos/PosReports
/Pos/HtmlReports
/Pos/AccountingReports
/Pos/JournalEntries
/Pos/KycBankFollowUp
/Pos/Payments
/Pos/PosPermissions
/Pos/PosSystemHealth
/Pos/PrintTemplate?name=KycCard
/Pos/KycAttachment/PrintAcknowledgment/{id}
```

### Important Scripts

```text
Areas/Pos/Sql/30_POS_SaveTransaction_UnicodeText.sql
Areas/Pos/Sql/31_POS_GetNextID_FromSequence_Concurrency.sql
Areas/Pos/Sql/34_POS_PerformanceStoredProcedures.sql
Areas/Pos/Sql/35_POS_SystemHealthPermissions.sql
Areas/Pos/Sql/36_POS_PerformanceIndexRollback.sql
Areas/Pos/Sql/PerformanceDeployment/01_Apply_Final_Pos_Performance_Procedures.sql
Areas/Pos/Sql/PerformanceDeployment/02_Rollback_Experimental_Indexes.sql
```

### Important Files

```text
Areas/Pos/Controllers/PosTransactionController.cs
Areas/Pos/Controllers/PosLoginController.cs
Areas/Pos/Data/PosSqlRepository.cs
Areas/Pos/Models/PosSaveTransactionRequest.cs
Areas/Pos/Scripts/pos-transaction.js
Areas/Pos/Scripts/account-selector.js
Areas/Pos/Services/PrintTemplateService.cs
Areas/Pos/Reports/KycCardReport.cs
```

This file should be updated whenever a major POS behavior, database script, permission, or production tuning decision changes.
