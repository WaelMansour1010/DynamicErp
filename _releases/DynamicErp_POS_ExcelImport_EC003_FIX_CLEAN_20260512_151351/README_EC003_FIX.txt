DynamicErp POS Excel Import EC003 Fix CLEAN Release
Generated: 2026-05-12 15:13:52

DEPLOY ONLY:
- Copy DEPLOY contents only to the client web root.
- Do not deploy SOURCE_PATCH.

Fix included:
- Workbook explicit branch code cells such as EC003 now have first priority over adjacent Arabic branch-name text.
- This prevents false ambiguous branch detection when the sheet has both EC003 and a broader Arabic branch label.

Validated with attached EC003.xlsx structure:
- Sheet 1 top cells include E1=EC003 and F1=محكمه جنوب القاهرة-زينهم.
- Parser/preflight now treats EC003 as the deterministic branch-code hint.

Includes previous clean Excel Import scope:
- KYC Excel preview + save step.
- Duplicate IPN warning for Excel invoices.
- Excel warning filters in sales screen.
- Same-branch overlapping Excel period guard.

Database:
- No new SQL script was added for this fix.
- Existing POS scripts 45 and 50 are included only for fresh/repair deployments.

Validation:
- node --check pos-transaction.js succeeded.
- MSBuild Debug Any CPU succeeded from clean worktree.
- MyERP.dll SHA256: E1733C968CF1F12350162D6B343E36BFFA83E61489560F35F7D5069ED5182013
