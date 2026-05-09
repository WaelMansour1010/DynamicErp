# Dynamic Report Designer — Execution Plan for Codex

> **هدف هذا الملف:** خطة تنفيذ احترافية ومجزَّأة، صالحة للإرسال المباشر إلى Codex جلسةً جلسة، لاستكمال مصمم التقارير الديناميكي في **DynamicErp** بدلاً من إعادة بنائه من الصفر.
>
> **المرجع التقني الأساسي:** نتائج Audit موثَّقة في هذا المستند نفسه (قسم §2). لا يوجد ملف Audit منفصل لتجنّب التشتت — كل ما يحتاجه Codex موجود هنا.
>
> **تاريخ الإصدار:** 2026-05-09
> **الفرع:** `claude/improve-report-designer-iAq77`
> **مالك الخطة:** Senior Product Architect (Claude Opus 4.7)
> **منفّذ الخطة:** Codex (جلسة لكل Phase)

---

## 1. ملخص تنفيذي

### 1.1 الوضع القائم (مفاجأة إيجابية)
الانطباع الأولي بأن مصمم التقارير «غير ناضج» **غير دقيق على المستوى المعماري**. الفحص يظهر:

- Solution كاملة تحت `/Areas/Reports/` تخدم 3 نطاقات (Web / POS / MainErp) من نفس الـ codebase.
- 5 جداول قاعدة بيانات بمؤشرات وقيود FK سليمة (`DynamicReportDefinitions`, `DynamicReportParameters`, `DynamicReportColumns`, `DynamicReportLayouts`, `DynamicReportPermissions`).
- Service Layer منفصل بشكل احترافي (Definition / Execution / Layout / Metadata / Permission / Sql Safety / Connection Factory / Designer State).
- نموذج أمني صارم: لا SQL حر، التحقق من Identifiers، Parameter binding مكتوب صحيح، صلاحيات Server-side.
- LayoutJson مُصدَّر (`designVersion: 2`) ومُطبَّع (Normalize) عند الحفظ.
- HTML5 Drag & Drop شغّال (Field → Group / Field → Column / Header reorder).
- RTL + عربية مدمجة من البداية.

### 1.2 السبب الحقيقي لانطباع «غير ناضج»
ليس مشكلة معمارية، بل **فجوات منتج** و**فجوات UX**:

| الفجوة | الأثر على المستخدم |
|---|---|
| لا يوجد Print Preview / HTML Print صفحة منفصلة | المستخدم لا يرى «التقرير كما سيُطبع» |
| لا يوجد تصدير PDF / XLSX (CSV فقط) | غير مقبول لعميل ERP حقيقي |
| لا يوجد Page Boundary مرئي على الـ Canvas | يشعر أنه يصمم في «صفحة بلا حدود» |
| لا يوجد Bands واضحة (Header/Detail/Footer) | التوقع كان Crystal-style، الموجود Grid-style |
| Parameter Lookups غير مرتبطة (`LookupSql` معرَّف لكن غير مستخدم) | المعاملات نص حر فقط، تجربة سيئة |
| لا توجد UI للصلاحيات (الجدول موجود) | الأدمن لا يستطيع منح صلاحية لتقرير محدد |
| Conditional Formatting Engine غير مكتمل | الـ JSON موجود بدون عرض فعلي |
| Group Collapse / Multi-sort UI غير ظاهرين | تجربة gridding ناقصة |
| Server-side filter غير موجود | بطء على بيانات > 5000 صف |

### 1.3 الاستراتيجية المعتمَدة
**Enhance, Don't Rebuild.** سننقذ كل ما هو موجود، ونغلق الفجوات بـ 9 Phases مرتَّبة حسب القيمة. الـ Vertical Slice الأولى (Phase 1) ستُثبت أن الدورة كاملة (تصميم → حفظ → تحميل → معاينة قابلة للطباعة) تعمل، وتغلق أكبر فجوة مرئية للمستخدم في وقت قصير.

### 1.4 ما هو **خارج النطاق** صراحةً
- ❌ إعادة كتابة المصمم من الصفر.
- ❌ استبدال HTML5 Drag & Drop بمكتبة (interact.js / GridStack / Konva).
- ❌ إدخال DevExpress / Telerik / Stimulsoft.
- ❌ بناء Banded Designer كامل بنمط Crystal Reports خلال هذه الخطة (يُؤجَّل إلى Phase 9 الاختيارية بعد إقرار صريح من المالك).
- ❌ تعديل Business Logic لـ POS أو MainErp.
- ❌ استبدال Schema قاعدة البيانات (إضافات فقط).

---

## 2. Audit مختصر (Single Source of Truth لـ Codex)

### 2.1 خريطة الملفات الفعلية (تم التحقق منها)

```
Areas/Reports/
├── ReportsAreaRegistration.cs
├── Web.config
├── Controllers/
│   ├── AdminController.cs           (143 LOC)
│   └── ViewerController.cs          (142 LOC)
├── Services/
│   ├── ReportDefinitionService.cs   (317 LOC)
│   ├── ReportExecutionService.cs    (169 LOC)
│   ├── ReportLayoutService.cs       (141 LOC)
│   ├── ReportMetadataService.cs     (140 LOC)
│   ├── DynamicReportSecurity.cs     ( 87 LOC)
│   ├── ReportSqlSafety.cs           ( 73 LOC)
│   ├── ReportPermissionService.cs   ( 59 LOC)
│   ├── DynamicReportConnectionFactory.cs (50 LOC)
│   └── ReportDesignerStateService.cs ( 32 LOC)
├── Models/
│   └── DynamicReportModels.cs       (131 LOC)
├── Views/
│   ├── Viewer/{Index, _ViewerBody}.cshtml
│   └── Admin/{Index, _AdminBody}.cshtml
├── Scripts/
│   ├── dynamic-reports-viewer.js    (854 LOC)
│   └── dynamic-reports-admin.js     (209 LOC)
├── Content/
│   └── dynamic-reports.css          (469 LOC)
└── Sql/
    ├── 01_DynamicReports_Schema.sql (129 LOC)
    ├── 02_DynamicReports_StoredProcedures.sql
    ├── 03_DynamicReports_SeedViews.sql
    └── 04_DynamicReports_LegacyPosMainErp_SeedViews.sql

Areas/Pos/Controllers/
├── DynamicReportsController.cs       (27 LOC)  → ترث من ViewerController
└── DynamicReportsAdminController.cs  (38 LOC)  → ترث من AdminController

Areas/MainErp/Controllers/
├── DynamicReportsController.cs       (27 LOC)
└── DynamicReportsAdminController.cs  (32 LOC)

AI_Docs/  (مرجعية فقط — لا يُعدَّل عليها)
├── DynamicReportDesigner_Architecture.md
├── DynamicReportDesigner_FinalReport.md
├── DynamicReportDesigner_CompletionHardening_Final.md
├── DynamicReportDesigner_UserGuide_AR.md
└── DynamicReportDesigner_AdminGuide_AR.md
```

### 2.2 جداول قاعدة البيانات (موجودة)
| الجدول | الغرض | حالة |
|---|---|---|
| `DynamicReportDefinitions` | تعريف التقرير + المصدر + الـ Scope | موجود |
| `DynamicReportParameters` | معاملات التقرير | موجود (LookupKey/LookupSql غير مستخدم) |
| `DynamicReportColumns` | أعمدة التقرير + خصائصها | موجود |
| `DynamicReportLayouts` | تخطيطات المستخدم لكل تقرير | موجود (LayoutJson NVARCHAR(MAX)) |
| `DynamicReportPermissions` | منح صلاحية لتقرير | موجود (لا UI لاستخدامه) |

### 2.3 Endpoints موجودة
```
GET  /Reports/Viewer/List?scope=Web|POS|MainErp
GET  /Reports/Viewer/Definition?id=X&scope=…
POST /Reports/Viewer/Execute?scope=…
GET  /Reports/Viewer/Layouts?reportId=X&scope=…
POST /Reports/Viewer/SaveLayout?reportId=X&layoutName=Y&isDefault=Z&scope=…
POST /Reports/Viewer/DeleteLayout?layoutId=X&scope=…

GET  /Reports/Admin/List?scope=…
GET  /Reports/Admin/Get?id=X&scope=…
POST /Reports/Admin/Save?scope=…
POST /Reports/Admin/LoadMetadata?scope=…&sourceType=…&sourceName=…
```

### 2.4 LayoutJson v2 الحالي (ما يحفظه المصمم الآن)
```jsonc
{
  "designVersion": 2,
  "visibleColumns":   { "<Field>": true|false },
  "columnOrder":      [ "<Field>", ... ],
  "captions":         { "<Field>": "..." },
  "widths":           { "<Field>": 120 },
  "alignment":        { "<Field>": "left"|"right" },
  "sort":             [ { "field": "...", "dir": "asc"|"desc" } ],
  "filters":          [ { "field": "...", "op": "...", "value": "...", "value2": "...", "logic": "and"|"or" } ],
  "groupBy":          [ "<Field>", ... ],
  "summaries":        { "<Field>": "sum"|"avg"|"count"|"min"|"max" },
  "formatting":       { "<Field>": { "format": "...", "decimals": 2 } },
  "conditionalFormatting": [],
  "pageSize": 50,
  "quickFilter": ""
}
```

### 2.5 ما يعمل بالكامل ✅
1. CRUD لتعريفات التقارير (Admin)
2. تحميل metadata من View / Stored Procedure (تلقائي)
3. تشغيل التقرير بـ parameters آمنة
4. حفظ/تحميل/حذف Layouts (مفلترة بـ User+Scope)
5. Drag & Drop (Field→Group, Field→Column reorder)
6. Filters / Sort / Group / Summary (كله Client-side)
7. تصدير CSV
8. RTL + عربية
9. Permissions Server-side في Controllers

### 2.6 ما ينقص أو ضعيف ❌
1. صفحة Print/Preview HTML مستقلة (حاليًا الجدول داخل الـ shell فقط)
2. تصدير PDF / XLSX
3. Parameter Lookups (Dropdown من `LookupSql`)
4. Conditional Formatting Engine (يحفظ الـ JSON بدون تطبيقه)
5. UI لإدارة `DynamicReportPermissions`
6. Group Collapse/Expand
7. Multi-sort UI (الـ JSON يدعمه، الـ UI لا)
8. Server-side filter/pagination (للأداء)
9. Page boundary على الـ Canvas
10. Bands واضحة (Header / Detail / Footer)
11. Banded Designer (Crystal-style) — قرار: **مؤجَّل لـ Phase 9 الاختيارية**

### 2.7 مديونيات تقنية لا تستحق إصلاحًا الآن
- لا توجد Unit Tests (مقبول، الـ Services قابلة للاختبار لاحقًا)
- لا توجد Resource files لتعريب Admin (Hardcoded AR، مقبول مرحليًا)
- ملفات Backup (`MyERP_Backup_*.csproj`) في الجذر — تنظيف منفصل خارج هذه الخطة.

---

## 3. الفجوة بين الموجود والمطلوب (مصفوفة قرار)

| متطلب المستخدم | الحالة | القرار |
|---|---|---|
| شاشة إدارة قوالب | ✅ موجودة (Admin/Index) | **إبقاء + تحسين بسيط في Phase 1** |
| اختيار مصدر بيانات | ✅ موجود (LoadMetadata) | إبقاء |
| Drag & Drop | ✅ موجود (HTML5 native) | إبقاء + توضيح بصري في Phase 1 |
| Toolbox عناصر | ⚠️ جزئي (Field list فقط) | **Phase 1 يضيف Toolbox واضح** |
| Properties Panel | ✅ موجود (Side panel) | إبقاء + إثراء في Phase 4 |
| Bands (Header/Detail/Footer) | ❌ غير موجود | **Phase 9 — اختياري** |
| Preview بطباعة | ⚠️ Browser print فقط | **Phase 1 — قلب الـ Vertical Slice** |
| Export PDF | ❌ | **Phase 5** |
| Export Excel | ❌ | **Phase 6** |
| Export HTML Print | ❌ | **Phase 1** |
| حفظ/تحميل LayoutJson | ✅ موجود | إبقاء |
| Versioning للقالب | ✅ designVersion field | إبقاء، نقفز لـ v3 عند إضافة fields جديدة |
| صلاحيات | ⚠️ Backend موجود، UI لا | **Phase 2** |
| Parameter Lookups | ⚠️ Field موجود غير مستخدم | **Phase 3** |
| Server-side perf للـ Big Reports | ❌ | **Phase 7** |
| Group Collapse + Multi-sort | ❌ | **Phase 8** |
| Conditional Formatting | ⚠️ JSON موجود بدون Engine | **Phase 4** |

---

## 4. حدود النطاق (المهم جدًا)

### 4.1 In Scope (سننفّذه)
- إضافات على ملفات Areas/Reports الحالية فقط.
- إضافة Views / Scripts / Services جديدة عند الحاجة.
- إضافة أعمدة / جداول جديدة بـ ALTER آمن (`IF NOT EXISTS` / `IF COL_LENGTH IS NULL`).
- ترقية LayoutJson إلى v3 مع **التوافق الرجعي الكامل** مع v2.

### 4.2 Out of Scope (لن نلمسه)
- لن نعدّل أي Controller خارج `Areas/Reports/`, `Areas/Pos/Controllers/DynamicReports*`, `Areas/MainErp/Controllers/DynamicReports*`.
- لن نعدّل Web.config الجذر.
- لن نمسّ أي Stored Procedure خارج `Areas/Reports/Sql/`.
- لن نعدّل Layout الرئيسي للنظام.
- لن نلمس POS/MainErp business logic (فواتير، مخزون، إلخ).
- لن نضيف مكتبات NuGet ثقيلة دون موافقة (انظر §10).

### 4.3 المكتبات المسموح اقتراحها
- **EPPlus** للـ XLSX — موجودة غالبًا في ERP، نتحقق أولًا. بديل: `OpenXML SDK` (مايكروسوفت رسمي، أخف).
- **DinkToPdf** أو **iTextSharp** للـ PDF — قرار خلال Phase 5 بناءً على وجودها مسبقًا.
- **بدون مكتبات Front-end جديدة** — HTML/CSS/Vanilla JS فقط.

---

## 5. منهجية تنفيذ كل Phase (Codex Discipline)

كل Phase تُعطى لـ Codex في جلسة مستقلة، بهذه الـ Discipline:

1. **اقرأ القسم المطابق فقط** من هذا الملف.
2. **لا تتجاوز الملفات المنصوص عليها** في الـ Phase.
3. **لا تعدّل قاعدة البيانات** إلا عبر الـ Migration script المنصوص عليه.
4. **لا تكسر التوافق الرجعي** مع LayoutJson القديم.
5. **في نهاية الـ Phase:**
   - يعمل Build بدون أخطاء.
   - تشغيل Smoke Test المذكور في Acceptance.
   - Commit واحد بعنوان واضح: `feat(reports): Phase N — <عنوان>`.
   - Push على `claude/improve-report-designer-iAq77`.
   - تحديث `Docs/Dynamic_Report_Designer_Implementation_Report.md` (إنشاؤه في Phase 1).

---

## 6. Phases

> **اصطلاح:** كل Phase لها بنية موحَّدة: الهدف / الملفات المتوقَّعة / SQL / الخطوات / Acceptance / اختبارات / مخاطر.

---

### Phase 0 — Smoke Verification (بدون كود)

**الهدف:** قبل لمس أي شيء، نتأكد من أن الـ baseline يعمل فعلًا في بيئة Codex، ونوثّق أي فشل.

**الملفات المتوقَّعة:**
- لا تعديلات. فقط ملف جديد: `Docs/Dynamic_Report_Designer_Smoke_Baseline.md`.

**SQL:** لا شيء.

**الخطوات:**
1. تأكَّد من تطبيق سكربتات `Areas/Reports/Sql/01..04` على قاعدة البيانات الافتراضية.
2. شغّل المشروع. افتح `/Reports/Admin/Index` — هل تظهر القائمة؟
3. افتح `/Reports/Viewer/Index` — هل يعمل؟
4. اختر تقرير seed، شغّله، احفظ Layout، أعد تحميله.
5. سجّل في `Smoke_Baseline.md`: ما يعمل، ما لا يعمل، أخطاء Console، أي 404.

**Acceptance:**
- [ ] الملف الموثَّق منشور.
- [ ] لا أخطاء حرجة (500/Compile errors). أي خطأ يظهر، يُسجَّل بالملف ولا يُحاول إصلاحه قبل Phase 1.

**اختبارات:** يدوية وفق الخطوات أعلاه.

**مخاطر:**
- قد يكشف الـ Smoke فجوات لم يلتقطها الـ Audit (مثلًا Connection String مفقودة لـ Pos في بيئة dev).
- إن لم يعمل أصلًا، Phase 1 يجب أن تبدأ بإصلاحات تشغيل قبل أي تطوير.

---

### Phase 1 — Vertical Slice: Print Preview + UX Clarity ⭐

> هذه هي الـ Vertical Slice المطلوبة. تُثبت الفكرة كاملةً وتغلق أكبر فجوة مرئية.
>
> **النطاق:** كل العناصر التي طلبها المالك صراحةً (Template List → Designer → Field List → Properties → Save/Load LayoutJson → Preview HTML)، مع تركيز على الجزء الناقص فعليًا = **Preview HTML قابل للطباعة + توضيح بصري للـ Canvas**.

**الهدف:**
1. التحقق من أن الدورة الكاملة تعمل من الـ Template List حتى Preview HTML قابل للطباعة.
2. إضافة صفحة **Preview قابلة للطباعة** مستقلة (`/Reports/Viewer/Print?reportId=X&layoutId=Y`).
3. توضيح بصري للـ Canvas: Page Boundary، Drop Zone واضحة، Selected State بارز، Mode Badge أوضح.
4. زر «معاينة قابلة للطباعة» في الـ Viewer شريط الأوامر يفتح الصفحة الجديدة في تبويب جديد.

**الملفات المتوقَّعة:**

جديدة:
- `Areas/Reports/Views/Viewer/Print.cshtml` — صفحة معاينة طباعة (Header شركة + Title + Filters Applied + Table + Footer مع رقم صفحة + تاريخ/مستخدم).
- `Areas/Reports/Views/Viewer/_PrintHeader.cshtml` (Partial اختياري لرأس الشركة).
- `Areas/Reports/Content/dynamic-reports-print.css` — CSS مخصَّص للطباعة (`@page`, `@media print`).
- `Docs/Dynamic_Report_Designer_Implementation_Report.md` — تقرير تنفيذ مستمر (Codex يضيف قسم لكل Phase).

تعديلات صغيرة:
- `Areas/Reports/Controllers/ViewerController.cs` — إضافة action واحد:
  ```
  public ActionResult Print(int reportId, int? layoutId, string scope)
  ```
  يُحضّر `DynamicReportExecutionResult` + الـ Layout (إن وُجد) ويمرّره للـ View. يُعيد استخدام `ReportExecutionService` و`ReportLayoutService` بدون أي خدمة جديدة.
- `Areas/Reports/Views/Viewer/_ViewerBody.cshtml` — إضافة زر «معاينة قابلة للطباعة» في الـ command bar (`<button id="btnPrintPreview">`).
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — wire up زر `btnPrintPreview`: يأخذ التقرير الحالي + Layout المختار ويفتح `window.open('/Reports/Viewer/Print?...', '_blank')`. **مهم:** Parameters التقرير تُمرَّر عبر POST → يُحلّ بحفظ مؤقت في sessionStorage ثم قراءته من الصفحة الجديدة، أو ببساطة بإضافة Form مخفي يُرسَل بـ `target="_blank"`. الحل المختار = **Form مخفي بـ POST** (أبسط، لا حاجة sessionStorage).
- `Areas/Reports/Content/dynamic-reports.css` — إضافة:
  - `.dr-canvas-page` (إطار صفحة A4 مرئي)
  - `.dr-canvas-page::before` content يدل على القياس (مثل `A4 — 21 × 29.7 سم`)
  - تعزيز `.dr-drop-zone--active` (highlight أوضح أثناء السحب)
  - `.dr-selected` (إطار 2px أزرق + handle ركن)
- `Areas/Reports/Views/Viewer/_ViewerBody.cshtml` — إضافة wrapper `.dr-canvas-page` حول جدول البيانات.

SQL: **لا تعديلات.**

**الخطوات:**
1. أنشئ branch من `claude/improve-report-designer-iAq77` إن لم تكن عليه.
2. أضِف `Print.cshtml` وموديل بسيط بـ ViewBag (`ReportName`, `Columns`, `Rows`, `AppliedFilters`, `GeneratedAt`, `GeneratedBy`).
3. أضِف Action `Print(int reportId, int? layoutId, string scope, FormCollection form)`:
   - تحقّق `CanView`.
   - استرجع التعريف.
   - اقرأ المعاملات من `form` (نفس أسماء parameters).
   - استدعِ `ReportExecutionService.Execute(...)`.
   - حمّل LayoutJson إن مرَّ `layoutId` ثم طبّق `visibleColumns` + `columnOrder` + `captions` + `formatting` على الـ Result قبل التمرير للـ View.
   - مرّر للـ View.
4. اكتب `dynamic-reports-print.css`:
   ```css
   @page { size: A4; margin: 1.5cm; }
   @media print {
     .no-print { display: none !important; }
     body { background: #fff; color: #000; font-size: 11pt; }
     table { border-collapse: collapse; width: 100%; }
     thead { display: table-header-group; } /* تكرار رأس الجدول كل صفحة */
     tfoot { display: table-footer-group; }
     tr { page-break-inside: avoid; }
   }
   ```
5. في `_ViewerBody.cshtml` لفّ الجدول داخل `<div class="dr-canvas-page">` وأضِف زر `<button id="btnPrintPreview" class="dr-btn">معاينة طباعة</button>`.
6. في `dynamic-reports-viewer.js` أضِف معالج النقر:
   ```js
   document.getElementById('btnPrintPreview').addEventListener('click', () => {
     const f = document.createElement('form');
     f.method = 'POST'; f.action = `${state.apiBase}/Viewer/Print`;
     f.target = '_blank';
     // reportId, layoutId, scope + جميع الـ params من state
     // append hidden inputs ...
     document.body.appendChild(f); f.submit(); f.remove();
   });
   ```
7. CSS للـ Canvas + Selected:
   ```css
   .dr-canvas-page {
     background: #fff;
     box-shadow: 0 0 0 1px #d7dde5, 0 4px 18px rgba(0,0,0,.06);
     padding: 1.5cm;
     margin: 12px auto;
     min-height: 29.7cm;
     max-width: 21cm;
   }
   .dr-drop-zone--active { outline: 2px dashed #22577a; background: #e8f3fb; }
   .dr-selected { outline: 2px solid #22577a; outline-offset: 1px; }
   ```
8. أضِف زر «معاينة طباعة» على شريط الأوامر بين «تشغيل» و«تصدير».
9. اكتب أول قسم من `Docs/Dynamic_Report_Designer_Implementation_Report.md`:
   - تاريخ، Phase، ما تم تغييره، روابط الـ commits.
10. `git add` للملفات المنصوص عليها فقط، commit، push.

**Acceptance:**
- [ ] صفحة `/Reports/Viewer/Print` تفتح وتعرض جدولاً مهيّأً للطباعة.
- [ ] الضغط على Ctrl+P يعطي معاينة متناسقة، رأس الجدول يتكرّر بين الصفحات.
- [ ] الـ Canvas في الـ Designer يعرض إطار صفحة مرئي.
- [ ] أثناء سحب حقل، الـ drop zone تتحوَّل لون/إطار واضح.
- [ ] العنصر المحدَّد له إطار أزرق ظاهر.
- [ ] لا أخطاء Console جديدة.
- [ ] التقارير القديمة (v2 layouts) تعمل بدون كسر.

**اختبارات يدوية:**
1. افتح Viewer، اختر تقرير seed، شغّله.
2. عدّل ترتيب الأعمدة بالسحب، احفظ Layout «اختبار».
3. أعد تحميل الصفحة، اختر Layout «اختبار»، شغّل التقرير.
4. اضغط «معاينة طباعة» → تبويب جديد بنفس الأعمدة بالترتيب المحفوظ.
5. اضغط Ctrl+P في التبويب الجديد → معاينة مهنيّة.
6. كرّر مع Scope = POS و MainErp.

**مخاطر:**
- قد لا تتوفر معلومات «اسم الشركة + الشعار» في `_PrintHeader`. **التخفيف:** قراءتها من `Web.config` أو `app_settings` إن وُجد، وإلا fallback لاسم نظام عام.
- POST + target=_blank يحجبه بعض المتصفحات. **التخفيف:** اختبار في Chrome/Edge/Firefox؛ إن فشل، استخدم sessionStorage + GET.
- LayoutJson قد يحوي حقول لا توجد فعلًا في الـ result (تقرير تغيّر مصدره). **التخفيف:** تطبيق Layout دفاعيًا — تجاهل أي field غير موجود.

---

### Phase 2 — Permissions UI (إغلاق فجوة أمنية)

**الهدف:** واجهة أدمن لمنح/سحب صلاحيات `CanView` و`CanDesign` و`CanExport` على مستوى تقرير-مستخدم/دور.

**الملفات المتوقَّعة:**
- `Areas/Reports/Views/Admin/_PermissionsPanel.cshtml` (Partial داخل Admin/Index بعد الـ Definition panel).
- `Areas/Reports/Controllers/AdminController.cs` — إضافة actions: `GetPermissions(int reportId, string scope)`, `SavePermission(...)`, `DeletePermission(int permissionId, string scope)`.
- `Areas/Reports/Services/ReportPermissionService.cs` — توسيع: `ListPermissions`, `Grant`, `Revoke` (transactional).
- `Areas/Reports/Scripts/dynamic-reports-admin.js` — قسم جديد للـ Permissions.
- `Areas/Reports/Models/DynamicReportModels.cs` — class `DynamicReportPermission`.

**SQL:** لا تعديلات (الجدول `DynamicReportPermissions` موجود).

**الخطوات:**
1. وسّع `ReportPermissionService` بدوال الإدارة (مع تحقق `CanDesign(user)`).
2. أضِف الـ actions في `AdminController`.
3. أضِف Partial `_PermissionsPanel.cshtml`: جدول صفوف (UserId/RoleId, CanView, CanDesign, CanExport) + إضافة جديدة + حذف.
4. JS يُحمِّل القائمة عند فتح تقرير في Admin، ويُتيح الإضافة/الحذف.

**Acceptance:**
- [ ] أدمن يستطيع رؤية صلاحيات تقرير محدد.
- [ ] إضافة منح لمستخدم/دور.
- [ ] حذف منح.
- [ ] مستخدم بلا منح لا يستطيع تشغيل التقرير (تأكيد عبر `/Reports/Viewer/Definition`).

**اختبارات:**
1. بصلاحية أدمن: امنح UserId=5 صلاحية `CanView` فقط على تقرير X.
2. سجِّل دخول كمستخدم 5: التقرير يظهر، الـ Export يفشل بصلاحية، الـ Designer ممنوع.

**مخاطر:**
- التحقق من وجود UserId/RoleId قبل الإدراج (FK مع Users؟). **التخفيف:** الـ Schema الحالي لا يفرض FK مع Users — نتحقق برمجيًا.

---

### Phase 3 — Parameter Lookups (Dropdowns)

**الهدف:** تحويل المعاملات من نص حر إلى Dropdown عند تعريف `LookupSql` (موجود بالفعل في الـ Schema، غير مستخدم).

**الملفات المتوقَّعة:**
- `Areas/Reports/Controllers/ViewerController.cs` — Action: `LoadLookup(int parameterId, string scope)`.
- `Areas/Reports/Services/ReportMetadataService.cs` — دالة `LoadParameterLookup(definition, parameterId, user)`.
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — render `<select>` بدل `<input>` عند `parameter.LookupSql`.
- `Areas/Reports/Views/Admin/_AdminBody.cshtml` — إضافة عمود LookupSql + LookupKey في جدول المعاملات.
- `Areas/Reports/Scripts/dynamic-reports-admin.js` — bind/collect الحقول الجديدة.

**SQL:** لا تعديلات.

**Acceptance:**
- [ ] Admin يستطيع كتابة `SELECT Id, Name FROM …` في `LookupSql`.
- [ ] الـ Viewer يعرض Dropdown مع Id/Name.
- [ ] Validate: `LookupSql` يجب أن يبدأ بـ `SELECT` ويُمنع كل ما عداه (ReportSqlSafety يضاف فحص ثانٍ).

**اختبارات:**
1. أنشئ معاملًا بـ LookupSql `SELECT BranchId, BranchName FROM Branches`.
2. شغّل التقرير: dropdown يظهر بالفروع.

**مخاطر:**
- LookupSql مكتوب من Admin = SQL حر بقدرٍ ما. **التخفيف:** قبول SELECT فقط، حظر `;`, `--`, `INSERT|UPDATE|DELETE|DROP|ALTER|EXEC|TRUNCATE|MERGE`. تنفيذ بـ `CommandTimeout` قصير (5s) و `MaxRows = 5000`.

---

### Phase 4 — Conditional Formatting Engine + Properties Panel ثري

**الهدف:** تفعيل القاعدة المُخزَّنة في `conditionalFormatting` داخل LayoutJson، وإثراء Properties Panel.

**الملفات المتوقَّعة:**
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — Engine بسيط:
  ```jsonc
  { "field":"Amount", "op":"gt", "value":1000, "style":{"bg":"#fde7e7","color":"#a00","bold":true} }
  ```
  يُطبَّق على الـ cell عند render.
- `Areas/Reports/Views/Viewer/_ViewerBody.cshtml` — قسم في Properties Panel لإدارة قواعد التنسيق.

**SQL:** لا تعديلات (designVersion يبقى 2 — مجرد إكمال لما هو معرَّف).

**Acceptance:**
- [ ] إضافة قاعدة من Properties Panel.
- [ ] حفظ Layout يُخزّن القاعدة.
- [ ] إعادة تحميل تُطبّقها على الـ cells المطابقة.

**اختبارات:**
1. على عمود Amount: قاعدة `gt 1000 → bg أحمر فاتح، نص أحمر داكن، bold`.
2. احفظ، أعد التحميل، تأكد من ظهور التنسيق.

**مخاطر:**
- Performance على Conditional قواعد كثيرة × صفوف كثيرة. **التخفيف:** حدّ أقصى 10 قواعد لكل تقرير، تحذير في الـ UI.

---

### Phase 5 — PDF Export

**الهدف:** زر «PDF» في الـ Viewer يصدّر التقرير الحالي إلى PDF.

**القرار التقني (Codex يبدأ بفحص النموذج المتاح):**
1. ابحث في `MyERP.csproj` عن أي مرجع لـ: `iTextSharp`, `iText7`, `DinkToPdf`, `Rotativa`, `wkhtmltopdf`, `Spire`, `Aspose`.
2. **إن وُجد:** استخدمه.
3. **إن لم يوجد:** اقترح **Rotativa.MVC** (Wrapper حول wkhtmltopdf، مجاني، شائع لـ ASP.NET MVC) في `Implementation_Report` ووقّف Phase حتى موافقة المالك.

**الملفات المتوقَّعة:**
- `Areas/Reports/Controllers/ViewerController.cs` — Action `ExportPdf(int reportId, int? layoutId, string scope, FormCollection form)`.
- `Areas/Reports/Services/ReportExportService.cs` (جديد) — منطق التصدير.
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — زر export PDF.
- إن استخدمنا Rotativa: تحويل صفحة `Print.cshtml` (من Phase 1) إلى PDF مباشرة (نفس الـ Razor، إعادة استخدام).

**SQL:** لا تعديلات.

**Acceptance:**
- [ ] زر «PDF» يُنزّل ملف `<ReportName>_<yyyyMMdd_HHmm>.pdf`.
- [ ] التنسيق مطابق لصفحة Print.cshtml.
- [ ] عربي/RTL يُعرض صحيحًا.

**مخاطر:**
- Rotativa يحتاج `wkhtmltopdf.exe` على الخادم. **التخفيف:** التوثيق + التحقق التلقائي عند Startup.
- خطوط عربية في PDF. **التخفيف:** تضمين `Tahoma`/`Arial Unicode` ضمن CSS الـ Print.

---

### Phase 6 — XLSX Export

**الهدف:** زر «Excel» يُصدّر بصيغة `.xlsx` (ليس CSV).

**القرار التقني:**
1. فحص `MyERP.csproj` عن `EPPlus` أو `OpenXml` أو `ClosedXML`.
2. الموصى به: **EPPlus 4.5.x** (نسخة LGPL القديمة، مجانية للاستخدام التجاري).
3. بديل: **ClosedXML** (مجاني، أبسط API).

**الملفات المتوقَّعة:**
- `Areas/Reports/Services/ReportExportService.cs` — توسعة بدالة `ExportXlsx(...)`.
- `Areas/Reports/Controllers/ViewerController.cs` — Action `ExportXlsx(...)`.
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — زر.

**SQL:** لا تعديلات.

**Acceptance:**
- [ ] ملف XLSX يفتح في Excel بصيغة Table مع headers.
- [ ] أنواع الأعمدة الرقمية والتاريخ مُهيّأة (Format).
- [ ] صفوف الـ Summary في الـ footer.

**مخاطر:** ترخيص EPPlus. اعتماد النسخة القديمة LGPL أو `ClosedXML`.

---

### Phase 7 — Server-Side Filter / Sort / Pagination (الأداء)

**الهدف:** للتقارير الكبيرة (>5000 صف)، الفلترة/الفرز/التصفّح يتم على الخادم بدلاً من المتصفّح.

**الملفات المتوقَّعة:**
- `Areas/Reports/Controllers/ViewerController.cs` — Action جديد `ExecutePaged(...)`.
- `Areas/Reports/Services/ReportExecutionService.cs` — دالة `ExecutePaged(request, paging, filters, sort, user)` تبني SQL ديناميكي **آمن** فوق الـ View/SP.
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — تبديل تلقائي للـ Server-side عند `rowCount > threshold`.
- `Areas/Reports/Models/DynamicReportModels.cs` — `DynamicReportPagedRequest`.

**SQL:** ربما إضافة helper function `ufn_BuildSafeWhere` (اختياري). الافتراضي: بناء `WHERE` بـ Parameterized SQL داخل C# مع whitelist للحقول من الـ Definition.

**Acceptance:**
- [ ] تقرير 50k صف يُحمّل أول 50 سريعًا.
- [ ] الفرز/الفلترة على الخادم، تحت 1.5 ثانية.
- [ ] فقط الحقول المعرَّفة في Definition قابلة للفلترة (تحقق Server-side).

**مخاطر:**
- Stored Procedures لا تقبل WHERE مضاف. **التخفيف:** للـ SP نُبقي الـ Client-side، للـ View نطبّق Server-side. توضيح ذلك في الـ UI.
- SQL Injection. **التخفيف:** Whitelist + Parameterized.

---

### Phase 8 — Group Collapse / Multi-Sort UI / Quality of Life

**الهدف:** إكمال تجربة الـ grid (Collapse/Expand للمجموعات، Multi-sort بالضغط Shift+Click، إعادة تعيين Layout بزر واحد، حالة Loading للجدول).

**الملفات المتوقَّعة:**
- `Areas/Reports/Scripts/dynamic-reports-viewer.js` — وسائل تفاعل جديدة.
- `Areas/Reports/Content/dynamic-reports.css` — أيقونات Collapse، Loading spinner.

**SQL:** لا تعديلات.

**Acceptance:**
- [ ] أيقونة `▾/▸` بجانب رأس المجموعة، تطوي الصفوف.
- [ ] Shift+Click على رأس عمود يضيف/يزيل Sort الإضافي.
- [ ] زر «إعادة تعيين» يعود للـ Definition defaults.
- [ ] Spinner واضح أثناء Execute.

**اختبارات:** يدوية وفق المعايير.

**مخاطر:** لا شيء كبير.

---

### Phase 9 — (اختياري) Banded Report Designer (Crystal-style)

> ⚠️ **هذه Phase كبيرة (5–10 أيام عمل). تُنفَّذ فقط بإقرار صريح من المالك.**

**الهدف:** وضع تصميم ثانٍ بجوار الـ Grid Designer الحالي، يدعم Bands حقيقية:
- Page Header / Page Footer
- Report Header / Report Footer
- Detail Band
- Group Header / Group Footer (بـ Group Field)

ومكوّنات قابلة للسحب (Label, Field, Image, Line, Box, Page Number, Date, Sum).

**القرار التقني:**
- إضافة `DesignerType` في `DynamicReportDefinitions` (`Grid` | `Banded`، الافتراضي `Grid`).
- LayoutJson v3 يُضيف بنية `bands[]` مع `elements[]`.
- مصمم جديد: `Areas/Reports/Views/Designer/Banded.cshtml` + `dynamic-reports-banded.js` (~1500 LOC متوقَّع).
- Renderer للـ Banded:
  - Web Preview: HTML/CSS Absolute positioning.
  - Print/PDF: Razor مع نفس الـ absolute positioning ضمن `@page` ثابت.

**SQL:**
```sql
IF COL_LENGTH('dbo.DynamicReportDefinitions','DesignerType') IS NULL
  ALTER TABLE dbo.DynamicReportDefinitions ADD DesignerType NVARCHAR(20) NOT NULL CONSTRAINT DF_DRD_DesignerType DEFAULT 'Grid';
GO
```

**Acceptance:**
- [ ] Admin يختار `DesignerType=Banded` عند إنشاء تقرير.
- [ ] فتح التقرير يفتح المصمم المختلف (Banded Canvas + Toolbox).
- [ ] سحب Label / Field على Detail Band.
- [ ] Save / Load LayoutJson v3.
- [ ] Preview ينتج Banded HTML.
- [ ] التقارير القديمة (Grid) تستمر بلا كسر.

**مخاطر:** ضخمة:
- بناء Renderer متطابق بين Web/Print/PDF صعب.
- WYSIWYG حقيقي يحتاج jelly fine-tuning.
- توقَّع 3 جلسات Codex على الأقل (Canvas → Properties + Toolbox → Renderer).

---

## 7. مخاطر شاملة وتخفيفها

| المخاطرة | التخفيف |
|---|---|
| كسر التقارير القديمة عند ترقية LayoutJson | كل ترقية v2→v3 تكون **Additive فقط** + دالة `MigrateLayoutJson(json)` في `ReportDesignerStateService` |
| إضافة مكتبة NuGet ثقيلة | كل Phase تُجبر Codex على فحص الموجود قبل الاقتراح، والاستئذان قبل الإضافة |
| كسر Pos أو MainErp بسبب تعديل في Areas/Reports | Phase Smoke في كل جلسة تشمل اختبار Pos+MainErp |
| Connection Strings مفقودة في بيئة dev | Phase 0 يكتشفها مبكرًا |
| Performance Regression على تقارير صغيرة عند تفعيل Server-side | Threshold قابل للضبط، الافتراضي 5000 |
| استهلاك ذاكرة في XLSX Export لتقارير كبيرة | Streaming Write + حد أقصى للصفوف عند التصدير |

---

## 8. ملحق A — مخطط LayoutJson v3 المقترَح

> **التوافق:** v3 ⊃ v2 (نفس الحقول + الإضافات التالية اختيارية).

```jsonc
{
  "designVersion": 3,
  // ... كل حقول v2 كما هي ...

  // Phase 4
  "conditionalFormatting": [
    { "field":"Amount", "op":"gt", "value":1000,
      "style": { "bg":"#fde7e7", "color":"#a00", "bold":true, "italic":false }
    }
  ],

  // Phase 7
  "serverSide": { "enabled": false, "pageSize": 50 },

  // Phase 8
  "collapsedGroups": [],
  "multiSort": [ { "field":"BranchId", "dir":"asc", "priority":1 } ],

  // Phase 9 (Banded)
  "bands": [
    { "kind":"PageHeader", "heightPx": 60,
      "elements": [
        { "type":"label", "text":"شركة …", "x":0,"y":0,"w":300,"h":24,"font":{"size":14,"bold":true} },
        { "type":"image", "src":"/images/logo.png", "x":700,"y":0,"w":80,"h":40 }
      ]
    },
    { "kind":"Detail", "heightPx": 22,
      "elements": [
        { "type":"field", "field":"InvoiceNo", "x":0,"y":0,"w":120,"h":20 },
        { "type":"field", "field":"Total", "x":600,"y":0,"w":100,"h":20, "format":"#,##0.00" }
      ]
    }
  ]
}
```

---

## 9. ملحق B — جدول الـ Migrations (إن لزمت)

| Phase | يلمس DB؟ | السكربت |
|---|---|---|
| 0 | ❌ | — |
| 1 | ❌ | — |
| 2 | ❌ | — |
| 3 | ❌ | — |
| 4 | ❌ | — |
| 5 | ❌ | — |
| 6 | ❌ | — |
| 7 | ❌ (اختياري function) | `Areas/Reports/Sql/05_DynamicReports_Helpers.sql` |
| 8 | ❌ | — |
| 9 | ✅ | `Areas/Reports/Sql/06_DynamicReports_DesignerType.sql` |

كل سكربت **DROP+CREATE** للـ SP، و**IF NOT EXISTS** للـ Tables/Columns. SQL Server 2012 compatible.

---

## 10. ملحق C — قواعد إضافة المكتبات (Codex Discipline)

قبل اقتراح أي NuGet جديد، Codex مُلزَم بـ:
1. فحص `MyERP.csproj` (`grep -i "<Reference Include=" MyERP.csproj`).
2. فحص `packages.config`.
3. فحص `bin_codex/` مجلد الـ binaries المنشورة.
4. **إن وُجد بديل بالفعل، استخدمه.**
5. **إن لم يوجد، أوقف Phase**، اكتب اقتراحًا في `Implementation_Report` يشرح:
   - الحاجة
   - 2 بدائل + ترخيص
   - حجم التبعية
   - ينتظر موافقة قبل التثبيت.

---

## 11. ملحق D — قائمة الملفات النهائية بعد كل Phases (تنبؤ)

```
Areas/Reports/
├── Controllers/
│   ├── AdminController.cs               (يُعدَّل في Phase 2)
│   └── ViewerController.cs              (يُعدَّل في Phases 1, 3, 5, 6, 7)
├── Services/
│   ├── ... (كل الموجود يبقى)
│   └── ReportExportService.cs           (جديد، Phase 5)
├── Models/
│   └── DynamicReportModels.cs           (يُوسَّع في Phases 2, 7, 9)
├── Views/
│   ├── Viewer/
│   │   ├── Index.cshtml
│   │   ├── _ViewerBody.cshtml           (يُعدَّل Phase 1, 4, 8)
│   │   ├── Print.cshtml                 (جديد، Phase 1)
│   │   └── _PrintHeader.cshtml          (اختياري، Phase 1)
│   └── Admin/
│       ├── Index.cshtml
│       ├── _AdminBody.cshtml            (يُعدَّل Phase 3)
│       └── _PermissionsPanel.cshtml     (جديد، Phase 2)
├── Scripts/
│   ├── dynamic-reports-viewer.js        (يُعدَّل في كل Phases تقريبًا)
│   └── dynamic-reports-admin.js         (يُعدَّل Phases 2, 3)
├── Content/
│   ├── dynamic-reports.css              (يُعدَّل Phase 1, 8)
│   └── dynamic-reports-print.css        (جديد، Phase 1)
└── Sql/
    ├── 01..04 (موجودة)
    ├── 05_DynamicReports_Helpers.sql    (Phase 7، اختياري)
    └── 06_DynamicReports_DesignerType.sql (Phase 9، اختياري)

Docs/
├── Dynamic_Report_Designer_Execution_Plan_For_Codex.md  (هذا الملف)
├── Dynamic_Report_Designer_Smoke_Baseline.md            (Phase 0)
└── Dynamic_Report_Designer_Implementation_Report.md     (Phase 1+، يُحدَّث كل Phase)
```

---

## 12. معيار القبول النهائي للمنتج (بعد Phase 1 على الأقل)

- ✅ المستخدم يفتح Viewer ويرى قائمة تقارير واضحة.
- ✅ يضغط تقريرًا، يدخل المعاملات، يضغط «تشغيل».
- ✅ يعدّل ترتيب الأعمدة بالسحب، يحفظ Layout بـ اسم.
- ✅ يضغط «معاينة طباعة» → تبويب جديد بصفحة جاهزة للطباعة.
- ✅ يطبع أو يصدّر CSV (PDF/XLSX بعد Phases 5/6).
- ✅ لا أخطاء Console، لا أزرار ميتة، لا نصوص Demo/Mock.
- ✅ التقارير القديمة لا تُكسَر.

---

# Next prompt for Codex Phase 1

> ⬇️ **انسخ الكتلة التالية كاملةً وأعطها لـ Codex في جلسة جديدة. لا تضف ولا تحذف.**

```text
أنت Senior ASP.NET MVC Engineer تعمل على مشروع DynamicErp في الفرع
claude/improve-report-designer-iAq77.

اقرأ أولاً (Read فقط، لا تعديل):
  Docs/Dynamic_Report_Designer_Execution_Plan_For_Codex.md

ثم نفّذ Phase 0 ثم Phase 1 من تلك الخطة فقط، بحرفية تامة:

================================
Phase 0 — Smoke Verification (دون كود)
================================
1. تأكد أن سكربتات Areas/Reports/Sql/01..04 مطبَّقة على قاعدة بيانات dev.
2. شغّل المشروع. اختبر يدويًا:
   - GET /Reports/Admin/Index
   - GET /Reports/Viewer/Index
   - تشغيل تقرير seed، حفظ Layout، إعادة تحميله.
   - كرّر مع Pos: /Pos/DynamicReports/Index
   - كرّر مع MainErp: /MainErp/DynamicReports/Index
3. أنشئ Docs/Dynamic_Report_Designer_Smoke_Baseline.md يحتوي:
   - تاريخ.
   - ما يعمل ✅
   - ما لا يعمل ❌ (مع نص الخطأ من Console و الـ Server log)
   - Routes مكسورة إن وُجدت.
4. لا تُصلح أي شيء في Phase 0. مجرد توثيق.

================================
Phase 1 — Vertical Slice: Print Preview + UX Clarity
================================

الهدف:
- إضافة صفحة معاينة طباعة مستقلة.
- توضيح بصري للـ Designer Canvas (إطار صفحة A4، Drop zones، Selected state).
- زر «معاينة طباعة» في شريط أوامر الـ Viewer.

الملفات المسموح إنشاؤها أو تعديلها (لا تتجاوزها):
  جديدة:
    - Areas/Reports/Views/Viewer/Print.cshtml
    - Areas/Reports/Views/Viewer/_PrintHeader.cshtml   (اختياري)
    - Areas/Reports/Content/dynamic-reports-print.css
    - Docs/Dynamic_Report_Designer_Implementation_Report.md   (افتح بقسم Phase 1)
  تعديل:
    - Areas/Reports/Controllers/ViewerController.cs
        أضف Action واحد فقط:
          [HttpPost] public ActionResult Print(int reportId, int? layoutId, string scope, FormCollection form)
        يستخدم ReportExecutionService و ReportLayoutService الموجودَين، بدون خدمات جديدة.
    - Areas/Reports/Views/Viewer/_ViewerBody.cshtml
        - لفّ الجدول داخل <div class="dr-canvas-page">
        - أضف زر <button id="btnPrintPreview" class="dr-btn">معاينة طباعة</button> في الـ command bar
    - Areas/Reports/Scripts/dynamic-reports-viewer.js
        - معالج نقر btnPrintPreview ينشئ Form مخفي بـ method=POST
          target=_blank action=`${state.apiBase}/Viewer/Print`
        - يضيف hidden inputs: reportId, layoutId (إن وُجد), scope,
          + كل المعاملات الحالية من state.parameters
    - Areas/Reports/Content/dynamic-reports.css
        - .dr-canvas-page (إطار صفحة A4 مرئي + ظل خفيف)
        - .dr-drop-zone--active (highlight أثناء السحب)
        - .dr-selected (إطار 2px أزرق)

قاعدة البيانات: لا تعديلات.

نقاط حرجة:
- لا تكسر Layouts v2 الموجودة.
- إذا layoutId مرَّ، طبّق visibleColumns/columnOrder/captions/widths/alignment/formatting
  دفاعيًا — تجاهل أي field غير موجود في الـ Result.
- صفحة Print.cshtml يجب أن:
  * تكون RTL.
  * تحتوي رأسًا (اسم التقرير + المعاملات المطبَّقة + تاريخ التوليد + اسم المستخدم).
  * تحتوي الجدول بكامل صفوفه (لا Pagination).
  * تحتوي تذييلًا (رقم الصفحة عبر CSS counter).
  * تستورد dynamic-reports-print.css.
  * تتعامل مع Ctrl+P بشكل مُهنّي (thead repeats, page-break-inside avoid).

Acceptance قبل Commit:
  [ ] Build بدون أخطاء.
  [ ] /Reports/Viewer/Print يعمل لـ Web و Pos و MainErp.
  [ ] الـ Canvas في Designer يعرض إطار صفحة مرئي.
  [ ] أثناء سحب حقل، drop zone واضحة.
  [ ] العنصر المحدَّد له إطار أزرق.
  [ ] تقارير قديمة تعمل بدون كسر.
  [ ] لا أخطاء Console جديدة.

اختبار يدوي إلزامي قبل Commit:
  1. افتح Viewer، شغّل تقرير seed.
  2. عدّل ترتيب الأعمدة، احفظ Layout «اختبار_P1».
  3. أعد التحميل، اختر «اختبار_P1»، شغّل.
  4. اضغط «معاينة طباعة» → تبويب جديد بنفس الترتيب.
  5. Ctrl+P → معاينة مهنية، رأس الجدول يتكرر بين الصفحات.
  6. كرّر السيناريو في Pos و MainErp.

مخرجات Phase 1:
  - Commit واحد بالعنوان:
      feat(reports): Phase 1 — Print preview + canvas UX clarity
  - تحديث Docs/Dynamic_Report_Designer_Implementation_Report.md
    بقسم «Phase 1» يشرح ما تم وروابط الملفات.
  - Push إلى claude/improve-report-designer-iAq77

ممنوع في Phase 1:
  - أي تعديل خارج الملفات المذكورة.
  - أي تغيير في قاعدة البيانات.
  - إضافة أي مكتبة NuGet أو Front-end.
  - أي تنفيذ من Phases 2..9.
  - أي إعادة كتابة لملفات موجودة (Edit only).

عند الانتهاء، اطبع باختصار:
  - قائمة الملفات المعدَّلة/المضافة.
  - نتائج الاختبار اليدوي (Pass/Fail لكل بند).
  - أي انحراف عن الخطة + سببه.
```

---

**نهاية الخطة.**
