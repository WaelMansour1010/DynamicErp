# Kishny POS Production Config Checklist - 2026-05-08

## Current Root Web.config Review
The checked root `Web.config` is **not production-safe** for Kishny customer deployment.

Observed risks:
- `KishnyCashConnection` points to local server/catalog (`Wael\Sql2019`, `Cash`) and uses local credentials.
- `MainErp_ConnectionString` points to `Eng`.
- `MyERP_ConnectionString` points to local `MyErp`.
- `EnableDevMasterPassword` is `true`.
- `DevMasterPassword` is present.
- `compilation debug="true"`.
- `/RunMode` and root DevStart routes are registered in `App_Start\RouteConfig.cs`.
- `MainErp` area routes are registered unconditionally in `Areas/MainErp/MainErpAreaRegistration.cs`.

## Required Production Values
- `KishnyCashConnection`: must point only to the Kishny/POS customer DB.
- `MyERP_ConnectionString`: if required by legacy/EF parts of POS, must point to the same approved Kishny/POS DB, not Eng, not local MyErp.
- `MainErp_ConnectionString`: should be omitted, blank disabled placeholder, or point nowhere useful for POS; MainErp must not be reachable.
- `EnableDevStart`: `false`.
- `EnableDevMasterPassword`: `false`.
- `DevMasterPassword`: absent or blank placeholder.
- `EnableMainErpMigration`: `false`.
- `EnableKishnyPos`: `true`.
- `DebugKYC` and all debug/local flags: `false`.
- `compilation debug`: `false`.
- `customErrors`: production-safe mode.
- Cookies: `httpOnlyCookies="true"`, `requireSSL="true"` if HTTPS is enabled end-to-end.
- Authentication/session cookies: secure/same-site policy appropriate for the site.
- `machineKey`: preserve the current deployed customer value unless intentionally rotating all sessions.
- Binding redirects: preserve required redirects from the current working deployment.

## Route Exposure Checklist
- `/Pos/Login`: allowed.
- `/Pos`: allowed.
- `/Pos/PosTransaction/Index`: allowed.
- `/RunMode`: must be disabled or blocked.
- `/DevStart`: must be disabled or blocked.
- `/MainErp`: must be disabled or blocked unless explicitly approved.
- POS menus/sidebar must not link to MainErp migration screens.
- Payment/cashing/excel import routes should be hidden/held unless explicitly approved.

## POS Isolation Findings
- POS repository classes use `KishnyCashConnection` for core POS operations.
- `Areas/Pos/Repositories/Payments/PaymentVoucherReadRepository.cs` inherits from `MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherReadRepository` and injects `KishnyCashConnection`; this still creates a MainErp code dependency and should not be part of the deadlock release.
- `Areas/Pos/Views/Payments/*` and `Areas/Pos/Views/Cashing/*` use MainErp view models and should be held.
- `Areas/Pos/Views/DiscountNotifications/Index.cshtml` references `~/Areas/MainErp/Content/mainerp/mainerp.css`; this is existing cross-area style coupling and should be reviewed if packaging POS-only.

## Production Checklist
- [ ] Use a production Web.config transform/template, not current root `Web.config`.
- [ ] Redact credentials in docs/package notes.
- [ ] Confirm customer DB name and SQL login with the customer environment.
- [ ] Confirm app pool identity and SQL permissions for `sp_getapplock`, sequence use, procedure execution, and index/table creation.
- [ ] Confirm `App_Data/Logs` is writable if file retry logs are enabled.
- [ ] Preserve deployed `machineKey`.
- [ ] Disable MainErp and DevStart routes.
- [ ] Hide MainErp/payment/cashing/excel import menu items unless approved.
- [ ] Build and smoke test after applying the production config.
