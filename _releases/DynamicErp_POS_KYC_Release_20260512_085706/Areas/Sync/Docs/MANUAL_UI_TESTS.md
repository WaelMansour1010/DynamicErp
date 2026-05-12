# Sync Admin Manual UI Tests

## Read-only navigation

1. Open `/sync`.
2. Confirm the Arabic header shows `مركز التحكم بالمزامنة`.
3. Confirm dashboard widgets load from `Sync_Outbox` / `Sync_Batch`.
4. Confirm warning banner states ApplyMode is blocked.

## Queue

1. Open `/sync/Queue`.
2. Filter by `SyncKey`, `BranchId`, status, payload hash, and date range.
3. Confirm actions are preview/diagnostics/readiness only.

## Diagnostics

1. Open `/sync/Diagnostics?syncKey=<sample>`.
2. Confirm queue row, object map, logs, and safety checks are visible.

## Pilot readiness

1. Open `/sync/Pilot`.
2. Confirm batch apply is blocked and ApplySingleSyncKey is required.
3. Confirm hard blockers appear in red.

## Apply safety

1. Open `/sync/Apply?syncKey=<sample>`.
2. Confirm the button is disabled.
3. POST `/sync/Apply/RequestApply` with an anti-forgery token.
4. Confirm HTTP 403.

## No execution

Confirm no PowerShell process is launched and no legacy invoice table rows are inserted, updated, or deleted by the UI.
