# UI_UX_CONSTITUTION.md

## DynamicErp Unified UI/UX Constitution

> This document defines the mandatory visual identity, UX behavior, layout philosophy, interaction rules, and enterprise persona for DynamicErp, MainErp, and Kishny POS.
>
> The purpose is to stop inconsistent screen design, random button placement, duplicated UX behavior, and disconnected visual styles.

---

# 1. Core UI Principle

DynamicErp must feel like ONE enterprise system.

Not:

- One screen designed as Bootstrap demo
- Another screen designed as desktop clone
- Another screen designed as admin template
- Another screen designed as mobile app

The user must feel:

- Same visual language
- Same behavior
- Same navigation philosophy
- Same grid behavior
- Same save/edit workflow
- Same dialogs
- Same spacing and typography

Even when modules differ.

---

# 2. System Persona

## Official Persona

DynamicErp is:

# Enterprise Arabic RTL Power System

Meaning:

- Professional
- Stable
- Fast
- Dense but readable
- Business-first
- Productivity-first
- Arabic RTL native
- Suitable for long daily operational use
- Suitable for accountants, tellers, HR users, managers, and data-entry users

It is NOT:

- A flashy startup dashboard
- A marketing website
- A colorful toy interface
- A mobile-first casual UI
- A random Bootstrap collection

---

# 3. Global Visual Identity

## 3.1 Unified Theme

The system must use one coherent theme system.

Do not create random visual identities between screens.

Examples of forbidden behavior:

- One screen with rounded modern cards
- Another screen with sharp legacy buttons
- One screen dark
- Another screen completely unrelated light
- One screen using random colors
- Another using grayscale only

## 3.2 Color Philosophy

Use calm enterprise colors.

Primary goals:

- readability
- hierarchy
- status clarity
- focus

Avoid:

- excessive gradients
- neon colors
- oversaturated buttons
- decorative animations
- excessive shadows

## 3.3 Typography

Arabic readability is mandatory.

Preferred characteristics:

- Clear Arabic font
- Comfortable spacing
- Medium density
- Good readability on long sessions

Do not:

- shrink fonts excessively
- use decorative fonts
- mix many font styles

---

# 4. RTL Constitution

## 4.1 Arabic Is First-Class

RTL support is mandatory.

The system is not an English system translated later.

Everything must respect RTL:

- forms
- grids
- dialogs
- filters
- tabs
- printing
- reports
- validation messages
- dropdown alignment
- icons near text

## 4.2 Alignment Rules

Arabic labels and controls must align consistently.

Do not create mixed alignment chaos.

## 4.3 Keyboard Flow

Keyboard-heavy operation is expected.

Users may spend 8+ hours daily inside the system.

Therefore:

- Enter navigation should work logically.
- Tab order must be clean.
- Focus states must be visible.
- Fast data-entry behavior is important.

---

# 5. Layout Constitution

## 5.1 Standard Screen Structure

Most enterprise screens should follow this structure:

```text
Page Header
Toolbar / Actions
Filters / Search Area
Main Content Grid or Form
Totals / Summary
Status / Validation Area
```

## 5.2 Page Header

Every major screen should clearly display:

- screen title
- current mode if relevant
- branch/context if relevant
- key status if relevant

Do not hide critical operational context.

## 5.3 Toolbar Rules

Toolbar/button placement must be unified.

Expected actions:

- New
- Save
- Edit
- Delete
- Refresh
- Print
- Export
- Search
- Close/Cancel

Do not randomly move buttons between screens.

## 5.4 Save/Cancel Consistency

Save and cancel behavior must feel identical across modules.

Users should not relearn each screen.

---

# 6. Grid Constitution

## 6.1 Grids Are Core Enterprise Components

The grid is one of the most important enterprise UI elements.

Grids must feel consistent everywhere.

## 6.2 Mandatory Grid Features

Where appropriate, grids should support:

- Search
- Filtering
- Sorting
- Export
- Totals
- Column resize
- Column reorder
- Pagination or virtualization when needed
- Persistent layout where useful

## 6.3 Density Philosophy

Enterprise users prefer information density.

Do not waste huge empty spaces.

But also:

Do not overcrowd screens to the point of unreadability.

## 6.4 Totals and Financial Data

Financial totals must be visually clear.

Important totals should not disappear inside random UI clutter.

## 6.5 Selection Rules

Selection behavior must be predictable.

Avoid inconsistent:

- row-click behavior
- checkbox behavior
- double-click actions
- edit triggers

---

# 7. Form Constitution

## 7.1 Field Consistency

Equivalent fields should look equivalent across screens.

Examples:

- date fields
- amount fields
- phone fields
- account lookups
- branch selectors
- employee selectors

## 7.2 Required Fields

Required fields must be visually obvious.

## 7.3 Validation Messages

Validation must be:

- clear
- short
- actionable
- visually consistent

Do not show raw exception dumps to users.

## 7.4 Lookup Experience

Enterprise lookups should be fast.

Avoid:

- loading massive dropdowns unnecessarily
- freezing screens
- repeating expensive lookups constantly

Use:

- search-based lookup
- caching where appropriate
- lazy loading

---

# 8. POS UX Constitution

## 8.1 POS Is Operational First

POS screens are not general ERP screens.

POS must optimize:

- speed
- cashier flow
- minimal clicks
- quick save
- quick printing
- operational clarity

## 8.2 POS Visual Rules

POS should:

- use larger controls
- reduce clutter
- emphasize totals and status
- lock defaults where possible
- reduce unnecessary interaction

## 8.3 POS Loading Rules

POS screens must avoid heavy startup operations.

Do not:

- load massive lists unnecessarily
- reload static data repeatedly
- block save flow with slow UI logic

## 8.4 POS Error Experience

Operational users must understand errors quickly.

Errors should:

- explain the issue clearly
- not expose raw stack traces
- preserve entered data where possible

---

# 9. MainErp UX Constitution

## 9.1 MainErp Is Power-User Oriented

MainErp users are often:

- accountants
- inventory managers
- HR staff
- administrators
- advanced operators

They require:

- dense information
- fast navigation
- filters
- exports
- advanced search
- keyboard support
- detailed grids

## 9.2 Enterprise Workflow Support

MainErp screens should support:

- review workflows
- audit visibility
- history where useful
- multi-step business operations
- operational clarity

## 9.3 Do Not Oversimplify Enterprise Screens

Do not convert enterprise workflows into shallow simplified demos.

Preserve operational power.

---

# 10. Shared Screen Constitution

## 10.1 Shared Screens Must Adapt

Shared screens may behave differently depending on context.

Examples:

- POS sees operational subset.
- MainErp sees advanced configuration.
- Admin sees maintenance/configuration tools.

But:

The visual identity and core behavior must remain unified.

## 10.2 Shared Components

The following should eventually become reusable shared UI components:

- page headers
- toolbars
- lookup dialogs
- grids
- totals panels
- filter panels
- confirmation dialogs
- notifications
- print/export buttons

---

# 11. Button Constitution

## 11.1 Buttons Must Mean The Same Thing Everywhere

Examples:

- Green save button always means save.
- Red delete button always means destructive action.
- Print button should always behave similarly.

Do not randomly reinvent button semantics.

## 11.2 Dangerous Actions

Dangerous operations require confirmation.

Examples:

- delete
- reposting
- recalculation
- reversing accounting entries
- mass update

## 11.3 Loading States

Buttons must clearly show loading/progress state.

Avoid:

- repeated clicking
- duplicate save attempts
- frozen UI confusion

---

# 12. Dialog and Popup Constitution

## 12.1 Dialog Consistency

Dialogs must use unified styling.

Do not mix many modal libraries/styles randomly.

## 12.2 Confirmation Style

Confirmation dialogs must:

- clearly explain action
- clearly explain risk if destructive
- avoid vague wording

## 12.3 Notification Style

Notifications should:

- be short
- consistent
- not spam users
- visually differentiate success/warning/error/info

---

# 13. Reporting UI Constitution

## 13.1 Reports Are Operational Tools

Reports are not secondary features.

Reporting UI must support:

- filters
- branch selection
- date ranges
- export
- printing
- performance

## 13.2 Report Loading Experience

Heavy reports must:

- show progress
- disable repeated clicks
- avoid frozen UI confusion

## 13.3 Financial Reports

Financial reports require:

- aligned numbers
- clear totals
- readable density
- print-friendly layout

---

# 14. Print and Document Constitution

## 14.1 Printing Is Part of the Business Workflow

Printing is not optional decoration.

Receipt/report layout behavior is part of production behavior.

## 14.2 Arabic Printing

Arabic print direction, spacing, and alignment must be respected.

## 14.3 Existing Crystal/VB6 Reports

When the task says to preserve/report-match an existing Crystal Report or VB6 print layout:

- preserve business meaning
- preserve visual structure where possible
- preserve spacing logic
- preserve operational expectations

---

# 15. Screen Review Philosophy

The current project stage is NOT only migration.

The current stage is:

# Stabilization + Unification

Meaning:

- many important screens already exist
- the system cycle is partially operational
- the goal now is consistency and stability

Therefore:

Before building new screens:

1. Review existing migrated screens.
2. Unify visual language.
3. Unify UX behavior.
4. Standardize toolbars and grids.
5. Fix inconsistent layouts.
6. Improve shared components.
7. Improve session and permissions consistency.

---

# 16. Forbidden UI Behaviors

The following are forbidden unless explicitly approved:

- Completely different screen styles between modules
- Random button placement
- Random colors per screen
- Different save workflows for equivalent screens
- Huge empty decorative layouts
- Mobile-app styling inside enterprise accounting screens
- Heavy animations
- Excessive gradients
- Inconsistent grids
- Repeated duplicate filters everywhere
- Rebuilding shared UI components repeatedly
- Mixing too many frontend paradigms randomly

---

# 17. Mandatory UX Questions Before Any Screen Change

Before editing a screen, the agent must ask internally:

1. Does this screen follow the unified system persona?
2. Does this look like the rest of the system?
3. Are toolbar/buttons consistent?
4. Is the grid behavior consistent?
5. Is the RTL alignment correct?
6. Is keyboard flow usable?
7. Is this optimized for long operational use?
8. Is the design business-first or decorative-first?
9. Does this duplicate another visual pattern unnecessarily?
10. Would a real accountant/cashier/operator feel this belongs to the same system?

If the answer is no, redesign before finalizing.

---

# 18. Final UI/UX Law

DynamicErp must evolve into a unified enterprise platform.

Different modules may serve different operational roles.

But:

The user experience, visual language, interaction philosophy, and operational behavior must feel like ONE coherent professional system.

Consistency is more important than flashy redesign.

