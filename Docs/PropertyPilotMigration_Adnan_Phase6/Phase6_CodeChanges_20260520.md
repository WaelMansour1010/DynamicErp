# Phase 6 Code Changes

Date: 2026-05-20

## Files Modified

1. `F:\Source Code\DynamicErp\Utils\ERPAuthorize.cs`
2. `F:\Source Code\DynamicErp\Views\Shared\_Layout.cshtml`

## Change 1 - ERPAuthorize.cs

### Before

- Global `ERPAuthorizeAttribute` ignored `[AllowAnonymous]`.
- Anonymous login requests reached permission logging.
- `Log(...)` assumed `Id` and `RoleId` claims always exist.
- Missing claims caused `NullReferenceException` and HTTP 500.

### After

- `AllowAnonymous` actions/controllers return before ERP authorization checks.
- `SkipERPAuthorize` check remains and is centralized.
- User `Id` and `RoleId` claims are read through a null-safe helper.
- If an authenticated identity lacks expected claims, the user is signed out safely.
- If ERP authorization itself throws, access is denied safely and the exception is traced; it does not grant access.

## Security Impact

- No bypass was added.
- ERP authorization remains active for protected screens.
- Anonymous access is allowed only where MVC already marks the action/controller with `[AllowAnonymous]`.
- Invalid/malformed authenticated identities are not trusted.

## Change 2 - _Layout.cshtml

### Before

`Views/Account/Login.cshtml` defined `@section Scripts`, but `_Layout.cshtml` did not render it.

### After

Added:

```csharp
@RenderSection("Scripts", required: false)
```

near the end of the layout.

## Build

`MSBuild MyERP.csproj /t:Build /p:Configuration=Debug` completed successfully. Existing warnings only; no build errors.
