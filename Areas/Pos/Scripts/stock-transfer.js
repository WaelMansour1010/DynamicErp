(function () {
    "use strict";

    var page = document.querySelector(".stock-page");
    if (!page) { return; }

    var rows = [];
    var rowSeed = 1;
    var lookupTimers = {};
    var busy = false;
    var lastImportResults = [];

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
        var target = byId("transferMessage");
        target.textContent = value || "";
        target.className = "stock-message " + (isError ? "is-error" : "is-success");
    }

    function getUrl(name) {
        return page.getAttribute(name);
    }

    function setBusy(value) {
        busy = value;
        byId("saveTransferBtn").disabled = value;
        byId("importSerialTextBtn").disabled = value;
        byId("importExcelBtn").disabled = value;
        byId("addTransferRowBtn").disabled = value;
    }

    function buildItemText(code, name) {
        code = text(code).trim();
        name = text(name).trim();
        if (code && name) { return code + " - " + name; }
        return code || name || "";
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
            quantity: toNumber(row.Quantity || row.quantity || 1),
            unitFactor: toNumber(row.UnitFactor || row.unitFactor || 1),
            price: toNumber(row.Price || row.price || 0),
            haveSerial: !!(row.HaveSerial || row.haveSerial),
            serial: row.Serial || row.serial || ""
        };
    }

    function lineCost(row) {
        return toNumber(row.quantity) * toNumber(row.unitFactor || 1) * toNumber(row.price);
    }

    function query(term, key, callback) {
        window.clearTimeout(lookupTimers[key]);
        lookupTimers[key] = window.setTimeout(function () {
            var url = getUrl("data-lookup-url") + "?term=" + encodeURIComponent(term || "");
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) { callback(data && data.success ? data.rows || [] : []); })
                .catch(function () { callback([]); });
        }, 180);
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
            button._transferPayload = item;
            button.addEventListener("click", function () { onPick(button._transferPayload); });
            container.appendChild(button);
        });
        container.classList.add("is-open");
    }

    function renderRows() {
        var body = byId("transferItemsBody");
        body.innerHTML = rows.map(function (row) {
            return "<tr data-row-id=\"" + row.id + "\" class=\"" + (row.haveSerial ? "is-serial" : "") + "\">" +
                "<td class=\"item-cell\"><div class=\"lookup-wrap\">" +
                "<input type=\"hidden\" class=\"row-item-id\" value=\"" + escapeHtml(row.itemId || "") + "\" />" +
                "<input type=\"text\" class=\"form-control row-item-search\" value=\"" + escapeHtml(row.searchText) + "\" placeholder=\"كود أو اسم الصنف\" autocomplete=\"off\" />" +
                "<div class=\"lookup-results row-item-results\" role=\"listbox\"></div></div></td>" +
                "<td class=\"unit-cell\"><input type=\"text\" class=\"form-control\" value=\"" + escapeHtml(row.unitName) + "\" readonly /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.001\" class=\"form-control row-number\" data-field=\"quantity\" value=\"" + escapeHtml(row.quantity) + "\" " + (row.haveSerial ? "readonly" : "") + " /></td>" +
                "<td class=\"serial-cell\"><input type=\"text\" class=\"form-control row-serial\" value=\"" + escapeHtml(row.serial) + "\" placeholder=\"رقم السيريال\" " + (!row.haveSerial ? "disabled" : "") + " />" +
                "<div class=\"serial-badge\">صنف مسلسل: الكمية 1 والسيريال مطلوب</div></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"price\" value=\"" + escapeHtml(row.price) + "\" /></td>" +
                "<td class=\"remove-cell\"><button type=\"button\" class=\"btn btn-danger btn-sm row-remove\">حذف</button></td>" +
                "</tr>";
        }).join("");
        renderTotals();
    }

    function renderTotals() {
        var itemCount = rows.filter(function (row) { return row.itemId; }).length;
        var quantity = 0;
        var cost = 0;
        rows.forEach(function (row) {
            quantity += toNumber(row.quantity);
            cost += lineCost(row);
        });
        byId("transferItemTotal").textContent = itemCount;
        byId("transferQuantityTotal").textContent = quantity.toFixed(3);
        byId("transferCostTotal").textContent = toMoney(cost);
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

    function pickItem(row, item) {
        row.itemId = item.Item_ID;
        row.itemCode = item.ItemCode || "";
        row.itemName = item.ItemName || "";
        row.searchText = buildItemText(row.itemCode, row.itemName);
        row.unitId = item.UnitId || null;
        row.unitName = item.UnitName || "";
        row.quantity = 1;
        row.unitFactor = toNumber(item.QtyBySmalltUnit || 1) || 1;
        row.price = toNumber(item.CostPrice || item.Price || 0);
        row.haveSerial = !!item.HaveSerial;
        if (!row.haveSerial) {
            row.serial = "";
        }
        renderRows();
    }

    function bindGrid() {
        byId("addTransferRowBtn").addEventListener("click", function () {
            rows.push(createRow());
            renderRows();
        });

        byId("transferItemsBody").addEventListener("input", function (event) {
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

                query(target.value, "item-" + row.id, function (items) {
                    renderLookupResults(results, items, function (item) {
                        pickItem(row, item);
                    });
                });
                return;
            }

            if (target.classList.contains("row-number")) {
                var field = target.getAttribute("data-field");
                row[field] = toNumber(target.value);
                if (row.haveSerial) {
                    row.quantity = 1;
                }
                renderRows();
                return;
            }

            if (target.classList.contains("row-serial")) {
                row.serial = target.value.trim();
                renderTotals();
            }
        });

        byId("transferItemsBody").addEventListener("click", function (event) {
            if (!event.target.classList.contains("row-remove")) { return; }
            var row = findRowFromElement(event.target);
            if (!row) { return; }
            rows = rows.filter(function (candidate) { return candidate.id !== row.id; });
            if (!rows.length) {
                rows.push(createRow());
            }
            renderRows();
        });

        byId("clearTransferBtn").addEventListener("click", function () {
            rows = [createRow()];
            lastImportResults = [];
            showImportResults();
            renderRows();
            message("", false);
        });
    }

    function bindBranchStores() {
        byId("transferBranchId").addEventListener("change", function () {
            var url = getUrl("data-stores-url") + "?branchId=" + encodeURIComponent(this.value);
            fetch(url, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (!data || !data.success) { return; }
                    var options = data.rows.map(function (store) {
                        return "<option value=\"" + escapeHtml(store.StoreID) + "\">" + escapeHtml(store.StoreName) + "</option>";
                    }).join("");
                    byId("transferSourceStoreId").innerHTML = options;
                });
        });
    }

    function showImportResults() {
        var panel = byId("importResultsPanel");
        var body = byId("importResultsBody");
        panel.hidden = lastImportResults.length === 0;
        body.innerHTML = lastImportResults.map(function (row) {
            var accepted = row.status === "accepted";
            return "<tr>" +
                "<td>" + escapeHtml(row.serial) + "</td>" +
                "<td>" + escapeHtml(row.itemName || "") + "</td>" +
                "<td><span class=\"status-pill " + (accepted ? "accepted" : "rejected") + "\">" + (accepted ? "مقبول" : "مرفوض") + "</span></td>" +
                "<td>" + escapeHtml(row.reason || "") + "</td>" +
                "</tr>";
        }).join("");
    }

    function appendAccepted(accepted) {
        (accepted || []).forEach(function (row) {
            var key = text(row.Serial).toLowerCase();
            if (key && rows.some(function (item) { return text(item.serial).toLowerCase() === key; })) {
                lastImportResults.push({ serial: row.Serial, itemName: row.ItemName, status: "rejected", reason: "السيريال موجود بالفعل في الجدول" });
                return;
            }

            rows.push(createRow({
                ItemId: row.ItemId,
                ItemName: row.ItemName,
                UnitId: row.UnitId,
                UnitName: row.UnitName,
                Quantity: 1,
                UnitFactor: row.UnitFactor || 1,
                Price: row.Price || 0,
                HaveSerial: true,
                Serial: row.Serial
            }));
            lastImportResults.push({ serial: row.Serial, itemName: row.ItemName, status: "accepted", reason: "" });
        });
        rows = rows.filter(function (row) { return row.itemId || row.searchText || row.serial; });
        if (!rows.length) { rows.push(createRow()); }
        renderRows();
    }

    function appendRejected(rejected) {
        (rejected || []).forEach(function (row) {
            lastImportResults.push({
                serial: row.Serial || "",
                itemName: "",
                status: "rejected",
                reason: row.Reason || ""
            });
        });
    }

    function importSerials(serials) {
        if (busy) { return; }
        if (!serials.length) {
            message("أدخل سيريال واحد على الأقل", true);
            return;
        }

        setBusy(true);
        lastImportResults = [];
        message("جاري فحص السيريالات...", false);
        fetch(getUrl("data-import-url"), {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                BranchId: parseInt(byId("transferBranchId").value || "0", 10),
                SourceStoreId: parseInt(byId("transferSourceStoreId").value || "0", 10),
                TransferDate: byId("transferDate").value,
                Serials: serials
            })
        })
            .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
            .then(function (result) {
                if (!result.ok || !result.data.success) {
                    throw new Error((result.data && (result.data.message || result.data.technicalMessage)) || "تعذر فحص السيريالات");
                }
                appendAccepted(result.data.result.Accepted || []);
                appendRejected(result.data.result.Rejected || []);
                showImportResults();
                message("تم قبول " + (result.data.result.Accepted || []).length + " سيريال ورفض " + (result.data.result.Rejected || []).length, false);
            })
            .catch(function (error) { message(error.message, true); })
            .finally(function () { setBusy(false); });
    }

    function bindImport() {
        byId("importSerialTextBtn").addEventListener("click", function () {
            var serials = byId("serialText").value.split(/\r?\n|,|;/).map(function (x) { return x.trim(); }).filter(Boolean);
            importSerials(serials);
        });

        byId("serialExcelFile").addEventListener("change", function () {
            byId("serialFileName").textContent = this.files && this.files.length ? this.files[0].name : "لم يتم اختيار ملف";
        });

        byId("importExcelBtn").addEventListener("click", function () {
            if (busy) { return; }
            var fileInput = byId("serialExcelFile");
            if (!fileInput.files || !fileInput.files.length) {
                message("اختر ملف Excel أولاً", true);
                return;
            }

            var form = new FormData();
            form.append("file", fileInput.files[0]);
            form.append("branchId", byId("transferBranchId").value || "0");
            form.append("sourceStoreId", byId("transferSourceStoreId").value || "0");
            form.append("transferDate", byId("transferDate").value);
            setBusy(true);
            lastImportResults = [];
            message("جاري استيراد Excel...", false);
            fetch(getUrl("data-import-excel-url"), {
                method: "POST",
                credentials: "same-origin",
                body: form
            })
                .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
                .then(function (result) {
                    if (!result.ok || !result.data.success) {
                        throw new Error((result.data && (result.data.message || result.data.technicalMessage)) || "تعذر استيراد Excel");
                    }
                    appendAccepted(result.data.result.Accepted || []);
                    appendRejected(result.data.result.Rejected || []);
                    showImportResults();
                    message("تم قبول " + (result.data.result.Accepted || []).length + " سيريال ورفض " + (result.data.result.Rejected || []).length, false);
                })
                .catch(function (error) { message(error.message, true); })
                .finally(function () { setBusy(false); });
        });
    }

    function validateBeforeSave() {
        var selectedRows = rows.filter(function (row) { return row.itemId; });
        var serials = {};

        if (!selectedRows.length) {
            return "يجب إضافة صنف واحد على الأقل";
        }

        if (byId("transferSourceStoreId").value === byId("transferDestinationStoreId").value) {
            return "لا يمكن التحويل إلى نفس المخزن";
        }

        for (var i = 0; i < selectedRows.length; i++) {
            var row = selectedRows[i];
            if (toNumber(row.quantity) <= 0) {
                return "كمية كل صنف يجب أن تكون أكبر من صفر";
            }
            if (toNumber(row.price) < 0) {
                return "التكلفة لا يمكن أن تكون سالبة";
            }
            if (row.haveSerial) {
                row.quantity = 1;
                if (!text(row.serial).trim()) {
                    return "السيريال مطلوب للصنف: " + row.searchText;
                }
                var key = text(row.serial).trim().toLowerCase();
                if (serials[key]) {
                    return "يوجد سيريال مكرر في الجدول: " + row.serial;
                }
                serials[key] = true;
            }
        }

        return "";
    }

    function collect() {
        return {
            VoucherNumber: byId("transferVoucherNumber").value,
            TransferDate: byId("transferDate").value,
            BranchId: parseInt(byId("transferBranchId").value || "0", 10),
            SourceStoreId: parseInt(byId("transferSourceStoreId").value || "0", 10),
            DestinationStoreId: parseInt(byId("transferDestinationStoreId").value || "0", 10),
            Remarks: byId("transferRemarks").value,
            Items: rows.filter(function (row) { return row.itemId; }).map(function (row) {
                return {
                    ItemId: row.itemId,
                    ItemName: row.itemName,
                    UnitId: row.unitId,
                    UnitName: row.unitName,
                    Quantity: row.haveSerial ? 1 : toNumber(row.quantity),
                    UnitFactor: toNumber(row.unitFactor) || 1,
                    Price: toNumber(row.price),
                    HaveSerial: !!row.haveSerial,
                    Serial: row.serial
                };
            })
        };
    }

    function bindSave() {
        byId("saveTransferBtn").addEventListener("click", function () {
            if (busy) { return; }
            var validationMessage = validateBeforeSave();
            if (validationMessage) {
                message(validationMessage, true);
                return;
            }

            setBusy(true);
            message("جاري حفظ سند التحويل...", false);
            fetch(getUrl("data-save-url"), {
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
                    byId("transferVoucherNumber").value = result.data.result.VoucherNumber || byId("transferVoucherNumber").value;
                    message("تم الحفظ. سند: " + result.data.result.VoucherNumber + " | حركة الصرف: " + result.data.result.SourceTransactionId + " | حركة الاستلام: " + result.data.result.DestinationTransactionId, false);
                })
                .catch(function (error) { message(error.message, true); })
                .finally(function () { setBusy(false); });
        });
    }

    document.addEventListener("click", function (event) {
        if (event.target.closest(".lookup-wrap")) { return; }
        Array.prototype.forEach.call(document.querySelectorAll(".lookup-results"), function (element) {
            element.classList.remove("is-open");
        });
    });

    function bindIndexWorkflow() {
        var indexPanel = byId("stockIndexPanel");
        var entryPanel = byId("stockEntryPanel");
        var searchBody = byId("stockIndexBody");
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
            searchBody.innerHTML = "<tr><td colspan=\"7\" class=\"empty-index-row\">" + escapeHtml(messageText) + "</td></tr>";
        }

        function renderSearchRows(items) {
            if (!items || !items.length) {
                emptyRow("لا توجد سندات مطابقة للفلاتر.");
                return;
            }

            searchBody.innerHTML = items.map(function (row) {
                return "<tr>" +
                    "<td>" + escapeHtml(row.VoucherNumber) + "</td>" +
                    "<td>" + escapeHtml(row.TransferDate) + "</td>" +
                    "<td>" + escapeHtml(row.SourceStoreName) + "</td>" +
                    "<td>" + escapeHtml(row.DestinationStoreName) + "</td>" +
                    "<td>" + escapeHtml(row.ItemCount) + "</td>" +
                    "<td>" + escapeHtml(toNumber(row.TotalQuantity).toFixed(3)) + "</td>" +
                    "<td><button type=\"button\" class=\"btn btn-default btn-sm\" data-stock-view=\"" + escapeHtml(row.SourceTransactionId) + "\">عرض</button></td>" +
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
                    VoucherNumber: byId("stockSearchVoucher").value,
                    FromDate: byId("stockSearchFrom").value,
                    ToDate: byId("stockSearchTo").value,
                    BranchId: byId("stockSearchBranch").value || null,
                    SourceStoreId: byId("stockSearchSourceStore").value || null,
                    DestinationStoreId: byId("stockSearchDestinationStore").value || null,
                    ItemOrSerialTerm: byId("stockSearchItem").value
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

        byId("stockAddNewBtn").addEventListener("click", showEntry);
        byId("stockBackToIndexBtn").addEventListener("click", showIndex);
        byId("stockSearchBtn").addEventListener("click", search);
        byId("stockClearSearchBtn").addEventListener("click", function () {
            byId("stockSearchVoucher").value = "";
            byId("stockSearchItem").value = "";
            emptyRow("اضغط بحث لعرض سندات التحويل.");
        });
        searchBody.addEventListener("click", function (event) {
            var button = event.target.closest("[data-stock-view]");
            if (!button) { return; }
            message("العرض التفصيلي لسندات التحويل السابقة سيستخدم نفس شاشة الإدخال بعد ربط تحميل التفاصيل.", true);
        });
    }

    rows.push(createRow());
    bindIndexWorkflow();
    bindGrid();
    bindBranchStores();
    bindImport();
    bindSave();
    renderRows();
}());
