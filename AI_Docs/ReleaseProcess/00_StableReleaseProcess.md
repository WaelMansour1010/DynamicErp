# Stable Release Process

Never deploy from a dirty mixed worktree. Customer deployment must come from a release or hotfix branch and a generated package.

Required flow:
1. Start from `main`.
2. Create `feature/*` for development.
3. Merge reviewed work to `develop`.
4. Create `release/<module>-YYYYMMDD` from a stable base.
5. Cherry-pick only approved changes.
6. Build package with module-specific package script.
7. Run release gate and smoke test.
8. Deploy only after GO.

Each release carries config, SQL, rollback notes, and a signed checklist.
