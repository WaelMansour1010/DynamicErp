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
        scale: 0.85,
        snapEnabled: false,
        gridStep: 5
    };

    function snap(value) {
        if (!state.snapEnabled || state.gridStep <= 0) { return value; }
        var step = state.gridStep;
        return Math.round(value / step) * step;
    }

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
        // Keep the snap-grid background sized in pixels that match the
        // template's gridStep at the current zoom level.
        if (state.gridStep > 0) {
            var px = unitsToScreen(state.gridStep);
            canvasEl.style.backgroundSize = px + "px " + px + "px";
        }
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
        var pendingFrame = null;

        function selectAndMark() {
            if (state.selectedFieldKey === field.FieldKey) { return; }
            state.selectedFieldKey = field.FieldKey;
            // Just update the selected class and rebuild props once - not
            // on every mousemove.
            var prev = document.querySelector(".ptd-field.is-selected");
            if (prev) { prev.classList.remove("is-selected"); }
            node.classList.add("is-selected");
            // Debug aid for the coordinate system: log the exact stored
            // X/Y/W/H of the selected field. Page coords are LTR - X is
            // the distance from the page's LEFT edge.
            if (window.console && console.log) {
                console.log("[PTD] selected field", field.FieldKey,
                    "X=" + field.X, "Y=" + field.Y,
                    "W=" + field.Width, "H=" + field.Height,
                    "(LTR: X from page left)");
            }
            renderPropertiesPanel();
            renderPalette();
        }

        node.addEventListener("mousedown", function (e) {
            if (e.target === handle) { return; }
            e.preventDefault();
            selectAndMark();

            dragState = {
                type: "move",
                startMouse: { x: e.clientX, y: e.clientY },
                startField: { x: field.X, y: field.Y }
            };

            window.addEventListener("mousemove", onMove);
            window.addEventListener("mouseup", onUp);
        });

        handle.addEventListener("mousedown", function (e) {
            e.preventDefault();
            e.stopPropagation();
            selectAndMark();

            dragState = {
                type: "resize",
                startMouse: { x: e.clientX, y: e.clientY },
                startField: { w: field.Width, h: field.Height }
            };

            window.addEventListener("mousemove", onMove);
            window.addEventListener("mouseup", onUp);
        });

        function onMove(e) {
            if (!dragState) { return; }
            // Coalesce mousemoves into a single frame so dragging stays
            // smooth at 60fps even when the user drags fast.
            dragState.lastMouse = { x: e.clientX, y: e.clientY };
            if (pendingFrame) { return; }
            pendingFrame = requestAnimationFrame(applyDrag);
        }

        function applyDrag() {
            pendingFrame = null;
            if (!dragState || !dragState.lastMouse) { return; }

            var dx = dragState.lastMouse.x - dragState.startMouse.x;
            var dy = dragState.lastMouse.y - dragState.startMouse.y;
            var dxUnits = screenToUnits(dx);
            var dyUnits = screenToUnits(dy);

            if (dragState.type === "move") {
                field.X = Math.max(0, snap(dragState.startField.x + dxUnits));
                field.Y = Math.max(0, snap(dragState.startField.y + dyUnits));
            } else {
                field.Width = Math.max(20, snap(dragState.startField.w + dxUnits));
                field.Height = Math.max(12, snap(dragState.startField.h + dyUnits));
                if (field.IsCellBased && field.CellCount > 0) {
                    field.CellWidth = Math.max(8, field.Width / field.CellCount);
                }
            }
            updateFieldNode(node, field);
            // Cheap update only - no full panel rebuild during drag.
            syncSelectedInputs(field);
        }

        function onUp() {
            window.removeEventListener("mousemove", onMove);
            window.removeEventListener("mouseup", onUp);
            if (pendingFrame) {
                cancelAnimationFrame(pendingFrame);
                pendingFrame = null;
            }
            dragState = null;
        }
    }

    // Update only the X/Y/W/H/CellWidth inputs in the properties panel
    // without rebuilding the whole DOM. Keeps dragging snappy.
    function syncSelectedInputs(field) {
        var pairs = [
            ["X", field.X], ["Y", field.Y],
            ["Width", field.Width], ["Height", field.Height],
            ["CellWidth", field.CellWidth]
        ];
        pairs.forEach(function (p) {
            var el = propsEl.querySelector('[data-prop="' + p[0] + '"]');
            if (el && document.activeElement !== el) {
                el.value = (Math.round(p[1] * 100) / 100);
            }
        });
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
            propsEl.appendChild(propRow("اتجاه الخانات (CellDirection)",
                select("CellDirection", field.CellDirection || "LTR",
                    ["LTR", "RTL"], field)));
            var cellDirHint = document.createElement("p");
            cellDirHint.className = "ptd-hint";
            cellDirHint.textContent = "LTR للأرقام والحروف اللاتينية (Token, National ID, Phone). RTL فقط لو محتوى الحقل عربي.";
            propsEl.appendChild(cellDirHint);
        }

        var hr3 = document.createElement("hr");
        propsEl.appendChild(hr3);

        var actionsRow = document.createElement("div");
        actionsRow.className = "ptd-prop-grid";

        var duplicateBtn = document.createElement("button");
        duplicateBtn.type = "button";
        duplicateBtn.className = "ptd-btn";
        duplicateBtn.textContent = "تكرار الحقل";
        duplicateBtn.addEventListener("click", function () { duplicateField(field); });
        actionsRow.appendChild(duplicateBtn);

        var resetBtn = document.createElement("button");
        resetBtn.type = "button";
        resetBtn.className = "ptd-btn";
        resetBtn.textContent = "إعادة الموقع للافتراضي";
        resetBtn.addEventListener("click", function () { resetFieldPosition(field); });
        actionsRow.appendChild(resetBtn);

        propsEl.appendChild(actionsRow);

        var deleteBtn = document.createElement("button");
        deleteBtn.type = "button";
        deleteBtn.className = "ptd-btn ptd-btn-warn";
        deleteBtn.style.marginTop = "8px";
        deleteBtn.style.width = "100%";
        deleteBtn.textContent = "حذف الحقل من القالب";
        deleteBtn.addEventListener("click", function () { removeField(field); });
        propsEl.appendChild(deleteBtn);
    }

    function removeField(field) {
        state.template.Fields = state.template.Fields.filter(function (f) {
            return f.FieldKey !== field.FieldKey;
        });
        state.selectedFieldKey = null;
        renderPalette();
        renderFields();
        renderPropertiesPanel();
    }

    function duplicateField(field) {
        var copy = JSON.parse(JSON.stringify(field));
        // Offset slightly so the duplicate doesn't sit exactly under the
        // original, and rename the key so palette/save don't collide.
        copy.X = (copy.X || 0) + 20;
        copy.Y = (copy.Y || 0) + 20;
        copy.FieldKey = field.FieldKey + "_copy";
        copy.Label = (field.Label || field.FieldKey) + " (نسخة)";
        // Avoid duplicate keys if user already made copies.
        var i = 2;
        while (findField(copy.FieldKey)) {
            copy.FieldKey = field.FieldKey + "_copy" + i;
            i++;
        }
        state.template.Fields.push(copy);
        state.selectedFieldKey = copy.FieldKey;
        renderPalette();
        renderFields();
        renderPropertiesPanel();
    }

    function resetFieldPosition(field) {
        // Resetting an arbitrary field needs a known default. We can ask
        // the server's GetTemplate (no name) to give us defaults, but a
        // simpler local fallback: place the field roughly in page center.
        field.X = Math.max(0, (state.template.PageWidth || 800) / 2 - (field.Width || 200) / 2);
        field.Y = Math.max(0, (state.template.PageHeight || 1100) / 2 - (field.Height || 22) / 2);
        renderFields();
        renderPropertiesPanel();
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
        input.setAttribute("data-prop", propKey);
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
        input.setAttribute("data-prop", propKey);
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
                if (window.console && console.log) {
                    console.log("[PTD] template loaded",
                        "name=" + state.templateName,
                        "version=" + (state.template.TemplateVersion || 0),
                        "pageWidth=" + state.template.PageWidth,
                        "pageHeight=" + state.template.PageHeight,
                        "fields=" + state.template.Fields.length);
                }
                applyCanvasSize();
                applyBackgroundSrc();
                syncPageInputs();
                renderPalette();
                renderFields();
                renderPropertiesPanel();
                setStatus("تم تحميل القالب: " + state.templateName, "success");
            });
    }

    function bindKeyboard() {
        document.addEventListener("keydown", function (e) {
            // Don't hijack typing in inputs/selects/textareas.
            var t = e.target;
            if (t && (t.tagName === "INPUT" || t.tagName === "SELECT" ||
                      t.tagName === "TEXTAREA" || t.isContentEditable)) {
                return;
            }
            if (!state.selectedFieldKey) { return; }
            var field = findField(state.selectedFieldKey);
            if (!field) { return; }

            // Ctrl+D = duplicate, Delete = remove
            if (e.key === "d" && (e.ctrlKey || e.metaKey)) {
                e.preventDefault();
                duplicateField(field);
                return;
            }
            if (e.key === "Delete" || e.key === "Del") {
                e.preventDefault();
                removeField(field);
                return;
            }

            var step;
            if (e.shiftKey) { step = 10; }
            else if (e.ctrlKey || e.metaKey) { step = 0.5; }
            else { step = 1; }

            var dx = 0, dy = 0;
            if (e.key === "ArrowLeft") { dx = -step; }
            else if (e.key === "ArrowRight") { dx = step; }
            else if (e.key === "ArrowUp") { dy = -step; }
            else if (e.key === "ArrowDown") { dy = step; }
            else { return; }

            e.preventDefault();
            field.X = Math.max(0, field.X + dx);
            field.Y = Math.max(0, field.Y + dy);
            renderFields();
            syncSelectedInputs(field);
        });
    }

    function bindZoom() {
        var zoomRange = document.getElementById("zoomRange");
        function applyZoom(value) {
            state.scale = Math.max(0.4, Math.min(1.5, value));
            zoomRange.value = state.scale;
            document.getElementById("zoomValue").textContent =
                Math.round(state.scale * 100) + "%";
            applyCanvasSize();
            renderFields();
        }
        document.getElementById("zoomOutBtn").addEventListener("click", function () {
            applyZoom(state.scale - 0.05);
        });
        document.getElementById("zoomInBtn").addEventListener("click", function () {
            applyZoom(state.scale + 0.05);
        });
        document.getElementById("zoomResetBtn").addEventListener("click", function () {
            applyZoom(1);
        });
    }

    function bindSnap() {
        var snapEl = document.getElementById("snapToGrid");
        var stepEl = document.getElementById("gridStep");
        var canvas = document.getElementById("canvas");

        function refresh() {
            state.snapEnabled = snapEl.checked;
            state.gridStep = Math.max(1, parseInt(stepEl.value, 10) || 5);
            canvas.classList.toggle("is-grid", state.snapEnabled);
            // Map grid step (template units) to screen pixels for the
            // CSS background-size so the visual grid matches.
            var px = unitsToScreen(state.gridStep);
            canvas.style.backgroundSize = px + "px " + px + "px";
        }
        snapEl.addEventListener("change", refresh);
        stepEl.addEventListener("input", refresh);
        refresh();
    }

    bindToolbar();
    bindPageSettings();
    bindCursor();
    bindKeyboard();
    bindZoom();
    bindSnap();
    loadTemplate();
})();
