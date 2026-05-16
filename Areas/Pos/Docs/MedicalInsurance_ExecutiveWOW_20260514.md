# Medical Insurance Executive WOW - 2026-05-14

## Objective

Phase 18 turns the medical-insurance module into the client-facing star feature for the Kishny POS operational experience.

The screen is designed to feel like a premium Egyptian SaaS insurance platform, not a legacy HR form.

## WOW-Factor Decisions

- Executive command center layout with KPI strip, membership card, portfolio donut, branch cost bars, department cost bars, renewal alerts, and financial preview.
- Premium employee insurance card with provider, plan, membership number, renewal date, payroll-linked state, QR placeholder, and family count.
- Wallet/full card modes for presentation and print.
- Dependents shown as mini profile cards for family insurance visibility.
- POS remains operational and read-only. Administration stays in MainErp.
- Protected financial messaging explains safety instead of looking incomplete.

## Operational Database Context Correction

### Cause Of Dania Leakage

The previous demo setup placed `PosEmployeePayrollDatabaseOverride=Dania` in `Web.config`, and the POS `EmployeePayrollController` applied the override whenever the value existed.

That made the Kishny POS medical-insurance screen read Dania employees even when the active POS environment was Kishny.

### Correction Implemented

- POS now resolves EmployeePayroll data from `KishnyCashConnection` by default.
- `PosEmployeePayrollDatabaseOverride` is empty by default.
- A new guard key, `PosEmployeePayrollDemoOverrideEnabled`, must be explicitly set to `true` before any override is honored.
- If the guard key is false, any override value is ignored.
- The POS screen now displays the active database, branch, store, user, and environment status.
- If demo override is enabled, the screen shows a clear `DEMO DATABASE` badge.

### Safe Demo Override Behavior

Demo override is now opt-in only:

```xml
<add key="PosEmployeePayrollDemoOverrideEnabled" value="true" />
<add key="PosEmployeePayrollDatabaseOverride" value="Dania" />
```

Operational/default behavior:

```xml
<add key="PosEmployeePayrollDemoOverrideEnabled" value="false" />
<add key="PosEmployeePayrollDatabaseOverride" value="" />
```

### QA Verification Results

- Web.config override disabled by default.
- Controller no longer applies Dania unless demo flag is explicitly true.
- Operational context is sent with the dashboard JSON payload.
- POS view renders active environment/database badges.
- No Dania employee source is configured for normal POS operation.
- Kishny operational database validated as `Cash`.
- Medical-insurance schema was applied to `Cash` using the existing SQL Server 2012 compatible script without demo data.
- `Cash` contains 331 Kishny employees and 0 insurance subscriptions at the time of validation, so the POS dashboard correctly shows live uninsured employees and no Dania records.
- Browser validation showed `data-pos-database=Cash`, `data-pos-demo=false`, `LIVE KISHNY POS` badge, and no JavaScript console errors.

## Dashboard Widgets

- Insured employees.
- Uninsured employees.
- Renewal due.
- Expired/suspended policies.
- Overdue installments.
- Employee contribution total.
- Company contribution total.
- Provider payable total.
- Branch cost comparison.
- Department cost comparison.
- Alerts center.

## Animation Decisions

- KPI values are visually separated for quick executive reading.
- Donut coverage is CSS-driven and lightweight.
- Branch/department bars avoid heavy chart dependencies in POS.
- Card interactions remain smooth and touch-friendly.

## Provider Branding Strategy

- Provider name and plan are first-class card elements.
- Logo area is reserved for future upload.
- Provider-specific color themes can be added without changing business logic.
- Demo provider examples can include AXA, MedNet, GlobeMed, MetLife, and Bupa.

## Client-Selling Rationale

The module answers management questions quickly:

- Who is insured?
- Who is uninsured?
- What does the company pay?
- What does the employee pay?
- Which renewals are risky?
- What is overdue?
- Which branches cost more?
- What accounting flow will be produced?

## Screenshots Checklist

- Active Kishny POS environment badge.
- Executive KPI strip.
- Premium membership card.
- Wallet card mode.
- Family/dependents cards.
- Coverage donut.
- Branch/department cost bars.
- Alerts center.
- Accounting preview.
- Printable card.
- Mobile layout.

## Remaining Roadmap

- Real provider logo upload.
- Real QR verification endpoint.
- Employee photo integration.
- Insurance network directory.
- Attachment/document viewer.
- Provider-level payable aging.
