(function () {
    "use strict";

    var page = document.querySelector(".purchase-invoice-page");
    if (!page) { return; }

    var items = [];
    var lookupTimer = null;

    function byId(id) { return document.getElementById(id); }
    function number(value) {
        var parsed = parseFloat(value);
        return isNaN(parsed) ? 0 : parsed;
    }
    function money(value) { return number(value).toFixed(2); }
    function html(value) {
        return String(value === null || value === undefined ? "" : value)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }
    function message(text, isError) {
        var target = byId("purchaseMessage");
        target.textContent = text || "";
        target.className = isError ? "pos-validation error" : "pos-validation success";
    }

    function query(kind, term, callback) {
        window.clearTimeout(lookupTimer);
        lookupTimer = window.setTimeout(function () {
            var url = page.getAttribute("data-lookup-url") + "?kind=" + encodeURIComponent(kind) + "&term=" + encodeURIComponent(term || "");
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) { callback(data && data.success ? data.rows || [] : []); })
                .catch(function () { callback([]); });
        }, 180);
    }

    function bindSupplierLookup() {
        var input = byId("purchaseSupplierText");
        var hidden = byId("purchaseSupplierId");
        var results = byId("supplierResults");
        input.addEventListener("input", function () {
            hidden.value = "";
            if (input.value.trim().length < 1) {
                results.innerHTML = "";
                return;
            }
            query("supplier", input.value, function (rows) {
                results.innerHTML = rows.map(function (row) {
                    return "<button type=\"button\" data-id=\"" + html(row.id) + "\" data-text=\"" + html(row.text) + "\">" + html(row.text) + "</button>";
                }).join("");
            });
        });
        results.addEventListener("click", function (event) {
            var button = event.target.closest("button[data-id]");
            if (!button) { return; }
            hidden.value = button.getAttribute("data-id");
            input.value = button.getAttribute("data-text");
            results.innerHTML = "";
        });
        byId("clearSupplierBtn").addEventListener("click", function () {
            hidden.value = "";
            input.value = "";
            results.innerHTML = "";
        });
    }

    function bindItemLookup() {
        var input = byId("purchaseItemSearch");
        var results = byId("itemResults");
        input.addEventListener("input", function () {
            if (input.value.trim().length < 1) {
                results.innerHTML = "";
                return;
            }
            query("item", input.value, function (rows) {
                results.innerHTML = rows.map(function (row) {
                    return "<button type=\"button\" data-item='" + html(JSON.stringify(row)) + "'>" +
                        html(row.ItemCode || row.Item_ID) + " - " + html(row.ItemName) +
                        "</button>";
                }).join("");
            });
        });
        results.addEventListener("click", function (event) {
            var button = event.target.closest("button[data-item]");
            if (!button) { return; }
            var item = JSON.parse(button.getAttribute("data-item"));
            items.push({
                itemId: item.Item_ID,
                itemName: item.ItemName,
                unitId: item.UnitId || null,
                unitName: item.UnitName || "",
                quantity: 1,
                purchasePrice: number(item.Price),
                discountValue: 0,
                vatValue: 0,
                vatPercent: 0
            });
            input.value = "";
            results.innerHTML = "";
            renderItems();
        });
    }

    function renderItems() {
        var body = byId("purchaseItemsBody");
        var discountTotal = 0;
        var vatTotal = 0;
        var grandTotal = 0;
        body.innerHTML = items.map(function (item, index) {
            var base = item.quantity * item.purchasePrice;
            var lineTotal = Math.max(0, base - item.discountValue + item.vatValue);
            discountTotal += item.discountValue;
            vatTotal += item.vatValue;
            grandTotal += lineTotal;
            return "<tr data-index=\"" + index + "\">" +
                "<td>" + html(item.itemName) + "</td>" +
                "<td>" + html(item.unitName) + "</td>" +
                "<td><input type=\"number\" min=\"0\" step=\"0.001\" data-field=\"quantity\" value=\"" + item.quantity + "\" /></td>" +
                "<td><input type=\"number\" min=\"0\" step=\"0.01\" data-field=\"purchasePrice\" value=\"" + item.purchasePrice + "\" /></td>" +
                "<td><input type=\"number\" min=\"0\" step=\"0.01\" data-field=\"discountValue\" value=\"" + item.discountValue + "\" /></td>" +
                "<td><input type=\"number\" min=\"0\" step=\"0.01\" data-field=\"vatValue\" value=\"" + item.vatValue + "\" /></td>" +
                "<td>" + money(lineTotal) + "</td>" +
                "<td><button type=\"button\" class=\"link-action\" data-remove=\"" + index + "\">حذف</button></td>" +
                "</tr>";
        }).join("");
        byId("purchaseDiscountTotal").textContent = money(discountTotal);
        byId("purchaseVatTotal").textContent = money(vatTotal);
        byId("purchaseGrandTotal").textContent = money(grandTotal);
    }

    function bindGrid() {
        byId("purchaseItemsBody").addEventListener("input", function (event) {
            var input = event.target;
            var row = input.closest("tr[data-index]");
            if (!row || !input.getAttribute("data-field")) { return; }
            var item = items[parseInt(row.getAttribute("data-index"), 10)];
            item[input.getAttribute("data-field")] = number(input.value);
            renderItems();
        });
        byId("purchaseItemsBody").addEventListener("click", function (event) {
            var remove = event.target.closest("[data-remove]");
            if (!remove) { return; }
            items.splice(parseInt(remove.getAttribute("data-remove"), 10), 1);
            renderItems();
        });
        byId("clearItemsBtn").addEventListener("click", function () {
            items = [];
            renderItems();
        });
    }

    function bindBranchStores() {
        byId("purchaseBranchId").addEventListener("change", function () {
            var url = page.getAttribute("data-stores-url") + "?branchId=" + encodeURIComponent(this.value);
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (!data || !data.success) { return; }
                    byId("purchaseStoreId").innerHTML = data.rows.map(function (store) {
                        return "<option value=\"" + html(store.StoreID) + "\">" + html(store.StoreName) + "</option>";
                    }).join("");
                });
        });
    }

    function collect() {
        var payloadItems = items.map(function (item) {
            var base = item.quantity * item.purchasePrice;
            return {
                ItemId: item.itemId,
                ItemName: item.itemName,
                UnitId: item.unitId,
                Quantity: item.quantity,
                PurchasePrice: item.purchasePrice,
                DiscountValue: item.discountValue,
                VatValue: item.vatValue,
                VatPercent: base > 0 ? (item.vatValue / Math.max(1, base - item.discountValue)) * 100 : 0,
                LineTotal: Math.max(0, base - item.discountValue + item.vatValue)
            };
        });
        return {
            InvoiceNumber: byId("purchaseInvoiceNumber").value,
            InvoiceDate: byId("purchaseInvoiceDate").value,
            SupplierId: parseInt(byId("purchaseSupplierId").value || "0", 10),
            BranchId: parseInt(byId("purchaseBranchId").value || "0", 10),
            StoreId: parseInt(byId("purchaseStoreId").value || "0", 10),
            PaymentType: parseInt(byId("purchasePaymentType").value || "1", 10),
            BoxId: byId("purchaseBoxId").value ? parseInt(byId("purchaseBoxId").value, 10) : null,
            BankId: byId("purchaseBankId").value ? parseInt(byId("purchaseBankId").value, 10) : null,
            ManualNo: byId("purchaseManualNo").value,
            Remarks: byId("purchaseRemarks").value,
            DiscountValue: payloadItems.reduce(function (sum, row) { return sum + row.DiscountValue; }, 0),
            VatValue: payloadItems.reduce(function (sum, row) { return sum + row.VatValue; }, 0),
            Items: payloadItems
        };
    }

    function bindSave() {
        byId("savePurchaseBtn").addEventListener("click", function () {
            var button = this;
            button.disabled = true;
            message("جاري الحفظ...", false);
            fetch(page.getAttribute("data-save-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(collect())
            })
                .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
                .then(function (result) {
                    if (!result.ok || !result.data.success) {
                        throw new Error((result.data && (result.data.message || result.data.technicalMessage)) || "تعذر الحفظ");
                    }
                    byId("purchaseInvoiceNumber").value = result.data.result.InvoiceNumber || byId("purchaseInvoiceNumber").value;
                    message("تم الحفظ. رقم الحركة: " + result.data.result.TransactionId + " | رقم الفاتورة: " + result.data.result.InvoiceNumber, false);
                })
                .catch(function (error) { message(error.message, true); })
                .finally(function () { button.disabled = false; });
        });
    }

    bindSupplierLookup();
    bindItemLookup();
    bindGrid();
    bindBranchStores();
    bindSave();
    renderItems();
}());
