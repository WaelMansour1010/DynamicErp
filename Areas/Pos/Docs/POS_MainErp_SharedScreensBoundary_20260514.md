# POS / MainErp Shared Screens Boundary - 2026-05-14

## Purpose

Define the boundary between POS and MainErp after the shared-screen repair pass.

Shared logic is allowed. Shared routes are not.

## POS-Native Screens

These remain POS-native and must not be routed through MainErp:

- POS sales;
- Kishny cards and KYC/card operations;
- daily closing;
- cash in/out;
- Excel import and reconciliation;
- token invoice lookup;
- POS operational reports;
- POS teller and cashier workflows.

## Shared With MainErp By Core Logic Only

| Module | POS Route | MainErp Route | Boundary |
| --- | --- | --- | --- |
| Users | `/Pos/PosLegacyAdmin/Users` | `/MainErp/Users` | Shared user repository/model allowed; separate wrappers and permissions |
| Stores | `/Pos/Stores` | `/MainErp/StoreData` | Shared store data core allowed; shell/context remain separate |
| Employees visibility | POS employee routes | MainErp employee/payroll routes | POS shows operational subset; MainErp owns full administration |
| Medical insurance visibility | POS medical insurance routes | MainErp employee/payroll medical insurance routes | POS shows operational workflow; MainErp owns full administration/review |
| Reports | POS report routes | MainErp report routes | Shared report read models allowed only when URLs and permissions come from wrapper |

## Context Leakage Prevention

- POS routes stay under `/Pos/...`.
- MainErp routes stay under `/MainErp/...`.
- Shared repositories receive the connection/context from the wrapper.
- POS views must not include `/MainErp/...` links.
- MainErp views must not include `/Pos/...` links.
- Shared partials must receive action URLs through a view model.

## Dania / Demo Override Safety

- POS should continue using the POS/Kishny active context.
- `Dania` is allowed for explicit inventory/accounting validation and protected demo/testing only.
- Any demo override must be visibly badged and must not silently change POS production context.
- MainErp inventory/finance validation may use `Dania` when explicitly requested; MainErp UI/runtime QA may use `Eng`.

## POS Route QA

| Route | Result |
| --- | --- |
| `/Pos/Login` | Pass |
| `/Pos/Dashboard` after POS admin login | Pass |
| `/Pos/PosLegacyAdmin/Users` | Pass |

QA details:

- POS admin credentials used for route smoke: `admin`.
- POS Users opens with title `مستخدمو POS - FrmEditUsers`.
- POS Users rendered in POS shell.
- POS Users contained no `/MainErp/` link leakage.
- Browser console errors: none on tested POS Users route.

## MainErp Boundary QA

| Route | Result |
| --- | --- |
| `/MainErp/Users?searchText=admin` | Pass |

QA details:

- MainErp admin credentials used for route smoke: `admin`.
- MainErp Users rendered in MainErp shell with `MainErp / Eng` context badge.
- MainErp Users contained no `/Pos/` link leakage.
- Browser console errors: none on tested MainErp Users route.

## Remaining Work

- Continue moving banks, boxes, stores, reports, and item lookup logic toward shared core as each screen is touched.
- Keep POS-only operational behavior out of MainErp.
- Keep dangerous MainErp administration/posting behavior out of POS unless a limited, protected preview is explicitly designed.
