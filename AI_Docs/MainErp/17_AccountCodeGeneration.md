# Account Code Generation

Implemented files:

- `Areas\MainErp\Interfaces\IAccountCodeGenerationService.cs`
- `Areas\MainErp\Models\Accounting\AccountCodePreview.cs`
- `Areas\MainErp\Services\Accounting\AccountCodeGenerationService.cs`

## Behavior

The service mirrors the VB6 `GetNewAcountCode` / `CHECK_LAST_ACCOUNT` pattern at a safe preview level:

- validates parent account exists;
- blocks child generation if `ACCOUNTS.last_account = 1`;
- reads children by `Parent_Account_Code`;
- detects the highest `parent + "a" + number` suffix;
- returns the next unused candidate;
- does not create accounts.

## Branch Awareness

Branch-aware account resolution is not implemented here yet. It should be added as a separate `BranchAccountResolver`, because VB6 `get_account_code_branch` reads branch/account mapping and sometimes forces branch behavior.

## Why Preview Only

LC and Project Extracts create accounts as a side effect of saving. That behavior is too risky to expose before account parent mappings, branch mappings, and duplicate rules are tested against production data.
