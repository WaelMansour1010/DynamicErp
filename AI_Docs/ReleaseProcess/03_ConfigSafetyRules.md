# Config Safety Rules

Production config must:
- use customer database only,
- set `debug=false`,
- set `EnableDevMasterPassword=false`,
- set `EnableDevStart=false`,
- set `EnableRunModeSelector=false`,
- set `EnableMainErpMigration=false` for Kishny POS,
- preserve customer `machineKey`,
- avoid committed secrets,
- avoid local DB names such as `Eng`, `Cash`, and `Wael\Sql2019`.

Do not copy local `Web.config` to a customer.
