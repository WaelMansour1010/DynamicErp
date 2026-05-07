# Shared Infrastructure Review

Scope: review current `DynamicErp` architecture for business-neutral reuse. No code was moved in this phase.

## Safe to Reuse as Patterns

| Area | Reuse type | Notes |
| --- | --- | --- |
| MVC Area registration | Pattern | `Areas\Sync` and `Areas\Pos` show route registration style. Main ERP should keep its own `MainErpAreaRegistration`. |
| Area-local controllers/views/content | Pattern | Keep Main ERP UI, CSS, JS, and views under `Areas\MainErp`. |
| Existing MVC authorization attributes | Review then adapt | `Utils\ERPAuthorize.cs` and related attributes can inspire permission checks, but Main ERP must use independent permission names. |
| Report shell concepts | Pattern only | Existing report helpers can inform viewer/export behavior if not POS-coupled. |
| Arabic/RTL layout conventions | Pattern | Reuse typography/layout conventions, not Kishny branding or POS visual language. |
| Grid/list/filter UI patterns | Pattern | Build Main ERP list pages with enterprise filters and actions. |

## Do Not Share Directly

| Component | Reason |
| --- | --- |
| `Areas\Pos` controllers/services/data/reports | POS/Kishny business logic: invoices, cards, KYC, commissions, POS reports. |
| `Areas\Pos\Sql` | Main ERP SQL must live under `Areas\MainErp\Sql`. |
| POS CSS/branding/scripts | Main ERP must not show Kishny/POS branding, colors, invoice assumptions, or session behavior. |
| POS health/session defaults | POS-specific operational assumptions should not affect Main ERP behavior. |
| Kishny report templates | Reports are business-specific and should remain isolated. |

## Existing Coupling Risk Found

`Global.asax.cs` imports/calls POS health infrastructure globally (`PosSystemHealthMonitor`). This should not be reused by Main ERP and should be reviewed later if Main ERP starts needing global startup/health hooks.

No change was made in this phase.

## Main ERP Shared Candidate Services

Keep these initially inside `Areas\MainErp`:

- `Services\Accounting\VoucherPostingService`
- `Services\Accounting\NoteNumberingService`
- `Services\Accounting\AccountCodeGenerationService`
- `Services\Accounting\BranchAccountResolver`
- `Repositories\Accounting\VoucherRepository`
- `Repositories\Shared\LookupRepository`
- `Helpers\SqlServer2012QueryHelper`

Move to a neutral shared folder only after the service is proven business-neutral and not dependent on LC, Project Extracts, or POS.

## Dangerous-to-Generalize Items

- Permission rules: generic check mechanism may be reusable; permission names and role assignments are not.
- Report viewer: viewer shell may be reusable; report files, query parameters, and templates are not.
- Lookup caching: cache mechanism may be reusable; default lookup values and branch assumptions are not.
- Audit logging: mechanism may be reusable; event names and business state transitions are not.
- DB helpers: connection and transaction wrappers may be reusable; hardcoded table names and manual id behavior are domain-specific.

## Recommended Next Infrastructure Step

Before writing LC/Project Extracts business logic, create small MainErp-local interfaces:

- `IMainErpDbConnectionFactory`
- `IMainErpUnitOfWork`
- `INoteNumberingService`
- `IVoucherPostingService`
- `IAccountCodeGenerationService`

Keep implementations internal to `Areas\MainErp` until at least two Main ERP domains use them safely.

## Safety Confirmation

This review did not require modifying:

- `F:\Source Code\DynamicErp\Areas\Pos`
- `F:\Source Code\DynamicErp\Areas\Pos\Sql`
- `F:\Source Code\SatriahMain\Cayshny`
- `F:\Source Code\SatriahMain\Main Script\AllScripts.sql`
