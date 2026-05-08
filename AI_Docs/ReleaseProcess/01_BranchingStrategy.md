# Branching Strategy

- `main`: stable production-ready base only.
- `develop`: active integration. Never deploy from this branch.
- `feature/*`: one task or module change per branch.
- `release/kishny-pos-YYYYMMDD`: approved Kishny POS release only.
- `release/mainerp-YYYYMMDD`: approved MainErp release only.
- `hotfix/kishny-pos-issue-name`: urgent customer fix from stable base.

Rules:
- Never deploy from local untracked files.
- Never deploy from `develop`.
- Never mix MainErp and POS in one customer package.
- Every package records branch and commit hash.
