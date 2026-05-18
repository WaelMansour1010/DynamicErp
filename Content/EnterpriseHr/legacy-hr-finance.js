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
    function close() { byId("lhfComponentEditor").hidden = true; }
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
    function today() {
        var d = new Date();
        return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
    }
    function setAdvanceMessage(text, isError) {
        var el = byId("lhfAdvanceMessage");
        if (!el) { return; }
        el.textContent = text || "";
        el.className = isError ? "lhf-message-error" : "";
    }
    function updateAdvancePreview() {
        var employee = byId("lhfAdvanceEmployeeId");
        var amount = parseFloat(val("lhfAdvanceValue") || "0");
        var count = parseInt(val("lhfPaymentCounts") || "0", 10);
        var selected = employee && employee.options[employee.selectedIndex];
        var meta = byId("lhfAdvanceEmployeeMeta");
        var preview = byId("lhfAdvancePartsPreview");
        if (meta && selected && selected.value) {
            meta.textContent = "الفرع: " + (selected.getAttribute("data-branch") || "-") + " | الإدارة: " + (selected.getAttribute("data-department") || "-") + " | الراتب الأساسي: " + (selected.getAttribute("data-salary") || "0");
        }
        if (preview) {
            preview.textContent = amount > 0 && count > 0 ? ("قيمة القسط التقريبية: " + (amount / count).toFixed(2)) : "سيتم حساب الأقساط عند الحفظ.";
        }
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
        setAdvanceMessage("");
        updateAdvancePreview();
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
    document.addEventListener("click", function (event) {
        if (event.target.closest("[data-close-component]")) { close(); return; }
        if (event.target.closest("[data-new-component]")) { show({ AddOrDiscount: true, ViewComponent: true, Salary: true }); return; }
        if (event.target.closest("[data-close-advance]")) { closeAdvance(); return; }
        if (event.target.closest("[data-new-advance]")) { showAdvance({ AutoDiscount: true, PaymentCounts: 1 }); return; }
        var editAdvance = event.target.closest("[data-edit-advance]");
        if (editAdvance) {
            fetch(root.getAttribute("data-advance-details-url") + "?id=" + encodeURIComponent(editAdvance.getAttribute("data-edit-advance")), { credentials: "same-origin" })
                .then(function (r) { return r.json(); })
                .then(function (res) { if (res.success) { showAdvance(res.data); } else { alert(res.message || "تعذر تحميل طلب السلفة"); } });
            return;
        }
        var deleteAdvance = event.target.closest("[data-delete-advance]");
        if (deleteAdvance) {
            if (!confirm("هل تريد حذف طلب السلفة؟")) { return; }
            deleteAdvance.disabled = true;
            fetch(root.getAttribute("data-delete-advance-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
                body: new URLSearchParams({ id: deleteAdvance.getAttribute("data-delete-advance"), __RequestVerificationToken: token() }).toString()
            }).then(function (r) {
                return r.json().then(function (j) { j._ok = r.ok; return j; });
            }).then(function (res) {
                if (!res.Success && !res.success) {
                    alert(res.Message || res.message || "تعذر حذف طلب السلفة");
                    deleteAdvance.disabled = false;
                    return;
                }
                window.location.reload();
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
            fetch(root.getAttribute("data-save-component-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
                body: new URLSearchParams(collect()).toString()
            }).then(function (r) {
                return r.json().then(function (j) { j._ok = r.ok; return j; });
            }).then(function (res) {
                if (!res.Success && !res.success) {
                    byId("lhfEditorMessage").textContent = res.Message || res.message || "تعذر الحفظ";
                    return;
                }
                window.location.reload();
            });
        });
    }
    ["lhfAdvanceEmployeeId", "lhfAdvanceValue", "lhfPaymentCounts"].forEach(function (id) {
        var el = byId(id);
        if (el) { el.addEventListener("input", updateAdvancePreview); el.addEventListener("change", updateAdvancePreview); }
    });
    var advanceForm = byId("lhfAdvanceForm");
    if (advanceForm) {
        advanceForm.addEventListener("submit", function (event) {
            event.preventDefault();
            var submit = advanceForm.querySelector("button[type='submit']");
            if (submit) { submit.disabled = true; }
            setAdvanceMessage("جار الحفظ...");
            fetch(root.getAttribute("data-save-advance-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
                body: new URLSearchParams(collectAdvance()).toString()
            }).then(function (r) {
                return r.json().then(function (j) { j._ok = r.ok; return j; });
            }).then(function (res) {
                if (!res.Success && !res.success) {
                    setAdvanceMessage(res.Message || res.message || "تعذر حفظ طلب السلفة", true);
                    if (submit) { submit.disabled = false; }
                    return;
                }
                setAdvanceMessage(res.Message || "تم الحفظ");
                window.location.reload();
            }).catch(function () {
                setAdvanceMessage("تعذر الاتصال بالخادم.", true);
                if (submit) { submit.disabled = false; }
            });
        });
    }
}());
