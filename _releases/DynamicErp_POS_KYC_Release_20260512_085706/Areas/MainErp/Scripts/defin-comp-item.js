(function () {
    var root = document.getElementById("definCompPage");
    if (!root) {
        return;
    }

    var lookupItemsUrl = root.dataset.lookupItemsUrl;
    var lookupUnitsUrl = root.dataset.lookupUnitsUrl;
    var saveUrl = root.dataset.saveUrl;
    var rebuildUrl = root.dataset.rebuildUrl;
    var deleteUrl = root.dataset.deleteUrl;
    var detailsUrl = root.dataset.detailsUrl;
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var saveBtn = document.getElementById("definCompSaveBtn");
    var rebuildBtn = document.getElementById("definCompRebuildBtn");
    var deleteBtn = document.getElementById("definCompDeleteBtn");
    var newBtn = document.getElementById("definCompNewBtn");
    var statusBox = document.getElementById("definCompStatus");
    var componentsTable = document.getElementById("componentsTable");
    var outputsTable = document.getElementById("outputsTable");
    var itemSearch = document.getElementById("itemNameSearch");
    var saveLock = false;
    var lookupCache = {};
    var saveButtonText = saveBtn ? saveBtn.textContent : "";
    var rebuildButtonText = rebuildBtn ? rebuildBtn.textContent : "";
    var deleteButtonText = deleteBtn ? deleteBtn.textContent : "";

    function token() {
        return tokenInput ? tokenInput.value : "";
    }

    function getRows(table) {
        return Array.prototype.slice.call(table.querySelectorAll("tbody tr.defin-comp-line"));
    }

    function setText(id, value) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = value;
        }
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

    function toNumber(value) {
        var n = parseFloat(value);
        return isNaN(n) ? 0 : n;
    }

    function toIntOrNull(value) {
        var n = parseInt(value, 10);
        return isNaN(n) ? null : n;
    }

    function recalcRow(row) {
        var qty = toNumber(row.querySelector(".qty").value);
        var cost = toNumber(row.querySelector(".cost").value);
        row.querySelector(".line-total").value = (qty * cost).toFixed(2);
    }

    function recalcTotals() {
        var componentQty = 0;
        var componentCost = 0;
        var outputQty = 0;
        var outputCost = 0;

        getRows(componentsTable).forEach(function (row) {
            recalcRow(row);
            var qty = toNumber(row.querySelector(".qty").value);
            var cost = toNumber(row.querySelector(".cost").value);
            componentQty += qty;
            componentCost += qty * cost;
        });

        getRows(outputsTable).forEach(function (row) {
            recalcRow(row);
            var qty = toNumber(row.querySelector(".qty").value);
            var cost = toNumber(row.querySelector(".cost").value);
            outputQty += qty;
            outputCost += qty * cost;
        });

        setText("componentQtyTotal", componentQty.toFixed(2));
        setText("componentCostTotal", componentCost.toFixed(2));
        setText("outputQtyTotal", outputQty.toFixed(2));
        setText("outputCostTotal", outputCost.toFixed(2));
        setText("differenceTotal", (outputCost - componentCost).toFixed(2));
    }

    function ensureEmptyRows() {
        [componentsTable, outputsTable].forEach(function (table) {
            var tbody = table.querySelector("tbody");
            var rows = tbody.querySelectorAll("tr.defin-comp-line");
            var empty = tbody.querySelector("tr.defin-comp-empty-row");
            if (rows.length === 0 && !empty) {
                var placeholder = document.createElement("tr");
                placeholder.className = "defin-comp-empty-row";
                placeholder.innerHTML = '<td colspan="7">لا توجد بيانات بعد.</td>';
                tbody.appendChild(placeholder);
            }
            if (rows.length > 0 && empty) {
                empty.parentNode.removeChild(empty);
            }
        });
    }

    function attachAutocomplete(input) {
        if (input.dataset.autocompleteBound === "1") {
            return;
        }

        input.dataset.autocompleteBound = "1";
        var datalistId = "items_" + Math.random().toString(36).slice(2);
        var datalist = document.createElement("datalist");
        datalist.id = datalistId;
        document.body.appendChild(datalist);
        input.setAttribute("list", datalistId);

        input.addEventListener("input", function () {
            var term = input.value || "";
            clearTimeout(input._lookupTimer);
            input._lookupTimer = setTimeout(function () {
                fetchItems(term, function (items) {
                    datalist.innerHTML = "";
                    items.forEach(function (item) {
                        var option = document.createElement("option");
                        option.value = item.Text;
                        option.setAttribute("data-id", item.Id);
                        datalist.appendChild(option);
                    });
                });
            }, 250);
        });

        input.addEventListener("change", function () {
            var options = datalist.querySelectorAll("option");
            var match = null;
            for (var i = 0; i < options.length; i++) {
                if (options[i].value === input.value) {
                    match = options[i];
                    break;
                }
            }
            if (match) {
                input.dataset.itemId = match.getAttribute("data-id");
            }
        });
    }

    function fetchItems(term, callback) {
        var key = "items:" + term;
        if (lookupCache[key]) {
            callback(lookupCache[key]);
            return;
        }

        fetch(lookupItemsUrl + "?term=" + encodeURIComponent(term || ""))
            .then(function (response) { return response.json(); })
            .then(function (json) {
                var items = json && json.items ? json.items : [];
                lookupCache[key] = items;
                callback(items);
            })
            .catch(function () { callback([]); });
    }

    function loadUnitsForRow(row) {
        var itemId = row.querySelector(".item-text").dataset.itemId;
        var unitSelect = row.querySelector(".unit-id");
        if (!itemId) {
            return;
        }

        fetch(lookupUnitsUrl + "?itemId=" + encodeURIComponent(itemId))
            .then(function (response) { return response.json(); })
            .then(function (json) {
                if (!json || !json.items) {
                    return;
                }
                unitSelect.innerHTML = '<option value="">اختر</option>';
                json.items.forEach(function (unit) {
                    var option = document.createElement("option");
                    option.value = unit.Id;
                    option.textContent = unit.Text;
                    unitSelect.appendChild(option);
                });
            })
            .catch(function () { });
    }

    function bindRow(row) {
        row.querySelectorAll(".qty, .cost").forEach(function (input) {
            input.addEventListener("input", recalcTotals);
        });

        var remove = row.querySelector(".js-remove-line");
        if (remove) {
            remove.addEventListener("click", function () {
                row.parentNode.removeChild(row);
                ensureEmptyRows();
                recalcTotals();
            });
        }

        var itemInput = row.querySelector(".item-text");
        if (itemInput) {
            attachAutocomplete(itemInput);
            itemInput.addEventListener("change", function () {
                loadUnitsForRow(row);
            });
        }
    }

    function newLineTemplate(kind) {
        var template = document.getElementById("lineTemplate");
        var clone = template.content ? template.content.firstElementChild.cloneNode(true) : template.querySelector("tr").cloneNode(true);
        clone.setAttribute("data-kind", kind);
        bindRow(clone);
        return clone;
    }

    function addLine(table, kind) {
        var tbody = table.querySelector("tbody");
        var empty = tbody.querySelector(".defin-comp-empty-row");
        if (empty) {
            empty.parentNode.removeChild(empty);
        }
        var row = newLineTemplate(kind);
        tbody.appendChild(row);
        recalcTotals();
        row.querySelector(".item-text").focus();
    }

    function collectLines(table) {
        return getRows(table).map(function (row) {
            var itemInput = row.querySelector(".item-text");
            return {
                ItemId: itemInput && itemInput.dataset.itemId ? parseInt(itemInput.dataset.itemId, 10) : null,
                ItemId2: itemInput && itemInput.dataset.itemId ? parseInt(itemInput.dataset.itemId, 10) : null,
                UnitId: row.querySelector(".unit-id") ? toIntOrNull(row.querySelector(".unit-id").value) : null,
                Qty: toNumber(row.querySelector(".qty").value),
                Cost: toNumber(row.querySelector(".cost").value),
                Price: toNumber(row.querySelector(".cost").value),
                Total: toNumber(row.querySelector(".qty").value) * toNumber(row.querySelector(".cost").value),
                Remark: row.querySelector(".remark") ? row.querySelector(".remark").value : "",
                LineId: null
            };
        });
    }

    function collectPayload() {
        return {
            Id: toIntOrNull(document.getElementById("definCompId").value),
            RecordDate: document.getElementById("recordDate").value,
            BranchId: toIntOrNull(document.getElementById("branchId").value),
            StoreId: toIntOrNull(document.getElementById("storeId").value),
            StoreId2: toIntOrNull(document.getElementById("storeId2").value),
            StoreId3: toIntOrNull(document.getElementById("storeId3").value),
            CusId: toIntOrNull(document.getElementById("cusId").value),
            ItemNameId: toIntOrNull(itemSearch.dataset.itemId),
            MaxNo: document.getElementById("maxNo").value,
            MaxName: document.getElementById("maxName").value,
            OrderNo: document.getElementById("orderNo").value,
            GroupId: toIntOrNull(document.getElementById("groupId").value),
            PaymentType: toIntOrNull(document.getElementById("paymentType").value),
            EmpId: toIntOrNull(document.getElementById("empId").value),
            BoxId: toIntOrNull(document.getElementById("boxId").value),
            Period: toNumber(document.getElementById("period").value),
            Qty1: toNumber(document.getElementById("qty1").value),
            Price: toNumber(document.getElementById("price").value),
            TotalAdd: toNumber(document.getElementById("totalAdd").value),
            TotalDisc: toNumber(document.getElementById("totalDisc").value),
            Net: toNumber(document.getElementById("net").value),
            TotalWithVat: toNumber(document.getElementById("totalWithVat").value),
            Vat2: toNumber(document.getElementById("vat2").value),
            TransactionComment: "سند تجميع",
            Components: collectLines(componentsTable),
            Outputs: collectLines(outputsTable)
        };
    }

    function validate(payload) {
        if (!payload.RecordDate) return "يجب تحديد التاريخ.";
        if (!payload.BranchId) return "يجب تحديد الفرع.";
        if (!payload.StoreId) return "يجب تحديد المخزن.";
        if (!payload.ItemNameId) return "يجب تحديد الصنف النهائي.";
        if (!payload.Components.length) return "أضف مكونًا واحدًا على الأقل.";
        if (!payload.Outputs.length) return "أضف منتجًا نهائيًا واحدًا على الأقل.";

        for (var i = 0; i < payload.Components.length; i++) {
            if (!payload.Components[i].ItemId || !payload.Components[i].UnitId || payload.Components[i].Qty <= 0) {
                return "تأكد من استكمال بيانات المكونات والكميات.";
            }
        }

        for (var j = 0; j < payload.Outputs.length; j++) {
            if (!payload.Outputs[j].ItemId || !payload.Outputs[j].UnitId || payload.Outputs[j].Qty <= 0) {
                return "تأكد من استكمال بيانات المنتجات النهائية والكميات.";
            }
        }

        return "";
    }

    function setBusy(busy) {
        saveLock = busy;
        [saveBtn, rebuildBtn, deleteBtn, newBtn].forEach(function (btn) {
            if (!btn) return;
            if (busy) {
                if (!btn.dataset.originalDisabled) {
                    btn.dataset.originalDisabled = btn.disabled ? "1" : "0";
                }
                if (!btn.dataset.originalText) {
                    btn.dataset.originalText = btn.textContent;
                }
                btn.disabled = true;
                if (btn === saveBtn) {
                    btn.textContent = "جارٍ الحفظ...";
                } else if (btn === rebuildBtn) {
                    btn.textContent = "جارٍ إعادة التوليد...";
                } else if (btn === deleteBtn) {
                    btn.textContent = "جارٍ الإلغاء...";
                }
            } else {
                btn.disabled = btn.dataset.originalDisabled === "1";
                if (btn.dataset.originalText) {
                    btn.textContent = btn.dataset.originalText;
                }
            }
        });
        document.body.classList.toggle("defin-comp-saving", busy);
    }

    function postJson(url, payload) {
        return fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/json; charset=utf-8",
                "RequestVerificationToken": token()
            },
            body: JSON.stringify(payload)
        }).then(function (response) { return response.json(); });
    }

    function save(rebuild) {
        if (saveLock) {
            return;
        }

        clearStatus();
        var payload = collectPayload();
        var error = validate(payload);
        if (error) {
            showStatus(error, "error");
            return;
        }

        setBusy(true);
        postJson(rebuild ? rebuildUrl : saveUrl, payload)
            .then(function (json) {
                setBusy(false);
                if (!json || !json.Success) {
                    showStatus((json && json.Message) ? json.Message : "تعذر حفظ السند.", "error");
                    return;
                }
                var parts = [json.Message || "تم الحفظ بنجاح."];
                if (json.IssueTransactionId || json.issueTransactionId) {
                    parts.push("سند الصرف: " + (json.IssueTransactionId || json.issueTransactionId));
                }
                if (json.ReceiptTransactionId || json.receiptTransactionId) {
                    parts.push("سند الاستلام: " + (json.ReceiptTransactionId || json.receiptTransactionId));
                }
                showStatus(parts.join(" - "), "success");
                window.setTimeout(function () {
                    window.location = window.location.pathname + "?id=" + encodeURIComponent(json.Id);
                }, 250);
            })
            .catch(function () {
                setBusy(false);
                showStatus("حدث خطأ أثناء الحفظ.", "error");
            });
    }

    function deleteCurrent() {
        var id = toIntOrNull(document.getElementById("definCompId").value);
        if (!id) {
            showStatus("لا يوجد سند محدد للحذف.", "error");
            return;
        }
        if (!confirm("هل تريد إلغاء سند التجميع الحالي؟")) {
            return;
        }
        clearStatus();
        setBusy(true);
        fetch(deleteUrl + "?id=" + encodeURIComponent(id), { method: "POST" })
            .then(function (response) { return response.json(); })
            .then(function (json) {
                setBusy(false);
                if (!json || !json.Success) {
                    showStatus((json && json.Message) ? json.Message : "تعذر الحذف.", "error");
                    return;
                }
                showStatus(json.Message || "تم حذف السند.", "success");
                window.setTimeout(function () {
                    window.location = window.location.pathname;
                }, 250);
            })
            .catch(function () {
                setBusy(false);
                showStatus("تعذر حذف السند.", "error");
            });
    }

    function loadDetails(id) {
        if (!id) return;
        fetch(detailsUrl + "?id=" + encodeURIComponent(id))
            .then(function (response) { return response.json(); })
            .then(function (json) {
                if (!json || !json.success) {
                    showStatus(json && json.message ? json.message : "تعذر تحميل السند.", "error");
                    return;
                }
                window.location = window.location.pathname + "?id=" + encodeURIComponent(id);
            })
            .catch(function () { showStatus("تعذر تحميل السند.", "error"); });
    }

    function initSearchRowButtons() {
        document.querySelectorAll(".js-load-defin-item").forEach(function (button) {
            button.addEventListener("click", function () {
                loadDetails(button.getAttribute("data-id"));
            });
        });
    }

    function resetForm() {
        window.location = window.location.pathname;
    }

    function init() {
        clearStatus();
        getRows(componentsTable).concat(getRows(outputsTable)).forEach(bindRow);
        recalcTotals();
        ensureEmptyRows();
        initSearchRowButtons();

        document.getElementById("addComponentRowBtn").addEventListener("click", function () {
            addLine(componentsTable, "component");
        });

        document.getElementById("addOutputRowBtn").addEventListener("click", function () {
            addLine(outputsTable, "output");
        });

        if (saveBtn) {
            saveBtn.addEventListener("click", function () { save(false); });
        }
        if (rebuildBtn) {
            rebuildBtn.addEventListener("click", function () { save(true); });
        }
        if (deleteBtn) {
            deleteBtn.addEventListener("click", deleteCurrent);
        }
        if (newBtn) {
            newBtn.addEventListener("click", resetForm);
        }

        if (itemSearch) {
            attachAutocomplete(itemSearch);
            itemSearch.addEventListener("change", function () {
                fetchItems(itemSearch.value || "", function (items) {
                    for (var i = 0; i < items.length; i++) {
                        if (items[i].Text === itemSearch.value) {
                            itemSearch.dataset.itemId = items[i].Id;
                            break;
                        }
                    }
                });
            });
        }
    }

    init();
})();
