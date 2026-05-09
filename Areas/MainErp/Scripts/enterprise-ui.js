(function (window, document) {
    "use strict";

    function wrapTables(root) {
        var tables = root.querySelectorAll("table.erp-table, table.main-erp-table, table.voucher-table, table.pos-table, table.ep-table, table.stock-table, table.index-table");
        Array.prototype.forEach.call(tables, function (table) {
            if (table.parentElement && /(^|\s)(erp-table-wrap|main-erp-table-wrap|voucher-table-wrap|pos-table-wrap|responsive-table|ep-table-card|items-scroll|index-table-wrap)(\s|$)/.test(table.parentElement.className)) {
                return;
            }

            var wrapper = document.createElement("div");
            wrapper.className = "erp-table-wrap";
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        });
    }

    function markEmptyTables(root) {
        var tables = root.querySelectorAll("table");
        Array.prototype.forEach.call(tables, function (table) {
            var body = table.tBodies && table.tBodies[0];
            if (!body || body.rows.length > 0) { return; }
            table.classList.add("erp-is-empty-table");
        });
    }

    function normalizeActionForms(root) {
        var inlineForms = root.querySelectorAll("form[style*='display:inline'], form.voucher-inline-form");
        Array.prototype.forEach.call(inlineForms, function (form) {
            form.classList.add("erp-inline-form");
        });
    }

    function init(root) {
        root = root || document;
        wrapTables(root);
        markEmptyTables(root);
        normalizeActionForms(root);
    }

    window.DynamicErpEnterpriseUI = {
        init: init,
        wrapTables: wrapTables
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () { init(document); });
    } else {
        init(document);
    }
})(window, document);
