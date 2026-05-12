(function () {
    "use strict";

    var root = document.getElementById("customersPage");
    if (!root) { return; }

    var config = root.dataset;
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var statusBox = document.getElementById("customerStatus");
    var saveBtn = document.getElementById("customerSaveBtn");
    var newBtn = document.getElementById("customerNewBtn");
    var deleteBtn = document.getElementById("customerDeleteBtn");

    function byId(id) {
        return document.getElementById(id);
    }

    function value(id) {
        var el = byId(id);
        return el ? el.value : "";
    }

    function checked(id) {
        var el = byId(id);
        return !!(el && el.checked);
    }

    function setValue(id, val) {
        var el = byId(id);
        if (el) { el.value = val == null ? "" : val; }
    }

    function setChecked(id, val) {
        var el = byId(id);
        if (el) { el.checked = val === true; }
    }

    function showStatus(message, kind) {
        if (!statusBox) { return; }
        statusBox.hidden = false;
        statusBox.className = "customers-status " + (kind || "info");
        statusBox.textContent = message || "";
    }

    function post(url, data) {
        data = data || {};
        if (token) {
            data.__RequestVerificationToken = token.value;
        }

        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
            body: new URLSearchParams(data).toString(),
            credentials: "same-origin"
        }).then(function (response) { return response.json(); });
    }

    function toNullableNumber(v) {
        if (v == null || String(v).trim() === "") { return ""; }
        return v;
    }

    function collect() {
        return {
            CusId: value("CusId"),
            CusName: value("CusName"),
            CusNameEnglish: value("CusNameEnglish"),
            ResponsibleContact: value("ResponsibleContact"),
            Phone: value("Phone"),
            Mobile: value("Mobile"),
            FaxNumber: value("FaxNumber"),
            Email: value("Email"),
            Address: value("Address"),
            Remark: value("Remark"),
            Remark2: value("Remark2"),
            Type: value("Type"),
            Prefix: value("Prefix"),
            Code: value("Code"),
            FullCode: value("FullCode"),
            BranchId: value("BranchId"),
            RecordDate: value("RecordDate"),
            OpenBalance: toNullableNumber(value("OpenBalance")),
            OpenBalanceType: value("OpenBalanceType"),
            OpenBalance1: toNullableNumber(value("OpenBalance1")),
            OpenBalanceType1: value("OpenBalanceType1"),
            OpenBalance2: toNullableNumber(value("OpenBalance2")),
            OpenBalanceType2: value("OpenBalanceType2"),
            OpenBalanceDate: value("OpenBalanceDate"),
            CreditLimit: toNullableNumber(value("CreditLimit")),
            CreditLimitCredit: toNullableNumber(value("CreditLimitCredit")),
            DebitInterval: toNullableNumber(value("DebitInterval")),
            CreditInterval: toNullableNumber(value("CreditInterval")),
            PaymentType: value("PaymentType"),
            AccountCode: value("AccountCode"),
            ParentAccount: value("ParentAccount"),
            ClassCustomersId: value("ClassCustomersId"),
            GroupsCustomersId: value("GroupsCustomersId"),
            SaleType: value("SaleType"),
            DiscountType: value("DiscountType"),
            DiscountValue: toNullableNumber(value("DiscountValue")),
            PurchaseDiscountType: value("PurchaseDiscountType"),
            PurchaseDiscountValue: toNullableNumber(value("PurchaseDiscountValue")),
            CustomerAndVendor: checked("CustomerAndVendor"),
            IsLocked: checked("IsLocked"),
            CreditLocked: checked("CreditLocked"),
            NationalNo: value("NationalNo"),
            VatNo: value("VatNo"),
            CommercialRegisterNo: value("CommercialRegisterNo"),
            TaxCardNo: value("TaxCardNo"),
            ImportCardNo: value("ImportCardNo"),
            ExportCardNo: value("ExportCardNo"),
            StreetName: value("StreetName"),
            AdditionalStreetName: value("AdditionalStreetName"),
            BuildingNumber: value("BuildingNumber"),
            PlotIdentification: value("PlotIdentification"),
            CityName: value("CityName"),
            CitySubdivisionName: value("CitySubdivisionName"),
            PostalZone: value("PostalZone"),
            IdentificationCode: value("IdentificationCode"),
            Id700: value("Id700"),
            BoxMil: value("BoxMil"),
            ZipCode: value("ZipCode"),
            TaxExempt: checked("TaxExempt"),
            Export: checked("Export"),
            BankName: value("BankName"),
            BankAccount: value("BankAccount"),
            BankCode: value("BankCode"),
            BankIban: value("BankIban"),
            BankAddress: value("BankAddress"),
            Iban: value("Iban")
        };
    }

    function apply(data) {
        data = data || {};
        setValue("CusId", data.CusId);
        setValue("AccountCode", data.AccountCode);
        setValue("ParentAccount", data.ParentAccount);
        setValue("CusName", data.CusName);
        setValue("CusNameEnglish", data.CusNameEnglish);
        setValue("ResponsibleContact", data.ResponsibleContact);
        setValue("Phone", data.Phone);
        setValue("Mobile", data.Mobile);
        setValue("FaxNumber", data.FaxNumber);
        setValue("Email", data.Email);
        setValue("Address", data.Address);
        setValue("Remark", data.Remark);
        setValue("Remark2", data.Remark2);
        setValue("Type", data.Type || 1);
        setValue("Prefix", data.Prefix);
        setValue("Code", data.Code);
        setValue("FullCode", data.FullCode);
        setValue("BranchId", data.BranchId);
        setValue("RecordDate", toDateInput(data.RecordDate));
        setValue("OpenBalance", data.OpenBalance);
        setValue("OpenBalanceType", data.OpenBalanceType);
        setValue("OpenBalance1", data.OpenBalance1);
        setValue("OpenBalanceType1", data.OpenBalanceType1);
        setValue("OpenBalance2", data.OpenBalance2);
        setValue("OpenBalanceType2", data.OpenBalanceType2);
        setValue("OpenBalanceDate", toDateInput(data.OpenBalanceDate));
        setValue("CreditLimit", data.CreditLimit);
        setValue("CreditLimitCredit", data.CreditLimitCredit);
        setValue("DebitInterval", data.DebitInterval);
        setValue("CreditInterval", data.CreditInterval);
        setValue("PaymentType", data.PaymentType);
        setValue("ClassCustomersId", data.ClassCustomersId);
        setValue("GroupsCustomersId", data.GroupsCustomersId);
        setValue("SaleType", data.SaleType);
        setValue("DiscountType", data.DiscountType);
        setValue("DiscountValue", data.DiscountValue);
        setValue("PurchaseDiscountType", data.PurchaseDiscountType);
        setValue("PurchaseDiscountValue", data.PurchaseDiscountValue);
        setChecked("CustomerAndVendor", data.CustomerAndVendor);
        setChecked("IsLocked", data.IsLocked);
        setChecked("CreditLocked", data.CreditLocked);
        setValue("NationalNo", data.NationalNo);
        setValue("VatNo", data.VatNo);
        setValue("CommercialRegisterNo", data.CommercialRegisterNo);
        setValue("TaxCardNo", data.TaxCardNo);
        setValue("ImportCardNo", data.ImportCardNo);
        setValue("ExportCardNo", data.ExportCardNo);
        setValue("StreetName", data.StreetName);
        setValue("AdditionalStreetName", data.AdditionalStreetName);
        setValue("BuildingNumber", data.BuildingNumber);
        setValue("PlotIdentification", data.PlotIdentification);
        setValue("CityName", data.CityName);
        setValue("CitySubdivisionName", data.CitySubdivisionName);
        setValue("PostalZone", data.PostalZone);
        setValue("IdentificationCode", data.IdentificationCode);
        setValue("Id700", data.Id700);
        setValue("BoxMil", data.BoxMil);
        setValue("ZipCode", data.ZipCode);
        setChecked("TaxExempt", data.TaxExempt);
        setChecked("Export", data.Export);
        setValue("BankName", data.BankName);
        setValue("BankAccount", data.BankAccount);
        setValue("BankCode", data.BankCode);
        setValue("BankIban", data.BankIban);
        setValue("BankAddress", data.BankAddress);
        setValue("Iban", data.Iban);

        byId("customerCodeBadge").textContent = data.CusId || "جديد";
        byId("customerEditorTitle").textContent = data.CusId ? "تعديل عميل" : "عميل جديد";
        byId("customerAccountBadge").textContent = data.AccountDisplay || "ينشأ عند الحفظ";
        if (deleteBtn) { deleteBtn.disabled = !data.CusId; }
    }

    function toDateInput(value) {
        if (!value) { return ""; }
        if (typeof value === "string") {
            var match = value.match(/\d{4}-\d{2}-\d{2}/);
            if (match) { return match[0]; }
            var ticks = value.match(/\/Date\((\d+)\)\//);
            if (ticks) {
                return new Date(parseInt(ticks[1], 10)).toISOString().slice(0, 10);
            }
        }
        return "";
    }

    function refreshFullCode() {
        setValue("FullCode", (value("Prefix") || "") + (value("Code") || ""));
    }

    function loadCustomer(id) {
        fetch(config.detailsUrl + "?id=" + encodeURIComponent(id), { credentials: "same-origin" })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                if (!result.success) {
                    showStatus(result.message || "تعذر تحميل بيانات العميل.", "error");
                    return;
                }

                apply(result.data);
                showStatus("تم تحميل بيانات العميل.", "success");
            })
            .catch(function () { showStatus("تعذر الاتصال بالخادم.", "error"); });
    }

    if (newBtn) {
        newBtn.addEventListener("click", function () {
            fetch(config.newUrl, { credentials: "same-origin" })
                .then(function (response) { return response.json(); })
                .then(function (result) {
                    if (!result.success) {
                        showStatus(result.message || "تعذر تجهيز عميل جديد.", "error");
                        return;
                    }

                    apply(result.data);
                    showStatus("جاهز لإضافة عميل جديد.", "info");
                });
        });
    }

    if (saveBtn) {
        saveBtn.addEventListener("click", function () {
            var data = collect();
            if (!data.CusName || !data.CusName.trim()) {
                showStatus("يجب إدخال اسم العميل.", "error");
                byId("CusName").focus();
                return;
            }

            saveBtn.disabled = true;
            post(config.saveUrl, data)
                .then(function (result) {
                    if (!result.Success) {
                        showStatus(result.Message || "تعذر حفظ بيانات العميل.", "error");
                        return;
                    }

                    apply(result.Customer || data);
                    showStatus(result.Message || "تم الحفظ بنجاح.", "success");
                })
                .catch(function () { showStatus("تعذر الاتصال بالخادم أثناء الحفظ.", "error"); })
                .finally(function () { saveBtn.disabled = false; });
        });
    }

    if (deleteBtn) {
        deleteBtn.addEventListener("click", function () {
            var id = value("CusId");
            if (!id) {
                showStatus("اختر العميل المطلوب حذفه أولاً.", "error");
                return;
            }

            if (!window.confirm("تأكيد حذف العميل المحدد؟")) {
                return;
            }

            deleteBtn.disabled = true;
            post(config.deleteUrl, { id: id })
                .then(function (result) {
                    if (!result.Success) {
                        showStatus(result.Message || "تعذر حذف العميل.", "error");
                        return;
                    }

                    showStatus(result.Message || "تم الحذف بنجاح.", "success");
                    if (newBtn) { newBtn.click(); }
                })
                .catch(function () { showStatus("تعذر الاتصال بالخادم أثناء الحذف.", "error"); })
                .finally(function () { deleteBtn.disabled = false; });
        });
    }

    document.querySelectorAll(".js-load-customer").forEach(function (button) {
        button.addEventListener("click", function () {
            loadCustomer(button.getAttribute("data-id"));
        });
    });

    ["Prefix", "Code"].forEach(function (id) {
        var input = byId(id);
        if (input) {
            input.addEventListener("input", refreshFullCode);
        }
    });
})();
