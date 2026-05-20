# Phase 10 - Excluded Contracts
Date: 2026-05-20
Source DB: Adnan read-only diagnostics
ReadyToTest DB: $db

## Decision
The following 10 contracts remain excluded from ReadyToTest and should be Archive Only / Manual Review. They are source shell rows with blank/null critical links: property, unit, renter, dates, and account.

| OldContractNo | DisplayContractNo | Reason | Decision |
|---:|---|---|---|
| 331.0 | 219030003 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 689.0 | 120030012 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 690.0 | 120030012 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 721.0 | 219050021 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 805.0 | 121080003 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 945.0 | 119020012 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 946.0 | 121070003 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 947.0 | 121070003 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 1626.0 | 119060033 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |
| 1716.0 | 119080012 | Core fields blank: property/unit/renter/dates | Exclude From Pilot / Archive Only |

Raw output: Phase10_ExcludedContracts_raw.txt.
