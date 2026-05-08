# Kishny POS Feature Release Smoke Test Matrix - 20260508

Package: `F:\Source Code\DynamicErp\Releases\KishnyPOS_FeatureRelease_20260508`
Build source: `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508`

| Area | Test | Result | Notes |
|---|---|---:|---|
| Build | `MyERP.sln` Release build | PASS | Build succeeded with existing warnings. |
| Gate | Release gate package/config scan | PASS | No MainErp content, backup files, uploaded Excel files, local build config, or risky production config flags found. |
| POS login | `/Pos/Login` | PASS | HTTP 200 under IIS Express. |
| POS shell | `/Pos` | PASS | HTTP 302 to `/Pos/Login` when unauthenticated. |
| POS transaction | `/Pos/PosTransaction/Index` | PASS | HTTP 302 to login when unauthenticated. |
| Purchases | `/Pos/PurchaseInvoice/Index` | PASS route smoke | HTTP 302 to login. Authenticated open/search/save needs safe DB session. |
| Stock transfer | `/Pos/StockTransfer/Index` | PASS route smoke | HTTP 302 to login. Authenticated open/search/save needs safe DB session. |
| Reports | `/Pos/PosReports/Index` | PASS route smoke | HTTP 302 to login. Running changed reports needs safe DB session. |
| Excel import | `/Pos/ExcelImport/Index` | PASS route smoke | HTTP 302 to login. Upload/preflight/commit needs safe DB and sample workbook. |
| Deadlock/save | normal save | PENDING | Requires safe test DB; do not run on production blindly. |
| Deadlock/save | duplicate IPN prevention | PENDING | Requires safe test DB and controlled invoice data. |
| Deadlock/save | save attempt log visible | PENDING | Requires SQL 47 applied on safe DB. |
| Excel import | upload sample workbook | PENDING | Requires App_Data write permission and safe test DB. |
| Excel import | preflight/validation | PENDING | Requires sample workbook. |
| Excel import | commit | PENDING | Only on safe test DB. |
| MainErp isolation | `/MainErp` | PASS | HTTP 404. |
| DevStart isolation | `/DevStart` | PASS | HTTP 404. |
| RunMode isolation | `/RunMode` | PASS | HTTP 404. |
| Payment/cashing exclusion | `/Pos/Payments/Index` | PASS | HTTP 404 with `EnablePosPaymentsCashing` false. |
| Payment/cashing exclusion | `/Pos/Cashing/Index` | PASS | HTTP 404. |
