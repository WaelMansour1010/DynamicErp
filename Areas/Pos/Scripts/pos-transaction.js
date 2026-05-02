(function () {
    "use strict";

    var modes = {
        "cash-in": { label: "كاش إن", isCashOut: false, isPOS: false, colorClass: "pos-mode-cash-in" },
        "cash-out": { label: "كاش أوت", isCashOut: true, isPOS: false, colorClass: "pos-mode-cash-out" },
        "card": { label: "كارت كيشني", isCashOut: false, isPOS: true, colorClass: "pos-mode-card" },
        "violations": { label: "مخالفات", isCashOut: false, isPOS: false, colorClass: "pos-mode-violations" }
    };

    var itemLookup = {};
    var itemSearchTimer = null;
    var customerLookupTimer = null;
    var commissionTimer = null;
    var todayInvoicesTimer = null;
    var todayInvoicesCache = [];
    var currentContext = null;
    var serviceLoadSequence = 0;
    var reviewMode = false;
    var lastSavedTransactionId = null;
    var kycSaveInProgress = false;

    function byId(id) { return document.getElementById(id); }
    function numberValue(id) { var value = parseFloat(byId(id).value); return isNaN(value) ? 0 : value; }
    function numberFromInput(input) { var value = parseFloat(input.value); return isNaN(value) ? 0 : value; }
    function selectedText(select) { return select.selectedIndex >= 0 ? select.options[select.selectedIndex].text : ""; }
    function savedTransactionId() { var value = parseInt(lastSavedTransactionId, 10); return isNaN(value) ? 0 : value; }
    function enablePrintIfAllowed() { byId("printBtn").disabled = !(savedTransactionId() > 0 && currentContext && currentContext.CanPrint === true); }
    function openPrintForTransaction(transactionId) {
        transactionId = parseInt(transactionId, 10) || 0;
        if (transactionId <= 0) {
            byId("saveResult").innerText = "لا توجد فاتورة محفوظة للطباعة";
            return false;
        }

        var previewUrl = getUrl("data-print-preview-url") || getUrl("data-print-url");
        window.open(previewUrl.replace(/\/$/, "") + "/" + encodeURIComponent(transactionId), "_blank");
        return true;
    }
    function isValidEgyptianMobile(value) { return /^(010|011|012|015)[0-9]{8}$/.test(value || ""); }
    function selectedRadioValue(name) {
        var selected = document.querySelector('input[name="' + name + '"]:checked');
        return selected ? selected.value : "";
    }

    function cardServiceItemIdFromCardNo(cardNo) {
        var value = (cardNo || "").trim();
        if (value.length === 18) { return 1; }
        if (value.length === 8) { return 19; }
        return 19;
    }

    function cardTypeNameFromCardNo(cardNo) {
        var value = (cardNo || "").trim();
        if (value.length === 18) { return "كارت ميزة كيشني"; }
        if (value.length === 8) { return "كارت البنك الأهلي"; }
        return "";
    }

    function getUrl(name) {
        return byId("posPage").getAttribute(name);
    }

    function getPageValue(name) {
        return byId("posPage").getAttribute(name) || "";
    }

    function readBool(value) {
        return value === true || value === "true" || value === "True";
    }

    function setCurrentContext(data) {
        data = data || {};
        currentContext = {
            UserId: data.UserId || data.UserID || null,
            UserName: data.UserName || "",
            EmpId: data.EmpId || data.EmpID || data.Emp_ID || null,
            EmpName: data.EmpName || "",
            BranchId: data.BranchId || null,
            BranchName: data.BranchName || "",
            StoreID: data.StoreID || data.StoreId || null,
            StoreName: data.StoreName || "",
            BoxID: data.BoxID || data.BoxId || null,
            BoxName: data.BoxName || "",
            PaymentNetid: data.PaymentNetid || data.PaymentNetId || null,
            PaymentTypeId: data.PaymentTypeId || null,
            PaymentName: data.PaymentName || "",
            BankId: data.BankId || null,
            BankName: data.BankName || "",
            CanAdd: data.CanAdd !== undefined ? readBool(data.CanAdd) : readBool(data.CanSave),
            CanPrint: readBool(data.CanPrint),
            CanReturn: readBool(data.CanReturn),
            CanOpenCashCustomer: readBool(data.CanOpenCashCustomer),
            IsFullAccess: readBool(data.IsFullAccess),
            CanChangeDefaults: readBool(data.CanChangeDefaults)
        };
    }

    function setInitialContextFromPage() {
        setCurrentContext({
            BranchId: getPageValue("data-default-branch-id"),
            EmpId: getPageValue("data-default-emp-id"),
            EmpName: getPageValue("data-default-emp-name"),
            StoreID: getPageValue("data-default-store-id"),
            BoxID: getPageValue("data-default-box-id"),
            BoxName: getPageValue("data-default-box-name"),
            PaymentTypeId: getPageValue("data-default-payment-id"),
            PaymentName: getPageValue("data-default-payment-name"),
            BankId: getPageValue("data-default-bank-id"),
            BankName: getPageValue("data-default-bank-name"),
            BranchName: getPageValue("data-default-branch-name"),
            StoreName: getPageValue("data-default-store-name"),
            CanAdd: getPageValue("data-can-save"),
            CanPrint: getPageValue("data-can-print"),
            CanReturn: getPageValue("data-can-return"),
            CanOpenCashCustomer: getPageValue("data-can-cash-customer"),
            IsFullAccess: getPageValue("data-is-full-access"),
            CanChangeDefaults: getPageValue("data-can-change-defaults")
        });
    }

    function contextValue(name, fallbackAttribute) {
        if (currentContext && currentContext[name] !== null && currentContext[name] !== undefined && currentContext[name] !== "") {
            return currentContext[name];
        }
        return getPageValue(fallbackAttribute);
    }

    function requestJson(method, url, body, callback) {
        var xhr = new XMLHttpRequest();
        xhr.open(method, url, true);
        xhr.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) { return; }

            var data = null;
            try {
                data = xhr.responseText ? JSON.parse(xhr.responseText) : null;
            } catch (ignore) {
                data = {
                    success: false,
                    message: "تعذر قراءة رد السيرفر أثناء الحفظ التجريبي",
                    technicalMessage: "HTTP " + xhr.status + (xhr.responseText ? " - " + xhr.responseText.substring(0, 500) : "")
                };
            }

            callback(xhr.status, data);
        };
        xhr.onerror = function () {
            callback(0, { success: false, message: "تعذر الاتصال بالسيرفر", technicalMessage: "Network error" });
        };
        xhr.send(body ? JSON.stringify(body) : null);
    }

    function requestFormData(url, formData, callback) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", url, true);
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) { return; }

            var data = null;
            try {
                data = xhr.responseText ? JSON.parse(xhr.responseText) : null;
            } catch (ignore) {
                data = null;
            }

            callback(xhr.status, data);
        };
        xhr.onerror = function () {
            callback(0, { success: false, message: "تعذر الاتصال بالسيرفر" });
        };
        xhr.send(formData);
    }

    function saveDisabledByContext(mode) {
        return (currentContext && currentContext.CanAdd === false) || (!currentContext && getPageValue("data-can-save") === "false");
    }

    function setMode(mode, suppressServiceLoad) {
        var config = modes[mode] || modes["cash-in"];
        var page = byId("posPage");
        page.className = "pos-page " + config.colorClass;
        byId("transactionType").value = mode;
        byId("isCashOut").value = config.isCashOut ? "true" : "false";
        byId("isPOS").value = config.isPOS ? "true" : "false";
        byId("posModeLabel").innerText = config.label;

        var buttons = document.querySelectorAll(".pos-type-btn");
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].classList.toggle("active", buttons[i].getAttribute("data-mode") === mode);
        }

        var cardFields = document.querySelectorAll(".card-field");
        for (var c = 0; c < cardFields.length; c++) {
            cardFields[c].classList.toggle("is-visible", mode === "card");
        }

        var kycFields = document.querySelectorAll(".kyc-only");
        for (var k = 0; k < kycFields.length; k++) {
            kycFields[k].classList.toggle("is-visible", mode === "card");
        }
        if (mode !== "card") {
            closeKycModal();
        }

        var walletFields = document.querySelectorAll(".wallet-field");
        for (var w = 0; w < walletFields.length; w++) {
            walletFields[w].classList.toggle("is-visible", mode === "cash-out");
        }

        var rechargeFields = document.querySelectorAll(".recharge-field");
        for (var r = 0; r < rechargeFields.length; r++) {
            rechargeFields[r].classList.toggle("is-hidden", mode === "violations" || mode === "card");
        }

        byId("violationPanel").style.display = mode === "violations" ? "block" : "none";
        byId("saveBtn").disabled = saveDisabledByContext(mode);
        byId("returnLaterBtn").disabled = mode === "violations" || !currentContext || currentContext.CanReturn !== true;

        if (mode === "cash-in") {
            byId("isWallet").value = "false";
            byId("tetNumPoket").value = "";
            byId("isRecharg").value = numberValue("rechargeValue") > 0 ? "true" : "false";
            byId("haveGuarantee").value = "false";
        }
        if (mode === "cash-out") {
            byId("isWallet").value = "true";
            byId("isRecharg").value = numberValue("rechargeValue") > 0 ? "true" : "false";
            byId("haveGuarantee").value = "false";
        }
        if (mode === "card") {
            byId("paymentCardNo").value = byId("visaNumber").value;
            byId("tetNumPoket").value = "";
            byId("isWallet").value = "false";
            byId("isRecharg").value = "false";
            byId("rechargeValue").value = "0";
            byId("rechargeValue").disabled = true;
        } else {
            byId("rechargeValue").disabled = false;
            clearKycFields();
        }
        if (mode === "violations") {
            byId("isWallet").value = "false";
            byId("tetNumPoket").value = "";
            byId("isRecharg").value = "false";
            byId("rechargeValue").value = "0";
        }

        if (!suppressServiceLoad) {
            resetServiceRows();
            if (mode === "card") {
                var cardItemId = cardServiceItemIdFromCardNo(byId("visaNumber").value);
                setSelectSingleOption("serviceItemId", cardItemId, cardItemId === 1 ? "كارت ميزة كيشني" : "كارت البنك الأهلي");
                loadDefaultServiceItem(mode, false, cardItemId);
            } else {
                loadPrimaryServiceItems(mode);
            }
            calculateTotals();
            scheduleCommissionPreview();
        }
        clearMessages();
    }

    function clearKycFields() {
        byId("cashCustomerId").value = "";
        byId("phone2").value = "";
        byId("ipn").value = "";
        byId("manualNo").value = "";
        byId("visaNumber").value = "";
        byId("cardNationalId").value = "";
        byId("paymentCardNo").value = "";
        byId("kycName").value = "";
        byId("kycNameE").value = "";
        byId("kycArabicName0").value = "";
        byId("kycArabicName1").value = "";
        byId("kycArabicName2").value = "";
        byId("kycEnglishName0").value = "";
        byId("kycEnglishName1").value = "";
        byId("kycEnglishName2").value = "";
        byId("kycEnglishName5").value = "";
        byId("kycEnglishName6").value = "";
        byId("kycEnglishName7").value = "";
        byId("kycPhoneNo2").value = "";
        byId("kycPhoneNo").value = "";
        byId("kycCardNo").value = "";
        byId("kycNationalId").value = "";
        byId("kycCardSource").value = "";
        byId("kycBirthDate").value = "";
        byId("kycCardDate").value = "";
        byId("kycCardEndDate").value = "";
        byId("kycAddress").value = "";
        byId("kycMailAddress").value = "";
        byId("kycTel").value = "";
        byId("kycCard").value = "";
        byId("kycSearchTerm").value = "";
        byId("kycSearchResults").innerHTML = "";
        setKycMessage("");
    }

    function setKycMessage(message, isError) {
        var messageBox = byId("kycSaveMessage");
        if (!messageBox) { return; }
        messageBox.innerText = message || "";
        messageBox.className = "kyc-save-message" + (message ? (isError ? " is-error" : " is-success") : "");
    }

    function addItemRow() {
        loadDefaultServiceItem(byId("transactionType").value, false, parseInt(byId("serviceItemId").value, 10) || null);
    }

    function createEmptyItemRow() {
        var tbody = byId("itemsTable").querySelector("tbody");
        var source = tbody.querySelector("tr");
        var row = source.cloneNode(true);
        clearRowData(row);

        var inputs = row.querySelectorAll("input");
        for (var i = 0; i < inputs.length; i++) {
            if (inputs[i].classList.contains("unit-id") || inputs[i].classList.contains("qty")) {
                inputs[i].value = "1";
            } else {
                inputs[i].value = inputs[i].classList.contains("item-name") ? "" : "0";
            }
        }

        tbody.appendChild(row);
        return row;
    }

    function clearRowData(row) {
        row.removeAttribute("data-item-id");
        row.removeAttribute("data-store-id2");
        row.removeAttribute("data-branch-id");
        row.removeAttribute("data-qty-by-small-unit");
        row.removeAttribute("data-show-price");
        row.removeAttribute("data-vatyo");
        row.removeAttribute("data-discount-value");
        row.removeAttribute("data-total-discount-per-line");
        row.removeAttribute("data-item-case");
        row.removeAttribute("data-cost-price");
        row.removeAttribute("data-saved-item-type");
    }

    function resetServiceRows() {
        serviceLoadSequence++;
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        for (var i = rows.length - 1; i > 0; i--) {
            rows[i].parentNode.removeChild(rows[i]);
        }

        clearRowData(rows[0]);
        var inputs = rows[0].querySelectorAll("input");
        for (var x = 0; x < inputs.length; x++) {
            if (inputs[x].classList.contains("unit-id") || inputs[x].classList.contains("qty")) {
                inputs[x].value = "1";
            } else {
                inputs[x].value = inputs[x].classList.contains("item-name") ? "" : "0";
            }
        }
        byId("serviceItemId").innerHTML = '<option value="">تحميل...</option>';
        byId("serviceItemId2").innerHTML = '<option value="">تحميل...</option>';
    }

    function calculateTotals() {
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        var itemTotal = 0;
        for (var i = 0; i < rows.length; i++) {
            var qty = parseFloat(rows[i].querySelector(".qty").value) || 0;
            var price = parseFloat(rows[i].querySelector(".price").value) || 0;
            var vat = parseFloat(rows[i].querySelector(".vat").value) || 0;
            var total = (qty * price) + vat;
            rows[i].querySelector(".line-total").value = total.toFixed(2);
            itemTotal += total;
        }
        var totalValue = itemTotal + numberValue("rechargeValue");
        byId("netValue").value = totalValue.toFixed(2);
        byId("remainValue").value = (totalValue - numberValue("payedValue")).toFixed(2);
        byId("totalFees").value = itemTotal.toFixed(2);
    }

    function validateForm() {
        var errors = [];
        var mode = byId("transactionType").value;

        if (!mode) { errors.push("نوع العملية مطلوب"); }
        if (!byId("cashCustomerPhone").value.trim()) { errors.push("رقم التليفون مطلوب"); }
        if (byId("cashCustomerPhone").value.trim() && !isValidEgyptianMobile(byId("cashCustomerPhone").value.trim())) { errors.push("رقم التليفون يجب أن يكون 11 رقم ويبدأ بـ 010 أو 011 أو 012 أو 015"); }
        if (!byId("cashCustomerName").value.trim()) { errors.push("اسم العميل مطلوب"); }
        if (!byId("ipn").value.trim()) { errors.push("ID مطلوب"); }
        if (!byId("manualNo").value.trim()) { errors.push("IPN مطلوب"); }
        if (mode === "card" && !byId("visaNumber").value.trim()) { errors.push("الكارت مطلوب في حالة كارت كيشني"); }
        if (mode === "card" && !byId("cashCustomerId").value) { errors.push("يجب تفعيل الكارت وحفظ بيانات KYC قبل حفظ الفاتورة"); }
        if (mode === "violations") {
            if (numberValue("violationValue") <= 0) { errors.push("قيمة المخالفات مطلوبة"); }
            if (selectedRadioValue("violationPayType") === "") { errors.push("طريقة دفع المخالفات مطلوبة"); }
            if (!byId("violationWalletNo").value.trim()) { errors.push("رقم المحفظة مطلوب"); }
        } else if (mode !== "card" && numberValue("rechargeValue") <= 0) {
            errors.push("مبلغ الشحن يجب أن يكون أكبر من صفر");
        }
        var primaryServiceId = parseInt(byId("serviceItemId").value, 10) || 0;
        if (mode !== "card" && mode !== "violations" && (primaryServiceId === 6 || primaryServiceId === 7 || primaryServiceId === 8 || primaryServiceId === 10) && !byId("serviceItemId2").value) {
            errors.push("المحفظة/البنك مطلوبة لهذا النوع");
        }
        if (!hasItemRows() || !hasValidItemSelection()) { errors.push("لا توجد خدمة كيشني محملة"); }
        if (!hasValidQuantities()) { errors.push("من فضلك أدخل كمية وسعر صحيح"); }
        if (!byId("branchId").value) { errors.push("الفرع غير محدد"); }
        if (!byId("paymentType").value) { errors.push("طريقة الدفع مطلوبة"); }
        if (!byId("boxId").value) { errors.push("الخزنة غير محددة"); }
        byId("validationSummary").innerHTML = errors.join("<br />");
        return errors.length === 0;
    }

    function hasItemRows() {
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        for (var i = 0; i < rows.length; i++) {
            var itemName = rows[i].querySelector(".item-name").value.trim();
            var qty = parseFloat(rows[i].querySelector(".qty").value) || 0;
            if (itemName && qty > 0) { return true; }
        }
        return false;
    }

    function hasValidItemSelection() {
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        for (var i = 0; i < rows.length; i++) {
            var itemName = rows[i].querySelector(".item-name").value.trim();
            if (itemName && !rows[i].getAttribute("data-item-id")) {
                return false;
            }
        }
        return true;
    }

    function hasValidQuantities() {
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        for (var i = 0; i < rows.length; i++) {
            var itemName = rows[i].querySelector(".item-name").value.trim();
            if (!itemName) { continue; }

            if (numberFromInput(rows[i].querySelector(".qty")) <= 0 || numberFromInput(rows[i].querySelector(".price")) < 0) {
                return false;
            }
        }
        return true;
    }

    function getFirstSelectedItemRow() {
        var rows = byId("itemsTable").querySelectorAll("tbody tr");
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].getAttribute("data-item-id")) {
                return rows[i];
            }
        }
        return null;
    }

    function buildRequest() {
        var items = [];
        var rows = byId("itemsTable").querySelectorAll("tbody tr");

        for (var i = 0; i < rows.length; i++) {
            var itemName = rows[i].querySelector(".item-name").value.trim();
            if (!itemName) { continue; }

            var qty = numberFromInput(rows[i].querySelector(".qty"));
            var price = numberFromInput(rows[i].querySelector(".price"));
            var vat = numberFromInput(rows[i].querySelector(".vat"));
            var total = (qty * price) + vat;

            items.push({
                Item_ID: parseInt(rows[i].getAttribute("data-item-id"), 10),
                ItemName: itemName,
                UnitId: parseInt(rows[i].querySelector(".unit-id").value, 10) || 1,
                Quantity: qty,
                ShowQty: qty,
                QtyBySmalltUnit: parseFloat(rows[i].getAttribute("data-qty-by-small-unit")) || 1,
                Price: price,
                ShowPrice: parseFloat(rows[i].getAttribute("data-show-price")) || price,
                TotalPrice: total,
                Vat: vat,
                Vatyo: parseFloat(rows[i].getAttribute("data-vatyo")) || 0,
                DiscountValue: parseFloat(rows[i].getAttribute("data-discount-value")) || 0,
                TotalDiscountPerLine: parseFloat(rows[i].getAttribute("data-total-discount-per-line")) || 0,
                StoreID2: parseInt(rows[i].getAttribute("data-store-id2"), 10) || null,
                ItemCase: parseInt(rows[i].getAttribute("data-item-case"), 10) || 1,
                CostPrice: parseFloat(rows[i].getAttribute("data-cost-price")) || 0,
                SavedItemType: parseInt(rows[i].getAttribute("data-saved-item-type"), 10) || 0
            });
        }

        var firstRow = getFirstSelectedItemRow();
        var branchId = parseInt(byId("branchId").value, 10) || null;
        var storeId = firstRow ? parseInt(firstRow.getAttribute("data-store-id2"), 10) : null;
        if (!storeId) {
            storeId = parseInt(byId("storeId").value, 10) || null;
        }
        var paymentType = parseInt(byId("paymentType").value, 10) || 0;
        var transactionType = byId("transactionType").value;
        var firstItemId = firstRow ? parseInt(firstRow.getAttribute("data-item-id"), 10) || null : null;
        var payType = transactionType === "violations"
            ? (parseInt(selectedRadioValue("violationPayType"), 10) || 0)
            : 1;

        return {
            TransactionType: transactionType,
            TransactionDate: new Date().toISOString().substring(0, 10),
            BranchId: branchId,
            StoreID: storeId,
            UserID: null,
            Emp_ID: parseInt(byId("empId").value, 10) || null,
            CustomerID: 2,
            TblCusCshId: transactionType === "card" ? parseInt(byId("cashCustomerId").value, 10) || null : null,
            DefaultCustomerId: 2,
            PaymentType: paymentType,
            BoxID: parseInt(byId("boxId").value, 10) || null,
            PayedValue: numberValue("payedValue"),
            NetValue: numberValue("netValue"),
            RemainValue: numberValue("remainValue"),
            PaymentNetid: null,
            IsCashOut: byId("isCashOut").value === "true",
            IsPOS: byId("isPOS").value === "true",
            OtherItems: byId("otherItems").value === "true",
            PayType: payType,
            POSBillType: 0,
            STableID: -1,
            SessionD: -1,
            BillBasedOn: 0,
            CashCustomerName: byId("cashCustomerName").value,
            CashCustomerPhone: byId("cashCustomerPhone").value,
            Phone2: transactionType === "card" ? byId("phone2").value : "",
            IPN: byId("ipn").value,
            ManualNO: byId("manualNo").value,
            NoID: "",
            VisaNumber: transactionType === "card" ? byId("visaNumber").value : "",
            CardSerial: transactionType === "card" ? byId("visaNumber").value : "",
            RechargeValue: transactionType === "card" ? 0 : (transactionType === "violations" ? null : numberValue("rechargeValue")),
            CommissionValue: numberValue("commissionValue"),
            VatValue: numberValue("vatValue"),
            TotalFees: numberValue("totalFees"),
            RechargeType: selectedText(byId("serviceItemId")),
            TrafficViolations: transactionType === "violations",
            ViolationsValue: numberValue("violationValue"),
            ItemIDService: parseInt(byId("serviceItemId").value, 10) || firstItemId,
            ItemIDService2: parseInt(byId("serviceItemId2").value, 10) || null,
            ViolationPayType: parseInt(selectedRadioValue("violationPayType"), 10) || 0,
            Tet_NumPoket: transactionType === "card" ? byId("cardNationalId").value : byId("tetNumPoket").value,
            IsRecharg: transactionType !== "card" && transactionType !== "violations" && numberValue("rechargeValue") > 0,
            IsWallet: transactionType === "violations" ? false : byId("isWallet").value === "true",
            HaveGuarantee: byId("haveGuarantee").value === "true",
            Items: items,
            SalesPayments: [{
                PaymentID: paymentType,
                PaymentName: selectedText(byId("paymentType")),
                Value: numberValue("payedValue"),
                CardNo: byId("paymentCardNo").value,
                MaxValue: numberValue("payedValue")
            }]
        };
    }

    function saveTransaction(event) {
        event.preventDefault();
        calculateTotals();
        clearMessages();

        if (!validateForm()) { return; }

        byId("saveBtn").disabled = true;
        requestJson("POST", getUrl("data-save-url"), buildRequest(), function (status, data) {
            byId("saveBtn").disabled = saveDisabledByContext(byId("transactionType").value);

            if (status >= 200 && status < 300 && data && data.success) {
                var successMessage = "تم الحفظ التجريبي بنجاح<br />رقم الفاتورة: " + (data.noteSerial1 || "") + "<br />رقم الحركة: " + (data.transactionId || "");
                lastSavedTransactionId = parseInt(data.transactionId, 10) || null;
                enablePrintIfAllowed();
                loadEmployeeBalances();
                loadTodayInvoices();
                byId("saveResult").innerHTML = successMessage;
                if (window.confirm("تم الحفظ بنجاح. هل تريد الطباعة؟")) {
                    openPrintForTransaction(lastSavedTransactionId);
                }

                reloadContextAndReset(successMessage);
                return;
            }

            showSaveError(data);
        });
    }

    function showSaveError(data) {
        data = data || {};
        var html = "";
        var validationErrors = data.validationErrors || {};

        if (Object.keys(validationErrors).length) {
            html += '<div class="save-error-block"><strong>أخطاء البيانات:</strong><br />';
            for (var key in validationErrors) {
                if (Object.prototype.hasOwnProperty.call(validationErrors, key)) {
                    html += escapeHtml(validationErrors[key]) + "<br />";
                }
            }
            html += "</div>";
        }

        html += '<div class="save-error-block">السبب: ' + escapeHtml(data.message || "تعذر الحفظ التجريبي") + "</div>";
        if (data.technicalMessage) {
            html += '<div class="save-error-technical">التفاصيل الفنية: ' + escapeHtml(data.technicalMessage) + "</div>";
        }

        byId("validationSummary").innerHTML = html;
    }

    function loadEmployeeBalances() {
        var url = getUrl("data-balances-url");
        if (!url) { return; }

        requestJson("GET", url, null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                byId("employeeBalanceText").innerText = "ذمة الموظف: غير متاح";
                byId("boxBalanceText").innerText = "عهدة الخزنة: غير متاح";
                return;
            }

            byId("employeeBalanceText").innerText = "ذمة الموظف: " + (data.EmployeeBalanceText || "غير محدد");
            byId("boxBalanceText").innerText = "عهدة الخزنة: " + (data.BoxBalanceText || "غير محدد");
            byId("employeeBalanceText").setAttribute("data-account-code", data.EmployeeAccountCode || "");
            byId("boxBalanceText").setAttribute("data-account-code", data.BoxAccountCode || "");
        });
    }

    function loadTodayInvoices(term) {
        var url = getUrl("data-today-invoices-url");
        if (!url) { return; }

        requestJson("GET", url + "?term=" + encodeURIComponent(term || ""), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                byId("todayInvoicesList").innerText = "تعذر تحميل فواتير اليوم";
                return;
            }

            todayInvoicesCache = data;
            renderTodayInvoices(data);
        });
    }

    function renderTodayInvoices(invoices) {
        var container = byId("todayInvoicesList");
        container.innerHTML = "";

        if (!invoices || !invoices.length) {
            container.innerText = "لا توجد فواتير اليوم";
            byId("todayInvoiceSummary").innerHTML = "";
            return;
        }

        for (var i = 0; i < invoices.length; i++) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "today-invoice-item";
            item.setAttribute("data-index", i);
            item.setAttribute("data-transaction-id", invoices[i].Transaction_ID || "");
            item.innerHTML =
                '<strong>' + escapeHtml(invoices[i].NoteSerial1 || invoices[i].Transaction_ID) + '</strong>' +
                '<span>' + escapeHtml(invoices[i].ServiceType || "") + ' | ' + escapeHtml(invoices[i].TransactionTime || "") + '</span>' +
                '<span>' + escapeHtml(invoices[i].CustomerName || "") + ' - ' + escapeHtml(invoices[i].CustomerPhone || "") + '</span>';
            container.appendChild(item);
        }
    }

    function showTodayInvoiceSummary(index) {
        var invoice = todayInvoicesCache[index];
        if (!invoice) { return; }

        byId("todayInvoiceSummary").innerHTML =
            '<div><strong>رقم الفاتورة:</strong> ' + escapeHtml(invoice.NoteSerial1 || "") + '</div>' +
            '<div><strong>رقم الحركة:</strong> ' + escapeHtml(invoice.Transaction_ID || "") + '</div>' +
            '<div><strong>النوع:</strong> ' + escapeHtml(invoice.ServiceType || "") + '</div>' +
            '<div><strong>العميل:</strong> ' + escapeHtml(invoice.CustomerName || "") + '</div>' +
            '<div><strong>التليفون:</strong> ' + escapeHtml(invoice.CustomerPhone || "") + '</div>' +
            '<div><strong>المدفوع:</strong> ' + escapeHtml(invoice.PayedValue || "0") + '</div>';
    }

    function setSelectSingleOption(selectId, value, text) {
        var select = byId(selectId);
        select.innerHTML = "";
        select.appendChild(new Option(text || value || "", value || ""));
        select.value = value || "";
    }

    function applyReviewItem(item) {
        resetServiceRows();
        if (!item) { return; }

        applyServiceItem(byId("itemsTable").querySelector("tbody tr"), {
            Item_ID: item.Item_ID,
            ItemName: item.ItemName,
            UnitId: item.UnitId,
            Quantity: item.Quantity || 1,
            Price: item.Price || 0,
            ShowPrice: item.ShowPrice || item.Price || 0,
            TotalPrice: item.TotalPrice || 0,
            QtyBySmalltUnit: item.QtyBySmalltUnit || 1,
            Vat: item.Vat || 0,
            Vatyo: item.Vatyo || 0,
            DiscountValue: item.DiscountValue || 0,
            TotalDiscountPerLine: item.TotalDiscountPerLine || 0,
            StoreID2: item.StoreID2 || "",
            ItemCase: item.ItemCase || 1,
            CostPrice: item.CostPrice || 0,
            SavedItemType: item.SavedItemType || 0
        }, true);
    }

    function decimalText(value) {
        var number = parseFloat(value);
        return isNaN(number) ? "0.00" : number.toFixed(2);
    }

    function textReference(value) {
        if (value === null || value === undefined) { return ""; }
        var text = String(value).trim();
        var scientific = text.match(/^([0-9]+(?:\.[0-9]+)?)e\+?([0-9]+)$/i);
        if (scientific) {
            var digits = scientific[1].replace(".", "");
            var decimals = (scientific[1].split(".")[1] || "").length;
            var zeros = parseInt(scientific[2], 10) - decimals;
            text = digits + new Array(Math.max(zeros, 0) + 1).join("0");
        }
        if (/^1[0-9]{9}$/.test(text)) {
            text = "0" + text;
        }
        return text;
    }

    function loadInvoiceForReview(transactionId) {
        if (!transactionId) { return; }

        requestJson("GET", getUrl("data-invoice-url") + "?transactionId=" + encodeURIComponent(transactionId), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                byId("validationSummary").innerText = "تعذر فتح الفاتورة";
                return;
            }

            lastSavedTransactionId = parseInt(data.Transaction_ID || transactionId, 10) || null;
            reviewMode = true;
            setMode(data.TransactionType || "cash-in", true);
            setSelectSingleOption("serviceItemId", data.ItemIDService || (data.Items && data.Items[0] ? data.Items[0].Item_ID : ""), data.Items && data.Items[0] ? data.Items[0].ItemName : "");
            setSelectSingleOption("serviceItemId2", data.ItemIDService2 || "", data.ItemIDService2 ? String(data.ItemIDService2) : "");

            byId("cashCustomerPhone").value = data.CashCustomerPhone || "";
            byId("cashCustomerName").value = data.CashCustomerName || "";
            byId("ipn").value = data.IPN || "";
            byId("manualNo").value = data.ManualNO || "";
            byId("phone2").value = data.Phone2 || "";
            byId("visaNumber").value = data.VisaNumber || "";
            byId("paymentCardNo").value = data.VisaNumber || "";
            byId("tetNumPoket").value = textReference(data.Tet_NumPoket);
            byId("rechargeValue").value = decimalText(data.RechargeValue);
            byId("commissionValue").value = decimalText(data.NetValue);
            byId("vatValue").value = decimalText(data.VatValue);
            byId("totalFees").value = decimalText(data.TotalFees);
            byId("netValue").value = decimalText((parseFloat(data.RechargeValue) || 0) + (parseFloat(data.TotalFees) || 0));
            byId("payedValue").value = decimalText(data.PayedValue);
            byId("remainValue").value = decimalText(data.RemainValue);
            byId("violationValue").value = decimalText(data.ViolationsValue);
            if ((data.TransactionType || "") === "card" && data.KycCustomer) {
                applyKeshniCustomer(data.KycCustomer);
            } else if ((data.TransactionType || "") === "card") {
                renderKycAttachments([]);
            }

            if (data.Items && data.Items.length) {
                applyReviewItem(data.Items[0]);
            }

            byId("rechargeValue").value = decimalText(data.RechargeValue);
            byId("commissionValue").value = decimalText(data.NetValue);
            byId("vatValue").value = decimalText(data.VatValue);
            byId("totalFees").value = decimalText(data.TotalFees);
            byId("netValue").value = decimalText((parseFloat(data.RechargeValue) || 0) + (parseFloat(data.TotalFees) || 0));
            byId("payedValue").value = decimalText(data.PayedValue);
            byId("remainValue").value = decimalText(data.RemainValue);

            byId("saveBtn").disabled = true;
            byId("saveResult").innerHTML = "وضع مراجعة فقط - رقم الفاتورة: " + escapeHtml(data.NoteSerial1 || "") + "<br />رقم الحركة: " + escapeHtml(data.Transaction_ID || "");
            enablePrintIfAllowed();
        });
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function populatePaymentTypes() {
        requestJson("GET", getUrl("data-payment-types-url"), null, function (status, data) {
            var select = byId("paymentType");
            select.innerHTML = "";

            if (status < 200 || status >= 300 || !data || !data.length) {
                select.innerHTML = '<option value="">لا توجد طرق دفع</option>';
                return;
            }

            select.appendChild(new Option("اختر طريقة الدفع", ""));
            for (var i = 0; i < data.length; i++) {
                var option = new Option(data[i].PaymentName, data[i].PaymentID);
                option.setAttribute("data-bank-name", data[i].BankName || "");
                if (data[i].MaxValue !== null && data[i].MaxValue !== undefined) {
                    option.setAttribute("data-max-value", data[i].MaxValue);
                }
                select.appendChild(option);
            }

            var defaultPaymentId = contextValue("PaymentTypeId", "data-default-payment-id");
            if (defaultPaymentId) { select.value = defaultPaymentId; }
            byId("bankName").value = selectedOptionAttribute(select, "data-bank-name") || contextValue("BankName", "data-default-bank-name") || "";
        });
    }

    function selectedOptionAttribute(select, attributeName) {
        return select.selectedIndex >= 0 ? select.options[select.selectedIndex].getAttribute(attributeName) : "";
    }

    function setSingleOption(selectId, value, text) {
        var select = byId(selectId);
        select.innerHTML = "";
        select.appendChild(new Option(text || value || "", value || ""));
        select.value = value || "";
    }

    function applyDefaultContextControls() {
        setSingleOption("branchId", contextValue("BranchId", "data-default-branch-id"), contextValue("BranchName", "data-default-branch-name"));
        setSingleOption("boxId", contextValue("BoxID", "data-default-box-id"), contextValue("BoxName", "data-default-box-name"));
        setSingleOption("paymentType", contextValue("PaymentTypeId", "data-default-payment-id"), contextValue("PaymentName", "data-default-payment-name"));

        byId("storeId").value = contextValue("StoreID", "data-default-store-id") || "";
        byId("storeName").value = contextValue("StoreName", "data-default-store-name") || "";
        byId("empId").value = contextValue("EmpId", "data-default-emp-id") || "";
        byId("empName").value = ((contextValue("EmpName", "data-default-emp-name") || "") + " - " + (contextValue("EmpId", "data-default-emp-id") || "")).replace(/^ - | - $/g, "");
        byId("bankName").value = contextValue("BankName", "data-default-bank-name") || "";
        byId("branchId").disabled = true;
        byId("boxId").disabled = true;
        byId("paymentType").disabled = true;
        byId("storeName").readOnly = true;
        byId("bankName").readOnly = true;
    }

    function loadContextControls() {
        if (!currentContext || currentContext.CanChangeDefaults !== true) {
            applyDefaultContextControls();
            return;
        }

        populatePaymentTypes();
        populateCashBoxes();
        populateBranches();
        loadStoresForBranch(contextValue("BranchId", "data-default-branch-id"));
        byId("paymentType").disabled = false;
        byId("bankName").readOnly = true;
    }

    function populateCashBoxes() {
        requestJson("GET", getUrl("data-cash-boxes-url"), null, function (status, data) {
            var select = byId("boxId");
            select.innerHTML = "";

            if (status < 200 || status >= 300 || !data || !data.length) {
                select.innerHTML = '<option value="">لا توجد خزنة مؤكدة</option>';
                return;
            }

            select.appendChild(new Option("اختر الخزنة", ""));
            for (var i = 0; i < data.length; i++) {
                var option = new Option(data[i].BoxName, data[i].BoxID);
                if (data[i].BranchId !== null && data[i].BranchId !== undefined) {
                    option.setAttribute("data-branch-id", data[i].BranchId);
                }
                option.setAttribute("data-is-wallet", data[i].IsWallet ? "true" : "false");
                option.setAttribute("data-is-terminal-pos", data[i].IsTerminalPOS ? "true" : "false");
                select.appendChild(option);
            }

            var defaultBoxId = contextValue("BoxID", "data-default-box-id");
            if (defaultBoxId) { select.value = defaultBoxId; }
            select.disabled = true;
        });
    }

    function populateBranches() {
        requestJson("GET", getUrl("data-branches-url"), null, function (status, data) {
            var select = byId("branchId");
            select.innerHTML = "";

            if (status < 200 || status >= 300 || !data || !data.length) {
                select.innerHTML = '<option value="">لا توجد فروع مؤكدة</option>';
                return;
            }

            select.appendChild(new Option("اختر الفرع", ""));
            for (var i = 0; i < data.length; i++) {
                select.appendChild(new Option(data[i].BranchName, data[i].BranchId));
            }

            var defaultBranchId = contextValue("BranchId", "data-default-branch-id");
            if (defaultBranchId) { select.value = defaultBranchId; }
            select.disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        });
    }

    function loadStoresForBranch(branchId) {
        requestJson("GET", getUrl("data-stores-url") + "?branchId=" + encodeURIComponent(branchId || ""), null, function (status, data) {
            if (status < 200 || status >= 300 || !data || !data.length) { return; }

            var defaultStoreId = parseInt(contextValue("StoreID", "data-default-store-id"), 10) || 0;
            for (var i = 0; i < data.length; i++) {
                if (data[i].StoreID === defaultStoreId || (!defaultStoreId && i === 0)) {
                    byId("storeId").value = data[i].StoreID;
                    byId("storeName").value = data[i].StoreName;
                    return;
                }
            }
        });
    }

    function searchItems(term) {
        requestJson("GET", getUrl("data-items-url") + "?term=" + encodeURIComponent(term || ""), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) { return; }

            var list = byId("itemsLookup");
            list.innerHTML = "";
            itemLookup = {};

            for (var i = 0; i < data.length; i++) {
                var label = data[i].Item_ID + " - " + data[i].ItemName;
                if (data[i].ItemCode) {
                    label += " - " + data[i].ItemCode;
                }

                itemLookup[label] = data[i];
                var option = document.createElement("option");
                option.value = label;
                list.appendChild(option);
            }
        });
    }

    function applyServiceItem(row, selected, suppressRecalc) {
        if (!selected) {
            clearRowData(row);
            return;
        }

        row.setAttribute("data-item-id", selected.Item_ID);
        row.setAttribute("data-store-id2", selected.StoreID2 || "");
        row.setAttribute("data-branch-id", selected.BranchId || "");
        row.setAttribute("data-qty-by-small-unit", selected.QtyBySmalltUnit || 1);
        row.setAttribute("data-show-price", selected.ShowPrice || selected.Price || 0);
        row.setAttribute("data-vatyo", selected.Vatyo || 0);
        row.setAttribute("data-discount-value", selected.DiscountValue || 0);
        row.setAttribute("data-total-discount-per-line", selected.TotalDiscountPerLine || 0);
        row.setAttribute("data-item-case", selected.ItemCase || 1);
        row.setAttribute("data-cost-price", selected.CostPrice || 0);
        row.setAttribute("data-saved-item-type", selected.SavedItemType || 0);

        row.querySelector(".item-name").value = selected.Item_ID + " - " + selected.ItemName;
        row.querySelector(".unit-id").value = selected.UnitId || 1;
        row.querySelector(".qty").value = selected.Quantity || 1;
        row.querySelector(".price").value = selected.Price || 0;
        row.querySelector(".vat").value = selected.Vat || 0;

        if (selected.BranchId && !byId("branchId").value && currentContext && currentContext.CanChangeDefaults === true) {
            byId("branchId").value = selected.BranchId;
        }

        if (!suppressRecalc) {
            calculateTotals();
            scheduleCommissionPreview();
        }
    }

    function applySelectedItem(input) {
        applyServiceItem(input.closest("tr"), itemLookup[input.value]);
    }

    function populateServiceSelect(select, data, emptyText) {
        select.innerHTML = "";
        if (!data || !data.length) {
            var emptyOption = document.createElement("option");
            emptyOption.value = "";
            emptyOption.text = emptyText || "لا توجد بيانات";
            select.appendChild(emptyOption);
            return null;
        }

        for (var i = 0; i < data.length; i++) {
            var option = document.createElement("option");
            option.value = data[i].Id;
            option.text = data[i].Name;
            select.appendChild(option);
        }

        return data[0].Id;
    }

    function loadPrimaryServiceItems(mode) {
        var requestedMode = mode || "cash-in";
        requestJson("GET", getUrl("data-primary-services-url") + "?serviceType=" + encodeURIComponent(requestedMode), null, function (status, data) {
            if (byId("transactionType").value !== requestedMode) {
                return;
            }

            if (status < 200 || status >= 300 || !data || !data.length) {
                byId("validationSummary").innerText = "تعذر تحميل نوع الشحن لهذا النوع";
                populateServiceSelect(byId("serviceItemId"), [], "تعذر التحميل");
                return;
            }

            var selectedId = populateServiceSelect(byId("serviceItemId"), data, "");
            loadSecondaryServiceItems(requestedMode, selectedId);
            loadDefaultServiceItem(requestedMode, false, selectedId);
        });
    }

    function loadSecondaryServiceItems(mode, itemId) {
        var select = byId("serviceItemId2");
        var hidden = mode === "card" || mode === "violations";
        var fields = document.querySelectorAll(".service-secondary-field");
        for (var i = 0; i < fields.length; i++) {
            fields[i].classList.toggle("is-hidden", hidden);
        }

        if (hidden || !itemId) {
            populateServiceSelect(select, [], hidden ? "" : "لا توجد بيانات");
            return;
        }

        requestJson("GET", getUrl("data-secondary-services-url") + "?serviceType=" + encodeURIComponent(mode) + "&itemId=" + encodeURIComponent(itemId), null, function (status, data) {
            if (byId("transactionType").value !== mode || byId("serviceItemId").value !== String(itemId)) {
                return;
            }

            if (status < 200 || status >= 300 || !data) {
                populateServiceSelect(select, [], "تعذر التحميل");
                return;
            }

            populateServiceSelect(select, data, "");
        });
    }

    function loadDefaultServiceItem(mode, append, itemId) {
        var requestedMode = mode || "cash-in";
        var requestId = ++serviceLoadSequence;
        var url = getUrl("data-default-service-url") + "?serviceType=" + encodeURIComponent(requestedMode);
        if (itemId) {
            url += "&itemId=" + encodeURIComponent(itemId);
        }

        requestJson("GET", url, null, function (status, data) {
            if (requestId !== serviceLoadSequence || byId("transactionType").value !== requestedMode) {
                return;
            }

            if (status < 200 || status >= 300 || !data || !data.length) {
                byId("validationSummary").innerText = "تعذر تحميل خدمة كيشني لهذا النوع";
                return;
            }

            var row = append ? createEmptyItemRow() : byId("itemsTable").querySelector("tbody tr");
            applyServiceItem(row, data[0]);
        });
    }

    function lookupCustomerByPhone(phone) {
        if (byId("transactionType").value !== "card") {
            return;
        }

        if (!phone) {
            return;
        }

        requestJson("GET", getUrl("data-customer-lookup-url") + "?phone=" + encodeURIComponent(phone), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                return;
            }

            applyKeshniCustomer(data);
        });
    }

    function openKycModal() {
        if (byId("transactionType").value !== "card") {
            byId("validationSummary").innerText = "تفعيل الكارت متاح فقط في كارت كيشني";
            return;
        }

        byId("cashCustomerPanel").classList.add("is-open");
        byId("cashCustomerPanel").setAttribute("aria-hidden", "false");
        setKycMessage("");
        byId("kycName").value = byId("kycName").value || byId("cashCustomerName").value;
        byId("kycPhoneNo2").value = byId("kycPhoneNo2").value || byId("cashCustomerPhone").value;
        byId("kycCardNo").value = byId("kycCardNo").value || byId("visaNumber").value;
        byId("kycNationalId").value = byId("kycNationalId").value || byId("cardNationalId").value;
        byId("kycSearchTerm").value = byId("cashCustomerPhone").value || byId("visaNumber").value || byId("cardNationalId").value;
        loadKycAttachments();
    }

    function closeKycModal() {
        var panel = byId("cashCustomerPanel");
        if (!panel) { return; }
        panel.classList.remove("is-open");
        panel.setAttribute("aria-hidden", "true");
    }

    function dateForInput(value) {
        if (!value) { return ""; }
        if (typeof value === "string" && value.indexOf("/Date(") === 0) {
            var ticks = parseInt(value.replace(/[^0-9-]/g, ""), 10);
            if (!isNaN(ticks)) { value = ticks; }
        }
        var parsed = new Date(value);
        if (isNaN(parsed.getTime())) { return ""; }
        return parsed.toISOString().substring(0, 10);
    }

    function extractBirthDateFromNationalId(value) {
        var id = (value || "").trim();
        if (!/^[0-9]{14}$/.test(id)) { return ""; }

        var century = id.charAt(0) === "3" ? "20" : "19";
        var year = century + id.substr(1, 2);
        var month = id.substr(3, 2);
        var day = id.substr(5, 2);
        var dateText = year + "-" + month + "-" + day;
        var parsed = new Date(dateText + "T00:00:00");
        return isNaN(parsed.getTime()) ? "" : dateText;
    }

    function addYears(dateText, years) {
        if (!dateText) { return ""; }
        var parsed = new Date(dateText + "T00:00:00");
        if (isNaN(parsed.getTime())) { return ""; }
        parsed.setFullYear(parsed.getFullYear() + years);
        return parsed.toISOString().substring(0, 10);
    }

    function applyKeshniCustomer(data) {
        if (!data) { return; }
        if (data.CustomerID) { byId("cashCustomerId").value = data.CustomerID; }
        if (data.CustomerName) { byId("cashCustomerName").value = data.CustomerName; }
        if (data.Phone) { byId("cashCustomerPhone").value = data.Phone; }
        if (data.Phone2) { byId("phone2").value = data.Phone2; }
        if (data.VisaNumber) {
            byId("visaNumber").value = data.VisaNumber;
            byId("paymentCardNo").value = data.VisaNumber;
            if (byId("cardTypeName")) {
                byId("cardTypeName").value = cardTypeNameFromCardNo(data.VisaNumber);
            }
        }
        if (data.Tet_NumPoket) {
            byId("cardNationalId").value = data.Tet_NumPoket;
            byId("kycNationalId").value = data.Tet_NumPoket;
        }
        if (data.BranchId && currentContext && currentContext.CanChangeDefaults === true) { byId("branchId").value = data.BranchId; }
        if (data.CustomerID) { loadKycAttachments(data.CustomerID); }
        byId("kycName").value = data.Name || data.CustomerName || "";
        byId("kycNameE").value = data.NameE || "";
        byId("kycArabicName0").value = data.ArabicName0 || "";
        byId("kycArabicName1").value = data.ArabicName1 || "";
        byId("kycArabicName2").value = data.ArabicName2 || "";
        byId("kycEnglishName0").value = data.EnglishName0 || "";
        byId("kycEnglishName1").value = data.EnglishName1 || "";
        byId("kycEnglishName2").value = data.EnglishName2 || "";
        byId("kycEnglishName5").value = data.EnglishName5 || "";
        byId("kycEnglishName6").value = data.EnglishName6 || "";
        byId("kycEnglishName7").value = data.EnglishName7 || "";
        byId("kycPhoneNo2").value = data.Phone2 || data.Phone || byId("cashCustomerPhone").value;
        byId("kycPhoneNo").value = data.Phone || "";
        byId("kycCardNo").value = data.VisaNumber || "";
        byId("kycCardSource").value = data.CardSource || "";
        byId("kycBirthDate").value = dateForInput(data.BirthDate);
        byId("kycCardDate").value = dateForInput(data.CardDate);
        byId("kycCardEndDate").value = dateForInput(data.CardEndDate);
        byId("kycAddress").value = data.Address || "";
        byId("kycMailAddress").value = data.MailAdress || "";
        byId("kycTel").value = data.Tel || "";
        byId("kycCard").value = data.CardSerial || "";
    }

    function renderKycAttachments(attachments) {
        var container = byId("kycAttachmentsList");
        if (!container) { return; }

        if (!attachments || !attachments.length) {
            container.innerHTML = "لا توجد مرفقات محفوظة";
            return;
        }

        var rows = ['<table class="kyc-attachments-table"><thead><tr><th>اسم الملف</th><th>تاريخ الرفع</th><th>نوع المستند</th><th>فتح</th></tr></thead><tbody>'];
        for (var i = 0; i < attachments.length; i++) {
            var item = attachments[i];
            var openUrl = getUrl("data-open-kyc-attachment-url") + "?id=" + encodeURIComponent(item.Id);
            rows.push("<tr><td>" + escapeHtml(item.FileName || "") + "</td><td>" +
                escapeHtml(dateForInput(item.ImageDate) || "") + "</td><td>" +
                escapeHtml(item.ImageTitle || item.Department || "") + "</td><td>" +
                '<a class="link-action" target="_blank" rel="noopener" href="' + openUrl + '">فتح</a></td></tr>');
        }
        rows.push("</tbody></table>");
        container.innerHTML = rows.join("");
    }

    function loadKycAttachments(customerId) {
        customerId = parseInt(customerId, 10) || parseInt(byId("cashCustomerId").value, 10) || 0;
        if (customerId <= 0) {
            renderKycAttachments([]);
            return;
        }

        requestJson("GET", getUrl("data-kyc-attachments-url") + "?customerId=" + encodeURIComponent(customerId), null, function (status, data) {
            if (status >= 200 && status < 300 && data && data.success) {
                renderKycAttachments(data.attachments || []);
                return;
            }

            byId("kycAttachmentsList").innerHTML = "تعذر تحميل المرفقات";
        });
    }

    function searchKeshniCardCustomers() {
        var term = byId("kycSearchTerm").value.trim() || byId("cashCustomerPhone").value.trim() || byId("visaNumber").value.trim() || byId("cardNationalId").value.trim();
        if (!term) {
            byId("kycSearchResults").innerHTML = "أدخل رقم موبايل أو كارت أو رقم قومي للبحث";
            return;
        }

        requestJson("GET", getUrl("data-customer-search-url") + "?term=" + encodeURIComponent(term), null, function (status, data) {
            if (status < 200 || status >= 300 || !data || !data.length) {
                byId("kycSearchResults").innerHTML = "لا توجد نتائج";
                return;
            }

            var html = [];
            for (var i = 0; i < data.length; i++) {
                html.push('<button type="button" class="kyc-result-item" data-index="' + i + '"><strong>' +
                    escapeHtml(data[i].CustomerName || data[i].Name || "") + '</strong><span>' +
                    escapeHtml(data[i].Phone || "") + ' | ' + escapeHtml(data[i].VisaNumber || "") + ' | ' +
                    escapeHtml(data[i].Tet_NumPoket || "") + '</span></button>');
            }
            byId("kycSearchResults").innerHTML = html.join("");
            byId("kycSearchResults")._items = data;
        });
    }

    function scheduleCommissionPreview() {
        window.clearTimeout(commissionTimer);
        commissionTimer = window.setTimeout(calculateCommissionPreview, 250);
    }

    function calculateCommissionPreview() {
        if (reviewMode) {
            return;
        }

        var firstRow = getFirstSelectedItemRow();
        if (!firstRow) {
            return;
        }

        var request = {
            ServiceType: byId("transactionType").value,
            ItemID: parseInt(firstRow.getAttribute("data-item-id"), 10) || null,
            RechargeValue: byId("transactionType").value === "card" ? 0 : (byId("transactionType").value === "violations" ? numberValue("violationValue") : numberValue("rechargeValue")),
            Vatyo: parseFloat(firstRow.getAttribute("data-vatyo")) || 14,
            IsWallet: byId("isWallet").value === "true",
            HaveGuarantee: byId("haveGuarantee").value === "true"
        };

        requestJson("POST", getUrl("data-commission-url"), request, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                return;
            }

            var commission = parseFloat(data.CommissionValue) || 0;
            var vatValue = parseFloat(data.VatValue) || 0;
            var vatPercent = parseFloat(data.VatPercent) || 0;
            var totalFees = parseFloat(data.TotalFees) || (commission + vatValue);
            var isCard = byId("transactionType").value === "card";
            var totalValue = isCard ? totalFees : (parseFloat(data.TotalValue) || (numberValue("rechargeValue") + totalFees));

            byId("commissionValue").value = isCard ? "0.00" : commission.toFixed(2);
            byId("vatValue").value = vatValue.toFixed(2);
            byId("totalFees").value = totalFees.toFixed(2);
            byId("netValue").value = totalValue.toFixed(2);
            byId("payedValue").value = totalValue.toFixed(2);
            byId("remainValue").value = "0.00";

            firstRow.querySelector(".price").value = commission.toFixed(2);
            firstRow.querySelector(".vat").value = vatValue.toFixed(2);
            firstRow.querySelector(".line-total").value = totalFees.toFixed(2);
            firstRow.querySelector(".qty").value = "1";
            firstRow.setAttribute("data-show-price", commission);
            firstRow.setAttribute("data-vatyo", vatPercent);
            calculateTotals();
        });
    }

    function saveCashCustomer() {
        if (kycSaveInProgress) {
            return;
        }

        if (byId("transactionType").value !== "card") {
            var wrongModeMessage = "بيانات KYC مطلوبة فقط في كارت كيشني";
            byId("validationSummary").innerText = wrongModeMessage;
            setKycMessage(wrongModeMessage, true);
            return;
        }

        if (!(byId("kycPhoneNo2").value || byId("cashCustomerPhone").value).trim()) {
            var phoneMessage = "من فضلك أدخل رقم التليفون";
            byId("validationSummary").innerText = phoneMessage;
            setKycMessage(phoneMessage, true);
            return;
        }
        if (!(byId("kycName").value || byId("cashCustomerName").value).trim()) {
            var nameMessage = "من فضلك أدخل اسم العميل";
            byId("validationSummary").innerText = nameMessage;
            setKycMessage(nameMessage, true);
            return;
        }
        if (!byId("kycNationalId").value.trim()) {
            var nationalIdMessage = "من فضلك أدخل الرقم القومي";
            byId("validationSummary").innerText = nationalIdMessage;
            setKycMessage(nationalIdMessage, true);
            return;
        }
        if (!byId("kycCardNo").value.trim()) {
            var cardMessage = "من فضلك أدخل رقم الكارت";
            byId("validationSummary").innerText = cardMessage;
            setKycMessage(cardMessage, true);
            return;
        }

        var formData = new FormData();
        formData.append("CustomerID", parseInt(byId("cashCustomerId").value, 10) || "");
        formData.append("Name", byId("kycName").value || byId("cashCustomerName").value);
        formData.append("NameE", byId("kycNameE").value);
        formData.append("ArabicName0", byId("kycArabicName0").value);
        formData.append("ArabicName1", byId("kycArabicName1").value);
        formData.append("ArabicName2", byId("kycArabicName2").value);
        formData.append("EnglishName0", byId("kycEnglishName0").value);
        formData.append("EnglishName1", byId("kycEnglishName1").value);
        formData.append("EnglishName2", byId("kycEnglishName2").value);
        formData.append("EnglishName5", byId("kycEnglishName5").value);
        formData.append("EnglishName6", byId("kycEnglishName6").value);
        formData.append("EnglishName7", byId("kycEnglishName7").value);
        formData.append("PhoneNo2", byId("kycPhoneNo2").value || byId("cashCustomerPhone").value);
        formData.append("PhoneNo", byId("kycPhoneNo").value || byId("phone2").value);
        formData.append("CardNo", byId("kycCardNo").value || byId("visaNumber").value);
        formData.append("CardId", byId("kycCardNo").value || byId("visaNumber").value);
        formData.append("CardSource", byId("kycCardSource").value);
        formData.append("Tet_NumPoket", byId("kycNationalId").value);
        formData.append("BirthDate", byId("kycBirthDate").value);
        formData.append("CardDate", byId("kycCardDate").value);
        formData.append("CardEndDate", byId("kycCardEndDate").value);
        formData.append("Address", byId("kycAddress").value);
        formData.append("MailAdress", byId("kycMailAddress").value);
        formData.append("Tel", byId("kycTel").value);
        formData.append("Card", byId("kycCard").value);
        formData.append("BranchId", parseInt(byId("branchId").value, 10) || "");

        var files = byId("kycAttachments").files;
        for (var i = 0; i < files.length; i++) {
            formData.append("attachments", files[i]);
        }

        var saveButton = byId("saveCashCustomerBtn");
        var oldButtonText = saveButton.innerText;
        kycSaveInProgress = true;
        saveButton.disabled = true;
        saveButton.innerText = "جاري الحفظ...";
        byId("validationSummary").innerText = "";
        byId("saveResult").innerText = "جاري حفظ بيانات الكارت...";
        setKycMessage("جاري حفظ بيانات الكارت...");

        requestFormData(getUrl("data-save-keshni-card-url"), formData, function (status, data) {
            kycSaveInProgress = false;
            saveButton.disabled = !currentContext || currentContext.CanOpenCashCustomer !== true;
            saveButton.innerText = oldButtonText;

            if (status >= 200 && status < 300 && data && data.success && data.customer) {
                applyKeshniCustomer(data.customer);
                renderKycAttachments(data.attachments || []);
                byId("saveResult").innerText = data.message || "تم حفظ بيانات العميل وتفعيل الكارت";
                byId("validationSummary").innerText = "";
                setKycMessage(data.message || "تم حفظ بيانات العميل وتفعيل الكارت");
                byId("kycAttachments").value = "";
                calculateCommissionPreview();
                closeKycModal();
                return;
            }

            var message = data && data.message ? data.message : "تعذر حفظ بيانات العميل";
            if (data && data.validationErrors) {
                message += "\n" + Object.keys(data.validationErrors).map(function (key) { return data.validationErrors[key]; }).join("\n");
            }
            if (data && data.technicalMessage) {
                message += "\nالتفاصيل الفنية: " + data.technicalMessage;
            }

            byId("validationSummary").innerText = message;
            byId("saveResult").innerText = "";
            setKycMessage(message, true);
        });
    }

    function clearMessages() {
        byId("validationSummary").innerHTML = "";
        byId("saveResult").innerHTML = "";
        setKycMessage("");
    }

    function clearFormForNewTransaction() {
        byId("posForm").reset();
        byId("printBtn").disabled = true;
        reviewMode = false;
        clearMessages();

        resetServiceRows();

        byId("cashCustomerId").value = "";
        byId("transactionType").value = "";
        byId("isCashOut").value = "false";
        byId("isPOS").value = "false";
        byId("otherItems").value = "false";
        byId("isRecharg").value = "false";
        byId("isWallet").value = "false";
        byId("haveGuarantee").value = "false";
        byId("tetNumPoket").value = "";
        byId("violationWalletNo").value = "";
        byId("violationValue").value = "0";
        byId("visaNumber").value = "";
        byId("paymentCardNo").value = "";
        byId("cardNationalId").value = "";
        byId("cardTypeName").value = "";
        byId("storeId").value = "";
        byId("storeName").value = "";
        byId("commissionValue").value = "0";
        byId("vatValue").value = "0";
        byId("totalFees").value = "0";
        byId("netValue").value = "0";
        byId("remainValue").value = "0";
        byId("payedValue").value = "0";
        byId("rechargeValue").value = "0";
        calculateTotals();
    }

    function reloadContextAndReset(messageHtml) {
        clearFormForNewTransaction();
        requestJson("GET", getUrl("data-context-url"), null, function (status, data) {
            if (status >= 200 && status < 300 && data) {
                setCurrentContext(data);
            }

            loadContextControls();
            setMode("cash-in");
            applyPermissions();
            calculateTotals();
            if (messageHtml) {
                byId("saveResult").innerHTML = messageHtml;
            }
            enablePrintIfAllowed();
        });
    }

    function applyPermissions() {
        byId("saveBtn").disabled = reviewMode || saveDisabledByContext(byId("transactionType").value);
        enablePrintIfAllowed();
        byId("returnLaterBtn").disabled = !currentContext || currentContext.CanReturn !== true || byId("transactionType").value === "violations";
        byId("saveCashCustomerBtn").disabled = !currentContext || currentContext.CanOpenCashCustomer !== true;
        byId("boxId").disabled = true;
        byId("branchId").disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        byId("paymentType").disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        byId("storeName").readOnly = true;
        byId("empName").readOnly = true;
        byId("bankName").readOnly = true;
    }

    document.addEventListener("click", function (event) {
        if (event.target.classList.contains("pos-type-btn")) { setMode(event.target.getAttribute("data-mode")); }
        if (event.target.id === "addItemBtn") { addItemRow(); }
        if (event.target.id === "printBtn") {
            var transactionId = savedTransactionId();
            openPrintForTransaction(transactionId);
        }
        var invoiceButton = event.target.closest ? event.target.closest(".today-invoice-item") : null;
        if (invoiceButton) {
            var invoiceIndex = parseInt(invoiceButton.getAttribute("data-index"), 10);
            showTodayInvoiceSummary(invoiceIndex);
            loadInvoiceForReview(invoiceButton.getAttribute("data-transaction-id"));
        }
        if (event.target.classList.contains("remove-row")) {
            var rows = byId("itemsTable").querySelectorAll("tbody tr");
            if (rows.length > 1) {
                var row = event.target.closest("tr");
                row.parentNode.removeChild(row);
                calculateTotals();
            }
        }
        if (event.target.id === "newBtn") { reloadContextAndReset(); }
        if (event.target.id === "searchCustomerBtn" || event.target.id === "activateCardBtn") { openKycModal(); }
        if (event.target.id === "closeKycModalBtn" || event.target.id === "kycModalBackdrop") { closeKycModal(); }
        if (event.target.id === "kycSearchBtn") { searchKeshniCardCustomers(); }
        if (event.target.id === "showKycAttachmentsBtn") { loadKycAttachments(); }
        var kycResultButton = event.target.closest ? event.target.closest(".kyc-result-item") : null;
        if (kycResultButton) {
            var kycItems = byId("kycSearchResults")._items || [];
            applyKeshniCustomer(kycItems[parseInt(kycResultButton.getAttribute("data-index"), 10)]);
        }
        if (event.target.id === "returnLaterBtn") { byId("saveResult").innerText = "هذه شاشة تجريبية فقط"; }
        if (event.target.id === "saveCashCustomerBtn") { saveCashCustomer(); }
        if (event.target.id === "refreshBalancesBtn") { loadEmployeeBalances(); }
    });

    document.addEventListener("input", function (event) {
        if (event.target.matches(".qty, .price, .vat, #rechargeValue, #commissionValue, #payedValue")) {
            calculateTotals();
        }

        if (event.target.id === "rechargeValue") {
            scheduleCommissionPreview();
        }

        if (event.target.id === "violationValue") {
            scheduleCommissionPreview();
        }

        if (event.target.id === "serviceItemId") {
            var mode = byId("transactionType").value;
            var itemId = parseInt(event.target.value, 10) || null;
            loadSecondaryServiceItems(mode, itemId);
            loadDefaultServiceItem(mode, false, itemId);
        }

        if (event.target.name === "violationPayType") {
            setMode(byId("transactionType").value);
        }

        if (event.target.id === "violationWalletNo") {
            byId("tetNumPoket").value = event.target.value;
        }

        if (event.target.id === "visaNumber" && byId("transactionType").value === "card") {
            byId("paymentCardNo").value = event.target.value;
            byId("kycCardNo").value = event.target.value;
            var cardItemId = cardServiceItemIdFromCardNo(event.target.value);
            var cardTypeName = cardTypeNameFromCardNo(event.target.value);
            if (byId("cardTypeName")) {
                byId("cardTypeName").value = cardTypeName;
            }
            setSelectSingleOption("serviceItemId", cardItemId, cardItemId === 1 ? "كارت ميزة كيشني" : "كارت البنك الأهلي");
            loadDefaultServiceItem("card", false, cardItemId);
        }

        if (event.target.id === "cardNationalId" && byId("transactionType").value === "card") {
            byId("kycNationalId").value = event.target.value;
            var birthDate = extractBirthDateFromNationalId(event.target.value);
            if (birthDate) {
                byId("kycBirthDate").value = birthDate;
            }
        }

        if (event.target.id === "kycNationalId") {
            var modalBirthDate = extractBirthDateFromNationalId(event.target.value);
            if (modalBirthDate) {
                byId("kycBirthDate").value = modalBirthDate;
                byId("cardNationalId").value = event.target.value;
            }
        }

        if (event.target.id === "cashCustomerPhone") {
            if (byId("transactionType").value !== "card") { return; }
            byId("kycPhoneNo2").value = event.target.value;
            window.clearTimeout(customerLookupTimer);
            customerLookupTimer = window.setTimeout(function () {
                var phone = event.target.value.trim();
                if (isValidEgyptianMobile(phone)) {
                    lookupCustomerByPhone(phone);
                }
            }, 350);
        }

        if (event.target.id === "kycCardDate") {
            var expiryDate = addYears(event.target.value, 10);
            if (expiryDate) {
                byId("kycCardEndDate").value = expiryDate;
            }
        }

        if (event.target.classList.contains("item-name")) {
            var input = event.target;
            window.clearTimeout(itemSearchTimer);
            itemSearchTimer = window.setTimeout(function () {
                searchItems(input.value);
            }, 250);
            applySelectedItem(input);
        }

        if (event.target.id === "todayInvoiceSearch") {
            window.clearTimeout(todayInvoicesTimer);
            todayInvoicesTimer = window.setTimeout(function () {
                loadTodayInvoices(event.target.value.trim());
            }, 250);
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.id === "branchId") {
            if (currentContext && currentContext.CanChangeDefaults === true) {
                loadStoresForBranch(event.target.value);
            } else {
                applyDefaultContextControls();
            }
        }
        if (event.target.id === "paymentType") {
            byId("bankName").value = selectedOptionAttribute(event.target, "data-bank-name") || "";
        }
        if (event.target.id === "serviceItemId") {
            var mode2 = byId("transactionType").value;
            var itemId2 = parseInt(event.target.value, 10) || null;
            loadSecondaryServiceItems(mode2, itemId2);
            loadDefaultServiceItem(mode2, false, itemId2);
        }
        if (event.target.name === "violationPayType") {
            setMode(byId("transactionType").value);
        }
    });

    document.addEventListener("change", function (event) {
        if (event.target.classList.contains("item-name")) {
            applySelectedItem(event.target);
        }
    });

    byId("posForm").addEventListener("submit", saveTransaction);
    setInitialContextFromPage();
    loadContextControls();
    searchItems("");
    setMode("cash-in");
    applyPermissions();
    loadEmployeeBalances();
    loadTodayInvoices();
    calculateTotals();
})();
