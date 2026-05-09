(function () {
    var state = {
        scope: window.DynamicReportsScope || "Web",
        apiBase: window.DynamicReportsApiBase || "/Reports/Admin",
        current: null
    };

    function api(path) {
        // state.apiBase already includes the controller URL (e.g. /Reports/Admin or /Pos/DynamicReportsAdmin).
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
            dr.permissions.load(state.current && state.current.ReportId);
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
            dr.permissions.load(r.reportId);
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

    var dr = window.DynamicReportsAdmin = window.DynamicReportsAdmin || {};
    dr.permissions = (function () {
        var searchTimer = null;

        function panelMsg(text) {
            $("#drPermMessage").text(text || "");
        }

        function currentReportId() {
            return parseInt($("#ReportId").val(), 10) || 0;
        }

        function currentProjectScope() {
            return $("#ProjectScope").val() || state.scope;
        }

        function clear() {
            $("#drPermRows").empty();
            $("#drPermEmpty").show();
            panelMsg("");
        }

        function bind(items) {
            var rows = $("#drPermRows").empty();
            items = items || [];
            $("#drPermEmpty").toggle(items.length === 0);
            items.forEach(function (item) {
                var flags = [];
                if (item.CanView) flags.push("مشاهدة");
                if (item.CanDesign) flags.push("تصميم");
                if (item.CanExport) flags.push("تصدير");

                $("<div class='dr-perm-row'>")
                    .append($("<div class='dr-perm-actor'>").append($("<strong>").text(item.DisplayName || "")).append($("<span>").text(item.ProjectScope || "")))
                    .append($("<div class='dr-perm-flags'>").text(flags.join(" / ")))
                    .append($("<button class='dr-mini-button danger' type='button'>").text("حذف").on("click", function () { remove(item.PermissionId); }))
                    .appendTo(rows);
            });
        }

        function load(reportId) {
            if (!reportId) {
                clear();
                return;
            }

            $.getJSON(api("ListPermissions") + "&reportId=" + encodeURIComponent(reportId))
                .done(function (r) { bind(r.data || []); })
                .fail(function (xhr) { panelMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تحميل الصلاحيات."); });
        }

        function loadRoles() {
            $.getJSON(api("ListRoles")).done(function (r) {
                var select = $("#drPermRoles").empty();
                (r.data || []).forEach(function (role) {
                    $("<option>").val(role.Key).text(role.Value || role.Key).appendTo(select);
                });
            }).fail(function () {
                $("#drPermRoles").empty();
                panelMsg("تعذر تحميل الأدوار.");
            });
        }

        function searchUser(q) {
            $.getJSON(api("ListUsersLite") + "&q=" + encodeURIComponent(q || "")).done(function (r) {
                var select = $("#drPermUsers").empty();
                (r.data || []).forEach(function (user) {
                    $("<option>").val(user.Key).text(user.Value || user.Key).appendTo(select);
                });
            }).fail(function () {
                $("#drPermUsers").empty();
                panelMsg("تعذر تحميل المستخدمين.");
            });
        }

        function collectInput() {
            var actorType = $("#drPermActorType").val();
            var input = {
                ReportId: currentReportId(),
                ProjectScope: currentProjectScope(),
                CanView: $("#drPermCanView").is(":checked"),
                CanDesign: $("#drPermCanDesign").is(":checked"),
                CanExport: $("#drPermCanExport").is(":checked")
            };

            if (actorType === "role") {
                input.RoleId = parseInt($("#drPermRoles").val(), 10) || null;
                input.UserId = null;
            } else {
                input.UserId = parseInt($("#drPermUsers").val(), 10) || null;
                input.RoleId = null;
            }

            return input;
        }

        function add(input) {
            if (!input.ReportId) {
                panelMsg("اختر تقريرًا أولًا.");
                return;
            }

            $.ajax({
                url: api("SavePermission"),
                method: "POST",
                data: JSON.stringify(input),
                contentType: "application/json; charset=utf-8"
            }).done(function () {
                panelMsg("تم حفظ الصلاحية.");
                load(input.ReportId);
            }).fail(function (xhr) {
                panelMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر حفظ الصلاحية.");
            });
        }

        function remove(permissionId) {
            $.post(api("DeletePermission") + "&permissionId=" + encodeURIComponent(permissionId))
                .done(function () {
                    panelMsg("تم حذف الصلاحية.");
                    load(currentReportId());
                })
                .fail(function (xhr) {
                    panelMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر حذف الصلاحية.");
                });
        }

        function refreshActorMode() {
            var isRole = $("#drPermActorType").val() === "role";
            $("#drPermRoleBox").toggle(isRole);
            $("#drPermUserBox").toggle(!isRole);
        }

        function wire() {
            refreshActorMode();
            loadRoles();
            searchUser("");
            $("#drPermActorType").on("change", refreshActorMode);
            $("#drSavePermission").on("click", function () { add(collectInput()); });
            $("#drPermUserSearch").on("input", function () {
                var q = $(this).val();
                clearTimeout(searchTimer);
                searchTimer = setTimeout(function () { searchUser(q); }, 250);
            });
        }

        return {
            load: load,
            bind: bind,
            add: add,
            delete: remove,
            searchUser: searchUser,
            loadRoles: loadRoles,
            clear: clear,
            wire: wire
        };
    })();

    dr.catalog = (function () {
        function catalogMsg(text) {
            $("#drCatalogMessage").text(text || "");
        }

        function statusClass(status) {
            return "dr-catalog-status-" + (status || "Pending");
        }

        function list(scope, status) {
            $.getJSON(api("CatalogList") + "&status=" + encodeURIComponent(status || "")).done(function (r) {
                var rows = $("#drCatalogRows").empty();
                var items = r.data || [];
                if (!items.length) {
                    $("<div class='dr-perm-empty'>").text("لا توجد مصادر في الكتالوج لهذا الفلتر.").appendTo(rows);
                    return;
                }

                items.forEach(function (item) {
                    var title = item.SourceSchema + "." + item.SourceName;
                    var row = $("<div class='dr-catalog-row'>").addClass(statusClass(item.ClassificationStatus));
                    row.append($("<div>").append($("<strong>").text(title)).append($("<span>").text(item.SourceType)));
                    row.append($("<div class='dr-catalog-score'>").text(item.ClassificationScore));
                    row.append($("<div class='dr-catalog-flags'>").text(item.RiskFlags || ""));
                    row.append($("<div>").text(item.ClassificationStatus));
                    var actions = $("<div class='dr-actions'>");
                    $("<button class='dr-mini-button' type='button'>").text("تفاصيل").on("click", function () { detail(item.CatalogId, scope); }).appendTo(actions);
                    $("<button class='dr-mini-button' type='button'>").text("اعتماد").on("click", function () {
                        var name = window.prompt("اسم التقرير المقترح", item.SuggestedReportName || item.SourceName);
                        if (name !== null) approve(item.CatalogId, scope, name);
                    }).appendTo(actions);
                    $("<button class='dr-mini-button danger' type='button'>").text("رفض").on("click", function () {
                        var reason = window.prompt("سبب الرفض", item.RejectionReason || "");
                        if (reason !== null) reject(item.CatalogId, scope, reason);
                    }).appendTo(actions);
                    $("<button class='dr-mini-button secondary' type='button'>").text("استيراد").on("click", function () { importOne(item.CatalogId, scope); }).appendTo(actions);
                    row.append(actions);
                    row.appendTo(rows);
                });
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تحميل الكتالوج.");
            });
        }

        function discover(scope) {
            catalogMsg("جاري الاكتشاف دون تنفيذ أي مصدر...");
            $.post(api("CatalogDiscover")).done(function (r) {
                var d = r.data || {};
                catalogMsg("تم الاكتشاف. جديد: " + (d.DiscoveredCount || 0) + "، محدّث: " + (d.UpdatedCount || 0) + "، أخطاء: " + (d.ErrorCount || 0));
                list(scope, $("#drCatalogStatus").val());
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تنفيذ الاكتشاف.");
            });
        }

        function detail(catalogId, scope) {
            $.getJSON(api("CatalogDetail") + "&catalogId=" + encodeURIComponent(catalogId)).done(function (r) {
                var d = r.data || {};
                var entry = d.Entry || {};
                var pane = $("#drCatalogDetail").empty();
                $("<h3>").text(entry.SourceSchema + "." + entry.SourceName).appendTo(pane);
                $("<p>").text("الحالة: " + entry.ClassificationStatus + " | الدرجة: " + entry.ClassificationScore + " | المخاطر: " + (entry.RiskFlags || "")).appendTo(pane);
                $("<h4>").text("الأعمدة").appendTo(pane);
                $("<pre class='dr-body-excerpt'>").text((d.Columns || []).map(function (c) { return c.FieldName + " : " + c.DataType; }).join("\n") || "لا توجد أعمدة مكتشفة.").appendTo(pane);
                $("<h4>").text("المعاملات").appendTo(pane);
                $("<pre class='dr-body-excerpt'>").text((d.Parameters || []).map(function (p) { return p.ParameterName + " : " + p.DataType; }).join("\n") || "لا توجد معاملات.").appendTo(pane);
                $("<h4>").text("نص للقراءة فقط، لا يتم تنفيذه").appendTo(pane);
                $("<pre class='dr-body-excerpt'>").text(d.BodyExcerpt || "").appendTo(pane);
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر تحميل التفاصيل.");
            });
        }

        function approve(catalogId, scope, suggestedName) {
            $.post(api("CatalogApprove") + "&catalogId=" + encodeURIComponent(catalogId), { suggestedName: suggestedName || "" }).done(function () {
                catalogMsg("تم اعتماد المصدر يدويًا.");
                list(scope, $("#drCatalogStatus").val());
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر اعتماد المصدر.");
            });
        }

        function reject(catalogId, scope, reason) {
            $.post(api("CatalogReject") + "&catalogId=" + encodeURIComponent(catalogId), { reason: reason || "" }).done(function () {
                catalogMsg("تم رفض المصدر.");
                list(scope, $("#drCatalogStatus").val());
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر رفض المصدر.");
            });
        }

        function importOne(catalogId, scope) {
            $.post(api("CatalogImport") + "&catalogId=" + encodeURIComponent(catalogId)).done(function (r) {
                var d = r.data || {};
                catalogMsg(d.Message || ("تم الاستيراد: " + (d.ReportCode || "")));
                list(scope, $("#drCatalogStatus").val());
                loadList();
            }).fail(function (xhr) {
                catalogMsg((xhr.responseJSON && xhr.responseJSON.message) || "تعذر استيراد التقرير.");
            });
        }

        function wire() {
            $("#drCatalogDiscover").on("click", function () { discover(state.scope); });
            $("#drCatalogRefresh").on("click", function () { list(state.scope, $("#drCatalogStatus").val()); });
            $("#drCatalogStatus").on("change", function () { list(state.scope, $(this).val()); });
            list(state.scope, "");
        }

        return {
            discover: discover,
            list: list,
            detail: detail,
            approve: approve,
            reject: reject,
            import: importOne,
            wire: wire
        };
    })();

    $(function () {
        state.current = emptyDefinition();
        bindForm();
        dr.permissions.wire();
        dr.permissions.clear();
        dr.catalog.wire();
        loadList();
        $("#drNew").on("click", function () { state.current = emptyDefinition(); bindForm(); dr.permissions.clear(); msg(""); });
        $("#drSave").on("click", save);
        $("#drLoadMetadata").on("click", loadMetadata);
        $("#drAddParameter").on("click", function () { addParameterRow({}, $("#drParameters tbody tr").length); });
    });
})();
