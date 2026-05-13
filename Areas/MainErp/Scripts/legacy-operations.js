(function () {
    var root = document.getElementById("legacyOpsPage");
    if (!root) return;
    var lookups = JSON.parse(document.getElementById("legacyLookups").textContent || "{}");
    var msg = document.getElementById("legacyMessage");
    var active = "boxes";

    function show(text, ok) {
        msg.textContent = text || "";
        msg.className = "legacy-message " + (ok ? "ok" : "bad");
    }

    function url(name) { return root.getAttribute("data-" + name + "-url"); }
    function today() { return new Date().toISOString().slice(0, 10); }
    function val(form, name) { var el = form.querySelector("[name='" + name + "']"); return el ? el.value : ""; }
    function num(v) { var n = parseFloat(v); return isNaN(n) ? 0 : n; }
    function intOrNull(v) { var n = parseInt(v, 10); return isNaN(n) ? null : n; }
    function dateValue(v) { return v ? v : null; }

    function post(target, data, done) {
        $.ajax({ url: target, method: "POST", data: JSON.stringify(data), contentType: "application/json; charset=utf-8" })
            .done(function (r) { show(r.Message || r.message || (r.Success ? "تم الحفظ" : ""), !!(r.Success || r.success)); if (done) done(r); })
            .fail(function (x) { show(x.responseText || "تعذر تنفيذ العملية", false); });
    }

    function get(target, data, done) {
        $.getJSON(target, data || {}).done(done).fail(function () { show("تعذر تحميل البيانات", false); });
    }

    root.querySelectorAll(".legacy-tabs button").forEach(function (b) {
        b.addEventListener("click", function () {
            active = b.getAttribute("data-tab");
            root.querySelectorAll(".legacy-tabs button").forEach(function (x) { x.classList.toggle("active", x === b); });
            root.querySelectorAll(".legacy-tab").forEach(function (x) { x.classList.toggle("active", x.getAttribute("data-panel") === active); });
            loadActive();
        });
    });

    document.getElementById("legacySearch").addEventListener("input", function () { clearTimeout(this._t); this._t = setTimeout(loadActive, 250); });

    function loadActive() {
        var search = document.getElementById("legacySearch").value;
        if (active === "boxes") loadBoxes(search);
        if (active === "cashing") loadCashing(search);
        if (active === "car") loadCar(search);
        if (active === "cars") loadCars(search);
        if (active === "carAuth") loadCarAuth(search);
    }

    function fillForm(form, data) {
        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el.name) return;
            var value = data[el.name];
            if (value === undefined) value = data[el.name.charAt(0).toLowerCase() + el.name.slice(1)];
            if (el.type === "checkbox") el.checked = !!value;
            else if (el.type === "date" && value) el.value = String(value).slice(0, 10);
            else el.value = value == null ? "" : value;
        });
    }

    function clearForm(id) {
        var form = document.getElementById(id);
        form.reset();
        form.querySelectorAll("input[type=hidden]").forEach(function (x) { x.value = ""; });
        form.querySelectorAll("input[type=date]").forEach(function (x) { x.value = today(); });
        if (id === "cashingForm") $("#cashingLines").empty(), addCashingLine();
        if (id === "carForm") $("#carLines").empty(), addCarLine(0);
        if (id === "carsForm") $("#carParts").empty(), clearGallery("carsGallery");
        if (id === "carAuthForm") $("#carAuthLines").empty(), $("#carAuthItems").empty(), addCarAuthLine(0), addCarAuthItem();
        if (id === "carAuthForm") clearGallery("carAuthGallery");
    }

    function loadBoxes(search) {
        get(url("boxes"), { search: search }, function (r) {
            var body = $("#boxesRows").empty();
            (r.data || []).forEach(function (x) {
                $("<tr>").append("<td>" + x.Id + "</td><td>" + (x.Name || "") + "</td><td>" + (x.AccountCode || "") + "</td>")
                    .on("click", function () { get(url("box"), { id: x.Id }, function (d) { fillForm(document.getElementById("boxForm"), d.data || {}); }); })
                    .appendTo(body);
            });
        });
    }

    function loadCashing(search) {
        get(url("cashing"), { search: search }, function (r) {
            var body = $("#cashingRows").empty();
            (r.data || []).forEach(function (x) {
                $("<tr>").append("<td>" + x.Id + "</td><td>" + (x.VoucherSerial || "") + "</td><td>" + ((x.RecordDate || "").slice(0, 10)) + "</td>")
                    .on("click", function () { get(url("cashing-details"), { id: x.Id }, bindCashing); })
                    .appendTo(body);
            });
        });
    }

    function loadCar(search) {
        get(url("car"), { search: search }, function (r) {
            var body = $("#carRows").empty();
            (r.data || []).forEach(function (x) {
                $("<tr>").append("<td>" + x.Id + "</td><td>" + (x.ClientName || "") + "</td><td>" + (x.TotalValue || 0) + "</td>")
                    .on("click", function () { get(url("car-details"), { id: x.Id }, bindCar); })
                    .appendTo(body);
            });
        });
    }

    function loadCars(search) {
        get(url("cars"), { search: search }, function (r) {
            var body = $("#carsRows").empty();
            (r.data || []).forEach(function (x) {
                $("<tr>").append("<td>" + (x.FullCode || x.Id) + "</td><td>" + (x.BoardNo || "") + "</td><td>" + (x.Name || "") + "</td>")
                    .on("click", function () { get(url("car-data"), { id: x.Id }, bindCars); })
                    .appendTo(body);
            });
        });
    }

    function loadCarAuth(search) {
        get(url("car-auth"), { search: search }, function (r) {
            var body = $("#carAuthRows").empty();
            (r.data || []).forEach(function (x) {
                $("<tr>").append("<td>" + x.Id + "</td><td>" + (x.ClientName || "") + "</td><td>" + (x.PlateNo || "") + "</td>")
                    .on("click", function () { get(url("car-auth-details"), { id: x.Id }, bindCarAuth); })
                    .appendTo(body);
            });
        });
    }

    function paymentOptions(selected) {
        var html = '<option value="0">نقدي</option>';
        (lookups.PaymentTypes || []).forEach(function (x) { html += '<option value="' + x.Id + '" data-us="' + (x.AccountCode || "") + '">' + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function accountOptions(selected) {
        var html = '<option value="">اختر</option>';
        (lookups.Accounts || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + x.Id + " - " + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function addCashingLine(line) {
        line = line || {};
        $("#cashingLines").append('<tr><td><select class="pay">' + paymentOptions(line.PaymentId || 0) + '</select></td><td><input class="collected" type="number" step="0.01" value="' + (line.CollectedValue || "") + '"></td><td><input class="commission" type="number" step="0.01" value="' + (line.CommissionValue || 0) + '"></td><td><input class="net" type="number" step="0.01" value="' + (line.NetValue || line.CollectedValue || "") + '"></td><td><input class="remarks" value="' + (line.Remarks || "") + '"></td><td><button type="button" class="remove">×</button></td></tr>');
    }

    function bindCashing(r) {
        var data = r.data || {};
        fillForm(document.getElementById("cashingForm"), data);
        $("#cashingLines").empty();
        (data.Lines || data.lines || []).forEach(addCashingLine);
        if (!$("#cashingLines tr").length) addCashingLine();
    }

    function maintenanceOptions(type, selected) {
        var source = type === 1 ? lookups.ExtraExpenses : lookups.MaintenanceWorks;
        var html = '<option value="">اختر</option>';
        (source || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function addCarLine(type, line) {
        line = line || {};
        type = line.Type == null ? type : line.Type;
        $("#carLines").append('<tr><td><select class="type"><option value="0">عمل صيانة</option><option value="1">مصروف إضافي</option></select></td><td><select class="mainte">' + maintenanceOptions(type, line.MainteId || "") + '</select></td><td><input class="value" type="number" step="0.01" value="' + (line.Value || "") + '"></td><td><input class="count" type="number" value="' + (line.Count || 1) + '"></td><td><input class="total" type="number" step="0.01" value="' + (line.TotalNet || line.Value || "") + '"></td><td><select class="account">' + accountOptions(line.AccountCode || "") + '</select></td><td><button type="button" class="remove">×</button></td></tr>');
        $("#carLines tr:last .type").val(type);
    }

    function employeeOptions(selected) {
        var html = '<option value="">اختر</option>';
        (lookups.Employees || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function departmentOptions(selected) {
        var html = '<option value="">اختر</option>';
        (lookups.Departments || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function itemOptions(selected) {
        var html = '<option value="">اختر</option>';
        (lookups.Items || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function fixedAssetOptions(selected) {
        var html = '<option value="">اختر</option>';
        (lookups.FixedAssets || []).forEach(function (x) { html += '<option value="' + x.Id + '">' + (x.AccountCode ? x.AccountCode + " - " : "") + x.Text + "</option>"; });
        return html.replace('value="' + selected + '"', 'value="' + selected + '" selected');
    }

    function addCarPart(part) {
        part = part || {};
        $("#carParts").append('<tr><td><select class="part">' + fixedAssetOptions(part.PartId || "") + '</select></td><td><button type="button" class="remove">×</button></td></tr>');
    }

    function addCarAuthLine(type, line) {
        line = line || {};
        type = line.Type == null ? type : line.Type;
        $("#carAuthLines").append('<tr><td><select class="type"><option value="0">عمل صيانة</option><option value="1">مصروف إضافي</option></select></td><td><select class="mainte">' + maintenanceOptions(type, line.MainteId || "") + '</select></td><td><input class="value" type="number" step="0.01" value="' + (line.Value || "") + '"></td><td><input class="count" type="number" value="' + (line.Count || 1) + '"></td><td><select class="employee">' + employeeOptions(line.EmployeeId || "") + '</select></td><td><select class="department">' + departmentOptions(line.DepartmentId || "") + '</select></td><td><button type="button" class="remove">×</button></td></tr>');
        $("#carAuthLines tr:last .type").val(type);
    }

    function addCarAuthItem(item) {
        item = item || {};
        $("#carAuthItems").append('<tr><td><select class="item">' + itemOptions(item.ItemId || "") + '</select></td><td><input class="qty" type="number" step="0.01" value="' + (item.Qty || 1) + '"></td><td><input class="price" type="number" step="0.01" value="' + (item.Price || "") + '"></td><td><input class="vat" type="number" step="0.01" value="' + (item.VatValue || 0) + '"></td><td><input class="total" type="number" step="0.01" value="' + (item.TotalWithVat || "") + '"></td><td><input class="remark" value="' + (item.Remark || "") + '"></td><td><button type="button" class="remove">×</button></td></tr>');
    }

    function bindCar(r) {
        var data = r.data || {};
        fillForm(document.getElementById("carForm"), data);
        $("#carLines").empty();
        (data.Lines || data.lines || []).forEach(function (x) { addCarLine(0, x); });
        if (!$("#carLines tr").length) addCarLine(0);
    }

    function bindCarAuth(r) {
        var data = r.data || {};
        fillForm(document.getElementById("carAuthForm"), data);
        $("#carAuthLines").empty();
        (data.Lines || data.lines || []).forEach(function (x) { addCarAuthLine(0, x); });
        if (!$("#carAuthLines tr").length) addCarAuthLine(0);
        $("#carAuthItems").empty();
        (data.Items || data.items || []).forEach(addCarAuthItem);
        if (!$("#carAuthItems tr").length) addCarAuthItem();
        loadAttachments("FrmCarAuthontication", data.Id || data.id, "carAuthGallery");
    }

    function bindCars(r) {
        var data = r.data || {};
        fillForm(document.getElementById("carsForm"), data);
        $("#carParts").empty();
        (data.Parts || data.parts || []).forEach(addCarPart);
        loadAttachments("FrmCars", data.Id || data.id, "carsGallery");
    }

    function clearGallery(id) {
        $("#" + id).empty().append('<div class="legacy-empty-media">لا توجد صور بعد</div>');
    }

    function loadAttachments(screenName, recordId, galleryId) {
        if (!recordId) { clearGallery(galleryId); return; }
        get(url("attachments"), { screenName: screenName, recordId: recordId }, function (r) {
            var gallery = $("#" + galleryId).empty();
            var data = r.data || [];
            if (!data.length) { clearGallery(galleryId); return; }
            data.forEach(function (x) {
                var isImage = (x.ContentType || "").indexOf("image/") === 0 || /\.(jpg|jpeg|png|webp)$/i.test(x.FilePath || "");
                var media = isImage ? '<img src="' + x.FilePath + '" alt="">' : '<div class="legacy-file-tile">PDF</div>';
                $('<figure class="' + (x.IsPrimary ? "primary" : "") + '">' + media + '<figcaption><span>' + (x.Caption || x.FileName || "") + '</span><button type="button" data-primary-attachment="' + x.Id + '">رئيسية</button><button type="button" data-delete-attachment="' + x.Id + '">حذف</button></figcaption></figure>').appendTo(gallery);
            });
        });
    }

    function uploadAttachments(input, screenName) {
        var formId = screenName === "FrmCars" ? "carsForm" : "carAuthForm";
        var galleryId = screenName === "FrmCars" ? "carsGallery" : "carAuthGallery";
        var recordId = intOrNull(val(document.getElementById(formId), "Id"));
        if (!recordId) { show("احفظ السجل أولا قبل رفع الصور.", false); input.value = ""; return; }
        Array.prototype.forEach.call(input.files || [], function (file) {
            var data = new FormData();
            data.append("screenName", screenName);
            data.append("recordId", recordId);
            data.append("file", file);
            $.ajax({ url: url("upload-attachment"), method: "POST", data: data, contentType: false, processData: false })
                .done(function (r) { show(r.Message || r.message, !!(r.Success || r.success)); loadAttachments(screenName, recordId, galleryId); })
                .fail(function (x) { show(x.responseText || "تعذر رفع الملف", false); });
        });
        input.value = "";
    }

    function uploadFileList(files, screenName) {
        var proxy = { files: files, value: "" };
        uploadAttachments(proxy, screenName);
    }

    $("#boxForm").on("submit", function (e) {
        e.preventDefault();
        var f = this;
        post(url("save-box"), {
            Id: intOrNull(val(f, "Id")), Name: val(f, "Name"), EnglishName: val(f, "EnglishName"), BranchId: intOrNull(val(f, "BranchId")),
            Type: intOrNull(val(f, "Type")) || 0, ParentAccountCode: val(f, "ParentAccountCode"), AccountCode: val(f, "AccountCode"),
            OpenBalance: num(val(f, "OpenBalance")), OpenBalanceDate: dateValue(val(f, "OpenBalanceDate")), Remarks: val(f, "Remarks")
        }, function (r) { if (r.Success) loadBoxes(); });
    });

    $("#cashingForm").on("submit", function (e) {
        e.preventDefault();
        var f = this, lines = [];
        $("#cashingLines tr").each(function () {
            var row = $(this), collected = num(row.find(".collected").val()), commission = num(row.find(".commission").val());
            lines.push({ PaymentId: intOrNull(row.find(".pay").val()) || 0, Balance: collected, CollectedValue: collected, CommissionValue: commission, NetValue: num(row.find(".net").val()) || collected - commission, Remarks: row.find(".remarks").val() });
        });
        post(url("save-cashing"), { Id: intOrNull(val(f, "Id")), NoteId: intOrNull(val(f, "NoteId")), RecordDate: val(f, "RecordDate"), FromDate: val(f, "FromDate"), ToDate: val(f, "ToDate"), BranchId: intOrNull(val(f, "BranchId")), GeneralBoxId: intOrNull(val(f, "GeneralBoxId")), SubBoxId: intOrNull(val(f, "SubBoxId")), CashierId: intOrNull(val(f, "CashierId")), Remarks: val(f, "Remarks"), Lines: lines }, function (r) { if (r.Success) loadCashing(); });
    });

    $("#carForm").on("submit", function (e) {
        e.preventDefault();
        var f = this, lines = [];
        $("#carLines tr").each(function () {
            var row = $(this);
            lines.push({ Type: intOrNull(row.find(".type").val()) || 0, MainteId: intOrNull(row.find(".mainte").val()), Value: num(row.find(".value").val()), Count: intOrNull(row.find(".count").val()) || 1, TotalNet: num(row.find(".total").val()), AccountCode: row.find(".account").val() });
        });
        post(url("save-car"), { Id: intOrNull(val(f, "Id")), NoteId: intOrNull(val(f, "NoteId")), RecordDate: val(f, "RecordDate"), EndDate: val(f, "EndDate"), BranchId: intOrNull(val(f, "BranchId")), CustomerId: intOrNull(val(f, "CustomerId")), ClientName: val(f, "ClientName"), Telephone: val(f, "Telephone"), CarTypeId: intOrNull(val(f, "CarTypeId")), CarModelId: intOrNull(val(f, "CarModelId")), ColorId: intOrNull(val(f, "ColorId")), PlateNo: val(f, "PlateNo"), PaymentType: intOrNull(val(f, "PaymentType")) || 0, BoxId: intOrNull(val(f, "BoxId")), PaymentValue: num(val(f, "PaymentValue")), VatValue: num(val(f, "VatValue")), VatAccountCode: val(f, "VatAccountCode"), Lines: lines }, function (r) { if (r.Success) loadCar(); });
    });

    $("#carsForm").on("submit", function (e) {
        e.preventDefault();
        var f = this;
        post(url("save-car-data"), {
            Id: intOrNull(val(f, "Id")), Code: val(f, "Code"), Prefix: val(f, "Prefix"), FullCode: val(f, "FullCode"),
            BranchId: intOrNull(val(f, "BranchId")), CarTypeId: intOrNull(val(f, "CarTypeId")), CarModelId: intOrNull(val(f, "CarModelId")), ColorId: intOrNull(val(f, "ColorId")), EmployeeId: intOrNull(val(f, "EmployeeId")),
            Name: val(f, "Name"), BoardNo: val(f, "BoardNo"), LicenseNo: val(f, "LicenseNo"), Model: val(f, "Model"), EquipmentName: val(f, "EquipmentName"), LeaderName: val(f, "LeaderName"),
            PurchaseDate: val(f, "PurchaseDate"), LicenseExpireDate: val(f, "LicenseExpireDate"), InsuranceExpireDate: val(f, "InsuranceExpireDate"), TestExpireDate: val(f, "TestExpireDate"),
            LastKMCounter: num(val(f, "LastKMCounter")), Capacity: num(val(f, "Capacity")), Rate: num(val(f, "Rate")), Total: num(val(f, "Total")), EngineNo: val(f, "EngineNo"), Chassis: val(f, "Chassis"), GearNo: val(f, "GearNo"), Notes: val(f, "Notes"),
            FormOriginal: f.FormOriginal.checked, AuthorizeLicense: f.AuthorizeLicense.checked, AuthorizeExamination: f.AuthorizeExamination.checked, SpareTyre: f.SpareTyre.checked, Battery: f.Battery.checked, Guarantee: f.Guarantee.checked,
            Parts: $("#carParts tr").map(function () { return { PartId: intOrNull($(this).find(".part").val()) }; }).get()
        }, function (r) { if (r.Success) { if (r.Id) f.Id.value = r.Id; loadCars(); loadAttachments("FrmCars", r.Id || intOrNull(f.Id.value), "carsGallery"); } });
    });

    $("#carAuthForm").on("submit", function (e) {
        e.preventDefault();
        var f = this, lines = [], items = [];
        $("#carAuthLines tr").each(function () {
            var row = $(this);
            lines.push({ Type: intOrNull(row.find(".type").val()) || 0, MainteId: intOrNull(row.find(".mainte").val()), Value: num(row.find(".value").val()), Count: intOrNull(row.find(".count").val()) || 1, EmployeeId: intOrNull(row.find(".employee").val()), DepartmentId: intOrNull(row.find(".department").val()) });
        });
        $("#carAuthItems tr").each(function () {
            var row = $(this), qty = num(row.find(".qty").val()), price = num(row.find(".price").val()), vat = num(row.find(".vat").val());
            items.push({ ItemId: intOrNull(row.find(".item").val()), Qty: qty, Price: price, VatValue: vat, TotalWithVat: num(row.find(".total").val()) || qty * price + vat, BeforeVat: qty * price, Remark: row.find(".remark").val() });
        });
        post(url("save-car-auth"), {
            Id: intOrNull(val(f, "Id")), RecordDate: val(f, "RecordDate"), EndDate: val(f, "EndDate"), BranchId: intOrNull(val(f, "BranchId")), CustomerId: intOrNull(val(f, "CustomerId")), ClientCode: val(f, "ClientCode"), ClientName: val(f, "ClientName"), Telephone: val(f, "Telephone"), Mobile: val(f, "Mobile"),
            CarId: intOrNull(val(f, "CarId")), CarTypeId: intOrNull(val(f, "CarTypeId")), CarModelId: intOrNull(val(f, "CarModelId")), ColorId: intOrNull(val(f, "ColorId")), PlateNo: val(f, "PlateNo"), YearFact: intOrNull(val(f, "YearFact")), OrderStatus: intOrNull(val(f, "OrderStatus")) || 0,
            CarMeter: num(val(f, "CarMeter")), CarMeterOut: num(val(f, "CarMeterOut")), Chassis: val(f, "Chassis"), WorkOrder: num(val(f, "WorkOrder")), AuthOrder: num(val(f, "AuthOrder")), Complaint: val(f, "Complaint"), InitialNote: val(f, "InitialNote"),
            Cash: f.Cash.checked, Account: f.Account.checked, Credit: f.Credit.checked, Accepted: f.Accepted.checked, Finished: f.Finished.checked, Waiting: f.Waiting.checked, SendSms: f.SendSms.checked, QrCodeData: val(f, "QrCodeData"), QrCodeDataPath: val(f, "QrCodeDataPath"),
            SubCar1: f.SubCar1.checked, SubCar2: f.SubCar2.checked, SubCar3: f.SubCar3.checked, SubCar4: f.SubCar4.checked, SubCar5: f.SubCar5.checked, SubCar6: f.SubCar6.checked, SubCar7: f.SubCar7.checked, SubCar8: f.SubCar8.checked, SubCar9: f.SubCar9.checked, SubCar10: f.SubCar10.checked, SubCar11: f.SubCar11.checked, SubCar12: f.SubCar12.checked, SubCar13: f.SubCar13.checked, SubCar14: f.SubCar14.checked,
            DiscountValue: num(val(f, "DiscountValue")), DiscountPercent: num(val(f, "DiscountPercent")), TotalAfterDiscount: num(val(f, "TotalAfterDiscount")), VatPercent: num(val(f, "VatPercent")), VatValue: num(val(f, "VatValue")), Lines: lines, Items: items
        }, function (r) { if (r.Success) { if (r.Id) f.Id.value = r.Id; loadCarAuth(); loadAttachments("FrmCarAuthontication", r.Id || intOrNull(f.Id.value), "carAuthGallery"); } });
    });

    root.addEventListener("click", function (e) {
        if (e.target.classList.contains("remove")) e.target.closest("tr").remove();
        if (e.target.getAttribute("data-add-line") === "cashing") addCashingLine();
        if (e.target.getAttribute("data-add-line") === "car") addCarLine(0);
        if (e.target.getAttribute("data-add-line") === "carPart") addCarPart();
        if (e.target.getAttribute("data-add-line") === "carAuth") addCarAuthLine(0);
        if (e.target.getAttribute("data-add-line") === "carAuthItem") addCarAuthItem();
        if (e.target.getAttribute("data-new") === "box") clearForm("boxForm");
        if (e.target.getAttribute("data-new") === "cashing") clearForm("cashingForm");
        if (e.target.getAttribute("data-new") === "car") clearForm("carForm");
        if (e.target.getAttribute("data-new") === "cars") clearForm("carsForm");
        if (e.target.getAttribute("data-new") === "carAuth") clearForm("carAuthForm");
        if (e.target.getAttribute("data-delete") === "box") post(url("delete-box"), { id: intOrNull(val(document.getElementById("boxForm"), "Id")) }, loadBoxes);
        if (e.target.getAttribute("data-delete") === "cashing") post(url("delete-cashing"), { id: intOrNull(val(document.getElementById("cashingForm"), "Id")) }, loadCashing);
        if (e.target.getAttribute("data-delete") === "car") post(url("delete-car"), { id: intOrNull(val(document.getElementById("carForm"), "Id")) }, loadCar);
        if (e.target.getAttribute("data-delete") === "cars") post(url("delete-car-data"), { id: intOrNull(val(document.getElementById("carsForm"), "Id")) }, loadCars);
        if (e.target.getAttribute("data-delete") === "carAuth") post(url("delete-car-auth"), { id: intOrNull(val(document.getElementById("carAuthForm"), "Id")) }, loadCarAuth);
        if (e.target.getAttribute("data-delete-attachment")) {
            post(url("delete-attachment"), { id: intOrNull(e.target.getAttribute("data-delete-attachment")) }, function () { loadAttachments(active === "cars" ? "FrmCars" : "FrmCarAuthontication", intOrNull(val(document.getElementById(active === "cars" ? "carsForm" : "carAuthForm"), "Id")), active === "cars" ? "carsGallery" : "carAuthGallery"); });
        }
        if (e.target.getAttribute("data-primary-attachment")) {
            post(url("primary-attachment"), { id: intOrNull(e.target.getAttribute("data-primary-attachment")) }, function () { loadAttachments(active === "cars" ? "FrmCars" : "FrmCarAuthontication", intOrNull(val(document.getElementById(active === "cars" ? "carsForm" : "carAuthForm"), "Id")), active === "cars" ? "carsGallery" : "carAuthGallery"); });
        }
    });

    $("#carLines").on("change", ".type", function () { $(this).closest("tr").find(".mainte").html(maintenanceOptions(parseInt(this.value, 10), "")); });
    $("#carAuthLines").on("change", ".type", function () { $(this).closest("tr").find(".mainte").html(maintenanceOptions(parseInt(this.value, 10), "")); });
    root.querySelectorAll("[data-upload-for]").forEach(function (x) { x.addEventListener("change", function () { uploadAttachments(this, this.getAttribute("data-upload-for")); }); });
    root.querySelectorAll(".legacy-dropzone").forEach(function (zone) {
        zone.addEventListener("dragover", function (e) { e.preventDefault(); zone.classList.add("dragover"); });
        zone.addEventListener("dragleave", function () { zone.classList.remove("dragover"); });
        zone.addEventListener("drop", function (e) {
            e.preventDefault(); zone.classList.remove("dragover");
            var input = zone.querySelector("[data-upload-for]");
            uploadFileList(e.dataTransfer.files, input.getAttribute("data-upload-for"));
        });
    });
    clearForm("boxForm"); clearForm("cashingForm"); clearForm("carForm"); clearForm("carsForm"); clearForm("carAuthForm"); loadActive();
})();
