(function () {
    var state = {
        reportId: window.DynamicReportReviewId || 0,
        scope: window.DynamicReportsScope || "Web",
        apiBase: window.DynamicReportsApiBase || "/Reports/Admin"
    };

    function api(path) {
        return state.apiBase.replace(/\/$/, "") + "/" + path + "?scope=" + encodeURIComponent(state.scope) + "&id=" + encodeURIComponent(state.reportId);
    }

    function msg(text) {
        $("#drRvMessage").text(text || "");
    }

    function renderValidation(report, status) {
        report = report || {};
        $("#drRvErrors").text(report.ErrorCount || 0);
        $("#drRvWarnings").text(report.WarningCount || 0);
        $("#drRvInfos").text(report.InfoCount || 0);
        if (status) $("#drRvStatus").text(status);
        var checks = $("#drRvChecks").empty();
        (report.CheckResults || []).forEach(function (check) {
            var level = (check.Level || "Info").toLowerCase();
            $("<div class='dr-rv-check'>")
                .addClass("dr-rv-pill-" + level)
                .append($("<strong>").text(check.Id || ""))
                .append($("<span>").text(check.Message || ""))
                .appendTo(checks);
        });
    }

    function validate() {
        msg("جاري فحص التقرير...");
        $.post(api("ValidateReport")).done(function (r) {
            renderValidation(r.data, r.status);
            msg("تم فحص التقرير.");
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر فحص التقرير.");
        });
    }

    function runSample() {
        msg("جاري تشغيل العينة...");
        $.post(api("RunSample")).done(function (r) {
            var data = r.data || {};
            $("#drRvSampleStats").text("Rows: " + (data.RowCount || 0) + " / Max: " + (data.MaxRows || 0));
            renderSample(data.Columns || [], data.Rows || []);
            msg(data.Message || "تم تشغيل العينة.");
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تشغيل العينة.");
        });
    }

    function renderSample(columns, rows) {
        var table = $("#drRvSampleTable").empty();
        var thead = $("<thead>").appendTo(table);
        var headRow = $("<tr>").appendTo(thead);
        columns.slice(0, 12).forEach(function (c) {
            $("<th>").text(c.CaptionAr || c.CaptionEn || c.FieldName).appendTo(headRow);
        });
        var tbody = $("<tbody>").appendTo(table);
        rows.slice(0, 5).forEach(function (row) {
            var tr = $("<tr>").appendTo(tbody);
            columns.slice(0, 12).forEach(function (c) {
                $("<td>").text(row[c.FieldName] == null ? "" : row[c.FieldName]).appendTo(tr);
            });
        });
    }

    function applySuggestion(field, kind) {
        var payload = {
            Field: field || "",
            Kind: kind || "",
            ApplyCaptions: kind === "caption" || kind === "all",
            ApplyFormatting: kind === "format" || kind === "all",
            ApplyWidthAlignment: kind === "layout" || kind === "all",
            ApplySort: kind === "hints" || kind === "all",
            ApplyGroupable: kind === "hints" || kind === "all",
            ApplyFilterable: kind === "hints" || kind === "all",
            ApplySortable: kind === "hints" || kind === "all",
            ApplyAggregate: kind === "hints" || kind === "all"
        };
        if (field) {
            payload.ApplyCaptions = kind === "caption";
        }
        $.ajax({
            url: api("ApplySuggestions"),
            method: "POST",
            data: JSON.stringify(payload),
            contentType: "application/json; charset=utf-8"
        }).done(function (r) {
            msg("تم تطبيق الاقتراحات. عدد الحقول/الخصائص المحدثة: " + (r.updated || 0));
            window.location.reload();
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تطبيق الاقتراحات.");
        });
    }

    function transition(toStatus) {
        msg("جاري تغيير الحالة...");
        $.post(api("TransitionStatus") + "&toStatus=" + encodeURIComponent(toStatus)).done(function (r) {
            var data = r.data || {};
            $("#drRvStatus").text(data.NewLifecycleStatus || data.NewStatus || toStatus);
            msg(data.Message || "تم تغيير الحالة.");
        }).fail(function (xhr) {
            var response = xhr.responseJSON || {};
            msg(response.message || "تعذر تغيير حالة التقرير.");
            if (response.data && response.data.Errors) {
                renderValidation({ CheckResults: response.data.Errors.map(function (e) { return { Id: "gate", Level: "Error", Message: e }; }), ErrorCount: response.data.Errors.length }, response.data.NewLifecycleStatus || response.data.NewStatus);
            }
        });
    }

    function postLifecycle(path, fallback) {
        $.post(api(path)).done(function (r) {
            var data = r.data || {};
            msg(data.Message || fallback);
            window.location.reload();
        }).fail(function (xhr) {
            msg((xhr.responseJSON && xhr.responseJSON.message) || fallback || "تعذرت العملية.");
        });
    }

    function openPrintPreview() {
        var viewerBase = state.apiBase.replace(/\/$/, "").replace(/\/Reports\/Admin$/i, "/Reports/Viewer").replace(/DynamicReportsAdmin$/i, "DynamicReports");
        var form = $("<form method='POST' target='_blank'>").attr("action", viewerBase + "/Print");
        $("<input type='hidden' name='reportId'>").val(state.reportId).appendTo(form);
        $("<input type='hidden' name='scope'>").val(state.scope).appendTo(form);
        form.appendTo(document.body);
        form[0].submit();
        form.remove();
    }

    function init(reportId, scope) {
        state.reportId = reportId || state.reportId;
        state.scope = scope || state.scope;
        $("#drRvValidate").on("click", validate);
        $("#drRvRunSample").on("click", runSample);
        $("#drRvApplyAll").on("click", function () { applySuggestion("", "all"); });
        $("[data-apply-kind]").on("click", function () { applySuggestion("", $(this).attr("data-apply-kind")); });
        $("[data-apply-caption]").on("click", function () { applySuggestion($(this).attr("data-apply-caption"), "caption"); });
        $("[data-transition]").on("click", function () { transition($(this).attr("data-transition")); });
        $("#drRvMarkReviewed").on("click", function () { postLifecycle("MarkReviewed", "تم حفظ المراجعة."); });
        $("#drRvProductionReady").on("click", function () { postLifecycle("MarkProductionReady", "تم اعتماد التقرير كجاهز للإنتاج."); });
        $("#drRvCertified").on("click", function () { postLifecycle("MarkCertified", "تم اعتماد التقرير نهائيًا."); });
        $("#drRvRevertReview").on("click", function () { postLifecycle("RevertReview", "تم إلغاء الاعتماد."); });
        $("#drRvPrint").on("click", openPrintPreview);
    }

    window.DynamicReportsReview = {
        init: init,
        validate: validate,
        runSample: runSample,
        applySuggestion: applySuggestion,
        applyAllSuggestions: function () { applySuggestion("", "all"); },
        transition: transition,
        markReviewed: function () { postLifecycle("MarkReviewed", "تم حفظ المراجعة."); },
        revertReview: function () { postLifecycle("RevertReview", "تم إلغاء الاعتماد."); },
        openPrintPreview: openPrintPreview
    };

    $(function () {
        init(state.reportId, state.scope);
    });
})();
