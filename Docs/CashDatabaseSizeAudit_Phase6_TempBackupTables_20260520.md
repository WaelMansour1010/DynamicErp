# Cash Database Size Audit - Phase 6 Temp/Backup/Old/Log/Staging Tables (2026-05-20)

## Scope
- فحص فقط (READ-ONLY) لجداول أسماؤها تحتوي:
  `_bak`, `backup`, `old`, `temp`, `tmp`, `log`, `audit`, `staging`, `import`, `test`
- لا حذف/تعديل/تقليص/ترانكيت.

## Top Tables by Size (Matched Names)
1. `dbo.LogFile` - `~812.41 MB`, rows `~2,022,282`
2. `dbo.POS_SaveAttemptLog` - `~91.21 MB`, rows `~183,957`
3. `dbo.POS_SystemErrorLog` - `~33.74 MB`, rows `~14,358`
4. `dbo.POS_SaveAllocationStageLog` - `~33.45 MB`, rows `~82,470`
5. `dbo.TblGroupItemProductLineUsersset` - `~32.77 MB`, rows `~277,382`
6. `dbo.POS_ImportBatchRow` - `~2.57 MB`, rows `~6,424`
7. الباقي غالبًا صغير جدًا أو شبه فارغ.

## Dependencies / FK Highlights
- `LogFile`: لا اعتماد SP/View ظاهر، لكن عليه FK كـ child (`fk_as_child_count=1`).
- `POS_SaveAttemptLog`: معتمد عليه SP واحد (`usp_POS_SaveAttemptDeadlockDiagnostics`).
- `POS_SaveAllocationStageLog`: معتمد عليه SP حفظ (`usp_POS_SaveTransaction`).
- `POS_ImportBatchRow`: اعتمادية واضحة (3 SP) + FK child/parent.
- `TblErrorLog` و `TblErrorLog_Archive`: عليهما SP/Views تشغيلية.
- جداول كثيرة `temp/tmp/test/old` بدون اعتماد واضح وحجمها صفري أو شبه صفري.

## Candidate Classification (Recommendation Only)

### A) Keep (Operational Logs / Active)
- `LogFile`
- `POS_SaveAttemptLog`
- `POS_SystemErrorLog`
- `POS_SaveAllocationStageLog`
- `POS_ImportBatchRow`
- `POS_ImportBatch`
- `CriticalRecoveryAudit`

Reason:
- أحجامها أعلى نسبيًا أو مرتبطة مباشرة بتشغيل/تشخيص/استيراد أو بـ SP حالية.

### B) Archive Candidate (Before Delete Consideration)
- `TblErrorLog` -> `TblErrorLog_Archive` workflow موجود
- `POS_SalesInvoiceEditLog`
- `POS_DailyClosingSummary_RebuildLog`

Reason:
- جداول سجل (log/audit style) يمكن ترحيل أقدم البيانات منها لدورة احتفاظ، مع إبقاء نافذة حديثة.

### C) Likely Cleanup Candidates (After Ownership Validation)
- جداول `temp/tmp/test/old` الفارغة أو الصغيرة جدًا بدون اعتماد معروف، مثل:
  - `temp1`, `temp2`, `temp_bill_items`, `NewTempTable`, `tmpAccount`, `TBLClosePosTmp`,
  - `TblContractInstallmentsOld`, `TblOLDContract`, `TblTestCertificate`, `TblTestCertificateDet`,
  - `TblTempCustomerAging`, `TblTempEmployee`, `TblTempEmpSalary`, `TblTempItemAging`,
  - `Templates*`/`Template_Details` (لازم تحقق من التبعيات لأن عليها بعض dependencies).

Reason:
- أغلبها صفوف = 0 وحجم مهمل، وبعضها قد يكون بقايا تجارب/ترحيل قديم.

## Risk Notes
- لا ينصح بأي حذف مباشر قبل:
  1. تأكيد مالك الجدول (app owner).
  2. مراجعة dependencies الديناميكية (SQL string-built) خارج `sys.sql_expression_dependencies`.
  3. أخذ backup حديث.
  4. خطة rollback.

## Suggested Next Step (Phase 6.5 Proposed)
1. إعداد whitelist للجداول التشغيلية التي يجب الإبقاء عليها.
2. إعداد retention policy للجداول اللوجية (مثلا 90/180 يوم).
3. تنفيذ Pilot على جدول صغير منخفض المخاطر في نافذة هادئة (بعد موافقة).

## Artifacts
- SELECT-only script:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase6_TempBackupTables_SELECT_ONLY.sql`
