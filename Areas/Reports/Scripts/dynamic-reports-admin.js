(function () {
    var state = {
        scope: window.DynamicReportsScope || "Web",
        apiBase: window.DynamicReportsApiBase || "/Reports/Admin",
        current: null
    };

    function api(path) {
        return state.apiBase.replace(/\/$/, "") + "/" + path + "?scope=" + encodeURIComponent(state.scope);
    }

    function msg(text) {
        $("#drMessage").text(text || "");
    }

    function emptyDefinition() {
        return {
            ReportId: 0,
            ReportCode: "",
            ReportNameAr: "",
            ReportNameEn: "",
            ProjectScope: state.scope,
            SourceType: "StoredProcedure",
            SourceName: "",
            RequireDateRange: false,
            MaxRows: 1000,
            CommandTimeoutSeconds: 30,
            IsActive: true,
            Parameters: [],
            Columns: []
        };
    }

    function loadList() {
        $.getJSON(api("List")).done(function (r) {
            var tbody = $("#drReports tbody").empty();
            (r.data || []).forEach(function (item) {
                $("<tr>")
                    .append($("<td>").text(item.ReportCode))
                    .append($("<td>").text(item.ReportNameAr || item.ReportNameEn))
                    .append($("<td>").text(item.ProjectScope))
                    .append($("<td>").text(item.SourceType))
                    .append($("<td>").text(item.IsActive ? "نشط" : "متوقف"))
                    .append($("<td>").append($("<button class='dr-button secondary' type='button'>").text("فتح").on("click", function () { loadDefinition(item.ReportId); })))
                    .appendTo(tbody);
            });
        }).fail(function () { msg("تعذر تحميل قائمة التقارير"); });
    }

    function loadDefinition(id) {
        $.getJSON(api("Get") + "&id=" + id).done(function (r) {
            state.current = r.data;
            bindForm();
            msg("تم تحميل التقرير");
        }).fail(function () { msg("تعذر تحميل التقرير"); });
    }

    function bindForm() {
        var d = state.current || emptyDefinition();
        $("#ReportId").val(d.ReportId || 0);
        $("#ReportCode").val(d.ReportCode || "");
        $("#ReportNameAr").val(d.ReportNameAr || "");
        $("#ReportNameEn").val(d.ReportNameEn || "");
        $("#ProjectScope").val(d.ProjectScope || state.scope);
        $("#SourceType").val(d.SourceType || "StoredProcedure");
        $("#SourceName").val(d.SourceName || "");
        $("#RequireDateRange").prop("checked", !!d.RequireDateRange);
        $("#MaxRows").val(d.MaxRows || 1000);
        $("#CommandTimeoutSeconds").val(d.CommandTimeoutSeconds || 30);
        $("#IsActive").prop("checked", d.IsActive !== false);
        bindParameters(d.Parameters || []);
        bindColumns(d.Columns || []);
    }

    function collectForm() {
        return {
            ReportId: parseInt($("#ReportId").val(), 10) || 0,
            ReportCode: $("#ReportCode").val(),
            ReportNameAr: $("#ReportNameAr").val(),
            ReportNameEn: $("#ReportNameEn").val(),
            ProjectScope: $("#ProjectScope").val(),
            SourceType: $("#SourceType").val(),
            SourceName: $("#SourceName").val(),
            RequireDateRange: $("#RequireDateRange").is(":checked"),
            MaxRows: parseInt($("#MaxRows").val(), 10) || 1000,
            CommandTimeoutSeconds: parseInt($("#CommandTimeoutSeconds").val(), 10) || 30,
            IsActive: $("#IsActive").is(":checked"),
            Parameters: collectParameters(),
            Columns: collectColumns()
        };
    }

    function bindParameters(parameters) {
        var tbody = $("#drParameters tbody").empty();
        parameters.forEach(function (p, index) { addParameterRow(p, index); });
    }

    function addParameterRow(p, index) {
        p = p || {};
        $("<tr>")
            .append($("<td>").append($("<input type='text' data-field='ParameterName' dir='ltr'>").val(p.ParameterName || "")))
            .append($("<td>").append($("<input type='text' data-field='CaptionAr'>").val(p.CaptionAr || "")))
            .append($("<td>").append($("<select data-field='DataType'><option>String</option><option>Int</option><option>Decimal</option><option>Date</option><option>DateTime</option><option>Bool</option><option>Guid</option></select>").val(p.DataType || "String")))
            .append($("<td>").append($("<input type='checkbox' data-field='IsRequired'>").prop("checked", !!p.IsRequired)))
            .append($("<td>").append($("<input type='text' data-field='DefaultValue'>").val(p.DefaultValue || "")))
            .append($("<td>").append($("<input type='number' data-field='SortOrder'>").val(p.SortOrder || index || 0)))
            .append($("<td>").append($("<button class='dr-button danger' type='button'>").text("حذف").on("click", function () { $(this).closest("tr").remove(); })))
            .appendTo("#drParameters tbody");
    }

    function collectParameters() {
        var rows = [];
        $("#drParameters tbody tr").each(function () {
            rows.push({
                ParameterName: $(this).find("[data-field=ParameterName]").val(),
                CaptionAr: $(this).find("[data-field=CaptionAr]").val(),
                DataType: $(this).find("[data-field=DataType]").val(),
                IsRequired: $(this).find("[data-field=IsRequired]").is(":checked"),
                DefaultValue: $(this).find("[data-field=DefaultValue]").val(),
                SortOrder: parseInt($(this).find("[data-field=SortOrder]").val(), 10) || 0
            });
        });
        return rows;
    }

    function bindColumns(columns) {
        var tbody = $("#drColumns tbody").empty();
        columns.forEach(function (c, index) { addColumnRow(c, index); });
    }

    function addColumnRow(c, index) {
        c = c || {};
        $("<tr>")
            .append($("<td>").text(c.FieldName || "").append($("<input type='hidden' data-field='FieldName'>").val(c.FieldName || "")))
            .append($("<td>").append($("<input type='text' data-field='CaptionAr'>").val(c.CaptionAr || c.FieldName || "")))
            .append($("<td>").append($("<input type='text' data-field='CaptionEn'>").val(c.CaptionEn || c.FieldName || "")))
            .append($("<td>").text(c.DataType || "").append($("<input type='hidden' data-field='DataType'>").val(c.DataType || "")))
            .append(checkCell("IsVisibleDefault", c.IsVisibleDefault !== false))
            .append(checkCell("IsFilterable", c.IsFilterable !== false))
            .append(checkCell("IsSortable", c.IsSortable !== false))
            .append(checkCell("IsGroupable", !!c.IsGroupable))
            .append(checkCell("IsSummable", !!c.IsSummable))
            .append($("<td>").append($("<input type='number' data-field='Width'>").val(c.Width || 140)))
            .append($("<td>").append($("<input type='number' data-field='SortOrder'>").val(c.SortOrder || index || 0)))
            .appendTo("#drColumns tbody");
    }

    function checkCell(field, checked) {
        return $("<td>").append($("<input type='checkbox' data-field='" + field + "'>").prop("checked", checked));
    }

    function collectColumns() {
        var rows = [];
        $("#drColumns tbody tr").each(function () {
            rows.push({
                FieldName: $(this).find("[data-field=FieldName]").val(),
                CaptionAr: $(this).find("[data-field=CaptionAr]").val(),
                CaptionEn: $(this).find("[data-field=CaptionEn]").val(),
                DataType: $(this).find("[data-field=DataType]").val(),
                IsVisibleDefault: $(this).find("[data-field=IsVisibleDefault]").is(":checked"),
                IsFilterable: $(this).find("[data-field=IsFilterable]").is(":checked"),
                IsSortable: $(this).find("[data-field=IsSortable]").is(":checked"),
                IsGroupable: $(this).find("[data-field=IsGroupable]").is(":checked"),
                IsSummable: $(this).find("[data-field=IsSummable]").is(":checked"),
                Width: parseInt($(this).find("[data-field=Width]").val(), 10) || 140,
                SortOrder: parseInt($(this).find("[data-field=SortOrder]").val(), 10) || 0
            });
        });
        return rows;
    }

    function save() {
        $.ajax({
            url: api("Save"),
            method: "POST",
            data: JSON.stringify(collectForm()),
            contentType: "application/json; charset=utf-8"
        }).done(function (r) {
            msg("تم الحفظ");
            $("#ReportId").val(r.reportId);
            loadList();
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر الحفظ");
        });
    }

    function loadMetadata() {
        $.post(api("LoadMetadata"), {
            sourceType: $("#SourceType").val(),
            sourceName: $("#SourceName").val()
        }).done(function (r) {
            bindParameters(r.parameters || []);
            bindColumns(r.columns || []);
            msg("تم تحميل الأعمدة والباراميترز");
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تحميل الأعمدة");
        });
    }

    $(function () {
        state.current = emptyDefinition();
        bindForm();
        loadList();
        $("#drNew").on("click", function () { state.current = emptyDefinition(); bindForm(); msg(""); });
        $("#drSave").on("click", save);
        $("#drLoadMetadata").on("click", loadMetadata);
        $("#drAddParameter").on("click", function () { addParameterRow({}, $("#drParameters tbody tr").length); });
    });
})();
