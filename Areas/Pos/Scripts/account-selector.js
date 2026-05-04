(function () {
    "use strict";

    var recentKey = "pos.accountSelector.recent";
    var favoriteKey = "pos.accountSelector.favorites";
    var modal;
    var activeConfig = null;
    var selected = {};
    var loadedChildren = {};
    var searchTimer = null;

    function byId(id) { return document.getElementById(id); }

    function escapeHtml(value) {
        return String(value || "").replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    function getProp(obj, pascal, camel) {
        return obj ? (obj[pascal] !== undefined ? obj[pascal] : obj[camel]) : "";
    }

    function normalizeAccount(raw) {
        raw = raw || {};
        return {
            code: getProp(raw, "AccountCode", "accountCode") || "",
            serial: getProp(raw, "AccountSerial", "accountSerial") || "",
            name: getProp(raw, "AccountName", "accountName") || "",
            hasChildren: getProp(raw, "HasChildren", "hasChildren") === true,
            isLastAccount: getProp(raw, "IsLastAccount", "isLastAccount") === true
        };
    }

    function displayLabel(account) {
        account = normalizeAccount(account);
        var label = "";
        if (account.serial) { label += account.serial + " - "; }
        label += account.name || "حساب بدون اسم";
        return label;
    }

    function readStore(key) {
        try {
            var value = JSON.parse(localStorage.getItem(key) || "[]");
            return Array.isArray(value) ? value : [];
        } catch (e) {
            return [];
        }
    }

    function writeStore(key, value) {
        try { localStorage.setItem(key, JSON.stringify((value || []).slice(0, 12))); } catch (e) { }
    }

    function rememberRecent(accounts) {
        var recent = readStore(recentKey);
        Object.keys(accounts || {}).forEach(function (code) {
            var account = accounts[code];
            recent = recent.filter(function (item) { return item.code !== code; });
            recent.unshift({ code: account.code, serial: account.serial, name: account.name });
        });
        writeStore(recentKey, recent);
    }

    function toggleFavorite(account) {
        account = normalizeAccount(account);
        if (!account.code) { return; }
        var favorites = readStore(favoriteKey);
        var exists = favorites.some(function (item) { return item.code === account.code; });
        favorites = favorites.filter(function (item) { return item.code !== account.code; });
        if (!exists) { favorites.unshift({ code: account.code, serial: account.serial, name: account.name }); }
        writeStore(favoriteKey, favorites);
        renderSavedLists();
        renderSelected();
    }

    function isFavorite(code) {
        return readStore(favoriteKey).some(function (item) { return item.code === code; });
    }

    function queryUrl(params) {
        var url = activeConfig.url || "";
        var sep = url.indexOf("?") >= 0 ? "&" : "?";
        var pieces = [];
        Object.keys(params || {}).forEach(function (key) {
            if (params[key] !== undefined && params[key] !== null && params[key] !== "") {
                pieces.push(encodeURIComponent(key) + "=" + encodeURIComponent(params[key]));
            }
        });
        return pieces.length ? url + sep + pieces.join("&") : url;
    }

    function fetchAccounts(params) {
        return fetch(queryUrl(params), { credentials: "same-origin" }).then(function (response) {
            if (!response.ok) { throw new Error("HTTP " + response.status); }
            return response.json();
        });
    }

    function createModal() {
        if (modal) { return modal; }
        var wrapper = document.createElement("div");
        wrapper.className = "account-modal-backdrop";
        wrapper.style.display = "none";
        wrapper.innerHTML =
            "<div class='account-modal' role='dialog' aria-modal='true' aria-label='اختيار الحسابات'>" +
            "  <div class='account-modal-header'>" +
            "    <div><h2>اختيار الحسابات</h2><p>ابحث بالكود أو اسم الحساب، أو افتح الشجرة واختر حسابا أو أكثر.</p></div>" +
            "    <button type='button' class='account-modal-close' data-account-close='1'>×</button>" +
            "  </div>" +
            "  <div class='account-modal-body'>" +
            "    <aside class='account-modal-sidebar'>" +
            "      <label class='account-modal-search-label'>بحث الحساب</label>" +
            "      <input type='search' id='accountSelectorSearch' class='account-modal-search' placeholder='ابحث بالكود أو اسم الحساب' autocomplete='off' />" +
            "      <div class='account-modal-hint'>اكتب حرفين على الأقل للبحث السريع.</div>" +
            "      <div class='account-saved-block'><h3>المفضلة</h3><div id='accountFavoriteList' class='account-mini-list'></div></div>" +
            "      <div class='account-saved-block'><h3>الأحدث</h3><div id='accountRecentList' class='account-mini-list'></div></div>" +
            "    </aside>" +
            "    <section class='account-modal-main'>" +
            "      <div class='account-mode-tabs'><button type='button' class='is-active' data-account-mode='tree'>الشجرة</button><button type='button' data-account-mode='list'>قائمة البحث</button></div>" +
            "      <div id='accountTreePanel' class='account-tree-panel-modal'><div class='account-loading'>جاري تحميل الحسابات...</div></div>" +
            "      <div id='accountSearchPanel' class='account-search-panel' hidden><div class='account-empty'>ابدأ البحث بالكود أو اسم الحساب.</div></div>" +
            "    </section>" +
            "    <aside class='account-modal-selected'><h3>الحسابات المختارة</h3><div id='accountSelectedList'></div></aside>" +
            "  </div>" +
            "  <div class='account-modal-footer'><button type='button' class='secondary-action' data-account-close='1'>إلغاء</button><button type='button' class='primary-action' id='accountSelectorConfirm'>تأكيد الاختيار</button></div>" +
            "</div>";
        document.body.appendChild(wrapper);
        modal = wrapper;
        bindModal();
        return modal;
    }

    function bindModal() {
        modal.addEventListener("click", function (event) {
            if (event.target.getAttribute("data-account-close") === "1") { closeModal(); return; }
            if (event.target.id === "accountSelectorConfirm") { confirmSelection(); return; }
            var mode = event.target.getAttribute("data-account-mode");
            if (mode) { switchMode(mode); return; }
            var expand = event.target.closest ? event.target.closest(".account-expand-btn") : null;
            if (expand) { toggleChildren(expand.getAttribute("data-code"), expand); return; }
            var star = event.target.closest ? event.target.closest(".account-favorite-btn") : null;
            if (star) { toggleFavorite(readAccountFromElement(star)); return; }
            var row = event.target.closest ? event.target.closest(".account-row") : null;
            if (row && !event.target.matches("input")) { toggleAccount(readAccountFromElement(row), !selected[row.getAttribute("data-code")]); return; }
        });

        modal.addEventListener("change", function (event) {
            if (!event.target.classList.contains("account-check")) { return; }
            var row = event.target.closest(".account-row");
            if (!row) { return; }
            toggleAccount(readAccountFromElement(row), event.target.checked, row);
        });

        byId("accountSelectorSearch").addEventListener("input", function (event) {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(function () { runSearch(event.target.value); }, 220);
        });

        document.addEventListener("keydown", function (event) {
            if (!modal || modal.style.display === "none") { return; }
            if (event.key === "Escape") { closeModal(); }
        });
    }

    function readAccountFromElement(element) {
        return {
            code: element.getAttribute("data-code") || "",
            serial: element.getAttribute("data-serial") || "",
            name: element.getAttribute("data-name") || "",
            hasChildren: element.getAttribute("data-has-children") === "true"
        };
    }

    function openModal(config) {
        activeConfig = config || {};
        selected = {};
        var input = byId(activeConfig.inputId);
        var existing = input && input.value ? input.value.split(",") : [];
        existing.forEach(function (code) {
            code = (code || "").trim();
            if (code) { selected[code] = { code: code, serial: "", name: "حساب مختار مسبقا" }; }
        });
        createModal().style.display = "flex";
        document.body.classList.add("account-modal-open");
        byId("accountSelectorSearch").value = "";
        renderSavedLists();
        renderSelected();
        switchMode("tree");
        loadRoots();
    }

    function closeModal() {
        if (modal) { modal.style.display = "none"; }
        document.body.classList.remove("account-modal-open");
    }

    function confirmSelection() {
        var input = byId(activeConfig.inputId);
        var summary = byId(activeConfig.summaryId);
        var codes = Object.keys(selected);
        if (input) { input.value = codes.join(","); }
        if (summary) {
            summary.textContent = codes.length ? ("تم اختيار " + codes.length + " حساب") : "لم يتم اختيار حسابات";
        }
        rememberRecent(selected);
        closeModal();
    }

    function switchMode(mode) {
        var treePanel = byId("accountTreePanel");
        var searchPanel = byId("accountSearchPanel");
        modal.querySelectorAll("[data-account-mode]").forEach(function (btn) {
            btn.classList.toggle("is-active", btn.getAttribute("data-account-mode") === mode);
        });
        treePanel.hidden = mode !== "tree";
        searchPanel.hidden = mode !== "list";
    }

    function loadRoots() {
        var panel = byId("accountTreePanel");
        if (panel.getAttribute("data-loaded") === "true") { syncChecks(); return; }
        panel.innerHTML = "<div class='account-loading'>جاري تحميل الحسابات...</div>";
        fetchAccounts({}).then(function (rows) {
            panel.setAttribute("data-loaded", "true");
            panel.innerHTML = renderRows(rows || [], 0);
            syncChecks();
        }).catch(function () {
            panel.innerHTML = "<div class='account-empty'>تعذر تحميل دليل الحسابات.</div>";
        });
    }

    function toggleChildren(code, button) {
        var container = modal.querySelector(".account-children[data-parent='" + cssEscape(code) + "']");
        if (!container) { return; }
        if (loadedChildren[code]) {
            container.hidden = !container.hidden;
            button.classList.toggle("is-open", !container.hidden);
            return;
        }
        container.hidden = false;
        container.innerHTML = "<div class='account-loading small'>جاري التحميل...</div>";
        fetchAccounts({ parentCode: code }).then(function (rows) {
            loadedChildren[code] = true;
            container.innerHTML = renderRows(rows || [], 1);
            button.classList.add("is-open");
            syncChecks();
        }).catch(function () {
            container.innerHTML = "<div class='account-empty'>تعذر تحميل الحسابات الفرعية.</div>";
        });
    }

    function runSearch(term) {
        var panel = byId("accountSearchPanel");
        term = (term || "").trim();
        if (term.length < 2) {
            panel.innerHTML = "<div class='account-empty'>اكتب حرفين على الأقل للبحث.</div>";
            return;
        }
        switchMode("list");
        panel.innerHTML = "<div class='account-loading'>جاري البحث...</div>";
        fetchAccounts({ term: term }).then(function (rows) {
            panel.innerHTML = rows && rows.length ? renderRows(rows, 0, true) : "<div class='account-empty'>لا توجد حسابات مطابقة.</div>";
            syncChecks();
        }).catch(function () {
            panel.innerHTML = "<div class='account-empty'>تعذر البحث في الحسابات.</div>";
        });
    }

    function renderRows(rows, level, flat) {
        return "<div class='account-row-list'>" + (rows || []).map(function (raw) {
            var account = normalizeAccount(raw);
            var label = displayLabel(account);
            var fav = isFavorite(account.code);
            return "<div class='account-row-wrap'>" +
                "<div class='account-row' tabindex='0' data-code='" + escapeHtml(account.code) + "' data-serial='" + escapeHtml(account.serial) + "' data-name='" + escapeHtml(account.name) + "' data-has-children='" + (account.hasChildren ? "true" : "false") + "' style='--level:" + (level || 0) + "'>" +
                (account.hasChildren && !flat ? "<button type='button' class='account-expand-btn' data-code='" + escapeHtml(account.code) + "' aria-label='فتح الحسابات الفرعية'></button>" : "<span class='account-expand-spacer'></span>") +
                "<input type='checkbox' class='account-check' />" +
                "<span class='account-row-title'>" + escapeHtml(label) + "</span>" +
                "<button type='button' class='account-favorite-btn " + (fav ? "is-favorite" : "") + "' title='إضافة للمفضلة' data-code='" + escapeHtml(account.code) + "' data-serial='" + escapeHtml(account.serial) + "' data-name='" + escapeHtml(account.name) + "'>★</button>" +
                "</div>" +
                (account.hasChildren && !flat ? "<div class='account-children' data-parent='" + escapeHtml(account.code) + "' hidden></div>" : "") +
                "</div>";
        }).join("") + "</div>";
    }

    function toggleAccount(account, checked, row) {
        account = normalizeAccount(account);
        if (!account.code) { return; }
        if (checked) { selected[account.code] = account; } else { delete selected[account.code]; }
        if (row) {
            row.querySelector(".account-check").checked = checked;
            row.classList.toggle("is-selected", checked);
            var childContainer = row.parentNode ? row.parentNode.querySelector(".account-children") : null;
            if (childContainer) {
                childContainer.querySelectorAll(".account-row").forEach(function (childRow) {
                    var child = readAccountFromElement(childRow);
                    if (checked) { selected[child.code] = child; } else { delete selected[child.code]; }
                    childRow.classList.toggle("is-selected", checked);
                    var check = childRow.querySelector(".account-check");
                    if (check) { check.checked = checked; }
                });
            }
        }
        renderSelected();
    }

    function syncChecks() {
        modal.querySelectorAll(".account-row").forEach(function (row) {
            var code = row.getAttribute("data-code");
            var check = row.querySelector(".account-check");
            if (check) { check.checked = !!selected[code]; }
            row.classList.toggle("is-selected", !!selected[code]);
        });
    }

    function renderSelected() {
        var target = byId("accountSelectedList");
        var codes = Object.keys(selected);
        if (!codes.length) {
            target.innerHTML = "<div class='account-empty'>لم يتم اختيار حسابات.</div>";
            return;
        }
        target.innerHTML = codes.map(function (code) {
            var account = selected[code];
            return "<span class='account-chip'><span>" + escapeHtml(displayLabel(account)) + "</span><button type='button' data-remove-account='" + escapeHtml(code) + "'>×</button></span>";
        }).join("");
        target.querySelectorAll("[data-remove-account]").forEach(function (btn) {
            btn.onclick = function () {
                delete selected[btn.getAttribute("data-remove-account")];
                renderSelected();
                syncChecks();
            };
        });
    }

    function renderSavedLists() {
        renderMiniList("accountFavoriteList", readStore(favoriteKey));
        renderMiniList("accountRecentList", readStore(recentKey));
    }

    function renderMiniList(id, rows) {
        var target = byId(id);
        if (!target) { return; }
        if (!rows || !rows.length) {
            target.innerHTML = "<span class='account-muted'>لا توجد حسابات.</span>";
            return;
        }
        target.innerHTML = rows.map(function (account) {
            return "<button type='button' class='account-mini-item' data-code='" + escapeHtml(account.code) + "' data-serial='" + escapeHtml(account.serial) + "' data-name='" + escapeHtml(account.name) + "'>" + escapeHtml(displayLabel(account)) + "</button>";
        }).join("");
        target.querySelectorAll(".account-mini-item").forEach(function (btn) {
            btn.onclick = function () {
                toggleAccount(readAccountFromElement(btn), true);
                syncChecks();
            };
        });
    }

    function cssEscape(value) {
        if (window.CSS && window.CSS.escape) { return window.CSS.escape(value); }
        return String(value || "").replace(/'/g, "\\'");
    }

    document.addEventListener("click", function (event) {
        var trigger = event.target.closest ? event.target.closest(".js-account-selector") : null;
        if (!trigger) { return; }
        openModal({
            url: trigger.getAttribute("data-account-tree-url"),
            inputId: trigger.getAttribute("data-target-input"),
            summaryId: trigger.getAttribute("data-summary-target")
        });
    });
})();
