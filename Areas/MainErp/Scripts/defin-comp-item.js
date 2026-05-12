(function () {
    "use strict";

    var root = document.getElementById("definCompPage");
    if (!root) {
        return;
    }

    var config = root.dataset;
    var lookupItemsUrl = config.lookupItemsUrl;
    var lookupUnitsUrl = config.lookupUnitsUrl;
    var saveUrl = config.saveUrl;
    var rebuildUrl = config.rebuildUrl;
    var deleteUrl = config.deleteUrl;
    var detailsUrl = config.detailsUrl;

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var statusBox = document.getElementById("definCompStatus");
    var outputsTbody = document.getElementById("outputsTbody");
    var componentsTbody = document.getElementById("componentsTbody");
    var outputTemplate = document.getElementById("outputLineTemplate");
    var componentTemplate = document.getElementById("componentLineTemplate");
    var addOutputBtn = document.getElementById("addOutputRowBtn");
    var addComponentBtn = document.getElementById("addComponentRowBtn");
    var saveBtn = document.getElementById("definCompSaveBtn");
    var rebuildBtn = document.getElementById("definCompRebuildBtn");
    var deleteBtn = document.getElementById("definCompDeleteBtn");
    var newBtn = document.getElementById("definCompNewBtn");
    var selectedOutputTitle = document.getElementById("selectedOutputTitle");
    var linkedIssueId = document.getElementById("linkedIssueId");
    var linkedReceiptId = document.getElementById("linkedReceiptId");
    var componentCountText = document.getElementById("componentCount");
    var linkedIssueTotal = document.getElementById("linkedIssueTotal");
    var linkedReceiptTotal = document.getElementById("linkedReceiptTotal");

    var cache = {
        items: {},
        units: {}
    };

    var state = {
        selectedOutputLineId: null,
        isBusy: false,
        isInit: false
    };

    function toNumber(v) {
        var n = parseFloat(v);
        return isNaN(n) ? 0 : n;
    }

    function toInt(v) {
        var n = parseInt(v, 10);
        return isNaN(n) ? null : n;
    }

    function formatMoney(v) {
        var n = toNumber(v);
        return n.toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function setText(id, value) {
        var el = document.getElementById(id);
        if (!el) {
            return;
        }
        el.textContent = value == null ? "" : value;
    }

    function setMoney(id, value) {
        setText(id, formatMoney(value));
    }

    function showStatus(message, kind) {
        if (!statusBox) {
            return;
        }
        statusBox.hidden = false;
        statusBox.className = "defin-comp-status " + (kind || "info");
        statusBox.textContent = message || "";
    }

    function clearStatus() {
        if (!statusBox) {
            return;
        }
        statusBox.hidden = true;
        statusBox.className = "defin-comp-status";
        statusBox.textContent = "";
    }

    function setBusy(busy, message) {
        state.isBusy = busy;

        [saveBtn, rebuildBtn, deleteBtn, newBtn, addOutputBtn, addComponentBtn].forEach(function (btn) {
            if (!btn) {
                return;
            }
            var blockedByServer = btn.getAttribute("data-disabled-default") === "1";
            if (busy) {
                btn.disabled = true;
            } else {
                btn.disabled = blockedByServer;
            }
        });

        root.classList.toggle("defin-comp-saving", busy);
        if (busy) {
            showStatus(message || "جاري تنفيذ العملية...", "info");
        } else {
            if (!statusBox.className.match(/success|error|warning/)) {
                clearStatus();
            }
        }
    }

    function outputRows() {
        return Array.prototype.slice.call(outputsTbody.querySelectorAll("tr.defin-comp-output-row"));
    }

    function componentRows() {
        return Array.prototype.slice.call(componentsTbody.querySelectorAll("tr.defin-comp-component-row"));
    }

    function getLineId(row) {
        if (!row || !row.dataset) {
            return 0;
        }
        var id = parseInt(row.dataset.lineId, 10);
        return isNaN(id) ? 0 : id;
    }

    function setLineId(row, value) {
        if (!row || !row.dataset) {
            return;
        }
        var lineId = toInt(value) || 0;
        row.dataset.lineId = String(lineId);
        if (row.dataset.kind === "output") {
            var badge = row.querySelector(".defin-comp-output-line-number");
            if (badge) {
                badge.textContent = lineId ? String(lineId) : "—";
            }
        }
    }

    function lineLabel(row) {
        var input = rowItemInput(row);
        return input && (input.value || "").trim();
    }

    function outputRowForLine(lineId) {
        return outputRows().find(function (r) {
            return getLineId(r) === lineId;
        }) || null;
    }

    function nextLineId() {
        var used = {};
        outputRows().forEach(function (row) {
            var id = getLineId(row);
            if (id > 0) {
                used[id] = true;
            }
        });

        var i = 1;
        while (used[i]) {
            i += 1;
        }
        return i;
    }

    function rowItemInput(row) {
        return row ? row.querySelector(".item-text") : null;
    }

    function rowUnitSelect(row) {
        return row ? row.querySelector(".unit-id") : null;
    }

    function rowQty(row) {
        return row ? row.querySelector(".qty") : null;
    }

    function rowCost(row) {
        return row ? row.querySelector(".cost, .line-cost") : null;
    }

    function rowTotal(row) {
        return row ? row.querySelector(".line-total") : null;
    }

    function rowRemark(row) {
        return row ? row.querySelector(".remark") : null;
    }

    function ensureOutputPlaceholder() {
        var empty = outputsTbody.querySelector("tr.defin-comp-empty-row");
        if (outputRows().length > 0) {
            if (empty) {
                empty.remove();
            }
            return;
        }
        if (empty) {
            return;
        }
        var tr = document.createElement("tr");
        tr.className = "defin-comp-empty-row";
        tr.innerHTML = "<td colspan='7'>لا توجد أصناف نهائية. أضف صنفاً نهائياً أولاً.</td>";
        outputsTbody.appendChild(tr);
    }

    function ensureComponentPlaceholder() {
        var existing = componentsTbody.querySelector("tr.defin-comp-empty-row");
        if (!state.selectedOutputLineId) {
            if (existing) {
                existing.remove();
            }
            var noSelection = document.createElement("tr");
            noSelection.className = "defin-comp-empty-row";
            noSelection.innerHTML = "<td colspan='7'>اختر صنفاً نهائياً لعرض المكونات.</td>";
            componentsTbody.appendChild(noSelection);
            return;
        }

        var visible = componentRows().some(function (r) {
            return !r.classList.contains("is-hidden-row");
        });

        if (existing) {
            if (visible) {
                existing.remove();
            } else {
                return;
            }
        }

        if (!visible) {
            var addHint = document.createElement("tr");
            addHint.className = "defin-comp-empty-row";
            addHint.innerHTML = "<td colspan='7'>لا توجد مكونات لهذا المنتج. أضف مكوناً.</td>";
            componentsTbody.appendChild(addHint);
        }
    }

    function applySelection(lineId) {
        state.selectedOutputLineId = lineId || null;
        outputRows().forEach(function (row) {
            row.classList.toggle("is-selected", getLineId(row) === state.selectedOutputLineId);
        });

        componentRows().forEach(function (row) {
            var isCurrent = getLineId(row) === state.selectedOutputLineId;
            row.classList.toggle("is-hidden-row", !isCurrent);
        });

        if (selectedOutputTitle) {
            var title = "اختر الصنف النهائي لعرض المكونات";
            var output = outputRowForLine(state.selectedOutputLineId);
            if (output) {
                var outputName = lineLabel(output);
                title = "مكونات: " + (outputName || "الصنف #" + state.selectedOutputLineId);
            }
            selectedOutputTitle.textContent = title;
        }

        ensureComponentPlaceholder();
        recalcTotals();
    }

    function setRowTotal(row, total) {
        if (!row || !rowTotal(row)) {
            return;
        }
        rowTotal(row).value = toNumber(total).toFixed(2);
    }

    function recalcLine(row) {
        if (!row || row.classList.contains("defin-comp-empty-row")) {
            return { qty: 0, cost: 0, total: 0 };
        }

        var qty = toNumber(rowQty(row) ? rowQty(row).value : 0);
        var cost = toNumber(rowCost(row) ? rowCost(row).value : 0);
        var total = qty * cost;
        setRowTotal(row, total);
        return {
            qty: qty,
            cost: cost,
            total: total
        };
    }

    function recalcTotals() {
        var componentCostTotal = 0;
        var outputQtyTotal = 0;
        var outputCostTotal = 0;
        var visibleComponents = 0;
        var outputCount = 0;
        outputRows().forEach(function (outRow) {
            if (outRow.classList.contains("defin-comp-empty-row")) {
                return;
            }

            var lineId = getLineId(outRow);
            outputCount += 1;

            var outputQty = toNumber(rowQty(outRow).value);
            var outputLineComponentsTotal = 0;

            componentRows().forEach(function (cmpRow) {
                if (getLineId(cmpRow) !== lineId) {
                    return;
                }
                var calc = recalcLine(cmpRow);
                outputLineComponentsTotal += calc.total;
                componentCostTotal += calc.total;
                if (!cmpRow.classList.contains("is-hidden-row")) {
                    visibleComponents += 1;
                }
            });

            var avgCost = outputQty > 0 ? outputLineComponentsTotal / outputQty : 0;
            var costInput = rowCost(outRow);
            if (costInput) {
                costInput.value = avgCost.toFixed(2);
            }
            setRowTotal(outRow, outputLineComponentsTotal);

            outputQtyTotal += outputQty;
            outputCostTotal += outputLineComponentsTotal;
        });

        if (!outputRows().length) {
            setText("itemsCountTotal", "0");
        } else {
            setText("itemsCountTotal", outputCount.toString());
        }

        setMoney("componentCostTotal", componentCostTotal);
        setMoney("outputCostTotal", outputCostTotal);
        setMoney("differenceTotal", outputCostTotal - componentCostTotal);
        setText("componentCount", visibleComponents.toString());

        if (linkedIssueTotal) {
            setMoney("linkedIssueTotal", componentCostTotal);
            linkedIssueTotal.textContent = formatMoney(componentCostTotal);
        }

        if (linkedReceiptTotal) {
            setMoney("linkedReceiptTotal", outputCostTotal);
            linkedReceiptTotal.textContent = formatMoney(outputCostTotal);
        }

        return {
            componentCostTotal: componentCostTotal,
            outputCostTotal: outputCostTotal,
            diff: outputCostTotal - componentCostTotal,
            outputQtyTotal: outputQtyTotal,
            componentQtyTotal: toNumber(document.getElementById("componentQtyTotal").value)
        };
    }

    function fillUnits(select, items) {
        var prevValue = select.value;
        select.innerHTML = "<option value=\"\">اختر الوحدة</option>";
        items.forEach(function (item) {
            var option = document.createElement("option");
            option.value = item.Id;
            option.textContent = item.Text;
            select.appendChild(option);
        });
        if (prevValue) {
            select.value = prevValue;
        }
    }

    function populateUnits(row, itemId) {
        if (!row || !itemId) {
            return Promise.resolve();
        }

        var select = rowUnitSelect(row);
        if (!select) {
            return Promise.resolve();
        }

        var cacheKey = String(itemId);
        if (cache.units[cacheKey]) {
            fillUnits(select, cache.units[cacheKey]);
            return Promise.resolve();
        }

        return fetch(lookupUnitsUrl + "?itemId=" + encodeURIComponent(itemId), {
            headers: {
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            }
        }).then(function (response) {
            return response.json();
        }).then(function (payload) {
            if (!payload || !payload.success) {
                fillUnits(select, []);
                return;
            }
            cache.units[cacheKey] = payload.items || [];
            fillUnits(select, cache.units[cacheKey]);
        }).catch(function () {
            fillUnits(select, []);
        });
    }

    function renderDatalist(datalist, items) {
        datalist.innerHTML = "";
        for (var i = 0; i < items.length; i += 1) {
            var item = items[i];
            var option = document.createElement("option");
            option.value = item.Text;
            option.setAttribute("data-id", item.Id);
            datalist.appendChild(option);
        }
    }

    function createAutocomplete(input) {
        if (!input || input.dataset.autocomplete === "1") {
            return;
        }
        input.dataset.autocomplete = "1";

        var datalistId = "datalist-" + Math.random().toString(36).slice(2);
        var datalist = document.createElement("datalist");
        datalist.id = datalistId;
        input.setAttribute("list", datalistId);
        document.body.appendChild(datalist);

        var timer;
        input.addEventListener("input", function () {
            var term = (input.value || "").trim();
            if (term.length < 2) {
                renderDatalist(datalist, []);
                return;
            }

            if (cache.items[term]) {
                renderDatalist(datalist, cache.items[term]);
                return;
            }

            clearTimeout(timer);
            timer = setTimeout(function () {
                fetch(lookupItemsUrl + "?term=" + encodeURIComponent(term), {
                    headers: {
                        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                    }
                }).then(function (response) {
                    return response.json();
                }).then(function (payload) {
                    if (!payload || !payload.success) {
                        renderDatalist(datalist, []);
                        return;
                    }
                    cache.items[term] = payload.items || [];
                    renderDatalist(datalist, cache.items[term]);
                })
                    .catch(function () {
                        renderDatalist(datalist, []);
                    });
            }, 220);
        });

        input.addEventListener("change", function () {
            var selectedId = "";
            var text = input.value || "";
            var found = datalist.querySelector('option[value="' + CSS.escape(text) + '"]');
            if (found) {
                selectedId = found.getAttribute("data-id") || "";
            }
            input.dataset.itemId = selectedId;
            var row = input.closest("tr");
            if (selectedId) {
                populateUnits(row, toInt(selectedId))
                    .then(function () {
                        recalcTotals();
                    });
            } else {
                recalcTotals();
            }
        });
    }

    function collectPayload() {
        recalcTotals();

        var outputsByLine = [];
        var outputGroups = [];
        var components = [];
        outputRows().forEach(function (row) {
            if (row.classList.contains("defin-comp-empty-row")) {
                return;
            }
            var lineId = getLineId(row) || nextLineId();
            setLineId(row, lineId);
            setLineId(row, lineId);

            var itemInput = rowItemInput(row);
            var outputObj = {
                ItemId: toInt(itemInput && itemInput.dataset.itemId),
                ItemId2: toInt(itemInput && itemInput.dataset.itemId),
                UnitId: toInt(rowUnitSelect(row) && rowUnitSelect(row).value),
                Qty: toNumber(rowQty(row) && rowQty(row).value),
                Cost: toNumber(rowCost(row) && rowCost(row).value),
                Price: toNumber(rowCost(row) && rowCost(row).value),
                Total: toNumber(rowTotal(row) && rowTotal(row).value),
                LineId: lineId,
                Remark: (rowRemark(row) && rowRemark(row).value) || ""
            };
            outputsByLine.push(outputObj);
            outputGroups.push({
                ItemId: outputObj.ItemId,
                ItemId2: outputObj.ItemId,
                UnitId: outputObj.UnitId,
                Qty: outputObj.Qty,
                Cost: outputObj.Cost,
                Price: outputObj.Price,
                Total: outputObj.Total,
                LineId: lineId,
                Remark: outputObj.Remark,
                Components: []
            });
        });

        componentRows().forEach(function (row) {
            if (row.classList.contains("defin-comp-empty-row")) {
                return;
            }
            if (!rowItemInput(row)) {
                return;
            }
            var lineId = getLineId(row);
            if (!lineId && outputsByLine.length > 0) {
                lineId = outputsByLine[0].LineId;
                setLineId(row, lineId);
            }
            if (!lineId) {
                return;
            }

            var itemInput = rowItemInput(row);
            var outputLine = outputRows().find(function (o) {
                return getLineId(o) === lineId;
            });
            var outputItemId = outputLine && outputLine.querySelector(".item-text")
                ? toInt(outputLine.querySelector(".item-text").dataset.itemId)
                : null;

            var componentObj = {
                ItemId: toInt(itemInput && itemInput.dataset.itemId),
                ItemId2: outputItemId,
                UnitId: toInt(rowUnitSelect(row) && rowUnitSelect(row).value),
                Qty: toNumber(rowQty(row) && rowQty(row).value),
                Cost: toNumber(rowCost(row) && rowCost(row).value),
                Price: toNumber(rowCost(row) && rowCost(row).value),
                Total: toNumber(rowTotal(row) && rowTotal(row).value),
                LineId: lineId,
                Remark: (rowRemark(row) && rowRemark(row).value) || ""
            };
            components.push(componentObj);

            var group = outputGroups.find(function (g) {
                return g.LineId === lineId;
            });
            if (group) {
                group.Components.push(componentObj);
            }
        });

        var totals = recalcTotals();
        return {
            Id: toInt(document.getElementById("definCompId").value),
            Mode: document.getElementById("definCompMode").value,
            RecordDate: document.getElementById("recordDate").value,
            BranchId: toInt(document.getElementById("branchId").value),
            StoreId: toInt(document.getElementById("storeId").value),
            StoreId2: toInt(document.getElementById("storeId2").value),
            StoreId3: toInt(document.getElementById("storeId3").value),
            CusId: toInt(document.getElementById("cusId").value),
            MaxNo: (document.getElementById("maxNo").value || "").trim(),
            MaxName: (document.getElementById("maxName").value || "").trim(),
            OrderNo: (document.getElementById("orderNo").value || "").trim(),
            GroupId: toInt(document.getElementById("groupId").value),
            PaymentType: toInt(document.getElementById("paymentType").value),
            EmpId: toInt(document.getElementById("empId").value),
            BoxId: toInt(document.getElementById("boxId").value),
            Period: toNumber(document.getElementById("period").value),
            Qty1: totals.componentCostTotal,
            Price: totals.outputCostTotal,
            TotalAdd: totals.outputCostTotal,
            TotalDisc: 0,
            Net: totals.outputCostTotal,
            TotalWithVat: totals.outputCostTotal,
            Vat2: totals.diff,
            TransactionComment: "سند تجميع",
            Outputs: outputsByLine,
            Components: components,
            OutputGroups: outputGroups,
            ForceRebuild: false
        };
    }

    function validate(payload) {
        if (!payload.RecordDate) {
            return "يرجى إدخال تاريخ السند.";
        }
        if (!payload.BranchId || payload.BranchId <= 0) {
            return "يرجى اختيار الفرع.";
        }
        if (!payload.StoreId || payload.StoreId <= 0) {
            return "يرجى اختيار المخزن الأساسي.";
        }
        if (!payload.Outputs || payload.Outputs.length === 0) {
            return "يرجى إضافة صنف نهائي واحد على الأقل.";
        }
        if (!payload.Components || payload.Components.length === 0) {
            return "يرجى إضافة مكونات على الأقل."
        }

        for (var i = 0; i < payload.Outputs.length; i += 1) {
            var output = payload.Outputs[i];
            if (!output.ItemId || output.ItemId <= 0) {
                return "توجد بند منتج نهائي بدون صنف.";
            }
            if (!output.UnitId || output.UnitId <= 0) {
                return "توجد بند منتج نهائي بدون وحدة قياس.";
            }
            if (!(output.Qty > 0)) {
                return "كمية المنتج النهائي يجب أن تكون أكبر من الصفر.";
            }
            if (output.Cost < 0) {
                return "تكلفة المنتج النهائي لا يجوز أن تكون سالبة.";
            }
            var lineComponents = payload.Components.filter(function (c) {
                return c.LineId === output.LineId;
            });
            if (lineComponents.length === 0) {
                return "كل منتج نهائي يجب أن يحتوي على مكونات مرتبطة به.";
            }
        }

        for (i = 0; i < payload.Components.length; i += 1) {
            var comp = payload.Components[i];
            if (!comp.ItemId || comp.ItemId <= 0) {
                return "توجد مكونات بدون صنف.";
            }
            if (!comp.UnitId || comp.UnitId <= 0) {
                return "توجد مكونات بدون وحدة قياس.";
            }
            if (!(comp.Qty > 0)) {
                return "كمية المكون يجب أن تكون أكبر من الصفر.";
            }
            if (comp.Cost < 0) {
                return "تكلفة المكون لا يجوز أن تكون سالبة.";
            }
            if (!comp.LineId || comp.LineId <= 0) {
                return "يوجد مكون غير مرتبط بأي منتج نهائي.";
            }
        }

        return "";
    }

    function post(url, payload) {
        return fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json; charset=utf-8",
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            },
            body: JSON.stringify(payload)
        }).then(function (response) {
            return response.json();
        });
    }

    function onSaved(payload) {
        if (!payload || !payload.Success) {
            showStatus(payload && payload.Message ? payload.Message : "تعذر حفظ السند.", "error");
            return;
        }

        if (payload.IssueTransactionId && linkedIssueId) {
            linkedIssueId.textContent = String(payload.IssueTransactionId);
        }
        if (payload.ReceiptTransactionId && linkedReceiptId) {
            linkedReceiptId.textContent = String(payload.ReceiptTransactionId);
        }

        if (payload.Warnings && payload.Warnings.length) {
            showStatus((payload.Message || "تم حفظ السند بنجاح.") + " " + payload.Warnings.join(" | "), "warning");
        } else {
            showStatus(payload.Message || "تم حفظ السند بنجاح.", "success");
        }
        if (payload.Id) {
            window.location = window.location.pathname + "?id=" + encodeURIComponent(payload.Id);
        } else {
            window.location.reload();
        }
    }

    function save(doRebuild) {
        if (state.isBusy) {
            return;
        }

        var payload = collectPayload();
        var validation = validate(payload);
        if (validation) {
            showStatus(validation, "error");
            return;
        }

        payload.ForceRebuild = !!doRebuild;
        setBusy(true, doRebuild ? "إعادة إنشاء الحركات المرتبطة..." : "حفظ سند التجميع...");
        post(doRebuild ? rebuildUrl : saveUrl, payload)
            .then(function (response) {
                setBusy(false);
                onSaved(response);
            })
            .catch(function () {
                setBusy(false);
                showStatus("حدث خطأ أثناء تنفيذ العملية، حاول مرة أخرى.", "error");
            });
    }

    function deleteCurrent() {
        if (state.isBusy) {
            return;
        }

        var id = toInt(document.getElementById("definCompId").value);
        if (!id) {
            showStatus("لا يوجد سند مفتوح للحذف.", "error");
            return;
        }
        if (!window.confirm("هل تريد حذف سند التجميع الحالي؟")) {
            return;
        }

        setBusy(true, "حذف السند...");
        fetch(deleteUrl + "?id=" + encodeURIComponent(id), {
            method: "POST",
            headers: {
                "RequestVerificationToken": tokenInput ? tokenInput.value : ""
            }
        }).then(function (response) {
            return response.json();
        }).then(function (payload) {
            setBusy(false);
            if (!payload || !payload.Success) {
                showStatus(payload && payload.Message ? payload.Message : "تعذر حذف السند.", "error");
                return;
            }
            showStatus(payload.Message || "تم حذف السند بنجاح.", "success");
            window.setTimeout(function () {
                window.location = window.location.pathname;
            }, 600);
        }).catch(function () {
            setBusy(false);
            showStatus("تعذر حذف السند، تحقق من الاتصال.", "error");
        });
    }

    function bindSearchRows() {
        document.querySelectorAll(".js-load-defin-item").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var id = btn.getAttribute("data-id");
                window.location = window.location.pathname + "?id=" + encodeURIComponent(id);
            });
        });
    }

    function bindOutputRow(row) {
        if (!row || row.classList.contains("defin-comp-empty-row")) {
            return;
        }

        if (!getLineId(row)) {
            setLineId(row, nextLineId());
        }

        var rowItem = rowItemInput(row);
        var rowQtyInput = rowQty(row);
        var rowUnit = rowUnitSelect(row);
        var removeBtn = row.querySelector(".js-remove-output");

        row.addEventListener("click", function () {
            applySelection(getLineId(row));
        });

        if (removeBtn) {
            removeBtn.addEventListener("click", function (ev) {
                ev.stopPropagation();
                if (state.isBusy) {
                    return;
                }
                var lineId = getLineId(row);
                if (!window.confirm("هل تريد حذف هذا الصنف النهائي؟")) {
                    return;
                }
                componentRows().forEach(function (compRow) {
                    if (getLineId(compRow) === lineId) {
                        compRow.remove();
                    }
                });
                row.remove();
                if (outputRows().length) {
                    applySelection(getLineId(outputRows()[0]));
                } else {
                    applySelection(null);
                    ensureOutputPlaceholder();
                }
                recalcTotals();
            });
        }

        if (rowItem) {
            createAutocomplete(rowItem);
            rowItem.addEventListener("change", function () {
                recalcTotals();
            });
        }

        if (rowQtyInput) {
            rowQtyInput.addEventListener("input", function () {
                recalcTotals();
            });
        }

        if (rowUnit) {
            rowUnit.addEventListener("change", function () {
                recalcTotals();
            });
        }
    }

    function bindComponentRow(row) {
        if (!row || row.classList.contains("defin-comp-empty-row")) {
            return;
        }
        if (!row.dataset.lineId && state.selectedOutputLineId) {
            setLineId(row, state.selectedOutputLineId);
        }

        var removeBtn = row.querySelector(".js-remove-line");
        var rowItem = rowItemInput(row);
        var rowQtyInput = rowQty(row);
        var rowCostInput = rowCost(row);
        var rowUnit = rowUnitSelect(row);

        if (removeBtn) {
            removeBtn.addEventListener("click", function () {
                if (state.isBusy) {
                    return;
                }
                row.remove();
                if (state.selectedOutputLineId) {
                    applySelection(state.selectedOutputLineId);
                } else {
                    recalcTotals();
                    ensureComponentPlaceholder();
                }
            });
        }

        if (rowItem) {
            createAutocomplete(rowItem);
            rowItem.addEventListener("change", function () {
                if (state.selectedOutputLineId) {
                    setLineId(row, state.selectedOutputLineId);
                }
                recalcTotals();
            });
        }
        if (rowQtyInput) {
            rowQtyInput.addEventListener("input", recalcTotals);
        }
        if (rowCostInput) {
            rowCostInput.addEventListener("input", recalcTotals);
        }
        if (rowUnit) {
            rowUnit.addEventListener("change", recalcTotals);
        }
    }

    function addOutput() {
        if (!outputTemplate || !outputTemplate.content) {
            return;
        }

        var empty = outputsTbody.querySelector("tr.defin-comp-empty-row");
        if (empty) {
            empty.remove();
        }

        var clone = outputTemplate.content.firstElementChild.cloneNode(true);
        setLineId(clone, nextLineId());
        outputsTbody.appendChild(clone);
        bindOutputRow(clone);
        applySelection(getLineId(clone));
        recalcTotals();
    }

    function addComponent() {
        if (!state.selectedOutputLineId) {
            showStatus("اختر صنفاً نهائياً قبل إضافة المكون.", "error");
            return;
        }
        if (!componentTemplate || !componentTemplate.content) {
            return;
        }

        var empty = componentsTbody.querySelector("tr.defin-comp-empty-row");
        if (empty) {
            empty.remove();
        }

        var clone = componentTemplate.content.firstElementChild.cloneNode(true);
        setLineId(clone, state.selectedOutputLineId);
        componentsTbody.appendChild(clone);
        bindComponentRow(clone);
        applySelection(state.selectedOutputLineId);
        recalcTotals();
    }

    function bindButtons() {
        if (saveBtn) {
            saveBtn.setAttribute("data-disabled-default", saveBtn.disabled ? "1" : "0");
            saveBtn.addEventListener("click", function () {
                save(false);
            });
        }

        if (rebuildBtn) {
            rebuildBtn.setAttribute("data-disabled-default", rebuildBtn.disabled ? "1" : "0");
            rebuildBtn.addEventListener("click", function () {
                save(true);
            });
        }

        if (deleteBtn) {
            deleteBtn.setAttribute("data-disabled-default", deleteBtn.disabled ? "1" : "0");
            deleteBtn.addEventListener("click", deleteCurrent);
        }

        if (newBtn) {
            newBtn.setAttribute("data-disabled-default", newBtn.disabled ? "1" : "0");
            newBtn.addEventListener("click", function () {
                window.location = window.location.pathname;
            });
        }

        if (addOutputBtn) {
            addOutputBtn.addEventListener("click", addOutput);
        }

        if (addComponentBtn) {
            addComponentBtn.addEventListener("click", addComponent);
        }
    }

    function normalizeLoadedRows() {
        outputRows().forEach(function (row, index) {
            if (!getLineId(row)) {
                setLineId(row, index + 1);
            }
            if (row.classList.contains("defin-comp-empty-row")) {
                return;
            }
            bindOutputRow(row);
        });

        componentRows().forEach(function (row) {
            if (row.classList.contains("defin-comp-empty-row")) {
                return;
            }

            if (!getLineId(row) && outputRows().length) {
                setLineId(row, getLineId(outputRows()[0]));
            }
            bindComponentRow(row);
        });
    }

    function initRows() {
        ensureOutputPlaceholder();
        normalizeLoadedRows();

        if (outputRows().length) {
            applySelection(getLineId(outputRows()[0]));
            componentRows().forEach(function (r) {
                r.classList.add("is-hidden-row");
            });
            applySelection(getLineId(outputRows()[0]));
        } else {
            applySelection(null);
        }

        recalcTotals();
        ensureComponentPlaceholder();
    }

    function init() {
        if (state.isInit) {
            return;
        }
        state.isInit = true;

        bindButtons();
        bindSearchRows();
        initRows();
    }

    init();
})(); 
