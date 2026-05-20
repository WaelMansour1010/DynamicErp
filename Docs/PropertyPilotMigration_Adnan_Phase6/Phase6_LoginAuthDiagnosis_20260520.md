# Phase 6 Login/Auth Diagnosis - Adnan Property Pilot

Date: 2026-05-20  
Sandbox DB: `Alromaizan_PropertyPilot_Adnan_20260520`

## Root Cause

The original HTTP 500 on `/Account/Login` was caused by `ERPAuthorizeAttribute` running as a global action filter before the anonymous login page could render.

The login action has `[AllowAnonymous]`, but `ERPAuthorizeAttribute.OnActionExecuting` only checked `[SkipERPAuthorize]` and did not respect `[AllowAnonymous]`. Therefore unauthenticated requests reached `Log(...)` with `userId = 0`.

Inside `Log(...)`, the code executed:

```csharp
((ClaimsIdentity)HttpContext.Current.User.Identity).FindFirst("RoleId").Value
```

For unauthenticated requests there is no `RoleId` claim, so `FindFirst("RoleId")` returned `null`, causing `NullReferenceException`.

## Confirmed From Logs

Windows Application Event Log:

- Request URL: `http://localhost:63735/Account/Login`
- User authenticated: false
- Exception: `NullReferenceException`
- Method: `MyERP.ERPAuthorizeAttribute.Log(...)`
- File: `F:\Source Code\DynamicErp\Utils\ERPAuthorize.cs`

## Secondary Login Rendering Issue

After fixing auth filter behavior, `/Account/Login` exposed a second issue:

- Exception: `HttpException`
- Message: section `Scripts` defined but not rendered for layout `~/Views/Shared/_Layout.cshtml`.

This was caused by `Views/Account/Login.cshtml` defining `@section Scripts`, while `_Layout.cshtml` did not render that optional section.

## Not The Cause

- Not caused by `Adnan` data.
- Not caused by `Alromaizan_PropertyPilot_Adnan_20260520` missing users.
- Not caused by Branch/CashBox/UserDepartment/UserCashBox operational seed.
- Not a SQL connection failure.
