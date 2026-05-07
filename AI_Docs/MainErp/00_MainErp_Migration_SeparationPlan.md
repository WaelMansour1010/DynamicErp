# Main ERP Migration Separation Plan

## Scope

Target web area: `F:\Source Code\DynamicErp\Areas\MainErp\`

Target legacy ERP source: `F:\Source Code\SatriahMain\`

Reference-only Kishny legacy source: `F:\Source Code\SatriahMain\Cayshny\`

The first migration candidates are Letters of Credit and Project Extracts. This phase creates only the foundation, route shell, skeleton pages, placeholder SQL files, and documentation indexes.

## Separation Strategy

- Use physical MVC area isolation: `Areas\MainErp` is separate from `Areas\Pos`.
- Use route isolation: all new routes begin with `/MainErp`.
- Use independent controllers, view models, services, repositories, views, scripts, content, SQL, docs, helpers, and permissions under MainErp.
- Use only business-neutral patterns from POS/Sync work. Do not copy POS business logic.
- Keep MainErp navigation inside the MainErp layout until a reviewed global menu permission entry is designed.

## Reuse Rules

Allowed reuse:

- MVC area route pattern.
- Controller/view model layering.
- Arabic-first layout conventions.
- Grid, filter, export, report shell patterns when business-neutral.
- Permission-check pattern after defining MainErp permission names.
- SQL script organization under the owning area.

Forbidden reuse:

- POS sales invoice behavior.
- Card, token, KYC, commission, POS session, Kishny report, Kishny deployment, or Kishny branding logic.
- Any direct change under `Areas\Pos` for MainErp features.
- Any MainErp SQL under `Areas\Pos\Sql`.

## Feature Flags

Physical separation is the primary control. Optional app settings can be added later after review:

- `EnableMainErpMigration=true`
- `EnableKishnyPos=true`

Flags must not replace area/module separation.

## Phase 1 Decision

No global menu modification was made in this first pass. The MainErp landing page has its own isolated navigation for authorized users through `[Authorize]`. This avoids accidental leakage of Kishny/POS menu items while the permission model is documented.
