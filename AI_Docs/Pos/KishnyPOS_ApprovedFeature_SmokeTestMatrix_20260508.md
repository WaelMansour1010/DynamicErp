# Kishny POS Approved Feature Smoke Test Matrix - 2026-05-08

Build validation: Release build succeeded from clean release worktree on 2026-05-08.

| Area | Test | Expected | Result |
|---|---|---|---|
| Core | `/Pos/Login` | Login page opens | Pending staging URL |
| Core | `/Pos` | POS shell opens after login | Pending staging URL |
| Sales invoice | `/Pos/PosTransaction/Index` | Invoice screen loads defaults | Pending staging URL |
| Sales invoice | Double-click save | One active save request; button disabled | Code gate implemented; pending browser/staging |
| Sales invoice | Validation error | Overlay hides, form remains, validation fields highlighted | Code gate implemented; pending staging |
| Sales invoice | Deadlock exhausted response | Friendly Arabic message, attempt id, 60s retry countdown | Code gate implemented; pending simulated/staging |
| Sales invoice | Normal save on test DB | Transaction id returned, print enabled, form resets only after success | Pending safe test DB |
| Search | Invoice search | Search opens and returns expected rows | Pending staging |
| Journal Entries | Open/list/details | POS route works, no MainErp dependency | Pending staging |
| استعاضة العهدة | Open/list/preview/save on test DB | `/Pos/Payments/Index` works; `/Pos/Cashing` blocked | Pending staging/test DB |
| Accounting reports | Open/run/export changed reports | POS reports render | Pending staging |
| Kishny/operational reports | Run operational reports | POS routes render, SQL present | Pending staging |
| Smart reports | Financial intelligence reports | POS routes render, SQL present | Pending staging |
| Monitoring/errors | Error log and save attempts tab | Error/save attempt logs visible, no secrets displayed | Pending staging |
| Security | `/MainErp` | 404/blocked | Static gate verified; pending staging |
| Security | `/DevStart` | 404/blocked | Static gate verified; pending staging |
| Security | `/RunMode` | 404/blocked | Static gate verified; pending staging |
| Security | `/Pos/ExcelImport` | 404/blocked | Route/config/package exclusion verified; pending staging |
| Security | `/Pos/Cashing` | 404/blocked | Route/config/package exclusion verified; pending staging |

No production DB destructive tests were run.
