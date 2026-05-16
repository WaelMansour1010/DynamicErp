(function () {
    "use strict";

    var root = document.getElementById("branchesPage");
    if (!root) { return; }

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var statusBox = document.getElementById("branchStatus");
    var deleteBtn = document.getElementById("branchDeleteBtn");

    function byId(id) { return document.getElementById(id); }
    function value(id) {
        var el = byId(id);
        return el ? el.value : "";
    }
    function setValue(id, val) {
        var el = byId(id);
        if (el) { el.value = val == null ? "" : val; }
    }
    function showStatus(message, kind) {
        statusBox.hidden = false;
        statusBox.className = "branches-status " + (kind || "info");
        statusBox.textContent = message || "";
    }
    function post(url, data) {
        if (token) { data.__RequestVerificationToken = token.value; }
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
            body: new URLSearchParams(data).toString()
        }).then(function (r) { return r.json(); });
    }
    function get(url) {
        return fetch(url, { credentials: "same-origin" }).then(function (r) { return r.json(); });
    }
    function collect() {
        var data = {
            BranchId: value("BranchId"),
            NameAr: value("NameAr"),
            NameEn: value("NameEn"),
            Phone: value("Phone"),
            Address: value("Address"),
            ManagerId: value("ManagerId"),
            ActivityTypeId: value("ActivityTypeId")
        };
        Array.prototype.forEach.call(root.querySelectorAll(".account-field"), function (input) {
            data["Accounts[" + input.getAttribute("data-field") + "]"] = input.value || "";
        });
        return data;
    }
    function apply(data) {
        data = data || {};
        setValue("BranchId", data.BranchId);
        setValue("NameAr", data.NameAr);
        setValue("NameEn", data.NameEn);
        setValue("Phone", data.Phone);
        setValue("Address", data.Address);
        setValue("ManagerId", data.ManagerId);
        setValue("ActivityTypeId", data.ActivityTypeId);
        Array.prototype.forEach.call(root.querySelectorAll(".account-field"), function (input) {
            var field = input.getAttribute("data-field");
            input.value = data.Accounts && data.Accounts[field] ? data.Accounts[field] : "";
        });
        byId("branchBadge").textContent = data.BranchId || "جديد";
        byId("branchTitle").textContent = data.BranchId ? "تعديل فرع" : "فرع جديد";
        if (deleteBtn) { deleteBtn.disabled = !data.BranchId; }
    }

    root.addEventListener("click", function (event) {
        var row = event.target.closest(".branch-row");
        if (row) {
            get(root.dataset.detailsUrl + "?id=" + encodeURIComponent(row.dataset.id)).then(function (res) {
                if (!res.success) { showStatus(res.message || "تعذر تحميل الفرع", "error"); return; }
                apply(res.data);
                showStatus("تم تحميل بيانات الفرع", "success");
            }).catch(function (err) { showStatus(err.message, "error"); });
            return;
        }

        var tab = event.target.closest(".branches-tabs button");
        if (tab) {
            Array.prototype.forEach.call(root.querySelectorAll(".branches-tabs button"), function (x) { x.classList.remove("active"); });
            Array.prototype.forEach.call(root.querySelectorAll(".branches-account-panel"), function (x) { x.classList.remove("active"); });
            tab.classList.add("active");
            var panel = root.querySelector('[data-panel="' + tab.getAttribute("data-category") + '"]');
            if (panel) { panel.classList.add("active"); }
        }
    });

    byId("branchNewBtn").addEventListener("click", function () {
        get(root.dataset.newUrl).then(function (res) {
            if (!res.success) { showStatus(res.message || "تعذر بدء فرع جديد", "error"); return; }
            apply(res.data);
            showStatus("جاهز لإضافة فرع جديد", "info");
        }).catch(function (err) { showStatus(err.message, "error"); });
    });

    byId("branchSaveBtn").addEventListener("click", function () {
        if (!value("NameAr").trim()) {
            showStatus("اسم الفرع مطلوب قبل الحفظ", "error");
            return;
        }
        post(root.dataset.saveUrl, collect()).then(function (res) {
            if (!res.Success && !res.success) { showStatus(res.Message || res.message || "تعذر الحفظ", "error"); return; }
            showStatus(res.Message || res.message || "تم الحفظ", "success");
            if (res.Id || res.id) { setValue("BranchId", res.Id || res.id); byId("branchBadge").textContent = res.Id || res.id; }
        }).catch(function (err) { showStatus(err.message, "error"); });
    });

    deleteBtn.addEventListener("click", function () {
        var id = value("BranchId");
        if (!id || !confirm("هل أنت متأكد من حذف الفرع؟")) { return; }
        post(root.dataset.deleteUrl, { id: id }).then(function (res) {
            if (!res.Success && !res.success) { showStatus(res.Message || res.message || "تعذر الحذف", "error"); return; }
            showStatus(res.Message || res.message || "تم الحذف", "success");
        }).catch(function (err) { showStatus(err.message, "error"); });
    });

    var search = byId("branchAccountSearch");
    if (search) {
        search.addEventListener("input", function () {
            var term = search.value.trim().toLowerCase();
            Array.prototype.forEach.call(root.querySelectorAll("[data-account-field]"), function (label) {
                label.style.display = !term || label.textContent.toLowerCase().indexOf(term) >= 0 ? "" : "none";
            });
        });
    }
}());
