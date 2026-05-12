(function () {
    var defaultLayoutVersion = 2;
    var state = {
        scope: window.DynamicReportsScope || "Web",
        apiBase: window.DynamicReportsApiBase || "/Reports/Viewer",
        definition: null,
        rows: [],
        resultMeta: null,
        columns: [],
        designMode: false,
        selectedField: null,
        sort: null,
        page: 1,
        layout: createEmptyLayout(),
        currentLayoutId: null
    };

    function api(path) {
        return state.apiBase.replace(/\/$/, "") + "/" + path + "?scope=" + encodeURIComponent(state.scope);
    }

    function createEmptyLayout() {
        return {
            designVersion: defaultLayoutVersion,
            areaScope: state ? state.scope : "Web",
            reportId: 0,
            visibleColumns: {},
            columnOrder: [],
            captions: {},
            widths: {},
            alignment: {},
            sort: [],
            filters: [],
            groupBy: [],
            summaries: {},
            formatting: {},
            conditionalFormatting: [],
            pageSize: 50,
            quickFilter: ""
        };
    }

    function msg(text, isError) {
        $("#drMessage").toggleClass("error", !!isError).text(text || "");
    }

    function label(column) {
        if (!column) return "";
        return state.layout.captions[column.FieldName] || column.CaptionAr || column.CaptionEn || column.FieldName;
    }

    function dataType(column) {
        return ((column && column.DataType) || "String").toLowerCase();
    }

    function isNumeric(column) {
        var t = dataType(column);
        return t.indexOf("int") >= 0 || t.indexOf("decimal") >= 0 || t.indexOf("number") >= 0 || t.indexOf("money") >= 0 || t.indexOf("float") >= 0 || t.indexOf("double") >= 0;
    }

    function isDate(column) {
        return dataType(column).indexOf("date") >= 0;
    }

    function isBool(column) {
        var t = dataType(column);
        return t.indexOf("bool") >= 0 || t === "bit";
    }

    function iconFor(column) {
        if (isDate(column)) return "📅";
        if (isNumeric(column)) return "#";
        if (isBool(column)) return "✓";
        return "T";
    }

    function normalizeColumns(columns) {
        state.columns = (columns || []).slice().sort(function (a, b) {
            return (a.SortOrder || 0) - (b.SortOrder || 0);
        });
        ensureLayoutDefaults();
    }

    function ensureLayoutDefaults() {
        if (!state.layout) state.layout = createEmptyLayout();
        state.layout.areaScope = state.scope;
        state.layout.reportId = state.definition ? state.definition.ReportId : 0;
        if (!state.layout.visibleColumns) state.layout.visibleColumns = {};
        if (!state.layout.columnOrder) state.layout.columnOrder = [];
        if (!state.layout.captions) state.layout.captions = {};
        if (!state.layout.widths) state.layout.widths = {};
        if (!state.layout.alignment) state.layout.alignment = {};
        if (!state.layout.sort) state.layout.sort = [];
        if (!state.layout.filters) state.layout.filters = [];
        if (!state.layout.groupBy) state.layout.groupBy = [];
        if (!state.layout.summaries) state.layout.summaries = {};
        if (!state.layout.formatting) state.layout.formatting = {};
        if (!state.layout.conditionalFormatting) state.layout.conditionalFormatting = [];
        if (!state.layout.pageSize) state.layout.pageSize = parseInt($("#drPageSize").val(), 10) || 50;

        state.columns.forEach(function (c) {
            if (!state.layout.visibleColumns.hasOwnProperty(c.FieldName)) {
                state.layout.visibleColumns[c.FieldName] = c.IsVisibleDefault !== false;
            }
            if (state.layout.columnOrder.indexOf(c.FieldName) < 0) {
                state.layout.columnOrder.push(c.FieldName);
            }
            if (!state.layout.captions[c.FieldName]) {
                state.layout.captions[c.FieldName] = c.CaptionAr || c.CaptionEn || c.FieldName;
            }
            if (!state.layout.widths[c.FieldName]) {
                state.layout.widths[c.FieldName] = c.Width || 140;
            }
            if (!state.layout.alignment[c.FieldName]) {
                state.layout.alignment[c.FieldName] = c.TextAlign || (isNumeric(c) ? "left" : "right");
            }
            if ((c.IsAggregatable || c.IsSummable) && isNumeric(c) && !state.layout.summaries[c.FieldName]) {
                state.layout.summaries[c.FieldName] = c.AggregateFunction || "sum";
            }
            if ((c.DisplayFormat || (c.DecimalPlaces !== null && c.DecimalPlaces !== undefined)) && !state.layout.formatting[c.FieldName]) {
                state.layout.formatting[c.FieldName] = { format: c.DisplayFormat || "", decimals: c.DecimalPlaces };
            }
        });
        state.layout.columnOrder = state.layout.columnOrder.filter(function (field) { return !!findColumn(field); });
    }

    function findColumn(field) {
        for (var i = 0; i < state.columns.length; i++) {
            if (state.columns[i].FieldName === field) return state.columns[i];
        }
        return null;
    }

    function orderedColumns(includeHidden) {
        ensureLayoutDefaults();
        var byName = {};
        state.columns.forEach(function (c) { byName[c.FieldName] = c; });
        return state.layout.columnOrder.map(function (field) { return byName[field]; })
            .filter(function (c) { return c && (includeHidden || state.layout.visibleColumns[c.FieldName] !== false); });
    }

    function loadReports() {
        $.getJSON(api("List")).done(function (r) {
            var select = $("#drReport").empty();
            select.append($("<option>").val("").text("اختر التقرير"));
            (r.data || []).forEach(function (item) {
                select.append($("<option>").val(item.ReportId).text(item.ReportNameAr || item.ReportNameEn || item.ReportCode));
            });
        }).fail(function () { msg("تعذر تحميل التقارير المتاحة", true); });
    }

    function loadDefinition() {
        var id = $("#drReport").val();
        if (!id) return;
        $.getJSON(api("Definition") + "&id=" + encodeURIComponent(id)).done(function (r) {
            state.definition = r.data;
            state.rows = [];
            state.resultMeta = null;
            state.layout = createEmptyLayout();
            normalizeColumns(state.definition.Columns || []);
            bindParameters();
            bindDesigner();
            loadLayouts();
            renderGrid();
            msg("تم تحميل تعريف التقرير. أدخل المعايير ثم اضغط تشغيل.");
        }).fail(function () { msg("ليست لديك صلاحية أو تعذر تحميل التقرير", true); });
    }

    function bindParameters() {
        var host = $("#drParameters").empty();
        var parameters = (state.definition && state.definition.Parameters) || [];
        if (!parameters.length) {
            host.append($("<div class='dr-empty'>").text("لا توجد معايير تشغيل لهذا التقرير."));
            return;
        }
        parameters.forEach(function (p) {
            var type = inputTypeForParameter(p);
            var input = $("<input>").attr("type", type).attr("data-param", p.ParameterName).val(p.DefaultValue || "");
            if (p.IsRequired) input.attr("required", "required");
            var labelText = (p.CaptionAr || p.CaptionEn || p.ParameterName) + (p.IsRequired ? " *" : "");
            host.append($("<div class='dr-field'>")
                .append($("<label>").text(labelText))
                .append(input)
                .append($("<span class='dr-muted'>").text(p.DataType || "String")));
        });
    }

    function inputTypeForParameter(p) {
        var t = ((p.DataType || "") + "").toLowerCase();
        if (t.indexOf("date") >= 0) return "date";
        if (t.indexOf("int") >= 0 || t.indexOf("decimal") >= 0 || t.indexOf("number") >= 0) return "number";
        if (t.indexOf("bool") >= 0 || t === "bit") return "checkbox";
        return "text";
    }

    function bindDesigner() {
        ensureLayoutDefaults();
        $("#drQuickFilter").val(state.layout.quickFilter || "");
        $("#drPageSize").val(String(state.layout.pageSize || 50));
        bindFieldChooser();
        bindGroupArea();
        bindFilters();
        bindConditionalRules();
        bindColumnProperties();
        updateDesignMode();
    }

    function bindFieldChooser() {
        var host = $("#drColumnsChooser").empty();
        var term = ($("#drFieldSearch").val() || "").toLowerCase();
        orderedColumns(true).forEach(function (c) {
            var display = label(c);
            if (term && (display + " " + c.FieldName).toLowerCase().indexOf(term) < 0) return;
            var item = $("<div class='dr-field-item'>")
                .attr("draggable", "true")
                .attr("data-field", c.FieldName)
                .toggleClass("selected", state.selectedField === c.FieldName)
                .toggleClass("dr-selected", state.selectedField === c.FieldName)
                .append($("<input type='checkbox' class='dr-field-visible'>").prop("checked", state.layout.visibleColumns[c.FieldName] !== false))
                .append($("<span class='dr-type-icon'>").text(iconFor(c)))
                .append($("<span class='dr-field-caption'>").text(display))
                .append($("<span class='dr-field-name'>").text(c.FieldName))
                .append($("<span class='dr-field-type'>").text(c.DataType || "String"))
                .append($("<button type='button' class='dr-mini-button dr-move-up'>").text("↑"))
                .append($("<button type='button' class='dr-mini-button dr-move-down'>").text("↓"));
            host.append(item);
        });
    }

    function bindGroupArea() {
        var list = $("#drGroupList").empty();
        (state.layout.groupBy || []).forEach(function (field) {
            var c = findColumn(field);
            if (!c) return;
            list.append($("<span class='dr-token'>")
                .attr("data-field", field)
                .text(label(c))
                .append($("<button type='button' class='dr-token-remove'>").text("×")));
        });
    }

    function bindColumnProperties() {
        var c = findColumn(state.selectedField);
        var host = $("#drColumnProperties").empty();
        if (!c) {
            $("#drSelectedColumnName").text("اختر عمودًا من الجدول أو الحقول.");
            host.addClass("dr-properties-empty").text("لا يوجد عمود محدد.");
            return;
        }
        host.removeClass("dr-properties-empty");
        $("#drSelectedColumnName").text(c.FieldName + " - " + (c.DataType || "String"));
        host.append(propertyInput("Caption", "العنوان", state.layout.captions[c.FieldName] || label(c), "text"));
        host.append(propertyInput("Width", "العرض", state.layout.widths[c.FieldName] || 140, "number"));
        host.append(propertySelect("Alignment", "المحاذاة", state.layout.alignment[c.FieldName] || "right", [["right", "يمين"], ["center", "وسط"], ["left", "يسار"]]));
        host.append(propertySelect("Format", "التنسيق", (state.layout.formatting[c.FieldName] || {}).format || "", formatOptions(c)));
        host.append(propertyInput("Decimals", "عدد الكسور", (state.layout.formatting[c.FieldName] || {}).decimals || "", "number"));
        host.append(propertySelect("Summary", "نوع الملخص", state.layout.summaries[c.FieldName] || "", [["", "بدون"], ["sum", "Sum"], ["avg", "Average"], ["count", "Count"], ["min", "Min"], ["max", "Max"]]));
        host.append(propertyCheck("Visible", "ظاهر", state.layout.visibleColumns[c.FieldName] !== false));
        host.append(propertyCheck("Groupable", "قابل للتجميع", c.IsGroupable !== false));
        host.append(propertyCheck("Filterable", "قابل للفلترة", c.IsFilterable !== false));
        host.append(propertyCheck("Sortable", "قابل للترتيب", c.IsSortable !== false));
        host.append(propertyCheck("Summable", "قابل للمجاميع", c.IsSummable || isNumeric(c)));
    }

    function formatOptions(c) {
        if (isDate(c)) return [["", "بدون"], ["dd/MM/yyyy", "dd/MM/yyyy"], ["yyyy-MM-dd", "yyyy-MM-dd"], ["datetime", "DateTime"]];
        if (isNumeric(c)) return [["", "بدون"], ["number", "رقم"], ["currency", "عملة"], ["percent", "نسبة"]];
        return [["", "بدون"], ["trim30", "نص مختصر 30"], ["trim60", "نص مختصر 60"]];
    }

    function propertyInput(key, text, value, type) {
        return $("<label class='dr-property'>").text(text).append($("<input>").attr("type", type).attr("data-prop", key).val(value));
    }

    function propertySelect(key, text, value, options) {
        var select = $("<select>").attr("data-prop", key);
        options.forEach(function (o) { select.append($("<option>").val(o[0]).text(o[1])); });
        select.val(value);
        return $("<label class='dr-property'>").text(text).append(select);
    }

    function propertyCheck(key, text, value) {
        return $("<label class='dr-property dr-check-property'>").append($("<input type='checkbox'>").attr("data-prop", key).prop("checked", !!value)).append(document.createTextNode(text));
    }

    function bindFilters() {
        var host = $("#drFilterRows").empty();
        (state.layout.filters || []).forEach(function (filter, index) {
            host.append(filterRow(filter, index));
        });
    }

    function filterRow(filter, index) {
        filter = filter || {};
        return $("<div class='dr-builder-row'>")
            .append(fieldSelect("field", filter.field))
            .append(operatorSelect(filter.op))
            .append($("<input type='text' data-filter='value'>").val(filter.value || "").attr("placeholder", "القيمة"))
            .append($("<input type='text' data-filter='value2'>").val(filter.value2 || "").attr("placeholder", "إلى"))
            .append(logicSelect(filter.logic || (index === 0 ? "AND" : "AND")))
            .append($("<button type='button' class='dr-mini-button danger dr-remove-filter'>").text("حذف"));
    }

    function fieldSelect(attr, value) {
        var select = $("<select>").attr("data-filter", attr).attr("data-rule", attr);
        orderedColumns(true).forEach(function (c) {
            select.append($("<option>").val(c.FieldName).text(label(c)));
        });
        select.val(value || (state.columns[0] && state.columns[0].FieldName));
        return select;
    }

    function operatorSelect(value) {
        var ops = [["eq", "يساوي"], ["neq", "لا يساوي"], ["contains", "يحتوي"], ["starts", "يبدأ بـ"], ["gt", "أكبر من"], ["lt", "أقل من"], ["between", "بين"], ["empty", "فارغ"], ["notempty", "غير فارغ"]];
        var select = $("<select data-filter='op'>");
        ops.forEach(function (o) { select.append($("<option>").val(o[0]).text(o[1])); });
        return select.val(value || "contains");
    }

    function logicSelect(value) {
        return $("<select data-filter='logic'><option value='AND'>AND</option><option value='OR'>OR</option></select>").val(value || "AND");
    }

    function bindConditionalRules() {
        var host = $("#drConditionalRows").empty();
        (state.layout.conditionalFormatting || []).forEach(function (rule) {
            host.append($("<div class='dr-builder-row'>")
                .append(fieldSelect("field", rule.field))
                .append(operatorSelect(rule.op))
                .append($("<input type='text' data-rule='value'>").val(rule.value || "").attr("placeholder", "القيمة"))
                .append($("<select data-rule='style'><option value='highlight'>تمييز</option><option value='danger'>تحذير</option><option value='success'>إيجابي</option></select>").val(rule.style || "highlight"))
                .append($("<button type='button' class='dr-mini-button danger dr-remove-rule'>").text("حذف")));
        });
    }

    function collectFilters() {
        var filters = [];
        $("#drFilterRows .dr-builder-row").each(function () {
            filters.push({
                field: $(this).find("[data-filter=field]").val(),
                op: $(this).find("[data-filter=op]").val(),
                value: $(this).find("[data-filter=value]").val(),
                value2: $(this).find("[data-filter=value2]").val(),
                logic: $(this).find("[data-filter=logic]").val() || "AND"
            });
        });
        state.layout.filters = filters;
    }

    function collectConditionalRules() {
        var rules = [];
        $("#drConditionalRows .dr-builder-row").each(function () {
            rules.push({
                field: $(this).find("[data-rule=field]").val(),
                op: $(this).find("[data-filter=op]").val(),
                value: $(this).find("[data-rule=value]").val(),
                style: $(this).find("[data-rule=style]").val()
            });
        });
        state.layout.conditionalFormatting = rules;
    }

    function collectParameterValues() {
        var parameters = {};
        var missing = false;
        $("#drParameters [data-param]").each(function () {
            var input = $(this);
            var value = input.attr("type") === "checkbox" ? input.is(":checked") : input.val();
            if (input.is("[required]") && !value) missing = true;
            parameters[input.attr("data-param")] = value;
        });
        return { parameters: parameters, missing: missing };
    }

    function execute() {
        if (!state.definition) {
            msg("اختر تقريرًا أولًا.", true);
            return;
        }
        var parameters = {};
        var missing = false;
        $("#drParameters [data-param]").each(function () {
            var input = $(this);
            var value = input.attr("type") === "checkbox" ? input.is(":checked") : input.val();
            if (input.is("[required]") && !value) missing = true;
            parameters[input.attr("data-param")] = value;
        });
        if (missing) {
            msg("أدخل المعايير المطلوبة قبل تشغيل التقرير.", true);
            return;
        }
        $("#drExecute").prop("disabled", true).text("جاري التشغيل...");
        $.ajax({
            url: api("Execute"),
            method: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ ReportId: state.definition.ReportId, Parameters: parameters })
        }).done(function (r) {
            state.rows = r.Rows || [];
            state.resultMeta = r;
            normalizeColumns(r.Columns || state.columns);
            bindDesigner();
            renderGrid();
            var text = r.Message || "تم تشغيل التقرير.";
            if (r.MaxRows && r.RowCount >= r.MaxRows) {
                text += " تم عرض أول " + r.MaxRows + " صف. ضيق الفلاتر لعرض نتائج أدق.";
            }
            msg(text);
        }).fail(function (xhr) {
            msg((xhr.responseJSON && (xhr.responseJSON.Message || xhr.responseJSON.message)) || "تعذر تشغيل التقرير", true);
        }).always(function () {
            $("#drExecute").prop("disabled", false).text("تشغيل التقرير");
        });
    }

    function filteredRows() {
        collectFilters();
        collectConditionalRules();
        var quick = ($("#drQuickFilter").val() || "").toLowerCase();
        state.layout.quickFilter = $("#drQuickFilter").val() || "";
        var rows = state.rows.filter(function (row) {
            var quickOk = !quick || JSON.stringify(row).toLowerCase().indexOf(quick) >= 0;
            return quickOk && filtersMatch(row);
        });
        var sort = state.layout.sort && state.layout.sort[0];
        if (sort && sort.field) {
            rows = rows.slice().sort(function (a, b) {
                var av = normalizeValue(a[sort.field]), bv = normalizeValue(b[sort.field]);
                if (av === bv) return 0;
                var result = av > bv ? 1 : -1;
                return sort.dir === "desc" ? -result : result;
            });
        }
        return rows;
    }

    function filtersMatch(row) {
        if (!state.layout.filters || !state.layout.filters.length) return true;
        var finalValue = null;
        state.layout.filters.forEach(function (f, index) {
            var match = filterMatch(row, f);
            if (index === 0) finalValue = match;
            else if (f.logic === "OR") finalValue = finalValue || match;
            else finalValue = finalValue && match;
        });
        return finalValue !== false;
    }

    function filterMatch(row, f) {
        if (!f || !f.field) return true;
        var raw = row[f.field];
        var value = normalizeValue(raw);
        var expected = normalizeValue(f.value);
        var expected2 = normalizeValue(f.value2);
        var text = (raw === null || raw === undefined ? "" : String(raw)).toLowerCase();
        var cmp = String(f.value || "").toLowerCase();
        switch (f.op) {
            case "eq": return String(value) === String(expected);
            case "neq": return String(value) !== String(expected);
            case "starts": return text.indexOf(cmp) === 0;
            case "gt": return value > expected;
            case "lt": return value < expected;
            case "between": return value >= expected && value <= expected2;
            case "empty": return raw === null || raw === undefined || raw === "";
            case "notempty": return !(raw === null || raw === undefined || raw === "");
            case "contains":
            default: return text.indexOf(cmp) >= 0;
        }
    }

    function normalizeValue(value) {
        if (value === null || value === undefined) return "";
        var dateTicks = parseMvcDate(value);
        if (dateTicks) return dateTicks.getTime();
        var n = parseFloat(value);
        if (!isNaN(n) && String(value).match(/^-?\d+(\.\d+)?$/)) return n;
        return String(value).toLowerCase();
    }

    function renderGrid() {
        ensureLayoutDefaults();
        var columns = orderedColumns(false);
        var rows = filteredRows();
        var pageSize = parseInt($("#drPageSize").val(), 10) || state.layout.pageSize || 50;
        state.layout.pageSize = pageSize;
        var totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
        if (state.page > totalPages) state.page = totalPages;
        var start = (state.page - 1) * pageSize;
        var pageRows = rows.slice(start, start + pageSize);
        var table = $("#drData").empty();
        var thead = $("<thead><tr></tr></thead>");
        columns.forEach(function (c) {
            var th = $("<th>")
                .attr("data-field", c.FieldName)
                .attr("draggable", state.designMode ? "true" : "false")
                .toggleClass("dr-selected", state.selectedField === c.FieldName)
                .css("width", (state.layout.widths[c.FieldName] || 140) + "px")
                .append($("<span>").text(label(c)))
                .append(sortMark(c.FieldName));
            thead.find("tr").append(th);
        });
        table.append(thead);
        var tbody = $("<tbody>");
        if (!pageRows.length) {
            tbody.append($("<tr>").append($("<td class='dr-empty'>").attr("colspan", Math.max(columns.length, 1)).text("لا توجد بيانات للعرض.")));
        } else if (state.layout.groupBy && state.layout.groupBy.length) {
            renderGroupedRows(tbody, pageRows, columns, state.layout.groupBy, 0);
        } else {
            pageRows.forEach(function (row) { tbody.append(dataRow(row, columns)); });
        }
        table.append(tbody);
        renderTotals(columns, rows);
        $("#drRowCount").text("الصفوف: " + rows.length + " | الصفحة " + state.page + " من " + totalPages);
        $("#drMaxRowsWarning").text(state.resultMeta && state.resultMeta.MaxRows && state.resultMeta.RowCount >= state.resultMeta.MaxRows ? "تم الوصول إلى الحد الأقصى للصفوف." : "");
    }

    function sortMark(field) {
        var sort = state.layout.sort && state.layout.sort[0];
        if (!sort || sort.field !== field) return $("<span class='dr-sort-mark'>");
        return $("<span class='dr-sort-mark'>").text(sort.dir === "desc" ? " ↓" : " ↑");
    }

    function renderGroupedRows(tbody, rows, columns, groups, level) {
        var field = groups[level];
        var buckets = {};
        rows.forEach(function (row) {
            var key = formatValue(row[field], findColumn(field)) || "(فارغ)";
            if (!buckets[key]) buckets[key] = [];
            buckets[key].push(row);
        });
        Object.keys(buckets).forEach(function (key) {
            tbody.append($("<tr class='dr-group-row'>")
                .append($("<td>").attr("colspan", Math.max(columns.length, 1)).text(Array(level + 1).join("› ") + label(findColumn(field)) + ": " + key + " (" + buckets[key].length + ")")));
            if (level + 1 < groups.length) renderGroupedRows(tbody, buckets[key], columns, groups, level + 1);
            else buckets[key].forEach(function (row) { tbody.append(dataRow(row, columns)); });
        });
    }

    function dataRow(row, columns) {
        var tr = $("<tr>");
        columns.forEach(function (c) {
            tr.append($("<td>")
                .attr("data-field", c.FieldName)
                .addClass(alignmentClass(c.FieldName))
                .addClass(conditionalClass(row, c))
                .css("max-width", (state.layout.widths[c.FieldName] || 140) + "px")
                .text(formatValue(row[c.FieldName], c)));
        });
        return tr;
    }

    function alignmentClass(field) {
        return "dr-align-" + (state.layout.alignment[field] || "right");
    }

    function conditionalClass(row, column) {
        var cls = "";
        (state.layout.conditionalFormatting || []).forEach(function (rule) {
            if (rule.field === column.FieldName && filterMatch(row, rule)) cls = "dr-cond-" + (rule.style || "highlight");
        });
        return cls;
    }

    function renderTotals(columns, rows) {
        var totals = $("#drTotals").empty();
        columns.forEach(function (c) {
            var type = state.layout.summaries[c.FieldName];
            if (!type) return;
            totals.append($("<span class='dr-token'>").text(label(c) + ": " + calculateSummary(rows, c.FieldName, type)));
        });
        if (!totals.children().length) totals.append($("<span class='dr-muted'>").text("لم يتم اختيار مجاميع."));
    }

    function calculateSummary(rows, field, type) {
        var values = rows.map(function (r) { return parseFloat(r[field]); }).filter(function (v) { return !isNaN(v); });
        if (type === "count") return rows.length;
        if (!values.length) return "0";
        if (type === "avg") return formatNumber(values.reduce(sum, 0) / values.length, 2);
        if (type === "min") return formatNumber(Math.min.apply(Math, values), 2);
        if (type === "max") return formatNumber(Math.max.apply(Math, values), 2);
        return formatNumber(values.reduce(sum, 0), 2);
    }

    function sum(a, b) { return a + b; }

    function formatValue(value, column) {
        if (value === null || value === undefined) return "";
        var date = parseMvcDate(value);
        var format = column ? ((state.layout.formatting[column.FieldName] || {}).format || "") : "";
        if (date) {
            if (format === "yyyy-MM-dd") return date.getFullYear() + "-" + pad(date.getMonth() + 1) + "-" + pad(date.getDate());
            if (format === "datetime") return date.toLocaleString();
            return pad(date.getDate()) + "/" + pad(date.getMonth() + 1) + "/" + date.getFullYear();
        }
        if (column && isNumeric(column)) {
            var decimals = parseInt((state.layout.formatting[column.FieldName] || {}).decimals, 10);
            var number = parseFloat(value);
            if (!isNaN(number)) {
                if (format === "percent") return formatNumber(number * 100, isNaN(decimals) ? 2 : decimals) + "%";
                return formatNumber(number, isNaN(decimals) ? 2 : decimals);
            }
        }
        var text = String(value);
        if (format === "trim30" && text.length > 30) return text.substring(0, 30) + "...";
        if (format === "trim60" && text.length > 60) return text.substring(0, 60) + "...";
        return text;
    }

    function parseMvcDate(value) {
        if (typeof value === "string" && value.indexOf("/Date(") === 0) {
            var ticks = parseInt(value.replace("/Date(", "").replace(")/", ""), 10);
            if (!isNaN(ticks)) return new Date(ticks);
        }
        return null;
    }

    function pad(value) {
        return value < 10 ? "0" + value : String(value);
    }

    function formatNumber(value, decimals) {
        return Number(value || 0).toLocaleString("en-US", { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
    }

    function toggleSort(field) {
        var current = state.layout.sort && state.layout.sort[0];
        if (!current || current.field !== field) state.layout.sort = [{ field: field, dir: "asc" }];
        else if (current.dir === "asc") state.layout.sort = [{ field: field, dir: "desc" }];
        else state.layout.sort = [];
        renderGrid();
    }

    function selectField(field) {
        state.selectedField = field;
        bindFieldChooser();
        bindColumnProperties();
    }

    function moveColumn(field, delta) {
        var order = state.layout.columnOrder;
        var index = order.indexOf(field);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= order.length) return;
        order.splice(index, 1);
        order.splice(next, 0, field);
        bindFieldChooser();
        renderGrid();
    }

    function setColumnVisible(field, visible) {
        state.layout.visibleColumns[field] = visible;
        if (visible && state.layout.columnOrder.indexOf(field) < 0) state.layout.columnOrder.push(field);
        bindFieldChooser();
        renderGrid();
    }

    function addGroup(field) {
        var c = findColumn(field);
        if (!c || state.layout.groupBy.indexOf(field) >= 0) return;
        state.layout.groupBy.push(field);
        state.layout.visibleColumns[field] = true;
        bindGroupArea();
        renderGrid();
    }

    function resetLayout() {
        state.layout = createEmptyLayout();
        ensureLayoutDefaults();
        state.selectedField = null;
        state.page = 1;
        bindDesigner();
        renderGrid();
        msg("تمت إعادة التصميم إلى الإعدادات الأساسية.");
    }

    function currentLayoutJson() {
        collectFilters();
        collectConditionalRules();
        state.layout.designVersion = defaultLayoutVersion;
        state.layout.areaScope = state.scope;
        state.layout.reportId = state.definition ? state.definition.ReportId : 0;
        state.layout.pageSize = parseInt($("#drPageSize").val(), 10) || 50;
        state.layout.quickFilter = $("#drQuickFilter").val() || "";
        return JSON.stringify(state.layout);
    }

    function saveLayout(forceDefault, nameOverride) {
        if (!state.definition) {
            msg("اختر تقريرًا قبل حفظ التصميم.", true);
            return;
        }
        var name = nameOverride || $("#drLayoutName").val() || "Default";
        $.ajax({
            url: api("SaveLayout") + "&reportId=" + state.definition.ReportId + "&layoutName=" + encodeURIComponent(name) + "&isDefault=" + !!forceDefault,
            method: "POST",
            data: currentLayoutJson(),
            contentType: "application/json; charset=utf-8"
        }).done(function (r) {
            if (r && r.layoutId) state.currentLayoutId = r.layoutId;
            msg(forceDefault ? "تم حفظ التصميم وجعله افتراضيًا." : "تم حفظ التصميم.");
            loadLayouts();
        }).fail(function () { msg("تعذر حفظ التصميم", true); });
    }

    function loadLayouts() {
        if (!state.definition) return;
        $.getJSON(api("Layouts") + "&reportId=" + state.definition.ReportId).done(function (r) {
            var select = $("#drLayouts").empty();
            state.currentLayoutId = null;
            select.append($("<option>").val("").text("اختر تصميمًا محفوظًا"));
            (r.data || []).forEach(function (item) {
                select.append($("<option>").val(item.LayoutId).attr("data-json", item.LayoutJson).text((item.IsDefault ? "★ " : "") + item.LayoutName));
                if (item.IsDefault) {
                    $("#drLayouts").val(item.LayoutId);
                    state.currentLayoutId = item.LayoutId;
                    applyLayoutJson(item.LayoutJson, true);
                }
            });
        });
    }

    function applySelectedLayout() {
        var option = $("#drLayouts option:selected");
        var raw = option.attr("data-json");
        if (!raw) return;
        state.currentLayoutId = parseInt(option.val(), 10) || null;
        applyLayoutJson(raw, false);
    }

    function applyLayoutJson(raw, quiet) {
        try {
            var layout = JSON.parse(raw);
            state.layout = $.extend(true, createEmptyLayout(), layout);
            ensureLayoutDefaults();
            $("#drLayoutName").val($("#drLayouts option:selected").text().replace(/^★\s*/, "") || "Default");
            $("#drQuickFilter").val(state.layout.quickFilter || "");
            $("#drPageSize").val(String(state.layout.pageSize || 50));
            bindDesigner();
            renderGrid();
            if (!quiet) msg("تم تحميل التصميم.");
        } catch (e) {
            msg("تعذر قراءة التصميم المحفوظ.", true);
        }
    }

    function deleteSelectedLayout() {
        var id = $("#drLayouts").val();
        if (!id) {
            msg("اختر تصميمًا لحذفه.", true);
            return;
        }
        if (!confirm("هل تريد حذف هذا التصميم؟")) return;
        $.ajax({ url: api("DeleteLayout") + "&layoutId=" + encodeURIComponent(id), method: "POST" })
            .done(function () { msg("تم حذف التصميم."); loadLayouts(); })
            .fail(function () { msg("تعذر حذف التصميم.", true); });
    }

    function exportCsv() {
        var columns = orderedColumns(false);
        var rows = filteredRows();
        var lines = [];
        lines.push(columns.map(function (c) { return csvCell(label(c)); }).join(","));
        rows.forEach(function (row) {
            lines.push(columns.map(function (c) { return csvCell(formatValue(row[c.FieldName], c)); }).join(","));
        });
        var blob = new Blob(["\ufeff" + lines.join("\r\n")], { type: "text/csv;charset=utf-8;" });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = ((state.definition && (state.definition.ReportCode || state.definition.ReportNameEn)) || "DynamicReport") + ".csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    function printPreview() {
        if (!state.definition) {
            msg("اختر تقريرًا أولًا.", true);
            return;
        }
        var collected = collectParameterValues();
        if (collected.missing) {
            msg("أدخل المعايير المطلوبة قبل معاينة الطباعة.", true);
            return;
        }
        var form = document.createElement("form");
        form.method = "POST";
        form.target = "_blank";
        form.action = state.apiBase.replace(/\/$/, "") + "/Print";
        addHidden(form, "reportId", state.definition.ReportId);
        addHidden(form, "layoutId", state.currentLayoutId || "");
        addHidden(form, "scope", state.scope);
        Object.keys(collected.parameters).forEach(function (key) {
            addHidden(form, key, collected.parameters[key]);
        });
        document.body.appendChild(form);
        form.submit();
        document.body.removeChild(form);
    }

    function addHidden(form, name, value) {
        var input = document.createElement("input");
        input.type = "hidden";
        input.name = name;
        input.value = value == null ? "" : value;
        form.appendChild(input);
    }

    function csvCell(value) {
        value = value === null || value === undefined ? "" : String(value);
        return '"' + value.replace(/"/g, '""') + '"';
    }

    function updateDesignMode() {
        $(".dr-designer-shell").toggleClass("design-mode", state.designMode);
        $("#drModeBadge").text(state.designMode ? "وضع التصميم" : "وضع العرض");
        $("#drToggleDesign").text(state.designMode ? "خروج من وضع التصميم" : "تصميم التقرير");
    }

    function applyPropertyChange(prop, value) {
        var field = state.selectedField;
        if (!field) return;
        if (prop === "Caption") state.layout.captions[field] = value || field;
        if (prop === "Width") state.layout.widths[field] = parseInt(value, 10) || 140;
        if (prop === "Alignment") state.layout.alignment[field] = value || "right";
        if (prop === "Visible") state.layout.visibleColumns[field] = !!value;
        if (prop === "Summary") {
            if (value) state.layout.summaries[field] = value;
            else delete state.layout.summaries[field];
        }
        if (prop === "Format" || prop === "Decimals") {
            if (!state.layout.formatting[field]) state.layout.formatting[field] = {};
            if (prop === "Format") state.layout.formatting[field].format = value;
            if (prop === "Decimals") state.layout.formatting[field].decimals = value;
        }
        bindFieldChooser();
        renderGrid();
    }

    function wireDragDrop() {
        var draggedField = null;
        $(document).on("dragstart", "[data-field][draggable=true], .dr-field-item", function (e) {
            draggedField = $(this).attr("data-field");
            e.originalEvent.dataTransfer.setData("text/plain", draggedField);
        });
        $("#drGroupArea").on("dragover", function (e) { e.preventDefault(); $(this).addClass("drag-over dr-drop-zone--active"); });
        $("#drGroupArea").on("dragleave", function () { $(this).removeClass("drag-over dr-drop-zone--active"); });
        $("#drGroupArea").on("drop", function (e) {
            e.preventDefault();
            $(this).removeClass("drag-over dr-drop-zone--active");
            addGroup(e.originalEvent.dataTransfer.getData("text/plain") || draggedField);
        });
        $("#drData").on("dragover", "th", function (e) { e.preventDefault(); });
        $("#drData").on("drop", "th", function (e) {
            e.preventDefault();
            var source = e.originalEvent.dataTransfer.getData("text/plain") || draggedField;
            var target = $(this).attr("data-field");
            if (!source || !target || source === target) return;
            state.layout.visibleColumns[source] = true;
            var order = state.layout.columnOrder;
            order.splice(order.indexOf(source), 1);
            order.splice(order.indexOf(target), 0, source);
            bindFieldChooser();
            renderGrid();
        });
    }

    $(function () {
        loadReports();
        wireDragDrop();
        $("#drReport").on("change", loadDefinition);
        $("#drExecute").on("click", execute);
        $("#drToggleDesign").on("click", function () { state.designMode = !state.designMode; updateDesignMode(); renderGrid(); });
        $("#drQuickFilter").on("input", function () { state.page = 1; renderGrid(); });
        $("#drPageSize").on("change", function () { state.page = 1; renderGrid(); });
        $("#drFieldSearch").on("input", bindFieldChooser);
        $("#drColumnsChooser").on("change", ".dr-field-visible", function () { setColumnVisible($(this).closest("[data-field]").attr("data-field"), $(this).is(":checked")); });
        $("#drColumnsChooser").on("click", ".dr-field-item", function (e) { if (!$(e.target).is("input,button")) selectField($(this).attr("data-field")); });
        $("#drColumnsChooser").on("click", ".dr-move-up", function (e) { e.stopPropagation(); moveColumn($(this).closest("[data-field]").attr("data-field"), -1); });
        $("#drColumnsChooser").on("click", ".dr-move-down", function (e) { e.stopPropagation(); moveColumn($(this).closest("[data-field]").attr("data-field"), 1); });
        $("#drData").on("click", "th[data-field]", function () { selectField($(this).attr("data-field")); });
        $("#drData").on("dblclick", "th[data-field]", function () { toggleSort($(this).attr("data-field")); });
        $("#drColumnProperties").on("input change", "[data-prop]", function () {
            var input = $(this);
            applyPropertyChange(input.attr("data-prop"), input.attr("type") === "checkbox" ? input.is(":checked") : input.val());
        });
        $("#drAddFilter").on("click", function () { $("#drFilterRows").append(filterRow({}, $("#drFilterRows .dr-builder-row").length)); });
        $("#drFilterRows").on("change input", "input,select", renderGrid);
        $("#drFilterRows").on("click", ".dr-remove-filter", function () { $(this).closest(".dr-builder-row").remove(); renderGrid(); });
        $("#drAddConditionalRule").on("click", function () { state.layout.conditionalFormatting.push({ field: orderedColumns(true)[0] && orderedColumns(true)[0].FieldName, op: "gt", value: "", style: "highlight" }); bindConditionalRules(); });
        $("#drConditionalRows").on("change input", "input,select", renderGrid);
        $("#drConditionalRows").on("click", ".dr-remove-rule", function () { $(this).closest(".dr-builder-row").remove(); renderGrid(); });
        $("#drGroupList").on("click", ".dr-token-remove", function () {
            var field = $(this).closest("[data-field]").attr("data-field");
            state.layout.groupBy = state.layout.groupBy.filter(function (x) { return x !== field; });
            bindGroupArea();
            renderGrid();
        });
        $("#drLoadLayout").on("click", applySelectedLayout);
        $("#drLayouts").on("change", applySelectedLayout);
        $("#drSaveLayout").on("click", function () { saveLayout(false); });
        $("#drSaveAsLayout").on("click", function () {
            var name = prompt("اسم التصميم الجديد", $("#drLayoutName").val() || "تصميم جديد");
            if (name) { $("#drLayoutName").val(name); saveLayout(false, name); }
        });
        $("#drSetDefault").on("click", function () { saveLayout(true); });
        $("#drResetLayout").on("click", resetLayout);
        $("#drDeleteLayout").on("click", deleteSelectedLayout);
        $("#drExportCsv").on("click", exportCsv);
        $("#btnPrintPreview").on("click", printPreview);
    });
})();
