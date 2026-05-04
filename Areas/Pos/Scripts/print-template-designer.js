(function () {
    "use strict";

    var pageEl = document.getElementById("ptdPage");
    if (!pageEl) { return; }

    var GET_URL = pageEl.getAttribute("data-get-url");
    var SAVE_URL = pageEl.getAttribute("data-save-url");
    var UPLOAD_URL = pageEl.getAttribute("data-upload-url");
    var BACKGROUND_URL = pageEl.getAttribute("data-background-url");
    var PREVIEW_URL = pageEl.getAttribute("data-preview-url");

    // Field palette: list of fields the report knows about. The
    // FieldKey here must match the keys produced by KycCardReport's
    // BuildValues dictionary.
    var FIELD_DEFS = [
        { key: "Token", label: "رقم Token (فوق)", isCellBased: true, defaultCells: 12 },
        { key: "ArabicName", label: "الاسم بالعربي" },
        { key: "EnglishName", label: "الاسم بالإنجليزي", direction: "LTR" },
        { key: "Address", label: "العنوان", direction: "LTR" },
        { key: "Nationality", label: "الجنسية" },
        { key: "BirthDate", label: "تاريخ الميلاد", direction: "LTR" },
        { key: "NationalId", label: "الرقم القومي", isCellBased: true, defaultCells: 14 },
        { key: "IssueDate", label: "تاريخ الإصدار", direction: "LTR" },
        { key: "Source", label: "جهة الإصدار" },
        { key: "ExpiryDate", label: "تاريخ الانتهاء", direction: "LTR" },
        { key: "Phone", label: "رقم المحمول", isCellBased: true, defaultCells: 11 },
        { key: "TokenNo", label: "Token NO. (تحت)", isCellBased: true, defaultCells: 12 },
        { key: "SignatureName", label: "اسم العميل (توقيع)" },
        { key: "FooterDate", label: "التاريخ" }
    ];

    var state = {
        templateName: pageEl.getAttribute("data-template-name") || "KycCard",
        template: null,
        selectedFieldKey: null,
        scale: 0.85
    };

    // ---------- DOM refs ----------
    var canvasEl = document.getElementById("canvas");
    var canvasBgEl = document.getElementById("canvasBackground");
    var fieldsLayerEl = document.getElementById("fieldsLayer");
    var canvasWrapEl = document.getElementById("canvasWrap");
    var paletteEl = document.getElementById("fieldPalette");
    var propsEl = document.getElementById("propertiesPanel");
    var coordsEl = document.getElementById("cursorCoords");
    var saveStatusEl = document.getElementById("saveStatus");
    var bgStatusEl = document.getElementById("backgroundStatus");

    // ---------- Coordinate conversion ----------
    function unitsToScreen(value) { return value * state.scale; }
    function screenToUnits(value) { return value / state.scale; }

    // ---------- Network helpers ----------
    function ajax(method, url, body, isForm, onLoad) {
        var xhr = new XMLHttpRequest();
        xhr.open(method, url, true);
        if (!isForm) {
            xhr.setRequestHeader("Content-Type", "application/json; charset=utf-8");
        }
        xhr.onload = function () {
            var data = null;
            try { data = JSON.parse(xhr.responseText); } catch (e) { data = null; }
            onLoad(xhr.status, data);
        };
        xhr.send(isForm ? body : (body ? JSON.stringify(body) : null));
    }

    function setStatus(message, kind) {
        saveStatusEl.textContent = message || "";
        saveStatusEl.className = "";
        if (kind) { saveStatusEl.className = "is-" + kind; }
    }

    // ---------- Canvas / scale ----------
    function applyCanvasSize() {
        var t = state.template;
        var widthPx = unitsToScreen(t.PageWidth);
        var heightPx = unitsToScreen(t.PageHeight);
        canvasEl.style.width = widthPx + "px";
        canvasEl.style.height = heightPx + "px";
        canvasBgEl.style.width = widthPx + "px";
        canvasBgEl.style.height = heightPx + "px";
    }

    function applyBackgroundSrc() {
        var t = state.template;
        if (t.BackgroundFileName) {
            canvasBgEl.src = BACKGROUND_URL + "?fileName=" +
                encodeURIComponent(t.BackgroundFileName) + "&v=" + Date.now();
            canvasBgEl.classList.remove("is-hidden");
            bgStatusEl.textContent = "الخلفية: " + t.BackgroundFileName;
        } else {
            canvasBgEl.removeAttribute("src");
            canvasBgEl.classList.add("is-hidden");
            bgStatusEl.textContent = "لا توجد صورة خلفية محملة.";
        }
        var showBg = document.getElementById("showBackgroundDesigner").checked;
        canvasBgEl.classList.toggle("is-hidden", !showBg);
    }

    // ---------- Palette ----------
    function renderPalette() {
        paletteEl.innerHTML = "";
        var keysOnCanvas = {};
        (state.template.Fields || []).forEach(function (f) {
            keysOnCanvas[f.FieldKey] = true;
        });

        FIELD_DEFS.forEach(function (def) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.textContent = def.label;
            btn.setAttribute("data-key", def.key);
            if (keysOnCanvas[def.key]) { btn.classList.add("is-on-canvas"); }
            btn.addEventListener("click", function () {
                if (keysOnCanvas[def.key]) {
                    state.selectedFieldKey = def.key;
                    renderFields();
                    renderPropertiesPanel();
                } else {
                    addFieldFromDef(def);
                }
            });
            paletteEl.appendChild(btn);
        });
    }

    function addFieldFromDef(def) {
        var t = state.template;
        var newField = {
            FieldKey: def.key,
            Label: def.label,
            X: t.PageWidth * 0.35,
            Y: t.PageHeight * 0.5,
            Width: def.isCellBased ? (def.defaultCells * 50) : 200,
            Height: 24,
            FontName: "Tahoma",
            FontSize: 10,
            Bold: true,
            Alignment: "Center",
            Direction: def.direction || "RTL",
            IsCellBased: !!def.isCellBased,
            CellCount: def.isCellBased ? def.defaultCells : 0,
            CellWidth: def.isCellBased ? 50 : 0,
            CharacterSpacing: 0
        };
        t.Fields.push(newField);
        state.selectedFieldKey = newField.FieldKey;
        renderPalette();
        renderFields();
        renderPropertiesPanel();
    }

    // ---------- Fields rendering ----------
    function renderFields() {
        fieldsLayerEl.innerHTML = "";
        (state.template.Fields || []).forEach(function (field) {
            var node = createFieldNode(field);
            fieldsLayerEl.appendChild(node);
        });
    }

    function createFieldNode(field) {
        var node = document.createElement("div");
        node.className = "ptd-field";
        if (state.selectedFieldKey === field.FieldKey) { node.classList.add("is-selected"); }
        node.setAttribute("data-key", field.FieldKey);

        node.style.left = unitsToScreen(field.X) + "px";
        node.style.top = unitsToScreen(field.Y) + "px";
        node.style.width = unitsToScreen(field.Width) + "px";
        node.style.height = unitsToScreen(field.Height) + "px";

        var label = document.createElement("span");
        label.className = "ptd-field-label";
        label.textContent = field.Label || field.FieldKey;
        node.appendChild(label);

        if (field.IsCellBased && field.CellCount > 1 && field.CellWidth > 0) {
            var cellsBox = document.createElement("div");
            cellsBox.className = "ptd-field-cells";
            for (var i = 1; i < field.CellCount; i++) {
                var divider = document.createElement("div");
                divider.className = "ptd-cell";
                divider.style.left = unitsToScreen(i * field.CellWidth) + "px";
                cellsBox.appendChild(divider);
            }
            node.appendChild(cellsBox);
        }

        var handle = document.createElement("div");
        handle.className = "ptd-field-handle";
        node.appendChild(handle);

        attachFieldDrag(node, handle, field);
        return node;
    }

    // ---------- Drag + resize ----------
    function attachFieldDrag(node, handle, field) {
        var dragState = null;

        node.addEventListener("mousedown", function (e) {
            if (e.target === handle) { return; }
            e.preventDefault();
            state.selectedFieldKey = field.FieldKey;
            renderFields();
            renderPropertiesPanel();

            var startMouse = { x: e.clientX, y: e.clientY };
            var startField = { x: field.X, y: field.Y };
            dragState = { type: "move", startMouse: startMouse, startField: startField };

            window.addEventListener("mousemove", onMove);
            window.addEventListener("mouseup", onUp);
        });

        handle.addEventListener("mousedown", function (e) {
            e.preventDefault();
            e.stopPropagation();
            state.selectedFieldKey = field.FieldKey;
            renderFields();
            renderPropertiesPanel();

            var startMouse = { x: e.clientX, y: e.clientY };
            var startField = { w: field.Width, h: field.Height };
            dragState = { type: "resize", startMouse: startMouse, startField: startField };

            window.addEventListener("mousemove", onMove);
            window.addEventListener("mouseup", onUp);
        });

        function onMove(e) {
            if (!dragState) { return; }
            var dx = e.clientX - dragState.startMouse.x;
            var dy = e.clientY - dragState.startMouse.y;
            // RTL canvas: dragging right (dx>0) on screen moves the
            // element toward the LEFT edge in template coords. Since the
            // canvas is a normal LTR HTML element underneath, treat
            // screen X as direct horizontal motion - inset-inline-start
            // already accounts for RTL on the wrapper.
            var dxUnits = screenToUnits(dx);
            var dyUnits = screenToUnits(dy);

            if (dragState.type === "move") {
                field.X = Math.max(0, dragState.startField.x + dxUnits);
                field.Y = Math.max(0, dragState.startField.y + dyUnits);
            } else {
                field.Width = Math.max(20, dragState.startField.w + dxUnits);
                field.Height = Math.max(12, dragState.startField.h + dyUnits);
                if (field.IsCellBased && field.CellCount > 0) {
                    field.CellWidth = Math.max(8, field.Width / field.CellCount);
                }
            }
            updateFieldNode(node, field);
            renderPropertiesPanel();
        }

        function onUp() {
            window.removeEventListener("mousemove", onMove);
            window.removeEventListener("mouseup", onUp);
            dragState = null;
        }
    }

    function updateFieldNode(node, field) {
        node.style.left = unitsToScreen(field.X) + "px";
        node.style.top = unitsToScreen(field.Y) + "px";
        node.style.width = unitsToScreen(field.Width) + "px";
        node.style.height = unitsToScreen(field.Height) + "px";
        // Recompute cell dividers if field is cell-based.
        var oldCells = node.querySelector(".ptd-field-cells");
        if (oldCells) { node.removeChild(oldCells); }
        if (field.IsCellBased && field.CellCount > 1 && field.CellWidth > 0) {
            var cellsBox = document.createElement("div");
            cellsBox.className = "ptd-field-cells";
            for (var i = 1; i < field.CellCount; i++) {
                var divider = document.createElement("div");
                divider.className = "ptd-cell";
                divider.style.left = unitsToScreen(i * field.CellWidth) + "px";
                cellsBox.appendChild(divider);
            }
            node.appendChild(cellsBox);
        }
    }

    // ---------- Properties panel ----------
    function renderPropertiesPanel() {
        if (!state.selectedFieldKey) {
            propsEl.innerHTML = '<p class="ptd-hint">اختر حقلاً من اللوحة لعرض خصائصه.</p>';
            return;
        }
        var field = findField(state.selectedFieldKey);
        if (!field) {
            propsEl.innerHTML = '<p class="ptd-hint">الحقل غير موجود.</p>';
            return;
        }

        propsEl.innerHTML = "";

        addPropRow(propsEl, "العنوان", inputText("Label", field.Label, field));
        propsEl.appendChild(grid([
            propRow("X", inputNumber("X", field.X, field, 1)),
            propRow("Y", inputNumber("Y", field.Y, field, 1))
        ]));
        propsEl.appendChild(grid([
            propRow("العرض", inputNumber("Width", field.Width, field, 1)),
            propRow("الارتفاع", inputNumber("Height", field.Height, field, 1))
        ]));

        var hr1 = document.createElement("hr");
        propsEl.appendChild(hr1);

        propsEl.appendChild(grid([
            propRow("الخط", inputText("FontName", field.FontName, field)),
            propRow("حجم الخط", inputNumber("FontSize", field.FontSize, field, 0.5))
        ]));

        propsEl.appendChild(checkboxRow("Bold", "خط عريض", field.Bold, field));

        propsEl.appendChild(grid([
            propRow("المحاذاة", select("Alignment", field.Alignment,
                ["Left", "Center", "Right"], field)),
            propRow("الاتجاه", select("Direction", field.Direction,
                ["LTR", "RTL"], field))
        ]));

        var hr2 = document.createElement("hr");
        propsEl.appendChild(hr2);

        propsEl.appendChild(checkboxRow("IsCellBased", "حقل خانات (Cell-based)",
            field.IsCellBased, field, function () { syncCellWidth(field); renderFields(); }));

        if (field.IsCellBased) {
            propsEl.appendChild(grid([
                propRow("عدد الخانات", inputNumber("CellCount", field.CellCount, field, 1,
                    function () { syncCellWidth(field); renderFields(); })),
                propRow("عرض الخانة", inputNumber("CellWidth", field.CellWidth, field, 1,
                    function () { syncFromCellWidth(field); renderFields(); }))
            ]));
            propsEl.appendChild(propRow("تباعد الحرف",
                inputNumber("CharacterSpacing", field.CharacterSpacing, field, 0.5,
                    function () { renderFields(); })));
        }

        var hr3 = document.createElement("hr");
        propsEl.appendChild(hr3);

        var deleteBtn = document.createElement("button");
        deleteBtn.type = "button";
        deleteBtn.className = "ptd-btn ptd-btn-warn";
        deleteBtn.textContent = "حذف الحقل من القالب";
        deleteBtn.addEventListener("click", function () {
            state.template.Fields = state.template.Fields.filter(function (f) {
                return f.FieldKey !== field.FieldKey;
            });
            state.selectedFieldKey = null;
            renderPalette();
            renderFields();
            renderPropertiesPanel();
        });
        propsEl.appendChild(deleteBtn);
    }

    function syncCellWidth(field) {
        if (field.IsCellBased && field.CellCount > 0) {
            field.CellWidth = field.Width / field.CellCount;
        } else if (!field.IsCellBased) {
            field.CellCount = 0;
            field.CellWidth = 0;
        }
    }

    function syncFromCellWidth(field) {
        if (field.IsCellBased && field.CellCount > 0) {
            field.Width = field.CellWidth * field.CellCount;
        }
    }

    function addPropRow(parent, label, control) {
        parent.appendChild(propRow(label, control));
    }

    function propRow(label, control) {
        var row = document.createElement("div");
        row.className = "ptd-prop-row";
        var lab = document.createElement("label");
        lab.textContent = label;
        row.appendChild(lab);
        row.appendChild(control);
        return row;
    }

    function grid(rows) {
        var box = document.createElement("div");
        box.className = "ptd-prop-grid";
        rows.forEach(function (r) { box.appendChild(r); });
        return box;
    }

    function checkboxRow(propKey, label, value, field, onChange) {
        var row = document.createElement("div");
        row.className = "ptd-prop-row is-checkbox";
        var input = document.createElement("input");
        input.type = "checkbox";
        input.checked = !!value;
        input.addEventListener("change", function () {
            field[propKey] = input.checked;
            if (onChange) { onChange(); }
        });
        var lab = document.createElement("label");
        lab.textContent = label;
        row.appendChild(input);
        row.appendChild(lab);
        return row;
    }

    function inputText(propKey, value, field) {
        var input = document.createElement("input");
        input.type = "text";
        input.value = value || "";
        input.addEventListener("input", function () {
            field[propKey] = input.value;
            if (propKey === "Label") { renderFields(); }
        });
        return input;
    }

    function inputNumber(propKey, value, field, step, onChange) {
        var input = document.createElement("input");
        input.type = "number";
        input.step = step || 1;
        input.value = (value === undefined || value === null) ? 0 : value;
        input.addEventListener("input", function () {
            var n = parseFloat(input.value);
            field[propKey] = isNaN(n) ? 0 : n;
            if (propKey === "X" || propKey === "Y" ||
                propKey === "Width" || propKey === "Height") {
                if (propKey === "Width" || propKey === "Height") {
                    if (field.IsCellBased && field.CellCount > 0 && propKey === "Width") {
                        field.CellWidth = field.Width / field.CellCount;
                    }
                }
                renderFields();
            }
            if (onChange) { onChange(); }
        });
        return input;
    }

    function select(propKey, value, options, field) {
        var sel = document.createElement("select");
        options.forEach(function (opt) {
            var o = document.createElement("option");
            o.value = opt;
            o.textContent = opt;
            if (opt === value) { o.selected = true; }
            sel.appendChild(o);
        });
        sel.addEventListener("change", function () {
            field[propKey] = sel.value;
            renderFields();
        });
        return sel;
    }

    function findField(key) {
        return (state.template.Fields || []).filter(function (f) {
            return f.FieldKey === key;
        })[0];
    }

    // ---------- Cursor coordinates ----------
    function bindCursor() {
        canvasEl.addEventListener("mousemove", function (e) {
            var rect = canvasEl.getBoundingClientRect();
            // Inverse the RTL: in HTML the canvas is laid out LTR
            // so use clientX directly relative to the box.
            var px = e.clientX - rect.left;
            var py = e.clientY - rect.top;
            coordsEl.textContent = "X: " + Math.round(screenToUnits(px)) +
                "   Y: " + Math.round(screenToUnits(py));
        });
    }

    // ---------- Page settings panel ----------
    function bindPageSettings() {
        var pairs = [
            ["pageWidth", "PageWidth", true],
            ["pageHeight", "PageHeight", true],
            ["imageWidth", "ImageWidth", false],
            ["imageHeight", "ImageHeight", false],
            ["globalXShift", "GlobalXShift", false],
            ["globalYShift", "GlobalYShift", false]
        ];
        pairs.forEach(function (p) {
            var el = document.getElementById(p[0]);
            el.addEventListener("input", function () {
                var n = parseFloat(el.value);
                state.template[p[1]] = isNaN(n) ? 0 : n;
                if (p[2]) { applyCanvasSize(); renderFields(); }
            });
        });

        document.getElementById("printBackground").addEventListener("change", function (e) {
            state.template.PrintBackground = e.target.checked;
        });
        document.getElementById("showBackgroundDesigner").addEventListener("change", function () {
            applyBackgroundSrc();
        });
        document.getElementById("zoomRange").addEventListener("input", function (e) {
            state.scale = parseFloat(e.target.value) || 0.85;
            document.getElementById("zoomValue").textContent =
                Math.round(state.scale * 100) + "%";
            applyCanvasSize();
            renderFields();
        });

        document.getElementById("backgroundFile").addEventListener("change", function (e) {
            var file = e.target.files && e.target.files[0];
            if (!file) { return; }
            var formData = new FormData();
            formData.append("file", file);
            ajax("POST", UPLOAD_URL + "?name=" + encodeURIComponent(state.templateName),
                formData, true, function (status, data) {
                    if (status >= 200 && status < 300 && data && data.success) {
                        state.template.BackgroundFileName = data.fileName;
                        applyBackgroundSrc();
                        bgStatusEl.textContent = "تم رفع الخلفية: " + data.fileName;
                    } else {
                        bgStatusEl.textContent = (data && data.message) || "فشل رفع الصورة";
                    }
                });
        });
    }

    function syncPageInputs() {
        var t = state.template;
        document.getElementById("pageWidth").value = t.PageWidth || 0;
        document.getElementById("pageHeight").value = t.PageHeight || 0;
        document.getElementById("imageWidth").value = t.ImageWidth || 0;
        document.getElementById("imageHeight").value = t.ImageHeight || 0;
        document.getElementById("globalXShift").value = t.GlobalXShift || 0;
        document.getElementById("globalYShift").value = t.GlobalYShift || 0;
        document.getElementById("printBackground").checked = !!t.PrintBackground;
    }

    // ---------- Toolbar actions ----------
    function bindToolbar() {
        document.getElementById("templateSelect").addEventListener("change", function (e) {
            state.templateName = e.target.value;
            loadTemplate();
        });
        document.getElementById("reloadBtn").addEventListener("click", loadTemplate);

        document.getElementById("saveBtn").addEventListener("click", function () {
            setStatus("جاري الحفظ...", null);
            ajax("POST", SAVE_URL + "?name=" + encodeURIComponent(state.templateName),
                state.template, false, function (status, data) {
                    if (status >= 200 && status < 300 && data && data.success) {
                        setStatus(data.message || "تم الحفظ", "success");
                    } else {
                        setStatus((data && data.message) || "فشل الحفظ", "error");
                    }
                });
        });

        document.getElementById("previewBtn").addEventListener("click", function () {
            window.open(PREVIEW_URL + "?name=" + encodeURIComponent(state.templateName), "_blank");
        });

        document.getElementById("exportJsonBtn").addEventListener("click", function () {
            var blob = new Blob([JSON.stringify(state.template, null, 2)],
                { type: "application/json" });
            var a = document.createElement("a");
            a.href = URL.createObjectURL(blob);
            a.download = state.templateName + ".json";
            a.click();
        });

        document.getElementById("resetBtn").addEventListener("click", function () {
            if (!confirm("سيتم استبدال القالب بالقيم الافتراضية. متابعة؟")) { return; }
            // Reload the default by hitting GetTemplate after deleting
            // the saved file is too aggressive - instead just refetch
            // and let the server return defaults if file missing.
            loadTemplate(true);
        });
    }

    // ---------- Initial load ----------
    function loadTemplate(forceDefault) {
        ajax("GET", GET_URL + "?name=" + encodeURIComponent(state.templateName) +
            (forceDefault ? "&_=" + Date.now() : ""), null, false, function (status, data) {
                if (status < 200 || status >= 300 || !data) {
                    setStatus("تعذر تحميل القالب", "error");
                    return;
                }
                state.template = data;
                state.template.Fields = state.template.Fields || [];
                state.selectedFieldKey = null;
                applyCanvasSize();
                applyBackgroundSrc();
                syncPageInputs();
                renderPalette();
                renderFields();
                renderPropertiesPanel();
                setStatus("تم تحميل القالب: " + state.templateName, "success");
            });
    }

    bindToolbar();
    bindPageSettings();
    bindCursor();
    loadTemplate();
})();
