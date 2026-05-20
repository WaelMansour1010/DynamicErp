# Phase 3 Web Validation - 2026-05-20

## Environment

| Item | Value |
|---|---|
| App path | `F:\Source Code\DynamicErp` |
| Server | IIS Express |
| Local port | `51220` |
| Sandbox DB | `Alromaizan_PropertyPilot_Adnan_20260520` |

## Result

| Test | Result |
|---|---|
| IIS Express startup | Passed |
| Root URL `/` | HTTP 200 |
| DevStart sandbox override | Attempted through local DevStart form/token |
| `/Property` | HTTP 302 redirect |
| `/PropertyUnit` | HTTP 302 redirect |
| `/PropertyContract` | HTTP 302 redirect |
| `/PropertyContract/AddEdit` | HTTP 302 redirect |
| `/CashReceiptVoucher` | HTTP 302 redirect |
| `/PropertyContractTermination` | HTTP 302 redirect |

## Conclusion

Full web validation was blocked by authentication redirects. No runtime data-screen validation, receipt creation, or termination test could be completed in this run.

## Required For Next Run

1. Provide or confirm a valid local admin login for the original DynamicErp Web project.
2. Or add an approved local-only QA bypass route for Pilot validation.
3. Keep the Debug database override pointing to the Sandbox only.
4. Re-run property screens after authenticated session is available.
