(function () {
    "use strict";
    var root = document.querySelector("[data-financial-admin]");
    if (!root) { return; }

    function token() {
        var input = root.querySelector("input[name='__RequestVerificationToken']");
        return input ? input.value : "";
    }

    function value(id, val) {
        var el = document.getElementById(id);
        if (!el) { return ""; }
        if (arguments.length > 1) {
            if (el.type === "checkbox") {
                el.checked = !!val;
            } else {
                el.value = val == null ? "" : val;
            }
        }
        return el.type === "checkbox" ? el.checked : el.value;
    }

    function showEditor(mode, data) {
        data = data || {};
        value("faEditorMode", mode);
        value("faBankId", mode === "bank" ? data.BankId : "");
        value("faBoxId", mode === "box" ? data.BoxId : "");
        value("faName", mode === "bank" ? data.BankName : data.BoxName);
        value("faNameEnglish", mode === "bank" ? data.BankNameEnglish : data.BoxNameEnglish);
        value("faAccountCode", data.AccountCode);
        value("faAccountCode1", data.AccountCode1);
        value("faAccountCode2", data.AccountCode2);
        value("faAccountCode3", data.AccountCode3);
        value("faParentAccount", data.ParentAccount);
        value("faBranchId", data.BranchId);
        value("faCurrencyId", data.CurrencyId);
        value("faEmployeeId", data.EmployeeId);
        value("faAccountNo", data.AccountNo);
        value("faIban", data.Iban);
        value("faBranchNo", data.BranchNo);
        value("faOpeningBalance", data.OpeningBalance || 0);
        value("faOpeningBalanceType", data.OpeningBalanceType);
        value("faLimitValue", data.LimitValue || 0);
        value("faBoxType", data.Type);
        value("faTelephone", data.Telephone);
        value("faEmail", data.Email);
        value("faAddress", data.Address);
        value("faComments", data.Comments);
        value("faRemarks", data.Remarks);
        value("faApprovalRequired", data.ApprovalRequired);
        value("faLoanBank", data.LoanBank);
        document.getElementById("faEditorTitle").textContent = mode === "bank" ? "\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0628\u0646\u0643" : "\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0635\u0646\u062f\u0648\u0642";
        document.getElementById("faEditorTitle").textContent = mode === "bank" ? "\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0628\u0646\u0643" : "\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0635\u0646\u062f\u0648\u0642";
        document.getElementById("faEditorSource").textContent = mode === "bank" ? "إدارة البنوك" : "إدارة الصناديق";
        document.querySelectorAll(".fa-bank-only").forEach(function (x) { x.hidden = mode !== "bank"; });
        document.querySelectorAll(".fa-box-only").forEach(function (x) { x.hidden = mode !== "box"; });
        document.getElementById("faEditorMessage").textContent = "";
        document.getElementById("faEditor").hidden = false;
    }

    function closeEditor() {
        document.getElementById("faEditor").hidden = true;
    }

    function fetchJson(url) {
        return fetch(url, { credentials: "same-origin" }).then(function (r) { return r.json(); });
    }

    var accountState = {
        input: null,
        rows: [],
        selected: -1,
        timer: null,
        requestId: 0
    };

    function accountResults() {
        return document.getElementById("faAccountResults");
    }

    function hideAccountResults() {
        var box = accountResults();
        if (box) {
            box.hidden = true;
            box.innerHTML = "";
        }
        accountState.rows = [];
        accountState.selected = -1;
    }

    function paintAccountResults() {
        var box = accountResults();
        if (!box || !accountState.input) { return; }

        var rect = accountState.input.getBoundingClientRect();
        box.style.left = rect.left + "px";
        box.style.top = (rect.bottom + 4) + "px";
        box.style.width = rect.width + "px";
        box.innerHTML = "";

        if (!accountState.rows.length) {
            box.hidden = true;
            return;
        }

        accountState.rows.forEach(function (row, index) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "fa-account-result" + (index === accountState.selected ? " is-active" : "");
            item.setAttribute("data-account-index", index);
            item.innerHTML = "<strong></strong><span></span>";
            item.querySelector("strong").textContent = row.Code || row.Id || "";
            item.querySelector("span").textContent = row.Name || "";
            box.appendChild(item);
        });

        box.hidden = false;
    }

    function chooseAccount(index) {
        var row = accountState.rows[index];
        if (!row || !accountState.input) { return; }
        accountState.input.value = row.Code || row.Id || "";
        hideAccountResults();
        accountState.input.focus();
    }

    function searchAccounts(input) {
        var url = root.getAttribute("data-account-lookup-url");
        var term = (input.value || "").trim();
        accountState.input = input;

        window.clearTimeout(accountState.timer);
        if (!url || term.length < 2) {
            hideAccountResults();
            return;
        }

        accountState.timer = window.setTimeout(function () {
            var requestId = ++accountState.requestId;
            fetchJson(url + "?term=" + encodeURIComponent(term) + "&limit=20").then(function (res) {
                if (requestId !== accountState.requestId || accountState.input !== input) { return; }
                accountState.rows = res && res.success && res.rows ? res.rows : [];
                accountState.selected = accountState.rows.length ? 0 : -1;
                paintAccountResults();
            }).catch(hideAccountResults);
        }, 250);
    }

    function submitForm(url, data) {
        data.__RequestVerificationToken = token();
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
            body: new URLSearchParams(data).toString()
        }).then(function (r) { return r.json().then(function (j) { j._ok = r.ok; return j; }); });
    }

    function collect() {
        var mode = value("faEditorMode");
        var data = {
            BankId: mode === "bank" ? value("faBankId") : "",
            BoxId: mode === "box" ? value("faBoxId") : "",
            BankName: value("faName"),
            BoxName: value("faName"),
            BankNameEnglish: value("faNameEnglish"),
            BoxNameEnglish: value("faNameEnglish"),
            AccountCode: value("faAccountCode"),
            AccountCode1: value("faAccountCode1"),
            AccountCode2: value("faAccountCode2"),
            AccountCode3: value("faAccountCode3"),
            ParentAccount: value("faParentAccount"),
            BranchId: value("faBranchId"),
            CurrencyId: value("faCurrencyId"),
            EmployeeId: value("faEmployeeId"),
            AccountNo: value("faAccountNo"),
            Iban: value("faIban"),
            BranchNo: value("faBranchNo"),
            OpeningBalance: value("faOpeningBalance"),
            OpeningBalanceType: value("faOpeningBalanceType"),
            LimitValue: value("faLimitValue"),
            Type: value("faBoxType"),
            Telephone: value("faTelephone"),
            Email: value("faEmail"),
            Address: value("faAddress"),
            Comments: value("faComments"),
            Remarks: value("faRemarks"),
            ApprovalRequired: value("faApprovalRequired"),
            LoanBank: value("faLoanBank"),
            HasChequeBox: value("faHasChequeBox")
        };
        return { mode: mode, data: data };
    }

    function tableToCsv(table) {
        var rows = [];
        table.querySelectorAll("tr").forEach(function (row) {
            var cells = [];
            row.querySelectorAll("th,td").forEach(function (cell) {
                var text = (cell.innerText || "").replace(/\s+/g, " ").trim();
                cells.push('"' + text.replace(/"/g, '""') + '"');
            });
            rows.push(cells.join(","));
        });
        return rows.join("\r\n");
    }

    document.addEventListener("click", function (event) {
        var button = event.target.closest("[data-export-table]");
        if (!button) {
            var close = event.target.closest("[data-close-editor]");
            if (close) { closeEditor(); return; }
            var accountItem = event.target.closest("[data-account-index]");
            if (accountItem) {
                chooseAccount(parseInt(accountItem.getAttribute("data-account-index"), 10));
                return;
            }
            var newBank = event.target.closest("[data-new-bank]");
            if (newBank) { showEditor("bank", {}); return; }
            var newBox = event.target.closest("[data-new-box]");
            if (newBox) { showEditor("box", {}); return; }
            var bankRow = event.target.closest("[data-edit-bank]");
            if (bankRow) {
                fetchJson(root.getAttribute("data-bank-details-url") + "?id=" + encodeURIComponent(bankRow.getAttribute("data-edit-bank"))).then(function (res) {
                    if (res.success) { showEditor("bank", res.data); }
                });
                return;
            }
            var boxRow = event.target.closest("[data-edit-box]");
            if (boxRow) {
                fetchJson(root.getAttribute("data-box-details-url") + "?id=" + encodeURIComponent(boxRow.getAttribute("data-edit-box"))).then(function (res) {
                    if (res.success) { showEditor("box", res.data); }
                });
                return;
            }
            return;
        }

        var table = document.querySelector(button.getAttribute("data-export-table"));
        if (!table) { return; }

        var blob = new Blob(["\ufeff" + tableToCsv(table)], { type: "text/csv;charset=utf-8;" });
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = "financial-administration.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    });

    var form = document.getElementById("faEditorForm");
    if (form) {
        form.addEventListener("input", function (event) {
            if (event.target && event.target.classList.contains("fa-account-search")) {
                searchAccounts(event.target);
            }
        });

        form.addEventListener("focusin", function (event) {
            if (event.target && event.target.classList.contains("fa-account-search")) {
                searchAccounts(event.target);
            }
        });

        form.addEventListener("keydown", function (event) {
            if (!event.target || !event.target.classList.contains("fa-account-search") || !accountState.rows.length) {
                return;
            }

            if (event.key === "ArrowDown") {
                event.preventDefault();
                accountState.selected = Math.min(accountState.selected + 1, accountState.rows.length - 1);
                paintAccountResults();
            } else if (event.key === "ArrowUp") {
                event.preventDefault();
                accountState.selected = Math.max(accountState.selected - 1, 0);
                paintAccountResults();
            } else if (event.key === "Enter") {
                event.preventDefault();
                chooseAccount(accountState.selected);
            } else if (event.key === "Escape") {
                hideAccountResults();
            }
        });

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            var payload = collect();
            var url = payload.mode === "bank" ? root.getAttribute("data-save-bank-url") : root.getAttribute("data-save-box-url");
            submitForm(url, payload.data).then(function (res) {
                if (!res || res.success === false || res._ok === false) {
                    document.getElementById("faEditorMessage").textContent = res.Message || res.message || "\u062a\u0639\u0630\u0631 \u0627\u0644\u062d\u0641\u0638";
                    return;
                }
                window.location.reload();
            });
        });
    }

    window.addEventListener("scroll", hideAccountResults, true);
    window.addEventListener("resize", hideAccountResults);
}());
