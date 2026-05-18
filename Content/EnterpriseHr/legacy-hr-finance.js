(function () {
    "use strict";
    var root = document.querySelector("[data-lhf-page]");
    if (!root) { return; }

    function byId(id) { return document.getElementById(id); }
    function token() {
        var input = root.querySelector("input[name='__RequestVerificationToken']");
        return input ? input.value : "";
    }
    function val(id, value) {
        var el = byId(id);
        if (!el) { return ""; }
        if (arguments.length > 1) {
            if (el.type === "checkbox") { el.checked = !!value; } else { el.value = value == null ? "" : value; }
        }
        return el.type === "checkbox" ? el.checked : el.value;
    }
    function money(value) {
        var number = Number(value || 0);
        return number.toLocaleString("ar-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    function text(value) {
        return String(value == null ? "" : value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }
    function days(value) {
        var number = Number(value || 0);
        return number.toLocaleString("ar-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " يوم";
    }
    function today() {
        var d = new Date();
        return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
    }
    function setButtonBusy(button, isBusy, text) {
        if (!button) { return; }
        if (isBusy) {
            if (button.getAttribute("data-busy") === "1") { return false; }
            button.setAttribute("data-busy", "1");
            button.setAttribute("data-original-text", button.textContent || "");
            button.disabled = true;
            button.classList.add("is-loading");
            if (text) { button.textContent = text; }
            return true;
        }
        button.removeAttribute("data-busy");
        button.disabled = false;
        button.classList.remove("is-loading");
        if (button.getAttribute("data-original-text")) {
            button.textContent = button.getAttribute("data-original-text");
            button.removeAttribute("data-original-text");
        }
        return true;
    }
    function postForm(url, body) {
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
            body: new URLSearchParams(body).toString()
        }).then(function (r) {
            return r.json().then(function (j) { j._ok = r.ok; return j; });
        });
    }
    function success(res) { return !!(res && (res.Success || res.success)); }
    function message(res, fallback) { return (res && (res.Message || res.message)) || fallback; }
    function normalize(value) {
        return String(value == null ? "" : value).toLowerCase()
            .replace(/[أإآ]/g, "ا")
            .replace(/ى/g, "ي")
            .replace(/ة/g, "ه");
    }
    function filterSelect(selectId, term) {
        var select = byId(selectId);
        if (!select) { return; }
        var needle = normalize(term);
        Array.prototype.forEach.call(select.options, function (option, index) {
            if (index === 0 || !needle) {
                option.hidden = false;
                return;
            }
            option.hidden = normalize(option.textContent || option.innerText).indexOf(needle) === -1;
        });
    }
    function selectedOption(selectId) {
        var select = byId(selectId);
        return select && select.selectedIndex >= 0 ? select.options[select.selectedIndex] : null;
    }
    function debounce(fn, delay) {
        var timer = null;
        return function () {
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(null, args); }, delay || 180);
        };
    }
    function setPageLoading(isLoading) {
        root.classList.toggle("is-loading", !!isLoading);
    }

    function show(data) {
        data = data || {};
        val("lhfComponentId", data.Id);
        val("lhfName", data.Name);
        val("lhfNameEnglish", data.NameEnglish);
        val("lhfAccountCode", data.AccountCode);
        val("lhfAccountCode1", data.AccountCode1);
        val("lhfUnit", data.Unit);
        val("lhfAllowIntroduction", data.AllowIntroduction);
        val("lhfAddOrDiscount", data.AddOrDiscount);
        val("lhfFixedOrChanged", data.FixedOrChanged);
        val("lhfViewComponent", data.ViewComponent);
        val("lhfSalary", data.Salary);
        val("lhfAbsence", data.Absence);
        val("lhfLate", data.Late);
        val("lhfOvertime", data.Overtime);
        val("lhfInsurance", data.Insurance);
        val("lhfReward", data.Reward);
        byId("lhfEditorMessage").textContent = "";
        byId("lhfComponentEditor").hidden = false;
    }
    function closeComponent() { byId("lhfComponentEditor").hidden = true; }
    function collect() {
        return {
            Id: val("lhfComponentId"),
            Name: val("lhfName"),
            NameEnglish: val("lhfNameEnglish"),
            AccountCode: val("lhfAccountCode"),
            AccountCode1: val("lhfAccountCode1"),
            Unit: val("lhfUnit"),
            AllowIntroduction: val("lhfAllowIntroduction"),
            AddOrDiscount: val("lhfAddOrDiscount"),
            FixedOrChanged: val("lhfFixedOrChanged"),
            ViewComponent: val("lhfViewComponent"),
            Salary: val("lhfSalary"),
            Absence: val("lhfAbsence"),
            Late: val("lhfLate"),
            Overtime: val("lhfOvertime"),
            Insurance: val("lhfInsurance"),
            Reward: val("lhfReward"),
            __RequestVerificationToken: token()
        };
    }

    function setAdvanceMessage(text, isError) {
        var el = byId("lhfAdvanceMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.className = isError ? "lhf-message-error" : "";
    }
    function advanceDueDate(year, month, index) {
        var d = new Date(Number(year || new Date().getFullYear()), Number(month || 1) - 1 + index, 1);
        return d;
    }
    function formatDueDate(date) {
        return String(date.getMonth() + 1).padStart(2, "0") + "/" + date.getFullYear();
    }
    function renderAdvanceParts(parts) {
        var grid = byId("lhfAdvancePartsGrid");
        if (!grid) { return; }
        grid.innerHTML = "";
        if (!parts || !parts.length) {
            var amount = parseFloat(val("lhfAdvanceValue") || "0");
            var count = parseInt(val("lhfPaymentCounts") || "0", 10);
            var firstMonth = parseInt(val("lhfFirstMonthPayment") || "0", 10);
            var firstYear = parseInt(val("lhfFirstYearPayment") || "0", 10);
            if (!(amount > 0 && count > 0 && firstMonth > 0 && firstYear > 0)) {
                grid.innerHTML = "<tr><td colspan='4'>سيظهر جدول الأقساط بعد إدخال القيمة وعدد الأقساط.</td></tr>";
                return;
            }
            var partValue = amount / count;
            parts = [];
            for (var i = 0; i < count; i += 1) {
                parts.push({ PartNo: i + 1, PartValue: partValue, PartDate: formatDueDate(advanceDueDate(firstYear, firstMonth, i)), Payed: false });
            }
        }
        var now = new Date();
        var monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
        parts.forEach(function (part) {
            var due = part.PartDate || "-";
            var dueDate = null;
            var match = String(due).match(/(\d{1,2})[\/\-](\d{4})/);
            if (match) { dueDate = new Date(Number(match[2]), Number(match[1]) - 1, 1); }
            var overdue = !part.Payed && dueDate && dueDate < monthStart;
            var tr = document.createElement("tr");
            if (overdue) { tr.className = "is-overdue"; }
            tr.innerHTML = "<td>" + (part.PartNo || "-") + "</td>" +
                "<td>" + due + "</td>" +
                "<td class='lhf-num'>" + money(part.PartValue) + "</td>" +
                "<td>" + (part.Payed ? "<span class='lhf-badge success'>مسدد</span>" : overdue ? "<span class='lhf-badge danger'>متأخر</span>" : "<span class='lhf-badge info'>مفتوح</span>") + "</td>";
            grid.appendChild(tr);
        });
    }
    function renderAdvanceParts(parts) {
        var grid = byId("lhfAdvancePartsGrid");
        if (!grid) { return; }
        grid.innerHTML = "";
        if (!parts || !parts.length) {
            var amount = parseFloat(val("lhfAdvanceValue") || "0");
            var count = parseInt(val("lhfPaymentCounts") || "0", 10);
            var firstMonth = parseInt(val("lhfFirstMonthPayment") || "0", 10);
            var firstYear = parseInt(val("lhfFirstYearPayment") || "0", 10);
            if (!(amount > 0 && count > 0 && firstMonth > 0 && firstYear > 0)) {
                grid.innerHTML = "<tr><td colspan='7'>سيظهر جدول الأقساط بعد إدخال القيمة وعدد الأقساط.</td></tr>";
                return;
            }
            var partValue = amount / count;
            parts = [];
            for (var i = 0; i < count; i += 1) {
                parts.push({ PartNo: i + 1, PartValue: partValue, PartDate: formatDueDate(advanceDueDate(firstYear, firstMonth, i)), Payed: false, RemainingValue: partValue });
            }
        }

        var now = new Date();
        var monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
        var total = 0;
        var remaining = 0;
        var duplicateMap = {};
        parts.forEach(function (part) {
            var due = part.PartDate || "-";
            var dueDate = null;
            var match = String(due).match(/(\d{1,2})[\/\-](\d{4})/);
            if (match) { dueDate = new Date(Number(match[2]), Number(match[1]) - 1, 1); }
            var isPaid = !!(part.Payed || part.PayrollPosted);
            var payrollLinked = !!(part.PayrollLinked || part.PayrollRunId);
            var overdue = !isPaid && dueDate && dueDate < monthStart;
            var partValue = Number(part.PartValue || 0);
            total += partValue;
            remaining += isPaid ? 0 : Number(part.RemainingValue == null ? partValue : part.RemainingValue);
            duplicateMap[part.PartNo] = (duplicateMap[part.PartNo] || 0) + 1;

            var tr = document.createElement("tr");
            if (overdue) { tr.className = "is-overdue"; }
            if (duplicateMap[part.PartNo] > 1) { tr.className = (tr.className ? tr.className + " " : "") + "lhf-row-error"; }
            var status = part.PayrollPosted ? "<span class='lhf-badge success'>مرحل بالمسير</span>"
                : payrollLinked ? "<span class='lhf-badge warning'>مرتبط بمسير</span>"
                    : isPaid ? "<span class='lhf-badge success'>مسدد</span>"
                        : overdue ? "<span class='lhf-badge danger'>متأخر</span>"
                            : "<span class='lhf-badge info'>مفتوح</span>";
            tr.innerHTML = "<td>" + (part.PartNo || "-") + "</td>" +
                "<td>" + due + "</td>" +
                "<td class='lhf-num'>" + money(part.PartValue) + "</td>" +
                "<td class='lhf-num'>" + money(part.RemainingValue == null ? (isPaid ? 0 : partValue) : part.RemainingValue) + "</td>" +
                "<td>" + status + "</td>" +
                "<td>" + (part.PayrollRunId ? ("مسير #" + text(part.PayrollRunId)) : "-") + "</td>" +
                "<td>" + (part.PayrollPostedAt || part.PaidDate || "-") + "</td>";
            grid.appendChild(tr);
        });

        var duplicate = Object.keys(duplicateMap).some(function (key) { return duplicateMap[key] > 1; });
        var footer = document.createElement("tr");
        footer.className = duplicate ? "lhf-row-error lhf-total-row" : "lhf-total-row";
        footer.innerHTML = "<td colspan='2'><strong>" + (duplicate ? "يوجد تكرار في أرقام الأقساط" : "إجمالي الأقساط") + "</strong></td>" +
            "<td class='lhf-num'><strong>" + money(total) + "</strong></td>" +
            "<td class='lhf-num'><strong>" + money(Math.max(0, remaining)) + "</strong></td>" +
            "<td colspan='3'></td>";
        grid.appendChild(footer);
    }

    function setAdvanceInstallmentValidation(data) {
        var el = byId("lhfAdvanceInstallmentValidation");
        if (!el) { return; }
        if (!data || !data.Id) {
            el.textContent = "سيتم التحقق من إجمالي الأقساط وحالتها عند تحميل الطلب.";
            el.className = "lhf-installment-validation";
            return;
        }
        var valid = data.InstallmentsValid !== false;
        el.textContent = data.InstallmentValidationMessage || (valid ? "الأقساط متوازنة مع قيمة السلفة." : "يوجد خطأ في بيانات الأقساط.");
        el.className = "lhf-installment-validation " + (valid ? "success" : "danger");
    }

    function updateAdvancePreview(parts) {
        var employee = byId("lhfAdvanceEmployeeId");
        var amount = parseFloat(val("lhfAdvanceValue") || "0");
        var count = parseInt(val("lhfPaymentCounts") || "0", 10);
        var selected = employee && employee.options[employee.selectedIndex];
        var meta = byId("lhfAdvanceEmployeeMeta");
        var preview = byId("lhfAdvancePartsPreview");
        var financials = byId("lhfAdvanceFinancialSummary");
        if (meta && selected && selected.value) {
            meta.textContent = "الفرع: " + (selected.getAttribute("data-branch") || "-") + " | الإدارة: " + (selected.getAttribute("data-department") || "-") + " | الراتب الأساسي: " + money(selected.getAttribute("data-salary") || 0);
        } else if (meta) {
            meta.textContent = "اختر موظفاً لعرض الفرع والإدارة والراتب الأساسي.";
        }
        if (preview) {
            preview.textContent = amount > 0 && count > 0 ? ("قيمة القسط التقريبية: " + money(amount / count)) : "سيتم حساب الأقساط عند الحفظ.";
        }
        if (financials) {
            var salary = selected && selected.value ? Number(selected.getAttribute("data-salary") || 0) : 0;
            var part = amount > 0 && count > 0 ? amount / count : 0;
            var ratio = salary > 0 && part > 0 ? ((part / salary) * 100).toFixed(1) + "%" : "-";
            financials.innerHTML = "<span>الراتب الأساسي: <strong>" + money(salary) + "</strong></span>" +
                "<span>قيمة السلفة: <strong>" + money(amount) + "</strong></span>" +
                "<span>القسط الشهري: <strong>" + money(part) + "</strong></span>" +
                "<span>نسبة القسط من الراتب: <strong>" + ratio + "</strong></span>";
        }
        renderAdvanceParts(parts);
    }
    function setAdvanceLocked(isLocked, reason) {
        var form = byId("lhfAdvanceForm");
        var banner = byId("lhfAdvanceLockBanner");
        if (!form) { return; }
        form.querySelectorAll("input, select, textarea").forEach(function (el) {
            if (el.id !== "lhfAdvanceId") { el.disabled = !!isLocked; }
        });
        var submit = form.querySelector("button[type='submit']");
        if (submit) { submit.disabled = !!isLocked; }
        if (banner) {
            banner.hidden = !isLocked;
            banner.textContent = isLocked ? (reason || "هذا الطلب مقفل ولا يمكن تعديله من هذه الشاشة.") : "";
        }
    }
    function setAdvanceBoundary(boundary) {
        var el = byId("lhfAdvanceAccountingBoundary");
        if (!el) { return; }
        if (!boundary) {
            el.className = "lhf-accounting-boundary";
            el.innerHTML = "<strong>الحد المحاسبي للسلفة</strong><span>سيتم عرض مصدر الأثر المحاسبي بعد حفظ أو تحميل طلب السلفة.</span>";
            return;
        }

        var level = boundary.HasUnsupportedAccountingTrace ? " danger" : (boundary.HasAnyAccountingTrace ? " warning" : " safe");
        el.className = "lhf-accounting-boundary" + level;
        el.innerHTML = "<strong>الحد المحاسبي للسلفة</strong>" +
            "<span>" + (boundary.BoundaryMessage || "لا توجد بيانات حدود محاسبية.") + "</span>" +
            "<div><span>السلفة الفعلية: <strong>" + (boundary.ActualAdvanceId || "-") + "</strong></span>" +
            "<span>قيود مباشرة: <strong>" + (boundary.NormalJournalLineCount || 0) + "</strong></span>" +
            "<span>قيود افتتاحية: <strong>" + (boundary.OpeningJournalLineCount || 0) + "</strong></span>" +
            "<span>أقساط مرتبطة بالمسير: <strong>" + (boundary.PayrollDeductionLineCount || 0) + "</strong></span></div>";
    }
    function setAdvanceBoundary(boundary) {
        var el = byId("lhfAdvanceAccountingBoundary");
        if (!el) { return; }
        if (!boundary) {
            el.className = "lhf-accounting-boundary";
            el.innerHTML = "<strong>حدود المحاسبة والسداد للسلفة</strong><span>سيتم عرض الأثر المحاسبي وروابط المسير بعد حفظ أو تحميل طلب السلفة.</span>";
            return;
        }

        var level = boundary.HasUnsupportedAccountingTrace ? " danger" : (boundary.HasAnyAccountingTrace ? " warning" : " safe");
        var payrollLinks = boundary.PayrollDeductionLinks || [];
        var directLines = boundary.DirectJournalTraces || [];
        var openingLines = boundary.OpeningBalanceTraces || [];
        function traceLine(line) {
            return "<li><strong>" + text(line.AccountSerial || "-") + " - " + text(line.AccountName || "حساب غير معروف") + "</strong>" +
                "<span>" + text(line.Description || "") + "</span>" +
                "<b>مدين " + money(line.Debit) + " / دائن " + money(line.Credit) + "</b>" +
                (line.NoteId ? "<small>قيد رقم " + text(line.NoteId) + "</small>" : "") + "</li>";
        }
        function payrollLine(line) {
            var period = line.PeriodMonth && line.PeriodYear ? (line.PeriodMonth + "/" + line.PeriodYear) : "-";
            return "<li><strong>مسير #" + text(line.PayrollRunId) + " - " + text(line.RunName || period) + "</strong>" +
                "<span>القسط " + text(line.PartNo || "-") + " | الاستحقاق " + text(line.PartDate || "-") + " | القيمة " + money(line.PartValue) + "</span>" +
                "<b class='" + (line.IsPosted ? "is-posted" : "is-draft") + "'>" + text(line.StatusText || (line.IsPosted ? "مرحل" : "غير مرحل")) + "</b>" +
                (line.NoteId ? "<small>قيد المسير رقم " + text(line.NoteId) + "</small>" : "") +
                (line.PostedAt ? "<small>تاريخ الترحيل " + text(line.PostedAt) + "</small>" : "") + "</li>";
        }
        el.className = "lhf-accounting-boundary" + level;
        el.innerHTML = "<strong>حدود المحاسبة والسداد للسلفة</strong>" +
            "<span>" + text(boundary.BoundaryMessage || "لا توجد بيانات أثر محاسبي مؤكدة.") + "</span>" +
            (boundary.HasUnsupportedAccountingTrace ? "<em>تحذير: يوجد أثر محاسبي تاريخي مباشر غير مدعوم للإنشاء من شاشة السلف. لا يتم إنشاء قيود مباشرة من هنا.</em>" : "") +
            "<div class='lhf-boundary-metrics'><span>السلفة الفعلية: <strong>" + text(boundary.ActualAdvanceId || "-") + "</strong></span>" +
            "<span>أقساط مرتبطة بالمسير: <strong>" + text(boundary.PayrollDeductionLineCount || 0) + "</strong></span>" +
            "<span>أقساط مرحلة: <strong>" + text(boundary.PostedPayrollDeductionLineCount || 0) + "</strong></span>" +
            "<span>إجمالي خصم المسير: <strong>" + money(boundary.PayrollDeductionTotal) + "</strong></span>" +
            "<span>قيود مباشرة تاريخية: <strong>" + text(boundary.NormalJournalLineCount || 0) + "</strong></span>" +
            "<span>قيود افتتاحية: <strong>" + text(boundary.OpeningJournalLineCount || 0) + "</strong></span></div>" +
            (payrollLinks.length ? "<section><h4>روابط خصم مسير الرواتب</h4><ul>" + payrollLinks.map(payrollLine).join("") + "</ul></section>" : "<section><h4>روابط خصم مسير الرواتب</h4><p>لا توجد أقساط مرتبطة بمسير رواتب حتى الآن.</p></section>") +
            (directLines.length ? "<section><h4>قيود مباشرة تاريخية</h4><ul>" + directLines.map(traceLine).join("") + "</ul></section>" : "") +
            (openingLines.length ? "<section><h4>قيود افتتاحية</h4><ul>" + openingLines.map(traceLine).join("") + "</ul></section>" : "") +
            "<footer>شاشة السلف تعرض الأثر فقط ولا تنشئ سند صرف أو قيد يومية مباشر.</footer>";
    }

    function loadAdvanceBoundary(id) {
        var url = root.getAttribute("data-advance-accounting-boundary-url");
        if (!url || !id) { setAdvanceBoundary(null); return; }
        fetch(url + "?id=" + encodeURIComponent(id), { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (res && res.success) { setAdvanceBoundary(res.data); }
            }).catch(function () {
                setAdvanceBoundary({ BoundaryMessage: "تعذر تحميل الحدود المحاسبية الآن.", HasUnsupportedAccountingTrace: true });
            });
    }
    function showAdvance(data) {
        data = data || {};
        var now = new Date();
        val("lhfAdvanceId", data.Id || "");
        val("lhfAdvanceEmployeeId", data.EmployeeId || "");
        val("lhfAdvanceDate", (data.AdvanceDate || today()).replace(/\//g, "-"));
        val("lhfAdvanceValue", data.AdvanceValue || "");
        val("lhfPaymentCounts", data.PaymentCounts || 1);
        val("lhfFirstMonthPayment", data.FirstMonthPayment || (now.getMonth() + 1));
        val("lhfFirstYearPayment", data.FirstYearPayment || now.getFullYear());
        val("lhfAutoDiscount", data.AutoDiscount == null ? true : data.AutoDiscount);
        val("lhfAdvanceReason", data.Reason || "");
        val("lhfAdvanceEmployeeSearch", "");
        filterAdvanceEmployees("");
        setAdvanceMessage("");
        setAdvanceLocked(data.CanEdit === false, data.LockReason);
        setAdvanceInstallmentValidation(data);
        updateAdvancePreview(data.Parts || null);
        loadAdvanceBoundary(data.Id || "");
        byId("lhfAdvanceEditor").hidden = false;
    }
    function closeAdvance() { byId("lhfAdvanceEditor").hidden = true; }
    function collectAdvance() {
        return {
            Id: val("lhfAdvanceId"),
            EmployeeId: val("lhfAdvanceEmployeeId"),
            AdvanceDate: val("lhfAdvanceDate"),
            AdvanceValue: val("lhfAdvanceValue"),
            PaymentCounts: val("lhfPaymentCounts"),
            FirstMonthPayment: val("lhfFirstMonthPayment"),
            FirstYearPayment: val("lhfFirstYearPayment"),
            AutoDiscount: val("lhfAutoDiscount"),
            Reason: val("lhfAdvanceReason"),
            __RequestVerificationToken: token()
        };
    }

    function setChangedMessage(text, isError) {
        var el = byId("lhfChangedMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.className = isError ? "lhf-message-error" : "lhf-editor-message";
    }
    function setCurrentPeriod(prefix) {
        var d = new Date();
        val(prefix + "RecordDate", today());
        val(prefix + "Year", d.getFullYear());
        val(prefix + "Month", d.getMonth() + 1);
    }
    function showChangedComponent(data) {
        data = data || {};
        val("lhfChangedDetailId", data.Id || "");
        val("lhfChangedEmployeeSearch", "");
        val("lhfChangedComponentSearch", "");
        filterSelect("lhfChangedEmployeeId", "");
        filterSelect("lhfChangedComponentId", "");
        val("lhfChangedEmployeeId", data.EmployeeId || "");
        val("lhfChangedComponentId", data.ComponentId || "");
        if (data.RecordDate) { val("lhfChangedRecordDate", String(data.RecordDate).replace(/\//g, "-")); } else { setCurrentPeriod("lhfChanged"); }
        val("lhfChangedYear", data.Year || byId("lhfChangedYear").value);
        val("lhfChangedMonth", data.Month || byId("lhfChangedMonth").value);
        val("lhfChangedValue", data.Value || "");
        val("lhfChangedDays", data.NoOfDays || "");
        val("lhfChangedHours", data.NoOfHours || "");
        val("lhfChangedMinutes", data.NoOfMinutes || "");
        val("lhfChangedHourRate", data.HourRate || "");
        val("lhfChangedSalary", data.Salary || "");
        val("lhfChangedRemarks", data.Remarks || "");
        setChangedMessage("");
        updateChangedEntryPreview();
        var editor = byId("lhfChangedComponentEditor");
        if (editor) {
            editor.hidden = false;
            setTimeout(function () {
                var first = byId("lhfChangedEmployeeSearch") || byId("lhfChangedEmployeeId");
                if (first) { first.focus(); }
            }, 30);
        }
    }
    function closeChangedComponent() {
        var editor = byId("lhfChangedComponentEditor");
        if (editor) { editor.hidden = true; }
    }
    function setChangedBulkMessage(text, isError) {
        var el = byId("lhfChangedBulkMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.className = isError ? "lhf-message-error" : "lhf-editor-message";
    }
    function setChangedBulkMode(mode) {
        mode = mode === "copy" ? "copy" : "bulk";
        val("lhfChangedBulkMode", mode);
        var rootEditor = byId("lhfChangedBulkEditor");
        if (rootEditor) {
            rootEditor.classList.toggle("is-copy-mode", mode === "copy");
            rootEditor.classList.toggle("is-bulk-mode", mode !== "copy");
        }
        var title = byId("lhfChangedBulkTitle");
        if (title) { title.textContent = mode === "copy" ? "نسخ مفردات شهر سابق" : "إدخال جماعي للمفردات المتغيرة"; }
        var component = byId("lhfChangedBulkComponentId");
        if (component) { component.required = mode !== "copy"; }
        var value = byId("lhfChangedBulkValue");
        if (value) { value.required = mode !== "copy"; }
    }
    function openChangedBulk(mode) {
        var now = new Date();
        setChangedBulkMode(mode);
        val("lhfChangedBulkEmployees", "");
        val("lhfChangedBulkComponentId", "");
        val("lhfChangedBulkSourceComponentId", "");
        val("lhfChangedBulkRecordDate", today());
        val("lhfChangedBulkYear", now.getFullYear());
        val("lhfChangedBulkMonth", now.getMonth() + 1);
        val("lhfChangedBulkSourceYear", now.getMonth() === 0 ? now.getFullYear() - 1 : now.getFullYear());
        val("lhfChangedBulkSourceMonth", now.getMonth() === 0 ? 12 : now.getMonth());
        val("lhfChangedBulkValue", "");
        val("lhfChangedBulkDays", "");
        val("lhfChangedBulkHours", "");
        val("lhfChangedBulkMinutes", "");
        val("lhfChangedBulkHourRate", "");
        val("lhfChangedBulkSalary", "");
        val("lhfChangedBulkRemarks", "");
        renderChangedBulkPreview(null);
        setChangedBulkMessage("");
        var editor = byId("lhfChangedBulkEditor");
        if (editor) { editor.hidden = false; }
    }
    function closeChangedBulk() {
        var editor = byId("lhfChangedBulkEditor");
        if (editor) { editor.hidden = true; }
    }
    function collectChangedBulk() {
        return {
            Mode: val("lhfChangedBulkMode") || "bulk",
            EmployeeTokens: val("lhfChangedBulkEmployees"),
            ComponentId: val("lhfChangedBulkComponentId"),
            SourceComponentId: val("lhfChangedBulkSourceComponentId"),
            SourceYear: val("lhfChangedBulkSourceYear"),
            SourceMonth: val("lhfChangedBulkSourceMonth"),
            RecordDate: val("lhfChangedBulkRecordDate"),
            Year: val("lhfChangedBulkYear"),
            Month: val("lhfChangedBulkMonth"),
            Value: val("lhfChangedBulkValue"),
            NoOfDays: val("lhfChangedBulkDays"),
            NoOfHours: val("lhfChangedBulkHours"),
            NoOfMinutes: val("lhfChangedBulkMinutes"),
            HourRate: val("lhfChangedBulkHourRate"),
            Salary: val("lhfChangedBulkSalary"),
            Remarks: val("lhfChangedBulkRemarks"),
            __RequestVerificationToken: token()
        };
    }
    function validateChangedBulkClient() {
        var mode = val("lhfChangedBulkMode") === "copy" ? "copy" : "bulk";
        var year = parseInt(val("lhfChangedBulkYear") || "0", 10);
        var month = parseInt(val("lhfChangedBulkMonth") || "0", 10);
        var amount = Number(val("lhfChangedBulkValue") || 0);
        if (year < 2006 || year > 3000) { return "سنة الهدف غير صحيحة."; }
        if (month < 1 || month > 12) { return "شهر الهدف غير صحيح."; }
        if (mode === "bulk") {
            if (!val("lhfChangedBulkEmployees")) { return "اكتب موظفاً واحداً على الأقل."; }
            if (!val("lhfChangedBulkComponentId")) { return "اختر المفردة."; }
            if (!(amount > 0)) { return "أدخل قيمة أكبر من صفر."; }
        } else {
            var sy = parseInt(val("lhfChangedBulkSourceYear") || "0", 10);
            var sm = parseInt(val("lhfChangedBulkSourceMonth") || "0", 10);
            if (sy < 2006 || sy > 3000 || sm < 1 || sm > 12) { return "فترة المصدر غير صحيحة."; }
            if (sy === year && sm === month) { return "لا يمكن نسخ الشهر إلى نفس الفترة."; }
        }
        return "";
    }
    function renderChangedBulkPreview(data) {
        var body = byId("lhfChangedBulkPreviewRows");
        var summary = byId("lhfChangedBulkSummary");
        var save = document.querySelector("[data-save-changed-bulk]");
        var wrap = document.querySelector(".lhf-bulk-preview-wrap");
        if (!body || !summary) { return; }
        body.innerHTML = "";
        if (!data || !data.Rows || !data.Rows.length) {
            summary.textContent = "المعاينة مطلوبة قبل الحفظ.";
            if (wrap) { wrap.hidden = true; }
            if (save) { save.disabled = true; }
            return;
        }
        summary.textContent = "إجمالي السطور: " + data.TotalRows + "، الصالح: " + data.ValidRows + "، المرفوض: " + data.InvalidRows + "، الإجمالي: " + Number(data.TotalValue || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        data.Rows.forEach(function (row) {
            var tr = document.createElement("tr");
            tr.className = row.IsValid ? "" : "lhf-row-locked";
            tr.innerHTML = "<td>" + row.RowNo + "</td>"
                + "<td><strong>" + text(row.EmployeeName || "") + "</strong><small>" + text(row.EmployeeCode || row.EmployeeId || "") + "</small></td>"
                + "<td><strong>" + text(row.ComponentName || "") + "</strong><small>" + text(row.ComponentType || "") + "</small></td>"
                + "<td>" + text(row.Month || "") + " / " + text(row.Year || "") + "</td>"
                + "<td class=\"lhf-num\">" + Number(row.Value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + "</td>"
                + "<td><span class=\"lhf-badge " + (row.IsValid ? "success" : "danger") + "\">" + text(row.Status || "") + "</span></td>"
                + "<td>" + text(row.Message || "") + "</td>";
            body.appendChild(tr);
        });
        if (wrap) { wrap.hidden = false; }
        if (save) { save.disabled = !(data.ValidRows > 0 && data.InvalidRows === 0); }
    }
    function collectChangedComponent() {
        return {
            Id: val("lhfChangedDetailId"),
            EmployeeId: val("lhfChangedEmployeeId"),
            ComponentId: val("lhfChangedComponentId"),
            RecordDate: val("lhfChangedRecordDate"),
            Year: val("lhfChangedYear"),
            Month: val("lhfChangedMonth"),
            Value: val("lhfChangedValue"),
            NoOfDays: val("lhfChangedDays"),
            NoOfHours: val("lhfChangedHours"),
            NoOfMinutes: val("lhfChangedMinutes"),
            HourRate: val("lhfChangedHourRate"),
            Salary: val("lhfChangedSalary"),
            Remarks: val("lhfChangedRemarks"),
            __RequestVerificationToken: token()
        };
    }
    function updateChangedEntryPreview() {
        var target = byId("lhfChangedEntryPreview");
        if (!target) { return; }
        var employee = selectedOption("lhfChangedEmployeeId");
        var component = selectedOption("lhfChangedComponentId");
        var amount = Number(val("lhfChangedValue") || 0);
        var salary = Number((employee && employee.getAttribute("data-salary")) || val("lhfChangedSalary") || 0);
        if (employee && employee.value && !val("lhfChangedSalary") && salary > 0) {
            val("lhfChangedSalary", salary);
        }
        if (!employee || !employee.value || !component || !component.value) {
            target.textContent = "اختر الموظف والمفردة لمراجعة اتجاه التأثير قبل الحفظ.";
            target.className = "lhf-changed-entry-preview";
            return;
        }
        var isAddition = component.getAttribute("data-add") === "true";
        var unit = parseInt(component.getAttribute("data-unit") || "0", 10);
        var unitText = unit === 1 ? "أيام" : unit === 2 ? "ساعات" : "قيمة مباشرة";
        target.innerHTML = "<strong>" + text(isAddition ? "إضافة" : "خصم") + "</strong>"
            + "<span>" + text(component.textContent || "") + "</span>"
            + "<small>" + text(employee.textContent || "") + " - " + unitText + " - " + money(amount) + "</small>";
        target.className = "lhf-changed-entry-preview " + (isAddition ? "addition" : "deduction");
    }
    function loadChangedEmployees(term) {
        var select = byId("lhfChangedEmployeeId");
        var url = root.getAttribute("data-employee-lookup-url");
        if (!select || !url || !term || term.trim().length < 2) {
            filterSelect("lhfChangedEmployeeId", term || "");
            return;
        }
        fetch(url + "?term=" + encodeURIComponent(term.trim()) + "&employeeStatus=active", { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                var rows = (res && (res.data || res.Data)) || [];
                if (!rows.length) { filterSelect("lhfChangedEmployeeId", term); return; }
                var current = select.value;
                select.innerHTML = '<option value="">اختر الموظف</option>';
                rows.forEach(function (employee) {
                    var option = document.createElement("option");
                    option.value = employee.Id || employee.id;
                    option.textContent = (employee.Code || employee.code || "") + " - " + (employee.Name || employee.name || "");
                    option.setAttribute("data-salary", employee.BasicSalary || employee.basicSalary || 0);
                    select.appendChild(option);
                });
                if (current) { select.value = current; }
                updateChangedEntryPreview();
            }).catch(function () {
                filterSelect("lhfChangedEmployeeId", term);
            });
    }
    function validateChangedComponentClient() {
        var employeeId = val("lhfChangedEmployeeId");
        var componentId = val("lhfChangedComponentId");
        var recordDate = val("lhfChangedRecordDate");
        var year = parseInt(val("lhfChangedYear") || "0", 10);
        var month = parseInt(val("lhfChangedMonth") || "0", 10);
        var amount = Number(val("lhfChangedValue") || 0);
        var daysValue = Number(val("lhfChangedDays") || 0);
        var hoursValue = Number(val("lhfChangedHours") || 0);
        var minutesValue = Number(val("lhfChangedMinutes") || 0);
        var rate = Number(val("lhfChangedHourRate") || 0);
        var salary = Number(val("lhfChangedSalary") || 0);
        var componentSelect = byId("lhfChangedComponentId");
        var selectedComponent = componentSelect && componentSelect.options[componentSelect.selectedIndex];
        var unit = selectedComponent ? parseInt(selectedComponent.getAttribute("data-unit") || "0", 10) : 0;

        if (!employeeId) { return "يجب اختيار الموظف."; }
        if (!componentId) { return "يجب اختيار المفردة."; }
        if (!recordDate || isNaN(Date.parse(recordDate))) { return "تاريخ التسجيل غير صحيح."; }
        if (year < 2006 || year > 3000) { return "سنة المسير غير صحيحة."; }
        if (month < 1 || month > 12) { return "شهر المسير غير صحيح."; }
        if (!(amount > 0)) { return "يجب إدخال قيمة أكبر من صفر."; }
        if (amount > 999999999) { return "قيمة المفردة كبيرة بشكل غير منطقي."; }
        if (daysValue < 0 || hoursValue < 0 || minutesValue < 0 || rate < 0 || salary < 0) { return "بيانات القياس لا تقبل قيماً سالبة."; }
        if (minutesValue >= 60) { return "الدقائق يجب أن تكون أقل من 60."; }
        if (unit === 1 && hoursValue > 0) { return "هذه المفردة محسوبة بالأيام. لا تسجل ساعات عليها."; }
        if (unit === 2 && daysValue > 0) { return "هذه المفردة محسوبة بالساعات. لا تسجل أيام عليها."; }
        if (unit !== 1 && unit !== 2 && (daysValue > 0 || hoursValue > 0 || minutesValue > 0 || rate > 0)) { return "هذه المفردة قيمتها مباشرة. لا تحتاج أيام أو ساعات أو معدل."; }
        return "";
    }
    function filterAdvanceEmployees(text) {
        var select = byId("lhfAdvanceEmployeeId");
        if (!select) { return; }
        var q = String(text || "").trim().toLowerCase();
        Array.prototype.forEach.call(select.options, function (option, index) {
            if (index === 0) { option.hidden = false; return; }
            option.hidden = q && option.text.toLowerCase().indexOf(q) === -1;
        });
    }
    function runAdvanceAction(button, options) {
        if (!setButtonBusy(button, true, options.busyText)) { return; }
        postForm(options.url, options.body).then(function (res) {
            if (!success(res)) {
                alert(message(res, options.errorText));
                setButtonBusy(button, false);
                return;
            }
            alert(message(res, options.successText));
            window.location.reload();
        }).catch(function () {
            alert("تعذر الاتصال بالخادم. برجاء المحاولة مرة أخرى.");
            setButtonBusy(button, false);
        });
    }
    function renderVacationBalance(data) {
        var result = byId("lhfVacationBalanceResult");
        if (!result) { return; }
        data = data || {};
        var errors = data.Errors || data.errors || [];
        var warnings = data.Warnings || data.warnings || [];
        var lines = data.Lines || data.lines || [];
        var canPost = data.CanPostPaidVacation !== false && data.canPostPaidVacation !== false;
        var statusClass = canPost ? "success" : "danger";
        var statusText = canPost ? "الرصيد يسمح بالإجازة المدفوعة" : "الرصيد لا يسمح بالإجازة المدفوعة";

        var html = "<div class='lhf-vac-balance-status " + statusClass + "'>" +
            "<strong>" + statusText + "</strong>" +
            "<span>" + (data.EmployeeName || data.employeeName || "الموظف المحدد") + "</span>" +
            "</div>";

        html += "<div class='lhf-vac-balance-grid'>" +
            "<article><span>الرصيد السنوي</span><strong>" + days(data.AnnualEntitlementDays || data.annualEntitlementDays) + "</strong></article>" +
            "<article><span>المستحق حتى التاريخ</span><strong>" + days(data.AccruedDays || data.accruedDays) + "</strong></article>" +
            "<article><span>الرصيد الافتتاحي</span><strong>" + days(data.OpeningBalanceDays || data.openingBalanceDays) + "</strong></article>" +
            "<article><span>المرحل من سنوات سابقة</span><strong>" + days(data.CarryOverDays || data.carryOverDays) + "</strong></article>" +
            "<article><span>المستهلك والمدفوع</span><strong>" + days(data.PaidVacationConsumedDays || data.paidVacationConsumedDays) + "</strong></article>" +
            "<article><span>طلبات معتمدة غير مسواة</span><strong>" + days(data.PendingApprovedDays || data.pendingApprovedDays) + "</strong></article>" +
            "<article><span>بدون راتب / غياب</span><strong>" + days(Number(data.UnpaidLeaveDays || data.unpaidLeaveDays || 0) + Number(data.AbsenceDeductionDays || data.absenceDeductionDays || 0)) + "</strong></article>" +
            "<article><span>المتاح قبل الطلب</span><strong>" + days(data.AvailableBeforeRequest || data.availableBeforeRequest) + "</strong></article>" +
            "<article class='lhf-vac-net'><span>المتاح بعد الطلب</span><strong>" + days(data.AvailableAfterRequest || data.availableAfterRequest) + "</strong></article>" +
            "</div>";

        if (errors.length) {
            html += "<div class='lhf-vac-errors'><strong>أخطاء تمنع الاعتماد</strong><ul>" +
                errors.map(function (item) { return "<li>" + item + "</li>"; }).join("") +
                "</ul></div>";
        }
        if (warnings.length) {
            html += "<div class='lhf-vac-warnings'><strong>تنبيهات</strong><ul>" +
                warnings.map(function (item) { return "<li>" + item + "</li>"; }).join("") +
                "</ul></div>";
        }
        if (lines.length) {
            html += "<div class='lhf-vac-line-list'><strong>تفاصيل الحساب</strong><table><thead><tr><th>المصدر</th><th>البيان</th><th>الأثر</th><th>الأيام</th></tr></thead><tbody>" +
                lines.map(function (line) {
                    return "<tr><td>" + (line.Source || line.source || "-") + "</td>" +
                        "<td>" + (line.Description || line.description || "-") + "</td>" +
                        "<td>" + (line.Effect || line.effect || "-") + "</td>" +
                        "<td class='lhf-num'>" + days(line.Days || line.days) + "</td></tr>";
                }).join("") +
                "</tbody></table></div>";
        }
        result.innerHTML = html;
    }
    function calculateVacationBalance(button) {
        var result = byId("lhfVacationBalanceResult");
        var url = root.getAttribute("data-vacation-balance-url");
        var employeeId = val("lhfVacationEmployeeId");
        if (!result || !url) { return; }
        if (!employeeId) {
            result.innerHTML = "<div class='lhf-vac-errors'>اختر الموظف أولا لحساب رصيد الإجازات.</div>";
            return;
        }
        if (!setButtonBusy(button, true, "جاري الحساب...")) { return; }
        result.innerHTML = "<div class='lhf-vac-loading'>جاري حساب الرصيد من بيانات الإجازات الفعلية...</div>";
        var params = new URLSearchParams();
        params.set("employeeId", employeeId);
        if (val("lhfVacationAsOfDate")) { params.set("asOfDate", val("lhfVacationAsOfDate")); }
        if (val("lhfVacationStartDate")) { params.set("vacationStartDate", val("lhfVacationStartDate")); }
        if (val("lhfVacationEndDate")) { params.set("vacationEndDate", val("lhfVacationEndDate")); }
        var requestedDays = dateDiffDays(val("lhfVacationStartDate"), val("lhfVacationEndDate"));
        if (requestedDays > 0) { params.set("requestedDays", requestedDays); }
        fetch(url + "?" + params.toString(), { credentials: "same-origin" })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                renderVacationBalance(res);
                setButtonBusy(button, false);
            }).catch(function () {
                result.innerHTML = "<div class='lhf-vac-errors'>تعذر حساب رصيد الإجازات الآن. راجع الاتصال أو صلاحيات الشاشة.</div>";
                setButtonBusy(button, false);
            });
    }
    function setVacationMessage(text, isError) {
        var el = byId("lhfVacationMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.className = isError ? "lhf-message-error" : "";
    }
    function closeVacation() {
        var editor = byId("lhfVacationEditor");
        if (editor) { editor.hidden = true; }
    }
    function isoDate(value) {
        return String(value || "").replace(/\//g, "-").substring(0, 10);
    }
    function dateDiffDays(fromValue, toValue) {
        if (!fromValue || !toValue) { return 0; }
        var from = new Date(fromValue + "T00:00:00");
        var to = new Date(toValue + "T00:00:00");
        if (isNaN(from.getTime()) || isNaN(to.getTime()) || to < from) { return 0; }
        return Math.round((to - from) / 86400000) + 1;
    }
    function parseUiDate(value) {
        if (!value) { return null; }
        var normalized = String(value).replace(/\//g, "-").substring(0, 10);
        var d = new Date(normalized + "T00:00:00");
        return isNaN(d.getTime()) ? null : d;
    }
    function detectVacationConflict(employeeId, fromValue, toValue, currentId) {
        var from = parseUiDate(fromValue);
        var to = parseUiDate(toValue);
        if (!employeeId || !from || !to) { return []; }
        var hits = [];
        root.querySelectorAll(".lhf-vacation-table tbody tr[data-vacation-employee]").forEach(function (row) {
            if (String(row.getAttribute("data-vacation-id") || "") === String(currentId || "")) { return; }
            if (String(row.getAttribute("data-vacation-employee") || "") !== String(employeeId)) { return; }
            var rowFrom = parseUiDate(row.getAttribute("data-vacation-from"));
            var rowTo = parseUiDate(row.getAttribute("data-vacation-to"));
            if (rowFrom && rowTo && from <= rowTo && to >= rowFrom) {
                hits.push({ id: row.getAttribute("data-vacation-id"), from: row.getAttribute("data-vacation-from"), to: row.getAttribute("data-vacation-to") });
            }
        });
        return hits;
    }
    function updateVacationPreview() {
        var employee = byId("lhfVacationRequestEmployeeId");
        var meta = byId("lhfVacationEmployeeMeta");
        var summary = byId("lhfVacationDateSummary");
        var conflict = byId("lhfVacationConflict");
        var selected = employee && employee.options[employee.selectedIndex];
        if (meta) {
            meta.innerHTML = selected && selected.value
                ? "الفرع: <strong>" + text(selected.getAttribute("data-branch") || "-") + "</strong> | الإدارة: <strong>" + text(selected.getAttribute("data-department") || "-") + "</strong> | الراتب الأساسي: <strong>" + money(selected.getAttribute("data-salary") || 0) + "</strong>"
                : "اختر موظفًا لعرض الفرع والإدارة والراتب الأساسي.";
        }
        if (summary) {
            var from = val("lhfVacationRequestFromDate");
            var to = val("lhfVacationRequestToDate");
            var resume = val("lhfVacationRequestResumeWork");
            var count = dateDiffDays(from, to);
            summary.innerHTML = count > 0
                ? "الفترة المختارة: <strong>" + days(count) + "</strong>" + (resume ? " | عودة العمل: <strong>" + text(resume) + "</strong>" : "")
                : "حدد فترة الإجازة لحساب الأيام وموعد العودة.";
        }
        if (conflict) {
            var conflicts = detectVacationConflict(val("lhfVacationRequestEmployeeId"), val("lhfVacationRequestFromDate"), val("lhfVacationRequestToDate"), val("lhfVacationId"));
            conflict.hidden = conflicts.length === 0;
            conflict.innerHTML = conflicts.length
                ? "<strong>تنبيه تعارض محتمل</strong><span>يوجد طلب إجازة لنفس الموظف داخل نفس الفترة: " + conflicts.map(function (x) { return "#" + text(x.id) + " (" + text(x.from) + " - " + text(x.to) + ")"; }).join("، ") + "</span>"
                : "";
        }
    }
    function filterVacationEmployees(search) {
        var select = byId("lhfVacationRequestEmployeeId");
        if (!select) { return; }
        search = String(search || "").toLowerCase();
        Array.prototype.forEach.call(select.options, function (option) {
            if (!option.value) { option.hidden = false; return; }
            option.hidden = !!(search && option.text.toLowerCase().indexOf(search) === -1);
        });
    }
    function ensureVacationEditorPanels() {
        var balance = byId("lhfVacationRequestBalance");
        if (!balance) { return; }
        if (!byId("lhfVacationEmployeeMeta")) {
            var summary = document.createElement("div");
            summary.className = "lhf-vacation-request-summary lhf-span-3";
            summary.innerHTML = "<div id='lhfVacationEmployeeMeta'>اختر موظفًا لعرض الفرع والإدارة والراتب الأساسي.</div><div id='lhfVacationDateSummary'>حدد فترة الإجازة لحساب الأيام وموعد العودة.</div>";
            balance.parentNode.insertBefore(summary, balance);
        }
        if (!byId("lhfVacationTimeline")) {
            var timeline = document.createElement("div");
            timeline.className = "lhf-vacation-timeline lhf-span-3";
            timeline.id = "lhfVacationTimeline";
            timeline.innerHTML = "<strong>مسار الاعتماد</strong><ol><li class='done'>إرسال الطلب</li><li>اعتماد المدير</li><li>اعتماد الموارد البشرية</li></ol>";
            balance.parentNode.insertBefore(timeline, balance.nextSibling);
        }
    }
    function renderVacationTimeline(data) {
        var timeline = byId("lhfVacationTimeline");
        if (!timeline) { return; }
        data = data || {};
        var history = data.ApprovalHistory || data.approvalHistory || [];
        var managerDone = !!(data.ManagerApproved || data.managerApproved);
        var hrDone = !!(data.HrApproved || data.hrApproved);
        var rejected = !!(data.Rejected || data.rejected);
        var html = "<strong>مسار الاعتماد</strong><ol>" +
            "<li class='done'>إرسال الطلب</li>" +
            "<li class='" + (managerDone ? "done" : rejected ? "danger" : "") + "'>اعتماد المدير</li>" +
            "<li class='" + (hrDone ? "done" : rejected ? "danger" : "") + "'>اعتماد الموارد البشرية</li>" +
            "</ol>";
        if (history.length) {
            html += "<div class='lhf-vacation-history'>";
            history.forEach(function (item) {
                html += "<span><strong>" + text(item.FromUser || item.fromUser || item.EmployeeName || item.employeeName || "-") + "</strong>" +
                    "<small>" + text(item.ApprovedAt || item.approvedAt || item.CancelledAt || item.cancelledAt || "-") + "</small>" +
                    "<em>" + text(item.Remarks || item.remarks || "") + "</em></span>";
            });
            html += "</div>";
        }
        timeline.innerHTML = html;
    }
    function showVacation(data) {
        data = data || {};
        ensureVacationEditorPanels();
        val("lhfVacationId", data.Id || "");
        val("lhfVacationRequestEmployeeId", data.EmployeeId || "");
        val("lhfVacationRequestFromDate", isoDate(data.FromDate || today()));
        val("lhfVacationRequestToDate", isoDate(data.ToDate || today()));
        val("lhfVacationRequestResumeWork", isoDate(data.ResumeWork || ""));
        val("lhfVacationRequestType", data.VacationType || "إجازة سنوية");
        val("lhfVacationWithSalary", data.WithSalary == null ? true : data.WithSalary);
        val("lhfVacationWithoutSalary", data.WithoutSalary || false);
        val("lhfVacationReason", data.Reason || "");
        val("lhfVacationEmployeeSearch", "");
        filterVacationEmployees("");
        var banner = byId("lhfVacationLockBanner");
        if (banner) {
            banner.hidden = data.CanEdit !== false;
            banner.textContent = data.CanEdit === false ? (data.LockReason || "هذا الطلب مقفل ولا يمكن تعديله.") : "";
        }
        setVacationMessage("");
        var result = byId("lhfVacationRequestBalance");
        if (result) { result.innerHTML = "سيتم حساب الرصيد قبل الحفظ."; }
        updateVacationPreview();
        renderVacationTimeline(data);
        byId("lhfVacationEditor").hidden = false;
    }
    function collectVacation() {
        return {
            Id: val("lhfVacationId"),
            EmployeeId: val("lhfVacationRequestEmployeeId"),
            FromDate: val("lhfVacationRequestFromDate"),
            ToDate: val("lhfVacationRequestToDate"),
            ResumeWork: val("lhfVacationRequestResumeWork"),
            VacationType: val("lhfVacationRequestType"),
            WithSalary: val("lhfVacationWithSalary"),
            WithoutSalary: val("lhfVacationWithoutSalary"),
            Reason: val("lhfVacationReason"),
            __RequestVerificationToken: token()
        };
    }
    function runVacationAction(button, url, id, promptText, busyText, successText) {
        var remarks = promptText ? prompt(promptText) : "";
        if (remarks === null) { return; }
        if (!setButtonBusy(button, true, busyText)) { return; }
        postForm(url, { id: id, remarks: remarks, __RequestVerificationToken: token() }).then(function (res) {
            if (!success(res)) {
                alert(message(res, "تعذر تنفيذ الإجراء على طلب الإجازة."));
                setButtonBusy(button, false);
                return;
            }
            alert(message(res, successText));
            window.location.reload();
        }).catch(function () {
            alert("تعذر الاتصال بالخادم.");
            setButtonBusy(button, false);
        });
    }

    document.addEventListener("click", function (event) {
        if (event.target.closest("[data-close-component]")) { closeComponent(); return; }
        if (event.target.closest("[data-new-component]")) { show({ AddOrDiscount: true, ViewComponent: true, Salary: true }); return; }
        if (event.target.closest("[data-close-changed-component]")) { closeChangedComponent(); return; }
        if (event.target.closest("[data-new-changed-component]")) { showChangedComponent({}); return; }
        if (event.target.closest("[data-close-changed-bulk]")) { closeChangedBulk(); return; }
        var openChangedBulkButton = event.target.closest("[data-open-changed-bulk]");
        if (openChangedBulkButton) { openChangedBulk(openChangedBulkButton.getAttribute("data-open-changed-bulk")); return; }
        var previewChangedBulk = event.target.closest("[data-preview-changed-bulk]");
        if (previewChangedBulk) {
            var bulkError = validateChangedBulkClient();
            if (bulkError) { setChangedBulkMessage(bulkError, true); return; }
            if (!setButtonBusy(previewChangedBulk, true, "معاينة...")) { return; }
            setChangedBulkMessage("جاري التحقق من السطور...");
            postForm(root.getAttribute("data-preview-changed-component-bulk-url"), collectChangedBulk()).then(function (res) {
                setButtonBusy(previewChangedBulk, false);
                if (!success(res)) {
                    setChangedBulkMessage(message(res, "تعذر إنشاء المعاينة."), true);
                    renderChangedBulkPreview(null);
                    return;
                }
                renderChangedBulkPreview(res.data || res.Data);
                setChangedBulkMessage(message(res, "تمت المعاينة."));
            }).catch(function () {
                setButtonBusy(previewChangedBulk, false);
                setChangedBulkMessage("تعذر الاتصال بالخادم.", true);
                renderChangedBulkPreview(null);
            });
            return;
        }
        var saveChangedBulk = event.target.closest("[data-save-changed-bulk]");
        if (saveChangedBulk) {
            if (saveChangedBulk.disabled) { return; }
            if (!confirm("سيتم حفظ السطور الصالحة فقط بعد إعادة التحقق على الخادم. متابعة؟")) { return; }
            if (!setButtonBusy(saveChangedBulk, true, "حفظ...")) { return; }
            setChangedBulkMessage("جاري الحفظ...");
            postForm(root.getAttribute("data-save-changed-component-bulk-url"), collectChangedBulk()).then(function (res) {
                if (!success(res)) {
                    setChangedBulkMessage(message(res, "تعذر حفظ الدفعة."), true);
                    setButtonBusy(saveChangedBulk, false);
                    return;
                }
                window.location.reload();
            }).catch(function () {
                setChangedBulkMessage("تعذر الاتصال بالخادم.", true);
                setButtonBusy(saveChangedBulk, false);
            });
            return;
        }
        if (event.target.closest("[data-close-advance]")) { closeAdvance(); return; }
        if (event.target.closest("[data-new-advance]")) { showAdvance({ AutoDiscount: true, PaymentCounts: 1, CanEdit: true }); return; }
        if (event.target.closest("[data-close-vacation]")) { closeVacation(); return; }
        if (event.target.closest("[data-new-vacation]")) { showVacation({ WithSalary: true, WithoutSalary: false, CanEdit: true }); return; }
        var calcVacationBalance = event.target.closest("[data-calc-vacation-balance]");
        if (calcVacationBalance) { calculateVacationBalance(calcVacationBalance); return; }

        var editVacation = event.target.closest("[data-edit-vacation]");
        if (editVacation) {
            if (!setButtonBusy(editVacation, true, "تحميل...")) { return; }
            fetch(root.getAttribute("data-vacation-details-url") + "?id=" + encodeURIComponent(editVacation.getAttribute("data-edit-vacation")), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    setButtonBusy(editVacation, false);
                    if (res.success) { showVacation(res.data); } else { alert(res.message || "تعذر تحميل طلب الإجازة."); }
                }).catch(function () {
                    setButtonBusy(editVacation, false);
                    alert("تعذر الاتصال بالخادم.");
                });
            return;
        }
        var managerApproveVacation = event.target.closest("[data-manager-approve-vacation]");
        if (managerApproveVacation) {
            runVacationAction(managerApproveVacation, root.getAttribute("data-manager-approve-vacation-url"), managerApproveVacation.getAttribute("data-manager-approve-vacation"), "ملاحظات اعتماد المدير", "اعتماد...", "تم اعتماد طلب الإجازة من المدير.");
            return;
        }
        var hrApproveVacation = event.target.closest("[data-hr-approve-vacation]");
        if (hrApproveVacation) {
            runVacationAction(hrApproveVacation, root.getAttribute("data-hr-approve-vacation-url"), hrApproveVacation.getAttribute("data-hr-approve-vacation"), "ملاحظات اعتماد الموارد البشرية", "اعتماد...", "تم اعتماد طلب الإجازة من الموارد البشرية.");
            return;
        }
        var rejectVacation = event.target.closest("[data-reject-vacation]");
        if (rejectVacation) {
            runVacationAction(rejectVacation, root.getAttribute("data-reject-vacation-url"), rejectVacation.getAttribute("data-reject-vacation"), "سبب رفض طلب الإجازة", "رفض...", "تم رفض طلب الإجازة.");
            return;
        }
        var createVacationEntitlement = event.target.closest("[data-create-vacation-entitlement]");
        if (createVacationEntitlement) {
            if (!confirm("تأكيد إنشاء مستند مستحقات لهذا الطلب؟ لن يتم إنشاء قيود أو ترحيل رواتب من هذه الشاشة.")) { return; }
            runVacationAction(createVacationEntitlement, root.getAttribute("data-create-vacation-entitlement-url"), createVacationEntitlement.getAttribute("data-create-vacation-entitlement"), "", "إنشاء...", "تم إنشاء مستند مستحقات الإجازة.");
            return;
        }
        var deleteVacationEntitlement = event.target.closest("[data-delete-vacation-entitlement]");
        if (deleteVacationEntitlement) {
            var typed = prompt("لحذف مستند المستحقات اكتب حذف. لن يسمح الحذف إذا كان مرتبطا بدفع أو مسير.");
            if (typed !== "حذف") { return; }
            runVacationAction(deleteVacationEntitlement, root.getAttribute("data-delete-vacation-entitlement-url"), deleteVacationEntitlement.getAttribute("data-delete-vacation-entitlement"), "", "حذف...", "تم حذف مستند مستحقات الإجازة.");
            return;
        }
        var saveVacationReturn = event.target.closest("[data-save-vacation-return]");
        if (saveVacationReturn) {
            var entitlementId = saveVacationReturn.getAttribute("data-save-vacation-return");
            var startDate = saveVacationReturn.getAttribute("data-vacation-start") || "";
            var endDate = saveVacationReturn.getAttribute("data-vacation-end") || "";
            var actualReturnDate = prompt("تاريخ المباشرة الفعلي بصيغة yyyy-mm-dd", endDate);
            if (actualReturnDate === null) { return; }
            var actualDays = prompt("عدد أيام الإجازة الفعلية", "");
            if (actualDays === null) { return; }
            var delayDays = prompt("عدد أيام التأخير", "0");
            if (delayDays === null) { return; }
            var treatment = "none";
            if (Number(delayDays || 0) > 0) {
                treatment = prompt("طريقة معالجة التأخير: unpaid = بدون راتب، balance = خصم من الرصيد", "unpaid");
                if (treatment === null) { return; }
            }
            var remarks = prompt("ملاحظات المباشرة", "مباشرة عمل من الويب");
            if (remarks === null) { return; }
            if (!setButtonBusy(saveVacationReturn, true, "حفظ...")) { return; }
            postForm(root.getAttribute("data-save-vacation-return-url"), {
                EntitlementId: entitlementId,
                ActualReturnDate: actualReturnDate,
                ActualVacationDays: actualDays,
                DelayDays: delayDays,
                DelayTreatment: treatment,
                Remarks: remarks,
                __RequestVerificationToken: token()
            }).then(function (res) {
                if (!success(res)) {
                    alert(message(res, "تعذر تسجيل مباشرة العمل."));
                    setButtonBusy(saveVacationReturn, false);
                    return;
                }
                alert(message(res, "تم تسجيل مباشرة العمل."));
                window.location.reload();
            }).catch(function () {
                alert("تعذر الاتصال بالخادم.");
                setButtonBusy(saveVacationReturn, false);
            });
            return;
        }
        var deleteVacationReturn = event.target.closest("[data-delete-vacation-return]");
        if (deleteVacationReturn) {
            var confirmDeleteReturn = prompt("لحذف مباشرة العمل وعكس أثرها على مستحقات الإجازة اكتب حذف");
            if (confirmDeleteReturn !== "حذف") { return; }
            runVacationAction(deleteVacationReturn, root.getAttribute("data-delete-vacation-return-url"), deleteVacationReturn.getAttribute("data-delete-vacation-return"), "", "حذف...", "تم حذف مباشرة العمل.");
            return;
        }
        var cancelVacation = event.target.closest("[data-cancel-vacation]");
        if (cancelVacation) {
            runVacationAction(cancelVacation, root.getAttribute("data-cancel-vacation-url"), cancelVacation.getAttribute("data-cancel-vacation"), "سبب إلغاء طلب الإجازة", "إلغاء...", "تم إلغاء طلب الإجازة بأمان.");
            return;
        }

        var editAdvance = event.target.closest("[data-edit-advance]");
        if (editAdvance) {
            if (!setButtonBusy(editAdvance, true, "تحميل...")) { return; }
            fetch(root.getAttribute("data-advance-details-url") + "?id=" + encodeURIComponent(editAdvance.getAttribute("data-edit-advance")), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    setButtonBusy(editAdvance, false);
                    if (res.success) { showAdvance(res.data); } else { alert(res.message || "تعذر تحميل طلب السلفة"); }
                }).catch(function () {
                    setButtonBusy(editAdvance, false);
                    alert("تعذر الاتصال بالخادم.");
                });
            return;
        }

        var deleteAdvance = event.target.closest("[data-delete-advance]");
        if (deleteAdvance) {
            var typed = prompt("للحذف النهائي اكتب كلمة حذف. يفضل استخدام الإلغاء الآمن إذا كان الطلب دخل دورة اعتماد أو صرف.");
            if (typed !== "حذف") { return; }
            runAdvanceAction(deleteAdvance, {
                url: root.getAttribute("data-delete-advance-url"),
                body: { id: deleteAdvance.getAttribute("data-delete-advance"), __RequestVerificationToken: token() },
                busyText: "حذف...",
                errorText: "تعذر حذف طلب السلفة",
                successText: "تم حذف طلب السلفة"
            });
            return;
        }

        var disburseAdvance = event.target.closest("[data-disburse-advance]");
        if (disburseAdvance) {
            if (!confirm("تأكيد صرف السلفة؟ سيتم إنشاء السلفة الفعلية وربطها بالطلب بدون إنشاء قيد محاسبي من هذه الشاشة.")) { return; }
            runAdvanceAction(disburseAdvance, {
                url: root.getAttribute("data-disburse-advance-url"),
                body: { id: disburseAdvance.getAttribute("data-disburse-advance"), __RequestVerificationToken: token() },
                busyText: "صرف...",
                errorText: "تعذر صرف السلفة",
                successText: "تم صرف السلفة"
            });
            return;
        }

        var sendAdvanceApproval = event.target.closest("[data-send-advance-approval]");
        if (sendAdvanceApproval) {
            if (!confirm("إرسال طلب السلفة للاعتماد؟ بعد الإرسال سيتم قفل التعديل حتى انتهاء دورة الاعتماد.")) { return; }
            runAdvanceAction(sendAdvanceApproval, {
                url: root.getAttribute("data-send-advance-approval-url"),
                body: { id: sendAdvanceApproval.getAttribute("data-send-advance-approval"), remarks: "إرسال للاعتماد من شاشة السلف", __RequestVerificationToken: token() },
                busyText: "إرسال...",
                errorText: "تعذر إرسال طلب السلفة للاعتماد",
                successText: "تم إرسال طلب السلفة للاعتماد"
            });
            return;
        }

        var approveAdvance = event.target.closest("[data-approve-advance]");
        if (approveAdvance) {
            if (!confirm("تأكيد اعتماد طلب السلفة؟ بعد الاعتماد يتم قفل مسار التعديل حسب حالة الصرف والسداد.")) { return; }
            runAdvanceAction(approveAdvance, {
                url: root.getAttribute("data-approve-advance-url"),
                body: { id: approveAdvance.getAttribute("data-approve-advance"), remarks: "اعتماد من شاشة السلف", __RequestVerificationToken: token() },
                busyText: "اعتماد...",
                errorText: "تعذر اعتماد طلب السلفة",
                successText: "تم اعتماد طلب السلفة"
            });
            return;
        }

        var cancelAdvance = event.target.closest("[data-cancel-advance]");
        if (cancelAdvance) {
            var reason = prompt("سبب إلغاء طلب السلفة");
            if (reason === null) { return; }
            runAdvanceAction(cancelAdvance, {
                url: root.getAttribute("data-cancel-advance-url"),
                body: { id: cancelAdvance.getAttribute("data-cancel-advance"), remarks: reason || "إلغاء من شاشة السلف", __RequestVerificationToken: token() },
                busyText: "إلغاء...",
                errorText: "تعذر إلغاء طلب السلفة",
                successText: "تم إلغاء طلب السلفة"
            });
            return;
        }

        var editChanged = event.target.closest("[data-edit-changed-component]");
        if (editChanged) {
            if (!setButtonBusy(editChanged, true, "تحميل...")) { return; }
            fetch(root.getAttribute("data-changed-component-details-url") + "?id=" + encodeURIComponent(editChanged.getAttribute("data-edit-changed-component")), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    setButtonBusy(editChanged, false);
                    if (res.success) { showChangedComponent(res.data); } else { alert(res.message || "تعذر تحميل المفردة المتغيرة"); }
                }).catch(function () {
                    setButtonBusy(editChanged, false);
                    alert("تعذر الاتصال بالخادم.");
                });
            return;
        }

        var deleteChanged = event.target.closest("[data-delete-changed-component]");
        if (deleteChanged) {
            if (!confirm("تأكيد حذف المفردة المتغيرة؟ لا يمكن الحذف إذا تم استخدامها في المسير.")) { return; }
            if (!setButtonBusy(deleteChanged, true, "حذف...")) { return; }
            postForm(root.getAttribute("data-delete-changed-component-url"), { id: deleteChanged.getAttribute("data-delete-changed-component"), __RequestVerificationToken: token() })
                .then(function (res) {
                    if (!success(res)) {
                        setButtonBusy(deleteChanged, false);
                        alert(message(res, "تعذر حذف المفردة المتغيرة"));
                        return;
                    }
                    window.location.reload();
                }).catch(function () {
                    setButtonBusy(deleteChanged, false);
                    alert("تعذر الاتصال بالخادم.");
                });
            return;
        }

        var row = event.target.closest("[data-component-id]");
        if (row) {
            fetch(root.getAttribute("data-component-details-url") + "?id=" + encodeURIComponent(row.getAttribute("data-component-id")), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (res) { if (res.success) { show(res.data); } });
        }
    });

    var form = byId("lhfComponentForm");
    if (form) {
        form.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = form.querySelector("button[type='submit']");
            if (!setButtonBusy(submit, true, "حفظ...")) { return; }
            fetch(root.getAttribute("data-save-component-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
                body: new URLSearchParams(collect()).toString()
            }).then(function (r) {
                return r.json().then(function (j) { j._ok = r.ok; return j; });
            }).then(function (res) {
                if (!success(res)) {
                    byId("lhfEditorMessage").textContent = message(res, "تعذر الحفظ");
                    setButtonBusy(submit, false);
                    return;
                }
                window.location.reload();
            }).catch(function () {
                byId("lhfEditorMessage").textContent = "تعذر الاتصال بالخادم.";
                setButtonBusy(submit, false);
            });
        });
    }

    var changedForm = byId("lhfChangedComponentForm");
    if (changedForm) {
        changedForm.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = changedForm.querySelector("button[type='submit']");
            var clientError = validateChangedComponentClient();
            if (clientError) {
                setChangedMessage(clientError, true);
                return;
            }
            if (!setButtonBusy(submit, true, "حفظ...")) { return; }
            setChangedMessage("جاري الحفظ...");
            postForm(root.getAttribute("data-save-changed-component-url"), collectChangedComponent()).then(function (res) {
                if (!success(res)) {
                    setChangedMessage(message(res, "تعذر حفظ المفردة المتغيرة"), true);
                    setButtonBusy(submit, false);
                    return;
                }
                window.location.reload();
            }).catch(function () {
                setChangedMessage("تعذر الاتصال بالخادم.", true);
                setButtonBusy(submit, false);
            });
        });
    }

    Array.prototype.forEach.call(document.querySelectorAll(".lhf-select-search[data-filter-select]"), function (input) {
        input.addEventListener("input", function () {
            if (input.id === "lhfChangedEmployeeSearch") { return; }
            filterSelect(input.getAttribute("data-filter-select"), input.value);
        });
        input.addEventListener("keydown", function (event) {
            if (event.key === "ArrowDown") {
                var select = byId(input.getAttribute("data-filter-select"));
                if (select) { select.focus(); event.preventDefault(); }
            }
        });
    });
    var changedEmployeeSearch = byId("lhfChangedEmployeeSearch");
    if (changedEmployeeSearch) {
        changedEmployeeSearch.addEventListener("input", debounce(function () {
            loadChangedEmployees(changedEmployeeSearch.value);
        }, 180));
    }
    ["lhfChangedEmployeeId", "lhfChangedComponentId", "lhfChangedValue", "lhfChangedDays", "lhfChangedHours", "lhfChangedMinutes", "lhfChangedHourRate"].forEach(function (id) {
        var el = byId(id);
        if (el) {
            el.addEventListener("input", updateChangedEntryPreview);
            el.addEventListener("change", updateChangedEntryPreview);
        }
    });
    if (changedForm) {
        changedForm.addEventListener("keydown", function (event) {
            if ((event.ctrlKey || event.metaKey) && event.key === "Enter") {
                event.preventDefault();
                var submit = changedForm.querySelector("button[type='submit']");
                if (submit) { submit.click(); }
            }
        });
    }
    Array.prototype.forEach.call(root.querySelectorAll("form.lhf-search"), function (formElement) {
        formElement.addEventListener("submit", function () { setPageLoading(true); });
    });

    ["lhfAdvanceEmployeeId", "lhfAdvanceValue", "lhfPaymentCounts", "lhfFirstMonthPayment", "lhfFirstYearPayment"].forEach(function (id) {
        var el = byId(id);
        if (el) { el.addEventListener("input", function () { updateAdvancePreview(); }); el.addEventListener("change", function () { updateAdvancePreview(); }); }
    });
    var employeeSearch = byId("lhfAdvanceEmployeeSearch");
    if (employeeSearch) {
        employeeSearch.addEventListener("input", function () { filterAdvanceEmployees(employeeSearch.value); });
    }

    var advanceForm = byId("lhfAdvanceForm");
    if (advanceForm) {
        advanceForm.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = advanceForm.querySelector("button[type='submit']");
            if (submit && submit.disabled) { return; }
            if (!setButtonBusy(submit, true, "حفظ...")) { return; }
            setAdvanceMessage("جار الحفظ...");
            postForm(root.getAttribute("data-save-advance-url"), collectAdvance()).then(function (res) {
                if (!success(res)) {
                    setAdvanceMessage(message(res, "تعذر حفظ طلب السلفة"), true);
                    setButtonBusy(submit, false);
                    return;
                }
                setAdvanceMessage(message(res, "تم الحفظ"));
                window.location.reload();
            }).catch(function () {
                setAdvanceMessage("تعذر الاتصال بالخادم.", true);
                setButtonBusy(submit, false);
            });
        });
    }
    ["lhfVacationWithSalary", "lhfVacationWithoutSalary"].forEach(function (id) {
        var el = byId(id);
        if (!el) { return; }
        el.addEventListener("change", function () {
            if (id === "lhfVacationWithSalary" && el.checked) { val("lhfVacationWithoutSalary", false); }
            if (id === "lhfVacationWithoutSalary" && el.checked) { val("lhfVacationWithSalary", false); }
            updateVacationPreview();
        });
    });
    ["lhfVacationRequestEmployeeId", "lhfVacationRequestFromDate", "lhfVacationRequestToDate", "lhfVacationRequestResumeWork"].forEach(function (id) {
        var el = byId(id);
        if (el) { el.addEventListener("change", updateVacationPreview); }
    });
    var vacationEmployeeSearch = byId("lhfVacationEmployeeSearch");
    if (vacationEmployeeSearch) {
        vacationEmployeeSearch.addEventListener("input", function () { filterVacationEmployees(vacationEmployeeSearch.value); });
    }
    var vacationBalanceEmployee = byId("lhfVacationEmployeeId");
    if (vacationBalanceEmployee) {
        vacationBalanceEmployee.addEventListener("change", function () {
            var button = root.querySelector("[data-calc-vacation-balance]");
            if (vacationBalanceEmployee.value && button) { calculateVacationBalance(button); }
        });
        if (vacationBalanceEmployee.value) {
            setTimeout(function () {
                var button = root.querySelector("[data-calc-vacation-balance]");
                if (button) { calculateVacationBalance(button); }
            }, 100);
        }
    }
    var vacationTableSearch = byId("lhfVacationTableSearch");
    if (vacationTableSearch) {
        vacationTableSearch.addEventListener("input", function () {
            var search = vacationTableSearch.value.toLowerCase();
            root.querySelectorAll(".lhf-vacation-table tbody tr[data-vacation-search]").forEach(function (row) {
                row.hidden = !!(search && (row.getAttribute("data-vacation-search") || "").indexOf(search) === -1);
            });
        });
    }
    var searchForm = root.querySelector(".lhf-search");
    if (searchForm) {
        searchForm.addEventListener("submit", function () {
            var submit = searchForm.querySelector("button[type='submit']");
            setPageLoading(true);
            setButtonBusy(submit, true, "بحث...");
        });
    }
    var vacationForm = byId("lhfVacationForm");
    if (vacationForm) {
        vacationForm.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = vacationForm.querySelector("button[type='submit']");
            if (!setButtonBusy(submit, true, "حفظ...")) { return; }
            setVacationMessage("جاري التحقق من الرصيد وحفظ الطلب...");
            postForm(root.getAttribute("data-save-vacation-url"), collectVacation()).then(function (res) {
                if (!success(res)) {
                    setVacationMessage(message(res, "تعذر حفظ طلب الإجازة."), true);
                    setButtonBusy(submit, false);
                    return;
                }
                setVacationMessage(message(res, "تم حفظ طلب الإجازة."));
                window.location.reload();
            }).catch(function () {
                setVacationMessage("تعذر الاتصال بالخادم.", true);
                setButtonBusy(submit, false);
            });
        });
    }
}());
