# Phase 10 - ReadyToTest Delivery Report
Date: 2026-05-20
Project: F:\Source Code\DynamicErp
Scope: DynamicErp main web property module only, not POS.

## Ready Database
| Item | Value |
|---|---|
| ReadyToTest DB | $db |
| Source of clone | Alromaizan_PropertyPilot_Adnan_PilotClone_20260520 after Phase9 cleanup |
| Source backup | $dir\Alromaizan_PropertyPilot_Adnan_PilotClone_20260520_to_Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520.bak |
| SQL Server | Wael\Sql2019 |
| Status | Ready for user testing |
| Rollback at end | Not executed |

## Connection / Runtime
Use the local debug selector:
1. Open http://localhost:63735/DevStart.
2. Select Original Web database: $db.
3. Open http://localhost:63735/LogIn.
4. Login with ErpAdmin using the local dev/test password already configured for this environment.

Connection string pattern if needed:

`	ext
Data Source=Wael\Sql2019;Initial Catalog=Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520;User ID=sa;Password=Admin@123;MultipleActiveResultSets=True
`

## Transferred Data
| Entity | Count |
|---|---:|
| Properties | 26 |
| Units | 258 |
| Tenants | 256 |
| Tenant accounts | 256 |
| Active contracts | 283 |
| Contract batches | 1,099 |
| Opening balance staging rows | 68 |
| Advance payment staging rows | 14 |

## Approved Financial Totals
| Metric | Value |
|---|---:|
| Opening Balance | 1,156,544.6600 |
| Future Gross | 19,234,398.7085 |
| Advance Payments | 55,592.8900 |
| Net Remain | 19,178,805.8185 |

## Operational Seed
- Pilot Branch exists.
- CashBox 1022 is linked to account 629.
- BankAccount 2024 is linked to account 631.
- Receipt payment methods CASH-PILOT and BANK-PILOT exist.
- Issue payment methods CASH-PILOT and BANK-PILOT exist.
- ErpAdmin is linked to Department 44 and CashBox 1022.

## Not Transferred
- The 10 source shell contracts with blank core links.
- Full old accounting history.
- Users or passwords from Adnan.
- Property owner payment scenario SourceTypeId=13 as an approved pilot operation.

## Delivery Status
The database is ready for business testing. It contains migrated property data and no test vouchers or test terminations.
