(function () {
    "use strict";

    var page = document.querySelector(".purchase-page");
    if (!page) { return; }

    var rows = [];
    var rowSeed = 1;
    var lookupTimers = {};
    var gridPage = 1;
    var gridPageSize = 100;

    function byId(id) { return document.getElementById(id); }

    function toNumber(value) {
        var parsed = parseFloat(value);
        return isNaN(parsed) ? 0 : parsed;
    }

    function toMoney(value) {
        return toNumber(value).toFixed(2);
    }

    function text(value) {
        return String(value === null || value === undefined ? "" : value);
    }

    function escapeHtml(value) {
        return text(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function message(value, isError) {
        var target = byId("purchaseMessage");
        target.textContent = value || "";
        target.className = "purchase-message " + (isError ? "is-error" : "is-success");
    }

    function getUrl(name) {
        return page.getAttribute(name);
    }

    function query(kind, term, key, callback) {
        window.clearTimeout(lookupTimers[key]);
        lookupTimers[key] = window.setTimeout(function () {
            var url = getUrl("data-lookup-url") + "?kind=" + encodeURIComponent(kind) + "&term=" + encodeURIComponent(term || "");
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) { callback(data && data.success ? data.rows || [] : []); })
                .catch(function () { callback([]); });
        }, 180);
    }

    function createRow(initial) {
        var row = initial || {};
        return {
            id: row.id || rowSeed++,
            itemId: row.ItemId || row.itemId || null,
            itemCode: row.ItemCode || row.itemCode || "",
            itemName: row.ItemName || row.itemName || "",
            searchText: row.searchText || buildItemText(row.ItemCode || row.itemCode, row.ItemName || row.itemName),
            unitId: row.UnitId || row.unitId || null,
            unitName: row.UnitName || row.unitName || "",
            units: row.units || [],
            quantity: toNumber(row.Quantity || row.quantity || 1),
            purchasePrice: toNumber(row.PurchasePrice || row.purchasePrice || 0),
            discountValue: toNumber(row.DiscountValue || row.discountValue || 0),
            vatPercent: toNumber(row.VatPercent || row.vatPercent || 0),
            vatValue: toNumber(row.VatValue || row.vatValue || 0),
            haveSerial: !!(row.HaveSerial || row.haveSerial),
            itemSerial: row.ItemSerial || row.itemSerial || ""
        };
    }

    function buildItemText(code, name) {
        code = text(code).trim();
        name = text(name).trim();
        if (code && name) { return code + " - " + name; }
        return code || name || "";
    }

    function lineSubtotal(row) {
        return toNumber(row.quantity) * toNumber(row.purchasePrice);
    }

    function lineTotal(row) {
        return Math.max(0, lineSubtotal(row) - toNumber(row.discountValue) + toNumber(row.vatValue));
    }

    function recalcVatFromPercent(row) {
        if (row.vatPercent > 0) {
            row.vatValue = Math.round(Math.max(0, lineSubtotal(row) - row.discountValue) * row.vatPercent) / 100;
        }
    }

    function renderRows() {
        var body = byId("purchaseItemsBody");
        var totalPages = Math.max(1, Math.ceil(rows.length / gridPageSize));
        if (gridPage > totalPages) { gridPage = totalPages; }
        if (gridPage < 1) { gridPage = 1; }
        var start = (gridPage - 1) * gridPageSize;
        var pageRows = rows.slice(start, start + gridPageSize);
        body.innerHTML = pageRows.map(function (row, index) {
            var unitOptions = (row.units && row.units.length ? row.units : [{
                UnitId: row.unitId,
                UnitName: row.unitName || "",
                PurchasePrice: row.purchasePrice
            }]).map(function (unit) {
                var id = unit.UnitId || unit.unitId || "";
                var selected = text(id) === text(row.unitId) ? " selected" : "";
                return "<option value=\"" + escapeHtml(id) + "\" data-price=\"" + escapeHtml(unit.PurchasePrice || unit.purchasePrice || row.purchasePrice) + "\"" + selected + ">" + escapeHtml(unit.UnitName || unit.unitName || "") + "</option>";
            }).join("");

            return "<tr data-row-id=\"" + row.id + "\" class=\"" + (row.haveSerial ? "is-serial" : "") + "\">" +
                "<td class=\"item-cell\"><div class=\"lookup-wrap\"><input type=\"hidden\" class=\"row-item-id\" value=\"" + escapeHtml(row.itemId || "") + "\" />" +
                "<input type=\"text\" class=\"form-control row-item-search\" value=\"" + escapeHtml(row.searchText) + "\" placeholder=\"كود أو اسم الصنف\" autocomplete=\"off\" />" +
                "<div class=\"lookup-results row-item-results\" role=\"listbox\"></div></div></td>" +
                "<td class=\"unit-cell\"><select class=\"form-control row-unit\">" + unitOptions + "</select></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.001\" class=\"form-control row-number\" data-field=\"quantity\" value=\"" + escapeHtml(row.quantity) + "\" /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"purchasePrice\" value=\"" + escapeHtml(row.purchasePrice) + "\" /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"discountValue\" value=\"" + escapeHtml(row.discountValue) + "\" /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"vatPercent\" value=\"" + escapeHtml(row.vatPercent) + "\" /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"vatValue\" value=\"" + escapeHtml(row.vatValue) + "\" /></td>" +
                "<td class=\"number-cell\"><div class=\"line-total\">" + toMoney(lineTotal(row)) + "</div></td>" +
                "<td class=\"serial-cell\"><input type=\"text\" class=\"form-control row-serial\" value=\"" + escapeHtml(row.itemSerial) + "\" placeholder=\"رقم السيريال\" />" +
                "<div class=\"serial-badge\">صنف مسلسل: الكمية 1 والسيريال مطلوب</div></td>" +
                "<td class=\"remove-cell\"><button type=\"button\" class=\"btn btn-danger btn-sm row-remove\">حذف</button></td>" +
                "</tr>";
        }).join("");
        renderTotals();
        renderGridPager();
    }

    function renderGridPager() {
        var pager = byId("purchaseItemsPager");
        if (!pager) { return; }
        var totalRows = rows.length;
        var totalPages = Math.max(1, Math.ceil(totalRows / gridPageSize));
        if (gridPage > totalPages) { gridPage = totalPages; }
        var start = totalRows ? ((gridPage - 1) * gridPageSize) + 1 : 0;
        var end = Math.min(totalRows, gridPage * gridPageSize);
        pager.innerHTML =
            "<div class=\"grid-page-info\">عرض " + start + " - " + end + " من " + totalRows + " سطر</div>" +
            "<div class=\"grid-page-actions\">" +
            "<button type=\"button\" class=\"btn btn-default btn-sm\" data-grid-page=\"first\"" + (gridPage <= 1 ? " disabled" : "") + ">الأولى</button>" +
            "<button type=\"button\" class=\"btn btn-default btn-sm\" data-grid-page=\"prev\"" + (gridPage <= 1 ? " disabled" : "") + ">السابق</button>" +
            "<span class=\"grid-page-current\">صفحة " + gridPage + " / " + totalPages + "</span>" +
            "<button type=\"button\" class=\"btn btn-default btn-sm\" data-grid-page=\"next\"" + (gridPage >= totalPages ? " disabled" : "") + ">التالي</button>" +
            "<button type=\"button\" class=\"btn btn-default btn-sm\" data-grid-page=\"last\"" + (gridPage >= totalPages ? " disabled" : "") + ">الأخيرة</button>" +
            "</div>";
    }

    function renderTotals() {
        var subtotal = 0;
        var discount = 0;
        var vat = 0;
        var net = 0;
        rows.forEach(function (row) {
            subtotal += lineSubtotal(row);
            discount += toNumber(row.discountValue);
            vat += toNumber(row.vatValue);
            net += lineTotal(row);
        });
        byId("purchaseSubtotal").textContent = toMoney(subtotal);
        byId("purchaseDiscountTotal").textContent = toMoney(discount);
        byId("purchaseVatTotal").textContent = toMoney(vat);
        byId("purchaseNetTotal").textContent = toMoney(net);
    }

    function findRowFromElement(element) {
        var tr = element.closest("tr[data-row-id]");
        if (!tr) { return null; }
        var id = parseInt(tr.getAttribute("data-row-id"), 10);
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].id === id) { return rows[i]; }
        }
        return null;
    }

    function renderLookupResults(container, items, onPick) {
        container.innerHTML = "";
        if (!items.length) {
            container.classList.remove("is-open");
            return;
        }

        items.forEach(function (item) {
            var button = document.createElement("button");
            button.type = "button";
            button.textContent = buildItemText(item.ItemCode || item.itemCode || item.id, item.ItemName || item.text || item.itemName);
            button._purchasePayload = item;
            button.addEventListener("click", function () { onPick(button._purchasePayload); });
            container.appendChild(button);
        });
        container.classList.add("is-open");
    }

    function bindSupplierLookup() {
        var input = byId("purchaseSupplierText");
        var hidden = byId("purchaseSupplierId");
        var results = byId("supplierResults");
        input.addEventListener("input", function () {
            hidden.value = "";
            if (input.value.trim().length < 1) {
                results.classList.remove("is-open");
                results.innerHTML = "";
                return;
            }

            query("supplier", input.value, "supplier", function (items) {
                renderLookupResults(results, items, function (item) {
                    hidden.value = item.id;
                    input.value = item.text;
                    results.classList.remove("is-open");
                    results.innerHTML = "";
                });
            });
        });

        byId("clearSupplierBtn").addEventListener("click", function () {
            hidden.value = "";
            input.value = "";
            results.classList.remove("is-open");
            results.innerHTML = "";
        });
    }

    function loadUnits(row) {
        if (!row.itemId) { return; }
        var url = getUrl("data-units-url") + "?itemId=" + encodeURIComponent(row.itemId);
        fetch(url, { credentials: "same-origin" })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (!data || !data.success) { return; }
                row.units = data.rows || [];
                var selected = row.units.filter(function (unit) { return text(unit.UnitId) === text(row.unitId); })[0] || row.units[0];
                if (selected) {
                    row.unitId = selected.UnitId;
                    row.unitName = selected.UnitName;
                    if (!row.purchasePrice) {
                        row.purchasePrice = toNumber(selected.PurchasePrice);
                    }
                }
                renderRows();
            });
    }

    function pickItem(row, item) {
        row.itemId = item.Item_ID;
        row.itemCode = item.ItemCode || "";
        row.itemName = item.ItemName || "";
        row.searchText = buildItemText(row.itemCode, row.itemName);
        row.unitId = item.UnitId || null;
        row.unitName = item.UnitName || "";
        row.purchasePrice = toNumber(item.Price);
        row.haveSerial = !!item.HaveSerial;
        if (row.haveSerial) {
            row.quantity = 1;
        }
        row.units = [];
        loadUnits(row);
        renderRows();
    }

    function bindGrid() {
        byId("addPurchaseRowBtn").addEventListener("click", function () {
            rows.push(createRow());
            gridPage = Math.ceil(rows.length / gridPageSize);
            renderRows();
        });

        byId("purchaseItemsBody").addEventListener("input", function (event) {
            var target = event.target;
            var row = findRowFromElement(target);
            if (!row) { return; }

            if (target.classList.contains("row-item-search")) {
                row.searchText = target.value;
                row.itemId = null;
                row.itemCode = "";
                row.itemName = "";
                var results = target.parentNode.querySelector(".row-item-results");
                if (target.value.trim().length < 1) {
                    results.classList.remove("is-open");
                    results.innerHTML = "";
                    return;
                }

                query("item", target.value, "item-" + row.id, function (items) {
                    renderLookupResults(results, items, function (item) {
                        pickItem(row, item);
                    });
                });
                return;
            }

            if (target.classList.contains("row-number")) {
                var field = target.getAttribute("data-field");
                row[field] = toNumber(target.value);
                if (row.haveSerial && field === "quantity") {
                    row.quantity = 1;
                }
                if (field === "vatPercent" || field === "quantity" || field === "purchasePrice" || field === "discountValue") {
                    recalcVatFromPercent(row);
                }
                renderRows();
                return;
            }

            if (target.classList.contains("row-serial")) {
                row.itemSerial = target.value;
            }
        });

        byId("purchaseItemsBody").addEventListener("change", function (event) {
            var target = event.target;
            var row = findRowFromElement(target);
            if (!row || !target.classList.contains("row-unit")) { return; }

            var selected = target.options[target.selectedIndex];
            row.unitId = target.value ? parseInt(target.value, 10) : null;
            row.unitName = selected ? selected.text : "";
            if (selected && selected.getAttribute("data-price")) {
                row.purchasePrice = toNumber(selected.getAttribute("data-price"));
            }
            renderRows();
        });

        byId("purchaseItemsBody").addEventListener("click", function (event) {
            if (!event.target.classList.contains("row-remove")) { return; }
            var row = findRowFromElement(event.target);
            if (!row) { return; }
            rows = rows.filter(function (candidate) { return candidate.id !== row.id; });
            if (!rows.length) {
                rows.push(createRow());
            }
            renderRows();
        });

        var pager = byId("purchaseItemsPager");
        if (pager) {
            pager.addEventListener("click", function (event) {
                var button = event.target.closest("[data-grid-page]");
                if (!button || button.disabled) { return; }
                var action = button.getAttribute("data-grid-page");
                var totalPages = Math.max(1, Math.ceil(rows.length / gridPageSize));
                if (action === "first") { gridPage = 1; }
                if (action === "prev") { gridPage = Math.max(1, gridPage - 1); }
                if (action === "next") { gridPage = Math.min(totalPages, gridPage + 1); }
                if (action === "last") { gridPage = totalPages; }
                renderRows();
            });
        }
    }

    function bindPaymentFields() {
        var select = byId("purchasePaymentType");
        function refresh() {
            var value = select.value;
            var cash = document.querySelector("[data-payment-field='cash']");
            var bank = document.querySelector("[data-payment-field='bank']");
            cash.classList.toggle("is-visible", value === "0");
            bank.classList.toggle("is-visible", value === "3");
        }
        select.addEventListener("change", refresh);
        refresh();
    }

    function bindBranchStores() {
        var branch = byId("purchaseBranchId");
        branch.addEventListener("change", function () {
            var url = getUrl("data-stores-url") + "?branchId=" + encodeURIComponent(branch.value);
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (!data || !data.success) { return; }
                    byId("purchaseStoreId").innerHTML = (data.rows || []).map(function (store) {
                        return "<option value=\"" + escapeHtml(store.StoreID) + "\">" + escapeHtml(store.StoreName) + "</option>";
                    }).join("");
                });
        });
    }

    function validatePayload(payload) {
        if (!payload.InvoiceDate) { return "تاريخ الفاتورة مطلوب"; }
        if (!payload.SupplierId) { return "المورد مطلوب"; }
        if (!payload.BranchId) { return "الفرع مطلوب"; }
        if (!payload.StoreId) { return "المخزن مطلوب"; }
        if (payload.PaymentType === 0 && !payload.BoxId) { return "الخزنة مطلوبة عند الدفع النقدي"; }
        if (payload.PaymentType === 3 && !payload.BankId) { return "البنك مطلوب عند الدفع البنكي"; }
        if (!payload.Items.length) { return "أضف صنفاً واحداً على الأقل"; }

        var serials = {};
        for (var i = 0; i < payload.Items.length; i++) {
            var item = payload.Items[i];
            if (!item.ItemId) { return "يوجد صف بدون صنف محدد"; }
            if (item.Quantity <= 0) { return "كمية كل صنف يجب أن تكون أكبر من صفر"; }
            if (item.PurchasePrice < 0 || item.DiscountValue < 0 || item.VatValue < 0) { return "القيم المالية لا يمكن أن تكون سالبة"; }
            if (item.HaveSerial) {
                if (item.Quantity !== 1) { return "كمية الصنف المسلسل يجب أن تكون 1"; }
                if (!item.ItemSerial) { return "السيريال مطلوب للصنف المسلسل: " + item.ItemName; }
                var key = item.ItemSerial.toLowerCase();
                if (serials[key]) { return "السيريال مكرر في نفس الفاتورة: " + item.ItemSerial; }
                serials[key] = true;
            }
        }

        return "";
    }

    function collect() {
        var payloadItems = rows.filter(function (row) { return row.itemId; }).map(function (row) {
            if (row.haveSerial) {
                row.quantity = 1;
            }
            recalcVatFromPercent(row);
            return {
                ItemId: row.itemId,
                ItemCode: row.itemCode,
                ItemName: row.itemName,
                UnitId: row.unitId,
                UnitName: row.unitName,
                Quantity: row.quantity,
                PurchasePrice: row.purchasePrice,
                DiscountValue: row.discountValue,
                VatPercent: row.vatPercent,
                VatValue: row.vatValue,
                LineTotal: lineTotal(row),
                HaveSerial: row.haveSerial,
                ItemSerial: text(row.itemSerial).trim()
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
            var payload = collect();
            var error = validatePayload(payload);
            if (error) {
                message(error, true);
                return;
            }

            var button = this;
            button.disabled = true;
            message("جاري الحفظ...", false);
            fetch(getUrl("data-save-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            })
                .then(function (response) {
                    return response.json().then(function (data) { return { ok: response.ok, data: data }; });
                })
                .then(function (result) {
                    if (!result.ok || !result.data.success) {
                        throw new Error((result.data && (result.data.technicalMessage || result.data.message)) || "تعذر الحفظ");
                    }
                    byId("purchaseInvoiceNumber").value = result.data.result.InvoiceNumber || byId("purchaseInvoiceNumber").value;
                    message("تم الحفظ. رقم الحركة: " + result.data.result.TransactionId + " | رقم الفاتورة: " + result.data.result.InvoiceNumber, false);
                })
                .catch(function (error) { message(error.message, true); })
                .finally(function () { button.disabled = false; });
        });
    }

    function bindImport() {
        byId("importPurchaseBtn").addEventListener("click", function () {
            var fileInput = byId("purchaseImportFile");
            if (!fileInput.files || !fileInput.files.length) {
                message("اختر ملف Excel للاستيراد", true);
                return;
            }

            var formData = new FormData();
            formData.append("file", fileInput.files[0]);
            message("جاري استيراد الملف...", false);

            fetch(getUrl("data-import-url"), {
                method: "POST",
                credentials: "same-origin",
                body: formData
            })
                .then(function (response) {
                    return response.json().then(function (data) { return { ok: response.ok, data: data }; });
                })
                .then(function (result) {
                    if (!result.ok || !result.data.success) {
                        throw new Error((result.data && result.data.message) || "تعذر استيراد الملف");
                    }

                    var importResult = result.data.result || { Accepted: [], Rejected: [] };
                    (importResult.Accepted || []).forEach(function (item) {
                        rows.push(createRow(item));
                    });
                    if (!rows.length) {
                        rows.push(createRow());
                    }
                    renderRows();
                    renderImportResult(importResult);
                    message("تم استيراد " + (importResult.Accepted || []).length + " صف. المرفوض: " + (importResult.Rejected || []).length, false);
                })
                .catch(function (error) { message(error.message, true); });
        });
    }

    function renderImportResult(result) {
        var box = byId("purchaseImportResult");
        var rejected = result.Rejected || [];
        var acceptedCount = (result.Accepted || []).length;
        var html = "<h3>نتيجة الاستيراد</h3><p>المقبول: " + acceptedCount + " | المرفوض: " + rejected.length + "</p>";
        if (rejected.length) {
            html += "<div class=\"table-responsive\"><table class=\"table table-condensed table-bordered\"><thead><tr><th>الصف</th><th>الصنف</th><th>السيريال</th><th>السبب</th></tr></thead><tbody>";
            html += rejected.map(function (row) {
                return "<tr><td>" + escapeHtml(row.RowNumber) + "</td><td>" + escapeHtml(row.ItemText) + "</td><td>" + escapeHtml(row.Serial) + "</td><td>" + escapeHtml(row.Reason) + "</td></tr>";
            }).join("");
            html += "</tbody></table></div>";
        }
        box.innerHTML = html;
        box.classList.add("is-open");
    }

    document.addEventListener("click", function (event) {
        if (!event.target.closest(".lookup-wrap")) {
            Array.prototype.forEach.call(document.querySelectorAll(".lookup-results"), function (el) {
                el.classList.remove("is-open");
            });
        }
    });

    function bindIndexWorkflow() {
        var indexPanel = byId("purchaseIndexPanel");
        var entryPanel = byId("purchaseEntryPanel");
        var searchBody = byId("purchaseIndexBody");
        if (!indexPanel || !entryPanel || !searchBody) { return; }

        function showEntry() {
            indexPanel.classList.add("is-hidden-ui");
            entryPanel.classList.remove("is-hidden-ui");
            window.scrollTo(0, 0);
        }

        function showIndex() {
            entryPanel.classList.add("is-hidden-ui");
            indexPanel.classList.remove("is-hidden-ui");
            window.scrollTo(0, 0);
        }

        function emptyRow(messageText) {
            searchBody.innerHTML = "<tr><td colspan=\"8\" class=\"empty-index-row\">" + escapeHtml(messageText) + "</td></tr>";
        }

        function renderSearchRows(items) {
            if (!items || !items.length) {
                emptyRow("لا توجد فواتير مطابقة للفلاتر.");
                return;
            }

            searchBody.innerHTML = items.map(function (row) {
                return "<tr>" +
                    "<td>" + escapeHtml(row.InvoiceNumber) + "</td>" +
                    "<td>" + escapeHtml(row.InvoiceDate) + "</td>" +
                    "<td>" + escapeHtml(row.SupplierName) + "</td>" +
                    "<td>" + escapeHtml(row.BranchName) + "</td>" +
                    "<td>" + escapeHtml(row.StoreName) + "</td>" +
                    "<td>" + escapeHtml(toMoney(row.NetTotal)) + "</td>" +
                    "<td>" + escapeHtml(row.Status) + "</td>" +
                    "<td><button type=\"button\" class=\"btn btn-default btn-sm\" data-purchase-view=\"" + escapeHtml(row.TransactionId) + "\">عرض</button></td>" +
                    "</tr>";
            }).join("");
        }

        function search() {
            emptyRow("جاري البحث...");
            fetch(getUrl("data-search-url"), {
                method: "POST",
                credentials: "same-origin",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    InvoiceNumber: byId("purchaseSearchInvoice").value,
                    FromDate: byId("purchaseSearchFrom").value,
                    ToDate: byId("purchaseSearchTo").value,
                    SupplierTerm: byId("purchaseSearchSupplier").value,
                    BranchId: byId("purchaseSearchBranch").value || null,
                    StoreId: byId("purchaseSearchStore").value || null,
                    PaymentType: byId("purchaseSearchPayment").value || null
                })
            })
                .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
                .then(function (result) {
                    if (!result.ok || !result.data.success) {
                        throw new Error((result.data && result.data.message) || "تعذر البحث");
                    }
                    renderSearchRows(result.data.rows || []);
                })
                .catch(function (error) { emptyRow(error.message); });
        }

        function loadInvoice(transactionId) {
            message("جاري تحميل الفاتورة...", false);
            fetch(getUrl("data-get-url") + "?transactionId=" + encodeURIComponent(transactionId), { credentials: "same-origin" })
                .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
                .then(function (result) {
                    if (!result.ok || !result.data.success || !result.data.invoice) {
                        throw new Error((result.data && result.data.message) || "تعذر تحميل الفاتورة");
                    }

                    var invoice = result.data.invoice;
                    byId("purchaseInvoiceNumber").value = invoice.InvoiceNumber || "";
                    byId("purchaseInvoiceDate").value = invoice.InvoiceDate || "";
                    byId("purchaseSupplierId").value = invoice.SupplierId || "";
                    byId("purchaseSupplierText").value = invoice.SupplierName || "";
                    byId("purchaseManualNo").value = invoice.ManualNo || "";
                    byId("purchaseBranchId").value = invoice.BranchId || "";
                    byId("purchaseStoreId").value = invoice.StoreId || "";
                    byId("purchasePaymentType").value = invoice.PaymentType === 0 || invoice.PaymentType === 3 ? invoice.PaymentType : 1;
                    byId("purchaseBoxId").value = invoice.BoxId || "";
                    byId("purchaseBankId").value = invoice.BankId || "";
                    byId("purchaseRemarks").value = invoice.Remarks || "";
                    rows = (invoice.Items || []).map(function (item) { return createRow(item); });
                    if (!rows.length) { rows.push(createRow()); }
                    gridPage = 1;
                    renderRows();
                    byId("purchasePaymentType").dispatchEvent(new Event("change"));
                    showEntry();
                    message("تم تحميل الفاتورة للعرض.", false);
                })
                .catch(function (error) { message(error.message, true); });
        }

        byId("purchaseAddNewBtn").addEventListener("click", showEntry);
        byId("purchaseBackToIndexBtn").addEventListener("click", showIndex);
        byId("purchaseSearchBtn").addEventListener("click", search);
        byId("purchaseClearSearchBtn").addEventListener("click", function () {
            byId("purchaseSearchInvoice").value = "";
            byId("purchaseSearchSupplier").value = "";
            byId("purchaseSearchPayment").value = "";
            emptyRow("اضغط بحث لعرض فواتير المشتريات.");
        });
        searchBody.addEventListener("click", function (event) {
            var button = event.target.closest("[data-purchase-view]");
            if (!button) { return; }
            loadInvoice(button.getAttribute("data-purchase-view"));
        });
    }

    bindIndexWorkflow();
    bindSupplierLookup();
    bindGrid();
    bindPaymentFields();
    bindBranchStores();
    bindSave();
    bindImport();
    rows.push(createRow());
    renderRows();
}());
