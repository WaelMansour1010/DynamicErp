(function () {
    "use strict";

    var modes = {
        "cash-in": { label: "كاش إن", operationLabel: "عملية شحن كيشني", isCashOut: false, isPOS: false, colorClass: "pos-mode-cash-in" },
        "cash-out": { label: "كاش أوت", operationLabel: "عملية كاش أوت", isCashOut: true, isPOS: false, colorClass: "pos-mode-cash-out" },
        "card": { label: "كارت كيشني", operationLabel: "عملية إصدار كارت كيشني", isCashOut: false, isPOS: true, colorClass: "pos-mode-card" },
        "violations": { label: "مخالفات", operationLabel: "عملية سداد مخالفات", isCashOut: false, isPOS: false, colorClass: "pos-mode-violations" }
    };

    var itemLookup = {};
    var itemSearchTimer = null;
    var customerLookupTimer = null;
    var kycUnusedLookupTimer = null;
    var commissionTimer = null;
    var amountCommitTimer = null;
    var todayInvoicesTimer = null;
    var todayInvoicesCache = [];
    var salesIndexCache = [];
    var salesBranchCache = null;
    var salesBranchLoading = false;
    var posEntryInitialized = false;
    var currentContext = null;
    var serviceLoadSequence = 0;
    var primaryServiceCache = {};
    var secondaryServiceCache = {};
    var commissionCache = {};
    var commissionsReady = false;
    var commissionCalculationPending = false;
    var commissionPreviewSequence = 0;
    var commissionPreviewXhr = null;
    var commissionPreviewUseOverlay = true;
    var amountEnterAdvancePending = false;
    var lastCommissionKey = "";
    var lastCashOutMachineWithdrawalAmount = 0;
    var lastCashOutBankMachineCommission = 0;
    var contextControlsLoaded = false;
    var contextControlsLoading = false;
    var reviewMode = false;
    var lastSavedTransactionId = null;
    var loadedInvoiceCreatedUserId = null;
    var loadedInvoiceBranchId = null;
    var loadedInvoiceStoreId = null;
    var loadedInvoiceBoxId = null;
    var loadedInvoiceEmpId = null;
    var kycSaveInProgress = false;
    var kycCardAvailabilityTimer = null;
    var kycCardAvailabilityXhr = null;
    var kycNameSyncing = false;
    var kycArabicNameTimer = null;
    var kycEnglishNameTimer = null;
    var kycArabicNameSource = "";
    var kycEnglishNameSource = "";
    var pendingDuplicateKycCustomer = null;
    var uxLoadingCount = 0;
    var uxSaving = false;
    var saveRequestInFlight = false;
    var uxLastActionAt = 0;
    var uxCurrentStep = "service";
    var uxDebounceMs = 200;
    var amountCommitDelayMs = 400;
    var posDebugEnabled = false;
    var amountWarningLimit = 20000;
    var maxRechargeValue = 100000;
    var pendingSaveConfirmation = false;
    var loadedInvoiceIsCancelled = false;

    function byId(id) { return document.getElementById(id); }
    try {
        posDebugEnabled = false;
    } catch (ignoreDebugFlag) {
        posDebugEnabled = false;
    }
    function posDebugLog(label, payload) {
        return;
    }
    function normalizeDigits(value) {
        return String(value === null || value === undefined ? "" : value)
            .replace(/[٠-٩]/g, function (d) { return "٠١٢٣٤٥٦٧٨٩".indexOf(d); })
            .replace(/[۰-۹]/g, function (d) { return "۰۱۲۳۴۵۶۷۸۹".indexOf(d); });
    }
    function parseMoney(value) {
        var text = normalizeDigits(value).replace(/,/g, "").replace(/\s/g, "").trim();
        if (!text) { return 0; }
        var parsed = parseFloat(text);
        return isNaN(parsed) ? 0 : parsed;
    }
    function numberValue(id) { return parseMoney(byId(id).value); }
    function numberFromInput(input) { return parseMoney(input.value); }
    function selectedText(select) { return select.selectedIndex >= 0 ? select.options[select.selectedIndex].text : ""; }
    function savedTransactionId() { var value = parseInt(lastSavedTransactionId, 10); return isNaN(value) ? 0 : value; }
    function localIsoDate(value) {
        value = value || new Date();
        var month = String(value.getMonth() + 1);
        var day = String(value.getDate());
        return value.getFullYear() + "-" + (month.length === 1 ? "0" + month : month) + "-" + (day.length === 1 ? "0" + day : day);
    }
    function dateInputValue(value) {
        if (!value) { return localIsoDate(); }
        if (Object.prototype.toString.call(value) === "[object Date]") {
            return isNaN(value.getTime()) ? localIsoDate() : localIsoDate(value);
        }

        var text = String(value).trim();
        var jsonDate = text.match(/\/Date\((-?\d+)\)\//);
        if (jsonDate) {
            var parsedJsonDate = new Date(parseInt(jsonDate[1], 10));
            return isNaN(parsedJsonDate.getTime()) ? localIsoDate() : localIsoDate(parsedJsonDate);
        }

        if (/^\d{4}-\d{2}-\d{2}/.test(text)) {
            return text.substring(0, 10);
        }

        var parts = text.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})$/);
        if (parts) {
            return parts[3] + "-" + ("0" + parts[2]).slice(-2) + "-" + ("0" + parts[1]).slice(-2);
        }

        var parsed = new Date(text);
        return isNaN(parsed.getTime()) ? localIsoDate() : localIsoDate(parsed);
    }
    function parseDisplayDate(value) {
        if (!value) { return null; }
        if (Object.prototype.toString.call(value) === "[object Date]") {
            return isNaN(value.getTime()) ? null : value;
        }

        var text = String(value).trim();
        var jsonDate = text.match(/\/?Date\((-?\d+)(?:[+-]\d+)?\)\/?/);
        if (jsonDate) {
            var parsedJsonDate = new Date(parseInt(jsonDate[1], 10));
            return isNaN(parsedJsonDate.getTime()) ? null : parsedJsonDate;
        }

        var dateOnly = text.match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (dateOnly) {
            return new Date(parseInt(dateOnly[1], 10), parseInt(dateOnly[2], 10) - 1, parseInt(dateOnly[3], 10));
        }

        var dayFirst = text.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})/);
        if (dayFirst) {
            return new Date(parseInt(dayFirst[3], 10), parseInt(dayFirst[2], 10) - 1, parseInt(dayFirst[1], 10));
        }

        var parsed = new Date(text);
        return isNaN(parsed.getTime()) ? null : parsed;
    }
    function dateDisplayValue(value) {
        var parsed = parseDisplayDate(value);
        if (!parsed) { return ""; }
        return ("0" + parsed.getDate()).slice(-2) + "/" + ("0" + (parsed.getMonth() + 1)).slice(-2) + "/" + parsed.getFullYear();
    }
    function currentTransactionDate() {
        var dateInput = byId("transactionDate");
        return dateInput && dateInput.value ? dateInput.value : localIsoDate();
    }
    function enablePrintIfAllowed() { byId("printBtn").disabled = !(savedTransactionId() > 0 && currentContext && currentContext.CanPrint === true); }
    function enableDeleteIfAllowed() {
        var button = byId("deleteCurrentInvoiceBtn");
        if (!button) { return; }
        button.disabled = !(savedTransactionId() > 0 && currentContext && currentContext.CanAdminDeleteInvoice === true);
    }
    function isCurrentInvoiceSameDay() {
        var value = currentTransactionDate();
        return !!value && value === localIsoDate();
    }
    function enableCancelIfAllowed() {
        var button = byId("cancelInvoiceBtn");
        if (!button) { return; }
        var mode = byId("transactionType").value;
        var allowedMode = mode === "cash-in" || mode === "cash-out";
        var canCancel = savedTransactionId() > 0
            && currentContext
            && currentContext.CanCancelInvoice === true
            && allowedMode
            && !loadedInvoiceIsCancelled
            && isCurrentInvoiceSameDay();
        button.style.display = canCancel ? "" : "none";
        button.disabled = !canCancel;
    }
    function savedKycCustomerId() {
        var element = byId("cashCustomerId");
        if (!element) { return 0; }
        var value = parseInt(element.value, 10);
        return isNaN(value) ? 0 : value;
    }
    function enablePrintAcknowledgmentIfAllowed() {
        var hasCustomer = savedKycCustomerId() > 0 && currentContext;
        var ackButton = byId("printAcknowledgmentBtn");
        if (ackButton) { ackButton.disabled = !(hasCustomer && currentContext.CanPrintKycAcknowledgment === true); }
        var cardButton = byId("printCardBtn");
        if (cardButton) { cardButton.disabled = !(hasCustomer && currentContext.CanPrintKycCard === true); }
    }
    function openPrintCardForCustomer(customerId) {
        customerId = parseInt(customerId, 10) || 0;
        if (customerId <= 0) {
            setKycMessage("لا توجد بيانات كارت محفوظة لطباعة الكارت", true);
            return false;
        }

        var url = getUrl("data-kyc-print-card-url");
        if (!url) { return false; }
        window.open(url.replace(/\/$/, "") + "/" + encodeURIComponent(customerId), "_blank");
        return true;
    }
    function openPrintAcknowledgmentForCustomer(customerId) {
        customerId = parseInt(customerId, 10) || 0;
        if (customerId <= 0) {
            setKycMessage("لا توجد بيانات كارت محفوظة لطباعة الإقرار", true);
            return false;
        }

        var url = getUrl("data-kyc-print-acknowledgment-url");
        if (!url) { return false; }
        window.open(url.replace(/\/$/, "") + "/" + encodeURIComponent(customerId), "_blank");
        return true;
    }
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
    function isValidEgyptianMobile(value) { return /^01[0-9]{9}$/.test(amountDigits(value || "")); }

    function phoneValidationMessage(value) {
        var digits = amountDigits(value || "");
        if (!digits) { return ""; }
        if (digits.length < 11) { return "رقم الهاتف يجب أن يتكون من 11 رقم"; }
        if (digits.substring(0, 2) !== "01") { return "رقم الهاتف المصري يجب أن يبدأ بـ 01"; }
        return "";
    }

    function updatePhoneHelper(input) {
        if (!input) { return; }
        var helper = document.querySelector("[data-phone-helper-for='" + input.id + "']");
        if (!helper) { return; }
        helper.innerText = amountDigits(input.value).length + " / 11";
    }

    function setPhoneValidState(input, isValid) {
        if (!input) { return; }
        input.classList.toggle("pos-field-valid", isValid);
        var wrapper = fieldWrapper(input);
        if (wrapper) { wrapper.classList.toggle("pos-field-valid-wrap", isValid); }
    }

    function normalizePhoneInput(input, showBlurErrors) {
        if (!input) { return true; }
        var digits = amountDigits(input.value).slice(0, 11);
        if (input.value !== digits) { input.value = digits; }
        updatePhoneHelper(input);
        setPhoneValidState(input, false);

        if (!digits) {
            clearFieldInvalid("#" + input.id);
            return false;
        }

        if (digits.length >= 2 && digits.substring(0, 2) !== "01") {
            markFieldInvalid("#" + input.id, "رقم الهاتف المصري يجب أن يبدأ بـ 01");
            return false;
        }

        if (digits.length < 11) {
            if (showBlurErrors) {
                markFieldInvalid("#" + input.id, "رقم الهاتف يجب أن يتكون من 11 رقم");
            } else {
                clearFieldInvalid("#" + input.id);
            }
            return false;
        }

        clearFieldInvalid("#" + input.id);
        setPhoneValidState(input, true);
        return true;
    }

    function refreshPhoneInputs() {
        var inputs = document.querySelectorAll(".pos-eg-phone");
        for (var i = 0; i < inputs.length; i++) {
            normalizePhoneInput(inputs[i], false);
        }
    }
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

    function queryValue(name) {
        var search = window.location.search || "";
        if (!search || search.length < 2) { return ""; }
        var parts = search.substring(1).split("&");
        for (var i = 0; i < parts.length; i++) {
            var pair = parts[i].split("=");
            if (decodeURIComponent(pair[0] || "") === name) {
                return decodeURIComponent((pair[1] || "").replace(/\+/g, " "));
            }
        }
        return "";
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
            CanViewJournalEntry: readBool(data.CanViewJournalEntry),
            CanViewReports: readBool(data.CanViewReports),
            CanPrintKycAcknowledgment: readBool(data.CanPrintKycAcknowledgment),
            CanPrintKycCard: readBool(data.CanPrintKycCard),
            CanEditKyc: readBool(data.CanEditKyc),
            CanTeller: readBool(data.CanTeller),
            CanEditInvoice: readBool(data.CanEditInvoice),
            CanCancelInvoice: readBool(data.CanCancelInvoice),
            CanAdminDeleteInvoice: readBool(data.CanAdminDeleteInvoice) || readBool(data.IsFullAccess),
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
            CanViewJournalEntry: getPageValue("data-can-view-journal"),
            CanPrintKycAcknowledgment: getPageValue("data-can-print-kyc-acknowledgment"),
            CanPrintKycCard: getPageValue("data-can-print-kyc-card"),
            CanEditKyc: getPageValue("data-can-edit-kyc"),
            CanTeller: getPageValue("data-can-teller"),
            CanEditInvoice: getPageValue("data-can-edit-invoice"),
            CanCancelInvoice: getPageValue("data-can-cancel-invoice"),
            CanAdminDeleteInvoice: getPageValue("data-can-admin-delete-invoice"),
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
        xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
        xhr.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) { return; }

            var data = null;
            try {
                data = xhr.responseText ? JSON.parse(xhr.responseText) : null;
            } catch (ignore) {
                data = nonJsonResponse("تعذر قراءة رد السيرفر", xhr.status, xhr.responseText);
            }

            callback(xhr.status, data);
        };
        xhr.onerror = function () {
            callback(0, { success: false, message: "تعذر الاتصال بالسيرفر", technicalMessage: "Network error" });
        };
        xhr.send(body ? JSON.stringify(body) : null);
        return xhr;
    }

    function requestJsonWithLoading(method, url, body, callback, message) {
        uxBeginLoading(message || "جاري تحميل البيانات...");
        return requestJson(method, url, body, function (status, data) {
            uxEndLoading();
            callback(status, data);
        });
    }

    function nonJsonResponse(defaultMessage, status, responseText) {
        responseText = responseText || "";
        var lower = responseText.toLowerCase();
        var isHtml = lower.indexOf("<html") >= 0 || lower.indexOf("<!doctype") >= 0;
        var isLogin = isHtml && (lower.indexOf("login") >= 0 || lower.indexOf("pages-login") >= 0 || lower.indexOf("تسجيل") >= 0);
        var message = defaultMessage;
        var details = "HTTP " + status;

        if (isLogin) {
            message = "انتهت جلسة نقطة البيع أو تم تحويل الطلب إلى صفحة الدخول. برجاء تسجيل الدخول مرة أخرى.";
            details += " - Login page returned instead of JSON";
        } else if (isHtml) {
            message = "رجع السيرفر صفحة HTML بدل رد JSON. راجع صلاحية الجلسة أو سجل أخطاء السيرفر.";
            details += " - HTML response returned instead of JSON";
        } else if (responseText) {
            details += " - " + responseText.substring(0, 500);
        } else {
            details += " - Empty response";
        }

        if (window.console && console.error) {
            console.error("POS request returned a non-JSON response", {
                status: status,
                responseText: responseText
            });
        }

        return {
            success: false,
            message: message,
            technicalMessage: details
        };
    }

    function requestFormData(url, formData, callback) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", url, true);
        xhr.setRequestHeader("X-Requested-With", "XMLHttpRequest");
        xhr.timeout = 60000;
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) { return; }

            var data = null;
            try {
                if (xhr.responseText) {
                    data = JSON.parse(xhr.responseText);
                } else {
                    data = {
                        success: false,
                        message: "لم يرجع السيرفر رداً أثناء حفظ بيانات الكارت",
                        technicalMessage: "HTTP " + xhr.status + " - Empty response",
                        technicalDetails: "HTTP " + xhr.status + " - Empty response"
                    };
                }
            } catch (ignore) {
                var responseText = xhr.responseText || "";
                var message = "تعذر قراءة رد السيرفر أثناء حفظ بيانات الكارت";
                if (xhr.status === 400) {
                    message = "رفض السيرفر طلب حفظ بيانات الكارت. راجع صيغة الحقول وحجم المرفقات ثم حاول مرة أخرى.";
                }
                data = nonJsonResponse(message, xhr.status, responseText);
                data.technicalDetails = data.technicalMessage;
            }

            callback(xhr.status, data);
        };
        xhr.onerror = function () {
            callback(0, {
                success: false,
                message: "تعذر الاتصال بالسيرفر أثناء حفظ بيانات الكارت",
                technicalMessage: "Network error",
                technicalDetails: "Network error"
            });
        };
        xhr.ontimeout = function () {
            callback(0, {
                success: false,
                message: "انتهت مهلة حفظ بيانات الكارت. راجع الاتصال أو قاعدة البيانات ثم حاول مرة أخرى.",
                technicalMessage: "KYC save request timed out after 60 seconds.",
                technicalDetails: "KYC save request timed out after 60 seconds."
            });
        };
        try {
            xhr.send(formData);
        } catch (ex) {
            callback(0, {
                success: false,
                message: "تعذر إرسال بيانات الكارت إلى السيرفر",
                technicalMessage: ex && ex.message ? ex.message : String(ex),
                technicalDetails: ex && ex.message ? ex.message : String(ex)
            });
        }
    }

    function base64Utf8(value) {
        try {
            return btoa(unescape(encodeURIComponent(value || "")));
        } catch (ignore) {
            return "";
        }
    }

    function safeUploadFileName(fileName, index) {
        var extension = "";
        var match = (fileName || "").match(/(\.[A-Za-z0-9]{1,10})$/);
        if (match) {
            extension = match[1].toLowerCase();
        }
        return "kyc_attachment_" + (index + 1) + extension;
    }

    function saveDisabledByContext(mode) {
        return (currentContext && currentContext.CanAdd === false) || (!currentContext && getPageValue("data-can-save") === "false");
    }

    function uxBeginLoading(message) {
        uxLoadingCount++;
        uxSetOverlay(true, message || "جاري تحميل البيانات...");
        uxApplyFlow();
    }

    function uxEndLoading() {
        uxLoadingCount = Math.max(uxLoadingCount - 1, 0);
        if (uxLoadingCount === 0 && !uxSaving) {
            uxSetOverlay(false);
        }
        uxApplyFlow();
    }

    function uxSetOverlay(visible, message) {
        var page = byId("posPage");
        var overlay = byId("posLoadingOverlay");
        var text = byId("posLoadingText");
        if (!page || !overlay) { return; }
        if (text && message) { text.innerText = message; }
        page.classList.toggle("is-loading", visible && !uxSaving);
        page.classList.toggle("is-saving", uxSaving);
        overlay.setAttribute("aria-hidden", visible || uxSaving ? "false" : "true");
    }

    function uxIsBusy() {
        return uxLoadingCount > 0 || uxSaving || saveRequestInFlight;
    }

    function uxDebounced() {
        var now = Date.now ? Date.now() : new Date().getTime();
        if (now - uxLastActionAt < uxDebounceMs) { return true; }
        uxLastActionAt = now;
        return false;
    }

    function uxShowGuide(message) {
        var text = message || "أكمل البيانات أولاً";
        var validation = byId("validationSummary");
        var saveButton = byId("saveBtn");
        if (validation) {
            validation.innerText = text;
            validation.classList.remove("is-error");
        }
        if (saveButton) {
            saveButton.title = text;
        }
    }

    var validationFieldSelectors = {
        TransactionType: "#transactionType",
        CashCustomerPhone: "#cashCustomerPhone",
        CustomerPhone: "#cashCustomerPhone",
        PhoneNo2: "#cashCustomerPhone",
        CashCustomerName: "#cashCustomerName",
        Name: "#cashCustomerName",
        IPN: "#ipn",
        ManualNO: "#manualNo",
        WalletNumber: "#tetNumPoket",
        Tet_NumPoket: "#tetNumPoket",
        ViolationsValue: "#violationValue",
        ViolationPayType: "input[name='violationPayType']",
        RechargeValue: "#rechargeValue",
        RechargeAmount: "#rechargeValue",
        ServiceType: "#serviceItemId",
        ItemIDService: "#serviceItemId",
        ItemIDService2: "#serviceItemId2",
        WalletBankId: "#serviceItemId2",
        PaymentType: "#paymentType",
        PaymentAmount: "#payedValue",
        PayedValue: "#payedValue",
        NetValue: "#netValue",
        BoxID: "#boxId",
        BranchId: "#branchId",
        StoreID: "#storeId",
        Emp_ID: "#empId",
        VisaNumber: "#visaNumber",
        CardNo: "#visaNumber",
        TblCusCshId: "#visaNumber",
        KycPhone: "#kycPhoneNo2",
        KycName: "#kycName",
        KycNationalId: "#kycNationalId",
        KycCardNo: "#kycCardNo",
        Items: "#itemsTable"
    };

    var validationMessageMap = [
        { text: "رقم المحفظة مطلوب", field: "WalletNumber" },
        { text: "رقم التليفون مطلوب", field: "CashCustomerPhone" },
        { text: "رقم المحمول مطلوب", field: "CashCustomerPhone" },
        { text: "اسم العميل مطلوب", field: "CashCustomerName" },
        { text: "ID مطلوب", field: "IPN" },
        { text: "IPN مطلوب", field: "ManualNO" },
        { text: "الكارت مطلوب", field: "VisaNumber" },
        { text: "قيمة المخالفات مطلوبة", field: "ViolationsValue" },
        { text: "طريقة دفع المخالفات مطلوبة", field: "ViolationPayType" },
        { text: "مبلغ الشحن", field: "RechargeValue" },
        { text: "المحفظة/البنك مطلوبة", field: "ItemIDService2" },
        { text: "طريقة الدفع مطلوبة", field: "PaymentType" },
        { text: "الخزنة غير محددة", field: "BoxID" },
        { text: "الفرع غير محدد", field: "BranchId" },
        { text: "المخزن غير محدد", field: "StoreID" },
        { text: "لا توجد خدمة", field: "Items" }
    ];

    function validationSelectorFor(fieldOrSelector) {
        if (!fieldOrSelector) { return ""; }
        var key = String(fieldOrSelector);
        if (key.charAt(0) === "#" || key.charAt(0) === "." || key.charAt(0) === "[" || key.indexOf("input[") === 0) {
            return key;
        }
        return validationFieldSelectors[key] || "[data-pos-field='" + key.replace(/'/g, "\\'") + "']";
    }

    function fieldElement(fieldOrSelector) {
        if (fieldOrSelector === "WalletNumber" || fieldOrSelector === "Tet_NumPoket") {
            var wallet = byId("tetNumPoket");
            var violationWallet = byId("violationWalletNo");
            if (wallet && isElementVisible(wallet)) { return wallet; }
            if (violationWallet && isElementVisible(violationWallet)) { return violationWallet; }
            return wallet || violationWallet;
        }
        var selector = validationSelectorFor(fieldOrSelector);
        return selector ? document.querySelector(selector) : null;
    }

    function fieldWrapper(element) {
        if (!element) { return null; }
        if (element.closest) {
            return element.closest("label") || element.closest(".smart-lookup") || element.closest(".pos-panel") || element.parentNode;
        }
        return element.parentNode;
    }

    function directValidationMessage(wrapper) {
        if (!wrapper) { return null; }
        for (var i = 0; i < wrapper.children.length; i++) {
            if (wrapper.children[i].classList && wrapper.children[i].classList.contains("pos-validation-message")) {
                return wrapper.children[i];
            }
        }
        return null;
    }

    function isElementVisible(element) {
        if (!element) { return false; }
        var current = element;
        while (current && current !== document.body) {
            if (current.classList && current.classList.contains("is-hidden")) {
                return false;
            }
            var style = window.getComputedStyle ? window.getComputedStyle(current) : null;
            if (style && (style.display === "none" || style.visibility === "hidden")) {
                return false;
            }
            current = current.parentNode;
        }
        return true;
    }

    function ensureFieldVisible(element) {
        if (!element) { return; }
        var panel = element.closest ? element.closest(".pos-panel") : null;
        if (panel) {
            panel.classList.remove("is-hidden");
        }
    }

    function markFieldInvalid(fieldSelector, message) {
        var element = fieldElement(fieldSelector);
        if (!element) { return null; }
        var wrapper = fieldWrapper(element);
        if (wrapper) {
            wrapper.classList.add("pos-field-invalid-wrap");
            var old = directValidationMessage(wrapper);
            if (!old) {
                old = document.createElement("span");
                old.className = "pos-validation-message";
                wrapper.appendChild(old);
            }
            old.innerText = message || "هذا الحقل مطلوب";
        }
        element.classList.add("pos-field-invalid", "pos-field-pulse");
        element.setAttribute("aria-invalid", "true");
        window.setTimeout(function () { element.classList.remove("pos-field-pulse"); }, 450);
        return element;
    }

    function clearFieldInvalid(fieldSelector) {
        var element = fieldElement(fieldSelector);
        if (!element) { return; }
        element.classList.remove("pos-field-invalid", "pos-field-pulse");
        element.removeAttribute("aria-invalid");
        var wrapper = fieldWrapper(element);
        if (wrapper) {
            wrapper.classList.remove("pos-field-invalid-wrap");
            var message = directValidationMessage(wrapper);
            if (message) { message.parentNode.removeChild(message); }
            var smartAction = wrapper.querySelector(".pos-smart-warning-action");
            if (smartAction && smartAction.parentNode) { smartAction.parentNode.removeChild(smartAction); }
            var softWarning = wrapper.querySelector(".pos-soft-limit-warning");
            if (softWarning && softWarning.parentNode) { softWarning.parentNode.removeChild(softWarning); }
        }
    }

    function clearFixedFieldIfValid(element) {
        if (!element || !element.classList || !element.classList.contains("pos-field-invalid")) { return; }
        if (element.type === "radio") {
            var checked = document.querySelector("input[name='" + element.name + "']:checked");
            if (checked) { clearFieldInvalid("input[name='" + element.name + "']"); }
            return;
        }
        var value = element.value;
        if (value !== null && value !== undefined && String(value).trim() !== "" && String(value).trim() !== "0") {
            clearFieldInvalid("#" + element.id);
        }
    }

    function clearHiddenValidationHighlights() {
        var invalid = document.querySelectorAll(".pos-field-invalid");
        for (var i = 0; i < invalid.length; i++) {
            if (!isElementVisible(invalid[i])) {
                var wrapper = fieldWrapper(invalid[i]);
                invalid[i].classList.remove("pos-field-invalid", "pos-field-pulse");
                invalid[i].removeAttribute("aria-invalid");
                if (wrapper) {
                    wrapper.classList.remove("pos-field-invalid-wrap");
                    var message = directValidationMessage(wrapper);
                    if (message && message.parentNode) { message.parentNode.removeChild(message); }
                }
            }
        }
    }

    function clearAllValidationHighlights() {
        var invalid = document.querySelectorAll(".pos-field-invalid");
        for (var i = 0; i < invalid.length; i++) {
            invalid[i].classList.remove("pos-field-invalid", "pos-field-pulse");
            invalid[i].removeAttribute("aria-invalid");
        }
        var wrappers = document.querySelectorAll(".pos-field-invalid-wrap");
        for (var w = 0; w < wrappers.length; w++) {
            wrappers[w].classList.remove("pos-field-invalid-wrap");
        }
        var messages = document.querySelectorAll(".pos-validation-message");
        for (var m = 0; m < messages.length; m++) {
            if (messages[m].parentNode) { messages[m].parentNode.removeChild(messages[m]); }
        }
        var extras = document.querySelectorAll(".pos-smart-warning-action, .pos-soft-limit-warning");
        for (var e = 0; e < extras.length; e++) {
            if (extras[e].parentNode) { extras[e].parentNode.removeChild(extras[e]); }
        }
    }

    function focusFirstInvalidField() {
        var element = document.querySelector(".pos-field-invalid");
        if (!element) { return; }
        ensureFieldVisible(element);
        if (element.scrollIntoView) {
            element.scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
        }
        window.setTimeout(function () {
            try { element.focus({ preventScroll: true }); } catch (ignore) { try { element.focus(); } catch (ignore2) { } }
        }, 220);
    }

    function addValidationError(errors, field, message) {
        errors.push({ field: field, message: message });
        markFieldInvalid(field, message);
    }

    function amountDigits(value) {
        return normalizeDigits(value).replace(/\D/g, "");
    }

    function looksLikeWalletOrPhone(value) {
        var raw = normalizeDigits(value).replace(/[,.\s]/g, "").trim();
        var digits = amountDigits(value);
        if (!digits) { return false; }
        return /^(010|011|012|015)[0-9]{8}$/.test(digits)
            || /^1[0-9]{10}$/.test(digits)
            || (digits.length >= 10 && digits.length <= 14 && parseMoney(raw) > maxRechargeValue);
    }

    function amountFieldName(id) {
        return id === "violationValue" ? "ViolationsValue" : "RechargeValue";
    }

    function amountFieldFriendlyName(id) {
        return id === "violationValue" ? "قيمة المخالفات" : "مبلغ العملية";
    }

    function targetWalletInputForMode() {
        return byId("transactionType").value === "violations" ? byId("violationWalletNo") : byId("tetNumPoket");
    }

    function transferSuspiciousAmountToWallet(sourceInput) {
        var wallet = targetWalletInputForMode();
        if (!sourceInput || !wallet) { return; }
        wallet.value = amountDigits(sourceInput.value);
        if (byId("transactionType").value === "violations") {
            byId("tetNumPoket").value = wallet.value;
        }
        sourceInput.value = "";
        clearFieldInvalid(amountFieldName(sourceInput.id));
        clearFieldInvalid("WalletNumber");
        recalculateInvoiceSummary({ source: "transfer-phone-like-amount", requestCommission: false });
        wallet.focus();
    }

    function showSuspiciousAmountWarning(input) {
        var fieldName = amountFieldName(input.id);
        var element = markFieldInvalid(fieldName, "يبدو أنك أدخلت رقم محفظة في خانة " + amountFieldFriendlyName(input.id));
        var wrapper = element ? fieldWrapper(element) : null;
        if (!wrapper || wrapper.querySelector(".pos-smart-warning-action")) { return; }
        var actions = document.createElement("span");
        actions.className = "pos-smart-warning-action";
        actions.innerHTML =
            '<span>نقل الرقم إلى خانة المحفظة؟</span>' +
            '<button type="button" data-transfer-phone-like-amount="' + input.id + '">نعم</button>' +
            '<button type="button" data-dismiss-phone-like-amount="' + input.id + '">لا</button>';
        wrapper.appendChild(actions);
    }

    function validateAmountSafety(input, showWarning) {
        if (!input) { return true; }
        clearFieldInvalid(amountFieldName(input.id));
        var value = parseMoney(input.value);
        if (looksLikeWalletOrPhone(input.value)) {
            if (showWarning !== false) { showSuspiciousAmountWarning(input); }
            return false;
        }
        if (value > maxRechargeValue) {
            markFieldInvalid(amountFieldName(input.id), "المبلغ أكبر من الحد المسموح. الحد الأقصى 100,000");
            return false;
        }
        if (value > amountWarningLimit && showWarning !== false) {
            var warning = fieldWrapper(input);
            if (warning && !warning.querySelector(".pos-soft-limit-warning")) {
                var soft = document.createElement("span");
                soft.className = "pos-soft-limit-warning";
                soft.innerText = "مبلغ كبير. راجع القيمة قبل التأكيد.";
                warning.appendChild(soft);
            }
        }
        return true;
    }

    function applyServerValidationErrors(data) {
        var raw = data && (data.validationErrorsDetailed || data.ValidationErrorsDetailed || data.validationErrors || data.ValidationErrors);
        var applied = false;
        if (raw) {
            if (Array.isArray(raw)) {
                for (var i = 0; i < raw.length; i++) {
                    var field = raw[i].field || raw[i].Field || raw[i].key || raw[i].Key;
                    var message = raw[i].message || raw[i].Message || "";
                    if (field) {
                        markFieldInvalid(field, message || "هذا الحقل مطلوب");
                        applied = true;
                    }
                }
            } else {
                for (var key in raw) {
                    if (Object.prototype.hasOwnProperty.call(raw, key)) {
                        markFieldInvalid(key, raw[key] || "هذا الحقل مطلوب");
                        applied = true;
                    }
                }
            }
        }

        if (!applied) {
            var messageText = ((data && (data.message || data.Message || data.technicalMessage)) || "").trim();
            for (var m = 0; m < validationMessageMap.length; m++) {
                if (messageText.indexOf(validationMessageMap[m].text) >= 0) {
                    markFieldInvalid(validationMessageMap[m].field, messageText);
                    applied = true;
                    break;
                }
            }
        }

        if (applied) {
            focusFirstInvalidField();
        }
        return applied;
    }

    function uxIsServiceReady() {
        var mode = byId("transactionType").value;
        if (mode === "card") {
            return !!byId("serviceItemId").value && hasItemRows() && hasValidItemSelection();
        }
        if (mode === "violations") {
            return !!byId("serviceItemId").value;
        }
        return !!byId("serviceItemId").value && hasItemRows() && hasValidItemSelection();
    }

    function uxIsValueReady() {
        var mode = byId("transactionType").value;
        if (!uxIsServiceReady()) { return false; }
        if (mode === "card") { return numberValue("netValue") > 0; }
        if (mode === "violations") { return numberValue("violationValue") > 0 && numberValue("netValue") > 0; }
        return numberValue("rechargeValue") > 0 && numberValue("netValue") > 0 && !commissionCalculationPending;
    }

    function uxIsPaymentReady() {
        if (!uxIsValueReady()) { return false; }
        if (!byId("paymentType").value || !byId("boxId").value) { return false; }
        return numberValue("remainValue") <= 0;
    }

    function uxHasRequiredCustomerData() {
        var mode = byId("transactionType").value;
        if (!byId("cashCustomerPhone").value.trim()) { return false; }
        if (!byId("cashCustomerName").value.trim()) { return false; }
        if (mode !== "card" && !byId("ipn").value.trim()) { return false; }
        if (isImportantIpnMode(mode) && !byId("manualNo").value.trim()) { return false; }
        if (mode === "card" && (!byId("visaNumber").value.trim() || !byId("cashCustomerId").value)) { return false; }
        if (mode === "violations" && !byId("violationWalletNo").value.trim()) { return false; }
        return true;
    }

    function isImportantIpnMode(mode) {
        return mode === "cash-in";
    }

    function uxContextAllowsSave() {
        var canEditLoadedInvoice = reviewMode && currentContext
            && (currentContext.CanEditInvoice === true || (loadedInvoiceCreatedUserId && loadedInvoiceCreatedUserId === currentContext.UserId && currentContext.CanAdd === true));
        return !(reviewMode && !canEditLoadedInvoice) && !loadedInvoiceIsCancelled && !saveDisabledByContext(byId("transactionType").value);
    }

    function uxIsSaveReady() {
        return !uxIsBusy()
            && uxIsPaymentReady()
            && uxHasRequiredCustomerData()
            && uxContextAllowsSave()
            && commissionsReady
            && !commissionCalculationPending;
    }

    function uxResolveStep() {
        if (!uxIsServiceReady()) { return "service"; }
        if (!uxIsValueReady()) { return "value"; }
        if (!uxIsPaymentReady()) { return "payment"; }
        return "save";
    }

    function uxStepRank(step) {
        if (step === "service") { return 0; }
        if (step === "value") { return 1; }
        if (step === "payment") { return 2; }
        return 3;
    }

    function uxCanEnterStep(step) {
        if (step === "service") { return true; }
        if (step === "value") { return uxIsServiceReady(); }
        if (step === "payment") { return uxIsValueReady(); }
        if (step === "save") { return uxIsPaymentReady(); }
        return false;
    }

    function uxPaintWorkflow() {
        var currentRank = uxStepRank(uxCurrentStep);
        var steps = document.querySelectorAll(".workflow-step");
        for (var i = 0; i < steps.length; i++) {
            var stepName = steps[i].getAttribute("data-step");
            var rank = uxStepRank(stepName);
            var complete = rank < currentRank;
            var current = rank === currentRank;
            var allowed = uxCanEnterStep(stepName) && !uxIsBusy();
            steps[i].classList.toggle("is-complete", rank < currentRank);
            steps[i].classList.toggle("is-current", rank === currentRank);
            steps[i].classList.toggle("is-pending", rank > currentRank);
            steps[i].disabled = !allowed;
            steps[i].setAttribute("aria-current", current ? "step" : "false");
            steps[i].setAttribute("aria-disabled", allowed ? "false" : "true");
            var circle = steps[i].querySelector(".workflow-step-circle");
            if (circle) { circle.innerText = complete ? "✓" : String(rank + 1); }
        }
    }

    function uxSetControlState(selector, disabled) {
        var controls = document.querySelectorAll(selector);
        for (var i = 0; i < controls.length; i++) {
            controls[i].disabled = disabled;
        }
    }

    function uxApplyFlow() {
        var serviceReady = uxIsServiceReady();
        var valueReady = uxIsValueReady();
        var paymentReady = uxIsPaymentReady();
        var busy = uxIsBusy();

        uxCurrentStep = uxResolveStep();
        uxPaintWorkflow();

        byId("amountPanel").classList.toggle("pos-step-locked", !serviceReady || busy);
        byId("bottomSummaryPanel").classList.toggle("pos-step-locked", !valueReady || busy);
        var paymentPanel = document.querySelector(".payment-panel");
        var canChangePayment = currentContext && currentContext.CanChangeDefaults === true;
        if (paymentPanel) { paymentPanel.classList.toggle("pos-step-locked", !valueReady || busy); }

        uxSetControlState("#rechargeValue, #violationValue, #violationWalletNo, input[name='violationPayType']", !serviceReady || busy);
        uxSetControlState("#paymentType", !valueReady || busy || !canChangePayment);
        uxSetControlState("#paymentCardNo, #payedValue", !valueReady || busy);
        uxSetControlState(".qty, .price, .vat, .item-name, .remove-row, #addItemBtn", busy);

        var saveButton = byId("saveBtn");
        if (saveButton) {
            var ready = uxIsSaveReady();
            saveButton.disabled = !ready;
            saveButton.title = ready ? "" : "أكمل البيانات أولاً";
            saveButton.classList.toggle("is-saving", uxSaving);
            saveButton.innerText = uxSaving ? "جاري الحفظ..." : "حفظ";
        }
    }

    function uxFocusStep(step) {
        var target = null;
        if (step === "service") {
            target = byId("serviceItemId");
        } else if (step === "value") {
            target = byId("transactionType").value === "violations" ? byId("violationValue") : byId("rechargeValue");
        } else if (step === "payment") {
            target = byId("paymentType");
        } else {
            target = byId("saveBtn");
        }
        if (target && !target.disabled && target.focus) { target.focus(); }
    }

    function uxHandleEnter(event) {
        if (!event || event.key !== "Enter") { return false; }
        var target = event.target;
        if (target && (target.tagName === "TEXTAREA" || target.closest(".pos-modal"))) { return false; }
        event.preventDefault();
        if (uxIsBusy() || uxDebounced()) {
            uxShowGuide();
            return true;
        }

        uxApplyFlow();
        if (uxCurrentStep === "save" && uxIsSaveReady()) {
            byId("posForm").requestSubmit ? byId("posForm").requestSubmit() : byId("saveBtn").click();
            return true;
        }

        uxShowGuide();
        uxFocusStep(uxCurrentStep);
        return true;
    }

    function uxHandleSaveShortcut(event) {
        var isF9 = event.key === "F9";
        var isCtrlS = (event.ctrlKey || event.metaKey) && (event.key || "").toLowerCase() === "s";
        if (!isF9 && !isCtrlS) { return; }
        event.preventDefault();
        if (uxIsSaveReady() && !uxDebounced()) {
            byId("posForm").requestSubmit ? byId("posForm").requestSubmit() : byId("saveBtn").click();
            return;
        }
        uxShowGuide();
        uxFocusStep(uxCurrentStep);
    }

    function focusPreviousEditable(current) {
        var controls = Array.prototype.slice.call(document.querySelectorAll("#posForm input, #posForm select, #posForm button"));
        controls = controls.filter(function (item) {
            return item && !item.disabled && !item.readOnly && isElementVisible(item) && item.type !== "hidden";
        });
        var index = controls.indexOf(current);
        var target = controls[Math.max(index - 1, 0)] || controls[0];
        if (target && target.focus) { target.focus(); }
    }

    function handleGlobalShortcuts(event) {
        if (!event) { return false; }
        if (event.key === "F2") { event.preventDefault(); switchTransactionMode("cash-in"); return true; }
        if (event.key === "F3") { event.preventDefault(); switchTransactionMode("cash-out"); return true; }
        if (event.key === "F4") { event.preventDefault(); switchTransactionMode("card"); return true; }
        if (event.key === "F5") {
            event.preventDefault();
            if (uxIsSaveReady()) {
                byId("posForm").requestSubmit ? byId("posForm").requestSubmit() : byId("saveBtn").click();
            } else {
                uxShowGuide();
                uxFocusStep(uxCurrentStep);
            }
            return true;
        }
        if (event.key === "Escape") {
            event.preventDefault();
            if (byId("saveConfirmPanel") && byId("saveConfirmPanel").classList.contains("is-open")) {
                closeSaveConfirmation();
            } else {
                reloadContextAndReset();
            }
            return true;
        }
        if (event.key === "Enter" && event.shiftKey) {
            event.preventDefault();
            focusPreviousEditable(event.target);
            return true;
        }
        return false;
    }

    function updateSaveButtonState() {
        var button = byId("saveBtn");
        if (!button) { return; }
        button.disabled = !uxIsSaveReady();
        button.title = button.disabled ? "أكمل البيانات أولاً" : "";
        uxApplyFlow();
    }

    function setCommissionStatus(message, isError) {
        var target = byId("saveResult");
        if (!target) { return; }
        if (!message) {
            if (target.getAttribute("data-commission-status") === "true") {
                target.innerText = "";
                target.classList.remove("is-error");
                target.removeAttribute("data-commission-status");
            }
            return;
        }
        target.setAttribute("data-commission-status", "true");
        target.innerText = message;
        target.classList.toggle("is-error", isError === true);
    }

    function clearCommissionPreviewValues() {
        var firstRow = getFirstSelectedItemRow();
        if (firstRow) {
            firstRow.querySelector(".price").value = "0.00";
            firstRow.querySelector(".vat").value = "0.00";
            firstRow.querySelector(".line-total").value = "0.00";
            firstRow.setAttribute("data-show-price", "0");
            firstRow.removeAttribute("data-bank-machine-commission");
            firstRow.removeAttribute("data-cashout-machine-withdrawal");
        }
        lastCashOutBankMachineCommission = 0;
        lastCashOutMachineWithdrawalAmount = 0;
        byId("commissionValue").value = "0.00";
        byId("vatValue").value = "0.00";
        byId("totalFees").value = "0.00";
        byId("netValue").value = byId("transactionType").value === "card" ? "0.00" : decimalText(numberValue("rechargeValue"));
        byId("payedValue").value = byId("netValue").value;
        byId("remainValue").value = "0.00";
        calculateTotals();
    }

    function markCommissionPending(message) {
        commissionCalculationPending = true;
        lastCommissionKey = "";
        clearCommissionPreviewValues();
        updateSaveButtonState();
        if (message) {
            setCommissionStatus(message);
        }
    }

    function setMode(mode, suppressServiceLoad) {
        var config = modes[mode] || modes["cash-in"];
        var page = byId("posPage");
        page.className = "pos-page " + config.colorClass;
        byId("transactionType").value = mode;
        byId("isCashOut").value = config.isCashOut ? "true" : "false";
        byId("isPOS").value = config.isPOS ? "true" : "false";
        byId("posModeLabel").innerText = config.label;
        if (byId("workflowOperationTitle")) {
            byId("workflowOperationTitle").innerText = config.operationLabel || ("عملية " + config.label);
        }

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

        var primaryServiceFields = document.querySelectorAll(".service-primary-field");
        for (var ps = 0; ps < primaryServiceFields.length; ps++) {
            primaryServiceFields[ps].classList.toggle("is-hidden", mode === "card");
        }

        var paymentAmountFields = document.querySelectorAll(".payment-amount-field");
        for (var pa = 0; pa < paymentAmountFields.length; pa++) {
            paymentAmountFields[pa].classList.toggle("is-hidden", mode === "card" || mode === "cash-in" || mode === "cash-out" || mode === "violations");
        }
        var idIpnFields = document.querySelectorAll(".id-ipn-field");
        for (var idf = 0; idf < idIpnFields.length; idf++) {
            idIpnFields[idf].classList.toggle("is-hidden-ui", mode === "card");
        }
        if (mode === "card") {
            byId("ipn").value = "";
            byId("manualNo").value = "";
            clearFieldInvalid("IPN");
            clearFieldInvalid("ManualNO");
        }

        toggleFields(".service-secondary-field", mode === "card" || mode === "violations");
        toggleFields(".amount-service-field", mode === "card" || mode === "cash-in" || mode === "cash-out" ? true : false);
        toggleFields(".amount-vat-field", mode === "card" || mode === "cash-in" || mode === "cash-out" || mode === "violations");
        toggleFields(".amount-fees-field", mode === "card" || mode === "cash-in" || mode === "cash-out" || mode === "violations");
        toggleFields(".amount-net-field", mode === "card" || mode === "cash-in" || mode === "cash-out" || mode === "violations");
        toggleFields(".store-field", true);
        byId("amountPanel").classList.toggle("is-hidden-ui", mode === "card");
        byId("amountGrid").className = "pos-grid five-cols amount-grid amount-mode-" + mode;

        updateAmountLabels(mode);

        byId("violationPanel").style.display = mode === "violations" ? "block" : "none";
        updateSaveButtonState();
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
            byId("serviceItemId2").value = "";
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

        clearHiddenValidationHighlights();

        if (!suppressServiceLoad) {
            resetServiceRows();
            if (mode === "card") {
                var cardItemId = cardServiceItemIdFromCardNo(byId("visaNumber").value);
                setSelectSingleOption("serviceItemId", cardItemId, cardItemId === 1 ? "كارت ميزة كيشني" : "كارت البنك الأهلي");
                loadDefaultServiceItem(mode, false, cardItemId);
            } else {
                loadPrimaryServiceItems(mode);
            }
            recalculateInvoiceSummary({ source: "setMode", requestCommission: true });
        }
        updateCashOutMachineDisplay();
        updateBottomSummary();
        clearMessages();
    }

    function updateAmountLabels(mode) {
        var isCard = mode === "card";
        byId("commissionLabel").innerText = isCard ? "قيمة الخدمة" : "قيمة الخدمة";
        byId("vatLabel").innerText = "الضريبة";
        byId("totalFeesLabel").innerText = isCard ? "إجمالي الرسوم / الصافي" : "إجمالي الرسوم";
    }

    function currentCashOutMachineInfo() {
        var firstRow = getFirstSelectedItemRow();
        var bankCommission = lastCashOutBankMachineCommission;
        var withdrawalAmount = lastCashOutMachineWithdrawalAmount;
        if (firstRow) {
            bankCommission = bankCommission || parseFloat(firstRow.getAttribute("data-bank-machine-commission")) || 0;
            withdrawalAmount = withdrawalAmount || parseFloat(firstRow.getAttribute("data-cashout-machine-withdrawal")) || 0;
        }

        return {
            bankCommission: bankCommission,
            withdrawalAmount: withdrawalAmount
        };
    }

    function currentMachineWithdrawalEstimate(bankCommission, withdrawalAmount) {
        if (byId("transactionType").value !== "cash-out") { return 0; }
        if (withdrawalAmount > 0) { return withdrawalAmount; }
        return Math.max(numberValue("netValue") - (bankCommission || 0), 0);
    }

    function updateCashOutMachineDisplay() {
        var mode = byId("transactionType").value;
        updateBottomSummary();
    }

    function updateBottomSummary() {
        var mode = byId("transactionType").value;
        var isCard = mode === "card";
        var title = byId("bottomSummaryTitle");
        var grid = byId("bottomSummaryGrid");
        if (!title || !grid) { return; }

        if (isCard) {
            var total = numberValue("netValue");
            var vat = numberValue("vatValue");
            var netBeforeVat = Math.max(total - vat, 0);
            title.innerText = "ملخص الكارت";
            grid.innerHTML =
                '<div><span>قيمة الكارت قبل الضريبة</span><strong>' + escapeHtml(decimalText(netBeforeVat)) + '</strong></div>' +
                '<div><span>الضريبة</span><strong>' + escapeHtml(decimalText(vat)) + '</strong></div>' +
                '<div class="summary-total"><span>الإجمالي</span><strong>' + escapeHtml(decimalText(total)) + '</strong></div>';
            return;
        }

        title.innerText = "ملخص العملية";
        if (mode === "cash-in" || mode === "cash-out") {
            var recharge = numberValue("rechargeValue");
            var fee = numberValue("commissionValue");
            var tax = numberValue("vatValue");
            var net = recharge + tax + fee;
            var summaryHtml =
                '<div><span>قيمة الشحن</span><strong>' + escapeHtml(decimalText(recharge)) + '</strong></div>' +
                '<div><span>الضريبة</span><strong>' + escapeHtml(decimalText(tax)) + '</strong></div>' +
                '<div><span>إجمالي الرسوم</span><strong>' + escapeHtml(decimalText(fee)) + '</strong></div>' +
                '<div class="summary-total"><span>الصافي</span><strong>' + escapeHtml(decimalText(net)) + '</strong></div>';
            var machineInfo = currentCashOutMachineInfo();
            if (mode === "cash-out") {
                var machineWithdrawal = currentMachineWithdrawalEstimate(machineInfo.bankCommission, machineInfo.withdrawalAmount);
                var machineHint = commissionCalculationPending
                    ? "جاري حساب عمولة السحب من الماكينة"
                    : (machineInfo.bankCommission > 0 ? ("عمولة السحب من الماكينة: " + decimalText(machineInfo.bankCommission)) : "عمولة السحب من الماكينة غير محسوبة بعد");
                summaryHtml += '<div class="summary-total cash-out-machine-alert' + (commissionCalculationPending ? ' is-pending' : '') + '"><span>المبلغ المطلوب سحبه من الماكينة</span><strong>' + escapeHtml(decimalText(machineWithdrawal)) + '</strong><small>' + escapeHtml(machineHint) + '</small></div>';
            }
            grid.innerHTML = summaryHtml;
            return;
        }

        grid.innerHTML =
            '<div><span>قيمة الشحن</span><strong>' + escapeHtml(decimalText(numberValue("rechargeValue"))) + '</strong></div>' +
            '<div><span>إجمالي الرسوم</span><strong>' + escapeHtml(decimalText(numberValue("totalFees"))) + '</strong></div>' +
            '<div><span>المدفوع</span><strong>' + escapeHtml(decimalText(numberValue("payedValue"))) + '</strong></div>' +
            '<div class="summary-total"><span>المتبقي</span><strong>' + escapeHtml(decimalText(numberValue("remainValue"))) + '</strong></div>';
    }

    function toggleFields(selector, hide) {
        var fields = document.querySelectorAll(selector);
        for (var i = 0; i < fields.length; i++) {
            fields[i].classList.toggle("is-hidden", hide);
        }
    }

    function normalizeNameSpaces(value) {
        return (value || "").replace(/\s+/g, " ").trim();
    }

    function splitNameIntoFour(value) {
        var parts = normalizeNameSpaces(value).split(" ").filter(function (part) { return !!part; });
        if (parts.length <= 4) {
            while (parts.length < 4) { parts.push(""); }
            return parts;
        }

        return [
            parts[0] || "",
            parts[1] || "",
            parts[2] || "",
            parts.slice(3).join(" ")
        ];
    }

    function mergeNameParts(ids) {
        return normalizeNameSpaces(ids.map(function (id) {
            var element = byId(id);
            return element ? element.value : "";
        }).filter(function (value) {
            return !!normalizeNameSpaces(value);
        }).join(" "));
    }

    function syncNameFromFull(fullId, partIds, normalizeFull) {
        if (kycNameSyncing) { return; }
        var full = byId(fullId);
        if (!full) { return; }

        var normalized = normalizeNameSpaces(full.value);
        kycNameSyncing = true;
        try {
            if (normalizeFull) { full.value = normalized; }
            var parts = splitNameIntoFour(normalized);
            for (var i = 0; i < partIds.length; i++) {
                byId(partIds[i]).value = parts[i] || "";
            }
        } finally {
            kycNameSyncing = false;
        }
    }

    function syncFullNameFromParts(fullId, partIds, normalizeParts) {
        if (kycNameSyncing) { return; }

        kycNameSyncing = true;
        try {
            if (normalizeParts) {
                for (var i = 0; i < partIds.length; i++) {
                    var part = byId(partIds[i]);
                    if (part) { part.value = normalizeNameSpaces(part.value); }
                }
            }
            byId(fullId).value = mergeNameParts(partIds);
        } finally {
            kycNameSyncing = false;
        }
    }

    function scheduleKycNameSync(target) {
        if (!target || !target.id || kycNameSyncing) { return; }

        var arabicParts = ["kycArabicName0", "kycArabicName1", "kycArabicName2", "kycArabicName3"];
        var englishParts = ["kycEnglishName0", "kycEnglishName1", "kycEnglishName2", "kycEnglishName3"];

        if (target.id === "kycName") {
            kycArabicNameSource = "full";
            window.clearTimeout(kycArabicNameTimer);
            kycArabicNameTimer = window.setTimeout(function () {
                syncNameFromFull("kycName", arabicParts, false);
            }, 350);
            return;
        }

        if (arabicParts.indexOf(target.id) >= 0) {
            kycArabicNameSource = "parts";
            window.clearTimeout(kycArabicNameTimer);
            kycArabicNameTimer = window.setTimeout(function () {
                syncFullNameFromParts("kycName", arabicParts, false);
            }, 250);
            return;
        }

        if (target.id === "kycNameE") {
            kycEnglishNameSource = "full";
            window.clearTimeout(kycEnglishNameTimer);
            kycEnglishNameTimer = window.setTimeout(function () {
                syncNameFromFull("kycNameE", englishParts, false);
            }, 350);
            return;
        }

        if (englishParts.indexOf(target.id) >= 0) {
            kycEnglishNameSource = "parts";
            window.clearTimeout(kycEnglishNameTimer);
            kycEnglishNameTimer = window.setTimeout(function () {
                syncFullNameFromParts("kycNameE", englishParts, false);
            }, 250);
        }
    }

    function commitKycNameSync(target) {
        if (!target || !target.id || kycNameSyncing) { return; }

        var arabicParts = ["kycArabicName0", "kycArabicName1", "kycArabicName2", "kycArabicName3"];
        var englishParts = ["kycEnglishName0", "kycEnglishName1", "kycEnglishName2", "kycEnglishName3"];

        if (target.id === "kycName") {
            kycArabicNameSource = "full";
            window.clearTimeout(kycArabicNameTimer);
            syncNameFromFull("kycName", arabicParts, true);
            return;
        }

        if (arabicParts.indexOf(target.id) >= 0) {
            kycArabicNameSource = "parts";
            window.clearTimeout(kycArabicNameTimer);
            syncFullNameFromParts("kycName", arabicParts, true);
            return;
        }

        if (target.id === "kycNameE") {
            kycEnglishNameSource = "full";
            window.clearTimeout(kycEnglishNameTimer);
            syncNameFromFull("kycNameE", englishParts, true);
            return;
        }

        if (englishParts.indexOf(target.id) >= 0) {
            kycEnglishNameSource = "parts";
            window.clearTimeout(kycEnglishNameTimer);
            syncFullNameFromParts("kycNameE", englishParts, true);
        }
    }

    function commitAllKycNameSync() {
        var arabicParts = ["kycArabicName0", "kycArabicName1", "kycArabicName2", "kycArabicName3"];
        var englishParts = ["kycEnglishName0", "kycEnglishName1", "kycEnglishName2", "kycEnglishName3"];

        window.clearTimeout(kycArabicNameTimer);
        window.clearTimeout(kycEnglishNameTimer);

        if (kycArabicNameSource === "parts") {
            syncFullNameFromParts("kycName", arabicParts, true);
        } else if (normalizeNameSpaces(byId("kycName").value)) {
            syncNameFromFull("kycName", arabicParts, true);
        } else {
            syncFullNameFromParts("kycName", arabicParts, true);
        }

        if (kycEnglishNameSource === "parts") {
            syncFullNameFromParts("kycNameE", englishParts, true);
        } else if (normalizeNameSpaces(byId("kycNameE").value)) {
            syncNameFromFull("kycNameE", englishParts, true);
        } else {
            syncFullNameFromParts("kycNameE", englishParts, true);
        }
    }

    function clearKycFields() {
        byId("cashCustomerId").value = "";
        pendingDuplicateKycCustomer = null;
        kycArabicNameSource = "";
        kycEnglishNameSource = "";
        enablePrintAcknowledgmentIfAllowed();
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
        byId("kycArabicName3").value = "";
        byId("kycEnglishName0").value = "";
        byId("kycEnglishName1").value = "";
        byId("kycEnglishName2").value = "";
        byId("kycEnglishName3").value = "";
        byId("kycEnglishName5").value = "";
        byId("kycEnglishName6").value = "";
        byId("kycEnglishName7").value = "";
        byId("kycPhoneNo2").value = "";
        byId("kycPhoneNo").value = "";
        byId("kycCardNo").value = "";
        byId("kycNationalId").value = "";
        byId("kycCardSource").value = "";
        byId("kycCreatedBranch").value = activeKycBranchName();
        byId("kycCreatedDate").value = defaultKycCreatedDate();
        refreshKycMetadataDisplay();
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

    function setKycMessage(message, isError, isInfo) {
        var messageBox = byId("kycSaveMessage");
        if (!messageBox) { return; }
        messageBox.innerText = message || "";
        messageBox.className = "kyc-save-message" + (message ? (isError ? " is-error" : (isInfo ? " is-info" : " is-success")) : "");
    }

    function scheduleKycCardAvailabilityCheck() {
        window.clearTimeout(kycCardAvailabilityTimer);
        kycCardAvailabilityTimer = window.setTimeout(validateKycCardAvailability, 350);
    }

    function validateKycCardAvailability() {
        var url = getUrl("data-kyc-card-availability-url");
        var cardNo = (byId("kycCardNo").value || byId("visaNumber").value || "").trim();
        if (!url || !cardNo || byId("transactionType").value !== "card") { return; }

        if (cardNo.length !== 8 && cardNo.length !== 18) {
            setKycMessage("رقم التوكن/الكارت يجب أن يكون 8 أو 18 رقم", true);
            return;
        }

        if (kycCardAvailabilityXhr && kycCardAvailabilityXhr.readyState !== 4) {
            kycCardAvailabilityXhr.abort();
        }

        var query = "?cardNo=" + encodeURIComponent(cardNo)
            + "&nationalId=" + encodeURIComponent((byId("kycNationalId").value || byId("cardNationalId").value || "").trim())
            + "&mobile=" + encodeURIComponent((byId("kycPhoneNo2").value || byId("cashCustomerPhone").value || "").trim())
            + "&customerId=" + encodeURIComponent(parseInt(byId("cashCustomerId").value, 10) || "");

        kycCardAvailabilityXhr = requestJson("GET", url + query, null, function (status, data) {
            if (!data || data.success === false) {
                setKycMessage((data && data.message) || "تعذر فحص حالة الكارت حالياً", true);
                return;
            }

            if (data.Available || data.available) {
                setKycMessage((data.Message || data.message || "الكارت متاح ويمكن تفعيله."), false, false);
                return;
            }

            setKycMessage((data.Message || data.message || "الكارت غير متاح للتفعيل."), true);
        });
    }

    function showDuplicateKycCustomerAction(data, message) {
        pendingDuplicateKycCustomer = data && data.existingCustomer ? data.existingCustomer : null;
        setKycMessage(message, true);

        var messageBox = byId("kycSaveMessage");
        if (!messageBox || !pendingDuplicateKycCustomer) { return; }

        var actionBox = document.createElement("div");
        actionBox.className = "kyc-duplicate-action";
        var button = document.createElement("button");
        button.type = "button";
        button.id = "loadDuplicateKycCustomerBtn";
        button.className = "secondary-action";
        button.innerText = "تحميل العميل المسجل";
        actionBox.appendChild(button);
        messageBox.appendChild(actionBox);
    }

    function loadPendingDuplicateKycCustomer() {
        if (!pendingDuplicateKycCustomer) {
            setKycMessage("لا توجد بيانات عميل مسجل للتحميل", true);
            return;
        }

        applyKeshniCustomer(pendingDuplicateKycCustomer);
        loadKycAttachments(pendingDuplicateKycCustomer.CustomerID);
        setKycMessage("تم تحميل العميل المسجل. يمكنك تعديل البيانات ثم الحفظ.");
        pendingDuplicateKycCustomer = null;
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
        row.removeAttribute("data-bank-machine-commission");
        row.removeAttribute("data-cashout-machine-withdrawal");
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
        byId("serviceItemId").innerHTML = '<option value="">اختر نوع الشحن</option>';
        byId("serviceItemId2").innerHTML = '<option value="">اختر المحفظة/البنك</option>';
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
        if (byId("transactionType").value === "card") {
            byId("payedValue").value = totalValue.toFixed(2);
        }
        byId("remainValue").value = (totalValue - numberValue("payedValue")).toFixed(2);
        byId("totalFees").value = itemTotal.toFixed(2);
        updateCashOutMachineDisplay();
        updateBottomSummary();
        uxApplyFlow();
    }

    function recalculateInvoiceSummary(options) {
        options = options || {};
        window.clearTimeout(amountCommitTimer);
        calculateTotals();
        posDebugLog("recalculateInvoiceSummary", {
            source: options.source || "",
            requestCommission: options.requestCommission === true,
            mode: byId("transactionType").value,
            rechargeValue: numberValue("rechargeValue"),
            fees: numberValue("totalFees"),
            net: numberValue("netValue"),
            machine: currentMachineWithdrawalEstimate(currentCashOutMachineInfo().bankCommission, currentCashOutMachineInfo().withdrawalAmount)
        });
        if (options.requestCommission === true) {
            if (byId("transactionType").value === "cash-out") {
                lastCashOutMachineWithdrawalAmount = 0;
                var firstRow = getFirstSelectedItemRow();
                if (firstRow) { firstRow.removeAttribute("data-cashout-machine-withdrawal"); }
                updateCashOutMachineDisplay();
            }
            scheduleCommissionPreview({
                delay: 0,
                quiet: true,
                useOverlay: false
            });
        }
    }

    function isAmountInput(target) {
        return target && target.matches && target.matches("#rechargeValue, #violationValue, #payedValue");
    }

    function commitAmountInput(target, immediate) {
        if (!target || !isAmountInput(target)) { return; }
        if (!validateAmountSafety(target, true)) {
            commissionCalculationPending = false;
            amountEnterAdvancePending = false;
            clearCommissionPreviewValues();
            updateSaveButtonState();
            return;
        }
        recalculateInvoiceSummary({
            source: target.id,
            requestCommission: target.id === "rechargeValue" || target.id === "violationValue"
        });

        if (target.id === "payedValue" && immediate) {
            amountEnterAdvancePending = false;
            uxApplyFlow();
            uxFocusStep(uxCurrentStep);
        }
    }

    function scheduleAmountCommit(target) {
        if (!target || !isAmountInput(target)) { return; }
        commitAmountInput(target, false);
        uxApplyFlow();
    }

    function uxAdvanceAfterAmountCommit() {
        if (!amountEnterAdvancePending) { return; }
        amountEnterAdvancePending = false;
        uxApplyFlow();
        uxFocusStep(uxCurrentStep);
    }

    function validateForm() {
        var errors = [];
        var mode = byId("transactionType").value;
        clearAllValidationHighlights();

        if (!mode) { addValidationError(errors, "TransactionType", "نوع العملية مطلوب"); }
        if (mode !== "card" && !validateAmountSafety(byId(mode === "violations" ? "violationValue" : "rechargeValue"), true)) {
            addValidationError(errors, mode === "violations" ? "ViolationsValue" : "RechargeValue", "قيمة المبلغ غير صحيحة. يبدو أنك أدخلت رقم هاتف بدل مبلغ العملية.");
        }
        if (!byId("cashCustomerPhone").value.trim()) { addValidationError(errors, "CashCustomerPhone", "رقم التليفون مطلوب"); }
        if (byId("cashCustomerPhone").value.trim() && !normalizePhoneInput(byId("cashCustomerPhone"), true)) { addValidationError(errors, "CashCustomerPhone", phoneValidationMessage(byId("cashCustomerPhone").value) || "رقم الهاتف يجب أن يتكون من 11 رقم"); }
        if (!byId("cashCustomerName").value.trim()) { addValidationError(errors, "CashCustomerName", "اسم العميل مطلوب"); }
        if (mode !== "card" && !byId("ipn").value.trim()) { addValidationError(errors, "IPN", "ID مطلوب"); }
        if (isImportantIpnMode(mode) && !byId("manualNo").value.trim()) { addValidationError(errors, "ManualNO", "IPN مطلوب في كاش إن فقط"); }
        if (mode === "card" && !byId("visaNumber").value.trim()) { addValidationError(errors, "VisaNumber", "الكارت مطلوب في حالة كارت كيشني"); }
        if (mode === "card" && !byId("cashCustomerId").value) { addValidationError(errors, "VisaNumber", "يجب تفعيل الكارت وحفظ بيانات KYC قبل حفظ الفاتورة"); }
        if (mode === "violations") {
            if (numberValue("violationValue") <= 0) { addValidationError(errors, "ViolationsValue", "قيمة المخالفات مطلوبة"); }
            if (numberValue("violationValue") > maxRechargeValue) { addValidationError(errors, "ViolationsValue", "قيمة المخالفات أكبر من الحد المسموح لهذه الخدمة"); }
            if (selectedRadioValue("violationPayType") === "") { addValidationError(errors, "ViolationPayType", "طريقة دفع المخالفات مطلوبة"); }
            if (!byId("violationWalletNo").value.trim()) { addValidationError(errors, "WalletNumber", "رقم المحفظة مطلوب"); }
            if (byId("violationWalletNo").value.trim() && !normalizePhoneInput(byId("violationWalletNo"), true)) { addValidationError(errors, "WalletNumber", phoneValidationMessage(byId("violationWalletNo").value)); }
        } else if (mode !== "card" && numberValue("rechargeValue") <= 0) {
            addValidationError(errors, "RechargeValue", "مبلغ الشحن يجب أن يكون أكبر من صفر");
        } else if (mode !== "card" && numberValue("rechargeValue") > maxRechargeValue) {
            addValidationError(errors, "RechargeValue", "مبلغ الشحن أكبر من الحد المسموح لهذه الخدمة");
        }
        if (mode === "cash-out" && byId("isWallet").value === "true" && !byId("tetNumPoket").value.trim()) {
            addValidationError(errors, "WalletNumber", "رقم المحفظة مطلوب");
        }
        if (mode === "cash-out" && byId("tetNumPoket").value.trim() && !normalizePhoneInput(byId("tetNumPoket"), true)) {
            addValidationError(errors, "WalletNumber", phoneValidationMessage(byId("tetNumPoket").value));
        }
        var primaryServiceId = parseInt(byId("serviceItemId").value, 10) || 0;
        if (mode !== "card" && mode !== "violations" && (primaryServiceId === 6 || primaryServiceId === 7 || primaryServiceId === 8 || primaryServiceId === 10) && !byId("serviceItemId2").value) {
            addValidationError(errors, "ItemIDService2", "المحفظة/البنك مطلوبة لهذا النوع");
        }
        if (!hasItemRows() || !hasValidItemSelection()) { addValidationError(errors, "Items", "لا توجد خدمة كيشني محملة"); }
        if (!hasValidQuantities()) { addValidationError(errors, "Items", "من فضلك أدخل كمية وسعر صحيح"); }
        if (!byId("branchId").value) { addValidationError(errors, "BranchId", "الفرع غير محدد"); }
        if (!byId("paymentType").value) { addValidationError(errors, "PaymentType", "طريقة الدفع مطلوبة"); }
        if (!byId("boxId").value) { addValidationError(errors, "BoxID", "الخزنة غير محددة"); }
        byId("validationSummary").innerHTML = errors.length ? "برجاء استكمال الحقول المطلوبة<br />" + errors.map(function (e) { return escapeHtml(e.message); }).join("<br />") : "";
        if (errors.length) { focusFirstInvalidField(); }
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

    // Legacy POS mapping:
    // UI field "ID" is stored in request.IPN and Transactions.IPN.
    // UI field "IPN" is stored in request.ManualNO and Transactions.ManualNO.
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
        if (reviewMode && !branchId) { branchId = loadedInvoiceBranchId; }
        var storeId = firstRow ? parseInt(firstRow.getAttribute("data-store-id2"), 10) : null;
        if (!storeId) {
            storeId = parseInt(byId("storeId").value, 10) || null;
        }
        if (reviewMode && !storeId) { storeId = loadedInvoiceStoreId; }
        var paymentType = parseInt(byId("paymentType").value, 10) || 0;
        var transactionType = byId("transactionType").value;
        var firstItemId = firstRow ? parseInt(firstRow.getAttribute("data-item-id"), 10) || null : null;
        var isCard = transactionType === "card";
        var payType = transactionType === "violations"
            ? (parseInt(selectedRadioValue("violationPayType"), 10) || 0)
            : 1;

        return {
            Transaction_ID: reviewMode ? savedTransactionId() : null,
            TransactionType: transactionType,
            TransactionDate: currentTransactionDate(),
            BranchId: branchId,
            StoreID: storeId,
            UserID: null,
            Emp_ID: parseInt(byId("empId").value, 10) || (reviewMode ? loadedInvoiceEmpId : null),
            CustomerID: 2,
            TblCusCshId: transactionType === "card" ? parseInt(byId("cashCustomerId").value, 10) || null : null,
            DefaultCustomerId: 2,
            PaymentType: paymentType,
            BoxID: parseInt(byId("boxId").value, 10) || (reviewMode ? loadedInvoiceBoxId : null),
            PayedValue: isCard ? numberValue("netValue") : numberValue("payedValue"),
            NetValue: numberValue("netValue"),
            RemainValue: isCard ? 0 : numberValue("remainValue"),
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
            IPN: isCard ? "" : byId("ipn").value,
            ManualNO: isCard ? "" : byId("manualNo").value,
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
            ItemIDService2: isCard ? null : (parseInt(byId("serviceItemId2").value, 10) || null),
            ViolationPayType: parseInt(selectedRadioValue("violationPayType"), 10) || 0,
            Tet_NumPoket: transactionType === "card" ? byId("cardNationalId").value : byId("tetNumPoket").value,
            IsRecharg: transactionType !== "card" && transactionType !== "violations" && numberValue("rechargeValue") > 0,
            IsWallet: transactionType === "violations" ? false : byId("isWallet").value === "true",
            HaveGuarantee: byId("haveGuarantee").value === "true",
            BankMachineCommission: currentCashOutMachineInfo().bankCommission,
            CashOutMachineWithdrawalAmount: currentCashOutMachineInfo().withdrawalAmount,
            EditPassword: "",
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

    function ensureCommissionReadyForSave() {
        if (!commissionsReady) {
            byId("validationSummary").innerText = "جاري تحميل إعدادات العمولات، برجاء الانتظار لحظات.";
            return false;
        }

        if (commissionCalculationPending) {
            byId("validationSummary").innerText = "جاري تحميل إعدادات العمولات، برجاء الانتظار لحظات.";
            return false;
        }

        var request = buildCommissionRequest();
        if (!request) {
            return true;
        }

        var key = commissionKey(request);
        if (commissionCache[key]) {
            applyCommissionResult(commissionCache[key]);
            lastCommissionKey = key;
            return true;
        }

        markCommissionPending("جاري تحميل إعدادات العمولات، برجاء الانتظار لحظات.");
        calculateCommissionPreview();
        byId("validationSummary").innerText = "تم تحديث قيم العمولات، برجاء مراجعة الإجمالي ثم اضغط حفظ مرة أخرى.";
        return false;
    }

    function maskedReference(value) {
        var text = String(value || "").trim();
        if (text.length <= 6) { return text; }
        return text.substring(0, 4) + "xxxx" + text.substring(text.length - 2);
    }

    function buildSaveConfirmationHtml() {
        var mode = byId("transactionType").value;
        var config = modes[mode] || modes["cash-in"];
        var amount = mode === "violations" ? numberValue("violationValue") : (mode === "card" ? numberValue("netValue") : numberValue("rechargeValue"));
        var wallet = mode === "violations" ? byId("violationWalletNo").value : byId("tetNumPoket").value;
        var reference = mode === "card" ? byId("visaNumber").value : wallet;
        return '<div class="save-confirm-type">' + escapeHtml(config.label) + '</div>' +
            '<div class="save-confirm-amount">' + escapeHtml(decimalText(amount)) + ' جنيه</div>' +
            (reference ? '<div class="save-confirm-reference">' + escapeHtml(maskedReference(reference)) + '</div>' : '') +
            '<dl>' +
            '<div><dt>العمولة</dt><dd>' + escapeHtml(decimalText(numberValue("commissionValue"))) + '</dd></div>' +
            '<div><dt>الضريبة</dt><dd>' + escapeHtml(decimalText(numberValue("vatValue"))) + '</dd></div>' +
            '<div><dt>الإجمالي</dt><dd>' + escapeHtml(decimalText(numberValue("netValue"))) + '</dd></div>' +
            '</dl>';
    }

    function openSaveConfirmation() {
        var panel = byId("saveConfirmPanel");
        if (!panel) { return false; }
        byId("saveConfirmSummary").innerHTML = buildSaveConfirmationHtml();
        panel.classList.add("is-open");
        panel.setAttribute("aria-hidden", "false");
        window.setTimeout(function () {
            if (byId("confirmSaveBtn")) { byId("confirmSaveBtn").focus(); }
        }, 50);
        return true;
    }

    function closeSaveConfirmation() {
        var panel = byId("saveConfirmPanel");
        if (!panel) { return; }
        panel.classList.remove("is-open");
        panel.setAttribute("aria-hidden", "true");
    }

    function saveTransaction(event) {
        event.preventDefault();
        if (saveRequestInFlight || uxIsBusy() || uxDebounced()) {
            uxShowGuide();
            return;
        }
        calculateTotals();
        clearMessages();

        if (!ensureCommissionReadyForSave()) { return; }
        uxApplyFlow();
        if (!uxIsSaveReady()) {
            uxShowGuide();
            uxFocusStep(uxCurrentStep);
            return;
        }
        if (!validateForm()) { return; }

        var request = buildRequest();
        if (!pendingSaveConfirmation) {
            openSaveConfirmation();
            return;
        }
        pendingSaveConfirmation = false;
        if (reviewMode && loadedInvoiceCreatedUserId && currentContext && loadedInvoiceCreatedUserId !== currentContext.UserId) {
            var password = window.prompt("أدخل كلمة مرور المستخدم الحالي لتأكيد تعديل الفاتورة");
            if (!password) {
                byId("validationSummary").innerText = "كلمة المرور غير صحيحة، لم يتم حفظ التعديل";
                return;
            }
            request.EditPassword = password;
        }

        saveRequestInFlight = true;
        uxSaving = true;
        uxSetOverlay(true, "جاري الحفظ...");
        uxApplyFlow();
        byId("saveBtn").disabled = true;
        try {
            requestJsonWithLoading("POST", getUrl("data-save-url"), request, function (status, data) {
                saveRequestInFlight = false;
                uxSaving = false;
                uxSetOverlay(false);
                updateSaveButtonState();

                if (status >= 200 && status < 300 && data && data.success) {
                    var successMessage = "تم الحفظ بنجاح<br />رقم الفاتورة: " + (data.noteSerial1 || "") + "<br />رقم الحركة: " + (data.transactionId || "");
                    lastSavedTransactionId = parseInt(data.transactionId, 10) || null;
                    if (byId("invoiceNumber")) { byId("invoiceNumber").value = data.noteSerial1 || ""; }
                    enablePrintIfAllowed();
                    enableDeleteIfAllowed();
                    enableCancelIfAllowed();
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
            }, "جاري الحفظ...");
        } catch (error) {
            saveRequestInFlight = false;
            uxSaving = false;
            uxSetOverlay(false);
            updateSaveButtonState();
            throw error;
        }
    }

    function showSaveError(data) {
        data = data || {};
        clearAllValidationHighlights();
        var html = "";
        var validationErrors = data.validationErrors || {};
        var hasFieldErrors = applyServerValidationErrors(data);

        if (validationErrors && !Array.isArray(validationErrors) && Object.keys(validationErrors).length) {
            html += '<div class="save-error-block"><strong>أخطاء البيانات:</strong><br />';
            for (var key in validationErrors) {
                if (Object.prototype.hasOwnProperty.call(validationErrors, key)) {
                    html += escapeHtml(validationErrors[key]) + "<br />";
                }
            }
            html += "</div>";
        } else if (Array.isArray(validationErrors) && validationErrors.length) {
            html += '<div class="save-error-block"><strong>أخطاء البيانات:</strong><br />';
            for (var v = 0; v < validationErrors.length; v++) {
                html += escapeHtml(validationErrors[v].message || validationErrors[v].Message || "") + "<br />";
            }
            html += "</div>";
        }

        html += '<div class="save-error-block">السبب: ' + escapeHtml(data.message || "تعذر الحفظ") + "</div>";
        if (posDebugEnabled && data.technicalMessage) {
            html += '<div class="save-error-technical">التفاصيل الفنية: ' + escapeHtml(data.technicalMessage) + "</div>";
        }

        byId("validationSummary").innerHTML = html;
        if (hasFieldErrors) {
            focusFirstInvalidField();
        }
    }

    function loadEmployeeBalances() {
        var url = getUrl("data-balances-url");
        if (!url) { return; }

        requestJsonWithLoading("GET", url, null, function (status, data) {
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
        var typeFilter = byId("todayInvoiceTypeFilter");
        var operationType = typeFilter ? typeFilter.value : "";
        var trimmedTerm = (term || "").trim();
        var list = byId("todayInvoicesList");
        var dateRangeEnabled = byId("todayDateRangeEnabled") && byId("todayDateRangeEnabled").checked;
        var fromDate = dateRangeEnabled && byId("todayFromDate") ? byId("todayFromDate").value : "";
        var toDate = dateRangeEnabled && byId("todayToDate") ? byId("todayToDate").value : "";
        if (trimmedTerm && trimmedTerm.length < 2) {
            if (list) { list.innerText = "اكتب حرفين على الأقل للبحث في الفواتير."; }
            byId("todayInvoiceSummary").innerHTML = "";
            return;
        }

        if (list) { list.innerText = "جاري البحث..."; }
        var excelOnly = byId("todayExcelOnly") && byId("todayExcelOnly").checked;
        requestJson("GET", url + "?term=" + encodeURIComponent(trimmedTerm) + "&operationType=" + encodeURIComponent(operationType || "") + "&excelOnly=" + encodeURIComponent(excelOnly ? "true" : "false") + "&fromDate=" + encodeURIComponent(fromDate) + "&toDate=" + encodeURIComponent(toDate), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                byId("todayInvoicesList").innerText = "تعذر تحميل فواتير اليوم";
                return;
            }

            todayInvoicesCache = data;
            renderTodayInvoices(data, !!trimmedTerm);
        });
    }

    function renderTodayInvoices(invoices, hasSearchTerm) {
        var container = byId("todayInvoicesList");
        container.innerHTML = "";

        if (!invoices || !invoices.length) {
            container.innerText = hasSearchTerm ? "لا توجد نتائج مطابقة" : "لا توجد فواتير اليوم";
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
                '<span>' + escapeHtml(invoices[i].ServiceType || "") + (invoices[i].IsCancelled ? ' | ملغاة' : '') + ' | ' + escapeHtml(invoices[i].TransactionTime || "") + (invoices[i].IsExcelImported ? ' | Excel' : '') + '</span>' +
                '<span>' + escapeHtml(invoices[i].CustomerName || "") + ' - ' + escapeHtml(invoices[i].CustomerPhone || "") + '</span>' +
                '<span>الصافي: ' + escapeHtml(decimalText(invoices[i].NetValue || invoices[i].PayedValue || 0)) + '</span>';
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
            '<div><strong>الحالة:</strong> ' + (invoice.IsCancelled ? "ملغاة" : "سارية") + '</div>' +
            '<div><strong>المصدر:</strong> ' + (invoice.IsExcelImported ? "Excel" : "إدخال يدوي") + '</div>' +
            '<div><strong>العميل:</strong> ' + escapeHtml(invoice.CustomerName || "") + '</div>' +
            '<div><strong>التليفون:</strong> ' + escapeHtml(invoice.CustomerPhone || "") + '</div>' +
            '<div><strong>الصافي:</strong> ' + escapeHtml(decimalText(invoice.NetValue || invoice.PayedValue || 0)) + '</div>' +
            (currentContext && currentContext.CanAdminDeleteInvoice === true
                ? '<button type="button" class="danger-action compact-action" data-delete-invoice="' + escapeHtml(invoice.Transaction_ID || "") + '">حذف الفاتورة</button>'
                : '');
    }

    function salesIndexFirst() {
        return readBool(getPageValue("data-sales-index-first"));
    }

    function showSalesIndex() {
        var indexPanel = byId("salesIndexPanel");
        var entryPanel = byId("salesEntryPanel");
        if (indexPanel) { indexPanel.classList.remove("is-hidden-ui"); }
        if (entryPanel) { entryPanel.classList.add("is-hidden-ui"); }
    }

    function initializePosEntry(openKyc) {
        if (posEntryInitialized) { return; }
        posEntryInitialized = true;
        loadContextControls();
        preloadCommissionSettings(function () {
            var initialMode = openKyc ? "card" : "cash-in";
            setMode(initialMode);
            applyPermissions();
            calculateTotals();
            if (initialMode === "card") {
                openKycModal();
            }
            setCommissionStatus(commissionsReady ? "تم تحميل إعدادات العمولات" : "تعذر تحميل إعدادات العمولات. لا يمكن الحفظ قبل إعادة تحميل الشاشة.", !commissionsReady);
        });
    }

    function showSalesEntry() {
        var indexPanel = byId("salesIndexPanel");
        var entryPanel = byId("salesEntryPanel");
        if (indexPanel) { indexPanel.classList.add("is-hidden-ui"); }
        if (entryPanel) { entryPanel.classList.remove("is-hidden-ui"); }
        initializePosEntry(queryValue("openKyc") === "true");
    }

    function initSalesIndexFilters() {
        var from = byId("salesSearchFromDate");
        var to = byId("salesSearchToDate");
        var today = localIsoDate();
        if (from && !from.value) { from.value = today; }
        if (to && !to.value) { to.value = today; }
    }

    function closeSalesBranchResults() {
        var results = byId("salesSearchBranchResults");
        if (results) {
            results.classList.remove("is-open");
            results.innerHTML = "";
        }
    }

    function setSalesBranch(branchId, branchName) {
        if (byId("salesSearchBranchId")) { byId("salesSearchBranchId").value = branchId || ""; }
        if (byId("salesSearchBranchText")) { byId("salesSearchBranchText").value = branchName || ""; }
        closeSalesBranchResults();
    }

    function renderSalesBranchResults(term) {
        var results = byId("salesSearchBranchResults");
        if (!results) { return; }
        var value = (term || "").trim().toLowerCase();
        var branches = salesBranchCache || [];
        var filtered = [];
        for (var i = 0; i < branches.length; i++) {
            var id = String(branches[i].BranchId || "");
            var name = branches[i].BranchName || "";
            if (!value || (id + " " + name).toLowerCase().indexOf(value) !== -1) {
                filtered.push(branches[i]);
            }
            if (filtered.length >= 30) { break; }
        }

        if (!filtered.length) {
            results.innerHTML = '<span class="lookup-row empty">لا توجد نتائج</span>';
            results.classList.add("is-open");
            return;
        }

        var html = "";
        for (var j = 0; j < filtered.length; j++) {
            html += '<button type="button" class="lookup-row" data-sales-branch-id="' + escapeHtml(filtered[j].BranchId) + '" data-sales-branch-name="' + escapeHtml(filtered[j].BranchName || "") + '">' +
                '<strong>' + escapeHtml(filtered[j].BranchName || ("فرع " + filtered[j].BranchId)) + '</strong>' +
                '<small>' + escapeHtml(filtered[j].BranchId || "") + '</small>' +
                '</button>';
        }
        results.innerHTML = html;
        results.classList.add("is-open");
    }

    function ensureSalesBranchesLoaded(callback) {
        if (salesBranchCache) {
            if (callback) { callback(); }
            return;
        }
        if (salesBranchLoading) { return; }
        salesBranchLoading = true;
        requestJsonWithLoading("GET", getUrl("data-branches-url"), null, function (status, data) {
            salesBranchLoading = false;
            salesBranchCache = (status >= 200 && status < 300 && data && data.length) ? data : [];
            if (callback) { callback(); }
        }, "جاري تحميل الفروع...");
    }

    function renderSalesIndex(invoices, hasSearchTerm) {
        var container = byId("salesIndexResults");
        if (!container) { return; }
        container.innerHTML = "";

        if (!invoices || !invoices.length) {
            container.innerHTML = '<div class="empty-state">' + (hasSearchTerm ? "لا توجد نتائج مطابقة" : "لا توجد فواتير للعرض") + '</div>';
            return;
        }

        var html = '<div class="responsive-table"><table class="pos-table sales-index-table"><thead><tr>' +
            '<th>رقم الفاتورة</th><th>رقم الحركة</th><th>النوع</th><th>الحالة</th><th>المصدر</th><th>التاريخ</th><th>الوقت</th><th>العميل</th><th>الهاتف</th><th>الصافي</th><th>إجراء</th>' +
            '</tr></thead><tbody>';
        for (var i = 0; i < invoices.length; i++) {
            var invoice = invoices[i] || {};
            html += '<tr>' +
                '<td>' + escapeHtml(invoice.NoteSerial1 || "") + '</td>' +
                '<td>' + escapeHtml(invoice.Transaction_ID || "") + '</td>' +
                '<td>' + escapeHtml(invoice.ServiceType || "") + '</td>' +
                '<td>' + (invoice.IsCancelled ? '<span class="manual-badge">ملغاة</span>' : '<span class="manual-badge">سارية</span>') + '</td>' +
                '<td>' + (invoice.IsExcelImported ? '<span class="excel-badge">Excel</span>' : '<span class="manual-badge">يدوي</span>') + '</td>' +
                '<td>' + escapeHtml(invoice.TransactionDate || "") + '</td>' +
                '<td>' + escapeHtml(invoice.TransactionTime || "") + '</td>' +
                '<td>' + escapeHtml(invoice.CustomerName || "") + '</td>' +
                '<td>' + escapeHtml(invoice.CustomerPhone || "") + '</td>' +
                '<td>' + escapeHtml(decimalText(invoice.NetValue || invoice.PayedValue || 0)) + '</td>' +
                '<td><button type="button" class="secondary-action compact-action" data-sales-open="' + escapeHtml(invoice.Transaction_ID || "") + '">عرض</button>' +
                (currentContext && currentContext.CanAdminDeleteInvoice === true ? ' <button type="button" class="danger-action compact-action" data-delete-invoice="' + escapeHtml(invoice.Transaction_ID || "") + '">حذف</button>' : '') +
                '</td>' +
                '</tr>';
        }
        html += '</tbody></table></div>';
        container.innerHTML = html;
    }

    function loadSalesIndexInvoices() {
        var url = getUrl("data-today-invoices-url");
        var termInput = byId("salesSearchText");
        var typeFilter = byId("salesSearchType");
        var fromDateInput = byId("salesSearchFromDate");
        var toDateInput = byId("salesSearchToDate");
        var branchInput = byId("salesSearchBranchId");
        var excelOnlyInput = byId("salesExcelOnly");
        var term = termInput ? termInput.value.trim() : "";
        var operationType = typeFilter ? typeFilter.value : "";
        var fromDate = fromDateInput ? fromDateInput.value : "";
        var toDate = toDateInput ? toDateInput.value : "";
        var branchId = branchInput ? branchInput.value : "";
        var excelOnly = excelOnlyInput && excelOnlyInput.checked;
        var message = byId("salesIndexMessage");
        if (!url) { return; }

        if (term && term.length < 2) {
            if (message) { message.innerText = "اكتب حرفين على الأقل للبحث."; }
            return;
        }

        if (message) { message.innerText = ""; }
        if (byId("salesIndexResults")) { byId("salesIndexResults").innerHTML = '<div class="empty-state">جاري البحث...</div>'; }
        requestJsonWithLoading("GET", url + "?term=" + encodeURIComponent(term) +
            "&operationType=" + encodeURIComponent(operationType || "") +
            "&fromDate=" + encodeURIComponent(fromDate || "") +
            "&toDate=" + encodeURIComponent(toDate || "") +
            "&branchId=" + encodeURIComponent(branchId || "") +
            "&excelOnly=" + encodeURIComponent(excelOnly ? "true" : "false"), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                if (message) { message.innerText = "تعذر تحميل الفواتير."; }
                renderSalesIndex([], !!term);
                return;
            }
            salesIndexCache = data;
            renderSalesIndex(data, !!term);
        }, "جاري تحميل الفواتير...");
    }

    function deleteInvoice(transactionId) {
        transactionId = parseInt(transactionId, 10) || 0;
        if (!transactionId || !currentContext || currentContext.CanAdminDeleteInvoice !== true) {
            return;
        }

        if (!window.confirm("سيتم حذف الفاتورة وكل تفاصيلها وسنداتها وقيودها المرتبطة. هل تريد المتابعة؟")) {
            return;
        }

        var password = window.prompt("أدخل كلمة مرور المدير لتأكيد الحذف");
        if (!password) {
            byId("validationSummary").innerText = "لم يتم الحذف: كلمة المرور مطلوبة.";
            return;
        }

        requestJsonWithLoading("POST", getUrl("data-delete-invoice-url"), {
            TransactionId: transactionId,
            AdminPassword: password
        }, function (status, data) {
            if (status < 200 || status >= 300 || !data || data.success === false) {
                byId("validationSummary").innerText = (data && data.message) || "تعذر حذف الفاتورة.";
                return;
            }

            byId("saveResult").innerText = data.message || "تم حذف الفاتورة.";
            if (lastSavedTransactionId === transactionId) {
                reloadContextAndReset(data.message || "تم حذف الفاتورة.");
            }
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
            if (salesIndexFirst()) { loadSalesIndexInvoices(); }
        }, "جاري حذف الفاتورة...");
    }

    function cancelInvoice(transactionId) {
        transactionId = parseInt(transactionId, 10) || 0;
        if (!transactionId || !currentContext || currentContext.CanCancelInvoice !== true) {
            return;
        }

        var password = window.prompt("أدخل كلمة مرور المستخدم الحالي لتأكيد الإلغاء");
        if (!password) {
            byId("validationSummary").innerText = "لم يتم الإلغاء: كلمة المرور مطلوبة.";
            return;
        }

        var reason = window.prompt("سبب الإلغاء (اختياري)");
        requestJsonWithLoading("POST", getUrl("data-cancel-invoice-url"), {
            TransactionId: transactionId,
            Password: password,
            CancelReason: reason || ""
        }, function (status, data) {
            if (status < 200 || status >= 300 || !data || data.success === false) {
                byId("validationSummary").innerText = (data && data.message) || "تعذر إلغاء الفاتورة.";
                return;
            }

            byId("saveResult").innerText = data.message || "تم إلغاء الفاتورة.";
            loadInvoiceForReview(transactionId);
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
            if (salesIndexFirst()) { loadSalesIndexInvoices(); }
        }, "جاري إلغاء الفاتورة...");
    }

    function deleteExcelInvoicesForRange() {
        if (!currentContext || currentContext.CanAdminDeleteInvoice !== true) { return; }
        var fromDate = byId("salesSearchFromDate") ? byId("salesSearchFromDate").value : "";
        var toDate = byId("salesSearchToDate") ? byId("salesSearchToDate").value : "";
        var branchId = byId("salesSearchBranchId") ? byId("salesSearchBranchId").value : "";
        var operationType = byId("salesSearchType") ? byId("salesSearchType").value : "";
        if (!fromDate || !toDate) {
            byId("salesIndexMessage").innerText = "حدد من تاريخ وإلى تاريخ قبل حذف فواتير Excel.";
            return;
        }

        if (!window.confirm("سيتم حذف كل فواتير Excel داخل الفترة المحددة وكل آثارها. هل تريد المتابعة؟")) {
            return;
        }

        var password = window.prompt("أدخل كلمة مرور المدير لتأكيد حذف فواتير Excel");
        if (!password) {
            byId("salesIndexMessage").innerText = "لم يتم الحذف: كلمة المرور مطلوبة.";
            return;
        }

        requestJsonWithLoading("POST", getUrl("data-delete-excel-invoices-url"), {
            FromDate: fromDate,
            ToDate: toDate,
            BranchId: branchId ? parseInt(branchId, 10) : null,
            OperationType: operationType || "",
            AdminPassword: password
        }, function (status, data) {
            if (status < 200 || status >= 300 || !data || data.success === false) {
                byId("salesIndexMessage").innerText = (data && data.message) || "تعذر حذف فواتير Excel.";
                return;
            }

            byId("salesIndexMessage").innerText = data.message || "تم حذف فواتير Excel.";
            loadSalesIndexInvoices();
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
        }, "جاري حذف فواتير Excel...");
    }

    function setTodayInvoicesCollapsed(collapsed, persist) {
        var page = byId("posPage");
        var button = byId("toggleTodayInvoicesBtn");
        if (!page || !button) { return; }

        page.classList.toggle("today-invoices-collapsed", collapsed);
        button.innerText = collapsed ? "إظهار" : "إخفاء";
        button.setAttribute("aria-expanded", collapsed ? "false" : "true");
        if (persist && window.localStorage) {
            window.localStorage.setItem("posTodayInvoicesCollapsed", collapsed ? "true" : "false");
        }
    }

    function initTodayInvoicesPanelState() {
        var collapsed = false;
        if (window.localStorage && window.localStorage.getItem("posTodayInvoicesCollapsed") === "true") {
            collapsed = true;
        }
        if (window.innerWidth && window.innerWidth <= 1180 && (!window.localStorage || !window.localStorage.getItem("posTodayInvoicesCollapsed"))) {
            collapsed = true;
        }
        setTodayInvoicesCollapsed(collapsed, false);
    }

    function clearJournalEntry() {
        var panel = byId("journalEntryPanel");
        if (panel) { panel.classList.add("is-hidden-ui"); }
        if (byId("journalEntryMessage")) { byId("journalEntryMessage").innerText = ""; }
        if (byId("journalEntryGrid")) { byId("journalEntryGrid").innerHTML = ""; }
    }

    function renderJournalEntries(entries) {
        var grid = byId("journalEntryGrid");
        if (!grid) { return; }
        if (!entries || !entries.length) {
            grid.innerHTML = '<div class="empty-state">لا يوجد قيد محاسبي مرتبط بهذه الحركة</div>';
            return;
        }

        var totalDebit = 0;
        var totalCredit = 0;
        var html = '<div class="responsive-table"><table class="pos-table"><thead><tr>' +
            '<th>رقم القيد</th><th>التاريخ</th><th>رقم الحساب</th><th>اسم الحساب</th><th>البيان</th><th>مدين</th><th>دائن</th>' +
            '</tr></thead><tbody>';
        for (var i = 0; i < entries.length; i++) {
            var row = entries[i];
            var debit = parseFloat(row.Debit || 0) || 0;
            var credit = parseFloat(row.Credit || 0) || 0;
            totalDebit += debit;
            totalCredit += credit;
            html += '<tr>' +
                '<td>' + escapeHtml(row.NoteSerial || "") + '</td>' +
                '<td>' + escapeHtml(row.RecordDateText || dateDisplayValue(row.RecordDate)) + '</td>' +
                '<td>' + escapeHtml(row.AccountSerial || row.AccountCode || "") + '</td>' +
                '<td>' + escapeHtml(row.AccountName || "") + '</td>' +
                '<td>' + escapeHtml(row.Description || "") + '</td>' +
                '<td>' + decimalText(debit) + '</td>' +
                '<td>' + decimalText(credit) + '</td>' +
                '</tr>';
        }
        html += '<tr class="journal-total-row"><td colspan="5">الإجمالي</td><td>' + decimalText(totalDebit) + '</td><td>' + decimalText(totalCredit) + '</td></tr>';
        html += '</tbody></table></div>';
        grid.innerHTML = html;
    }

    function loadJournalEntry(transactionId) {
        var panel = byId("journalEntryPanel");
        if (!panel || !currentContext || currentContext.CanViewJournalEntry !== true) { return; }
        transactionId = parseInt(transactionId || savedTransactionId(), 10) || 0;
        if (transactionId <= 0) {
            clearJournalEntry();
            return;
        }

        panel.classList.remove("is-hidden-ui");
        byId("journalEntryMessage").innerText = "جاري تحميل القيد المحاسبي...";
        requestJson("GET", getUrl("data-journal-url") + "?transactionId=" + encodeURIComponent(transactionId), null, function (status, data) {
            if (status < 200 || status >= 300 || !data || data.success === false) {
                byId("journalEntryMessage").innerText = (data && data.message) || "تعذر تحميل القيد المحاسبي";
                byId("journalEntryGrid").innerHTML = "";
                return;
            }
            byId("journalEntryMessage").innerText = "";
            renderJournalEntries(data.entries || []);
        });
    }

    function setSelectSingleOption(selectId, value, text) {
        var select = byId(selectId);
        select.innerHTML = "";
        select.appendChild(new Option(text || value || "", value || ""));
        select.value = value || "";
    }

    function ensureSelectOption(selectId, value, text) {
        var select = byId(selectId);
        if (!select || value === null || value === undefined || value === "") { return; }
        value = String(value);
        for (var i = 0; i < select.options.length; i++) {
            if (String(select.options[i].value) === value) {
                select.value = value;
                return;
            }
        }
        select.appendChild(new Option(text || value, value));
        select.value = value;
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
        var number = parseMoney(value);
        if (isNaN(number)) { number = 0; }
        try {
            return number.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        } catch (ignore) {
            return number.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ",");
        }
    }

    function formatMoneyInput(input) {
        if (!input || input.readOnly || input.disabled) { return; }
        var value = parseMoney(input.value);
        if (value > 0 || String(input.value || "").trim()) {
            input.value = decimalText(value);
        }
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

    function firstTextValue() {
        for (var i = 0; i < arguments.length; i++) {
            if (arguments[i] !== null && arguments[i] !== undefined) {
                var value = String(arguments[i]).trim();
                if (value) { return value; }
            }
        }
        return "";
    }

    function setInvoiceIdentityFields(data) {
        if (!data) { return; }

        // Legacy POS mapping:
        // Transactions.IPN is shown in the UI as "ID".
        // Transactions.ManualNO is shown in the UI as "IPN".
        byId("ipn").value = firstTextValue(data.IPN, data.Ipn, data.ID, data.Id, data.NoID);
        byId("manualNo").value = firstTextValue(data.ManualNO, data.ManualNo, data.ManualNumber, data.IPNManual);
    }

    function loadInvoiceForReview(transactionId) {
        if (!transactionId) { return; }

        requestJson("GET", getUrl("data-invoice-url") + "?transactionId=" + encodeURIComponent(transactionId), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) {
                byId("validationSummary").innerText = "تعذر فتح الفاتورة";
                return;
            }

            lastSavedTransactionId = parseInt(data.Transaction_ID || transactionId, 10) || null;
            loadedInvoiceIsCancelled = data.IsCancelled === true;
            loadedInvoiceCreatedUserId = parseInt(data.CreatedUserId, 10) || null;
            loadedInvoiceBranchId = parseInt(data.BranchId, 10) || null;
            loadedInvoiceStoreId = parseInt(data.StoreID, 10) || null;
            loadedInvoiceBoxId = parseInt(data.BoxID, 10) || null;
            loadedInvoiceEmpId = parseInt(data.Emp_ID, 10) || null;
            reviewMode = true;
            setMode(data.TransactionType || "cash-in", true);
            if (byId("transactionDate")) {
                byId("transactionDate").value = dateInputValue(data.TransactionDate);
                byId("transactionDate").disabled = true;
            }
            if (byId("invoiceNumber")) { byId("invoiceNumber").value = data.NoteSerial1 || ""; }
            lastCashOutBankMachineCommission = (data.TransactionType || "") === "cash-out" ? (parseFloat(data.BankMachineCommission) || 0) : 0;
            lastCashOutMachineWithdrawalAmount = (data.TransactionType || "") === "cash-out" ? (parseFloat(data.CashOutMachineWithdrawalAmount) || 0) : 0;
            if (data.BranchId) { ensureSelectOption("branchId", data.BranchId, data.BranchName || ("فرع " + data.BranchId)); }
            if (data.StoreID) {
                byId("storeId").value = data.StoreID;
                byId("storeName").value = data.StoreName || byId("storeName").value || "";
            }
            if (data.BoxID) { ensureSelectOption("boxId", data.BoxID, data.BoxName || ("خزنة " + data.BoxID)); }
            if (data.Emp_ID) { byId("empId").value = data.Emp_ID; }
            byId("cashCustomerPhone").value = data.CashCustomerPhone || "";
            byId("cashCustomerName").value = data.CashCustomerName || "";
            setInvoiceIdentityFields(data);
            byId("phone2").value = data.Phone2 || "";
            byId("visaNumber").value = data.VisaNumber || "";
            byId("paymentCardNo").value = data.VisaNumber || "";
            byId("tetNumPoket").value = textReference(data.Tet_NumPoket);
            refreshPhoneInputs();
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
                var reviewRow = getFirstSelectedItemRow();
                if (reviewRow && (data.TransactionType || "") === "cash-out") {
                    reviewRow.setAttribute("data-bank-machine-commission", lastCashOutBankMachineCommission);
                    reviewRow.setAttribute("data-cashout-machine-withdrawal", lastCashOutMachineWithdrawalAmount);
                }
            }
            bindReviewServiceSelects(data);

            byId("rechargeValue").value = decimalText(data.RechargeValue);
            byId("commissionValue").value = decimalText(data.NetValue);
            byId("vatValue").value = decimalText(data.VatValue);
            byId("totalFees").value = decimalText(data.TotalFees);
            byId("netValue").value = decimalText((parseFloat(data.RechargeValue) || 0) + (parseFloat(data.TotalFees) || 0));
            byId("payedValue").value = decimalText(data.PayedValue);
            byId("remainValue").value = decimalText(data.RemainValue);

            byId("branchId").disabled = true;
            setInvoiceIdentityFields(data);
            updateSaveButtonState();
            byId("saveResult").innerHTML = "وضع مراجعة فقط - رقم الفاتورة: " + escapeHtml(data.NoteSerial1 || "") + "<br />رقم الحركة: " + escapeHtml(data.Transaction_ID || "");
            byId("cancelledBanner").style.display = loadedInvoiceIsCancelled ? "" : "none";
            enablePrintIfAllowed();
            enableDeleteIfAllowed();
            enableCancelIfAllowed();
            loadJournalEntry(lastSavedTransactionId);
            updateCashOutMachineDisplay();
            updateBottomSummary();
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
        requestJsonWithLoading("GET", getUrl("data-payment-types-url"), null, function (status, data) {
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

    function seedChangeableContextControls() {
        applyDefaultContextControls();
        byId("branchId").disabled = false;
        byId("paymentType").disabled = false;
        byId("boxId").disabled = true;
        byId("storeName").readOnly = true;
        byId("bankName").readOnly = true;
    }

    function loadContextControls() {
        if (!currentContext || currentContext.CanChangeDefaults !== true) {
            applyDefaultContextControls();
            return;
        }

        seedChangeableContextControls();
    }

    function ensureContextControlsLoaded() {
        if (!currentContext || currentContext.CanChangeDefaults !== true || contextControlsLoaded || contextControlsLoading) {
            return;
        }

        contextControlsLoading = true;
        populatePaymentTypes();
        populateCashBoxes();
        populateBranches();
        loadStoresForBranch(contextValue("BranchId", "data-default-branch-id"));
        byId("paymentType").disabled = false;
        byId("bankName").readOnly = true;
        contextControlsLoaded = true;
        contextControlsLoading = false;
    }

    function populateCashBoxes() {
        requestJsonWithLoading("GET", getUrl("data-cash-boxes-url"), null, function (status, data) {
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
            if (reviewMode && loadedInvoiceBoxId) {
                ensureSelectOption("boxId", loadedInvoiceBoxId, "خزنة " + loadedInvoiceBoxId);
            } else if (defaultBoxId) {
                select.value = defaultBoxId;
            }
            select.disabled = true;
        });
    }

    function populateBranches() {
        requestJsonWithLoading("GET", getUrl("data-branches-url"), null, function (status, data) {
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
            if (reviewMode && loadedInvoiceBranchId) {
                ensureSelectOption("branchId", loadedInvoiceBranchId, "فرع " + loadedInvoiceBranchId);
            } else if (defaultBranchId) {
                select.value = defaultBranchId;
            }
            select.disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        });
    }

    function loadStoresForBranch(branchId) {
        requestJsonWithLoading("GET", getUrl("data-stores-url") + "?branchId=" + encodeURIComponent(branchId || ""), null, function (status, data) {
            if (status < 200 || status >= 300 || !data || !data.length) { return; }

            var defaultStoreId = parseInt(contextValue("StoreID", "data-default-store-id"), 10) || 0;
            if (reviewMode && loadedInvoiceStoreId) {
                byId("storeId").value = loadedInvoiceStoreId;
                if (!byId("storeName").value) {
                    byId("storeName").value = "مخزن " + loadedInvoiceStoreId;
                }
                return;
            }
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
        row.querySelector(".line-total").value = decimalText(selected.TotalPrice || ((parseFloat(selected.Price) || 0) + (parseFloat(selected.Vat) || 0)));

        if (selected.BranchId && !byId("branchId").value && currentContext && currentContext.CanChangeDefaults === true) {
            byId("branchId").value = selected.BranchId;
        }

        if (!suppressRecalc) {
            recalculateInvoiceSummary({ source: "applyServiceItem", requestCommission: true });
        }
    }

    function applySelectedItem(input) {
        applyServiceItem(input.closest("tr"), itemLookup[input.value]);
    }

    function setSelectLoading(select, loadingText) {
        select.innerHTML = "";
        var option = document.createElement("option");
        option.value = "";
        option.text = loadingText || "تحميل...";
        select.appendChild(option);
        select.disabled = true;
    }

    function populateServiceSelect(select, data, emptyText, selectedValue, selectedText) {
        select.innerHTML = "";
        select.disabled = false;
        if (!data || !data.length) {
            var emptyOption = document.createElement("option");
            emptyOption.value = "";
            emptyOption.text = emptyText || "لا توجد بيانات";
            select.appendChild(emptyOption);
            if (selectedValue) {
                var fallbackOption = document.createElement("option");
                fallbackOption.value = selectedValue;
                fallbackOption.text = selectedText || selectedValue;
                select.appendChild(fallbackOption);
                select.value = String(selectedValue);
                return selectedValue;
            }
            return null;
        }

        for (var i = 0; i < data.length; i++) {
            var option = document.createElement("option");
            option.value = data[i].Id;
            option.text = data[i].Name;
            select.appendChild(option);
        }

        if (selectedValue) {
            var wanted = String(selectedValue);
            for (var x = 0; x < select.options.length; x++) {
                if (select.options[x].value === wanted) {
                    select.value = wanted;
                    return selectedValue;
                }
            }

            var selectedOption = document.createElement("option");
            selectedOption.value = selectedValue;
            selectedOption.text = selectedText || selectedValue;
            select.appendChild(selectedOption);
            select.value = String(selectedValue);
            return selectedValue;
        }

        return data[0].Id;
    }

    function secondaryCacheKey(mode, itemId) {
        return (mode || "") + "|" + (itemId || "");
    }

    function preloadCommissionSettings(callback) {
        commissionsReady = false;
        updateSaveButtonState();
        setCommissionStatus("جاري تحميل إعدادات العمولات...");

        requestJsonWithLoading("GET", getUrl("data-commission-bootstrap-url"), null, function (status, data) {
            var services = data && (data.primaryServices || data.PrimaryServices);
            if (status >= 200 && status < 300 && services) {
                primaryServiceCache["cash-in"] = services["cash-in"] || services.CashIn || services.cashIn || [];
                primaryServiceCache["cash-out"] = services["cash-out"] || services.CashOut || services.cashOut || [];
                primaryServiceCache.card = services.card || services.Card || [];
                primaryServiceCache.violations = services.violations || services.Violations || [];
                commissionsReady = true;
            } else {
                commissionsReady = false;
            }

            updateSaveButtonState();
            setCommissionStatus(commissionsReady ? "تم تحميل إعدادات العمولات" : "تعذر تحميل إعدادات العمولات. لا يمكن الحفظ قبل إعادة تحميل الشاشة.", !commissionsReady);
            if (callback) { callback(); }
        });
    }

    function loadPrimaryServiceItems(mode) {
        var requestedMode = mode || "cash-in";
        if (reviewMode) { return; }
        if (primaryServiceCache[requestedMode]) {
            var cachedSelectedId = populateServiceSelect(byId("serviceItemId"), primaryServiceCache[requestedMode], "");
            loadSecondaryServiceItems(requestedMode, cachedSelectedId);
            loadDefaultServiceItem(requestedMode, false, cachedSelectedId);
            return;
        }

        setSelectLoading(byId("serviceItemId"), "تحميل...");
        requestJsonWithLoading("GET", getUrl("data-primary-services-url") + "?serviceType=" + encodeURIComponent(requestedMode), null, function (status, data) {
            if (reviewMode || byId("transactionType").value !== requestedMode) {
                return;
            }

            if (status < 200 || status >= 300 || !data || !data.length) {
                byId("validationSummary").innerText = "تعذر تحميل نوع الشحن لهذا النوع";
                populateServiceSelect(byId("serviceItemId"), [], "تعذر التحميل");
                return;
            }

            primaryServiceCache[requestedMode] = data;
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
        if (reviewMode) { return; }

        if (hidden || !itemId) {
            populateServiceSelect(select, [], hidden ? "" : "لا توجد بيانات");
            return;
        }

        var cacheKey = secondaryCacheKey(mode, itemId);
        if (secondaryServiceCache[cacheKey]) {
            populateServiceSelect(select, secondaryServiceCache[cacheKey], "");
            return;
        }

        setSelectLoading(select, "تحميل...");
        requestJsonWithLoading("GET", getUrl("data-secondary-services-url") + "?serviceType=" + encodeURIComponent(mode) + "&itemId=" + encodeURIComponent(itemId), null, function (status, data) {
            if (reviewMode || byId("transactionType").value !== mode || byId("serviceItemId").value !== String(itemId)) {
                return;
            }

            if (status < 200 || status >= 300 || !data) {
                populateServiceSelect(select, [], "تعذر التحميل");
                return;
            }

            secondaryServiceCache[cacheKey] = data || [];
            populateServiceSelect(select, data, "");
        });
    }

    function bindReviewSecondaryServiceItems(mode, itemId, selectedValue, selectedText) {
        var select = byId("serviceItemId2");
        var hidden = mode === "card" || mode === "violations";
        if (hidden || !itemId) {
            populateServiceSelect(select, [], hidden ? "" : "لا توجد بيانات", selectedValue, selectedText);
            return;
        }

        var cacheKey = secondaryCacheKey(mode, itemId);
        if (secondaryServiceCache[cacheKey]) {
            populateServiceSelect(select, secondaryServiceCache[cacheKey], "", selectedValue, selectedText);
            return;
        }

        setSelectLoading(select, "تحميل...");
        requestJsonWithLoading("GET", getUrl("data-secondary-services-url") + "?serviceType=" + encodeURIComponent(mode) + "&itemId=" + encodeURIComponent(itemId), null, function (status, data) {
            if (byId("transactionType").value !== mode || byId("serviceItemId").value !== String(itemId)) {
                return;
            }

            if (status < 200 || status >= 300 || !data) {
                populateServiceSelect(select, [], "تعذر التحميل", selectedValue, selectedText);
                return;
            }

            secondaryServiceCache[cacheKey] = data || [];
            populateServiceSelect(select, secondaryServiceCache[cacheKey], "", selectedValue, selectedText);
        });
    }

    function bindReviewServiceSelects(data) {
        var mode = data.TransactionType || "cash-in";
        var firstItem = data.Items && data.Items.length ? data.Items[0] : null;
        var primaryValue = data.ItemIDService || (firstItem ? firstItem.Item_ID : "");
        var primaryText = data.ItemIDServiceName || (firstItem ? firstItem.ItemName : "") || (primaryValue ? String(primaryValue) : "");
        var secondaryValue = data.ItemIDService2 || "";
        var secondaryText = data.ItemIDService2Name || (secondaryValue ? String(secondaryValue) : "");

        if (primaryServiceCache[mode]) {
            var selectedPrimary = populateServiceSelect(byId("serviceItemId"), primaryServiceCache[mode], "", primaryValue, primaryText);
            bindReviewSecondaryServiceItems(mode, selectedPrimary || primaryValue, secondaryValue, secondaryText);
            return;
        }

        setSelectLoading(byId("serviceItemId"), "تحميل...");
        requestJsonWithLoading("GET", getUrl("data-primary-services-url") + "?serviceType=" + encodeURIComponent(mode), null, function (status, services) {
            if (byId("transactionType").value !== mode) {
                return;
            }

            if (status < 200 || status >= 300 || !services || !services.length) {
                populateServiceSelect(byId("serviceItemId"), [], "تعذر التحميل", primaryValue, primaryText);
                bindReviewSecondaryServiceItems(mode, primaryValue, secondaryValue, secondaryText);
                return;
            }

            primaryServiceCache[mode] = services;
            var selectedPrimary = populateServiceSelect(byId("serviceItemId"), services, "", primaryValue, primaryText);
            bindReviewSecondaryServiceItems(mode, selectedPrimary || primaryValue, secondaryValue, secondaryText);
        });
    }

    function loadDefaultServiceItem(mode, append, itemId) {
        var requestedMode = mode || "cash-in";
        var requestId = ++serviceLoadSequence;
        var url = getUrl("data-default-service-url") + "?serviceType=" + encodeURIComponent(requestedMode);
        if (itemId) {
            url += "&itemId=" + encodeURIComponent(itemId);
        }

        markCommissionPending("جاري تحميل خدمة كيشني وإعدادات العمولات...");
        requestJsonWithLoading("GET", url, null, function (status, data) {
            if (reviewMode || requestId !== serviceLoadSequence || byId("transactionType").value !== requestedMode) {
                return;
            }

            if (status < 200 || status >= 300 || !data || !data.length) {
                byId("validationSummary").innerText = "تعذر تحميل خدمة كيشني لهذا النوع";
                commissionCalculationPending = false;
                updateSaveButtonState();
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
        if (!savedKycCustomerId()) {
            byId("kycCreatedBranch").value = activeKycBranchName();
            byId("kycCreatedDate").value = byId("kycCreatedDate").value || defaultKycCreatedDate();
        }
        refreshKycMetadataDisplay();
        loadKycAttachments();
        scheduleKycCardAvailabilityCheck();
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

    function dateTimeForDisplay(value) {
        if (!value) { return ""; }
        if (typeof value === "string" && value.indexOf("/Date(") === 0) {
            var ticks = parseInt(value.replace(/[^0-9-]/g, ""), 10);
            if (!isNaN(ticks)) { value = ticks; }
        }

        var parsed = new Date(value);
        if (isNaN(parsed.getTime())) {
            return String(value || "").trim();
        }

        var year = parsed.getFullYear();
        var month = ("0" + (parsed.getMonth() + 1)).slice(-2);
        var day = ("0" + parsed.getDate()).slice(-2);
        var hours = parsed.getHours();
        var minutes = ("0" + parsed.getMinutes()).slice(-2);
        var suffix = hours >= 12 ? "PM" : "AM";
        var hour12 = hours % 12;
        if (hour12 === 0) { hour12 = 12; }
        return year + "-" + month + "-" + day + " " + ("0" + hour12).slice(-2) + ":" + minutes + " " + suffix;
    }

    function activeKycBranchName() {
        var branchSelect = byId("branchId");
        if (branchSelect && branchSelect.value) {
            var text = selectedText(branchSelect);
            if (text && text !== "تحميل...") { return text; }
        }

        return contextValue("BranchName", "data-default-kyc-created-branch")
            || contextValue("BranchName", "data-default-branch-name")
            || "جلسة POS غير مكتملة";
    }

    function defaultKycCreatedDate() {
        return getPageValue("data-default-kyc-created-date") || dateTimeForDisplay(new Date());
    }

    function ensureKycMetadataDefaults() {
        if (!byId("kycCreatedBranch").value) {
            byId("kycCreatedBranch").value = activeKycBranchName();
        }
        if (!byId("kycCreatedDate").value) {
            byId("kycCreatedDate").value = defaultKycCreatedDate();
        }
    }

    function setTextIfExists(id, value) {
        var element = byId(id);
        if (element) { element.innerText = value || "جلسة POS غير مكتملة"; }
    }

    function refreshKycMetadataDisplay() {
        ensureKycMetadataDefaults();
        setTextIfExists("kycCreatedBranchDisplay", byId("kycCreatedBranch").value || activeKycBranchName());
        setTextIfExists("kycCreatedDateDisplay", byId("kycCreatedDate").value || defaultKycCreatedDate());
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

    function kycDateValidationMessage(id, label) {
        var value = (byId(id).value || "").trim();
        if (!value) { return ""; }
        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
        if (!match) {
            return label + " غير صحيح. برجاء إدخال التاريخ بصيغة صحيحة.";
        }

        var year = parseInt(match[1], 10);
        if (year < 1753 || year > 9999) {
            return label + " غير صحيح. برجاء إدخال سنة كاملة وصحيحة مثل 2026.";
        }

        var parsed = new Date(value + "T00:00:00");
        if (isNaN(parsed.getTime())
            || parsed.getFullYear() !== year
            || parsed.getMonth() + 1 !== parseInt(match[2], 10)
            || parsed.getDate() !== parseInt(match[3], 10)) {
            return label + " غير صحيح. برجاء مراجعة اليوم والشهر والسنة.";
        }

        return "";
    }

    function validateKycDatesBeforeSave() {
        var errors = [
            kycDateValidationMessage("kycBirthDate", "تاريخ الميلاد"),
            kycDateValidationMessage("kycCardDate", "تاريخ الإصدار"),
            kycDateValidationMessage("kycCardEndDate", "تاريخ الانتهاء")
        ].filter(function (message) { return !!message; });

        if (errors.length) {
            var message = errors.join("\n");
            byId("validationSummary").innerText = message;
            setKycMessage(message, true);
            return false;
        }

        return true;
    }

    function applyKeshniCustomer(data) {
        if (!data) { return; }
        kycArabicNameSource = "";
        kycEnglishNameSource = "";
        if (data.CustomerID) { byId("cashCustomerId").value = data.CustomerID; }
        enablePrintAcknowledgmentIfAllowed();
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
        byId("kycArabicName3").value = data.ArabicName3 || "";
        byId("kycEnglishName0").value = data.EnglishName0 || "";
        byId("kycEnglishName1").value = data.EnglishName1 || "";
        byId("kycEnglishName2").value = data.EnglishName2 || "";
        byId("kycEnglishName3").value = data.EnglishName3 || "";
        byId("kycEnglishName5").value = data.EnglishName5 || "";
        byId("kycEnglishName6").value = data.EnglishName6 || "";
        byId("kycEnglishName7").value = data.EnglishName7 || "";
        byId("kycPhoneNo2").value = data.Phone2 || data.Phone || byId("cashCustomerPhone").value;
        byId("kycPhoneNo").value = data.Phone || "";
        byId("kycCardNo").value = data.VisaNumber || "";
        byId("kycCardSource").value = data.CardSource || "";
        byId("kycCreatedBranch").value = data.BranchName || (data.BranchId ? ("فرع " + data.BranchId) : activeKycBranchName());
        byId("kycCreatedDate").value = dateTimeForDisplay(data.CreatedDate) || defaultKycCreatedDate();
        refreshKycMetadataDisplay();
        byId("kycBirthDate").value = dateForInput(data.BirthDate);
        byId("kycCardDate").value = dateForInput(data.CardDate);
        byId("kycCardEndDate").value = dateForInput(data.CardEndDate);
        byId("kycAddress").value = data.Address || "";
        byId("kycMailAddress").value = data.MailAdress || "";
        byId("kycTel").value = data.Tel || "";
        byId("kycCard").value = data.CardSerial || "";
        refreshPhoneInputs();
    }

    function kycFileTypeLabel(fileName) {
        var extension = ((fileName || "").split(".").pop() || "file").toLowerCase();
        if (extension === "jpg" || extension === "jpeg" || extension === "png" || extension === "gif" || extension === "webp") { return "IMG"; }
        if (extension === "pdf") { return "PDF"; }
        if (extension === "doc" || extension === "docx") { return "DOC"; }
        return extension.substring(0, 4).toUpperCase() || "FILE";
    }

    function formatFileSize(bytes) {
        bytes = parseInt(bytes, 10) || 0;
        if (bytes >= 1024 * 1024) { return (bytes / 1024 / 1024).toFixed(1) + " MB"; }
        if (bytes >= 1024) { return Math.ceil(bytes / 1024) + " KB"; }
        return bytes + " B";
    }

    function renderSelectedKycFiles() {
        var input = byId("kycAttachments");
        var container = byId("kycSelectedAttachmentsList");
        if (!input || !container) { return; }

        var files = input.files || [];
        if (!files.length) {
            container.innerHTML = "";
            return;
        }

        var html = ['<div class="kyc-attachments-grid">'];
        for (var i = 0; i < files.length; i++) {
            html.push('<div class="kyc-attachment-card"><span class="kyc-file-icon">' +
                escapeHtml(kycFileTypeLabel(files[i].name || "")) + '</span><span class="kyc-attachment-info"><strong>' +
                escapeHtml(files[i].name || "مرفق جديد") + '</strong><span>جاهز للرفع - ' +
                escapeHtml(formatFileSize(files[i].size)) + '</span></span>' +
                '<button type="button" class="link-action kyc-remove-file" data-remove-kyc-file="' + i + '">إزالة</button></div>');
        }
        html.push("</div>");
        container.innerHTML = html.join("");
    }

    function removeSelectedKycFile(index) {
        var input = byId("kycAttachments");
        if (!input || !input.files || index < 0 || index >= input.files.length) { return; }
        if (!window.DataTransfer) {
            input.value = "";
            renderSelectedKycFiles();
            return;
        }

        var nextFiles;
        try {
            nextFiles = new DataTransfer();
        } catch (ex) {
            input.value = "";
            renderSelectedKycFiles();
            return;
        }
        for (var i = 0; i < input.files.length; i++) {
            if (i !== index) { nextFiles.items.add(input.files[i]); }
        }
        input.files = nextFiles.files;
        renderSelectedKycFiles();
    }

    function renderKycAttachments(attachments) {
        var container = byId("kycAttachmentsList");
        if (!container) { return; }

        if (!attachments || !attachments.length) {
            container.innerHTML = '<div class="kyc-attachments-empty"><span class="kyc-file-icon">FILE</span><span>لا توجد مرفقات محفوظة</span></div>';
            return;
        }

        var rows = ['<div class="kyc-attachments-grid">'];
        for (var i = 0; i < attachments.length; i++) {
            var item = attachments[i];
            var openUrl = getUrl("data-open-kyc-attachment-url") + "?id=" + encodeURIComponent(item.Id);
            rows.push('<div class="kyc-attachment-card"><span class="kyc-file-icon">' +
                escapeHtml(kycFileTypeLabel(item.FileName || "")) + '</span><span class="kyc-attachment-info"><strong>' +
                escapeHtml(item.FileName || "مرفق KYC") + "</strong><span>" +
                escapeHtml(dateForInput(item.ImageDate) || "تاريخ الرفع غير محدد") + " - " +
                escapeHtml(item.ImageTitle || item.Department || "مستند KYC") + '</span></span>' +
                '<a class="link-action" target="_blank" rel="noopener" href="' + openUrl + '">معاينة</a></div>');
        }
        rows.push("</div>");
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

    function openTodaySummary() {
        var panel = byId("todaySummaryPanel");
        if (!panel) { return; }
        panel.classList.add("is-open");
        panel.setAttribute("aria-hidden", "false");
        byId("todaySummaryCards").innerHTML = "";
        byId("todaySummaryMessage").innerText = "جاري تحميل ملخص اليوم...";
        byId("todaySummaryMessage").className = "kyc-save-message";

        requestJson("GET", getUrl("data-today-summary-url"), null, function (status, data) {
            if (status < 200 || status >= 300 || !data || data.success === false) {
                byId("todaySummaryCards").innerHTML = "";
                var message = data && data.message ? data.message : "تعذر تحميل ملخص اليوم";
                if (status === 440 || (data && data.details === "POS session context is missing.")) {
                    message = "انتهت جلسة نقطة البيع أثناء تحميل ملخص اليوم فقط. بيانات الفاتورة الحالية لم يتم مسحها؛ برجاء تسجيل الدخول في تبويب جديد أو تحديث الصفحة بعد حفظ البيانات.";
                }
                if (data && data.technicalMessage) {
                    message += "\nالتفاصيل الفنية: " + data.technicalMessage;
                }
                byId("todaySummaryMessage").innerText = message;
                byId("todaySummaryMessage").className = "kyc-save-message is-error";
                return;
            }

            byId("todaySummaryMessage").innerText = "آخر تحديث: " + (data.GeneratedAt || "");
            renderTodaySummary(data.Items || [], data.SellerRank || data.sellerRank || null, data.TargetAchievement || data.targetAchievement || null);
        });
    }

    function closeTodaySummary() {
        var panel = byId("todaySummaryPanel");
        if (!panel) { return; }
        panel.classList.remove("is-open");
        panel.setAttribute("aria-hidden", "true");
    }

    function renderTodaySummary(items, sellerRank, targetAchievement) {
        var labels = {
            "cash-in": "كاش إن",
            "cash-out": "كاش أوت",
            "card": "كارت كيشني",
            "violations": "مخالفات"
        };
        var html = "";
        for (var key in labels) {
            if (!Object.prototype.hasOwnProperty.call(labels, key)) { continue; }
            var item = null;
            for (var i = 0; i < items.length; i++) {
                if (items[i].ServiceType === key) {
                    item = items[i];
                    break;
                }
            }
            item = item || { Count: 0, NetValue: 0, PayedValue: 0 };
            html += '<article class="summary-card summary-' + key + '">' +
                '<h3>' + labels[key] + '</h3>' +
                '<div><span>عدد الحركات</span><strong>' + escapeHtml(item.Count || 0) + '</strong></div>' +
                '<div><span>الصافي</span><strong>' + decimalText(item.NetValue) + '</strong></div>' +
                '</article>';
        }
        html += renderTodayTargetCard(targetAchievement || {});
        if (sellerRank) {
            html += renderSellerRankCard(sellerRank);
        }
        byId("todaySummaryCards").innerHTML = html;
    }

    function renderTodayTargetCard(target) {
        var configured = target.IsConfigured === true || target.isConfigured === true;
        var cssClass = target.PerformanceClass || target.performanceClass || "target-neutral";
        var statusText = target.StatusText || target.statusText || (configured ? "مؤشر التارجت" : "التارجت غير مضبوط");
        var message = target.Message || target.message || "";
        var rechargeTarget = parseFloat(target.DailyRechargeTarget || target.dailyRechargeTarget || 0) || 0;
        var cardTarget = parseFloat(target.DailyCardTarget || target.dailyCardTarget || 0) || 0;
        var rechargeAchievement = parseFloat(target.RechargeAchievement || target.rechargeAchievement || 0) || 0;
        var cardAchievement = parseFloat(target.CardAchievement || target.cardAchievement || 0) || 0;
        var rechargePercent = parseFloat(target.RechargeAchievementPercent || target.rechargeAchievementPercent || 0) || 0;
        var cardPercent = parseFloat(target.CardAchievementPercent || target.cardAchievementPercent || 0) || 0;
        var overallPercent = parseFloat(target.OverallAchievementPercent || target.overallAchievementPercent || 0) || 0;
        var progress = Math.max(0, Math.min(100, overallPercent));

        return '<article class="summary-card today-target-card ' + escapeHtml(cssClass) + '">' +
            '<div class="today-target-head">' +
            '<div><h3>تارجت اليوم</h3><span>' + escapeHtml(statusText) + '</span></div>' +
            '<strong>' + decimalText(overallPercent) + '%</strong>' +
            '</div>' +
            '<div class="today-target-grid">' +
            '<div><span>الشحنات</span><b>' + decimalText(rechargeAchievement) + '</b><small>من ' + decimalText(rechargeTarget) + ' - ' + decimalText(rechargePercent) + '%</small></div>' +
            '<div><span>الكروت</span><b>' + decimalText(cardAchievement) + '</b><small>من ' + decimalText(cardTarget) + ' - ' + decimalText(cardPercent) + '%</small></div>' +
            '</div>' +
            '<div class="today-target-progress" title="' + escapeHtml(progress.toFixed(0)) + '%"><span style="width:' + progress.toFixed(0) + '%"></span></div>' +
            '<p>' + escapeHtml(message || "تابع تحقيقك اليومي من هنا.") + '</p>' +
            '</article>';
    }

    function renderSellerRankCard(rank) {
        var bucketLabels = {
            "top-10": "أنت ضمن أفضل 10% اليوم",
            "top-25": "أنت ضمن أفضل 25% اليوم",
            "top-50": "أنت ضمن أفضل 50% اليوم",
            "needs-improvement": "فرصة للتحسن اليوم",
            "no-activity": "لم تبدأ حركات اليوم بعد"
        };
        var rankNo = rank.RankNo || rank.rankNo || null;
        var activeCount = rank.ActiveSellersCount || rank.activeSellersCount || 0;
        var amountToNext = rank.AmountToNextRank || rank.amountToNextRank || 0;
        var bucket = rank.PercentileBucket || rank.percentileBucket || "no-activity";
        var isLeading = rank.IsLeading === true || rank.isLeading === true;
        var message = rank.Message || rank.message || "";
        var motivation = rank.MotivationMessage || rank.motivationMessage || "";
        var rankIcon = rank.RankIcon || rank.rankIcon || "🚀";
        var badgeText = rank.RankBadgeText || rank.rankBadgeText || bucketLabels[bucket] || "ترتيبك اليوم";
        var cssClass = rank.RankCssClass || rank.rankCssClass || "rank-progress";
        var progress = parseFloat(rank.ProgressPercent || rank.progressPercent || 0);
        if (isNaN(progress)) { progress = 0; }
        progress = Math.max(0, Math.min(100, progress));
        var rankText = rankNo ? ("#" + rankNo + " من " + activeCount) : "لا يوجد ترتيب بعد";
        var nextText = isLeading
            ? "أنت متصدر اليوم"
            : (rankNo ? ("تحتاج " + decimalText(amountToNext) + " جنيه للوصول للمركز السابق") : "ابدأ أول حركة اليوم");

        return '<article class="summary-card seller-rank-card ' + escapeHtml(cssClass) + '">' +
            '<div class="seller-rank-header">' +
            '<span class="seller-rank-icon" aria-hidden="true">' + escapeHtml(rankIcon) + '</span>' +
            '<div><h3>ترتيبك اليوم</h3><span class="seller-rank-badge">' + escapeHtml(badgeText) + '</span></div>' +
            '</div>' +
            '<div><span>من إجمالي البائعين النشطين</span><strong>' + escapeHtml(rankText) + '</strong></div>' +
            '<div><span>مؤشر الأداء</span><strong>' + escapeHtml(bucketLabels[bucket] || bucketLabels["needs-improvement"]) + '</strong></div>' +
            '<div><span>الخطوة التالية</span><strong>' + escapeHtml(nextText) + '</strong></div>' +
            '<div class="seller-rank-progress" title="' + escapeHtml(progress.toFixed(0)) + '%"><span style="width:' + progress.toFixed(0) + '%"></span></div>' +
            '<p>' + escapeHtml(motivation || message || "أداء ممتاز، استمر") + '</p>' +
            '</article>';
    }

    function searchKeshniCardCustomers() {
        var term = byId("kycSearchTerm").value.trim() || byId("cashCustomerPhone").value.trim() || byId("visaNumber").value.trim() || byId("cardNationalId").value.trim();
        if (!term) {
            byId("kycSearchResults").innerHTML = "أدخل رقم موبايل أو توكن أو رقم قومي أو اسم عميل للبحث";
            return;
        }
        if (term.length < 2) {
            byId("kycSearchResults").innerHTML = "اكتب حرفين على الأقل للبحث";
            return;
        }

        byId("kycSearchResults").innerHTML = "جاري البحث...";
        requestJson("GET", getUrl("data-customer-search-url") + "?term=" + encodeURIComponent(term), null, function (status, data) {
            var resultsBox = byId("kycSearchResults");
            if (status < 200 || status >= 300 || !data) {
                resultsBox.innerHTML = "لا توجد نتائج";
                return;
            }

            if (!Array.isArray(data)) {
                resultsBox._items = [];
                resultsBox.innerHTML = "";
                if (data.otherBranch === true) {
                    setKycMessage(data.message || "تم العثور على بيانات KYC في فرع آخر.", false, true);
                    return;
                }
                resultsBox.innerHTML = data.message || "لا توجد نتائج";
                return;
            }

            if (!data.length) {
                resultsBox._items = [];
                byId("kycSearchResults").innerHTML = "لا توجد نتائج";
                return;
            }

            var html = [];
            for (var i = 0; i < data.length; i++) {
                html.push('<button type="button" class="kyc-result-item" data-index="' + i + '"><strong>' +
                    escapeHtml(data[i].CustomerName || data[i].Name || "") + '</strong><span>' +
                    escapeHtml(data[i].Phone || "") + ' | ' + escapeHtml(data[i].VisaNumber || data[i].CardNo || data[i].CardId || "") + ' | ' +
                    escapeHtml(data[i].Tet_NumPoket || "") + '</span></button>');
            }
            byId("kycSearchResults").innerHTML = html.join("");
            byId("kycSearchResults")._items = data;
        });
    }

    function renderKycCustomerChoices(items) {
        var html = [];
        for (var i = 0; i < items.length; i++) {
            html.push('<button type="button" class="kyc-result-item" data-index="' + i + '"><strong>' +
                escapeHtml(items[i].CustomerName || items[i].Name || "") + '</strong><span>' +
                escapeHtml(items[i].Phone || "") + ' | ' + escapeHtml(items[i].VisaNumber || items[i].CardNo || items[i].CardId || "") + ' | ' +
                escapeHtml(items[i].Tet_NumPoket || "") + '</span></button>');
        }
        byId("kycSearchResults").innerHTML = html.join("");
        byId("kycSearchResults")._items = items;
    }

    function isCompleteKycLookupTerm(value) {
        value = (value || "").trim();
        return /^(010|011|012|015)[0-9]{8}$/.test(value)
            || /^[0-9]{14}$/.test(value)
            || value.length === 8
            || value.length === 18;
    }

    function scheduleUnusedKycLookup(value) {
        if (byId("transactionType").value !== "card" || savedKycCustomerId() > 0) { return; }

        value = (value || "").trim();
        window.clearTimeout(kycUnusedLookupTimer);
        if (!isCompleteKycLookupTerm(value)) { return; }

        kycUnusedLookupTimer = window.setTimeout(function () {
            lookupUnusedKycCustomer(value);
        }, 350);
    }

    function lookupUnusedKycCustomer(term) {
        var url = getUrl("data-unused-kyc-lookup-url");
        if (!url || !term || savedKycCustomerId() > 0) { return; }

        requestJson("GET", url + "?term=" + encodeURIComponent(term), null, function (status, data) {
            if (status < 200 || status >= 300 || !data) { return; }
            if (savedKycCustomerId() > 0) { return; }

            if (data.found === true && data.customer) {
                applyKeshniCustomer(data.customer);
                setKycMessage(data.message || "تم العثور على بيانات KYC محفوظة مسبقاً ولم يتم إصدار فاتورة لها، وتم تحميلها للتعديل/الاستخدام.");
                return;
            }

            if (data.multiple === true && data.customers && data.customers.length) {
                renderKycCustomerChoices(data.customers);
                setKycMessage(data.message || "يوجد أكثر من عميل مطابق. برجاء اختيار العميل من النتائج أو البحث ببيانات أدق.", true);
                return;
            }

            if (data.otherBranch === true) {
                byId("kycSearchResults")._items = [];
                byId("kycSearchResults").innerHTML = "";
                setKycMessage(data.message || "تم العثور على بيانات KYC في فرع آخر.", false, true);
            }
        });
    }

    function scheduleCommissionPreview(options) {
        options = options || {};
        window.clearTimeout(commissionTimer);
        commissionPreviewUseOverlay = options.useOverlay !== false;
        if (commissionPreviewXhr && commissionPreviewXhr.readyState !== 4 && commissionPreviewXhr.abort) {
            commissionPreviewSequence++;
            commissionPreviewXhr.abort();
        }
        if (!reviewMode && options.quiet !== true) {
            markCommissionPending("جاري تحميل إعدادات العمولات، برجاء الانتظار لحظات.");
        } else {
            commissionCalculationPending = true;
            updateSaveButtonState();
        }
        updateCashOutMachineDisplay();
        posDebugLog("scheduleCommissionPreview", {
            delay: options.delay === 0 ? 0 : (options.delay || 400),
            mode: byId("transactionType").value,
            amount: byId("transactionType").value === "violations" ? numberValue("violationValue") : numberValue("rechargeValue")
        });
        commissionTimer = window.setTimeout(calculateCommissionPreview, options.delay === 0 ? 0 : (options.delay || 400));
    }

    function buildCommissionRequest() {
        var firstRow = getFirstSelectedItemRow();
        if (!firstRow) {
            return null;
        }
        var amount = byId("transactionType").value === "card" ? 0 : (byId("transactionType").value === "violations" ? numberValue("violationValue") : numberValue("rechargeValue"));
        var amountInput = byId("transactionType").value === "violations" ? byId("violationValue") : byId("rechargeValue");
        if (byId("transactionType").value !== "card" && !validateAmountSafety(amountInput, true)) {
            setCommissionStatus("قيمة المبلغ غير صحيحة. راجع خانة المبلغ قبل حساب العمولة.", true);
            return null;
        }
        if (amount > maxRechargeValue) {
            setCommissionStatus("المبلغ أكبر من الحد المسموح لهذه الخدمة. الحد الأقصى 100,000", true);
            return null;
        }

        return {
            ServiceType: byId("transactionType").value,
            ItemID: parseInt(firstRow.getAttribute("data-item-id"), 10) || null,
            BranchId: parseInt(byId("branchId").value, 10) || null,
            RechargeValue: amount,
            Vatyo: parseFloat(firstRow.getAttribute("data-vatyo")) || 14,
            IsWallet: byId("isWallet").value === "true",
            HaveGuarantee: byId("haveGuarantee").value === "true"
        };
    }

    function commissionKey(request) {
        if (!request) { return ""; }
        return [
            request.ServiceType || "",
            request.ItemID || "",
            request.BranchId || "",
            decimalText(request.RechargeValue),
            decimalText(request.Vatyo),
            request.IsWallet ? "1" : "0",
            request.HaveGuarantee ? "1" : "0"
        ].join("|");
    }

    function applyCommissionResult(data) {
        var firstRow = getFirstSelectedItemRow();
        if (!firstRow || !data) { return; }

        var commission = parseFloat(data.CommissionValue) || 0;
        var vatValue = parseFloat(data.VatValue) || 0;
        var vatPercent = parseFloat(data.VatPercent) || 0;
        var totalFees = parseFloat(data.TotalFees) || (commission + vatValue);
        var isCard = byId("transactionType").value === "card";
        var totalValue = isCard ? totalFees : (parseFloat(data.TotalValue) || (numberValue("rechargeValue") + totalFees));
        var isCashOut = byId("transactionType").value === "cash-out";
        lastCashOutBankMachineCommission = isCashOut ? (parseFloat(data.BankMachineCommission) || parseFloat(data.bankMachineCommission) || 0) : 0;
        lastCashOutMachineWithdrawalAmount = isCashOut ? (parseFloat(data.CashOutMachineWithdrawalAmount) || parseFloat(data.cashOutMachineWithdrawalAmount) || 0) : 0;
        if (!isCashOut) {
            firstRow.removeAttribute("data-bank-machine-commission");
            firstRow.removeAttribute("data-cashout-machine-withdrawal");
        }

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
        firstRow.setAttribute("data-bank-machine-commission", lastCashOutBankMachineCommission);
        firstRow.setAttribute("data-cashout-machine-withdrawal", lastCashOutMachineWithdrawalAmount);
        calculateTotals();
    }

    function calculateCommissionPreview() {
        var request = buildCommissionRequest();
        if (!request) {
            commissionCalculationPending = false;
            amountEnterAdvancePending = false;
            updateSaveButtonState();
            updateCashOutMachineDisplay();
            return;
        }
        posDebugLog("calculateCommissionPreview", request);

        var key = commissionKey(request);
        if (commissionCache[key]) {
            applyCommissionResult(commissionCache[key]);
            lastCommissionKey = key;
            commissionCalculationPending = false;
            updateSaveButtonState();
            updateCashOutMachineDisplay();
            setCommissionStatus("");
            uxAdvanceAfterAmountCommit();
            return;
        }

        var requestId = ++commissionPreviewSequence;
        var send = commissionPreviewUseOverlay ? requestJsonWithLoading : requestJson;
        commissionPreviewXhr = send("POST", getUrl("data-commission-url"), request, function (status, data) {
            if (requestId !== commissionPreviewSequence || key !== commissionKey(buildCommissionRequest())) {
                return;
            }

            if (status < 200 || status >= 300 || !data) {
                commissionCalculationPending = false;
                amountEnterAdvancePending = false;
                updateSaveButtonState();
                updateCashOutMachineDisplay();
                setCommissionStatus("تعذر تحميل إعدادات العمولات لهذا الاختيار", true);
                return;
            }

            commissionCache[key] = data;
            posDebugLog("commissionResult", data);
            applyCommissionResult(data);
            lastCommissionKey = key;
            commissionCalculationPending = false;
            updateSaveButtonState();
            updateCashOutMachineDisplay();
            setCommissionStatus("");
            uxAdvanceAfterAmountCommit();
        });
    }

    function saveCashCustomer() {
        if (kycSaveInProgress) {
            return;
        }

        commitAllKycNameSync();

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
        var kycPhone = (byId("kycPhoneNo2").value || byId("cashCustomerPhone").value).trim();
        if (!isValidEgyptianMobile(kycPhone)) {
            var phoneFormatMessage = phoneValidationMessage(kycPhone) || "رقم الهاتف يجب أن يتكون من 11 رقم";
            byId("validationSummary").innerText = phoneFormatMessage;
            setKycMessage(phoneFormatMessage, true);
            normalizePhoneInput(byId("kycPhoneNo2"), true);
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
        if (!/^[0-9]{14}$/.test(byId("kycNationalId").value.trim())) {
            var nationalIdFormatMessage = "الرقم القومي يجب أن يكون 14 رقم";
            byId("validationSummary").innerText = nationalIdFormatMessage;
            setKycMessage(nationalIdFormatMessage, true);
            return;
        }
        if (!byId("kycCardNo").value.trim()) {
            var cardMessage = "من فضلك أدخل رقم الكارت";
            byId("validationSummary").innerText = cardMessage;
            setKycMessage(cardMessage, true);
            return;
        }
        var kycCardNo = byId("kycCardNo").value.trim();
        if (kycCardNo.length !== 8 && kycCardNo.length !== 18) {
            var cardFormatMessage = "رقم التوكن/الكارت يجب أن يكون 8 أو 18 رقم";
            byId("validationSummary").innerText = cardFormatMessage;
            setKycMessage(cardFormatMessage, true);
            return;
        }
        if (!validateKycDatesBeforeSave()) {
            return;
        }

        var formData = new FormData();
        formData.append("CustomerID", parseInt(byId("cashCustomerId").value, 10) || "");
        formData.append("Name", byId("kycName").value || byId("cashCustomerName").value);
        formData.append("NameE", byId("kycNameE").value);
        formData.append("ArabicName0", byId("kycArabicName0").value);
        formData.append("ArabicName1", byId("kycArabicName1").value);
        formData.append("ArabicName2", byId("kycArabicName2").value);
        formData.append("ArabicName3", byId("kycArabicName3").value);
        formData.append("EnglishName0", byId("kycEnglishName0").value);
        formData.append("EnglishName1", byId("kycEnglishName1").value);
        formData.append("EnglishName2", byId("kycEnglishName2").value);
        formData.append("EnglishName3", byId("kycEnglishName3").value);
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
        var totalAttachmentBytes = 0;
        for (var i = 0; i < files.length; i++) {
            totalAttachmentBytes += files[i].size || 0;
        }
        if (totalAttachmentBytes > 100 * 1024 * 1024) {
            var sizeMessage = "حجم المرفقات أكبر من الحد المسموح به. الحد الأقصى الحالي 100 ميجا.";
            byId("validationSummary").innerText = sizeMessage;
            setKycMessage(sizeMessage, true);
            return;
        }
        for (var i = 0; i < files.length; i++) {
            formData.append("attachmentOriginalNamesBase64", base64Utf8(files[i].name || ""));
            formData.append("attachments", files[i], safeUploadFileName(files[i].name, i));
        }

        var saveButton = byId("saveCashCustomerBtn");
        var oldButtonText = saveButton.innerText;
        function resetKycSaveButton() {
            kycSaveInProgress = false;
            saveButton.disabled = !currentContext || currentContext.CanOpenCashCustomer !== true;
            saveButton.innerText = oldButtonText;
        }

        kycSaveInProgress = true;
        saveButton.disabled = true;
        saveButton.innerText = "جاري الحفظ...";
        byId("validationSummary").innerText = "";
        byId("saveResult").innerText = "جاري حفظ بيانات الكارت...";
        setKycMessage("جاري حفظ بيانات الكارت...");

        requestFormData(getUrl("data-save-keshni-card-url"), formData, function (status, data) {
            resetKycSaveButton();

            try {
                if (status >= 200 && status < 300 && data && data.success && data.customer) {
                    applyKeshniCustomer(data.customer);
                    renderKycAttachments(data.attachments || []);
                    var successMessage = data.message || "تم حفظ بيانات العميل وتفعيل الكارت";
                    byId("saveResult").innerText = successMessage;
                    byId("validationSummary").innerText = "";
                    setKycMessage(successMessage + " — يمكنك الآن طباعة الإقرار.");
                    calculateCommissionPreview();
                    enablePrintAcknowledgmentIfAllowed();
                    return;
                }

                var message = data && data.message ? data.message : "تعذر حفظ بيانات العميل";
                if (!(status >= 200 && status < 300) && status) {
                    message += "\nHTTP Status: " + status;
                }
                var validationList = data && data.validationErrorsList ? data.validationErrorsList : (data && data.validationErrors);
                if (validationList) {
                    if (Array.isArray(validationList) && validationList.length > 0) {
                        message += "\nالسبب: " + validationList.join("\n");
                    } else {
                        var validationKeys = Object.keys(validationList);
                        if (validationKeys.length > 0) {
                            message += "\nالسبب: " + validationKeys.map(function (key) { return validationList[key]; }).join("\n");
                        }
                    }
                }
                if (data && data.duplicateCustomerId) {
                    message += "\nيمكن البحث عن العميل المسجل واستخدامه. رقم العميل: " + data.duplicateCustomerId;
                }
                if (data && (data.details || data.technicalDetails || data.technicalMessage)) {
                    var details = data.details || data.technicalDetails || data.technicalMessage;
                    message += "\nالتفاصيل الفنية: " + details;
                    if (window.console && console.error) {
                        console.error("KYC save failed", {
                            status: status,
                            message: data.message,
                            details: details,
                            validationErrors: data.validationErrors || data.validationErrorsList
                        });
                    }
                }

                byId("validationSummary").innerText = message;
                byId("saveResult").innerText = "";
                if (data && data.duplicate === true && data.existingCustomer) {
                    showDuplicateKycCustomerAction(data, message);
                    return;
                }
                setKycMessage(message, true);
            } catch (ex) {
                resetKycSaveButton();
                var scriptMessage = "حدث خطأ في عرض نتيجة حفظ بيانات الكارت";
                if (ex && ex.message) {
                    scriptMessage += "\nالتفاصيل الفنية: " + ex.message;
                }
                byId("validationSummary").innerText = scriptMessage;
                byId("saveResult").innerText = "";
                setKycMessage(scriptMessage, true);
            }
        });
    }

    function clearMessages() {
        byId("validationSummary").innerHTML = "";
        clearAllValidationHighlights();
        if (byId("saveResult").getAttribute("data-commission-status") !== "true") {
            byId("saveResult").innerHTML = "";
        }
        setKycMessage("");
    }

    function clearFormForNewTransaction(clearLastSavedTransaction) {
        byId("posForm").reset();
        pendingSaveConfirmation = false;
        closeSaveConfirmation();
        if (clearLastSavedTransaction) {
            lastSavedTransactionId = null;
        }
        loadedInvoiceCreatedUserId = null;
        loadedInvoiceBranchId = null;
        loadedInvoiceStoreId = null;
        loadedInvoiceBoxId = null;
        loadedInvoiceEmpId = null;
        loadedInvoiceIsCancelled = false;
        byId("printBtn").disabled = true;
        if (byId("cancelledBanner")) { byId("cancelledBanner").style.display = "none"; }
        enableDeleteIfAllowed();
        enableCancelIfAllowed();
        reviewMode = false;
        clearJournalEntry();
        clearMessages();
        closeKycModal();

        resetServiceRows();

        byId("cashCustomerId").value = "";
        pendingDuplicateKycCustomer = null;
        enablePrintAcknowledgmentIfAllowed();
        byId("transactionType").value = "";
        byId("isCashOut").value = "false";
        byId("isPOS").value = "false";
        byId("otherItems").value = "false";
        byId("isRecharg").value = "false";
        byId("isWallet").value = "false";
        byId("haveGuarantee").value = "false";
        if (byId("transactionDate")) {
            byId("transactionDate").value = localIsoDate();
            byId("transactionDate").disabled = true;
        }
        if (byId("invoiceNumber")) { byId("invoiceNumber").value = ""; }
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
        lastCashOutMachineWithdrawalAmount = 0;
        lastCashOutBankMachineCommission = 0;
        byId("remainValue").value = "0";
        byId("payedValue").value = "0";
        byId("rechargeValue").value = "0";
        calculateTotals();
    }

    function switchTransactionMode(mode) {
        clearFormForNewTransaction(true);
        loadContextControls();
        setMode(mode || "cash-in");
        applyPermissions();
        calculateTotals();
        enablePrintIfAllowed();
        enableDeleteIfAllowed();
        enableCancelIfAllowed();
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
            enableDeleteIfAllowed();
            enableCancelIfAllowed();
        });
    }

    function applyPermissions() {
        updateSaveButtonState();
        enablePrintIfAllowed();
        enableDeleteIfAllowed();
        enableCancelIfAllowed();
        byId("returnLaterBtn").disabled = !currentContext || currentContext.CanReturn !== true || byId("transactionType").value === "violations";
        byId("saveCashCustomerBtn").disabled = !currentContext || currentContext.CanEditKyc !== true;
        byId("boxId").disabled = true;
        byId("branchId").disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        byId("paymentType").disabled = !currentContext || currentContext.CanChangeDefaults !== true;
        byId("storeName").readOnly = true;
        byId("empName").readOnly = true;
        byId("bankName").readOnly = true;
    }

    document.addEventListener("click", function (event) {
        var transferAmountButton = event.target.closest ? event.target.closest("[data-transfer-phone-like-amount]") : null;
        if (transferAmountButton) {
            transferSuspiciousAmountToWallet(byId(transferAmountButton.getAttribute("data-transfer-phone-like-amount")));
            return;
        }
        var dismissAmountButton = event.target.closest ? event.target.closest("[data-dismiss-phone-like-amount]") : null;
        if (dismissAmountButton) {
            clearFieldInvalid(amountFieldName(dismissAmountButton.getAttribute("data-dismiss-phone-like-amount")));
            return;
        }
        if (event.target.id === "confirmSaveBtn") {
            pendingSaveConfirmation = true;
            closeSaveConfirmation();
            byId("posForm").requestSubmit ? byId("posForm").requestSubmit() : byId("saveBtn").click();
            return;
        }
        if (event.target.id === "cancelSaveConfirmBtn" || event.target.id === "editSaveConfirmBtn" || event.target.id === "saveConfirmBackdrop") {
            closeSaveConfirmation();
            return;
        }
        if (event.target.classList.contains("pos-type-btn")) { switchTransactionMode(event.target.getAttribute("data-mode")); }
        if (event.target.id === "addItemBtn") { addItemRow(); }
        if (event.target.id === "printBtn") {
            var transactionId = savedTransactionId();
            openPrintForTransaction(transactionId);
        }
        if (event.target.id === "printAcknowledgmentBtn") {
            openPrintAcknowledgmentForCustomer(savedKycCustomerId());
        }
        if (event.target.id === "printCardBtn") {
            openPrintCardForCustomer(savedKycCustomerId());
        }
        if (event.target.id === "reloadJournalBtn") {
            loadJournalEntry(savedTransactionId());
        }
        if (event.target.id === "loadDuplicateKycCustomerBtn") {
            loadPendingDuplicateKycCustomer();
        }
        if (event.target.id === "salesSearchBtn") {
            loadSalesIndexInvoices();
        }
        if (event.target.id === "deleteCurrentInvoiceBtn") {
            deleteInvoice(savedTransactionId());
        }
        if (event.target.id === "cancelInvoiceBtn") {
            cancelInvoice(savedTransactionId());
        }
        if (event.target.id === "deleteExcelRangeBtn") {
            deleteExcelInvoicesForRange();
        }
        if (event.target.id === "salesClearSearchBtn") {
            if (byId("salesSearchText")) { byId("salesSearchText").value = ""; }
            if (byId("salesSearchType")) { byId("salesSearchType").value = ""; }
            if (byId("salesSearchFromDate")) { byId("salesSearchFromDate").value = localIsoDate(); }
            if (byId("salesSearchToDate")) { byId("salesSearchToDate").value = localIsoDate(); }
            if (byId("salesExcelOnly")) { byId("salesExcelOnly").checked = false; }
            setSalesBranch("", "");
            salesIndexCache = [];
            if (byId("salesIndexMessage")) { byId("salesIndexMessage").innerText = ""; }
            if (byId("salesIndexResults")) { byId("salesIndexResults").innerHTML = '<div class="empty-state">اضغط بحث البيانات لعرض الفواتير.</div>'; }
        }
        if (event.target.id === "salesSearchBranchClear") {
            setSalesBranch("", "");
        }
        if (event.target.id === "salesAddNewBtn") {
            showSalesEntry();
            reloadContextAndReset();
        }
        if (event.target.id === "backToSalesIndexBtn") {
            showSalesIndex();
        }
        var deleteInvoiceButton = event.target.closest ? event.target.closest("[data-delete-invoice]") : null;
        if (deleteInvoiceButton) {
            deleteInvoice(deleteInvoiceButton.getAttribute("data-delete-invoice"));
            return;
        }
        var salesOpenButton = event.target.closest ? event.target.closest("[data-sales-open]") : null;
        if (salesOpenButton) {
            var salesTransactionId = salesOpenButton.getAttribute("data-sales-open");
            showSalesEntry();
            loadInvoiceForReview(salesTransactionId);
        }
        var salesBranchButton = event.target.closest ? event.target.closest("[data-sales-branch-id]") : null;
        if (salesBranchButton) {
            setSalesBranch(salesBranchButton.getAttribute("data-sales-branch-id"), salesBranchButton.getAttribute("data-sales-branch-name"));
        }
        var invoiceButton = event.target.closest ? event.target.closest(".today-invoice-item") : null;
        if (invoiceButton) {
            var invoiceIndex = parseInt(invoiceButton.getAttribute("data-index"), 10);
            showTodayInvoiceSummary(invoiceIndex);
            loadInvoiceForReview(invoiceButton.getAttribute("data-transaction-id"));
        }
        var workflowButton = event.target.closest ? event.target.closest(".workflow-step") : null;
        if (workflowButton) {
            var requestedStep = workflowButton.getAttribute("data-step");
            if (uxCanEnterStep(requestedStep)) {
                uxFocusStep(requestedStep);
            } else {
                uxShowGuide();
                uxFocusStep(uxCurrentStep);
            }
        }
        if (event.target.classList.contains("remove-row")) {
            var rows = byId("itemsTable").querySelectorAll("tbody tr");
            if (rows.length > 1) {
                var row = event.target.closest("tr");
                row.parentNode.removeChild(row);
                recalculateInvoiceSummary({ source: "remove-row", requestCommission: false });
            }
        }
        if (event.target.id === "newBtn") { reloadContextAndReset(); }
        if (event.target.id === "searchCustomerBtn" || event.target.id === "activateCardBtn") { openKycModal(); }
        if (event.target.id === "closeKycModalBtn" || event.target.id === "kycModalBackdrop") { closeKycModal(); }
        if (event.target.id === "kycSearchBtn") { searchKeshniCardCustomers(); }
        if (event.target.id === "showKycAttachmentsBtn") { loadKycAttachments(); }
        var removeKycFileButton = event.target.closest ? event.target.closest("[data-remove-kyc-file]") : null;
        if (removeKycFileButton) {
            removeSelectedKycFile(parseInt(removeKycFileButton.getAttribute("data-remove-kyc-file"), 10));
            return;
        }
        if (event.target.id === "todaySummaryBtn") { openTodaySummary(); }
        if (event.target.id === "closeTodaySummaryBtn" || event.target.id === "todaySummaryBackdrop") { closeTodaySummary(); }
        if (event.target.id === "toggleTodayInvoicesBtn") {
            setTodayInvoicesCollapsed(!byId("posPage").classList.contains("today-invoices-collapsed"), true);
        }
        if (event.target.id === "clearTodayInvoiceSearchBtn") {
            if (byId("todayInvoiceSearch")) { byId("todayInvoiceSearch").value = ""; }
            if (byId("todayInvoiceTypeFilter")) { byId("todayInvoiceTypeFilter").value = ""; }
            if (byId("todayDateRangeEnabled")) { byId("todayDateRangeEnabled").checked = false; }
            if (byId("todayFromDate")) { byId("todayFromDate").value = ""; }
            if (byId("todayToDate")) { byId("todayToDate").value = ""; }
            if (byId("todayExcelOnly")) { byId("todayExcelOnly").checked = false; }
            todayInvoicesCache = [];
            byId("todayInvoicesList").innerText = "اضغط بحث أو اختر نوع الفاتورة لعرض الفواتير.";
            byId("todayInvoiceSummary").innerHTML = "";
        }
        var kycResultButton = event.target.closest ? event.target.closest(".kyc-result-item") : null;
        if (kycResultButton) {
            var kycItems = byId("kycSearchResults")._items || [];
            applyKeshniCustomer(kycItems[parseInt(kycResultButton.getAttribute("data-index"), 10)]);
        }
        if (event.target.id === "returnLaterBtn") { byId("saveResult").innerText = "هذه العملية غير متاحة حاليا."; }
        if (event.target.id === "saveCashCustomerBtn") { saveCashCustomer(); }
        if (event.target.id === "refreshBalancesBtn") { loadEmployeeBalances(); }
        uxApplyFlow();
    });

    document.addEventListener("keydown", function (event) {
        if (handleGlobalShortcuts(event)) { return; }
        if (event.key === "Enter" && isAmountInput(event.target)) {
            event.preventDefault();
            amountEnterAdvancePending = true;
            commitAmountInput(event.target, true);
            formatMoneyInput(event.target);
            if (event.target.id !== "payedValue") {
                uxShowGuide("جاري حساب القيمة...");
            }
            return;
        }
        uxHandleSaveShortcut(event);
        uxHandleEnter(event);
    });

    document.addEventListener("input", function (event) {
        clearFixedFieldIfValid(event.target);
        if (event.target.classList && event.target.classList.contains("pos-eg-phone")) {
            normalizePhoneInput(event.target, false);
        }
        scheduleKycNameSync(event.target);
        if (event.target.matches(".qty, .price, .vat, #commissionValue")) {
            recalculateInvoiceSummary({ source: "row-input", requestCommission: false });
        }

        if (isAmountInput(event.target)) {
            scheduleAmountCommit(event.target);
        }

        if (event.target.name === "violationPayType") {
            setMode(byId("transactionType").value);
        }

        if (event.target.id === "violationWalletNo") {
            byId("tetNumPoket").value = event.target.value;
            normalizePhoneInput(byId("tetNumPoket"), false);
            clearFieldInvalid("WalletNumber");
        }

        if (event.target.id === "visaNumber" && byId("transactionType").value === "card") {
            byId("paymentCardNo").value = event.target.value;
            byId("kycCardNo").value = event.target.value;
            scheduleUnusedKycLookup(event.target.value);
            scheduleKycCardAvailabilityCheck();
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
            scheduleUnusedKycLookup(event.target.value);
            var birthDate = extractBirthDateFromNationalId(event.target.value);
            if (birthDate) {
                byId("kycBirthDate").value = birthDate;
            }
        }

        if (event.target.id === "kycNationalId") {
            var modalBirthDate = extractBirthDateFromNationalId(event.target.value);
            scheduleUnusedKycLookup(event.target.value);
            scheduleKycCardAvailabilityCheck();
            if (modalBirthDate) {
                byId("kycBirthDate").value = modalBirthDate;
                byId("cardNationalId").value = event.target.value;
            }
        }

        if (event.target.id === "cashCustomerPhone") {
            byId("kycPhoneNo2").value = event.target.value;
            normalizePhoneInput(byId("kycPhoneNo2"), false);
            scheduleKycCardAvailabilityCheck();
            if (byId("transactionType").value !== "card") { return; }
            window.clearTimeout(customerLookupTimer);
            customerLookupTimer = window.setTimeout(function () {
                var phone = event.target.value.trim();
                if (isValidEgyptianMobile(phone)) {
                    lookupUnusedKycCustomer(phone);
                }
            }, 350);
        }

        if (event.target.id === "kycPhoneNo2" || event.target.id === "kycCardNo") {
            if (event.target.id === "kycPhoneNo2") {
                byId("cashCustomerPhone").value = event.target.value;
                normalizePhoneInput(byId("cashCustomerPhone"), false);
            }
            scheduleUnusedKycLookup(event.target.value);
            scheduleKycCardAvailabilityCheck();
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

        if (event.target.id === "salesSearchText") {
            window.clearTimeout(todayInvoicesTimer);
            todayInvoicesTimer = window.setTimeout(function () {
                if ((event.target.value || "").trim().length >= 2) {
                    loadSalesIndexInvoices();
                }
            }, 300);
        }
        if (event.target.id === "salesSearchBranchText") {
            if (byId("salesSearchBranchId")) { byId("salesSearchBranchId").value = ""; }
            var branchTerm = event.target.value || "";
            ensureSalesBranchesLoaded(function () {
                renderSalesBranchResults(branchTerm);
            });
        }

        uxApplyFlow();
    });

    document.addEventListener("wheel", function (event) {
        if (event.target && event.target.matches && event.target.matches('input[type="number"]')) {
            event.preventDefault();
        }
    }, { passive: false });

    document.addEventListener("focusin", function (event) {
        if (event.target && (event.target.id === "branchId" || event.target.id === "paymentType" || event.target.id === "boxId")) {
            ensureContextControlsLoaded();
        }
        if (event.target && event.target.id === "salesSearchBranchText") {
            ensureSalesBranchesLoaded(function () {
                renderSalesBranchResults(event.target.value || "");
            });
        }
    });

    document.addEventListener("blur", function (event) {
        if (event.target.classList && event.target.classList.contains("pos-eg-phone")) {
            normalizePhoneInput(event.target, true);
        }
        commitKycNameSync(event.target);
    }, true);

    document.addEventListener("focusout", function (event) {
        if (isAmountInput(event.target)) {
            validateAmountSafety(event.target, true);
            formatMoneyInput(event.target);
        }
    });

    document.addEventListener("mousedown", function (event) {
        if (event.target && (event.target.id === "branchId" || event.target.id === "paymentType" || event.target.id === "boxId")) {
            ensureContextControlsLoaded();
        }
    });

    document.addEventListener("change", function (event) {
        clearFixedFieldIfValid(event.target);
        if (isAmountInput(event.target)) {
            commitAmountInput(event.target, true);
            formatMoneyInput(event.target);
        }
        if (event.target.id === "branchId") {
            if (!savedKycCustomerId()) {
                byId("kycCreatedBranch").value = activeKycBranchName();
                byId("kycCreatedDate").value = byId("kycCreatedDate").value || defaultKycCreatedDate();
                refreshKycMetadataDisplay();
            }
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
            recalculateInvoiceSummary({ source: "serviceItemId", requestCommission: true });
        }
        if (event.target.id === "serviceItemId2") {
            recalculateInvoiceSummary({ source: "serviceItemId2", requestCommission: true });
        }
        if (event.target.id === "kycAttachments") {
            renderSelectedKycFiles();
        }
        if (event.target.name === "violationPayType") {
            setMode(byId("transactionType").value);
        }
        if (event.target.id === "todayInvoiceTypeFilter") {
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
        }
        if (event.target.id === "todayDateRangeEnabled") {
            if (event.target.checked) {
                var today = localIsoDate();
                if (byId("todayFromDate") && !byId("todayFromDate").value) { byId("todayFromDate").value = today; }
                if (byId("todayToDate") && !byId("todayToDate").value) { byId("todayToDate").value = today; }
            }
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
        }
        if (event.target.id === "todayFromDate" || event.target.id === "todayToDate") {
            if (byId("todayDateRangeEnabled")) { byId("todayDateRangeEnabled").checked = true; }
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
        }
        if (event.target.id === "todayExcelOnly") {
            loadTodayInvoices((byId("todayInvoiceSearch") && byId("todayInvoiceSearch").value.trim()) || "");
        }
        if (event.target.id === "salesSearchType") {
            loadSalesIndexInvoices();
        }
        if (event.target.id === "salesExcelOnly") {
            loadSalesIndexInvoices();
        }
        if (event.target.id === "salesSearchFromDate" || event.target.id === "salesSearchToDate") {
            loadSalesIndexInvoices();
        }
        uxApplyFlow();
    });

    document.addEventListener("change", function (event) {
        if (event.target.classList.contains("item-name")) {
            applySelectedItem(event.target);
        }
        uxApplyFlow();
    });

    byId("posForm").addEventListener("submit", saveTransaction);
    setInitialContextFromPage();
    loadEmployeeBalances();
    if (byId("transactionDate")) {
        byId("transactionDate").value = localIsoDate();
        byId("transactionDate").disabled = true;
    }
    initSalesIndexFilters();
    initTodayInvoicesPanelState();
    refreshPhoneInputs();
    if (salesIndexFirst()) {
        showSalesIndex();
    } else {
        initializePosEntry(queryValue("openKyc") === "true");
    }
    updateSaveButtonState();
    uxApplyFlow();
})();
