(function () {
    "use strict";
    var root = document.querySelector(".ep-page");
    if (!root) { return; }
    var screen = root.getAttribute("data-screen");
    var readOnly = root.getAttribute("data-read-only") === "true";
    var enterpriseDependents = [];
    var employeeInsuranceDependents = [];
    var enterpriseRules = [];
    var enterpriseCoverageRows = [];
    var actionLocks = {};
    var currentEmployee = null;

    function byId(id) { return document.getElementById(id); }
    function money(v) { var n = parseFloat(v || 0); return isNaN(n) ? "0.00" : n.toFixed(2); }
    function number(v) { var n = parseFloat(v || 0); return isNaN(n) ? 0 : n; }
    function html(v) { return String(v == null ? "" : v).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;"); }
    function initials(name) {
        name = String(name || "").trim();
        if (!name) { return "HR"; }
        return name.split(/\s+/).slice(0, 2).map(function (x) { return x.charAt(0); }).join("").toUpperCase();
    }
    function photoMarkup(dataUrl, name, cssClass) {
        if (dataUrl) {
            return '<img class="' + html(cssClass || "") + '" src="' + html(dataUrl) + '" alt="' + html(name || "Employee") + '" />';
        }
        return '<span class="' + html(cssClass || "") + '">' + html(initials(name)) + '</span>';
    }
    function setEmployeePhoto(dataUrl, name) {
        var input = byId("epPhotoDataUrl");
        var preview = byId("epPhotoPreview");
        if (input) { input.value = dataUrl || ""; }
        if (preview) { preview.innerHTML = dataUrl ? '<img src="' + html(dataUrl) + '" alt="' + html(name || "") + '" />' : '<i class="fas fa-user"></i>'; }
        if (currentEmployee) {
            currentEmployee.PhotoDataUrl = dataUrl || "";
        }
    }
    function resizeEmployeePhoto(file) {
        return new Promise(function (resolve, reject) {
            if (!file || !/^image\/(png|jpeg|jpg|webp)$/i.test(file.type || "")) {
                reject(new Error("صيغة الصورة غير مدعومة. استخدم PNG أو JPG أو WebP."));
                return;
            }
            if (file.size > 4 * 1024 * 1024) {
                reject(new Error("حجم الصورة كبير. اختر صورة أقل من 4 ميجابايت."));
                return;
            }
            var reader = new FileReader();
            reader.onerror = function () { reject(new Error("تعذر قراءة الصورة.")); };
            reader.onload = function () {
                var image = new Image();
                image.onerror = function () { reject(new Error("تعذر تجهيز الصورة للطباعة.")); };
                image.onload = function () {
                    var max = 360;
                    var scale = Math.min(1, max / Math.max(image.width, image.height));
                    var canvas = document.createElement("canvas");
                    canvas.width = Math.max(1, Math.round(image.width * scale));
                    canvas.height = Math.max(1, Math.round(image.height * scale));
                    var ctx = canvas.getContext("2d");
                    ctx.drawImage(image, 0, 0, canvas.width, canvas.height);
                    resolve(canvas.toDataURL("image/jpeg", 0.86));
                };
                image.src = reader.result;
            };
            reader.readAsDataURL(file);
        });
    }
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
    function hasAnyBusyAction() {
        return Object.keys(actionLocks).some(function (key) { return actionLocks[key]; });
    }
    function setBusy(key, active, text, button) {
        actionLocks[key] = !!active;
        root.classList.toggle("ep-is-busy", hasAnyBusyAction());
        var shade = byId("epLoadingShade");
        var label = byId("epLoadingText");
        if (shade) { shade.hidden = !hasAnyBusyAction(); }
        if (label && text) { label.textContent = text; }
        if (button) {
            if (active) {
                if (!button.getAttribute("data-original-html")) {
                    button.setAttribute("data-original-html", button.innerHTML);
                }
                button.disabled = true;
                button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> ' + html(text || "جار التنفيذ...");
            } else {
                button.disabled = false;
                if (button.getAttribute("data-original-html")) {
                    button.innerHTML = button.getAttribute("data-original-html");
                    button.removeAttribute("data-original-html");
                }
            }
        }
    }
    function guardedAction(key, button, text, fn) {
        if (actionLocks[key]) { return Promise.resolve(); }
        setBusy(key, true, text, button);
        return Promise.resolve().then(fn).then(function (x) {
            setBusy(key, false, text, button);
            return x;
        }, function (err) {
            setBusy(key, false, text, button);
            throw err;
        });
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
    function debounce(fn, wait) {
        var timer = null;
        return function () {
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(null, args); }, wait || 300);
        };
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
            root._lookups = data;
            ["epBranchFilter", "epBranch", "epRunBranch", "epMedBranch", "epReportBranch"].forEach(function (id) { fillSelect(byId(id), data.Branches, id === "epBranch" ? "غير محدد" : "كل الفروع"); });
            ["epDepartmentFilter", "epDepartment", "epRunDepartment", "epMedDepartment", "epReportDepartment"].forEach(function (id) { fillSelect(byId(id), data.Departments, id === "epDepartment" ? "غير محدد" : "كل الإدارات"); });
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
    function hasEmployeeSearchCriteria(filter) {
        filter = filter || employeeFilter();
        return (filter.Term && filter.Term.trim().length >= 2) || filter.BranchId || filter.DepartmentId || filter.IsActive !== null;
    }
    function updateEmployeeDashboard(rows) {
        rows = rows || [];
        var branches = {};
        var salaryTotal = 0;
        rows.forEach(function (x) {
            if (x.BranchName) { branches[x.BranchName] = true; }
            salaryTotal += Number(x.BasicSalary || 0);
        });
        setText("epDashEmployees", rows.length);
        setText("epDashBranches", Object.keys(branches).length);
        setText("epDashInsured", rows.filter(function (x) {
            return x.MedicalInsurance && (x.MedicalInsurance.IsActive || x.MedicalInsurance.PlanId);
        }).length);
        setText("epDashPendingInsurance", rows.filter(function (x) {
            return x.MedicalInsurance && x.MedicalInsurance.IsActive && !x.MedicalInsurance.PlanId;
        }).length);
        setText("epProfileSalaryTotal", money(salaryTotal));
    }
    function buildEmployeePremiumCard(employee) {
        employee = employee || {};
        var insurance = employee.MedicalInsurance || {};
        var insured = insurance.IsActive || insurance.PlanId;
        return '<article class="ep-premium-id-card employee-card">' +
            '<div class="ep-premium-card-bg"></div>' +
            '<header><div><span>بطاقة موظف</span><strong>DynamicErp HR</strong></div><b>' + html(employee.EmployeeCode || "") + '</b></header>' +
            '<section class="ep-premium-card-main"><div class="ep-premium-card-photo">' + photoMarkup(employee.PhotoDataUrl, employee.EmployeeName, "ep-card-photo-img") + '</div><div><h3>' + html(employee.EmployeeName || "موظف") + '</h3><p>' + html(employee.JobTypeName || "الوظيفة غير محددة") + '</p><em class="' + (employee.IsActive ? "active" : "inactive") + '">' + (employee.IsActive ? "نشط" : "غير نشط") + '</em></div></section>' +
            '<dl><div><dt>الفرع</dt><dd>' + html(employee.BranchName || "غير محدد") + '</dd></div><div><dt>الإدارة</dt><dd>' + html(employee.DepartmentName || "غير محدد") + '</dd></div><div><dt>الراتب الأساسي</dt><dd>' + (number(employee.BasicSalary) > 0 ? money(employee.BasicSalary) : "غير محدد") + '</dd></div><div><dt>التأمين</dt><dd>' + (insured ? "مشترك" : "غير مشترك") + '</dd></div></dl>' +
            '<footer><span>تاريخ الإصدار: ' + html(dateInput(new Date())) + '</span><strong>HR</strong></footer>' +
            '</article>';
    }
    function buildEmployeeInsurancePremiumCard(employee) {
        employee = employee || {};
        var insurance = employee.MedicalInsurance || {};
        var planName = "";
        var plans = root._lookups && root._lookups.MedicalInsurancePlans ? root._lookups.MedicalInsurancePlans : [];
        plans.forEach(function (p) { if (String(p.Id) === String(insurance.PlanId)) { planName = p.Name; } });
        return '<article class="ep-premium-id-card insurance-card">' +
            '<div class="ep-premium-card-bg"></div>' +
            '<header><div><span>بطاقة التأمين الطبي</span><strong>' + html(planName || "Medical Insurance") + '</strong></div><b>' + html(insurance.CardNumber || employee.EmployeeCode || "") + '</b></header>' +
            '<section class="ep-premium-card-main"><div class="ep-premium-card-photo">' + photoMarkup(employee.PhotoDataUrl, employee.EmployeeName, "ep-card-photo-img") + '</div><div><h3>' + html(employee.EmployeeName || "موظف") + '</h3><p>' + html(insurance.PolicyNumber || "وثيقة غير محددة") + '</p><em class="' + (insurance.IsActive ? "active" : "inactive") + '">' + (insurance.IsActive ? "تأمين نشط" : "تأمين غير نشط") + '</em></div></section>' +
            '<dl><div><dt>بداية الاشتراك</dt><dd>' + html(dateInput(insurance.StartDate) || "غير محدد") + '</dd></div><div><dt>نهاية الاشتراك</dt><dd>' + html(dateInput(insurance.EndDate) || "مفتوح") + '</dd></div><div><dt>خصم الموظف</dt><dd>' + money(insurance.EmployeeMonthlyDeduction || 0) + '</dd></div><div><dt>تحمل الشركة</dt><dd>' + money(insurance.CompanyMonthlyCost || 0) + '</dd></div></dl>' +
            '<footer><span>يستخدم لأغراض التعريف الداخلي والمراجعة</span><strong>MI</strong></footer>' +
            '</article>';
    }
    function renderPrintCards(employee) {
        var host = byId("epCardPrintStage");
        if (!host) { return; }
        host.innerHTML = buildEmployeePremiumCard(employee) + buildEmployeeInsurancePremiumCard(employee);
    }
    function printEmployeeCard(kind) {
        var employee = currentEmployee || collectEmployee();
        if (!employee || !employee.EmployeeName) {
            message("اختر موظفا أولا قبل طباعة الكارت.", true);
            return;
        }
        renderPrintCards(employee);
        document.body.classList.remove("ep-print-employee-card", "ep-print-employee-insurance-card", "ep-print-medical-card");
        document.body.classList.add(kind === "insurance" ? "ep-print-employee-insurance-card" : "ep-print-employee-card");
        setTimeout(function () {
            window.print();
            setTimeout(function () {
                document.body.classList.remove("ep-print-employee-card", "ep-print-employee-insurance-card");
            }, 300);
        }, 50);
    }
    function renderEmployeeProfile(employee) {
        var host = byId("epProfileSummary");
        if (!host) { return; }
        if (!employee) {
            currentEmployee = null;
            host.innerHTML = '<div class="ep-profile-empty"><i class="fas fa-user-circle"></i><strong>اختر موظفا</strong><span>سيظهر هنا ملخص الفرع، الإدارة، الراتب، التأمين، وحالة الملف بدون عرض أي أكواد حسابات داخلية.</span></div>';
            return;
        }
        var salary = number(employee.BasicSalary);
        var insurance = employee.MedicalInsurance || {};
        var insured = insurance.IsActive || insurance.PlanId;
        currentEmployee = employee;
        host.innerHTML =
            '<div class="ep-profile-card">' +
            '<div class="ep-profile-avatar">' + photoMarkup(employee.PhotoDataUrl, employee.EmployeeName, "ep-profile-photo-img") + '</div>' +
            '<h3>' + html(employee.EmployeeName || "موظف") + '</h3>' +
            '<p>' + html(employee.EmployeeCode || "") + '</p>' +
            '<div class="ep-profile-badges"><span class="ep-status-badge ' + (employee.IsActive ? "active" : "cancelled") + '">' + (employee.IsActive ? "نشط" : "غير نشط") + '</span><span class="ep-status-badge ' + (insured ? "active" : "pending-approval") + '">' + (insured ? "مؤمن" : "بدون تأمين") + '</span></div>' +
            '<dl class="ep-profile-facts">' +
            '<div><dt>الفرع</dt><dd>' + html(employee.BranchName || "غير محدد") + '</dd></div>' +
            '<div><dt>الإدارة</dt><dd>' + html(employee.DepartmentName || "غير محدد") + '</dd></div>' +
            '<div><dt>الوظيفة</dt><dd>' + html(employee.JobTypeName || "غير محدد") + '</dd></div>' +
            '<div><dt>الراتب الأساسي</dt><dd>' + (salary > 0 ? money(salary) : "غير محدد") + '</dd></div>' +
            '</dl>' +
            '<div class="ep-profile-actions"><button type="button" class="ep-secondary" id="epProfilePrintEmployeeCard"><i class="fas fa-id-badge"></i> طباعة كارت الموظف</button><button type="button" class="ep-secondary" id="epProfilePrintInsuranceCard"><i class="fas fa-briefcase-medical"></i> طباعة كارت التأمين</button></div>' +
            '<div class="ep-profile-note"><i class="fas fa-lock"></i><span>الحسابات الداخلية محفوظة للنظام ولا تعرض في شاشة المراجعة.</span></div>' +
            '</div>';
    }
    function loadEmployees() {
        var filter = employeeFilter();
        if (!hasEmployeeSearchCriteria(filter)) {
            updateEmployeeDashboard([]);
            setText("epEmployeeLoadState", "بانتظار فلتر");
            var empty = byId("epEmployeesRows");
            if (empty) {
                empty.innerHTML = '<tr><td colspan="9" class="ep-empty-row">اكتب حرفين على الأقل أو اختر فرعا/إدارة/حالة لعرض الموظفين بدون تحميل قائمة كبيرة.</td></tr>';
            }
            message("استخدم البحث أو الفلاتر أولا لتسريع تحميل الموظفين.", true);
            return Promise.resolve();
        }
        setText("epEmployeeLoadState", "جار التحميل");
        var url = root.getAttribute("data-search-url") + "?" + queryString(employeeFilter());
        return getJson(url).then(function (res) {
            var rows = res.rows || [];
            root._employeeRows = rows;
            updateEmployeeDashboard(rows);
            var tbody = byId("epEmployeesRows");
            tbody.innerHTML = "";
            rows.forEach(function (x, index) {
                tbody.insertAdjacentHTML("beforeend",
                    '<tr data-employee-row="' + index + '"><td>' + html(x.EmployeeCode) + '</td><td><div class="ep-employee-name-cell"><span class="ep-avatar-thumb">' + photoMarkup(x.PhotoDataUrl, x.EmployeeName, "ep-avatar-thumb-img") + '</span><strong>' + html(x.EmployeeName) + '</strong></div></td><td>' + html(x.BranchName) + '</td><td>' + html(x.DepartmentName) + '</td><td>' + html(x.JobTypeName) + '</td><td>' + (number(x.BasicSalary) > 0 ? money(x.BasicSalary) : "غير محدد") + '</td><td><span class="ep-status-badge pending-approval">يفتح من الملف</span></td><td><span class="ep-status-badge ' + (x.IsActive ? "active" : "cancelled") + '">' + (x.IsActive ? "نشط" : "غير نشط") + '</span></td><td><div class="ep-row-actions"><button type="button" title="فتح الملف" data-edit="' + x.EmployeeId + '"><i class="fas fa-edit"></i></button><button type="button" title="' + (x.IsActive ? "تعطيل آمن" : "تفعيل") + '" data-active="' + x.EmployeeId + '" data-state="' + (!x.IsActive) + '">' + (x.IsActive ? "تعطيل" : "تفعيل") + '</button></div></td></tr>');
            });
            if (!rows.length) {
                tbody.innerHTML = '<tr><td colspan="9" class="ep-empty-row">لا توجد نتائج مطابقة.</td></tr>';
            }
            setText("epEmployeeLoadState", "تم التحميل");
            message("تم تحميل " + (res.rows || []).length + " موظف");
            renderEmployeeProfile(rows[0]);
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
        currentEmployee = employee;
        byId("epEmployeeId").value = employee.EmployeeId || "";
        byId("epCode").value = employee.EmployeeCode || "";
        byId("epName").value = employee.EmployeeName || "";
        byId("epMobile").value = employee.Mobile || "";
        byId("epPhone").value = employee.Phone || "";
        byId("epEmail").value = employee.Email || "";
        setEmployeePhoto(employee.PhotoDataUrl || "", employee.EmployeeName || "");
        byId("epIsActive").checked = employee.IsActive !== false;
        byId("epBranch").value = employee.BranchId || "";
        byId("epDepartment").value = employee.DepartmentId || "";
        byId("epJob").value = employee.JobTypeId || "";
        byId("epHiringDate").value = dateInput(employee.HiringDate);
        byId("epSalary").value = employee.BasicSalary || 0;
        byId("epAccountCode").value = "";
        byId("epAccruedAccountCode").value = "";
        byId("epNotes").value = employee.Notes || "";
        var mi = employee.MedicalInsurance || {};
        byId("epInsuranceId").value = mi.Id || "";
        byId("epInsuranceActive").checked = !!mi.IsActive;
        byId("epInsurancePlan").value = mi.PlanId || "";
        byId("epInsurancePolicyNumber").value = mi.PolicyNumber || "";
        byId("epInsuranceCardNumber").value = mi.CardNumber || "";
        byId("epInsuranceCoveragePercent").value = mi.CoveragePercent || 100;
        byId("epInsuranceMonthlyCost").value = mi.MonthlyCost || 0;
        byId("epEmployeeShareType").value = mi.EmployeeShareType || mi.DeductionType || "Amount";
        byId("epEmployeeShareValue").value = mi.EmployeeShareValue || mi.Amount || 0;
        byId("epCompanyShareType").value = mi.CompanyShareType || "AutoBalance";
        byId("epCompanyShareValue").value = mi.CompanyShareValue || 0;
        byId("epInsuranceStart").value = dateInput(mi.StartDate);
        byId("epInsuranceEnd").value = dateInput(mi.EndDate);
        byId("epInsuranceMonthly").checked = mi.IsMonthly !== false;
        byId("epInsuranceNotes").value = mi.Notes || "";
        employeeInsuranceDependents = (mi.Dependents || employee.MedicalInsuranceDependents || []).map(function (x) {
            return {
                DependentId: x.DependentId || null,
                DependentName: x.DependentName || x.Name || "",
                Relation: x.Relation || "Child",
                BirthDate: dateInput(x.BirthDate),
                CoveragePercent: x.CoveragePercent || 100,
                IsActive: x.IsActive !== false
            };
        });
        renderEmployeeInsuranceDependents();
        renderInsuranceHistory(employee.MedicalInsuranceHistory || []);
        renderEmployeeProfile(employee);
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
            PhotoDataUrl: byId("epPhotoDataUrl") ? byId("epPhotoDataUrl").value : "",
            Notes: byId("epNotes").value,
            MedicalInsurance: {
                Id: byId("epInsuranceId").value ? parseInt(byId("epInsuranceId").value, 10) : null,
                PlanId: byId("epInsurancePlan").value ? parseInt(byId("epInsurancePlan").value, 10) : null,
                PolicyNumber: byId("epInsurancePolicyNumber").value,
                CardNumber: byId("epInsuranceCardNumber").value,
                CoveragePercent: number(byId("epInsuranceCoveragePercent").value) || 100,
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
                Notes: byId("epInsuranceNotes").value,
                Dependents: employeeInsuranceDependents
            }
        };
    }
    function validateEmployeeInsuranceForSave(employee) {
        var mi = employee && employee.MedicalInsurance ? employee.MedicalInsurance : null;
        if (!mi || !mi.IsActive) { return true; }
        if (!mi.PlanId) {
            message("يجب اختيار خطة التأمين الطبي للاشتراك النشط.", true);
            return false;
        }
        if (mi.CoveragePercent < 0 || mi.CoveragePercent > 100) {
            message("نسبة تغطية التأمين الطبي يجب أن تكون بين 0 و 100.", true);
            return false;
        }
        if (mi.EndDate && mi.StartDate && mi.EndDate < mi.StartDate) {
            message("تاريخ نهاية اشتراك التأمين لا يمكن أن يكون قبل تاريخ البداية.", true);
            return false;
        }
        return true;
    }

    function salaryRequest() {
        return {
            PayrollRunId: byId("epPayrollRunId") && byId("epPayrollRunId").value ? parseInt(byId("epPayrollRunId").value, 10) : null,
            RunName: byId("epRunName") ? byId("epRunName").value : "",
            Year: parseInt(byId("epRunYear").value, 10),
            Month: parseInt(byId("epRunMonth").value, 10),
            BranchId: byId("epRunBranch").value ? parseInt(byId("epRunBranch").value, 10) : null,
            DepartmentId: byId("epRunDepartment").value ? parseInt(byId("epRunDepartment").value, 10) : null,
            EmployeeId: byId("epRunEmployee").value ? parseInt(byId("epRunEmployee").value, 10) : null,
            PostingStatus: byId("epPostingStatus") ? byId("epPostingStatus").value : "",
            IncludeSavedDrafts: true,
            RebuildEmployees: byId("epRebuildEmployees") ? !!byId("epRebuildEmployees").checked : false,
            ExcludeAlreadyIncluded: byId("epExcludeAlreadyIncluded") ? !!byId("epExcludeAlreadyIncluded").checked : true,
            OnlyUnincluded: byId("epOnlyUnincluded") ? !!byId("epOnlyUnincluded").checked : false,
            AllowDuplicateEmployees: byId("epAllowDuplicateEmployees") ? !!byId("epAllowDuplicateEmployees").checked : false,
            ManualEmployeeIds: byId("epManualEmployeeIds") ? byId("epManualEmployeeIds").value : "",
            RowLimit: 300,
            JournalPreviewLimit: 500
        };
    }
    function postingRequest(saveFirst) {
        var req = salaryRequest();
        req.IncludeLineDetails = true;
        req.SaveSalaryRunBeforePosting = !!saveFirst;
        return req;
    }
    function setRunMonth(offset) {
        var year = parseInt(byId("epRunYear").value, 10) || new Date().getFullYear();
        var month = parseInt(byId("epRunMonth").value, 10) || (new Date().getMonth() + 1);
        if (offset === "current") {
            var now = new Date();
            year = now.getFullYear();
            month = now.getMonth() + 1;
        } else {
            var next = new Date(year, month - 1 + offset, 1);
            year = next.getFullYear();
            month = next.getMonth() + 1;
        }
        byId("epRunYear").value = year;
        byId("epRunMonth").value = month;
    }
    function renderSalary(preview) {
        preview = preview || {};
        root._salaryPreview = preview;
        root._salaryRows = preview.Rows || [];
        byId("epTotalBasic").textContent = money(preview.TotalBasic);
        byId("epTotalAdditions").textContent = money(preview.TotalAdditions);
        byId("epTotalDeductions").textContent = money(preview.TotalDeductions);
        byId("epTotalMedical").textContent = money(preview.TotalMedicalInsurance);
        byId("epTotalCompanyMedical").textContent = money(preview.TotalMedicalInsuranceCompanyCost);
        byId("epTotalNet").textContent = money(preview.TotalNet);
        setText("epRunRowsCount", root._salaryRows.length);
        setText("epPostedRowsCount", root._salaryRows.filter(function (x) { return !!x.IsApproved; }).length);
        setText("epUnpostedRowsCount", root._salaryRows.filter(function (x) { return !x.IsApproved; }).length);
        byId("epSalaryRows").innerHTML = "";
        (preview.Rows || []).forEach(function (x, index) {
            byId("epSalaryRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.BranchName || "") + '</td><td><span class="ep-status-badge ' + (x.IsApproved ? "active" : "pending-approval") + '">' + (x.IsApproved ? "مرحل" : "غير مرحل") + '</span></td><td>' + money(x.BasicSalary) + '</td><td>' + money((x.SalaryAllowances || 0) + (x.VariableAdditions || 0)) + '</td><td>' + money(x.AdvanceDeduction) + '</td><td>' + money((x.ExistingDiscounts || 0) + (x.MedicalInsuranceDeduction || 0) + (x.VacationDeduction || 0)) + '</td><td>' + money(x.MedicalInsuranceDeduction) + '</td><td>' + money(x.NetSalary) + '</td><td><button type="button" data-payroll-row="' + index + '"><i class="fas fa-eye"></i> تفاصيل</button></td></tr>');
        });
        if (!(preview.Rows || []).length) {
            byId("epSalaryRows").innerHTML = '<tr><td colspan="10" class="ep-empty-row">لا توجد صفوف مطابقة للفلاتر الحالية.</td></tr>';
        }
        byId("epJournalRows").innerHTML = "";
        (preview.JournalPreview || []).forEach(function (x) {
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.AccountSerial || "غير محدد") + '</td><td>' + html(x.AccountName || "حساب غير محدد") + '</td><td>' + html(x.Description) + '</td><td>' + html(x.PayrollRunId || preview.PayrollRunId || "") + '</td><td>' + money(x.Debit) + '</td><td>' + money(x.Credit) + '</td></tr>');
        });
        if (!(preview.JournalPreview || []).length) {
            byId("epJournalRows").innerHTML = '<tr><td colspan="6" class="ep-empty-row">لا توجد سطور قيد للمعاينة الحالية.</td></tr>';
        }
        message(preview.Message || "تم حساب المسير");
    }
    function renderPayrollIssues(issues, debit, credit) {
        var host = byId("epPayrollIssues");
        if (!host) { return; }
        var rows = issues || [];
        var diff = number(debit) - number(credit);
        if (Math.abs(diff) > 0.01) {
            rows = rows.concat([{ ArabicMessage: "القيد غير متوازن. الفرق: " + money(diff) }]);
        }
        if (!rows.length) {
            host.innerHTML = '<div class="ep-empty-row">لا توجد ملاحظات تمنع الترحيل.</div>';
            setText("epIssuesStatus", "لا توجد ملاحظات");
            setText("epIssueCount", "0");
            setText("epIssueHint", "المسير جاهز للمراجعة");
            if (byId("epIssuesStatus")) { byId("epIssuesStatus").className = "ep-status-badge active"; }
            return;
        }
        host.innerHTML = rows.map(function (x) {
            return '<article class="ep-issue-row"><strong>' + html(x.EmployeeName || x.ComponentName || "ملاحظة") + '</strong><span>' + html(x.ArabicMessage || x.Message || "") + '</span></article>';
        }).join("");
        setText("epIssuesStatus", rows.length + " ملاحظة");
        setText("epIssueCount", rows.length);
        setText("epIssueHint", "راجع مركز الأخطاء قبل الترحيل");
        if (byId("epIssuesStatus")) { byId("epIssuesStatus").className = "ep-status-badge cancelled"; }
    }

    function setReadinessCard(id, state, title, hint) {
        var card = byId(id);
        if (!card) { return; }
        card.className = state || "waiting";
        var strong = card.querySelector("strong");
        var small = card.querySelector("small");
        if (strong) { strong.textContent = title || ""; }
        if (small) { small.textContent = hint || ""; }
    }

    function updatePayrollReadiness(preview, rows, debit, credit, issuesCount) {
        rows = rows || [];
        var runId = preview && preview.PayrollRunId;
        var diff = number(debit) - number(credit);
        var hasJournal = (preview && preview.JournalPreview && preview.JournalPreview.length) || debit || credit;
        var isPartialJournal = preview && preview._JournalPreviewLimited;
        setReadinessCard(
            "epReadinessEmployees",
            rows.length ? "ready" : "waiting",
            rows.length ? rows.length + " موظف داخل المسير" : "لم تتم المعاينة",
            rows.length ? "تم تطبيق الفلاتر وقواعد الاستحقاق على الموظفين." : "اضغط معاينة لتحميل الموظفين المؤهلين فقط."
        );
        setReadinessCard(
            "epReadinessSnapshot",
            runId ? "ready" : "waiting",
            runId ? "Snapshot محفوظ #" + runId : "لم يحفظ بعد",
            runId ? "إعادة الحفظ لن تغيّر الموظفين إلا عند اختيار إعادة التكوين." : "بعد الحفظ يتم تثبيت Snapshot الموظفين."
        );
        setReadinessCard(
            "epReadinessJournal",
            hasJournal ? (isPartialJournal ? "waiting" : (Math.abs(diff) <= 0.01 ? "ready" : "blocked")) : "waiting",
            hasJournal ? (isPartialJournal ? "معاينة مبدئية للقيد" : (Math.abs(diff) <= 0.01 ? "القيد متوازن" : "القيد غير متوازن")) : "بانتظار المراجعة",
            hasJournal ? (isPartialJournal ? "اضغط مراجعة القيد لإظهار الحكم النهائي." : ("المدين " + money(debit) + " / الدائن " + money(credit))) : "راجع المدين والدائن قبل الترحيل."
        );
        setReadinessCard(
            "epReadinessIssues",
            issuesCount ? "blocked" : (rows.length ? "ready" : "waiting"),
            issuesCount ? issuesCount + " ملاحظة تحتاج مراجعة" : (rows.length ? "لا توجد موانع ظاهرة" : "لا توجد بيانات"),
            issuesCount ? "افتح تبويب الأخطاء والتحذيرات قبل الاعتماد." : "سيظهر هنا أي مانع قبل الاعتماد."
        );
    }

    function renderSalary(preview) {
        preview = preview || {};
        root._salaryPreview = preview;
        root._salaryRows = preview.Rows || [];
        if (preview.PayrollRunId && byId("epPayrollRunId")) { byId("epPayrollRunId").value = preview.PayrollRunId; }
        if (preview.RunName && byId("epRunName")) { byId("epRunName").value = preview.RunName; }
        var rows = root._salaryRows;
        var totalAdditions = number(preview.TotalAdditions);
        var totalDeductions = number(preview.TotalDeductions);
        setText("epTotalBasic", money(preview.TotalBasic));
        setText("epTotalAdditions", money(totalAdditions));
        setText("epTotalDeductions", money(totalDeductions));
        setText("epTotalMedical", money(preview.TotalMedicalInsurance));
        setText("epTotalCompanyMedical", money(preview.TotalMedicalInsuranceCompanyCost));
        setText("epTotalNet", money(preview.TotalNet));
        setText("epTopTotalBasic", money(preview.TotalBasic));
        setText("epTopTotalAdditions", money(totalAdditions));
        setText("epTopTotalDeductions", money(totalDeductions));
        setText("epTopTotalNet", money(preview.TotalNet));
        setText("epRunRowsCount", rows.length);
        setText("epFooterTotalBasic", money(preview.TotalBasic));
        setText("epFooterTotalAdditions", money(totalAdditions));
        setText("epFooterTotalDeductions", money(totalDeductions));
        setText("epFooterTotalMedical", money(preview.TotalMedicalInsurance));
        setText("epFooterTotalAdvance", money(preview.TotalAdvance));
        setText("epFooterTotalNet", money(preview.TotalNet));
        setText("epFooterTotalRows", rows.length + " موظف");
        setText("epPostedRowsCount", rows.filter(function (x) { return !!x.IsApproved; }).length);
        setText("epUnpostedRowsCount", rows.filter(function (x) { return !x.IsApproved; }).length);
        var postedCount = rows.filter(function (x) { return !!x.IsApproved; }).length;
        setText("epRunPostingSummary", postedCount && postedCount === rows.length ? "مرحل" : "غير مرحل");
        setText("epRunPostingSummaryHint", postedCount ? (postedCount + " مرحل من " + rows.length) : "سيتم إنشاء القيد عند اعتماد المسير");
        byId("epSalaryRows").innerHTML = "";
        rows.forEach(function (x, index) {
            var notes = [];
            if (!x.AccruedSalaryAccountCode) { notes.push("حساب الأجور المستحقة غير محدد"); }
            if (x.MedicalInsuranceDeduction > 0 && !x.MedicalInsuranceEmployeeAccountCode) { notes.push("حساب استحقاق التأمين غير محدد"); }
            if (number(x.VacationDays) > 0) { notes.push("أيام إجازة: " + number(x.VacationDays)); }
            if (number(x.VacationDeduction) > 0) { notes.push("خصم إجازات/مرضية: " + money(x.VacationDeduction)); }
            byId("epSalaryRows").insertAdjacentHTML("beforeend",
                '<tr data-payroll-search="' + html(((x.EmployeeCode || "") + " " + (x.EmployeeName || "") + " " + (x.BranchName || "") + " " + (x.DepartmentName || "")).toLowerCase()) + '">' +
                '<td>' + html(x.EmployeeCode || "") + '</td>' +
                '<td><button type="button" class="ep-link-button" data-payroll-row="' + index + '">' + html(x.EmployeeName || "") + '</button></td>' +
                '<td>' + html(x.BranchName || "") + '</td>' +
                '<td>' + html(x.DepartmentName || "") + '</td>' +
                '<td>' + money(x.BasicSalary) + '</td>' +
                '<td>' + money((x.SalaryAllowances || 0) + (x.VariableAdditions || 0)) + '</td>' +
                '<td>' + money((x.ExistingDiscounts || 0) + (x.MedicalInsuranceDeduction || 0) + (x.VacationDeduction || 0)) + '</td>' +
                '<td>' + money(x.MedicalInsuranceDeduction) + '</td>' +
                '<td>' + money(x.AdvanceDeduction) + '</td>' +
                '<td>' + money(x.NetSalary) + '</td>' +
                '<td><span class="ep-status-badge ' + (x.IsApproved ? "active" : "pending-approval") + '">' + (x.IsApproved ? "مرحل" : "غير مرحل") + '</span></td>' +
                '<td>' + html(notes.join("، ") || "سليم") + '</td></tr>');
        });
        if (!rows.length) {
            byId("epSalaryRows").innerHTML = '<tr><td colspan="12" class="ep-empty-row">لا توجد صفوف مطابقة للفلاتر الحالية.</td></tr>';
        }
        byId("epJournalRows").innerHTML = "";
        var journalDebit = 0;
        var journalCredit = 0;
        var journalRows = preview.JournalPreview || [];
        var journalPreviewLimited = true;
        preview._JournalPreviewLimited = journalPreviewLimited;
        journalRows.forEach(function (x) {
            journalDebit += number(x.Debit);
            journalCredit += number(x.Credit);
            byId("epJournalRows").insertAdjacentHTML("beforeend",
                '<tr><td>' + html(x.AccountSerial || "غير محدد") + '</td><td>' + html(x.AccountName || "حساب غير محدد") + '</td><td>' + html(x.Description || "") + '</td><td>' + html(x.PayrollRunId || preview.PayrollRunId || "") + '</td><td>' + money(x.Debit) + '</td><td>' + money(x.Credit) + '</td></tr>');
        });
        if (!journalRows.length) {
            byId("epJournalRows").innerHTML = '<tr><td colspan="6" class="ep-empty-row">لا توجد سطور قيد للمعاينة الحالية.</td></tr>';
        }
        setText("epJournalDebitTotal", money(journalDebit));
        setText("epJournalCreditTotal", money(journalCredit));
        setText("epJournalBalanceDiff", money(journalDebit - journalCredit));
        var balanced = Math.abs(journalDebit - journalCredit) <= 0.01;
        setText("epJournalBalanceStatus", journalPreviewLimited ? "معاينة مبدئية للقيد" : (balanced ? "القيد متوازن" : "القيد غير متوازن"));
        if (byId("epJournalBalanceStatus")) { byId("epJournalBalanceStatus").className = "ep-status-badge " + (journalPreviewLimited ? "pending-approval" : (balanced ? "active" : "cancelled")); }
        var previewIssues = (preview.AccountIssues || preview.ValidationIssues || preview.Issues || preview.CompatibilityWarnings || []);
        renderPayrollIssues(previewIssues, journalPreviewLimited ? 0 : journalDebit, journalPreviewLimited ? 0 : journalCredit);
        updatePayrollReadiness(preview, rows, journalDebit, journalCredit, previewIssues.length + (!journalPreviewLimited && Math.abs(journalDebit - journalCredit) > 0.01 ? 1 : 0));
        message(preview.Message || "تم حساب المسير");
    }

    function previewSalary() {
        var url = root.getAttribute("data-preview-url") + "?" + queryString(salaryRequest());
        return getJson(url).then(function (res) {
            if (!res.success) { message(res.message || "تعذر الحساب", true); return; }
            renderSalary(res.preview);
            return loadSalarySheet();
        });
    }

    function formatMoney(v) {
        var n = number(v);
        return n.toLocaleString ? n.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : money(n);
    }
    function renderPayrollDetail(row) {
        var panel = byId("epRunDetailPanel");
        if (!panel || !row) { return; }
        panel.hidden = false;
        setText("epRunDetailTitle", (row.EmployeeCode || "") + " - " + (row.EmployeeName || ""));
        setText("epRunDetailSubtitle", "الفرع: " + (row.BranchName || "غير محدد") + " | الإدارة: " + (row.DepartmentName || "غير محدد"));
        setText("epRunDetailStatus", row.IsApproved ? "مرحل" : "غير مرحل");
        byId("epRunDetailStatus").className = "ep-status-badge " + (row.IsApproved ? "active" : "pending-approval");
        setText("epDetailBasic", money(row.BasicSalary));
        setText("epDetailAdditions", money((row.SalaryAllowances || 0) + (row.VariableAdditions || 0)));
        setText("epDetailAdvance", money(row.AdvanceDeduction));
        setText("epDetailDiscounts", money((row.ExistingDiscounts || 0) + (row.MedicalInsuranceDeduction || 0) + (row.VacationDeduction || 0)));
        setText("epDetailInsurance", money(row.MedicalInsuranceDeduction));
        setText("epDetailNet", money(row.NetSalary));
        var advanceShell = byId("epAdvanceInstallmentsShell");
        var advanceRows = byId("epAdvanceInstallmentsRows");
        if (advanceShell && advanceRows) {
            var parts = row.AdvanceInstallments || [];
            advanceRows.innerHTML = parts.length
                ? parts.map(function (p) {
                    return '<tr><td>' + html(p.AdvanceId || "") + '</td><td>' + html(p.PartNo || "") + '</td><td>' + html(p.PartDate || "") + '</td><td>' + money(p.PartValue || 0) + '</td><td>' + html(p.StatusText || (p.IsPosted ? "تم الخصم" : "جاهز للخصم")) + '</td></tr>';
                }).join("")
                : '<tr><td colspan="5" class="ep-empty-row">لا توجد أقساط سلفة لهذا الموظف في هذا الشهر.</td></tr>';
            advanceShell.hidden = false;
        }
        panel.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
    function renderPostingSummary(result) {
        result = result || {};
        root._lastPostingResult = result;
        var status = byId("epPreviewStatus");
        if (status) {
            status.textContent = result.AlreadyPosted ? "مرحل سابقاً" : (result.IsPosted ? "تم الترحيل" : "جاهز للمراجعة");
            status.className = "ep-status-badge " + (result.AlreadyPosted || result.IsPosted ? "active" : "pending-approval");
        }
        byId("epJournalRows").innerHTML = "";
        (result.AccountIssues || []).forEach(function (x) {
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.AccountSerial || "غير محدد") + '</td><td>' + html(x.AccountName || x.AccountSource || "مصدر الحساب غير محدد") + '</td><td>' + html(x.ArabicMessage || "") + '</td><td>' + html(result.PayrollRunId || "") + '</td><td>' + money(x.Direction === "Debit" ? x.Amount : 0) + '</td><td>' + money(x.Direction === "Credit" ? x.Amount : 0) + '</td></tr>');
        });
        (result.AffectedAccounts || []).forEach(function (x) {
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.Key) + '</td><td>' + money(x.Debit) + '</td><td>' + money(x.Credit) + '</td><td>' + html(result.Message || "") + '</td><td>' + html(x.Lines) + '</td></tr>');
        });
        if (!(result.AffectedAccounts || []).length && !(result.AccountIssues || []).length) {
            byId("epJournalRows").innerHTML = '<tr><td colspan="5" class="ep-empty-row">' + html(result.Message || "لا توجد سطور ترحيل.") + "</td></tr>";
        }
        var hasIssues = (result.AccountIssues || []).length > 0 || Math.abs(number(result.Balance)) > 0.01;
        message((result.Message || "") + " المدين: " + money(result.DebitTotal) + " الدائن: " + money(result.CreditTotal) + " الفرق: " + money(result.Balance), hasIssues);
    }
    function renderPostingSummary(result) {
        result = result || {};
        root._lastPostingResult = result;
        var status = byId("epPreviewStatus");
        if (status) {
            status.textContent = result.AlreadyPosted ? "مرحل سابقا" : (result.IsPosted ? "تم الترحيل" : "جاهز للمراجعة");
            status.className = "ep-status-badge " + (result.AlreadyPosted || result.IsPosted ? "active" : "pending-approval");
        }
        byId("epJournalRows").innerHTML = "";
        var debit = 0;
        var credit = 0;
        (result.AccountIssues || []).forEach(function (x) {
            var d = x.Direction === "Debit" ? number(x.Amount) : 0;
            var c = x.Direction === "Credit" ? number(x.Amount) : 0;
            debit += d;
            credit += c;
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr class="ep-row-error"><td>' + html(x.AccountSerial || "غير محدد") + '</td><td>' + html(x.AccountName || x.AccountSource || "مصدر الحساب غير محدد") + '</td><td>' + html(x.ArabicMessage || "") + '</td><td>' + html(result.PayrollRunId || "") + '</td><td>' + money(d) + '</td><td>' + money(c) + '</td></tr>');
        });
        (result.AffectedAccounts || []).forEach(function (x) {
            debit += number(x.Debit);
            credit += number(x.Credit);
            byId("epJournalRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.AccountSerial || "غير محدد") + '</td><td>' + html(x.AccountName || "حساب غير محدد") + '</td><td>' + html(result.Message || "معاينة القيد قبل الترحيل") + '</td><td>' + html(result.PayrollRunId || "") + '</td><td>' + money(x.Debit) + '</td><td>' + money(x.Credit) + '</td></tr>');
        });
        if (!(result.AffectedAccounts || []).length && !(result.AccountIssues || []).length) {
            byId("epJournalRows").innerHTML = '<tr><td colspan="6" class="ep-empty-row">' + html(result.Message || "لا توجد سطور ترحيل.") + "</td></tr>";
        }
        setText("epJournalDebitTotal", money(result.DebitTotal || result.TotalDebit || debit));
        setText("epJournalCreditTotal", money(result.CreditTotal || result.TotalCredit || credit));
        var balance = number(result.Balance);
        setText("epJournalBalanceDiff", money(balance || ((result.DebitTotal || result.TotalDebit || debit) - (result.CreditTotal || result.TotalCredit || credit))));
        var hasIssues = (result.AccountIssues || []).length > 0 || Math.abs(balance) > 0.01;
        setText("epJournalBalanceStatus", hasIssues ? "يوجد خطأ يمنع الترحيل" : "القيد متوازن");
        if (byId("epJournalBalanceStatus")) { byId("epJournalBalanceStatus").className = "ep-status-badge " + (hasIssues ? "cancelled" : "active"); }
        var issueCount = (result.AccountIssues || []).length + (hasIssues && !(result.AccountIssues || []).length ? 1 : 0);
        renderPayrollIssues(result.AccountIssues || [], result.DebitTotal || result.TotalDebit || debit, result.CreditTotal || result.TotalCredit || credit);
        updatePayrollReadiness(root._salaryPreview || { PayrollRunId: result.PayrollRunId }, root._salaryRows || [], result.DebitTotal || result.TotalDebit || debit, result.CreditTotal || result.TotalCredit || credit, issueCount);
        message((result.Message || "") + " المدين: " + money(result.DebitTotal || result.TotalDebit || debit) + " الدائن: " + money(result.CreditTotal || result.TotalCredit || credit) + " الفرق: " + money(balance), hasIssues);
    }

    function postingDryRun() {
        var url = root.getAttribute("data-posting-dry-run-url");
        if (!url) { message("معاينة الترحيل غير متاحة في هذه الشاشة.", true); return Promise.resolve(null); }
        return postJson(url, postingRequest(false)).then(function (res) {
            if (!res.success) { message(res.message || "تعذرت معاينة الترحيل", true); return null; }
            renderPostingSummary(res.result);
            return res.result;
        });
    }
    function comparePayrollRuns() {
        var url = root.getAttribute("data-compare-runs-url");
        var a = byId("epCompareRunA") && byId("epCompareRunA").value ? parseInt(byId("epCompareRunA").value, 10) : 0;
        var b = byId("epCompareRunB") && byId("epCompareRunB").value ? parseInt(byId("epCompareRunB").value, 10) : 0;
        if (!url || !a || !b) {
            message("اختر رقم المسير الأول والثاني قبل المقارنة.", true);
            return Promise.resolve(null);
        }
        return postJson(url, { FirstPayrollRunId: a, SecondPayrollRunId: b }).then(function (res) {
            if (!res.success) { message(res.message || "تعذرت مقارنة المسيرين", true); return null; }
            var r = res.result || {};
            var htmlResult = "المشترك: " + (r.CommonEmployees || 0) +
                " | في الأول فقط: " + (r.FirstOnlyEmployees || 0) +
                " | في الثاني فقط: " + (r.SecondOnlyEmployees || 0) +
                " | فرق الأساسي: " + money(r.BasicDifference) +
                " | فرق البدلات: " + money(r.AllowancesDifference) +
                " | فرق الخصومات: " + money(r.DeductionsDifference) +
                " | فرق الصافي: " + money(r.NetDifference) +
                " | فرق مدين القيد: " + money(r.JournalDebitDifference) +
                " | فرق دائن القيد: " + money(r.JournalCreditDifference);
            setText("epCompareCommon", r.CommonEmployees || 0);
            setText("epCompareFirstOnly", r.FirstOnlyEmployees || 0);
            setText("epCompareSecondOnly", r.SecondOnlyEmployees || 0);
            setText("epCompareNetDiff", money(r.NetDifference));
            setText("epCompareJournalDiff", money(number(r.JournalDebitDifference) - number(r.JournalCreditDifference)));
            setText("epCompareResult", htmlResult);
            message("تمت مقارنة المسيرين");
            return r;
        });
    }
    function postPayroll() {
        var url = root.getAttribute("data-posting-url");
        if (!url) { message("ترحيل القيد غير متاح في هذه الشاشة.", true); return Promise.resolve(); }
        return postingDryRun().then(function (dryRun) {
            if (!dryRun) { return null; }
            if ((dryRun.AccountIssues || []).length || Math.abs(number(dryRun.Balance)) > 0.01) {
                message("لا يمكن الترحيل قبل معالجة الحسابات الناقصة واتزان القيد.", true);
                return null;
            }
            return postJson(url, postingRequest(true)).then(function (res) {
                if (!res.success) { message(res.message || "تعذر ترحيل القيد", true); return; }
                renderPostingSummary(res.result);
                return previewSalary();
            });
        });
    }
    function renderSalarySheet(report) {
        report = report || {};
        root._salarySheetReport = report;
        var panel = byId("epSalarySheetPanel");
        if (panel) { panel.hidden = false; }
        setText("epSheetTitle", report.ReportTitle || "مسير رواتب");
        setText("epSheetPeriod", report.PeriodLabel || "");
        setText("epSheetPeriodRange", "الفترة: " + dateInput(report.PeriodFrom) + " إلى " + dateInput(report.PeriodTo));
        setText("epSheetGeneratedAt", "وقت الإنشاء: " + dateInput(report.GeneratedAt));
        setText("epSheetPostingFilter", "حالة الترحيل: " + (byId("epPostingStatus") && byId("epPostingStatus").selectedIndex >= 0 ? byId("epPostingStatus").options[byId("epPostingStatus").selectedIndex].text : "الكل"));
        setText("epSheetTotalBasic", money(report.TotalBasic));
        setText("epSheetTotalAllowances", money(report.TotalAllowances));
        setText("epSheetTotalAdvances", money(report.TotalAdvances));
        setText("epSheetTotalDeductions", money(report.TotalDeductions));
        setText("epSheetTotalInsurance", money(report.TotalInsurance));
        setText("epSheetTotalNet", money(report.TotalNet));
        setText("epSheetTotalRows", String(report.TotalRows || 0) + " / مرحل: " + String(report.PostedRows || 0) + " / غير مرحل: " + String(report.UnpostedRows || 0));
        setText("epSheetJournalDebit", money(report.JournalDebitTotal));
        setText("epSheetJournalCredit", money(report.JournalCreditTotal));
        setText("epSheetJournalBalance", money(report.JournalBalance));
        var status = byId("epSheetJournalStatus");
        if (status) {
            status.textContent = report.JournalBalanced ? "القيد متزن" : "القيد غير متزن";
            status.className = "ep-status-badge " + (report.JournalBalanced ? "active" : "expired");
        }
        var tbody = byId("epSalarySheetRows");
        if (!tbody) { return; }
        tbody.innerHTML = "";
        (report.Rows || []).forEach(function (x) {
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode || "") + " - " + html(x.EmployeeName || "") + '</td><td>' + html(x.BranchName || "غير محدد") + '<br><small>' + html(x.DepartmentName || "غير محدد") + '</small></td><td>' + money(x.BasicSalary) + '</td><td>' + money(x.Allowances) + '</td><td>' + money(x.Advances) + '</td><td>' + money(x.Deductions) + '</td><td>' + money(x.Insurance) + '</td><td>' + money(x.NetSalary) + '</td><td><span class="ep-status-badge ' + (x.IsPosted ? "active" : "pending-approval") + '">' + html(x.PostingStatus || (x.IsPosted ? "مرحل" : "غير مرحل")) + '</span></td></tr>');
        });
        if (!(report.Rows || []).length) {
            tbody.innerHTML = '<tr><td colspan="9" class="ep-empty-row">لا توجد بيانات مطابقة للفلاتر الحالية.</td></tr>';
        }
    }
    function loadSalarySheet() {
        var url = root.getAttribute("data-salary-sheet-url");
        if (!url) { message("تقرير مسير الرواتب غير متاح في هذه الشاشة.", true); return Promise.resolve(null); }
        return getJson(url + "?" + queryString(salaryRequest())).then(function (res) {
            if (!res.success) { message(res.message || "تعذر توليد تقرير المسير", true); return null; }
            renderSalarySheet(res.report);
            message("تم توليد تقرير مسير الرواتب.");
            return res.report;
        });
    }
    function exportSalaryCsv() {
        var report = root._salarySheetReport;
        var rows = report && report.Rows ? report.Rows : [];
        if (!rows.length) { message("لا توجد صفوف لتصديرها.", true); return; }
        var headers = ["كود الموظف", "الموظف", "الفرع", "الإدارة", "الأساسي", "الإضافات", "السلف", "الخصومات", "التأمين", "الصافي", "حالة الترحيل"];
        var meta = [
            ["مسير رواتب"],
            [report.PeriodLabel || ""],
            ["الفترة", dateInput(report.PeriodFrom), dateInput(report.PeriodTo)],
            ["إجمالي المدين", money(report.JournalDebitTotal), "إجمالي الدائن", money(report.JournalCreditTotal), "الفرق", money(report.JournalBalance)],
            []
        ];
        var csvRows = meta.concat([headers]).concat(rows.map(function (x) {
            return [x.EmployeeCode, x.EmployeeName, x.BranchName, x.DepartmentName, money(x.BasicSalary), money(x.Allowances), money(x.Advances), money(x.Deductions), money(x.Insurance), money(x.NetSalary), x.PostingStatus];
        })).map(function (cols) {
            return cols.map(function (value) { return '"' + String(value == null ? "" : value).replace(/"/g, '""') + '"'; }).join(",");
        }).join("\r\n");
        var blob = new Blob(["\ufeff" + csvRows], { type: "text/csv;charset=utf-8;" });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = "payroll-" + byId("epRunYear").value + "-" + byId("epRunMonth").value + ".csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        message("تم تجهيز ملف التصدير.");
    }
    function setText(id, value) {
        var el = byId(id);
        if (el) { el.textContent = value; }
    }
    function statusClass(value) {
        return String(value || "Draft").toLowerCase().replace(/\s+/g, "-");
    }
    function lifecycleLabel(value) {
        value = value || "Draft";
        if (value === "Active") { return "نشط"; }
        if (value === "Draft") { return "مسودة"; }
        if (value === "Pending Approval") { return "بانتظار الاعتماد"; }
        if (value === "Suspended") { return "موقوف"; }
        if (value === "Expired") { return "منتهي"; }
        if (value === "Cancelled") { return "ملغي"; }
        return value;
    }
    function updateLifecycleBadge() {
        var status = byId("epPlanLifecycleStatus") ? byId("epPlanLifecycleStatus").value : "Draft";
        var badge = byId("epLifecycleBadge");
        var heroBadge = byId("epHeroLifecycleBadge");
        if (badge) {
            badge.textContent = lifecycleLabel(status);
            badge.className = "ep-status-badge " + statusClass(status);
        }
        if (heroBadge) {
            heroBadge.textContent = lifecycleLabel(status);
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
        var cost = number(byId("epPlanMonthlyCost") ? byId("epPlanMonthlyCost").value : 0);
        var employeeShare = calcShare(cost, byId("epPlanEmployeeShareType").value, number(byId("epPlanEmployeeShareValue").value));
        employeeShare = Math.max(0, Math.min(cost, employeeShare || 0));
        var companyShare = calcShare(cost, byId("epPlanCompanyShareType").value, number(byId("epPlanCompanyShareValue").value));
        if (companyShare === null) { companyShare = cost - employeeShare; }
        companyShare = Math.max(0, Math.min(cost, companyShare || 0));
        if (employeeShare + companyShare > cost) { companyShare = Math.max(0, cost - employeeShare); }
        var showInPayroll = !byId("epPlanShowInPayroll") || byId("epPlanShowInPayroll").checked;
        if (start && end && new Date(end) < new Date(start)) { errors.push("لا يمكن أن ينتهي التأمين قبل تاريخ البداية."); }
        if (enterpriseDependents.length > maxDependents) { errors.push("عدد التابعين يتجاوز الحد الأقصى المحدد للخطة."); }
        if (status === "Active" && (!byId("epPlanProvider").value || !byId("epPlanNameAr").value)) { errors.push("تفعيل الخطة يتطلب شركة تأمين واسم خطة."); }
        if (number(byId("epPlanMonthlyCost") ? byId("epPlanMonthlyCost").value : 0) <= 0 && status === "Active") { errors.push("تفعيل الخطة يتطلب تكلفة شهرية أكبر من صفر."); }
        if (status === "Active" && showInPayroll && (employeeShare > 0 || companyShare > 0)) {
            setText("epPlanAccountingState", "سيقوم النظام بتهيئة حسابات التأمين تلقائيا عند الحفظ، ثم يعرضها بأسماء واضحة في معاينة قيد الرواتب.");
        }
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
                cards.insertAdjacentHTML("beforeend", '<article class="ep-dependent-card"><div><strong>' + html(x.Name) + '</strong><span>' + html(relationLabel(x.Relation)) + '</span></div><div class="ep-dependent-metrics"><span>' + age + ' سنة</span><span>' + html(x.CoveragePercent) + '%</span><span>' + formatMoney(cost) + '</span></div><button type="button" title="حذف التابع" data-remove-dependent="' + i + '"><i class="fas fa-trash"></i></button></article>');
            }
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.Name) + '</td><td>' + html(relationLabel(x.Relation)) + '</td><td>' + age + '</td><td>' + html(x.CoveragePercent) + '%</td><td>' + formatMoney(cost) + '</td><td><button type="button" data-remove-dependent="' + i + '"><i class="fas fa-trash"></i></button></td></tr>');
        });
        if (cards && !cards.innerHTML) { cards.innerHTML = '<div class="ep-empty-card">لا توجد بيانات تابعين في معاينة هذه الخطة.</div>'; }
        setText("epHeroDependents", enterpriseDependents.length);
        validateEnterpriseInsurance();
    }
    function renderEmployeeInsuranceDependents() {
        var host = byId("epEmployeeDependentsCards");
        var count = byId("epEmployeeDependentsCount");
        if (!host) { return; }
        host.innerHTML = "";
        if (count) { count.textContent = employeeInsuranceDependents.length; }
        if (!employeeInsuranceDependents.length) {
            host.innerHTML = '<div class="ep-empty-card">لا يوجد تابعون مسجلون لهذا الموظف. أضف الزوج/الزوجة أو الأبناء هنا، وليس داخل تعريف الخطة.</div>';
            return;
        }
        employeeInsuranceDependents.forEach(function (x, i) {
            var age = ageFromDate(x.BirthDate);
            host.insertAdjacentHTML("beforeend",
                '<article class="ep-dependent-card ep-employee-dependent-card">' +
                '<div><strong>' + html(x.DependentName || x.Name || "") + '</strong><span>' + html(relationLabel(x.Relation)) + '</span></div>' +
                '<div class="ep-dependent-metrics"><span>' + age + ' سنة</span><span>' + html(x.CoveragePercent || 100) + '%</span></div>' +
                '<button type="button" title="حذف التابع من اشتراك الموظف" data-remove-employee-dependent="' + i + '"><i class="fas fa-trash"></i></button>' +
                '</article>');
        });
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
                cards.insertAdjacentHTML("beforeend", '<article class="ep-rule-card"><div class="ep-rule-line"><span>إذا</span><strong>' + html(x.Scope) + '</strong><em>=</em><strong>' + html(x.Value) + '</strong></div><div class="ep-rule-line then"><span>فإن</span><strong>تحمل الشركة</strong><em>=</em><strong>' + html(x.CompanyPercent) + '%</strong></div><p>' + html(x.Description || "") + '</p></article>');
            }
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.Scope) + '</td><td>' + html(x.Value) + '</td><td>' + html(x.CompanyPercent) + '%</td><td>' + html(x.Description) + '</td></tr>');
        });
        if (cards && !cards.innerHTML) { cards.innerHTML = '<div class="ep-empty-card">لا توجد قواعد تحمل تلقائية حتى الآن.</div>'; }
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
                providerHost.insertAdjacentHTML("beforeend", '<article class="ep-provider-mini-card" data-provider="' + html(x.ProviderId) + '"><div><strong>' + html(x.ProviderNameAr || "شركة تأمين") + '</strong><span>' + html(x.Phone ? ("هاتف: " + x.Phone) : "اضغط لعرض بيانات الشركة") + '</span></div><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></article>');
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
                planHost.insertAdjacentHTML("beforeend", '<article class="ep-plan-master-card" data-plan="' + html(x.PlanId) + '"><div class="ep-plan-card-top"><span class="ep-status-badge ' + statusClass(lifecycle) + '">' + html(lifecycleLabel(lifecycle)) + '</span><button type="button" data-plan="' + html(x.PlanId) + '"><i class="fas fa-pen"></i></button></div><h3>' + html(x.PlanNameAr || "خطة تأمين") + '</h3><p>' + html(x.ProviderName || "بدون شركة") + '</p><dl><div><dt>التكلفة الشهرية</dt><dd>' + formatMoney(x.DefaultMonthlyCost) + '</dd></div><div><dt>خصم الموظف</dt><dd>' + html(shareTypeLabel(x.DefaultEmployeeShareType)) + ' ' + money(x.DefaultEmployeeShareValue) + '</dd></div><div><dt>تحمل الشركة</dt><dd>' + html(shareTypeLabel(x.DefaultCompanyShareType)) + ' ' + money(x.DefaultCompanyShareValue) + '</dd></div></dl></article>');
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
            tbody.insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + ' - ' + html(x.EmployeeName) + '</td><td>' + html(x.ProviderName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html(dateInput(x.StartDate)) + '</td><td>' + html(dateInput(x.EndDate)) + '</td><td>' + formatMoney(x.MonthlyCost) + '</td><td>' + formatMoney(x.EmployeeMonthlyDeduction) + '</td><td>' + formatMoney(x.CompanyMonthlyCost) + '</td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "منتهي") + '</span></td></tr>');
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
        setText("epPlanAccountingState", (x.EmployeeDeductionAccountCode || x.CompanyCostAccountCode)
            ? "الحسابات المحاسبية مهيأة لهذه الخطة وسيتم عرضها في معاينة قيد الرواتب بأسماء الحسابات."
            : "سيتم إنشاء الحسابات المطلوبة تلقائيا عند حفظ الخطة إذا كانت نشطة وتظهر في المسير.");
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
                byId("epPlansRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.PlanNameAr) + '</td><td>' + html(x.ProviderName) + '</td><td>' + formatMoney(x.DefaultMonthlyCost) + '</td><td>' + html(shareTypeLabel(x.DefaultEmployeeShareType)) + " " + money(x.DefaultEmployeeShareValue) + '</td><td>' + html(shareTypeLabel(x.DefaultCompanyShareType)) + " " + money(x.DefaultCompanyShareValue) + '</td><td><span class="ep-status-badge ' + statusClass(lifecycle) + '">' + html(lifecycleLabel(lifecycle)) + '</span></td><td><span class="ep-pill ' + (x.IsActive ? "" : "off") + '">' + (x.IsActive ? "نشط" : "متوقف") + '</span></td><td><button type="button" title="تعديل" data-plan="' + x.PlanId + '"><i class="fas fa-edit"></i></button></td></tr>');
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

    function insuranceText(value) {
        var map = {
            "Active": "نشط",
            "Draft": "مسودة",
            "Pending Approval": "بانتظار الاعتماد",
            "Suspended": "موقوف مؤقتا",
            "Expired": "منتهي",
            "Cancelled": "ملغي",
            "Renewal Due": "تجديد قريب",
            "Employee": "الموظف",
            "Company": "الشركة",
            "Monthly Cost": "التكلفة الشهرية",
            "Annual Cost": "التكلفة السنوية",
            "Debit": "مدين",
            "Credit": "دائن",
            "Payroll-linked": "مرتبط بالمسير",
            "Review": "تحت المراجعة",
            "Linked": "مرتبط",
            "Renewal": "التجديد",
            "Payroll": "المسير",
            "Family": "التابعون",
            "Coverage": "التغطية",
            "Provider": "شركة التأمين",
            "Plan": "الخطة",
            "Dependent": "تابع",
            "Open": "مفتوح",
            "overdue": "متأخر",
            "family": "تابعين"
        };
        return map[value] || value || "";
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
                '<article class="ep-med-journal-line"><div><span>' + html(insuranceText(x.Step)) + '</span><strong>' + formatMoney(x.Amount) + '</strong></div><dl><div><dt>مدين</dt><dd>' + html(x.DebitAccount) + '</dd></div><div><dt>دائن</dt><dd>' + html(x.CreditAccount) + '</dd></div></dl><p>' + html(x.Explanation) + '</p></article>');
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
            '<div class="ep-med-id-top"><div class="ep-med-brand"><span>التأمين الطبي</span><strong>بطاقة الاشتراك</strong></div><div class="ep-med-provider-logo">' + html((row.ProviderName || "MI").substring(0, 2)) + '</div></div>' +
            '<div class="ep-med-id-main"><div class="ep-med-avatar">' + photoMarkup(row.PhotoDataUrl, row.EmployeeName, "ep-med-avatar-img") + '</div><div><h3>' + html(row.EmployeeName || "موظف") + '</h3><p>' + html(row.EmployeeCode || "") + ' - ' + html(row.MembershipNumber || "") + '</p><b class="ep-med-badge ' + status + '">' + medStatusLabel(row.Status) + '</b></div></div>' +
            '<div class="ep-med-id-plan"><span>' + html(row.ProviderName || "شركة التأمين") + '</span><strong>' + html(row.PlanName || "الخطة") + '</strong></div>' +
            '<dl class="ep-med-id-metrics"><div><dt>التجديد</dt><dd>' + html(dateInput(row.RenewalDate || row.EndDate) || "مفتوح") + '</dd></div><div><dt>المسير</dt><dd>' + (row.PayrollLinked ? "مرتبط" : "تحت المراجعة") + '</dd></div><div><dt>التابعون</dt><dd>' + html(row.DependentsCount || 0) + '</dd></div></dl>' +
            '<div class="ep-med-id-footer"><span>الخصم: الموظف ' + formatMoney(row.EmployeeMonthlyDeduction) + ' / الشركة ' + formatMoney(row.CompanyMonthlyCost) + '</span><em>QR</em></div>' +
            '</article>';
        if (depHost) {
            depHost.innerHTML = "";
            (row.Dependents || []).forEach(function (x) {
                depHost.insertAdjacentHTML("beforeend",
                    '<article class="ep-med-dependent-card"><i class="fas fa-user"></i><div><strong>' + html(x.Name || "تابع") + '</strong><span>' + html(insuranceText(x.Relation) || "تابع") + ' - ' + html(x.Age || 0) + ' سنة - ' + formatMoney(x.CoveragePercent).replace(".00", "") + '%</span></div><b>' + (x.IsActive ? "نشط" : "غير نشط") + '</b></article>');
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
        if (!host.innerHTML) { host.innerHTML = '<div class="ep-empty-card">لا يوجد توزيع تكلفة حتى الآن.</div>'; }
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
            if (x.PayrollLinked) { flags.push('<span><i class="fas fa-link"></i> مرتبط بالمسير</span>'); }
            if (x.DependentsCount) { flags.push('<span><i class="fas fa-users"></i> ' + html(x.DependentsCount) + ' تابع</span>'); }
            if (x.OverdueInstallments) { flags.push('<span class="danger"><i class="fas fa-bell"></i> ' + html(x.OverdueInstallments) + ' قسط متأخر</span>'); }
            host.insertAdjacentHTML("beforeend",
                '<article class="ep-med-employee-card ' + status + (index === 0 ? ' selected' : '') + '" data-med-employee="' + index + '">' +
                '<div class="ep-med-employee-head"><div><span>' + html(x.EmployeeCode || "") + '</span><h3>' + html(x.EmployeeName || "موظف") + '</h3></div><b class="ep-med-badge ' + status + '">' + medStatusLabel(x.Status) + '</b></div>' +
                '<div class="ep-med-provider"><i class="fas fa-hospital"></i><div><strong>' + html(x.ProviderName || "غير محدد") + '</strong><span>' + html(x.PlanName || "خطة غير محددة") + '</span></div></div>' +
                '<dl class="ep-med-card-metrics"><div><dt>خصم الموظف</dt><dd>' + formatMoney(x.EmployeeMonthlyDeduction) + '</dd></div><div><dt>تحمل الشركة</dt><dd>' + formatMoney(x.CompanyMonthlyCost) + '</dd></div><div><dt>متأخرات</dt><dd>' + formatMoney(x.OverdueAmount) + '</dd></div></dl>' +
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
            BranchId: byId("epReportBranch") ? byId("epReportBranch").value || null : null,
            DepartmentId: byId("epReportDepartment") ? byId("epReportDepartment").value || null : null,
            EmployeeId: byId("epReportEmployee") ? byId("epReportEmployee").value || null : null,
            ProviderId: byId("epReportProvider").value || null,
            PlanId: byId("epReportPlan").value || null,
            Status: byId("epReportStatus") ? byId("epReportStatus").value || null : null,
            PostingStatus: byId("epReportPostingStatus") ? byId("epReportPostingStatus").value || null : null,
            ActiveOnly: byId("epReportActiveOnly").checked
        };
    }
    function statusText(value) {
        if (value === "Active") { return "نشط"; }
        if (value === "Expired") { return "منتهي"; }
        if (value === "Cancelled") { return "ملغي/متوقف"; }
        return value || "";
    }
    function renderEmpty(tbodyId, columns) {
        var body = byId(tbodyId);
        if (body && !body.children.length) {
            body.innerHTML = '<tr><td colspan="' + columns + '" class="ep-empty-row">لا توجد بيانات مطابقة للفلاتر.</td></tr>';
        }
    }
    function setReportTotals(report) {
        setText("epReportSubscriptionCount", report.SubscriptionCount || 0);
        setText("epReportActiveCount", report.ActiveCount || 0);
        setText("epReportExpiredCount", report.ExpiredCount || 0);
        setText("epReportCancelledCount", report.CancelledCount || 0);
        setText("epReportEmployeeTotal", money(report.TotalEmployeeDeduction));
        setText("epReportCompanyTotal", money(report.TotalCompanyCost));
        setText("epReportPayableTotal", money(report.TotalPayable));
        setText("epReportParamsText", report.PeriodLabel || "كل الفترات");
    }
    function renderMedicalReportBundle(report) {
        report = report || {};
        root._medicalReport = report;
        setReportTotals(report);
        byId("epSubscriptionsReportRows").innerHTML = "";
        (report.Subscriptions || []).forEach(function (x) {
            byId("epSubscriptionsReportRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.BranchName) + '</td><td>' + html(x.DepartmentName) + '</td><td>' + html(x.ProviderName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html(x.PolicyNumber) + '</td><td>' + html(x.CardNumber) + '</td><td>' + html(dateInput(x.StartDate)) + '</td><td>' + html(dateInput(x.EndDate)) + '</td><td>' + money(x.MonthlyCost) + '</td><td>' + money(x.EmployeeMonthlyDeduction) + '</td><td>' + money(x.CompanyMonthlyCost) + '</td><td><span class="ep-status-badge ' + statusClass(x.Status) + '">' + statusText(x.Status) + '</span></td></tr>');
        });
        renderEmpty("epSubscriptionsReportRows", 13);
        var statuses = [
            { name: "نشط", rows: (report.Subscriptions || []).filter(function (x) { return x.Status === "Active"; }) },
            { name: "منتهي", rows: (report.Subscriptions || []).filter(function (x) { return x.Status === "Expired"; }) },
            { name: "ملغي/متوقف", rows: (report.Subscriptions || []).filter(function (x) { return x.Status === "Cancelled"; }) }
        ];
        byId("epStatusReportRows").innerHTML = "";
        statuses.forEach(function (group) {
            byId("epStatusReportRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(group.name) + '</td><td>' + group.rows.length + '</td><td>' + money(group.rows.reduce(function (s, x) { return s + number(x.MonthlyCost); }, 0)) + '</td><td>' + money(group.rows.reduce(function (s, x) { return s + number(x.EmployeeMonthlyDeduction); }, 0)) + '</td><td>' + money(group.rows.reduce(function (s, x) { return s + number(x.CompanyMonthlyCost); }, 0)) + '</td></tr>');
        });
        byId("epDeductionsReportRows").innerHTML = "";
        (report.MonthlyDeductions || []).forEach(function (x) {
            byId("epDeductionsReportRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html(x.BranchName) + '</td><td>' + html(x.DepartmentName) + '</td><td>' + html(x.ProviderName) + '</td><td>' + html(x.PlanName) + '</td><td>' + html((x.Year || "") + "/" + (x.Month || "")) + '</td><td>' + money(x.EmployeeDeduction) + '</td><td>' + money(x.CompanyCost) + '</td><td><span class="ep-status-badge ' + (x.IsPosted ? "posted" : "unposted") + '">' + html(x.PostingStatus) + '</span></td></tr>');
        });
        renderEmpty("epDeductionsReportRows", 9);
        byId("epCompanyContributionRows").innerHTML = "";
        (report.CompanyContributions || []).forEach(function (x) {
            byId("epCompanyContributionRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.GroupName) + '</td><td>' + html(x.Employees) + '</td><td>' + money(x.EmployeeDeduction) + '</td><td>' + money(x.CompanyCost) + '</td><td>' + money(x.TotalPayable) + '</td></tr>');
        });
        renderEmpty("epCompanyContributionRows", 5);
        byId("epPayableSummaryRows").innerHTML = "";
        (report.Payables || []).forEach(function (x) {
            byId("epPayableSummaryRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.ProviderName) + '</td><td>' + html(x.AccountDisplay || x.AccountName || x.AccountSerial || "غير محدد") + '</td><td>' + money(x.EmployeeDeduction) + '</td><td>' + money(x.CompanyCost) + '</td><td>' + money(x.PayrollPayable) + '</td><td>' + money(x.PostedNetCredit) + '</td><td>' + money(x.Difference) + '</td></tr>');
        });
        renderEmpty("epPayableSummaryRows", 7);
        byId("epPayrollIntegrationRows").innerHTML = "";
        (report.PayrollIntegration || []).forEach(function (x) {
            byId("epPayrollIntegrationRows").insertAdjacentHTML("beforeend", '<tr><td>' + html(x.EmployeeCode) + " - " + html(x.EmployeeName) + '</td><td>' + html((x.Year || "") + "/" + (x.Month || "")) + '</td><td>' + html(x.ProviderName) + '</td><td>' + money(x.EmployeeDeduction) + '</td><td>' + money(x.CompanyCost) + '</td><td>' + money(x.PayrollNetSalary) + '</td><td><span class="ep-status-badge ' + (x.IsPosted ? "posted" : "unposted") + '">' + html(x.PostingStatus) + '</span></td><td>' + html(x.NoteId || "") + '</td><td>' + money(x.JournalDebit) + '</td><td>' + money(x.JournalCredit) + '</td><td>' + money(x.JournalBalance) + '</td></tr>');
        });
        renderEmpty("epPayrollIntegrationRows", 11);
    }
    function exportMedicalReportCsv() {
        var report = root._medicalReport || {};
        var lines = [["التقرير", "الموظف", "شركة التأمين", "الخطة", "الفترة", "خصم الموظف", "تحمل الشركة", "صافي الراتب", "حالة الترحيل", "رقم القيد"].join(",")];
        (report.MonthlyDeductions || []).forEach(function (x) {
            lines.push(["MonthlyDeductions", '"' + (x.EmployeeCode || "") + " - " + (x.EmployeeName || "") + '"', '"' + (x.ProviderName || "") + '"', '"' + (x.PlanName || "") + '"', (x.Year || "") + "/" + (x.Month || ""), money(x.EmployeeDeduction), money(x.CompanyCost), "", '"' + (x.PostingStatus || "") + '"', ""].join(","));
        });
        (report.PayrollIntegration || []).forEach(function (x) {
            lines.push(["PayrollIntegration", '"' + (x.EmployeeCode || "") + " - " + (x.EmployeeName || "") + '"', '"' + (x.ProviderName || "") + '"', '"' + (x.PlanName || "") + '"', (x.Year || "") + "/" + (x.Month || ""), money(x.EmployeeDeduction), money(x.CompanyCost), money(x.PayrollNetSalary), '"' + (x.PostingStatus || "") + '"', x.NoteId || ""].join(","));
        });
        var blob = new Blob(["\ufeff" + lines.join("\r\n")], { type: "text/csv;charset=utf-8" });
        var a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "medical-insurance-reports.csv";
        a.click();
        URL.revokeObjectURL(a.href);
    }
    function loadReports() {
        var q = queryString(reportFilter());
        var url = root.getAttribute("data-report-bundle-url") || root.getAttribute("data-subscriptions-url");
        return getJson(url + "?" + q).then(function (res) {
            if (!res.success) { message(res.message || "تعذر تحميل تقارير التأمين الطبي.", true); return; }
            renderMedicalReportBundle(res.report || {});
            message("تم تحميل تقارير التأمين الطبي.");
        });
    }

    if (readOnly && byId("epEmployeesRows") && window.MutationObserver) {
        new MutationObserver(enforceReadOnlyRows).observe(byId("epEmployeesRows"), { childList: true, subtree: true });
    }

    loadLookups().then(function () {
        if (screen === "employees") {
            setText("epEmployeeLoadState", "بانتظار البحث");
            message("اكتب حرفين على الأقل أو اختر فلتر لعرض الموظفين بسرعة.");
        }
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
        var employeeRow = e.target.closest("[data-employee-row]");
        if (screen === "employees" && employeeRow && !btn) {
            var employeeIndex = parseInt(employeeRow.getAttribute("data-employee-row"), 10);
            root.querySelectorAll("[data-employee-row]").forEach(function (x) { x.classList.toggle("selected", x === employeeRow); });
            renderEmployeeProfile((root._employeeRows || [])[employeeIndex]);
            return;
        }
        if (!btn) { return; }
        if (btn.hasAttribute("data-run-tab")) {
            var tab = btn.getAttribute("data-run-tab");
            root.querySelectorAll("[data-run-tab]").forEach(function (x) { x.classList.toggle("active", x === btn); });
            root.querySelectorAll("[data-run-panel]").forEach(function (x) { x.hidden = x.getAttribute("data-run-panel") !== tab; });
            return;
        }
        if (btn.hasAttribute("data-payroll-row")) {
            root.querySelectorAll("[data-run-tab]").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-run-tab") === "variables"); });
            root.querySelectorAll("[data-run-panel]").forEach(function (x) { x.hidden = x.getAttribute("data-run-panel") !== "variables"; });
            renderPayrollDetail((root._salaryRows || [])[parseInt(btn.getAttribute("data-payroll-row"), 10)]);
            return;
        }
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
        if (btn.id === "epSearchBtn") { guardedAction("searchEmployees", btn, "جار البحث...", loadEmployees); return; }
        if (btn.id === "epNewEmployee") {
            if (readOnly) { message("POS operational view only. Manage employees from MainErp.", true); return; }
            openEditor();
        }
        if (btn.id === "epPrintEmployeeCard" || btn.id === "epProfilePrintEmployeeCard") { printEmployeeCard("employee"); return; }
        if (btn.id === "epPrintEmployeeInsuranceCard" || btn.id === "epProfilePrintInsuranceCard") { printEmployeeCard("insurance"); return; }
        if (btn.id === "epRemovePhoto") { setEmployeePhoto("", byId("epName") ? byId("epName").value : ""); return; }
        if (btn.id === "epCloseEditor") { byId("epEmployeeEditor").hidden = true; }
        if (btn.id === "epEndInsuranceSubscription") {
            if (!byId("epInsuranceActive") || !byId("epInsuranceEnd")) { return; }
            byId("epInsuranceActive").checked = false;
            if (!byId("epInsuranceEnd").value) { byId("epInsuranceEnd").value = dateInput(new Date()); }
            updateInsurancePreview();
            message("تم تجهيز إنهاء الاشتراك. اضغط حفظ الملف لتطبيق الإيقاف بدون حذف البيانات السابقة.");
            return;
        }
        if (btn.id === "epPreviewRun" || btn.id === "epPreviewRunSticky") { guardedAction("previewSalary", btn, btn.getAttribute("data-busy-text") || "جار الحساب...", previewSalary); return; }
        if (btn.id === "epLoadParity") { message("فحص التوافق التفصيلي متاح من شاشة الإدارة الرئيسية، ويمكن استخدام فحص قبل الترحيل هنا لمراجعة الحسابات.", false); return; }
        if (btn.id === "epLoadReplay") { guardedAction("postingDryRun", btn, btn.getAttribute("data-busy-text") || "جار المراجعة...", postingDryRun); return; }
        if (btn.id === "epPostingDryRun") { guardedAction("postingDryRun", btn, btn.getAttribute("data-busy-text") || "جار الفحص...", postingDryRun); return; }
        if (btn.id === "epCompareRuns") { guardedAction("comparePayrollRuns", btn, "جار المقارنة...", comparePayrollRuns); return; }
        if (btn.id === "epPostPayroll") { guardedAction("postPayroll", btn, btn.getAttribute("data-busy-text") || "جار الترحيل...", postPayroll); return; }
        if (btn.id === "epPrevMonth") { setRunMonth(-1); guardedAction("previewSalary", btn, "جار الحساب...", previewSalary); return; }
        if (btn.id === "epCurrentMonth") { setRunMonth("current"); guardedAction("previewSalary", btn, "جار الحساب...", previewSalary); return; }
        if (btn.id === "epNextMonth") { setRunMonth(1); guardedAction("previewSalary", btn, "جار الحساب...", previewSalary); return; }
        if (btn.id === "epPrintRun") { guardedAction("salarySheet", btn, "جار تجهيز التقرير...", function () { return loadSalarySheet().then(function (report) { if (report) { window.print(); } }); }); return; }
        if (btn.id === "epExportRun") { guardedAction("salarySheet", btn, "جار تجهيز التصدير...", function () { return loadSalarySheet().then(function (report) { if (report) { exportSalaryCsv(); } }); }); return; }
        if (btn.id === "epSaveRun") {
            if (readOnly || !root.getAttribute("data-save-url")) { message("حفظ مسير الرواتب غير متاح في هذه الشاشة.", true); return; }
            guardedAction("saveSalary", btn, btn.getAttribute("data-busy-text") || "جار الحفظ...", function () {
                return postJson(root.getAttribute("data-save-url"), salaryRequest()).then(function (res) {
                    if (res.success && res.result && res.result.PayrollRunId && byId("epPayrollRunId")) { byId("epPayrollRunId").value = res.result.PayrollRunId; }
                    message(res.success ? res.result.Message : res.message, !res.success);
                    return previewSalary();
                });
            });
            return;
        }
        if (btn.id === "epLoadReports") { guardedAction("loadReports", btn, "جار تحميل التقرير...", loadReports); return; }
        if (btn.id === "epPrintReports") { guardedAction("printReports", btn, "جار تجهيز الطباعة...", function () { window.print(); }); return; }
        if (btn.id === "epExportReports") { guardedAction("exportReports", btn, "جار تجهيز التصدير...", exportMedicalReportCsv); return; }
        if (btn.id === "epMedRefresh" || btn.id === "epMedSearch") { guardedAction("medicalDashboard", btn, "جار تحميل التأمين...", loadMedOperationalDashboard); return; }
        if (btn.id === "epMedPrintCard") {
            document.body.classList.add("ep-print-medical-card");
            setTimeout(function () {
                window.print();
                setTimeout(function () { document.body.classList.remove("ep-print-medical-card"); }, 300);
            }, 50);
            return;
        }
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
        if (btn.id === "epAddEmployeeDependent") {
            var employeeDependentName = byId("epEmployeeDependentName") ? byId("epEmployeeDependentName").value : "";
            var employeeDependentRelation = byId("epEmployeeDependentRelation") ? byId("epEmployeeDependentRelation").value : "Child";
            var employeeDependentBirthDate = byId("epEmployeeDependentBirthDate") ? byId("epEmployeeDependentBirthDate").value : "";
            var employeeDependentCoverage = byId("epEmployeeDependentCoverage") ? number(byId("epEmployeeDependentCoverage").value) || 100 : 100;
            var selectedPlan = (root._plans || []).filter(function (x) { return String(x.PlanId) === String(byId("epInsurancePlan") ? byId("epInsurancePlan").value : ""); })[0] || {};
            var maxDependentsForEmployee = parseInt(selectedPlan.MaxDependents || "0", 10);
            var maxChildAgeForEmployee = parseInt(selectedPlan.ChildrenMaxAge || "0", 10);
            if (!employeeDependentName) { message("اكتب اسم التابع أولا.", true); return; }
            if (maxDependentsForEmployee > 0 && employeeInsuranceDependents.length >= maxDependentsForEmployee) { message("عدد التابعين يتجاوز الحد الأقصى المسموح في الخطة.", true); return; }
            if (employeeDependentRelation === "Child" && maxChildAgeForEmployee > 0 && ageFromDate(employeeDependentBirthDate) > maxChildAgeForEmployee) { message("عمر الابن/الابنة يتجاوز حد الخطة.", true); return; }
            employeeInsuranceDependents.push({ DependentName: employeeDependentName, Relation: employeeDependentRelation, BirthDate: employeeDependentBirthDate, CoveragePercent: employeeDependentCoverage, IsActive: true });
            if (byId("epEmployeeDependentName")) { byId("epEmployeeDependentName").value = ""; }
            if (byId("epEmployeeDependentBirthDate")) { byId("epEmployeeDependentBirthDate").value = ""; }
            renderEmployeeInsuranceDependents();
            return;
        }
        if (btn.hasAttribute("data-remove-dependent")) {
            enterpriseDependents.splice(parseInt(btn.getAttribute("data-remove-dependent"), 10), 1);
            renderDependents();
        }
        if (btn.hasAttribute("data-remove-employee-dependent")) {
            employeeInsuranceDependents.splice(parseInt(btn.getAttribute("data-remove-employee-dependent"), 10), 1);
            renderEmployeeInsuranceDependents();
            return;
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
            if (readOnly || !root.getAttribute("data-active-url")) { message("تغيير حالة الموظف يتم من الشاشة الرئيسية المصرح بها.", true); return; }
            var activating = btn.getAttribute("data-state") === "true";
            if (!activating && !window.confirm("سيتم تعطيل الموظف بدون حذف أي مسير أو قيد أو اشتراك تأمين سابق. هل تريد المتابعة؟")) { return; }
            guardedAction("setEmployeeActive" + btn.getAttribute("data-active"), btn, activating ? "جار التفعيل..." : "جار التعطيل...", function () {
                return postJson(root.getAttribute("data-active-url"), { id: btn.getAttribute("data-active"), active: activating }).then(function (res) {
                    message(res.message || (res.success ? "تم تحديث حالة الموظف" : "تعذر تحديث الحالة"), !res.success);
                    if (res.success) { return loadEmployees(); }
                });
            });
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
        if (screen === "employees" && e.target && e.target.id === "epPhotoInput") {
            var file = e.target.files && e.target.files[0];
            if (!file) { return; }
            guardedAction("employeePhoto", null, "جاري تجهيز صورة الموظف...", function () {
                return resizeEmployeePhoto(file).then(function (dataUrl) {
                    setEmployeePhoto(dataUrl, byId("epName") ? byId("epName").value : "");
                    message("تم تجهيز صورة الموظف. اضغط حفظ الملف لتثبيتها.");
                }, function (err) {
                    message(err.message || "تعذر تجهيز الصورة.", true);
                });
            });
        }
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
            guardedAction("medicalDashboard", null, "جار تحميل التأمين...", loadMedOperationalDashboard);
        }
        if (screen === "employees" && e.target && (e.target.id === "epBranchFilter" || e.target.id === "epDepartmentFilter" || e.target.id === "epActiveFilter")) {
            guardedAction("searchEmployees", byId("epSearchBtn"), "جار البحث...", loadEmployees);
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
        if (screen === "salary" && e.target && e.target.id === "epSalarySearch") {
            var term = (e.target.value || "").toLowerCase();
            root.querySelectorAll("#epSalaryRows tr[data-payroll-search]").forEach(function (row) {
                row.hidden = term && row.getAttribute("data-payroll-search").indexOf(term) < 0;
            });
        }
    });

    if (screen === "employees") {
        byId("epEmployeeForm").addEventListener("submit", function (e) {
            e.preventDefault();
            if (readOnly || !root.getAttribute("data-save-url")) { message("POS operational view only. Manage employee data from MainErp.", true); return; }
            var employee = collectEmployee();
            if (!validateEmployeeInsuranceForSave(employee)) { return; }
            guardedAction("saveEmployee", e.submitter || byId("epEmployeeForm").querySelector("button[type='submit']"), "جار حفظ ملف الموظف...", function () {
                return postJson(root.getAttribute("data-save-url"), employee).then(function (res) {
                    message(res.message || (res.success ? "تم الحفظ" : "تعذر الحفظ"), !res.success);
                    if (res.success) {
                        byId("epEmployeeEditor").hidden = true;
                        return loadEmployees();
                    }
                });
            });
        });
        byId("epSearchTerm").addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); guardedAction("searchEmployees", byId("epSearchBtn"), "جار البحث...", loadEmployees); } });
        byId("epSearchTerm").addEventListener("input", debounce(function () {
            if (hasEmployeeSearchCriteria()) { guardedAction("searchEmployees", byId("epSearchBtn"), "جار البحث...", loadEmployees); }
        }, 450));
    }

    if (screen === "insurance-settings") {
        byId("epProviderForm").addEventListener("submit", function (e) {
            e.preventDefault();
            guardedAction("saveProvider", e.submitter || byId("epProviderForm").querySelector("button[type='submit']"), "جار حفظ شركة التأمين...", function () {
                return postJson(root.getAttribute("data-save-provider-url"), collectProvider()).then(function (res) {
                    message(res.message || "تم الحفظ", !res.success);
                    if (res.success) { byId("epProviderForm").reset(); byId("epProviderId").value = ""; closeDrawers(); return loadInsuranceSettings(); }
                });
            });
        });
        byId("epPlanForm").addEventListener("submit", function (e) {
            e.preventDefault();
            if (!validateEnterpriseInsurance()) { message("راجع تنبيهات الخطة قبل الحفظ", true); return; }
            guardedAction("savePlan", e.submitter || byId("epPlanForm").querySelector("button[type='submit']"), "جار حفظ خطة التأمين...", function () {
                return postJson(root.getAttribute("data-save-plan-url"), collectPlan()).then(function (res) {
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
                        return loadInsuranceSettings();
                    }
                });
            });
        });
    }
    if (screen === "insurance-operational" && byId("epMedTerm")) {
        byId("epMedTerm").addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                guardedAction("medicalDashboard", byId("epMedSearch"), "جار تحميل التأمين...", loadMedOperationalDashboard);
            }
        });
    }
})();

