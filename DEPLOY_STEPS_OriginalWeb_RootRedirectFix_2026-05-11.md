# Original Web Root Redirect Fix - Deploy Steps

Target: original DynamicErp web only.

## Package Contents

- `bin/MyERP.dll`

No `.cshtml`, JavaScript, SQL, or `Web.config` changes are required for this fix.

## Root Cause

The root route (`/`) is mapped to `DevStartController.Root`. A recent POS/dev-start change made the disabled production path redirect to:

`~/Pos/PosLogin/Index`

That made the original web root fall into Kishny POS login. The fix restores the production fallback to:

`~/Home/Index`

The POS area routes remain unchanged, so POS is still reached explicitly through `/Pos` when POS is enabled in the deployed configuration.

## Deploy

1. Back up production `bin/MyERP.dll`.
2. Copy the package `bin/MyERP.dll` to the original web `bin` folder.
3. Recycle the IIS application pool.
4. Test:
   - `/` should go to the original web home/login flow, not POS.
   - `/Home/Index` should stay original web.
   - `/Login` should stay original web login.
   - `/Pos` should still open POS login/dashboard only when explicitly requested and POS is enabled.
