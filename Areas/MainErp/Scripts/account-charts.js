(function () {
    "use strict";

    var page = document.querySelector(".account-charts-page");
    if (!page) {
        return;
    }

    var state = {
        tree: JSON.parse(document.getElementById("accountChartsInitialTree").textContent || "[]"),
        permissions: JSON.parse(document.getElementById("accountChartsPermissions").textContent || "{}"),
        selectedCode: "r",
        mode: "view"
    };

    var el = {
        tree: document.getElementById("accountChartsTree"),
        search: document.getElementById("accountChartsSearch"),
        form: document.getElementById("accountChartsForm"),
        message: document.getElementById("accountChartsMessage"),
        mode: document.getElementById("accountChartsMode"),
        modeLabel: document.getElementById("accountChartsModeLabel"),
        title: document.getElementById("accountChartsEditorTitle"),
        token: document.querySelector("input[name='__RequestVerificationToken']"),
        buttons: {
            refresh: document.getElementById("accountChartsRefresh"),
            print: document.getElementById("accountChartsPrint"),
            newAccount: document.getElementById("accountChartsNew"),
            edit: document.getElementById("accountChartsEdit"),
            deleteAccount: document.getElementById("accountChartsDelete"),
            cancel: document.getElementById("accountChartsCancel"),
            save: document.getElementById("accountChartsSave")
        }
    };

    function byId(id) {
        return document.getElementById(id);
    }

    function setMessage(text, type) {
        if (!text) {
            el.message.hidden = true;
            el.message.textContent = "";
            el.message.className = "account-charts-message";
            return;
        }

        el.message.hidden = false;
        el.message.textContent = text;
        el.message.className = "account-charts-message " + (type || "info");
    }

    function setMode(mode) {
        state.mode = mode;
        el.mode.value = mode;
        var isReadOnly = mode === "view";
        var inputs = el.form.querySelectorAll("input, select");
        Array.prototype.forEach.call(inputs, function (input) {
            if (input.type !== "hidden") {
                input.disabled = isReadOnly;
            }
        });

        el.buttons.save.disabled = isReadOnly;
        el.buttons.cancel.disabled = isReadOnly;
        el.buttons.edit.disabled = !state.permissions.CanEdit || !state.selectedCode || state.selectedCode === "r" || !isReadOnly;
        el.buttons.deleteAccount.disabled = !state.permissions.CanDelete || !state.selectedCode || state.selectedCode === "r" || !isReadOnly;
        el.buttons.newAccount.disabled = !state.permissions.CanAdd || !isReadOnly;
        el.modeLabel.textContent = mode === "new" ? "إضافة" : mode === "edit" ? "تعديل" : "عرض";
    }

    function accountDisplay(item) {
        var serial = item.AccountSerial || "";
        var name = item.AccountName || item.AccountNameEnglish || "";
        return serial ? serial + " - " + name : name;
    }

    function renderTree() {
        var query = (el.search.value || "").trim().toLowerCase();
        var children = {};
        state.tree.forEach(function (item) {
            var parent = item.ParentAccountCode || "";
            if (!children[parent]) {
                children[parent] = [];
            }
            children[parent].push(item);
        });

        function matches(item) {
            if (!query) {
                return true;
            }

            var text = [
                item.AccountSerial,
                item.AccountName,
                item.AccountNameEnglish
            ].join(" ").toLowerCase();

            if (text.indexOf(query) >= 0) {
                return true;
            }

            return (children[item.AccountCode] || []).some(matches);
        }

        function renderBranch(parentCode) {
            var nodes = children[parentCode] || [];
            var ul = document.createElement("ul");
            nodes.forEach(function (item) {
                if (!matches(item)) {
                    return;
                }

                var li = document.createElement("li");
                li.className = item.IsLastAccount ? "is-final" : "is-master";
                if (item.AccountCode === state.selectedCode) {
                    li.className += " is-selected";
                }

                var button = document.createElement("button");
                button.type = "button";
                button.dataset.code = item.AccountCode;
                button.innerHTML = "<i class='fas " + (item.IsLastAccount ? "fa-file-alt" : "fa-folder") + "'></i><span></span>";
                button.querySelector("span").textContent = accountDisplay(item);
                button.addEventListener("click", function () {
                    selectAccount(item.AccountCode);
                });
                li.appendChild(button);

                if (!item.IsLastAccount) {
                    li.appendChild(renderBranch(item.AccountCode));
                }

                ul.appendChild(li);
            });
            return ul;
        }

        el.tree.innerHTML = "";
        el.tree.appendChild(renderBranch("r"));
        populateParentOptions();
    }

    function populateParentOptions() {
        var select = byId("ParentAccountCode");
        var selected = select.value || state.selectedCode || "r";
        select.innerHTML = "";
        state.tree.forEach(function (item) {
            if (item.IsLastAccount) {
                return;
            }

            var option = document.createElement("option");
            option.value = item.AccountCode;
            option.textContent = accountDisplay(item);
            select.appendChild(option);
        });

        if (selected) {
            select.value = selected;
        }
    }

    function selectAccount(accountCode) {
        state.selectedCode = accountCode;
        setMessage("");
        fetch(page.dataset.detailsUrl + "?accountCode=" + encodeURIComponent(accountCode), { credentials: "same-origin" })
            .then(readJson)
            .then(function (data) {
                if (!data.success) {
                    setMessage(data.message || "تعذر تحميل الحساب.", "error");
                    return;
                }

                fillForm(data.account);
                setMode("view");
                renderTree();
            })
            .catch(function (error) {
                setMessage(error.message, "error");
            });
    }

    function fillForm(account) {
        byId("AccountId").value = account.AccountId || "";
        byId("AccountCode").value = account.AccountCode || "";
        byId("ParentAccountCode").value = account.ParentAccountCode || "";
        byId("AccountSerial").value = account.AccountSerial || "";
        byId("AccountName").value = account.AccountName || "";
        byId("AccountNameEnglish").value = account.AccountNameEnglish || "";
        byId("CurrencyCode").value = account.CurrencyCode || "1";
        byId("ActivityTypeId").value = account.ActivityTypeId || "";
        byId("AccountTypes").value = account.AccountTypes == null ? "0" : account.AccountTypes;
        byId("AccountTab").value = account.AccountTab == null ? "0" : account.AccountTab;
        byId("DebitOrCredit").value = account.DebitOrCredit == null ? "0" : account.DebitOrCredit;
        byId("DifferentType").value = account.DifferentType == null ? "1" : account.DifferentType;
        byId("Authority").value = account.Authority == null ? "0" : account.Authority;
        byId("UserGroupId").value = account.UserGroupId || "";
        byId("UserId").value = account.UserId || "";
        byId("CostCenterId").value = account.CostCenterId || "";

        byId("IsLastAccountFinal").checked = account.IsLastAccount === true;
        byId("IsLastAccountMaster").checked = account.IsLastAccount !== true;
        byId("HasBudget").checked = account.HasBudget === true;
        byId("HasCostCenter").checked = account.HasCostCenter === true;
        byId("IsSummaryAccount").checked = account.IsSummaryAccount === true;
        byId("IsBlocked").checked = account.IsBlocked === true;
        byId("CostCenterFixed").checked = account.CostCenterType === 1;
        byId("CostCenterNone").checked = account.CostCenterType !== 1;

        setMulti("BranchIds", account.BranchIds || []);
        setMulti("UserIds", account.UserIds || []);
        el.title.textContent = account.AccountSerial ? account.AccountSerial + " - " + account.AccountName : "بيانات الحساب";
        el.buttons.deleteAccount.title = account.DeleteBlockReason || "حذف";
    }

    function setMulti(id, values) {
        var stringValues = values.map(function (v) { return String(v); });
        Array.prototype.forEach.call(byId(id).options, function (option) {
            option.selected = stringValues.indexOf(option.value) >= 0;
        });
    }

    function clearForNew() {
        el.form.reset();
        byId("AccountId").value = "";
        byId("AccountCode").value = "";
        byId("ParentAccountCode").value = state.selectedCode || "r";
        byId("CurrencyCode").value = "1";
        byId("IsLastAccountFinal").checked = true;
        byId("CostCenterNone").checked = true;
        byId("AccountTypes").value = "0";
        byId("AccountTab").value = "0";
        byId("DebitOrCredit").value = "0";
        byId("DifferentType").value = "1";
        byId("Authority").value = "0";
        el.title.textContent = "حساب جديد";
        setMode("new");
    }

    function save() {
        var mode = state.mode;
        if (mode !== "new" && mode !== "edit") {
            return;
        }

        if (!byId("AccountName").value.trim()) {
            setMessage("يجب كتابة اسم الحساب.", "error");
            byId("AccountName").focus();
            return;
        }

        if (byId("HasCostCenter").checked && byId("CostCenterFixed").checked && !byId("CostCenterId").value) {
            setMessage("يجب اختيار مركز التكلفة.", "error");
            byId("CostCenterId").focus();
            return;
        }

        var formData = new FormData(el.form);
        formData.append("__RequestVerificationToken", el.token.value);
        normalizeCheckbox(formData, "HasBudget", byId("HasBudget").checked);
        normalizeCheckbox(formData, "HasCostCenter", byId("HasCostCenter").checked);
        normalizeCheckbox(formData, "IsSummaryAccount", byId("IsSummaryAccount").checked);
        normalizeCheckbox(formData, "IsBlocked", byId("IsBlocked").checked);

        var url = mode === "new" ? page.dataset.createUrl : page.dataset.updateUrl;
        post(url, formData)
            .then(function (data) {
                setMessage(data.Message || data.message, data.Success || data.success ? "success" : "error");
                if (!(data.Success || data.success)) {
                    return;
                }

                return reloadTree().then(function () {
                    selectAccount(data.AccountCode || data.accountCode);
                });
            })
            .catch(function (error) {
                setMessage(error.message, "error");
            });
    }

    function normalizeCheckbox(formData, name, checked) {
        formData.delete(name);
        formData.append(name, checked ? "true" : "false");
    }

    function deleteAccount() {
        if (!state.selectedCode || state.selectedCode === "r") {
            return;
        }

        if (!window.confirm("هل تريد حذف الحساب المحدد؟")) {
            return;
        }

        var formData = new FormData();
        formData.append("__RequestVerificationToken", el.token.value);
        formData.append("accountCode", state.selectedCode);

        post(page.dataset.deleteUrl, formData)
            .then(function (data) {
                setMessage(data.Message || data.message, data.Success || data.success ? "success" : "error");
                if (data.Success || data.success) {
                    state.selectedCode = "r";
                    return reloadTree().then(function () { selectAccount("r"); });
                }
            })
            .catch(function (error) {
                setMessage(error.message, "error");
            });
    }

    function reloadTree() {
        return fetch(page.dataset.treeUrl, { credentials: "same-origin" })
            .then(readJson)
            .then(function (data) {
                if (!data.success) {
                    throw new Error(data.message || "تعذر تحديث الشجرة.");
                }

                state.tree = data.items || [];
                renderTree();
            });
    }

    function post(url, formData) {
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            body: formData
        }).then(readJson);
    }

    function readJson(response) {
        if (!response.ok) {
            throw new Error(response.statusText || "حدث خطأ في الاتصال.");
        }

        return response.json();
    }

    el.search.addEventListener("input", renderTree);
    el.buttons.refresh.addEventListener("click", function () {
        reloadTree().then(function () {
            setMessage("تم تحديث الشجرة.", "success");
        }).catch(function (error) {
            setMessage(error.message, "error");
        });
    });
    el.buttons.newAccount.addEventListener("click", clearForNew);
    el.buttons.edit.addEventListener("click", function () { setMode("edit"); });
    el.buttons.cancel.addEventListener("click", function () { selectAccount(state.selectedCode || "r"); });
    el.buttons.save.addEventListener("click", save);
    el.buttons.deleteAccount.addEventListener("click", deleteAccount);
    el.buttons.print.addEventListener("click", function () { window.print(); });

    renderTree();
    reloadTree().then(function () {
        selectAccount("r");
    }).catch(function (error) {
        setMessage(error.message, "error");
    });
})();
