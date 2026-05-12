# DynamicErp Enterprise UI System

Date: 2026-05-09
Scope: `Areas/MainErp` and `Areas/Pos`

## Purpose

This document defines the shared Enterprise UI language introduced for DynamicErp MainErp and Kishny POS. The goal is to make both areas feel like one professional ERP product: calm, dense, readable, RTL-first, and consistent across dashboards, search screens, forms, reports, tables, and operational POS screens.

## Audit Summary

The UI audit found multiple competing visual systems:

- `main-erp-*` classes in MainErp.
- `pos-*` classes in POS transaction/report pages.
- `ep-*` classes in employee payroll/medical insurance pages.
- `voucher-*` classes in cashing/payments screens.
- `html-report-*` classes in report pages.
- Inline CSS blocks in multiple large POS pages.
- Bootstrap grid usage mixed with custom grid/flex systems.
- Different header styles, button styles, table spacing, card shadows, and filter layouts.
- Several pages using standalone `Layout = null`, which increases drift.

Main risks found:

- Search/filter panels are not visually consistent.
- Add/edit forms use different label and field spacing.
- Tables have different density, sticky header behavior, and horizontal-scroll handling.
- Action buttons differ between POS, MainErp, payroll, and reports.
- Several pages rely on inline styles or page-local CSS.
- MainErp topbar exposed database name to the user; this was removed from UI display.

## Implemented Design System

The shared system lives in:

- `Areas/MainErp/Content/enterprise-ui.css`
- `Areas/MainErp/Scripts/enterprise-ui.js`
- `Areas/MainErp/Views/Shared/_EnterprisePageHeader.cshtml`
- `Areas/MainErp/Views/Shared/_EnterpriseEmptyState.cshtml`

The CSS is loaded by:

- MainErp shared layout: `_MainErpLayout.cshtml`
- POS shared CSS files through `@import`:
  - `pos-transaction.css`
  - `employee-payroll.css`
  - `html-reports.css`
  - `financial-intelligence.css`
  - `print-template-designer.css`
- POS standalone Bootstrap-heavy screens:
  - `StockTransfer/Index.cshtml`
  - `PurchaseInvoice/Index.cshtml`
  - `SalesTargets/Index.cshtml`
  - `SalesRepresentativesPerformance/Index.cshtml`

## Visual Language

### Color

Use restrained enterprise colors:

- Background: `--erp-bg`
- Surface/card: `--erp-surface`
- Border: `--erp-border`
- Primary action: `--erp-primary`
- Muted text: `--erp-muted`
- Success/warning/danger: `--erp-success`, `--erp-warning`, `--erp-danger`

Avoid:

- Full-page dark heroes for operational screens.
- Large decorative gradients.
- Screen-specific palettes unless functionally meaningful.

### Typography

Default font stack:

```css
Tahoma, Arial, "Segoe UI", sans-serif
```

Rules:

- RTL-first.
- No negative letter spacing.
- Compact headings for operational screens.
- Use body-size text for dense ERP panels.

### Spacing

Shared spacing variables:

- `--erp-gap-xs: 6px`
- `--erp-gap-sm: 10px`
- `--erp-gap: 14px`
- `--erp-gap-lg: 18px`

Cards and sections should use 14-18px internal spacing. Avoid isolated inline margins except for dynamic progress widths or temporary visibility states.

## Core Classes

Page shell:

```html
<main class="erp-page">
```

Page header:

```html
<header class="erp-page-header">
  <div>
    <span class="main-erp-kicker">Module</span>
    <h1>عنوان الشاشة</h1>
    <p>وصف مختصر لوظيفة الشاشة</p>
  </div>
  <div class="erp-actions">...</div>
</header>
```

Card:

```html
<section class="erp-card">
  <div class="erp-section-title"><h2>بيانات أساسية</h2></div>
</section>
```

Filters:

```html
<section class="erp-filter-card">
  <form class="erp-filter-grid">
    <label class="erp-field">
      <span>من تاريخ</span>
      <input type="date" />
    </label>
    <div class="erp-actions">
      <button class="erp-btn erp-btn-primary">بحث</button>
    </div>
  </form>
</section>
```

Forms:

```html
<div class="erp-form-grid">
  <label class="erp-field">
    <span>الاسم العربي</span>
    <input type="text" />
  </label>
</div>
```

Tables:

```html
<div class="erp-table-wrap">
  <table class="erp-table">
```

Empty state:

```html
<div class="erp-empty">
  <strong>لا توجد بيانات</strong>
  <p>لا توجد نتائج مطابقة للبحث الحالي.</p>
</div>
```

Actions:

```html
<div class="erp-actions">
  <button class="erp-btn erp-btn-primary">حفظ</button>
  <a class="erp-btn">طباعة</a>
</div>
```

## Screen Standards

Every ERP screen should contain:

1. Page header:
   - Screen title.
   - Short description.
   - Optional module/eyebrow.
   - Actions in `erp-actions`.

2. Search/filter card:
   - `erp-filter-card`.
   - `erp-filter-grid`.
   - Labels above inputs.
   - Search/export/reset actions grouped at the end.

3. Data section:
   - `erp-table-card` or `erp-card`.
   - Table wrapped with `erp-table-wrap`.
   - Sticky headers when practical.
   - Empty state row or `erp-empty`.

4. Add/edit forms:
   - `erp-form-grid`.
   - `erp-field`.
   - Group long screens into cards/sections.
   - Checkboxes use `erp-checkbox`.

5. Responsive behavior:
   - Desktop: multi-column grids.
   - Tablet: auto-fit columns.
   - Mobile: single-column forms and stacked actions.

## Applied Screens

Unified foundation now applies to:

MainErp:

- Shared MainErp layout.
- MainErp dashboard and all screens inheriting `_MainErpLayout.cshtml`.
- MainErp search/list/detail pages using `main-erp-*`, `voucher-*`, and `main-erp-table`.

POS:

- POS dashboard.
- POS transaction.
- POS reports.
- POS login.
- POS closing.
- POS permissions.
- Accounting and HTML reports.
- Financial intelligence pages.
- Employee payroll and medical insurance pages.
- Cashing and payment voucher pages.
- Excel import pages.
- Stock transfer.
- Purchase invoice.
- Sales targets.
- Sales representatives performance.
- Print template designer.

## Still Needs Migration

The following still need targeted markup cleanup beyond the shared CSS layer:

- Large page-local `<style>` blocks in:
  - `Areas/Pos/Views/StockTransfer/Index.cshtml`
  - `Areas/Pos/Views/PurchaseInvoice/Index.cshtml`
  - `Areas/Pos/Views/SalesTargets/Index.cshtml`
  - `Areas/Pos/Views/SalesRepresentativesPerformance/Index.cshtml`
  - `Areas/Pos/Views/Payments/Index.cshtml`
  - `Areas/Pos/Views/JournalEntries/Index.cshtml`
  - `Areas/Pos/Views/PosReports/Index.cshtml`
- Backup files under POS should be excluded from future UI audits and publish checks.
- MainErp pages with mojibake Arabic text need encoding/content cleanup.
- MainErp/Pos visible references to source-system terms should be removed in a separate content-hardening pass where still present.

## CSS/JS Consolidation Report

Added:

- `Areas/MainErp/Content/enterprise-ui.css`
- `Areas/MainErp/Scripts/enterprise-ui.js`

Integrated:

- `Areas/MainErp/Views/Shared/_MainErpLayout.cshtml`
- POS CSS foundation files listed above.

Deleted:

- No CSS/JS files were deleted in this pass.

Reason:

- The current system is large and operational. Removing page-local CSS before visual regression coverage would be high risk. The shared system was introduced as a stable foundation first, then future migrations can gradually delete duplicated CSS.

## Before and After

Before:

- MainErp and POS used unrelated visual rules.
- Search cards, buttons, tables, and forms changed style per screen.
- Some screens exposed technical context such as database name in the topbar.
- Many tables were not consistently wrapped for horizontal overflow.

After:

- MainErp and POS share common tokens, cards, forms, filters, tables, actions, status pills, empty states, and responsive behavior.
- MainErp layout loads the shared system globally.
- POS high-use CSS files import the same system.
- Bootstrap-heavy POS screens now load the shared enterprise layer.
- MainErp no longer displays the database name in the user-facing topbar.

## Migration Rules Going Forward

1. Do not add new inline styles unless the value is truly dynamic.
2. Use `erp-page-header` for every new page header.
3. Use `erp-filter-grid` for search and reports filters.
4. Use `erp-form-grid` and `erp-field` for add/edit forms.
5. Use `erp-table-wrap` and `erp-table` for data grids.
6. Use `erp-actions` for all command groups.
7. Keep business logic, IDs, `name` attributes, AJAX endpoints, and bindings unchanged unless explicitly refactoring.
8. Treat `enterprise-ui.css` as the design-system source of truth.
