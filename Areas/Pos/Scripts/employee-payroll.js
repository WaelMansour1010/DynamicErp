(function () {
    "use strict";
    var root = document.querySelector(".ep-page");
    if (!root) { return; }
    var screen = root.getAttribute("data-screen");
    var readOnly = root.getAttribute("data-read-only") === "true";
    var enterpriseDependents = [];
    var enterpriseRules = [];
    var enterpriseCoverageRows = [];

    function byId(id) { return document.getElementById(id); }
    function money(v) { var n = parseFloat(v || 0); return isNaN(n) ? "0.00" : n.toFixed(2); }
    function number(v) { var n = parseFloat(v || 0); return isNaN(n) ? 0 : n; }
    function html(v) { return String(v == null ? "" : v).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }
    function dateInput(v) {
        if (!v) { return ""; }
        if (typeof v === "string" && v.indexOf("/Date(") === 0) {
            v = new Date(parseInt(/\/Date\((-?\d+)\)\//.exec(v)[1], 10));
        } else {
            v = new Date(v);
        }
        if (isNaN(v.getTime())) { return ""; }
        return v.getFullYear() + "-" + String(v.getMonth() + 1).padStart(2, "0") + "-" + String(v.getDate()).padStart(2, "0");
    }
    function message(text, error) {
        var el = byId("epMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.classList.toggle("error", !!error);
    }
    function getJson(url) {
        return fetch(url, { credentials: "same-origin" }).then(function (r) {
            return r.json().catch(function () {
                return { success: false, message: "Unexpected server response." };
            });
        }).catch(function () {
            return { success: false, message: "Network or server error." };
        });
    }
    function postJson(url, data) {
        if (!url) {
            return Promise.resolve({ success: false, message: "This action is not available in this area." });
        }
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data || {})
        }).then(function (r) {
            return r.json().catch(function () {
                return { success: false, message: "Unexpected server response." };
            });
        }).catch(function () {
            return { success: false, message: "Network or server error." };
        });
    }
    function queryString(data) {
        var params = new URLSearchParams();
        Object.keys(data || {}).forEach(function (key) {
            var value = data[key];
            if (value !== null && value !== undefined && value !== "") {
                params.append(key, value);
            }
        });
        return params.toString();
    }
    function fillSelect(select, rows, emptyText) {
        if (!select) { return; }
        select.innerHTML = '<option value="">' + html(emptyText || "الكل") + "</option>";
        (rows || []).forEach(function (x) {
            select.insertAdjacentHTML("beforeend", '<option value="' + html(x.Id) + '">' + html(x.Name) + "</option>");
        });
    }
    function fillSelectFromRows(select, rows, idProp, textProp, emptyText) {
        if (!select) { return; }
        select.innerHTML = '<option value="">' + html(emptyText || "الكل") + "</option>";
        (rows || []).forEach(function (x) {
            select.insertAdjacentHTML("beforeend", '<option value="' + html(x[idProp]) + '">' + html(x[textProp]) + "</option>");
        });
    }
    function loadLookups() {
        var url = root.getAttribute("data-lookups-url");
        if (!url) { return Promise.resolve({}); }
        return getJson(url).then(function (res) {
            var data = res.data || {};
            ["epBranchFilter", "epBranch", "epRunBranch", "epMedBranch"].forEach(function (id) { fillSelect(byId(id), data.Branches, id === "epBranch" ? "غير محدد" : "كل الفروع"); });
            ["epDepartmentFilter", "epDepartment", "epRunDepartment", "epMedDepartment"].forEach(function (id) { fillSelect(byId(id), data.Departments, id === "epDepartment" ? "غير محدد" : "كل الإدارات"); });
            fillSelect(byId("epJob"), data.Jobs, "غير محدد");
            fillSelect(byId("epInsurancePlan"), data.MedicalInsurancePlans, "غير محدد");
            fillSelect(byId("epReportProvider"), data.MedicalInsuranceProviders, "كل الشركات");
            fillSelect(byId("epReportPlan"), data.MedicalInsurancePlans, "كل الخطط");
            return data;
        });
    }

    function calcShare(cost, type, value) {
        if (type === "Percent") { return cost * value / 100; }
        if (type === "AutoBalance") { return null; }
        return value;
    }
    function updateInsurancePreview() {
        if (!byId("epInsuranceMonthlyCost")) { return; }
        var cost = number(byId("epInsuranceMonthlyCost").value);
        var employee = calcShare(cost, byId("epEmployeeShareType").value, number(byId("epEmployeeShareValue").value));
        employee = Math.max(0, Math.min(cost, employee || 0));
        var company = calcShare(cost, byId("epCompanyShareType").value, number(byId("epCompanyShareValue").value));
        if (company === null) { company = cost - employee; }
        company = Math.max(0, company || 0);
        if (employee + company > cost) { company = Math.max(0, cost - employee); }
        byId("epEmployeeDeductionPreview").value = money(employee);
        byId("epCompanyCostPreview").value = money(company);
    }

    function employeeFilter() {
        return {
            Term: byId("epSearchTerm").value,
            BranchId: byId("epBranchFilter").value || null,
            DepartmentId: byId("epDepartmentFilter").value || null,
            IsActive: byId("epActiveFilter").value === "" ? null : byId("epActiveFilter").value === "true"
        };
    }
    function loadEmployees() {
        var url = root.getAttribute("data-search-url") + "?" + queryString(employeeFilter());
        return getJson(url).then(function (res) {
            var tbody = byId("epEmployeesRows");
            tbody.innerHTML = "";
            (res.rows || []).forEach(function (x) {
                tbody.insertAdjacentHTML("beforeend",
                    '<tr><td>' + html(x.EmployeeCode) + '</td><td>' + html(x.EmployeeName) + '</td><td>' + html(x.BranchName) + '</td><td>' + html(x.DepartmentName) + '</td><td>' + html(x.JobTypeName) + '</td><td>' + money(x.BasicSalary) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "غير نشط") + '</span></td><td><div class="ep-row-actions"><button type="button" data-edit="' + x.EmployeeId + '"><i class="fas fa-edit"></i></button><button type="button" data-active="' + x.EmployeeId + '" data-state="' + (!x.IsActive) + '">' + (x.IsActive ? "تعطيل" : "تفعيل") + '</button></div></td></tr>');
            });
            message("تم تحميل " + (res.rows || []).length + " موظف");
        });
    }
    function enforceReadOnlyRows() {
        var tbody = byId("epEmployeesRows");
        if (!readOnly || !tbody) { return; }
        tbody.querySelectorAll("[data-active]").forEach(function (x) { x.remove(); });
        tbody.querySelectorAll("[data-edit] i").forEach(function (x) { x.className = "fas fa-eye"; });
    }
    function openEditor(employee) {
        employee = employee || { IsActive: true, MedicalInsurance: { IsMonthly: true, EmployeeShareType: "Amount", CompanyShareType: "AutoBalance" }, MedicalInsuranceHistory: [] };
        byId("epEmployeeId").value = employee.EmployeeId || "";
        byId("epCode").value = employee.EmployeeCode || "";
        byId("epName").value = employee.EmployeeName || "";
        byId("epMobile").value = employee.Mobile || "";
        byId("epPhone").value = employee.Phone || "";
        byId("epEmail").value = employee.Email || "";
        byId("epIsActive").checked = employee.IsActive !== false;
        byId("epBranch").value = employee.BranchId || "";
        byId("epDepartment").value = employee.DepartmentId || "";
        byId("epJob").value = employee.JobTypeId || "";
        byId("epHiringDate").value = dateInput(employee.HiringDate);
        byId("epSalary").value = employee.BasicSalary || 0;
        byId("epAccountCode").value = employee.AccountCode || "";
        byId("epAccruedAccountCode").value = employee.AccruedSalaryAccountCode || "";
        byId("epNotes").value = employee.Notes || "";
        var mi = employee.MedicalInsurance || {};
        byId("epInsuranceId").value = mi.Id || "";
        byId("epInsuranceActive").checked = !!mi.IsActive;
        byId("epInsurancePlan").value = mi.PlanId || "";
        byId("epInsuranceMonthlyCost").value = mi.MonthlyCost || 0;
        byId("epEmployeeShareType").value = mi.EmployeeShareType || mi.DeductionType || "Amount";
        byId("epEmployeeShareValue").value = mi.EmployeeShareValue || mi.Amount || 0;
        byId("epCompanyShareType").value = mi.CompanyShareType || "AutoBalance";
        byId("epCompanyShareValue").value = mi.CompanyShareValue || 0;
        byId("epInsuranceStart").value = dateInput(mi.StartDate);
        byId("epInsuranceEnd").value = dateInput(mi.EndDate);
        byId("epInsuranceMonthly").checked = mi.IsMonthly !== false;
        byId("epInsuranceNotes").value = mi.Notes || "";
        renderInsuranceHistory(employee.MedicalInsuranceHistory || []);
        updateInsurancePreview();
        byId("epEmployeeEditor").hidden = false;
        if (readOnly) {
            byId("epEmployeeEditor").querySelectorAll("input, select, textarea").forEach(function (el) {
                el.disabled = true;
            });
        }
    }
    function renderInsuranceHistory(rows) {
        var tbody = byId("epInsuranceHistory");
        if (!tbody) { return; }
        tbody.innerHTML = "";
        (rows || []).forEach(function (x) {
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.PlanName) + '</td><td>' + html(dateInput(x.StartDate)) + '</td><td>' + html(dateInput(x.EndDate)) + '</td><td>' + money(x.EmployeeMonthlyDeduction) + '</td><td>' + money(x.CompanyMonthlyCost) + '</td><td>' + (x.IsActive ? "نشط" : "متوقف") + '</td></tr>');
        });
    }
    function collectEmployee() {
        updateInsurancePreview();
        return {
            EmployeeId: byId("epEmployeeId").value ? parseInt(byId("epEmployeeId").value, 10) : null,
            EmployeeCode: byId("epCode").value,
            EmployeeName: byId("epName").value,
            BranchId: byId("epBranch").value ? parseInt(byId("epBranch").value, 10) : null,
            DepartmentId: byId("epDepartment").value ? parseInt(byId("epDepartment").value, 10) : null,
            JobTypeId: byId("epJob").value ? parseInt(byId("epJob").value, 10) : null,
            HiringDate: byId("epHiringDate").value || null,
            IsActive: byId("epIsActive").checked,
            BasicSalary: number(byId("epSalary").value),
            AccountCode: byId("epAccountCode").value,
            AccruedSalaryAccountCode: byId("epAccruedAccountCode").value,
            Phone: byId("epPhone").value,
            Mobile: byId("epMobile").value,
            Email: byId("epEmail").value,
            Notes: byId("epNotes").value,
            MedicalInsurance: {
                Id: byId("epInsuranceId").value ? parseInt(byId("epInsuranceId").value, 10) : null,
                PlanId: byId("epInsurancePlan").value ? parseInt(byId("epInsurancePlan").value, 10) : null,
                MonthlyCost: number(byId("epInsuranceMonthlyCost").value),
                EmployeeShareType: byId("epEmployeeShareType").value,
                EmployeeShareValue: number(byId("epEmployeeShareValue").value),
                CompanyShareType: byId("epCompanyShareType").value,
                CompanyShareValue: number(byId("epCompanyShareValue").value),
                EmployeeMonthlyDeduction: number(byId("epEmployeeDeductionPreview").value),
                CompanyMonthlyCost: number(byId("epCompanyCostPreview").value),
                StartDate: byId("epInsuranceStart").value || null,
                EndDate: byId("epInsuranceEnd").value || null,
                IsMonthly: byId("epInsuranceMonthly").checked,
                IsActive: byId("epInsuranceActive").checked,
                Notes: byId("epInsuranceNotes").value
            }
        };
    }

    function salaryRequest() {
        return {
            Year: parseInt(byId("epRunYear").value, 10),
            Month: parseInt(byId("epRunMonth").value, 10),
            BranchId: byId("epRunBranch").value ? parseInt(byId("epRunBranch").value, 10) : null,
            DepartmentId: byId("epRunDepartment").value ? parseInt(byId("epRunDepartment").value, 10) : null,
            EmployeeId: byId("epRunEmployee").value ? parseInt(byId("epRunEmployee").value, 10) : null,
            IncludeSavedDrafts: true
        };
    }
    function renderSalary(preview) {
        preview = preview || {};
        byId("epTotalBasic").textContent = money(preview.TotalBasic);
        byId("epTotalAdditions").textContent = money(preview.TotalAdditions);
        byId("epTotalDeductions").textContent = money(preview.TotalDeductions);
        byId("epTotalMedical").textContent = money(preview.TotalMedicalInsurance);
        byId("epTotalCompanyMedical").textContent = money(preview.TotalMedicalInsuranceCompanyCost);
        byId("epTotalNet").textContent = money(preview.TotalNet);
        byId("epSalaryRows").innerHTML = "";
        (preview.Rows || []).forEach(function (x) {
            byId("epSalaryRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.BranchName) + '</td><td>' + money(x.BasicSalary) + '</td><td>' + money((x.SalaryAllowances || 0) + (x.VariableAdditions || 0)) + '</td><td>' + money(x.AdvanceDeduction) + '</td><td>' + money(x.ExistingDiscounts) + '</td><td>' + html(x.MedicalInsurancePlanName || "") + '</td><td>' + money(x.MedicalInsuranceMonthlyCost) + '</td><td>' + money(x.MedicalInsuranceDeduction) + '</td><td>' + money(x.MedicalInsuranceCompanyCost) + '</td><td>' + money(x.NetSalary) + '</td><td>' + (x.IsApproved ? "معتمد" : "مسودة") + '</td></tr>');
        });
        byId("epJournalRows").innerHTML = "";
        (preview.JournalPreview || []).forEach(function (x) {
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.AccountCode || "غير محدد") + '</td><td>' + money(x.Debit) + '</td><td>' + money(x.Credit) + '</td><td>' + html(x.Description) + '</td><td>' + html(x.EmployeeId || "") + '</td></tr>');
        });
        message(preview.Message || "تم حساب المسير");
    }
    function previewSalary() {
        var url = root.getAttribute("data-preview-url") + "?" + queryString(salaryRequest());
        return getJson(url).then(function (res) {
            if (!res.success) { message(res.message || "تعذر الحساب", true); return; }
            renderSalary(res.preview);
        });
    }

    function formatMoney(v) {
        var n = number(v);
        return n.toLocaleString ? n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : money(n);
    }
    function setText(id, value) {
        var el = byId(id);
        if (el) { el.textContent = value; }
    }
    function statusClass(value) {
        return String(value || "Draft").toLowerCase().replace(/\s+/g, "-");
    }
    function updateLifecycleBadge() {
        var status = byId("epPlanLifecycleStatus") ? byId("epPlanLifecycleStatus").value : "Draft";
        var badge = byId("epLifecycleBadge");
        var heroBadge = byId("epHeroLifecycleBadge");
        if (badge) {
            badge.textContent = status;
            badge.className = "ep-status-badge " + statusClass(status);
        }
        if (heroBadge) {
            heroBadge.textContent = status;
            heroBadge.className = "ep-status-badge " + statusClass(status);
        }
        if (byId("epHeroActiveState")) {
            byId("epHeroActiveState").textContent = byId("epPlanActive") && byId("epPlanActive").checked ? "متاحة للربط" : "غير متاحة";
        }
    }
    function selectedText(select) {
        if (!select || select.selectedIndex < 0) { return ""; }
        return select.options[select.selectedIndex] ? select.options[select.selectedIndex].text : "";
    }
    function updateHeroSummary() {
        var heroTitle = byId("epHeroPlanName");
        if (!heroTitle) { return; }
        var planName = byId("epPlanNameAr") ? byId("epPlanNameAr").value : "";
        var providerName = selectedText(byId("epPlanProvider"));
        var start = byId("epPlanStartDate") ? byId("epPlanStartDate").value : "";
        var end = byId("epPlanEndDate") ? byId("epPlanEndDate").value : "";
        byId("epHeroPlanName").textContent = planName || "إدارة التأمين الطبي";
        byId("epHeroProviderName").textContent = providerName || "مساحة عمل منظمة لإدارة شركات التأمين والخطط وربطها بالمسير.";
        byId("epHeroProviderMeta").textContent = providerName || "غير محدد";
        byId("epHeroDateRange").textContent = start || end ? ((start || "غير محدد") + " - " + (end || "مفتوح")) : "غير محدد";
        setText("epHeroMonthlyCost", formatMoney(byId("epPlanMonthlyCost") ? byId("epPlanMonthlyCost").value : 0));
        setText("epHeroDependents", enterpriseDependents.length);
        updateLifecycleBadge();
    }
    function calculatePlanPreview() {
        if (!byId("epPreviewSalary")) { return; }
        var salary = number(byId("epPreviewSalary").value);
        var cost = number(byId("epPlanMonthlyCost").value);
        var employee = calcShare(cost, byId("epPlanEmployeeShareType").value, number(byId("epPlanEmployeeShareValue").value));
        employee = Math.max(0, Math.min(cost, employee || 0));
        var company = calcShare(cost, byId("epPlanCompanyShareType").value, number(byId("epPlanCompanyShareValue").value));
        if (company === null) { company = cost - employee; }
        company = Math.max(0, Math.min(cost, company || 0));
        if (employee + company > cost) { company = Math.max(0, cost - employee); }
        setText("epPreviewSalaryOut", formatMoney(salary));
        setText("epPreviewCostOut", formatMoney(cost));
        setText("epPreviewEmployeeOut", formatMoney(employee));
        setText("epPreviewCompanyOut", formatMoney(company));
        setText("epPreviewNetOut", formatMoney(Math.max(0, salary - employee)));
        updateHeroSummary();
        validateEnterpriseInsurance();
    }
    function validateEnterpriseInsurance() {
        var box = byId("epInsuranceValidation");
        if (!box) { return true; }
        var errors = [];
        var start = byId("epPlanStartDate") ? byId("epPlanStartDate").value : "";
        var end = byId("epPlanEndDate") ? byId("epPlanEndDate").value : "";
        var status = byId("epPlanLifecycleStatus") ? byId("epPlanLifecycleStatus").value : "Draft";
        var maxDependents = byId("epMaxDependents") ? parseInt(byId("epMaxDependents").value || "0", 10) : 0;
        if (start && end && new Date(end) < new Date(start)) { errors.push("لا يمكن أن ينتهي التأمين قبل تاريخ البداية."); }
        if (enterpriseDependents.length > maxDependents) { errors.push("عدد التابعين يتجاوز الحد الأقصى المحدد للخطة."); }
        if (status === "Active" && (!byId("epPlanProvider").value || !byId("epPlanNameAr").value)) { errors.push("تفعيل الخطة يتطلب شركة تأمين واسم خطة."); }
        if (number(byId("epPlanMonthlyCost") ? byId("epPlanMonthlyCost").value : 0) <= 0 && status === "Active") { errors.push("تفعيل الخطة يتطلب تكلفة شهرية أكبر من صفر."); }
        box.innerHTML = errors.map(function (x) { return "<div>" + html(x) + "</div>"; }).join("");
        return errors.length === 0;
    }
    function relationLabel(value) {
        if (value === "Child") { return "ابن/ابنة"; }
        if (value === "Parent") { return "والد/والدة"; }
        return "زوجة/زوج";
    }
    function ageFromDate(value) {
        if (!value) { return 0; }
        var d = new Date(value);
        if (isNaN(d.getTime())) { return 0; }
        var now = new Date();
        var age = now.getFullYear() - d.getFullYear();
        if (now.getMonth() < d.getMonth() || (now.getMonth() === d.getMonth() && now.getDate() < d.getDate())) { age--; }
        return Math.max(0, age);
    }
    function dependentCost(relation) {
        if (relation === "Child") { return number(byId("epChildCost").value); }
        if (relation === "Parent") { return number(byId("epParentCost").value); }
        return number(byId("epSpouseCost").value);
    }
    function renderDependents() {
        var tbody = byId("epDependentsRows");
        var cards = byId("epDependentsCards");
        if (!tbody) { return; }
        tbody.innerHTML = "";
        if (cards) { cards.innerHTML = ""; }
        if (!enterpriseDependents.length) {
            tbody.innerHTML = '<tr><td colspan="6" class="ep-empty-row">لا يوجد تابعون في المعاينة الحالية.</td></tr>';
        }
        enterpriseDependents.forEach(function (x, i) {
            var age = ageFromDate(x.BirthDate);
            var cost = dependentCost(x.Relation);
            if (cards) {
                cards.insertAdjacentHTML("beforeend", '<article class="ep-dependent-card"><div><strong>' + html(x.Name) + '</strong><span>' + html(relationLabel(x.Relation)) + '</span></div><div class="ep-dependent-metrics"><span>' + age + ' yrs</span><span>' + html(x.CoveragePercent) + '%</span><span>' + formatMoney(cost) + '</span></div><button type="button" title="Remove" data-remove-dependent="' + i + '"><i class="fas fa-trash"></i></button></article>');
            }
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.Name) + '</td><td>' + html(relationLabel(x.Relation)) + '</td><td>' + age + '</td><td>' + html(x.CoveragePercent) + '%</td><td>' + formatMoney(cost) + '</td><td><button type="button" data-remove-dependent="' + i + '"><i class="fas fa-trash"></i></button></td></tr>');
        });
        if (cards && !cards.innerHTML) { cards.innerHTML = '<div class="ep-empty-card">No dependents in this plan preview.</div>'; }
        setText("epHeroDependents", enterpriseDependents.length);
        validateEnterpriseInsurance();
    }
    function renderRules() {
        var tbody = byId("epRulesRows");
        var cards = byId("epRulesCards");
        if (!tbody) { return; }
        tbody.innerHTML = "";
        if (cards) { cards.innerHTML = ""; }
        if (!enterpriseRules.length) {
            tbody.innerHTML = '<tr><td colspan="4" class="ep-empty-row">أضف قواعد تحمل ذكية حسب الوظيفة أو الإدارة أو الفرع.</td></tr>';
        }
        enterpriseRules.forEach(function (x) {
            if (cards) {
                cards.insertAdjacentHTML("beforeend", '<article class="ep-rule-card"><div class="ep-rule-line"><span>IF</span><strong>' + html(x.Scope) + '</strong><em>=</em><strong>' + html(x.Value) + '</strong></div><div class="ep-rule-line then"><span>THEN</span><strong>Company Contribution</strong><em>=</em><strong>' + html(x.CompanyPercent) + '%</strong></div><p>' + html(x.Description || "") + '</p></article>');
            }
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.Scope) + '</td><td>' + html(x.Value) + '</td><td>' + html(x.CompanyPercent) + '%</td><td>' + html(x.Description) + '</td></tr>');
        });
        if (cards && !cards.innerHTML) { cards.innerHTML = '<div class="ep-empty-card">No automation rules yet.</div>'; }
    }
    function addRule(scope, value, percent, description) {
        enterpriseRules.push({ Scope: scope, Value: value, CompanyPercent: percent, Description: description });
        renderRules();
    }
    function openDrawer(id) {
        var drawer = byId(id);
        var backdrop = byId("epDrawerBackdrop");
        if (backdrop) { backdrop.hidden = false; }
        if (drawer) {
            drawer.hidden = false;
            setTimeout(function () { drawer.classList.add("open"); }, 0);
        }
        document.body.classList.add("ep-drawer-open");
    }
    function closeDrawers() {
        var backdrop = byId("epDrawerBackdrop");
        if (backdrop) { backdrop.hidden = true; }
        root.querySelectorAll(".ep-side-drawer").forEach(function (drawer) {
            drawer.classList.remove("open");
            drawer.hidden = true;
        });
        document.body.classList.remove("ep-drawer-open");
    }
    function resetPlanForm() {
        byId("epPlanForm").reset();
        byId("epPlanId").value = "";
        enterpriseDependents = [];
        enterpriseRules = [];
        renderDependents();
        renderRules();
        updateHeroSummary();
    }
    function renderMasterCards(providers, plans) {
        var providerHost = byId("epProvidersCards");
        var planHost = byId("epPlansCards");
        var term = (byId("epMasterSearch") ? byId("epMasterSearch").value : "").toLowerCase();
        var status = byId("epMasterStatus") ? byId("epMasterStatus").value : "";
        if (providerHost) {
            providerHost.innerHTML = "";
            (providers || []).filter(function (x) {
                return !term || [x.ProviderNameAr, x.ProviderNameEn, x.Phone].join(" ").toLowerCase().indexOf(term) >= 0;
            }).forEach(function (x) {
                providerHost.insertAdjacentHTML("beforeend", '<article class="ep-provider-mini-card" data-provider="' + html(x.ProviderId) + '"><div><strong>' + html(x.ProviderNameAr || "شركة تأمين") + '</strong><span>' + html(x.ProviderNameEn || x.Phone || "") + '</span></div><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></article>');
            });
            if (!providerHost.innerHTML) { providerHost.innerHTML = '<div class="ep-empty-card">لا توجد شركات مطابقة.</div>'; }
        }
        if (planHost) {
            planHost.innerHTML = "";
            (plans || []).filter(function (x) {
                var lifecycle = x.LifecycleStatus || (x.IsActive ? "Active" : "Draft");
                var haystack = [x.PlanNameAr, x.PlanNameEn, x.ProviderName, lifecycle].join(" ").toLowerCase();
                return (!term || haystack.indexOf(term) >= 0) && (!status || lifecycle === status);
            }).forEach(function (x) {
                var lifecycle = x.LifecycleStatus || (x.IsActive ? "Active" : "Draft");
                planHost.insertAdjacentHTML("beforeend", '<article class="ep-plan-master-card" data-plan="' + html(x.PlanId) + '"><div class="ep-plan-card-top"><span class="ep-status-badge ' + statusClass(lifecycle) + '">' + html(lifecycle) + '</span><button type="button" data-plan="' + html(x.PlanId) + '"><i class="fas fa-pen"></i></button></div><h3>' + html(x.PlanNameAr || "خطة تأمين") + '</h3><p>' + html(x.ProviderName || "بدون شركة") + '</p><dl><div><dt>Monthly Cost</dt><dd>' + formatMoney(x.DefaultMonthlyCost) + '</dd></div><div><dt>Employee</dt><dd>' + html(shareTypeLabel(x.DefaultEmployeeShareType)) + ' ' + money(x.DefaultEmployeeShareValue) + '</dd></div><div><dt>Company</dt><dd>' + html(shareTypeLabel(x.DefaultCompanyShareType)) + ' ' + money(x.DefaultCompanyShareValue) + '</dd></div></dl></article>');
            });
            if (!planHost.innerHTML) { planHost.innerHTML = '<div class="ep-empty-card">لا توجد خطط مطابقة للفلاتر.</div>'; }
        }
    }
    function renderCoverageRows(rows) {
        var tbody = byId("epCoverageRows");
        if (!tbody) { return; }
        var term = (byId("epCoverageSearch") ? byId("epCoverageSearch").value : "").toLowerCase();
        var status = byId("epCoverageStatus") ? byId("epCoverageStatus").value : "";
        var monthly = 0, employee = 0, company = 0;
        tbody.innerHTML = "";
        (rows || []).filter(function (x) {
            var haystack = [x.EmployeeCode, x.EmployeeName, x.ProviderName, x.PlanName].join(" ").toLowerCase();
            var rowStatus = x.IsActive ? "active" : "expired";
            return (!term || haystack.indexOf(term) >= 0) && (!status || status === rowStatus);
        }).forEach(function (x) {
            monthly += number(x.MonthlyCost);
            employee += number(x.EmployeeMonthlyDeduction);
            company += number(x.CompanyMonthlyCost);
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + ' - ' + html(x.EmployeeName) + '</td><td>' + html(x.ProviderName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html(dateInput(x.StartDate)) + '</td><td>' + html(dateInput(x.EndDate)) + '</td><td>' + formatMoney(x.MonthlyCost) + '</td><td>' + formatMoney(x.EmployeeMonthlyDeduction) + '</td><td>' + formatMoney(x.CompanyMonthlyCost) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "Active" : "Expired") + '</span></td></tr>');
        });
        if (!tbody.innerHTML) { tbody.innerHTML = '<tr><td colspan="9" class="ep-empty-row">لا توجد بيانات تغطية مطابقة للفلاتر.</td></tr>'; }
        setText("epAnalyticsMonthly", formatMoney(monthly));
        setText("epAnalyticsAnnual", formatMoney(monthly * 12));
        setText("epAnalyticsEmployee", formatMoney(employee));
        setText("epAnalyticsCompany", formatMoney(company));
    }
    function loadCoverageAnalytics() {
        var url = root.getAttribute("data-subscriptions-url");
        if (!url) { return Promise.resolve(); }
        return getJson(url + "?ActiveOnly=false").then(function (res) {
            enterpriseCoverageRows = res.rows || [];
            var activeRows = enterpriseCoverageRows.filter(function (x) { return x.IsActive; });
            var expiredRows = enterpriseCoverageRows.filter(function (x) { return !x.IsActive; });
            setText("epStatCovered", activeRows.length);
            setText("epStatExpired", expiredRows.length);
            setText("epStatEmployeeCost", formatMoney(activeRows.reduce(function (s, x) { return s + number(x.EmployeeMonthlyDeduction); }, 0)));
            setText("epStatCompanyCost", formatMoney(activeRows.reduce(function (s, x) { return s + number(x.CompanyMonthlyCost); }, 0)));
            renderCoverageRows(enterpriseCoverageRows);
        });
    }

    function collectProvider() {
        return {
            ProviderId: byId("epProviderId").value ? parseInt(byId("epProviderId").value, 10) : null,
            ProviderNameAr: byId("epProviderNameAr").value,
            ProviderNameEn: byId("epProviderNameEn").value,
            Phone: byId("epProviderPhone").value,
            Notes: byId("epProviderNotes").value,
            IsActive: byId("epProviderActive").checked
        };
    }
    function collectPlan() {
        return {
            PlanId: byId("epPlanId").value ? parseInt(byId("epPlanId").value, 10) : null,
            ProviderId: byId("epPlanProvider").value ? parseInt(byId("epPlanProvider").value, 10) : 0,
            PlanNameAr: byId("epPlanNameAr").value,
            PlanNameEn: byId("epPlanNameEn").value,
            DefaultMonthlyCost: number(byId("epPlanMonthlyCost").value),
            DefaultEmployeeShareType: byId("epPlanEmployeeShareType").value,
            DefaultEmployeeShareValue: number(byId("epPlanEmployeeShareValue").value),
            DefaultCompanyShareType: byId("epPlanCompanyShareType").value,
            DefaultCompanyShareValue: number(byId("epPlanCompanyShareValue").value),
            EmployeeDeductionAccountCode: byId("epPlanEmployeeAccount").value,
            CompanyCostAccountCode: byId("epPlanCompanyAccount").value,
            LifecycleStatus: byId("epPlanLifecycleStatus") ? byId("epPlanLifecycleStatus").value : "Draft",
            StartDate: byId("epPlanStartDate") ? byId("epPlanStartDate").value || null : null,
            EndDate: byId("epPlanEndDate") ? byId("epPlanEndDate").value || null : null,
            PayrollStartDate: byId("epPlanPayrollStartDate") ? byId("epPlanPayrollStartDate").value || null : null,
            SuspensionDate: byId("epPlanSuspensionDate") ? byId("epPlanSuspensionDate").value || null : null,
            CancellationDate: byId("epPlanCancellationDate") ? byId("epPlanCancellationDate").value || null : null,
            CostCenterCode: byId("epPlanCostCenter") ? byId("epPlanCostCenter").value : "",
            PayrollDeductionType: byId("epPlanPayrollDeductionType") ? byId("epPlanPayrollDeductionType").value : "Fixed",
            IsMonthlyDeduction: byId("epPlanIsMonthlyDeduction") ? byId("epPlanIsMonthlyDeduction").checked : true,
            AutoStopAtEndDate: byId("epPlanAutoStop") ? byId("epPlanAutoStop").checked : true,
            ShowInPayroll: byId("epPlanShowInPayroll") ? byId("epPlanShowInPayroll").checked : true,
            DistributeByDepartment: byId("epPlanDistributeDepartments") ? byId("epPlanDistributeDepartments").checked : false,
            DistributeByCostCenter: byId("epPlanDistributeCostCenter") ? byId("epPlanDistributeCostCenter").checked : false,
            TaxMode: byId("epPlanTaxMode") ? byId("epPlanTaxMode").value : "AfterTax",
            MaxDependents: byId("epMaxDependents") ? parseInt(byId("epMaxDependents").value || "0", 10) : 0,
            ChildrenMaxAge: byId("epChildrenMaxAge") ? parseInt(byId("epChildrenMaxAge").value || "0", 10) : 0,
            SpouseAdditionalCost: byId("epSpouseCost") ? number(byId("epSpouseCost").value) : 0,
            ChildAdditionalCost: byId("epChildCost") ? number(byId("epChildCost").value) : 0,
            ParentAdditionalCost: byId("epParentCost") ? number(byId("epParentCost").value) : 0,
            DefaultCoveragePercent: byId("epDefaultCoveragePercent") ? number(byId("epDefaultCoveragePercent").value) : 100,
            AutoEnrollAfterDays: byId("epAutoEnrollAfterDays") ? parseInt(byId("epAutoEnrollAfterDays").value || "0", 10) : 0,
            AutoEnrollCriteria: byId("epAutoEnrollCriteria") ? byId("epAutoEnrollCriteria").value : "",
            RulesJson: JSON.stringify(enterpriseRules),
            DependentsTemplateJson: JSON.stringify(enterpriseDependents),
            IsActive: byId("epPlanActive").checked,
            Notes: byId("epPlanNotes").value
        };
    }
    function fillProviderForm(x) {
        byId("epProviderId").value = x.ProviderId || "";
        byId("epProviderNameAr").value = x.ProviderNameAr || "";
        byId("epProviderNameEn").value = x.ProviderNameEn || "";
        byId("epProviderPhone").value = x.Phone || "";
        byId("epProviderNotes").value = x.Notes || "";
        byId("epProviderActive").checked = x.IsActive !== false;
    }
    function fillPlanForm(x) {
        byId("epPlanId").value = x.PlanId || "";
        byId("epPlanProvider").value = x.ProviderId || "";
        byId("epPlanNameAr").value = x.PlanNameAr || "";
        byId("epPlanNameEn").value = x.PlanNameEn || "";
        byId("epPlanMonthlyCost").value = x.DefaultMonthlyCost || 0;
        byId("epPlanEmployeeShareType").value = x.DefaultEmployeeShareType || "Amount";
        byId("epPlanEmployeeShareValue").value = x.DefaultEmployeeShareValue || 0;
        byId("epPlanCompanyShareType").value = x.DefaultCompanyShareType || "AutoBalance";
        byId("epPlanCompanyShareValue").value = x.DefaultCompanyShareValue || 0;
        byId("epPlanEmployeeAccount").value = x.EmployeeDeductionAccountCode || "";
        byId("epPlanCompanyAccount").value = x.CompanyCostAccountCode || "";
        if (byId("epPlanLifecycleStatus")) { byId("epPlanLifecycleStatus").value = x.LifecycleStatus || (x.IsActive ? "Active" : "Draft"); }
        if (byId("epPlanStartDate")) { byId("epPlanStartDate").value = dateInput(x.StartDate); }
        if (byId("epPlanEndDate")) { byId("epPlanEndDate").value = dateInput(x.EndDate); }
        if (byId("epPlanPayrollStartDate")) { byId("epPlanPayrollStartDate").value = dateInput(x.PayrollStartDate); }
        if (byId("epPlanSuspensionDate")) { byId("epPlanSuspensionDate").value = dateInput(x.SuspensionDate); }
        if (byId("epPlanCancellationDate")) { byId("epPlanCancellationDate").value = dateInput(x.CancellationDate); }
        if (byId("epPlanCostCenter")) { byId("epPlanCostCenter").value = x.CostCenterCode || ""; }
        if (byId("epPlanPayrollDeductionType")) { byId("epPlanPayrollDeductionType").value = x.PayrollDeductionType || "Fixed"; }
        if (byId("epPlanIsMonthlyDeduction")) { byId("epPlanIsMonthlyDeduction").checked = x.IsMonthlyDeduction !== false; }
        if (byId("epPlanAutoStop")) { byId("epPlanAutoStop").checked = x.AutoStopAtEndDate !== false; }
        if (byId("epPlanShowInPayroll")) { byId("epPlanShowInPayroll").checked = x.ShowInPayroll !== false; }
        if (byId("epPlanDistributeDepartments")) { byId("epPlanDistributeDepartments").checked = !!x.DistributeByDepartment; }
        if (byId("epPlanDistributeCostCenter")) { byId("epPlanDistributeCostCenter").checked = !!x.DistributeByCostCenter; }
        if (byId("epPlanTaxMode")) { byId("epPlanTaxMode").value = x.TaxMode || "AfterTax"; }
        if (byId("epMaxDependents")) { byId("epMaxDependents").value = x.MaxDependents || 4; }
        if (byId("epChildrenMaxAge")) { byId("epChildrenMaxAge").value = x.ChildrenMaxAge || 21; }
        if (byId("epSpouseCost")) { byId("epSpouseCost").value = x.SpouseAdditionalCost || 0; }
        if (byId("epChildCost")) { byId("epChildCost").value = x.ChildAdditionalCost || 0; }
        if (byId("epParentCost")) { byId("epParentCost").value = x.ParentAdditionalCost || 0; }
        if (byId("epDefaultCoveragePercent")) { byId("epDefaultCoveragePercent").value = x.DefaultCoveragePercent || 100; }
        if (byId("epAutoEnrollAfterDays")) { byId("epAutoEnrollAfterDays").value = x.AutoEnrollAfterDays || 30; }
        if (byId("epAutoEnrollCriteria")) { byId("epAutoEnrollCriteria").value = x.AutoEnrollCriteria || ""; }
        try { enterpriseRules = x.RulesJson ? JSON.parse(x.RulesJson) : []; } catch (ignoreRules) { enterpriseRules = []; }
        try { enterpriseDependents = x.DependentsTemplateJson ? JSON.parse(x.DependentsTemplateJson) : []; } catch (ignoreDeps) { enterpriseDependents = []; }
        byId("epPlanActive").checked = x.IsActive !== false;
        byId("epPlanNotes").value = x.Notes || "";
        renderRules();
        renderDependents();
        updateLifecycleBadge();
        calculatePlanPreview();
    }
    function loadInsuranceSettings() {
        return Promise.all([
            getJson(root.getAttribute("data-providers-url")),
            getJson(root.getAttribute("data-plans-url"))
        ]).then(function (all) {
            var providers = all[0].rows || [];
            var plans = all[1].rows || [];
            fillSelectFromRows(byId("epPlanProvider"), providers, "ProviderId", "ProviderNameAr", "اختر الشركة");
            byId("epProvidersRows").innerHTML = "";
            providers.forEach(function (x) {
                byId("epProvidersRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.ProviderNameAr) + '</td><td>' + html(x.Phone) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></td><td><button type="button" title="تعديل" data-provider="' + x.ProviderId + '"><i class="fas fa-edit"></i></button></td></tr>');
            });
            byId("epPlansRows").innerHTML = "";
            plans.forEach(function (x) {
                byId("epPlansRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.PlanNameAr) + '</td><td>' + html(x.ProviderName) + '</td><td>' + money(x.DefaultMonthlyCost) + '</td><td>' + html(shareTypeLabel(x.DefaultEmployeeShareType)) + " " + money(x.DefaultEmployeeShareValue) + '</td><td>' + html(shareTypeLabel(x.DefaultCompanyShareType)) + " " + money(x.DefaultCompanyShareValue) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></td><td><button type="button" title="تعديل" data-plan="' + x.PlanId + '"><i class="fas fa-edit"></i></button></td></tr>');
            });
            root._providers = providers;
            root._plans = plans;
            renderMasterCards(providers, plans);
        });
    }

    function loadInsuranceSettings() {
        return Promise.all([
            getJson(root.getAttribute("data-providers-url")),
            getJson(root.getAttribute("data-plans-url"))
        ]).then(function (all) {
            var providers = all[0].rows || [];
            var plans = all[1].rows || [];
            fillSelectFromRows(byId("epPlanProvider"), providers, "ProviderId", "ProviderNameAr", "اختر الشركة");
            setText("epStatProviders", providers.length);
            setText("epStatActivePlans", plans.filter(function (x) { return x.IsActive; }).length);
            byId("epProvidersRows").innerHTML = "";
            providers.forEach(function (x) {
                byId("epProvidersRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.ProviderNameAr) + '</td><td>' + html(x.Phone) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></td><td><button type="button" title="تعديل" data-provider="' + x.ProviderId + '"><i class="fas fa-edit"></i></button></td></tr>');
            });
            byId("epPlansRows").innerHTML = "";
            plans.forEach(function (x) {
                var lifecycle = x.LifecycleStatus || (x.IsActive ? "Active" : "Draft");
                byId("epPlansRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.PlanNameAr) + '</td><td>' + html(x.ProviderName) + '</td><td>' + formatMoney(x.DefaultMonthlyCost) + '</td><td>' + html(shareTypeLabel(x.DefaultEmployeeShareType)) + " " + money(x.DefaultEmployeeShareValue) + '</td><td>' + html(shareTypeLabel(x.DefaultCompanyShareType)) + " " + money(x.DefaultCompanyShareValue) + '</td><td><span class="ep-status-badge ' + statusClass(lifecycle) + '">' + html(lifecycle) + '</span></td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></td><td><button type="button" title="تعديل" data-plan="' + x.PlanId + '"><i class="fas fa-edit"></i></button></td></tr>');
            });
            root._providers = providers;
            root._plans = plans;
            renderMasterCards(providers, plans);
            renderRules();
            renderDependents();
            updateLifecycleBadge();
            calculatePlanPreview();
            return loadCoverageAnalytics();
        });
    }

    function shareTypeLabel(value) {
        if (value === "Percent") { return "نسبة"; }
        if (value === "AutoBalance") { return "الباقي"; }
        return "مبلغ";
    }

    function medStatusLabel(value) {
        if (value === "Renewal Due") { return "تجديد قريب"; }
        if (value === "Suspended") { return "موقوف"; }
        if (value === "Expired") { return "منتهي"; }
        return "نشط";
    }
    function medStatusClass(value) {
        if (value === "Renewal Due") { return "renewal"; }
        if (value === "Suspended") { return "suspended"; }
        if (value === "Expired") { return "expired"; }
        return "active";
    }
    function medOperationalFilter() {
        return {
            Term: byId("epMedTerm") ? byId("epMedTerm").value : "",
            BranchId: byId("epMedBranch") && byId("epMedBranch").value ? byId("epMedBranch").value : null,
            DepartmentId: byId("epMedDepartment") && byId("epMedDepartment").value ? byId("epMedDepartment").value : null,
            Status: byId("epMedStatus") ? byId("epMedStatus").value : "",
            RenewalDays: 45
        };
    }
    function renderMedAccounting(rows) {
        var host = byId("epMedAccountingPreview");
        if (!host) { return; }
        host.innerHTML = "";
        (rows || []).forEach(function (x) {
            host.insertAdjacentHTML("beforeend",
                '<article class="ep-med-journal-line"><div><span>' + html(x.Step) + '</span><strong>' + formatMoney(x.Amount) + '</strong></div><dl><div><dt>Debit</dt><dd>' + html(x.DebitAccount) + '</dd></div><div><dt>Credit</dt><dd>' + html(x.CreditAccount) + '</dd></div></dl><p>' + html(x.Explanation) + '</p></article>');
        });
        if (!host.innerHTML) {
            host.innerHTML = '<div class="ep-empty-card">سيظهر نموذج القيد بعد تحميل بيانات التأمين.</div>';
        }
    }
    function renderMedMembershipCard(row) {
        var host = byId("epMedMembershipCard");
        var depHost = byId("epMedDependents");
        if (!host) { return; }
        if (!row) {
            host.innerHTML = '<div class="ep-empty-card">اختر موظفا لعرض بطاقة التأمين وبيانات الاشتراك.</div>';
            if (depHost) { depHost.innerHTML = ""; }
            return;
        }
        var status = medStatusClass(row.Status);
        var mode = root.getAttribute("data-card-mode") || "full";
        host.classList.toggle("wallet", mode === "wallet");
        host.innerHTML =
            '<article class="ep-med-id-card ' + status + '">' +
            '<div class="ep-med-id-bg"></div>' +
            '<div class="ep-med-id-top"><div class="ep-med-brand"><span>Dania</span><strong>Medical Insurance</strong></div><div class="ep-med-provider-logo">' + html((row.ProviderName || "MI").substring(0, 2)) + '</div></div>' +
            '<div class="ep-med-id-main"><div class="ep-med-avatar">' + html(row.AvatarText || "MI") + '</div><div><h3>' + html(row.EmployeeName || "Employee") + '</h3><p>' + html(row.EmployeeCode || "") + ' - ' + html(row.MembershipNumber || "") + '</p><b class="ep-med-badge ' + status + '">' + medStatusLabel(row.Status) + '</b></div></div>' +
            '<div class="ep-med-id-plan"><span>' + html(row.ProviderName || "Provider") + '</span><strong>' + html(row.PlanName || "Plan") + '</strong></div>' +
            '<dl class="ep-med-id-metrics"><div><dt>Renewal</dt><dd>' + html(dateInput(row.RenewalDate || row.EndDate) || "Open") + '</dd></div><div><dt>Payroll</dt><dd>' + (row.PayrollLinked ? "Linked" : "Review") + '</dd></div><div><dt>Family</dt><dd>' + html(row.DependentsCount || 0) + '</dd></div></dl>' +
            '<div class="ep-med-id-footer"><span>Coverage: employee ' + formatMoney(row.EmployeeMonthlyDeduction) + ' / company ' + formatMoney(row.CompanyMonthlyCost) + '</span><em>QR</em></div>' +
            '</article>';
        if (depHost) {
            depHost.innerHTML = "";
            (row.Dependents || []).forEach(function (x) {
                depHost.insertAdjacentHTML("beforeend",
                    '<article class="ep-med-dependent-card"><i class="fas fa-user"></i><div><strong>' + html(x.Name || "Dependent") + '</strong><span>' + html(x.Relation || "Family") + ' - ' + html(x.Age || 0) + ' سنة - ' + formatMoney(x.CoveragePercent).replace(".00", "") + '%</span></div><b>' + (x.IsActive ? "نشط" : "غير نشط") + '</b></article>');
            });
            if (!depHost.innerHTML) { depHost.innerHTML = '<div class="ep-empty-card">لا توجد بيانات تابعين لهذا الموظف.</div>'; }
        }
    }
    function renderMedBars(hostId, rows) {
        var host = byId(hostId);
        if (!host) { return; }
        host.innerHTML = "";
        var max = Math.max.apply(Math, (rows || []).map(function (x) { return number(x.TotalCost); }).concat([1]));
        (rows || []).forEach(function (x) {
            var width = Math.max(4, Math.round(number(x.TotalCost) * 100 / max));
            host.insertAdjacentHTML("beforeend",
                '<div class="ep-med-bar-row"><div><strong>' + html(x.Name || "غير محدد") + '</strong><span>' + html(x.Employees || 0) + ' موظف - ' + formatMoney(x.TotalCost) + '</span></div><em><i style="width:' + width + '%"></i></em></div>');
        });
        if (!host.innerHTML) { host.innerHTML = '<div class="ep-empty-card">No cost distribution yet.</div>'; }
    }
    function renderMedAlerts(rows) {
        var host = byId("epMedAlerts");
        if (!host) { return; }
        host.innerHTML = "";
        (rows || []).forEach(function (x) {
            var sev = String(x.Severity || "Info").toLowerCase();
            host.insertAdjacentHTML("beforeend",
                '<article class="ep-med-alert ' + sev + '"><i class="fas fa-bell"></i><div><strong>' + html(x.Title) + '</strong><p>' + html(x.Description) + '</p><span>' + html(x.EmployeeName || "") + ' - ' + html(x.BranchName || "") + (x.DueDate ? ' - ' + html(dateInput(x.DueDate)) : '') + '</span></div></article>');
        });
        if (!host.innerHTML) { host.innerHTML = '<div class="ep-empty-card">لا توجد تنبيهات مهمة في الفترة الحالية.</div>'; }
    }
    function renderMedEmployees(rows) {
        var host = byId("epMedEmployees");
        if (!host) { return; }
        host.innerHTML = "";
        root._medRows = rows || [];
        (rows || []).forEach(function (x, index) {
            var status = medStatusClass(x.Status);
            var flags = [];
            if (x.PayrollLinked) { flags.push('<span><i class="fas fa-link"></i> Payroll-linked</span>'); }
            if (x.DependentsCount) { flags.push('<span><i class="fas fa-users"></i> ' + html(x.DependentsCount) + ' family</span>'); }
            if (x.OverdueInstallments) { flags.push('<span class="danger"><i class="fas fa-bell"></i> ' + html(x.OverdueInstallments) + ' overdue</span>'); }
            host.insertAdjacentHTML("beforeend",
                '<article class="ep-med-employee-card ' + status + (index === 0 ? ' selected' : '') + '" data-med-employee="' + index + '">' +
                '<div class="ep-med-employee-head"><div><span>' + html(x.EmployeeCode || "") + '</span><h3>' + html(x.EmployeeName || "Employee") + '</h3></div><b class="ep-med-badge ' + status + '">' + medStatusLabel(x.Status) + '</b></div>' +
                '<div class="ep-med-provider"><i class="fas fa-hospital"></i><div><strong>' + html(x.ProviderName || "غير محدد") + '</strong><span>' + html(x.PlanName || "خطة غير محددة") + '</span></div></div>' +
                '<dl class="ep-med-card-metrics"><div><dt>Employee</dt><dd>' + formatMoney(x.EmployeeMonthlyDeduction) + '</dd></div><div><dt>Company</dt><dd>' + formatMoney(x.CompanyMonthlyCost) + '</dd></div><div><dt>Overdue</dt><dd>' + formatMoney(x.OverdueAmount) + '</dd></div></dl>' +
                '<p class="ep-med-card-meta">' + html(x.BranchName || "فرع غير محدد") + ' / ' + html(x.DepartmentName || "إدارة غير محددة") + '</p>' +
                '<div class="ep-med-flags">' + flags.join("") + '</div>' +
                '</article>');
        });
        if (!host.innerHTML) {
            host.innerHTML = '<div class="ep-empty-card">لا توجد اشتراكات مطابقة. جرّب تعديل الفلاتر أو تحميل البيانات.</div>';
        }
        renderMedMembershipCard((rows || [])[0]);
    }
    function renderMedDashboard(dashboard) {
        dashboard = dashboard || {};
        setText("epMedActive", dashboard.ActiveInsured || 0);
        setText("epMedUninsured", dashboard.UninsuredEmployees || 0);
        setText("epMedEmployeeShare", formatMoney(dashboard.MonthlyEmployeeShare));
        setText("epMedCompanyShare", formatMoney(dashboard.MonthlyCompanyShare));
        setText("epMedRenewals", dashboard.UpcomingRenewals || 0);
        setText("epMedOverdue", dashboard.OverdueInstallments || 0);
        setText("epMedInactive", (dashboard.Suspended || 0) + (dashboard.Expired || 0));
        setText("epMedPayable", formatMoney(dashboard.MonthlyPayable));
        var total = Math.max(1, dashboard.TotalEmployees || 0);
        var insured = dashboard.ActiveInsured || 0;
        var coverage = Math.round(insured * 100 / total);
        setText("epMedCoveragePercent", coverage + "%");
        var donut = byId("epMedDonut");
        if (donut) {
            donut.style.setProperty("--active", String(Math.max(0, dashboard.ActiveInsured - dashboard.UpcomingRenewals)));
            donut.style.setProperty("--renewal", String(dashboard.UpcomingRenewals || 0));
            donut.style.setProperty("--expired", String(dashboard.Expired || 0));
            donut.style.setProperty("--suspended", String(dashboard.Suspended || 0));
        }
        renderMedEmployees(dashboard.Employees || []);
        renderMedAccounting(dashboard.AccountingPreview || []);
        renderMedBars("epMedBranchCosts", dashboard.BranchCosts || []);
        renderMedBars("epMedDepartmentCosts", dashboard.DepartmentCosts || []);
        renderMedAlerts(dashboard.Alerts || []);
        if (!dashboard.SchemaReady) {
            message(dashboard.Message || "Medical insurance setup is not installed in this database.", true);
        } else {
            message("تم تحميل بيانات التأمين الطبي بنجاح.");
        }
    }

    function loadMedOperationalDashboard() {
        var url = root.getAttribute("data-dashboard-url");
        if (!url) { return Promise.resolve(); }
        message("جاري تحميل مؤشرات التأمين...");
        return getJson(url + "?" + queryString(medOperationalFilter())).then(function (res) {
            if (!res.success) { message(res.message || "تعذر تحميل التأمين الطبي.", true); return; }
            renderMedDashboard(res.dashboard);
        });
    }

    function reportFilter() {
        return {
            PeriodFrom: byId("epReportFrom").value || null,
            PeriodTo: byId("epReportTo").value || null,
            ProviderId: byId("epReportProvider").value || null,
            PlanId: byId("epReportPlan").value || null,
            ActiveOnly: byId("epReportActiveOnly").checked
        };
    }
    function loadReports() {
        var q = queryString(reportFilter());
        return Promise.all([
            getJson(root.getAttribute("data-subscriptions-url") + "?" + q),
            getJson(root.getAttribute("data-deductions-url") + "?" + q)
        ]).then(function (all) {
            var subs = all[0].rows || [];
            var deductions = all[1].rows || [];
            var empTotal = 0;
            var companyTotal = 0;
            byId("epSubscriptionsReportRows").innerHTML = "";
            subs.forEach(function (x) {
                empTotal += number(x.EmployeeMonthlyDeduction);
                companyTotal += number(x.CompanyMonthlyCost);
                byId("epSubscriptionsReportRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.ProviderName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html(dateInput(x.StartDate)) + '</td><td>' + html(dateInput(x.EndDate)) + '</td><td>' + money(x.MonthlyCost) + '</td><td>' + money(x.EmployeeMonthlyDeduction) + '</td><td>' + money(x.CompanyMonthlyCost) + '</td><td>' + (x.IsActive ? "نشط" : "متوقف") + '</td></tr>');
            });
            byId("epDeductionsReportRows").innerHTML = "";
            deductions.forEach(function (x) {
                byId("epDeductionsReportRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html(dateInput(x.PeriodFrom)) + '</td><td>' + html(dateInput(x.PeriodTo)) + '</td><td>' + money(x.EmployeeDeduction) + '</td><td>' + money(x.CompanyCost) + '</td></tr>');
            });
            byId("epReportSubscriptionCount").textContent = subs.length;
            byId("epReportEmployeeTotal").textContent = money(empTotal);
            byId("epReportCompanyTotal").textContent = money(companyTotal);
            message("تم تحميل التقارير");
        });
    }

    if (readOnly && byId("epEmployeesRows") && window.MutationObserver) {
        new MutationObserver(enforceReadOnlyRows).observe(byId("epEmployeesRows"), { childList: true, subtree: true });
    }

    loadLookups().then(function () {
        if (screen === "employees") { loadEmployees(); }
        if (screen === "insurance-settings") { loadInsuranceSettings(); }
        if (screen === "insurance-reports") { loadReports(); }
        if (screen === "insurance-operational") { loadMedOperationalDashboard(); }
    });

    root.addEventListener("click", function (e) {
        if (screen === "insurance-settings" && e.target && e.target.id === "epDrawerBackdrop") {
            closeDrawers();
            return;
        }
        var masterCard = e.target.closest(".ep-provider-mini-card[data-provider], .ep-plan-master-card[data-plan]");
        if (screen === "insurance-settings" && masterCard && !e.target.closest("button")) {
            if (masterCard.hasAttribute("data-provider")) {
                var cardProvider = (root._providers || []).filter(function (x) { return String(x.ProviderId) === masterCard.getAttribute("data-provider"); })[0];
                if (cardProvider) { fillProviderForm(cardProvider); openDrawer("epProviderDrawer"); }
            }
            if (masterCard.hasAttribute("data-plan")) {
                var cardPlan = (root._plans || []).filter(function (x) { return String(x.PlanId) === masterCard.getAttribute("data-plan"); })[0];
                if (cardPlan) { fillPlanForm(cardPlan); openDrawer("epPlanDrawer"); }
            }
            return;
        }
        var btn = e.target.closest("button");
        var medCard = e.target.closest("[data-med-employee]");
        if (screen === "insurance-operational" && medCard) {
            var index = parseInt(medCard.getAttribute("data-med-employee"), 10);
            root.querySelectorAll("[data-med-employee]").forEach(function (x) { x.classList.toggle("selected", x === medCard); });
            renderMedMembershipCard((root._medRows || [])[index]);
            return;
        }
        if (!btn) { return; }
        if (screen === "insurance-settings" && btn.hasAttribute("data-close-drawer")) { closeDrawers(); return; }
        if (screen === "insurance-settings" && btn.id === "epOpenProviderDrawer") { fillProviderForm({ IsActive: true }); openDrawer("epProviderDrawer"); return; }
        if (screen === "insurance-settings" && btn.id === "epOpenProviderDrawer2") { fillProviderForm({ IsActive: true }); openDrawer("epProviderDrawer"); return; }
        if (screen === "insurance-settings" && btn.id === "epOpenPlanDrawer") { resetPlanForm(); openDrawer("epPlanDrawer"); return; }
        if (screen === "insurance-settings" && btn.id === "epOpenPlanDrawer2") { resetPlanForm(); openDrawer("epPlanDrawer"); return; }
        if (screen === "insurance-settings" && btn.hasAttribute("data-drawer-tab")) {
            var drawerTab = btn.getAttribute("data-drawer-tab");
            var drawer = btn.closest(".ep-side-drawer");
            if (drawer) {
                drawer.querySelectorAll("[data-drawer-tab]").forEach(function (x) { x.classList.toggle("active", x === btn); });
                drawer.querySelectorAll("[data-drawer-panel]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-drawer-panel") === drawerTab); });
            }
            return;
        }
        if (btn.id === "epSearchBtn") { loadEmployees(); }
        if (btn.id === "epNewEmployee") {
            if (readOnly) { message("POS operational view only. Manage employees from MainErp.", true); return; }
            openEditor();
        }
        if (btn.id === "epCloseEditor") { byId("epEmployeeEditor").hidden = true; }
        if (btn.id === "epPreviewRun") { previewSalary(); }
        if (btn.id === "epSaveRun") {
            if (readOnly || !root.getAttribute("data-save-url")) { message("Salary run saving is protected in POS. Use MainErp for administration.", true); return; }
            postJson(root.getAttribute("data-save-url"), salaryRequest()).then(function (res) {
                message(res.success ? res.result.Message : res.message, !res.success);
                previewSalary();
            });
        }
        if (btn.id === "epLoadReports") { loadReports(); }
        if (btn.id === "epMedRefresh" || btn.id === "epMedSearch") { loadMedOperationalDashboard(); }
        if (btn.id === "epMedPrintCard") { window.print(); }
        if (screen === "insurance-operational" && btn.hasAttribute("data-card-mode")) {
            root.setAttribute("data-card-mode", btn.getAttribute("data-card-mode"));
            root.querySelectorAll("[data-card-mode]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            var selectedCard = root.querySelector("[data-med-employee].selected");
            var selectedIndex = selectedCard ? parseInt(selectedCard.getAttribute("data-med-employee"), 10) : 0;
            renderMedMembershipCard((root._medRows || [])[selectedIndex]);
            return;
        }
        if (btn.id === "epSaveDraftPlan") {
            if (byId("epPlanLifecycleStatus")) { byId("epPlanLifecycleStatus").value = "Draft"; }
            updateHeroSummary();
            byId("epPlanForm").dispatchEvent(new Event("submit", { cancelable: true }));
        }
        if (btn.id === "epActivatePlan") {
            if (byId("epPlanLifecycleStatus")) { byId("epPlanLifecycleStatus").value = "Active"; }
            if (byId("epPlanActive")) { byId("epPlanActive").checked = true; }
            updateHeroSummary();
            byId("epPlanForm").dispatchEvent(new Event("submit", { cancelable: true }));
        }
        if (btn.id === "epCancelPlan") {
            byId("epPlanForm").reset();
            byId("epPlanId").value = "";
            enterpriseDependents = [];
            enterpriseRules = [];
            renderDependents();
            renderRules();
            updateHeroSummary();
            message("تم إلغاء التعديلات غير المحفوظة");
        }
        if (btn.id === "epPrintPlan") { window.print(); }
        if (btn.id === "epExportPlan") { message("يمكن استخدام تقارير التأمين للتصدير التفصيلي."); }
        if (btn.id === "epAddDependent") {
            var depName = byId("epDependentName").value;
            var relation = byId("epDependentRelation").value;
            var birthDate = byId("epDependentBirthDate").value;
            var maxAge = parseInt(byId("epChildrenMaxAge").value || "0", 10);
            if (!depName) { message("ادخل اسم التابع أولا", true); return; }
            if (relation === "Child" && maxAge > 0 && ageFromDate(birthDate) > maxAge) { message("عمر الابن/الابنة يتجاوز حد الخطة", true); return; }
            enterpriseDependents.push({ Name: depName, Relation: relation, BirthDate: birthDate, CoveragePercent: number(byId("epDefaultCoveragePercent").value) || 100 });
            byId("epDependentName").value = "";
            byId("epDependentBirthDate").value = "";
            renderDependents();
            calculatePlanPreview();
        }
        if (btn.hasAttribute("data-remove-dependent")) {
            enterpriseDependents.splice(parseInt(btn.getAttribute("data-remove-dependent"), 10), 1);
            renderDependents();
        }
        if (btn.hasAttribute("data-insurance-section")) {
            root.querySelectorAll("[data-insurance-section]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            root.querySelectorAll("[data-insurance-panel]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-insurance-panel") === btn.getAttribute("data-insurance-section")); });
        }
        if (btn.hasAttribute("data-insurance-tab")) {
            var tabName = btn.getAttribute("data-insurance-tab");
            root.querySelectorAll("[data-insurance-tab]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            root.querySelectorAll("[data-insurance-tab-panel]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-insurance-tab-panel") === tabName); });
            if (root.scrollIntoView) { root.scrollIntoView({ behavior: "smooth", block: "start" }); }
        }
        if (btn.hasAttribute("data-workflow-step")) {
            root.querySelectorAll("[data-workflow-step]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            var panel = root.querySelector('[data-workflow-panel="' + btn.getAttribute("data-workflow-step") + '"]');
            if (panel && panel.scrollIntoView) { panel.scrollIntoView({ behavior: "smooth", block: "start" }); }
        }
        if (btn.hasAttribute("data-rule-template")) {
            var template = btn.getAttribute("data-rule-template");
            if (template === "Managers100") { addRule("JobGrade", "Managers", 100, "الشركة تتحمل كامل تكلفة المديرين."); }
            if (template === "Department80") { addRule("Department", "Administration", 80, "الشركة تتحمل 80% لموظفي الإدارة."); }
            if (template === "NewHire50") { addRule("Hiring", "First 90 days", 50, "الموظفون الجدد يتحملون 50% حتى انتهاء فترة التجربة."); }
        }
        if (btn.hasAttribute("data-edit")) {
            getJson(root.getAttribute("data-get-url") + "?id=" + encodeURIComponent(btn.getAttribute("data-edit"))).then(function (res) { openEditor(res.employee); });
        }
        if (btn.hasAttribute("data-active")) {
            if (readOnly || !root.getAttribute("data-active-url")) { message("Employee status changes are managed from MainErp.", true); return; }
            postJson(root.getAttribute("data-active-url"), { id: btn.getAttribute("data-active"), active: btn.getAttribute("data-state") === "true" }).then(loadEmployees);
        }
        if (btn.hasAttribute("data-tab")) {
            root.querySelectorAll("[data-tab]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            root.querySelectorAll("[data-tab-panel]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-tab-panel") === btn.getAttribute("data-tab")); });
        }
        if (btn.hasAttribute("data-provider")) {
            var provider = (root._providers || []).filter(function (x) { return String(x.ProviderId) === btn.getAttribute("data-provider"); })[0];
            if (provider) { fillProviderForm(provider); openDrawer("epProviderDrawer"); }
        }
        if (btn.hasAttribute("data-plan")) {
            var plan = (root._plans || []).filter(function (x) { return String(x.PlanId) === btn.getAttribute("data-plan"); })[0];
            if (plan) { fillPlanForm(plan); openDrawer("epPlanDrawer"); }
        }
    });

    root.addEventListener("change", function (e) {
        if (e.target && e.target.id === "epInsurancePlan" && e.target.value) {
            getJson(root.getAttribute("data-plan-defaults-url") + "?id=" + encodeURIComponent(e.target.value)).then(function (res) {
                var p = res.plan || {};
                byId("epInsuranceMonthlyCost").value = p.DefaultMonthlyCost || 0;
                byId("epEmployeeShareType").value = p.DefaultEmployeeShareType || "Amount";
                byId("epEmployeeShareValue").value = p.DefaultEmployeeShareValue || 0;
                byId("epCompanyShareType").value = p.DefaultCompanyShareType || "AutoBalance";
                byId("epCompanyShareValue").value = p.DefaultCompanyShareValue || 0;
                updateInsurancePreview();
            });
        }
        if (e.target && e.target.closest("[data-tab-panel='insurance']")) { updateInsurancePreview(); }
        if (screen === "insurance-settings" && e.target && e.target.closest("#epPlanForm")) {
            updateLifecycleBadge();
            calculatePlanPreview();
        }
        if (screen === "insurance-settings" && e.target && e.target.id === "epCoverageStatus") {
            renderCoverageRows(enterpriseCoverageRows);
        }
        if (screen === "insurance-settings" && e.target && e.target.id === "epMasterStatus") {
            renderMasterCards(root._providers || [], root._plans || []);
        }
        if (screen === "insurance-operational" && e.target && (e.target.id === "epMedBranch" || e.target.id === "epMedDepartment" || e.target.id === "epMedStatus")) {
            loadMedOperationalDashboard();
        }
    });
    root.addEventListener("input", function (e) {
        if (e.target && e.target.closest("[data-tab-panel='insurance']")) { updateInsurancePreview(); }
        if (screen === "insurance-settings" && e.target && e.target.closest("#epPlanForm")) {
            calculatePlanPreview();
            renderDependents();
        }
        if (screen === "insurance-settings" && e.target && e.target.id === "epCoverageSearch") {
            renderCoverageRows(enterpriseCoverageRows);
        }
        if (screen === "insurance-settings" && e.target && e.target.id === "epMasterSearch") {
            renderMasterCards(root._providers || [], root._plans || []);
        }
    });

    if (screen === "employees") {
        byId("epEmployeeForm").addEventListener("submit", function (e) {
            e.preventDefault();
            if (readOnly || !root.getAttribute("data-save-url")) { message("POS operational view only. Manage employee data from MainErp.", true); return; }
            postJson(root.getAttribute("data-save-url"), collectEmployee()).then(function (res) {
                message(res.message || (res.success ? "تم الحفظ" : "تعذر الحفظ"), !res.success);
                if (res.success) {
                    byId("epEmployeeEditor").hidden = true;
                    loadEmployees();
                }
            });
        });
        byId("epSearchTerm").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); loadEmployees(); } });
    }

    if (screen === "insurance-settings") {
        byId("epProviderForm").addEventListener("submit", function (e) {
            e.preventDefault();
            postJson(root.getAttribute("data-save-provider-url"), collectProvider()).then(function (res) {
                message(res.message || "تم الحفظ", !res.success);
                if (res.success) { byId("epProviderForm").reset(); byId("epProviderId").value = ""; closeDrawers(); loadInsuranceSettings(); }
            });
        });
        byId("epPlanForm").addEventListener("submit", function (e) {
            e.preventDefault();
            if (!validateEnterpriseInsurance()) { message("راجع تنبيهات الخطة قبل الحفظ", true); return; }
            postJson(root.getAttribute("data-save-plan-url"), collectPlan()).then(function (res) {
                message(res.message || "تم الحفظ", !res.success);
                if (res.success) {
                    byId("epPlanForm").reset();
                    byId("epPlanId").value = "";
                    enterpriseDependents = [];
                    enterpriseRules = [];
                    renderDependents();
                    renderRules();
                    updateHeroSummary();
                    closeDrawers();
                    loadInsuranceSettings();
                }
            });
        });
    }
    if (screen === "insurance-operational" && byId("epMedTerm")) {
        byId("epMedTerm").addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                loadMedOperationalDashboard();
            }
        });
    }
})();

