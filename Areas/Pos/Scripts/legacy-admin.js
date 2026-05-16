(function () {
    "use strict";

    function byId(id) { return document.getElementById(id); }

    function status(id, text, type) {
        var el = byId(id);
        if (!el) { return; }
        el.hidden = false;
        el.textContent = text || "";
        el.className = "legacy-status " + (type || "info");
    }

    function antiForgery(root) {
        var token = root.querySelector("input[name='__RequestVerificationToken']");
        return token ? token.value : "";
    }

    function postForm(url, form, token) {
        var data = new FormData(form);
        data.append("__RequestVerificationToken", token);
        return fetch(url, { method: "POST", body: data, credentials: "same-origin" })
            .then(function (response) {
                return response.json().then(function (json) {
                    if (!response.ok || !json.Success) {
                        throw new Error(json.Message || "تعذر الحفظ");
                    }
                    return json;
                });
            });
    }

    var usersPage = byId("legacyUsersPage");
    if (usersPage) {
        var saveUserBtn = byId("legacyUserSave");
        var deleteUserBtn = byId("legacyUserDelete");
        var userForm = byId("legacyUserForm");

        saveUserBtn.addEventListener("click", function () {
            status("legacyUsersStatus", "جار حفظ المستخدم...", "info");
            postForm(usersPage.getAttribute("data-save-url"), userForm, antiForgery(usersPage))
                .then(function (result) {
                    status("legacyUsersStatus", result.Message || "تم الحفظ", "success");
                    if (result.Id) {
                        window.setTimeout(function () {
                            window.location.href = "/Pos/PosLegacyAdmin/Users/" + result.Id;
                        }, 650);
                    }
                })
                .catch(function (error) {
                    status("legacyUsersStatus", error.message, "error");
                });
        });

        if (deleteUserBtn) {
            deleteUserBtn.addEventListener("click", function () {
                var id = userForm.querySelector("input[name='UserID']").value;
                if (!id || !window.confirm("تأكيد حذف المستخدم؟")) { return; }
                var data = new FormData();
                data.append("__RequestVerificationToken", antiForgery(usersPage));
                data.append("id", id);
                fetch(usersPage.getAttribute("data-delete-url"), { method: "POST", body: data, credentials: "same-origin" })
                    .then(function (response) {
                        return response.json().then(function (json) {
                            if (!response.ok || !json.Success) { throw new Error(json.Message || "تعذر الحذف"); }
                            return json;
                        });
                    })
                    .then(function (result) {
                        status("legacyUsersStatus", result.Message || "تم الحذف", "success");
                        window.setTimeout(function () { window.location.href = "/Pos/PosLegacyAdmin/Users"; }, 650);
                    })
                    .catch(function (error) { status("legacyUsersStatus", error.message, "error"); });
            });
        }
    }

    var branchesPage = byId("legacyBranchesPage");
    if (branchesPage) {
        var table = byId("legacyBranchesTable");
        var addRowBtn = byId("legacyBranchesAddRow");
        var saveBranchesBtn = byId("legacyBranchesSave");
        var branchesForm = byId("legacyBranchesForm");

        function reindex() {
            var rows = table.querySelectorAll("tbody tr");
            Array.prototype.forEach.call(rows, function (row, index) {
                Array.prototype.forEach.call(row.querySelectorAll("[name]"), function (input) {
                    input.name = input.name.replace(/branches\[(?:__i__|\d+)\]/, "branches[" + index + "]");
                });
            });
        }

        addRowBtn.addEventListener("click", function () {
            var template = byId("legacyBranchRowTemplate");
            var holder = document.createElement("tbody");
            holder.innerHTML = template.innerHTML.replace(/__i__/g, table.querySelectorAll("tbody tr").length);
            table.querySelector("tbody").appendChild(holder.firstElementChild);
            reindex();
        });

        table.addEventListener("click", function (event) {
            if (!event.target.classList.contains("legacy-row-remove")) { return; }
            event.target.closest("tr").remove();
            reindex();
        });

        saveBranchesBtn.addEventListener("click", function () {
            reindex();
            status("legacyBranchesStatus", "جار حفظ بيانات النشاط والفروع...", "info");
            postForm(branchesPage.getAttribute("data-save-url"), branchesForm, antiForgery(branchesPage))
                .then(function (result) {
                    status("legacyBranchesStatus", result.Message || "تم الحفظ", "success");
                    if (result.Id) {
                        window.setTimeout(function () {
                            window.location.href = "/Pos/PosLegacyAdmin/BranchesData/" + result.Id;
                        }, 650);
                    }
                })
                .catch(function (error) {
                    status("legacyBranchesStatus", error.message, "error");
                });
        });
    }
})();
