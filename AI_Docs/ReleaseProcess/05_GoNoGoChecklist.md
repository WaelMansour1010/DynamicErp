# GO / NO-GO Checklist

GO requires:
- branch is `release/*` or `hotfix/*`,
- worktree is clean,
- Release build succeeded,
- package excludes MainErp, Excel test files, backup files, and AI docs,
- production config has debug/dev flags off,
- SQL package contains only approved module scripts,
- smoke tests pass,
- rollback plan exists,
- manual sign-off is recorded.

Any failed item is NO-GO.
