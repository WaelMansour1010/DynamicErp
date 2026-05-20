# Phase 5 Property Type Mapping Results

Date: 2026-05-20  
Database: Alromaizan_PropertyPilot_Adnan_20260520

## Result

Property type and unit type mapping were applied in Sandbox only.

## PropertyTypeId

| Old Table/Field | Old Id | Old Name | New Table | New Id | New Name | Status |
|---|---:|---|---|---:|---|---|
| TblAqar.aqartypeid | 1 | سكنية | PropertyType | 5 | سكنية | Mapped by code/name |
| TblAqar.aqartypeid | 2 | تجارية | PropertyType | 6 | تجارية | Mapped by code/name |

Important: 18 migrated active properties have qartypeid = NULL in Adnan, so Property.PropertyTypeId remained NULL. No unsafe default was forced.

## PropertyUnitTypeId

All 13 Adnan unit types were seeded/mapped into Sandbox PropertyUnitType with ADNAN-<OldId> codes to avoid wrong semantic collision with existing Alromaizan codes.

| Old Id | Old Name | New Id | New Name |
|---:|---|---:|---|
| 1 | شقة | 18 | شقة |
| 2 | شقه صغيرة ( ملحق ) | 19 | شقه صغيرة ( ملحق ) |
| 3 | غرفه صغيرة | 20 | غرفه صغيرة |
| 4 | محل | 21 | محل |
| 5 | فيلا | 22 | فيلا |
| 6 | برج اتصالات | 23 | برج اتصالات |
| 7 | فيلا - دور ارضي | 24 | فيلا - دور ارضي |
| 8 | فيلا - دور علوي | 25 | فيلا - دور علوي |
| 9 | مستودع | 26 | مستودع |
| 10 | حضانة | 27 | حضانة |
| 11 | مدرسه | 28 | مدرسه |
| 12 | مجمع | 29 | مجمع |
| 13 | مكتب | 30 | مكتب |

## Migration Impact

- PropertyDetail.PropertyUnitTypeId: 0 nulls for migrated units.
- PropertyContract.PropertyUnitTypeId: 0 nulls for migrated contracts.
- Property.PropertyTypeId: 18 nulls due missing source values.
