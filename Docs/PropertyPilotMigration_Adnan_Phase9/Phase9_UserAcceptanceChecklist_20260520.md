# Phase 9 - User Acceptance Checklist
Date: 2026-05-20
Pilot Clone: $clone

## Migration Acceptance
| Item | Expected / Action | Status |
|---|---|---|
| Confirm migrated active contracts | 283 contracts | Ready |
| Confirm excluded contracts | 10 contracts remain excluded/archive/manual review | Ready |
| Confirm opening balance | 1,156,544.6600 | Ready |
| Confirm future gross | 19,234,398.7085 | Ready |
| Confirm advance payments | 55,592.8900 staged, no historical journal posting | Ready |
| Confirm net remain | 19,178,805.8185 | Ready |

## Business Screens
| Screen | Acceptance Action |
|---|---|
| Properties | Open transferred buildings and check names/codes/types |
| Units | Open sample units and check unit number/type/status |
| Contracts | Open arrears, advance-payment, high-value, and normal contracts |
| Payment schedule | Compare installments, paid, remain for sampled contracts |
| Receipts | Test cash and bank receipt with operator approval |
| Termination | Test one contract termination and validate calculation |
| Reports | Review property contract, due batches, receipts, and renter balance reports |

## Payment Vouchers
| Scenario | Pilot Position |
|---|---|
| Cash direct expense | Allowed only with validated department expense account distinct from cashbox |
| Bank direct expense | Allowed only with validated department expense account distinct from bank account |
| Property owner payment / SourceTypeId=13 | Deferred / Manual Review only |
| Any payment producing same account on both sides | Blocked |

## Items To Show The Client
- Number of migrated contracts: 283.
- Number of excluded contracts: 10.
- Opening balance and net remain totals.
- Sample contract: ADNAN-C-1096.
- Sample advance-payment contract: ADNAN-C-2131.
- Sample high-value contract: ADNAN-C-1748.
- Notes explaining that old full accounting history was not migrated.
