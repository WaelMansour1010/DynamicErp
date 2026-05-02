# DynamicErp (MyERP) - Project Overview

## Project Description

DynamicErp is a comprehensive Enterprise Resource Planning (ERP) system built for managing multiple business operations including accounting, property management, human resources, inventory, and hotel management. The system is designed as a multi-module web application with extensive reporting capabilities.

## Technology Stack

### Backend
- **MyERP Project**: ASP.NET MVC 5 (.NET Framework 4.8)
  - Entity Framework 6.5.1
  - OWIN Authentication
  - Microsoft.AspNet.Identity 2.2.4

- **HRProject**: ASP.NET Core 7.0
  - Entity Framework Core 7.0.10
  - ASP.NET Core Identity
  - AutoMapper 12.0.1
  - Dapper 2.0.143

### Frontend
- jQuery 3.3.1
- Bootstrap 3.4.1
- DevExpress 23.1.5 (UI components, reporting, charts)
- jQuery Validation

### Database
- SQL Server 2019
- Primary Database: `MyErp` (Server: `Pc2\Sql2019`)
- HR Database: `HROnline`

### Third-Party Integrations
- **AWS SDK**: S3 cloud storage
- **Azure**: Identity and Core services
- **Firebase Admin**: Push notifications
- **Oursms API**: SMS messaging
- **DevExpress XtraReports**: Enterprise reporting
- **DocumentFormat.OpenXml**: Document processing
- **ExcelDataReader**: Excel file handling

## Project Structure

```
DynamicErp/
├── Controllers/              # MVC Controllers (111+ files)
│   ├── AccountController.cs  # Authentication
│   ├── AccountSettings/      # Financial accounting
│   ├── ActivityManagement/   # Activity tracking
│   ├── EducationManagement/  # Employee education
│   ├── HotelManagement/      # Hotel operations
│   ├── HR/                   # Human Resources
│   ├── MedicalManagement/    # Medical benefits
│   ├── PartyManagement/      # Customer/Supplier management
│   ├── ProjectsManagement/   # Project tracking
│   ├── PropertyManagement/   # Real estate management
│   ├── SystemSettings/       # System configuration
│   └── WarehouseManagement/  # Inventory management
│
├── Models/                   # Domain Models (744+ entity classes)
├── Views/                    # Razor Views
├── ViewModels/              # View-specific models
├── Repository/              # Data Access Layer
│   ├── IRepository.cs       # Generic repository interface
│   └── Repository.cs        # Generic repository implementation
│
├── Utils/                   # Utility Classes
│   ├── AmazonHelper.cs      # AWS S3 integration
│   ├── SmsService.cs        # SMS sending
│   ├── Notification.cs      # Notification system
│   ├── MysoftErpEntity.cs   # EF DbContext
│   └── ERPAuthorize.cs      # Authorization
│
├── Reporting/               # Report Management
├── Scripts/                 # Database scripts
├── Content/                 # Static content
├── assets/                  # Frontend assets
│   ├── css/, js/, fonts/, images/
│   ├── libs/                # Third-party JS libraries
│   └── xtrareportsjs/       # DevExpress reporting JS
│
├── App_Start/               # Configuration
│   ├── BundleConfig.cs      # Asset bundling
│   ├── RouteConfig.cs       # URL routing
│   └── IdentityConfig.cs    # Identity configuration
│
├── HRProject/               # Separate HR Module (.NET Core 7)
│   ├── Controllers/
│   ├── Views/
│   ├── Models/
│   ├── Data/
│   ├── Auth/
│   └── wwwroot/
│
└── Documentation/           # Project documentation
```

## Core Modules

### 1. Accounting/Financial Management
- Chart of Accounts
- Cash Box Management
- Bank Account Transactions
- Cheque/Receipt Management
- Cost Centers
- Balance Reviews
- Debit/Credit Notifications

### 2. Property Management
- Property Contracts (with merged units support)
- Property Details & Units
- Property Bill Registration
- Contract Renewal & Termination
- Unit Status Management

### 3. Human Resources
- Employee Data Management
- Attendance & Leave Management
- Employee Opening Balance
- Educational Management
- Employee Approvals

### 4. Warehouse/Inventory
- Item Management
- Additional Items & Groups
- Alternative Items
- Assembly Vouchers
- Stock Transfers

### 5. Hotel Management
- Booking Instructions
- Borrow Requests
- Activity Management

### 6. Medical Management
- Medical benefits tracking

### 7. System Settings
- User Management
- Authorization/Permissions
- Module Access Control

## Architecture Patterns

### MVC with Repository Pattern
```
Request Flow:
Controller → Service/Utility → Repository → Entity Framework → Database
```

### Key Patterns

1. **Generic Repository Pattern**
   - Interface: `IRepository<T>`
   - Async/sync CRUD operations
   - Soft delete implementation (IsDeleted flag)
   - Paging support

2. **Entity Framework 6 (MyERP)**
   - DbContext: `MySoftERPEntity`
   - Stored procedures for complex operations
   - Entity data models from database

3. **Entity Framework Core (HRProject)**
   - DbContext: `HROnlineModel`
   - AutoMapper for DTO mapping
   - Modern async patterns

4. **Notification System**
   - Event-based notifications
   - Action tracking (Add, Edit, Delete, View)
   - Firebase push notifications
   - SMS via Oursms API

5. **Authorization/Security**
   - `ERPAuthorize` custom attribute
   - Claims-based identity
   - User permission checking on actions
   - Notification settings per user

## Naming Conventions

- **Controllers**: `[Entity]Controller.cs`
- **Views**: Organized by controller name in Views folder
- **Models**: Entity names match database tables
- **Namespaces**: `MyERP.Controllers.[Module]`

## Database Conventions

- Soft delete via `IsDeleted` boolean flag
- `Id` as primary key
- DateTime fields for timestamps
- Stored procedures for complex operations
- Foreign key relationships

## Configuration

### Database Connection
- Server: `Pc2\Sql2019`
- Main Database: `MyErp`
- HR Database: `HROnline`
- Authentication: Integrated SQL authentication

### Authentication
- OWIN-based (MyERP)
- ASP.NET Core Identity (HRProject)
- Cookie-based authentication
- Claims-based authorization

### External Services
- **Firebase Admin SDK**: Push notifications
- **AWS S3**: Cloud storage
- **Azure Identity**: Alternative cloud integration
- **Oursms API**: SMS messaging

## Recent Development Activity

Based on git history, recent features include:
- **[2026-01-07] Document Numbering Fix** - Thread-safe document number generation to prevent duplicate key violations
  - Implemented centralized `GetNextDocumentNumberSafe` stored procedure with AppLock protection
  - Fixed 6 stored procedures: PropertyDueBatch_Insert, CashIssueVoucher_Insert, CashReceiptVoucher_Insert, PropertyContractTerminate_Insert, PropertyContractJE_Insert, PropertBillRegisteration_Insert
  - Added Party tracking (PartyType, PartyId) to all journal entry details
  - Full documentation in `Scripts/` folder
- Property contract termination with merged units
- Contract renewal functionality
- SMS integration (Oursms API)
- Property contract batch handling
- Branch and unit editing after contract save

## Project Statistics

- Total C# Files: 1,251+
- Total Lines of Code: 177,150+
- Database Models: 744 entity classes
- Controllers: 111+ controller files
- NuGet Packages: 92 dependencies

## Development Notes

### Current Branch
- Main branch: `main`

### Modified Files (Current Status)
- `HRProject/appsettings.json`
- `Utils/SmsService.cs`

## Important Files

### Configuration Files
- `Web.config` - IIS and application configuration
- `HRProject/appsettings.json` - HR module configuration
- `packages.config` - NuGet dependencies (92 packages)
- `App_Start/` - Application startup configuration

### Core Files
- `Global.asax.cs` - Application lifecycle events
- `Utils/MysoftErpEntity.cs` - Main EF DbContext
- `Repository/Repository.cs` - Generic repository implementation
- `Utils/ERPAuthorize.cs` - Custom authorization attribute

### Documentation
- `CHANGES_SUMMARY.md` - Property contract feature changes
- `PropertyContract_Update_Instructions.md` - Update instructions
- `PropertyContract_Update_Fixed.sql` - Database migration
- `تطبيق_SQL_مطلوب.txt` - Required SQL applications (Arabic)

## Tips for Working with This Codebase

1. **Always read files before modifying** - The codebase is extensive with many interdependencies
2. **Check repository pattern usage** - Most data access goes through the generic repository
3. **Verify authorization attributes** - Controllers use `ERPAuthorize` for permission checks
4. **Test notification system** - Changes may trigger notifications to users
5. **Consider soft deletes** - Entities use `IsDeleted` flag instead of hard deletes
6. **DevExpress dependencies** - Many views rely on DevExpress controls
7. **Multi-project solution** - Be aware of MyERP (.NET Framework) vs HRProject (.NET Core) differences
8. **Database scripts** - Check Scripts/ folder for migration scripts before schema changes
9. **Document numbering** - Always use `GetNextDocumentNumberSafe` stored procedure for generating document numbers (thread-safe with AppLock)
10. **Journal entries** - Always include PartyType and PartyId in JournalEntryDetail for proper party tracking

## Known Issues and Solutions

### Fixed Issues

#### Document Number Duplication (Fixed: 2026-01-07)
**Issue:** Duplicate key violations in JournalEntry table due to race conditions
```
Cannot insert duplicate key row in object 'dbo.JournalEntry'
with unique index 'UX_JournalEntry_DocumentNumber'
```

**Solution:** All document numbering now uses the centralized `GetNextDocumentNumberSafe` stored procedure which implements:
- `sp_getapplock` for transaction-level locking
- `UPDLOCK + HOLDLOCK` for safe serial number retrieval
- Proper Party tracking in all journal entries

**Documentation:** See `Scripts/` folder for complete implementation details and deployment instructions.
