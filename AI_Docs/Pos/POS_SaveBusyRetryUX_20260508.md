# POS Save Busy Retry UX - 2026-05-08

## Scope
Approved POS sales invoice UX hardening for slow saves, SQL deadlock retry exhaustion, and server-busy failures.

## Server behavior
- `PosTransactionController.Save` now creates a `saveAttemptId` for every save request.
- Validation failures return `success=false`, `errorType=Validation`, validation fields, `canRetry=false`, and the attempt id.
- Deadlock exhaustion returns HTTP 503 with `errorType=ServerBusyDeadlock`, `retryAfterSeconds=60`, `canRetry=true`, and the Arabic customer message.
- Unknown SQL failures return `errorType=SqlError` with a support-friendly message. Raw SQL text is hidden unless `DebugKYC=true`.
- `PosSqlRepository.SaveTransaction` accepts the controller-provided attempt id and writes it into the existing retry/log flow.

## Client behavior
- Save button is disabled immediately.
- The invoice remains on screen during save and after any failure.
- A save overlay shows `جارٍ حفظ الفاتورة...`.
- If the request remains active, the overlay updates to `السيرفر مشغول، جاري إعادة المحاولة...`.
- `ServerBusyDeadlock` shows the final friendly message, attempt id reference, and a 60-second countdown.
- Save remains disabled during cooldown. After cooldown, the button text becomes `إعادة محاولة الحفظ`.
- Success alone enables print/new invoice behavior.

## Safety notes
- No accounting SQL logic was changed for this UX update.
- Failed saves are not treated as saved.
- Duplicate clicks are blocked client-side while the request is active.
- Duplicate/IPN protection remains server-side.
