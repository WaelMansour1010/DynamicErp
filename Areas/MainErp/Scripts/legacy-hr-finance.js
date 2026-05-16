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
    document.addEventListener("click", function (event) {
        if (event.target.closest("[data-close-component]")) { close(); return; }
        if (event.target.closest("[data-new-component]")) { show({ AddOrDiscount: true, ViewComponent: true, Salary: true }); return; }
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
}());
