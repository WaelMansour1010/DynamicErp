# Kishny POS SQL Rollback

Rollback requires the backup output captured before apply.

1. Restore the previous `dbo.usp_POS_SaveTransaction` definition.
2. Restore the previous `dbo.GetNextID_FromSequence` definition if needed.
3. Drop newly added indexes only if they are linked to a regression.
4. Keep `dbo.POS_SaveAttemptLog` unless it causes an issue; it is additive. Export rows before dropping.
5. Recycle the app pool and run POS login/open/save smoke tests against safe data.
