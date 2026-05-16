# Shared Screens Architecture - 2026-05-14

## Principle

Shared module does not mean shared route.

The correct pattern is:

- one shared business core;
- two safe area wrappers;
- two area layouts;
- two permission models;
- two context/database resolution paths.

POS must not link directly to MainErp routes. MainErp must not depend on POS routes.

## Screen Classification

| Screen / Module | Classification | Route Rule |
| --- | --- | --- |
| POS sale | POS-only | POS routes only |
| Kishny cards / KYC / card operations | POS-only | POS routes only |
| Cash in/out and POS closing | POS-only | POS routes only |
| POS Excel import and token invoice lookup | POS-only | POS routes only |
| Letters of Credit | MainErp-only | MainErp routes only |
| Project Extracts | MainErp-only | MainErp routes only |
| Full projects administration | MainErp-only | MainErp routes only |
| Full payroll administration and protected posting review | MainErp-only | MainErp routes only unless an explicit POS operational preview exists |
| Advanced finance/admin/posting tools | MainErp-only | MainErp routes only |
| Permissions and system setup | MainErp-only for full administration, POS-only for POS operational permissions | Separate wrappers |
| Users | Shared module with two shells | `/MainErp/Users` and `/Pos/PosLegacyAdmin/Users` |
| Banks | Shared module candidate with two shells | `/MainErp/Banks` or financial admin route, plus POS wrapper if needed |
| Boxes | Shared module candidate with two shells | `/MainErp/Boxes` or financial admin route, plus POS wrapper if needed |
| Stores | Shared module candidate with two shells | `/MainErp/StoreData` and `/Pos/Stores` |
| Items | Shared module candidate with two shells | `/MainErp/Items` and future POS-safe wrapper if needed |
| Employees | Shared module with visibility/admin split | MainErp full admin, POS limited operational visibility |
| Medical insurance | Shared module with visibility/admin split | MainErp admin/review, POS operational visibility |
| Reports | Shared module candidate | area route decides shell, permissions, context |
| Customers | Shared module candidate when POS customer/card behavior is separated from ERP customer/vendor administration | separate wrappers only |

## Implemented Shared Core

### Users

- Shared models: `Common/Users/SharedUserModels.cs`
- Shared repository: `Common/Users/SharedUsersRepository.cs`
- MainErp wrapper: `Areas/MainErp/Controllers/UsersController.cs`
- MainErp view: `Areas/MainErp/Views/Users/Index.cshtml`
- POS wrapper remains POS-native: `/Pos/PosLegacyAdmin/Users`

The shared user repository does not know which area called it. It accepts a caller-owned `SqlConnection` factory, so the wrapper controls database and context.

## Existing Shared-Core Direction

- `Common/StoreData` is the correct direction for stores/warehouses.
- `Common/EmployeePayroll` is the correct direction for shared employee/payroll preview logic where the shell decides whether the feature is full administration or limited POS visibility.
- Future banks/boxes/items sharing should follow the same approach: shared DTOs/repositories/services, but no shared route.

## Wrapper Rules

### MainErp Wrapper

- Uses MainErp layout.
- Uses MainErp authentication/session.
- Uses `MainErpDbConnectionFactory` and selected MainErp context.
- Uses MainErp legacy screen permission service where applicable.
- Links only to `/MainErp/...`.

### POS Wrapper

- Uses POS layout.
- Uses POS authentication/session.
- Uses POS/Kishny active context unless an explicit demo override is visible and protected.
- Uses POS permission service.
- Links only to `/Pos/...`.

## Shared Partial Rules

Shared partials are allowed only when they receive all URLs/actions through a view model.

Shared partials must not hard-code:

- `/MainErp/...`
- `/Pos/...`
- database names;
- branch assumptions;
- permission assumptions.

## Context Rules

- MainErp uses the selected MainErp context. For this QA pass it was `Eng`.
- POS uses its own POS context. For this QA pass POS admin opened against POS/Cash context.
- `Dania` must not leak into POS unless an explicit protected demo override is enabled and visibly badged.
- No shared repository should select a database by itself.
- Context badges should be shown where the user could otherwise confuse area/database/branch.

## Permission Rules

- MainErp permissions protect ERP administration screens.
- POS permissions protect POS operational screens.
- Admin override may be used to keep MainErp enterprise screens visible for QA/admin, but unauthorized users must get controlled redirects or controlled unauthorized responses.
- A shared service may expose capabilities, but the wrapper decides which capabilities are visible.

## No-Cross-Area-Routing Rule

Allowed:

- `/MainErp/Users` and `/Pos/PosLegacyAdmin/Users` both use a shared user repository.
- MainErp and POS each render their own layout and route.

Not allowed:

- POS sidebar linking to `/MainErp/Users`.
- MainErp sidebar linking to `/Pos/PosLegacyAdmin/Users`.
- shared partials containing hard-coded area URLs.
- MainErp controllers depending on POS controllers or POS session state.

## QA Results

| Check | Result |
| --- | --- |
| MainErp Users opens with `MainErp / Eng` context badge | Pass |
| MainErp Users search for `admin` loads | Pass |
| MainErp Users page contains no POS route leakage | Pass |
| POS Users opens in POS shell | Pass |
| POS Users page contains no MainErp route leakage | Pass |
| Anonymous MainErp routes redirect to MainErp login | Pass |
| Anonymous POS route redirects to POS login | Pass |
