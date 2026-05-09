(function () {
    "use strict";
    var root = document.querySelector(".ep-page");
    if (!root) { return; }
    var screen = root.getAttribute("data-screen");

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
        return fetch(url, { credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function postJson(url, data) {
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data || {})
        }).then(function (r) { return r.json(); });
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
            ["epBranchFilter", "epBranch", "epRunBranch"].forEach(function (id) { fillSelect(byId(id), data.Branches, id === "epBranch" ? "غير محدد" : "كل الفروع"); });
            ["epDepartmentFilter", "epDepartment", "epRunDepartment"].forEach(function (id) { fillSelect(byId(id), data.Departments, id === "epDepartment" ? "غير محدد" : "كل الإدارات"); });
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
        byId("epPlanActive").checked = x.IsActive !== false;
        byId("epPlanNotes").value = x.Notes || "";
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
        });
    }

    function shareTypeLabel(value) {
        if (value === "Percent") { return "نسبة"; }
        if (value === "AutoBalance") { return "الباقي"; }
        return "مبلغ";
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

    loadLookups().then(function () {
        if (screen === "employees") { loadEmployees(); }
        if (screen === "insurance-settings") { loadInsuranceSettings(); }
        if (screen === "insurance-reports") { loadReports(); }
    });

    root.addEventListener("click", function (e) {
        var btn = e.target.closest("button");
        if (!btn) { return; }
        if (btn.id === "epSearchBtn") { loadEmployees(); }
        if (btn.id === "epNewEmployee") { openEditor(); }
        if (btn.id === "epCloseEditor") { byId("epEmployeeEditor").hidden = true; }
        if (btn.id === "epPreviewRun") { previewSalary(); }
        if (btn.id === "epSaveRun") {
            postJson(root.getAttribute("data-save-url"), salaryRequest()).then(function (res) {
                message(res.success ? res.result.Message : res.message, !res.success);
                previewSalary();
            });
        }
        if (btn.id === "epLoadReports") { loadReports(); }
        if (btn.hasAttribute("data-edit")) {
            getJson(root.getAttribute("data-get-url") + "?id=" + encodeURIComponent(btn.getAttribute("data-edit"))).then(function (res) { openEditor(res.employee); });
        }
        if (btn.hasAttribute("data-active")) {
            postJson(root.getAttribute("data-active-url"), { id: btn.getAttribute("data-active"), active: btn.getAttribute("data-state") === "true" }).then(loadEmployees);
        }
        if (btn.hasAttribute("data-tab")) {
            root.querySelectorAll("[data-tab]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            root.querySelectorAll("[data-tab-panel]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-tab-panel") === btn.getAttribute("data-tab")); });
        }
        if (btn.hasAttribute("data-provider")) {
            var provider = (root._providers || []).filter(function (x) { return String(x.ProviderId) === btn.getAttribute("data-provider"); })[0];
            if (provider) { fillProviderForm(provider); }
        }
        if (btn.hasAttribute("data-plan")) {
            var plan = (root._plans || []).filter(function (x) { return String(x.PlanId) === btn.getAttribute("data-plan"); })[0];
            if (plan) { fillPlanForm(plan); }
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
    });
    root.addEventListener("input", function (e) {
        if (e.target && e.target.closest("[data-tab-panel='insurance']")) { updateInsurancePreview(); }
    });

    if (screen === "employees") {
        byId("epEmployeeForm").addEventListener("submit", function (e) {
            e.preventDefault();
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
                if (res.success) { byId("epProviderForm").reset(); byId("epProviderId").value = ""; loadInsuranceSettings(); }
            });
        });
        byId("epPlanForm").addEventListener("submit", function (e) {
            e.preventDefault();
            postJson(root.getAttribute("data-save-plan-url"), collectPlan()).then(function (res) {
                message(res.message || "تم الحفظ", !res.success);
                if (res.success) { byId("epPlanForm").reset(); byId("epPlanId").value = ""; loadInsuranceSettings(); }
            });
        });
    }
})();
