# Kishny POS SQL Apply Order

Target module: Kishny POS
Target database: customer Kishny/POS database only

1. `00_BACKUP_BEFORE_APPLY.sql`
2. `31_POS_GetNextID_FromSequence_Concurrency.sql`
3. `47_POS_SaveAttemptLog.sql`
4. `46_POS_SaveTransaction_ConcurrencyIndexes.sql`
5. `30_POS_SaveTransaction_UnicodeText.sql`
6. Optional/manual diagnostics: `39_POS_Deadlock_Diagnostics.sql`

Do not apply any MainErp SQL in this release.
