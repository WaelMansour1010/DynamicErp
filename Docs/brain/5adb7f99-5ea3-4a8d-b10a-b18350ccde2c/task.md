# Project Extracts Integration Task List

- [x] Create project extract view models modeling AJAX payload (`ProjectViewModels.cs`)
- [x] Build flat modern tabular Editor inputs in dynamic jQuery view (`Create.cshtml` & `_ExtractItemsGrid.cshtml`)
- [x] Configure Ajax JSON creation action endpoint inside Controller (`ProjectExtractsController.cs`)
- [x] Implement the primary project extract save operations inside repository (`ProjectRepository.cs`)
- [x] Integrate robust double-entry financial posting engine under active transaction:
  - [x] Add clean re-post/edit deletion logic in `dbo.DOUBLE_ENTREY_VOUCHERS` and `dbo.Notes`
  - [x] Set up concurrent AppLock transaction-locked manual serial ID generation (`NextId`)
  - [x] Add balanced credit/debit posting matrix mapping client/project account leaf nodes
  - [x] Design helper resolution rules for safe leaf account fallback lookup
- [x] Verify project compilation via MSBuild (completed with 0 errors)
- [x] Draft technical implementation walkthrough (`walkthrough.md`)
