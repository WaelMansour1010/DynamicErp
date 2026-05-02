# Keshni POS System - Technical Handover Document

## 1. CURRENT WEB POS IMPLEMENTATION

### Entry Point
- **Controller**: `PosTransactionController` in `\Areas\Pos\Controllers\PosTransactionController.cs`
- **Main Action**: `Index()` method renders the main POS view
- **JavaScript Entry**: `pos-transaction.js` handles all client-side logic

### Key Files in Areas/Pos
- **Controllers**: `PosTransactionController.cs`, `PosLoginController.cs`
- **Models**: `PosSaveTransactionRequest.cs` and related DTOs
- **Data**: `PosSqlRepository.cs` for all database operations
- **Views**: `Index.cshtml` (single view for all POS operations)
- **Scripts**: `pos-transaction.js` for client-side logic
- **Stored Procedures**: Uses `usp_POS_SaveTransaction` for transaction saving

### Flow: UI → Controller → DB
1. **UI**: User fills POS form in `Index.cshtml`
2. **JavaScript**: Client-side validation and data preparation in `pos-transaction.js`
3. **Controller**: `PosTransactionController.Save()` method receives request
4. **Repository**: `PosSqlRepository.SaveTransaction()` calls stored procedure
5. **Database**: `usp_POS_SaveTransaction` performs actual database operations

## 2. WHAT WAS RECENTLY IMPLEMENTED (CRITICAL)

### User Context Loading
- Loads user context from `TblUsers`, `TblEmployee`, `TblBranchesData`, etc.
- Caches context in session after login
- Supports both full access and permission-based access via `ScreenJuncUser`
- **EmpID MUST be loaded from TblUsers.EmpID**

### Field Locking for Non-Admin Users
- Non-admin users cannot change branch, store, box by default
- Payment type, bank, and other fields locked for restricted users
- **EmpID must be locked for non-admin users**
- Controlled by `CanChangeDefaults` flag from user context

### KYC Logic (Only for Keshni Card)
- Special handling for card transactions requiring full KYC
- Automatically creates/updates `TblCusCsh` records for card customers
- Uses fixed customer ID=2 for ALL transactions (default accounting customer)
- Mobile number always required for all transaction types
- KYC fields only populated for Keshni Card transactions, KYC data in separate fields

### Mobile Validation
- Strict Egyptian mobile format validation (010, 011, 012, 015 + 8 digits)
- Applied to all phone number fields
- Implemented in both client-side JavaScript and server-side C#

### Service/Item Loading Logic
- Dynamic item search with `GetItems()` method
- Service-specific item loading with `GetDefaultServiceItem()`
- Supports hierarchical service selection (primary/secondary service items)

### Violations Handling
- Special transaction type for violations (`Item_ID = 20`)
- Required fields: Violation value, payment type, pocket number
- Dedicated UI section and validation rules

### Separation of IPN vs ID
- **ID** field maps to `Transactions.IPN` (customer document ID)
- **IPN** field maps to `Transactions.ManualNo` (internal customer reference)
- Clear distinction in UI and database mapping

### NoteSerial Fix Attempt
- Previously stored as float (scientific notation issues)
- Now properly handled as string
- Still has issues with type conversion in database

## 3. FIELD MAPPING (CRITICAL)

### Correct Field Mapping
- **Screen "ID"** → `Transactions.IPN` (customer document ID)
- **Screen "IPN"** → `Transactions.ManualNo` (internal customer reference)
- **`Transactions.NoteSerial`** → Journal voucher number (string)
- **`Transactions.NoteSerial1`** → Secondary journal reference (string)

### Critical Note
- Screen "ID" and Screen "IPN" are NOT system identifiers
- They are NOT foreign keys
- They are NOT used for relational joins
- They are only external reference text fields (bank/customer reference)
- Required for saving transactions
- Used for display and tracking only
- Do NOT treat these fields as numeric IDs or system-generated keys
- Do NOT use them in joins or business logic

### Previous Bugs (Now Partially Fixed)
- **IPN/ID merge issue**: Previously both mapped to same field
- **NoteSerial corruption**: Stored as float causing scientific notation (e.g., 2.026E+10)
- **Still partially broken**: Scientific notation bug still exists in some code paths

## 4. TRANSACTION SAVE LOGIC

### Transactions Table Insertion
- Called via `PosSqlRepository.SaveTransaction()` → `usp_POS_SaveTransaction`
- Populates core fields: `TransactionDate`, `BranchId`, `StoreID`, `UserID`, `Emp_ID`
- Sets fixed customer ID = 2 for all transactions (KYC data in separate fields)
- Generates `NoteSerial` and `NoteSerial1` via voucher coding functions

### Transaction_Details Insertion
- Passed as structured table parameter `@Items`
- Columns include: `Item_ID`, `Quantity`, `Price`, `UnitId`, `Vat`, `CostPrice`
- Unit conversion applied via `QtyBySmalltUnit`
- Store information preserved in `StoreID2`

### Missing/Default Fields vs VB6
- `CusID` always set to 2 (default customer)
- `Transaction_Type` always 21 (POS sale)
- Missing: `NOTS` field linking to Issue Voucher
- Missing: proper accounting entry generation
- Missing: cost update logic (`UpdateTransactionsCost`)

## 5. SERVICE / ITEM BEHAVIOR

### Transaction Type Selection
- **Cash In**: Standard service selection
- **Cash Out**: Special service items (6,7,8,10) require secondary service
- **Card**: Requires VisaNumber and special KYC handling
- **Violations**: Uses fixed Item_ID = 20, special validation

### cmbItemName and cmbItemName2 (from VB6)
- Replaced with dynamic service selection via API calls
- `GetPrimaryServiceItems()` and `GetSecondaryServiceItems()`
- No direct equivalent of VB6 combo boxes

### Current Issues vs Expected
- **Services not loading**: API calls sometimes return empty results
- **Secondary services broken**: Validation and selection not working
- **Item search slow**: Performance issues with large item database
- **cmbItemName and cmbItemName2 mapping from VB6 is incorrect and incomplete**: Current Web replacement doesn't match VB6 behavior

## 6. ACCOUNTING LOGIC (IMPORTANT)

### Current Implementation
- **Calls stored procedure** `usp_POS_SaveTransaction`
- **No real accounting entries created** in `DOUBLE_ENTREY_VOUCHERS`
- **Fake entries created** but not linked to actual transactions
- **PG() function from VB6 not implemented**

### What's Missing vs VB6 PG() Logic
- **Accounting logic MUST be copied exactly from VB6**: FrmSaleBill6 → Sub PG()
- **No custom accounting logic allowed** - must replicate VB6 behavior exactly
- **Account mapping**: No `get_account_code_branch()` or `GetMyAccountCode()`
- **Debit/Credit entries**: No proper accounting entries created
- **VAT handling**: No VAT calculations or entries
- **Payment method entries**: Cash/Bank/Card accounts not properly linked
- **Cost of goods**: No inventory reduction or cost of goods entries
- **Commission entries**: Bank commissions for card payments missing

### Why Current Journal is Incorrect
- **No balanced entries**: Debits don't equal credits
- **Wrong account codes**: Using dummy values instead of real accounts
- **Missing entries**: Many required entries completely omitted
- **No validation**: No checks for accounting integrity

## 7. CURRENT BUGS / GAPS

### Validation Issues
- **Reading wrong fields**: KYC validation uses wrong field names
- **Mobile validation not consistent**: Sometimes bypassed
- **Required field validation incomplete**: Some fields not properly checked

### Service/Item Loading
- **Services not loading**: API calls sometimes return empty results
- **Secondary services broken**: Validation and selection not working
- **Item search slow**: Performance issues with large item database
- **cmbItemName and cmbItemName2 mapping from VB6 is incorrect and incomplete**: Current Web replacement doesn't match VB6 behavior

### Functional Issues
- **Violations not auto-adding item**: Manual selection required instead of auto-add Item_ID=20
- **Branch still editable**: Should be locked for restricted users
- **Employee (EmpID) not loaded**: Sometimes missing from context - MUST be loaded from TblUsers.EmpID and included in every transaction

### Database Field Issues
- **Missing DB fields**: Several important fields from VB6 not populated
- **NoteSerial corruption**: Still stored as float in some places
- **Wrong CusID**: Should allow different customers, not fixed to 2

### Critical Missing Features
- **Issue Voucher creation**: No linking to Issue Voucher (Transaction_Type=19)
- **Accounting entries**: No proper double-entry accounting
- **Cost updates**: No `UpdateTransactionsCost` calls
- **Commission logic**: Different from VB6 calculations

## 8. WHAT CLAUDE MUST DO NEXT

### Field Mapping and Validation Fixes
- [ ] Fix validation field mapping to match database schema
- [ ] Restore proper IPN/ID field handling (currently reversed)
- [ ] Fix NoteSerial type handling to always use string, never float
- [ ] Restore proper CusID loading instead of fixed value 2
- [ ] Ensure EmpID is loaded from TblUsers.EmpID and locked for non-admin users

### Item/Service Loading Restoration
- [ ] Restore item/service loading from VB6 logic
- [ ] Fix combo box equivalent functionality for service selection
- [ ] Implement auto-add of Item_ID=20 for violations
- [ ] Fix secondary service validation for Cash Out transactions

### Accounting Implementation
- [ ] Implement full PG() accounting logic from VB6
- [ ] Create proper accounting entries in DOUBLE_ENTREY_VOUCHERS
- [ ] Implement get_account_code_branch() and GetMyAccountCode() equivalents
- [ ] Add proper VAT, discount, and commission handling
- [ ] Ensure all entries are balanced (debits = credits)

### Transaction Processing Completion
- [ ] Implement proper Issue Voucher creation (Transaction_Type=19)
- [ ] Add NOTS field linking between transactions
- [ ] Implement UpdateTransactionsCost calls for cost calculations
- [ ] Add Multi-store logic for StoreID2 grouping

### Required Stored Procedures/Functions
- [ ] usp_POS_SaveTransaction: Complete implementation with proper accounting
- [ ] Voucher_coding/Notes_coding: Serial number generation functions
- [ ] get_account_code_branch: Branch-specific account code lookup
- [ ] GetMyAccountCode: Table-based account code lookup
- [ ] UpdateTransactionsCost: FIFO cost calculation logic

### Validation and Quality
- [ ] Add comprehensive field validation matching VB6 behavior
- [ ] Restore proper user permission checking
- [ ] Implement complete mobile number validation
- [ ] Add proper error handling and user feedback

## 9. IDENTIFIER TYPES (CRITICAL)

There are 3 different types of identifiers in the system:

1) System IDs:
- Transaction ID
- NoteSerial (journal voucher)
Used for internal linking only

2) Operational Serials:
- Ser
- Voucher numbering
Used for sequencing

3) Reference Text Fields:
- Screen "ID" → Transactions.IPN
- Screen "IPN" → Transactions.ManualNo

These are:
- Text only
- Not keys
- Not used in joins
- Required for saving only