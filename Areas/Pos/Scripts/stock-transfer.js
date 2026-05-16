(function () {
    "use strict";

    var page = document.querySelector(".stock-page");
    if (!page) { return; }

    var rows = [];
    var rowSeed = 1;
    var lookupTimers = {};
    var busy = false;
    var lastImportResults = [];
    var gridPage = 1;
    var gridPageSize = 100;
    var initialHeader = {};
    var activeRowId = null;
    var serialPicker = {
        rowId: null,
        item: null,
        page: 1,
        pageSize: 50,
        totalRows: 0,
        rows: [],
        selected: {}
    };

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
        byId("openSelectedSerialPickerBtn").disabled = value;
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
        var totalPages = Math.max(1, Math.ceil(rows.length / gridPageSize));
        if (gridPage > totalPages) { gridPage = totalPages; }
        if (gridPage < 1) { gridPage = 1; }
        var start = (gridPage - 1) * gridPageSize;
        var pageRows = rows.slice(start, start + gridPageSize);
        body.innerHTML = pageRows.map(function (row) {
            return "<tr data-row-id=\"" + row.id + "\" class=\"" + (row.haveSerial ? "is-serial" : "") + "\">" +
                "<td class=\"item-cell\"><div class=\"lookup-wrap\">" +
                "<input type=\"hidden\" class=\"row-item-id\" value=\"" + escapeHtml(row.itemId || "") + "\" />" +
                "<input type=\"text\" class=\"form-control row-item-search\" value=\"" + escapeHtml(row.searchText) + "\" placeholder=\"كود أو اسم الصنف\" autocomplete=\"off\" />" +
                "<div class=\"lookup-results row-item-results\" role=\"listbox\"></div></div></td>" +
                "<td class=\"unit-cell\"><input type=\"text\" class=\"form-control\" value=\"" + escapeHtml(row.unitName) + "\" readonly /></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.001\" class=\"form-control row-number\" data-field=\"quantity\" value=\"" + escapeHtml(row.quantity) + "\" " + (row.haveSerial ? "readonly" : "") + " /></td>" +
                "<td class=\"serial-cell\"><div class=\"serial-input-row\"><input type=\"text\" class=\"form-control row-serial\" value=\"" + escapeHtml(row.serial) + "\" placeholder=\"رقم السيريال\" " + (!row.haveSerial ? "disabled" : "") + " />" +
                "<button type=\"button\" class=\"btn btn-default btn-sm serial-picker-open\" " + (!row.haveSerial || !row.itemId ? "disabled" : "") + ">اختيار</button></div>" +
                "<div class=\"serial-badge\">صنف مسلسل: الكمية 1 والسيريال مطلوب</div></td>" +
                "<td class=\"number-cell\"><input type=\"number\" min=\"0\" step=\"0.01\" class=\"form-control row-number\" data-field=\"price\" value=\"" + escapeHtml(row.price) + "\" /></td>" +
                "<td class=\"remove-cell\"><button type=\"button\" class=\"btn btn-danger btn-sm row-remove\">حذف</button></td>" +
                "</tr>";
        }).join("");
        renderTotals();
        renderGridPager();
    }

    function renderGridPager() {
        var pager = byId("transferItemsPager");
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

    function captureInitialHeader() {
        initialHeader = {
            transferDate: byId("transferDate").value || "",
            branchId: byId("transferBranchId").value || "",
            sourceStoreId: byId("transferSourceStoreId").value || "",
            destinationStoreId: byId("transferDestinationStoreId").value || ""
        };
    }

    function resetTransferForm() {
        byId("transferVoucherNumber").value = "";
        byId("transferDate").value = initialHeader.transferDate || "";
        byId("transferBranchId").value = initialHeader.branchId || "";
        byId("transferSourceStoreId").value = initialHeader.sourceStoreId || "";
        byId("transferDestinationStoreId").value = initialHeader.destinationStoreId || "";
        byId("transferRemarks").value = "";
        byId("serialText").value = "";
        byId("serialFileName").textContent = "لم يتم اختيار ملف";
        rows = [createRow()];
        gridPage = 1;
        lastImportResults = [];
        showImportResults();
        renderRows();
        message("", false);
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
        activeRowId = id;
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].id === id) { return rows[i]; }
        }
        return null;
    }

    function getActiveRow() {
        if (activeRowId) {
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].id === activeRowId) { return rows[i]; }
            }
        }

        for (var j = 0; j < rows.length; j++) {
            if (rows[j].itemId && rows[j].haveSerial) { return rows[j]; }
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
        if (row.haveSerial) {
            openSerialPicker(row);
        }
    }

    function selectedSerialCount() {
        var count = 0;
        Object.keys(serialPicker.selected).forEach(function (key) {
            if (serialPicker.selected[key]) { count++; }
        });
        return count;
    }

    function setSerialPickerStatus(value) {
        var target = byId("serialPickerStatus");
        if (target) { target.textContent = value || ""; }
    }

    function buildSerialRequest(page, pageSize) {
        return {
            BranchId: parseInt(byId("transferBranchId").value || "0", 10),
            SourceStoreId: parseInt(byId("transferSourceStoreId").value || "0", 10),
            ItemId: serialPicker.item ? serialPicker.item.itemId : 0,
            TransferDate: byId("transferDate").value,
            SerialFrom: byId("serialPickerFrom").value,
            SerialTo: byId("serialPickerTo").value,
            SerialTerm: byId("serialPickerTerm").value,
            Page: page || serialPicker.page,
            PageSize: pageSize || serialPicker.pageSize
        };
    }

    function fetchSerialPickerPage(page, pageSize) {
        return fetch(getUrl("data-available-serials-url"), {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(buildSerialRequest(page, pageSize))
        }).then(function (response) {
            return response.json().then(function (data) { return { ok: response.ok, data: data }; });
        }).then(function (result) {
            if (!result.ok || !result.data.success) {
                throw new Error((result.data && (result.data.technicalMessage || result.data.message)) || "تعذر تحميل السيريالات");
            }
            return result.data;
        });
    }

    function renderSerialPicker() {
        var body = byId("serialPickerBody");
        var totalPages = Math.max(1, Math.ceil(serialPicker.totalRows / serialPicker.pageSize));
        byId("serialPickerPageInfo").textContent = "صفحة " + serialPicker.page + " / " + totalPages + " - إجمالي " + serialPicker.totalRows;
        byId("serialPickerPrevBtn").disabled = serialPicker.page <= 1;
        byId("serialPickerNextBtn").disabled = serialPicker.page >= totalPages;
        byId("serialPickerCheckAll").checked = serialPicker.rows.length > 0 && serialPicker.rows.every(function (row) {
            return !!serialPicker.selected[text(row.Serial).toLowerCase()];
        });

        if (!serialPicker.rows.length) {
            body.innerHTML = "<tr><td colspan=\"4\" class=\"empty-index-row\">لا توجد سيريالات متاحة مطابقة.</td></tr>";
            setSerialPickerStatus("المحدد: " + selectedSerialCount());
            return;
        }

        body.innerHTML = serialPicker.rows.map(function (row) {
            var key = text(row.Serial).toLowerCase();
            return "<tr>" +
                "<td><input type=\"checkbox\" class=\"serial-picker-check\" data-serial=\"" + escapeHtml(row.Serial) + "\" " + (serialPicker.selected[key] ? "checked" : "") + " /></td>" +
                "<td>" + escapeHtml(row.Serial) + "</td>" +
                "<td>" + escapeHtml(row.ItemName || "") + "</td>" +
                "<td>" + escapeHtml(toNumber(row.AvailableQty).toFixed(3)) + "</td>" +
                "</tr>";
        }).join("");
        setSerialPickerStatus("المحدد: " + selectedSerialCount());
    }

    function loadSerialPicker(page) {
        serialPicker.page = page || 1;
        serialPicker.pageSize = parseInt(byId("serialPickerPageSize").value || "50", 10) || 50;
        setSerialPickerStatus("جاري تحميل السيريالات...");
        fetchSerialPickerPage(serialPicker.page, serialPicker.pageSize)
            .then(function (data) {
                serialPicker.rows = data.rows || [];
                serialPicker.totalRows = data.totalRows || 0;
                renderSerialPicker();
            })
            .catch(function (error) {
                serialPicker.rows = [];
                serialPicker.totalRows = 0;
                renderSerialPicker();
                setSerialPickerStatus(error.message);
            });
    }

    function openSerialPicker(row) {
        if (!row || !row.itemId || !row.haveSerial) { return; }
        serialPicker.rowId = row.id;
        serialPicker.item = row;
        serialPicker.page = 1;
        serialPicker.totalRows = 0;
        serialPicker.rows = [];
        serialPicker.selected = {};
        if (row.serial) {
            serialPicker.selected[text(row.serial).toLowerCase()] = {
                ItemId: row.itemId,
                ItemCode: row.itemCode,
                ItemName: row.itemName,
                UnitId: row.unitId,
                UnitName: row.unitName,
                Quantity: 1,
                UnitFactor: row.unitFactor || 1,
                Price: row.price || 0,
                HaveSerial: true,
                Serial: row.serial
            };
        }
        byId("serialPickerItemName").textContent = row.searchText || row.itemName || "";
        byId("serialPickerFrom").value = "";
        byId("serialPickerTo").value = "";
        byId("serialPickerTerm").value = "";
        byId("serialPickerOverlay").classList.add("is-open");
        byId("serialPickerOverlay").setAttribute("aria-hidden", "false");
        loadSerialPicker(1);
    }

    function closeSerialPicker() {
        byId("serialPickerOverlay").classList.remove("is-open");
        byId("serialPickerOverlay").setAttribute("aria-hidden", "true");
    }

    function toggleSerialPickerRows(checked) {
        serialPicker.rows.forEach(function (row) {
            var key = text(row.Serial).toLowerCase();
            serialPicker.selected[key] = checked ? row : null;
        });
        renderSerialPicker();
    }

    function addSelectedSerialRows() {
        var selected = Object.keys(serialPicker.selected).map(function (key) { return serialPicker.selected[key]; }).filter(Boolean);
        if (!selected.length) {
            setSerialPickerStatus("اختر سيريال واحد على الأقل.");
            return;
        }

        var baseRow = rows.filter(function (row) { return row.id === serialPicker.rowId; })[0];
        var firstApplied = false;
        selected.forEach(function (row) {
            var serialKey = text(row.Serial).toLowerCase();
            if (serialKey && rows.some(function (candidate) {
                return candidate.id !== serialPicker.rowId && text(candidate.serial).toLowerCase() === serialKey;
            })) {
                return;
            }

            var target = !firstApplied && baseRow && baseRow.itemId === serialPicker.item.itemId ? baseRow : null;
            if (!target) {
                target = createRow();
                rows.push(target);
            }

            target.itemId = row.ItemId || row.itemId || serialPicker.item.itemId;
            target.itemCode = row.ItemCode || row.itemCode || serialPicker.item.itemCode;
            target.itemName = row.ItemName || row.itemName || serialPicker.item.itemName;
            target.searchText = buildItemText(target.itemCode, target.itemName);
            target.unitId = row.UnitId || row.unitId || serialPicker.item.unitId;
            target.unitName = row.UnitName || row.unitName || serialPicker.item.unitName;
            target.quantity = 1;
            target.unitFactor = toNumber(row.UnitFactor || row.unitFactor || serialPicker.item.unitFactor || 1) || 1;
            target.price = toNumber(row.Price || row.price || serialPicker.item.price || 0);
            target.haveSerial = true;
            target.serial = row.Serial || row.serial || "";
            firstApplied = true;
        });

        rows = rows.filter(function (row) { return row.itemId || row.searchText || row.serial; });
        if (!rows.length) { rows.push(createRow()); }
        gridPage = Math.max(1, Math.ceil(rows.length / gridPageSize));
        renderRows();
        closeSerialPicker();
    }

    function selectAllFilteredSerials() {
        serialPicker.selected = {};
        serialPicker.pageSize = parseInt(byId("serialPickerPageSize").value || "50", 10) || 50;
        var fetchSize = 500;
        var page = 1;
        var total = null;

        function next() {
            setSerialPickerStatus("جاري اختيار كل النتائج... " + selectedSerialCount());
            return fetchSerialPickerPage(page, fetchSize).then(function (data) {
                var fetchedRows = data.rows || [];
                total = data.totalRows || fetchedRows.length;
                fetchedRows.forEach(function (row) {
                    serialPicker.selected[text(row.Serial).toLowerCase()] = row;
                });
                if (selectedSerialCount() < total && fetchedRows.length) {
                    page++;
                    return next();
                }
                renderSerialPicker();
            });
        }

        next().catch(function (error) { setSerialPickerStatus(error.message); });
    }

    function bindGrid() {
        byId("addTransferRowBtn").addEventListener("click", function () {
            rows.push(createRow());
            gridPage = Math.ceil(rows.length / gridPageSize);
            renderRows();
        });

        byId("openSelectedSerialPickerBtn").addEventListener("click", function () {
            var row = getActiveRow();
            if (!row || !row.itemId) {
                window.alert("اختر صنف مسلسل أولا، ثم اضغط زر اختيار السيريالات.");
                message("اختر صنف مسلسل أولا، ثم اضغط زر اختيار السيريالات.", true);
                return;
            }
            if (!row.haveSerial) {
                window.alert("الصنف المحدد ليس صنفا مسلسلا.");
                message("الصنف المحدد ليس صنفا مسلسلا.", true);
                return;
            }
            openSerialPicker(row);
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
            var touchedRow = findRowFromElement(event.target);

            if (event.target.classList.contains("serial-picker-open")) {
                openSerialPicker(touchedRow);
                return;
            }

            if (!event.target.classList.contains("row-remove")) { return; }
            var row = touchedRow;
            if (!row) { return; }
            rows = rows.filter(function (candidate) { return candidate.id !== row.id; });
            if (!rows.length) {
                rows.push(createRow());
            }
            renderRows();
        });

        var pager = byId("transferItemsPager");
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

        byId("clearTransferBtn").addEventListener("click", function () {
            rows = [createRow()];
            gridPage = 1;
            lastImportResults = [];
            showImportResults();
            renderRows();
            message("", false);
        });
    }

    function bindSerialPicker() {
        var overlay = byId("serialPickerOverlay");
        if (!overlay) { return; }

        byId("serialPickerCloseBtn").addEventListener("click", closeSerialPicker);
        byId("serialPickerSearchBtn").addEventListener("click", function () { serialPicker.selected = {}; loadSerialPicker(1); });
        byId("serialPickerPrevBtn").addEventListener("click", function () { loadSerialPicker(Math.max(1, serialPicker.page - 1)); });
        byId("serialPickerNextBtn").addEventListener("click", function () {
            var totalPages = Math.max(1, Math.ceil(serialPicker.totalRows / serialPicker.pageSize));
            loadSerialPicker(Math.min(totalPages, serialPicker.page + 1));
        });
        byId("serialPickerInsertBtn").addEventListener("click", addSelectedSerialRows);
        byId("serialPickerSelectPageBtn").addEventListener("click", function () { toggleSerialPickerRows(true); });
        byId("serialPickerSelectAllBtn").addEventListener("click", selectAllFilteredSerials);
        byId("serialPickerPageSize").addEventListener("change", function () { loadSerialPicker(1); });
        byId("serialPickerCheckAll").addEventListener("change", function () { toggleSerialPickerRows(this.checked); });

        byId("serialPickerBody").addEventListener("change", function (event) {
            if (!event.target.classList.contains("serial-picker-check")) { return; }
            var serial = event.target.getAttribute("data-serial") || "";
            var key = serial.toLowerCase();
            var row = serialPicker.rows.filter(function (candidate) { return text(candidate.Serial).toLowerCase() === key; })[0];
            serialPicker.selected[key] = event.target.checked ? row : null;
            renderSerialPicker();
        });

        overlay.addEventListener("click", function (event) {
            if (event.target === overlay) { closeSerialPicker(); }
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
        gridPage = 1;
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
                message("اختر ملف Excel أولا", true);
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
                        throw new Error((result.data && (result.data.technicalMessage || result.data.message)) || "تعذر الحفظ");
                    }
                    byId("transferVoucherNumber").value = result.data.result.VoucherNumber || byId("transferVoucherNumber").value;
                    message("تم الحفظ. سند: " + result.data.result.VoucherNumber + " | قيد: " + (result.data.result.NoteSerial || result.data.result.NoteId || "") + " | حركة الصرف: " + result.data.result.SourceTransactionId + " | حركة الاستلام: " + result.data.result.DestinationTransactionId, false);
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

        var searchRunning = false;
        function setSearchBusy(value) {
            var button = byId("stockSearchBtn");
            if (!button) { return; }
            button.disabled = !!value;
            button.innerText = value ? "جاري البحث..." : "بحث";
        }

        function validateSearch() {
            var voucher = (byId("stockSearchVoucher").value || "").trim();
            var itemTerm = (byId("stockSearchItem").value || "").trim();
            var fromDate = byId("stockSearchFrom").value || "";
            var toDate = byId("stockSearchTo").value || "";
            if (voucher && voucher.length < 3 && !/^[0-9]{3,}$/.test(voucher)) {
                return "اكتب 3 أحرف على الأقل في رقم السند.";
            }
            if (itemTerm && itemTerm.length < 3 && !/^[0-9]{3,}$/.test(itemTerm)) {
                return "اكتب 3 أحرف على الأقل في بحث الصنف أو السيريال.";
            }
            if (!fromDate || !toDate) {
                return (voucher.length >= 3 || itemTerm.length >= 3) ? "" : "حدد فترة البحث أو اكتب بحثاً محدداً من 3 أحرف على الأقل.";
            }
            return "";
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
            var validationMessage = validateSearch();
            if (validationMessage) {
                emptyRow(validationMessage);
                return;
            }
            if (searchRunning) { return; }
            searchRunning = true;
            setSearchBusy(true);
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
                .catch(function (error) { emptyRow(error.message); })
                .then(function () {
                    searchRunning = false;
                    setSearchBusy(false);
                });
        }

        function loadTransfer(sourceTransactionId) {
            message("جاري تحميل سند التحويل...", false);
            fetch(getUrl("data-get-url") + "?sourceTransactionId=" + encodeURIComponent(sourceTransactionId), { credentials: "same-origin" })
                .then(function (response) { return response.json().then(function (data) { return { ok: response.ok, data: data }; }); })
                .then(function (result) {
                    if (!result.ok || !result.data.success || !result.data.transfer) {
                        throw new Error((result.data && result.data.message) || "تعذر تحميل سند التحويل");
                    }

                    var transfer = result.data.transfer;
                    byId("transferVoucherNumber").value = transfer.VoucherNumber || "";
                    byId("transferDate").value = transfer.TransferDate || "";
                    byId("transferBranchId").value = transfer.BranchId || "";
                    byId("transferSourceStoreId").value = transfer.SourceStoreId || "";
                    byId("transferDestinationStoreId").value = transfer.DestinationStoreId || "";
                    byId("transferRemarks").value = transfer.Remarks || "";
                    rows = (transfer.Items || []).map(function (item) { return createRow(item); });
                    if (!rows.length) { rows.push(createRow()); }
                    gridPage = 1;
                    renderRows();
                    showEntry();
                    message("تم تحميل سند التحويل للعرض.", false);
                })
                .catch(function (error) { message(error.message, true); });
        }

        byId("stockAddNewBtn").addEventListener("click", function () {
            resetTransferForm();
            showEntry();
        });
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
            loadTransfer(button.getAttribute("data-stock-view"));
        });

        var params = new URLSearchParams(window.location.search || "");
        var sourceTransactionId = params.get("sourceTransactionId") || params.get("openStockTransfer");
        if (sourceTransactionId) {
            loadTransfer(sourceTransactionId);
        }
    }

    captureInitialHeader();
    rows.push(createRow());
    bindIndexWorkflow();
    bindGrid();
    bindSerialPicker();
    bindBranchStores();
    bindImport();
    bindSave();
    renderRows();
}());
