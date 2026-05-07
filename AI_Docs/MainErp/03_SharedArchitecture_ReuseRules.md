# Shared Architecture Reuse Rules

## Neutral Shared Code Allowed

Shared helpers can be introduced only when the behavior is business-neutral and belongs outside both POS and MainErp. Candidate folders:

- `F:\Source Code\DynamicErp\Common\`
- `F:\Source Code\DynamicErp\Infrastructure\`

Before creating shared helpers, inspect current project conventions and document why the helper is neutral.

## MainErp-Only Code

Keep these under `Areas\MainErp`:

- LC controllers, services, repositories, view models, scripts, CSS, views, SQL scripts, permissions, and docs.
- Project Extracts controllers, services, repositories, view models, scripts, CSS, views, SQL scripts, permissions, and docs.

## POS/Kishny-Only Code

Keep these under `Areas\Pos` or legacy `Cayshny`:

- POS invoice.
- Cards/tokens.
- KYC.
- Commissions.
- Kishny reports.
- POS session/deployment assumptions.
- Kishny-specific permissions and branding.

## Extraction Rule

If code mentions POS, Kishny, Cayshny, card, token, KYC, commission, or POS reports, it is not shared architecture.
