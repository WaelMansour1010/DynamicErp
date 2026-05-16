(function () {
    "use strict";

    var page = document.getElementById("optionsPage");
    if (!page) {
        return;
    }

    var saveBtn = document.getElementById("optionsSaveBtn");
    var statusBox = document.getElementById("optionsStatus");
    var searchBox = document.getElementById("optionsSearch");

    function showStatus(message, type) {
        statusBox.hidden = false;
        statusBox.textContent = message;
        statusBox.className = "options-status " + (type || "info");
    }

    function token() {
        var input = page.querySelector("input[name='__RequestVerificationToken']");
        return input ? input.value : "";
    }

    function activateCategory(key) {
        var tabs = page.querySelectorAll(".options-tab");
        var panels = page.querySelectorAll(".options-category");

        Array.prototype.forEach.call(tabs, function (tab) {
            tab.classList.toggle("active", tab.getAttribute("data-category") === key);
        });

        Array.prototype.forEach.call(panels, function (panel) {
            panel.classList.toggle("active", panel.getAttribute("data-category-panel") === key);
        });
    }

    function collectPayload() {
        var fields = page.querySelectorAll(".option-field");
        var payload = [];

        Array.prototype.forEach.call(fields, function (field) {
            var name = field.getAttribute("data-name");
            if (!name || field.disabled) {
                return;
            }

            payload.push({
                name: name,
                value: field.type === "checkbox" ? (field.checked ? "true" : "false") : field.value
            });
        });

        return payload;
    }

    function save() {
        if (!saveBtn || saveBtn.disabled) {
            return;
        }

        var fields = collectPayload();
        var data = new FormData();
        data.append("__RequestVerificationToken", token());

        fields.forEach(function (field, index) {
            data.append("Fields[" + index + "].Name", field.name);
            data.append("Fields[" + index + "].Value", field.value);
        });

        saveBtn.disabled = true;
        showStatus("جار حفظ إعدادات النظام...", "info");

        fetch(page.getAttribute("data-save-url"), {
            method: "POST",
            body: data,
            credentials: "same-origin"
        })
            .then(function (response) {
                return response.json().then(function (json) {
                    if (!response.ok || !json.Success) {
                        throw new Error(json.Message || "تعذر حفظ الإعدادات.");
                    }

                    return json;
                });
            })
            .then(function (json) {
                showStatus(json.Message + " (" + json.UpdatedFields + " حقل)", "success");
            })
            .catch(function (error) {
                showStatus(error.message || "تعذر حفظ الإعدادات.", "error");
            })
            .finally(function () {
                saveBtn.disabled = false;
            });
    }

    Array.prototype.forEach.call(page.querySelectorAll(".options-tab"), function (tab) {
        tab.addEventListener("click", function () {
            activateCategory(tab.getAttribute("data-category"));
        });
    });

    if (searchBox) {
        searchBox.addEventListener("input", function () {
            var text = (searchBox.value || "").toLowerCase().trim();
            var cards = page.querySelectorAll("[data-field-card]");

            Array.prototype.forEach.call(cards, function (card) {
                var haystack = card.getAttribute("data-search") || "";
                card.hidden = text.length > 0 && haystack.indexOf(text) === -1;
            });

            if (text.length > 0) {
                Array.prototype.forEach.call(page.querySelectorAll(".options-category"), function (panel) {
                    panel.classList.add("active");
                });
            } else {
                var activeTab = page.querySelector(".options-tab.active") || page.querySelector(".options-tab");
                if (activeTab) {
                    activateCategory(activeTab.getAttribute("data-category"));
                }
            }
        });
    }

    if (saveBtn) {
        saveBtn.addEventListener("click", save);
    }
})();
