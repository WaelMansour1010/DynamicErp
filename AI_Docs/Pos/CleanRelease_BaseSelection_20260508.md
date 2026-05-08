# Clean Release Base Selection - 2026-05-08

Selected baseline: git `HEAD` commit `80b490c` on `main`.

Reason:
- `C:\WWWSite\cayshny\` was not available on this machine, so the deployed customer folder could not be used as a filesystem baseline.
- The current mixed worktree contains many uncommitted MainErp/POS/Excel/payment/cashing changes and was not used.
- `HEAD` already contains the approved POS deadlock retry/save-attempt logging code and the required POS SQL scripts.

Clean release worktree:
- `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508`
- Branch: `release/kishny-pos-deadlock-20260508`

Release package output:
- `F:\Source Code\DynamicErp_release_kishny_pos_deadlock_20260508\Releases\KishnyPOS_20260508`

Additional release-safety changes added on the release branch:
- `EnableMainErpMigration=false` prevents MainErp route registration.
- `EnableRunModeSelector=false` and `EnableDevStart=false` prevent `/RunMode` and DevStart route exposure.
- `EnableKishnyPos` explicitly controls POS route registration.
