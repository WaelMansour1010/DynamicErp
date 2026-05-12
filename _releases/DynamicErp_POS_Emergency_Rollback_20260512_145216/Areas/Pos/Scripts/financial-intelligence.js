(function () {
    "use strict";

    var lastTables = [];
    var chart;

    function q(selector, root) {
        return (root || document).querySelector(selector);
    }

    function qa(selector, root) {
        return Array.prototype.slice.call((root || document).querySelectorAll(selector));
    }

    function attr(name) {
        return document.body.getAttribute(name) || "";
    }

    function value(selector) {
        var element = q(selector);
        return element ? element.value : "";
    }

    function filters() {
        return {
            fromDate: value("[data-fi-from]"),
            toDate: value("[data-fi-to]"),
            branchId: value("[data-fi-branch]"),
            userId: value("[data-fi-user]"),
            employeeId: value("[data-fi-employee]"),
            accountCode: value("[data-fi-account]"),
            receivableParentSerial: value("[data-fi-receivable-parent]"),
            custodyParentSerial: value("[data-fi-custody-parent]")
        };
    }

    function escapeHtml(text) {
        return String(text === null || text === undefined ? "" : text)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function format(item) {
        if (item === null || item === undefined || item === "") {
            return "-";
        }

        if (typeof item === "string" && /Date\((\d+)\)/.test(item)) {
            var ticks = parseInt(RegExp.$1, 10);
            return new Date(ticks).toISOString().slice(0, 10);
        }

        if (typeof item === "number") {
            return Math.abs(item) >= 1000 ? item.toLocaleString(undefined, { maximumFractionDigits: 2 }) : item;
        }

        return item;
    }

    function riskClass(score) {
        score = parseFloat(score || 0);
        if (score >= 70) { return "high"; }
        if (score >= 35) { return "mid"; }
        return "low";
    }

    function post(url, data) {
        return fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json; charset=utf-8" },
            credentials: "same-origin",
            body: JSON.stringify(data || {})
        }).then(function (response) {
            return response.json().then(function (json) {
                if (!response.ok || !json.success) {
                    throw new Error(json.message || json.technicalMessage || "تعذر التحميل");
                }
                return json.data;
            });
        });
    }

    function rows(table) {
        return table ? (table.rows || table.Rows || []) : [];
    }

    function columns(table) {
        return table ? (table.columns || table.Columns || []) : [];
    }

    function renderKpis(table) {
        var host = q("[data-fi-kpis]");
        if (!host) { return; }
        host.innerHTML = "";
        var items = rows(table);
        if (!items.length) {
            host.innerHTML = "<article class=\"fi-kpi fi-kpi-empty\"><span>ملخص النتائج</span><strong>-</strong><small>اختر الفلاتر ثم اضغط تشغيل التشخيص لعرض المؤشرات.</small></article>";
            return;
        }

        var row = items[0];
        Object.keys(row).forEach(function (key) {
            var card = document.createElement("article");
            card.className = "fi-kpi";
            card.innerHTML = "<span>" + escapeHtml(key) + "</span><strong>" + escapeHtml(format(row[key])) + "</strong>";
            host.appendChild(card);
        });
    }

    function actionButtons(row, options) {
        var html = "";
        var rootUrl = attr("data-fi-root-url");
        var accountUrl = attr("data-fi-account-url");
        var sourceUrl = attr("data-fi-source-url");
        var journalUrl = attr("data-fi-journal-url");
        var voucherId = row.Double_Entry_Vouchers_ID || row.DoubleEntryVoucherId || row.VoucherId;
        var noteId = row.Notes_ID || row.NotesId || row.NoteId;
        var transactionId = row.Transaction_ID || row.TransactionId;
        var account = row.AccountCode || row.Account_Code || row.AccountCodeFrom || "";

        if (voucherId || noteId || transactionId) {
            html += "<button type=\"button\" class=\"fi-mini-btn\" data-fi-open-journal data-voucher=\"" + escapeHtml(voucherId || "") + "\" data-note=\"" + escapeHtml(noteId || "") + "\" data-transaction=\"" + escapeHtml(transactionId || "") + "\" data-account=\"" + escapeHtml(account) + "\"><i class=\"fas fa-book-open\"></i>فتح القيد</button>";
        } else if (journalUrl) {
            html += "<a class=\"fi-mini-btn\" href=\"" + journalUrl + "\"><i class=\"fas fa-book-open\"></i>فتح القيد</a>";
        }

        if (account && rootUrl) {
            html += "<a class=\"fi-mini-btn\" href=\"" + rootUrl + "?accountCode=" + encodeURIComponent(account) + "\"><i class=\"fas fa-search\"></i>تحليل السبب</a>";
        }

        if (account && accountUrl) {
            html += "<a class=\"fi-mini-btn\" href=\"" + accountUrl + "?accountCode=" + encodeURIComponent(account) + "\"><i class=\"fas fa-file-invoice\"></i>كشف الحساب</a>";
        }

        if (transactionId && sourceUrl) {
            html += "<a class=\"fi-mini-btn\" href=\"" + sourceUrl + "?transactionId=" + encodeURIComponent(transactionId) + "\"><i class=\"fas fa-external-link-alt\"></i>فتح المستند الأصلي</a>";
        }

        if (options && options.impact && account && rootUrl) {
            html += "<a class=\"fi-mini-btn\" href=\"" + rootUrl + "?accountCode=" + encodeURIComponent(account) + "\"><i class=\"fas fa-chart-bar\"></i>أكبر الحركات المؤثرة</a>";
        }

        return html ? "<div class=\"fi-row-actions\">" + html + "</div>" : "-";
    }

    function renderTable(table, target, options) {
        var host = typeof target === "string" ? q(target) : target;
        if (!host) { return; }

        options = options || {};
        var tableRows = rows(table);
        var tableColumns = columns(table);
        if (!tableRows.length) {
            host.innerHTML = "<div class=\"fi-empty\">لا توجد بيانات ضمن الفلاتر الحالية.</div>";
            return;
        }

        var html = "<div class=\"fi-table-wrap\"><table class=\"fi-table\"><thead><tr>";
        tableColumns.forEach(function (column) {
            html += "<th>" + escapeHtml(column) + "</th>";
        });
        html += "<th>إجراءات</th></tr></thead><tbody>";

        tableRows.forEach(function (row) {
            html += "<tr>";
            tableColumns.forEach(function (column) {
                var cell = format(row[column]);
                if (/RiskScore|درجة/i.test(column)) {
                    cell = "<span class=\"fi-risk " + riskClass(row[column]) + "\">" + escapeHtml(format(row[column])) + "</span>";
                } else {
                    cell = escapeHtml(cell);
                }
                html += "<td>" + cell + "</td>";
            });
            html += "<td>" + actionButtons(row, options) + "</td>";
            html += "</tr>";
        });

        html += "</tbody></table></div>";
        host.innerHTML = html;
    }

    function renderTimeline(table) {
        var canvas = q("[data-fi-chart]");
        if (!canvas || !window.Chart) { return; }
        var chartBox = canvas.parentNode;

        var tableRows = rows(table);
        if (!tableRows.length) {
            if (chart) { chart.destroy(); chart = null; }
            if (chartBox) {
                chartBox.classList.add("is-empty");
                var empty = q(".fi-chart-empty", chartBox);
                if (!empty) {
                    empty = document.createElement("div");
                    empty.className = "fi-chart-empty";
                    chartBox.appendChild(empty);
                }
                empty.textContent = "اختر الفلاتر ثم اضغط تشغيل التشخيص لعرض النتائج";
            }
            return;
        }

        if (chartBox) {
            chartBox.classList.remove("is-empty");
            var oldEmpty = q(".fi-chart-empty", chartBox);
            if (oldEmpty) {
                oldEmpty.parentNode.removeChild(oldEmpty);
            }
        }

        var labels = tableRows.map(function (r) { return format(r.MovementDate || r.JournalDate || r.TransactionDate || r.DayDate || r.ReportDate); });
        var data = tableRows.map(function (r) {
            return parseFloat(r.NetMovement || r.CurrentBalance || r.RunningBalance || r.NetCashMovement || r.ProfitabilityIndicator || 0);
        });

        if (chart) { chart.destroy(); }
        chart = new Chart(canvas.getContext("2d"), {
            type: "line",
            data: {
                labels: labels,
                datasets: [{
                    label: "صافي الحركة",
                    data: data,
                    borderColor: "#0f766e",
                    backgroundColor: "rgba(15,118,110,.12)",
                    tension: .25,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                legend: { display: false },
                scales: { yAxes: [{ ticks: { beginAtZero: false } }] }
            }
        });
    }

    function renderDrilldown(data) {
        var host = q("[data-fi-drilldown]");
        if (!host) { return; }
        var tables = data.tables || data.Tables || [];
        var parts = [];
        tables.forEach(function (table, index) {
            parts.push("<div class=\"fi-drill-section\"><h3>" + (index === 0 ? "بيانات القيد" : "تفاصيل السطور") + "</h3>");
            parts.push("<div data-fi-drill-table=\"" + index + "\"></div></div>");
        });
        host.innerHTML = parts.join("");
        tables.forEach(function (table, index) {
            renderTable(table, "[data-fi-drill-table=\"" + index + "\"]");
        });
    }

    function openJournal(button) {
        var url = attr("data-fi-journal-details-url");
        if (!url) { return; }
        var host = q("[data-fi-drilldown]");
        if (host) {
            host.innerHTML = "<div class=\"fi-loading\">جاري فتح تفاصيل القيد...</div>";
        }
        post(url, {
            doubleEntryVoucherId: button.getAttribute("data-voucher") || null,
            notesId: button.getAttribute("data-note") || null,
            transactionId: button.getAttribute("data-transaction") || null,
            accountCode: button.getAttribute("data-account") || null
        }).then(renderDrilldown).catch(function (error) {
            if (host) { host.innerHTML = "<div class=\"fi-alert\">" + escapeHtml(error.message) + "</div>"; }
        });
    }

    function renderPage(data) {
        var tables = data.tables || data.Tables || [];
        lastTables = tables;
        var mode = attr("data-fi-mode") || "dashboard";

        if (mode === "dashboard") {
            renderKpis(tables[0]);
            renderTimeline(tables[1]);
            renderTable(tables[2], "[data-fi-main-table]", { impact: true });
            renderTable(tables[3], "[data-fi-side-table]", { impact: true });
            renderTable(tables[4], "[data-fi-extra-table]", { impact: true });
        } else if (mode === "root") {
            renderTimeline(tables[0]);
            renderTable(tables[0], "[data-fi-main-table]");
            renderTable(tables[1], "[data-fi-side-table]", { impact: true });
            renderTable(tables[2], "[data-fi-extra-table]", { impact: true });
        } else {
            var timelineIndex = parseInt(attr("data-fi-timeline-index") || "0", 10);
            if (isNaN(timelineIndex) || timelineIndex < 0) { timelineIndex = 0; }
            renderTimeline(tables[timelineIndex]);
            renderTable(tables[0], "[data-fi-main-table]", { impact: true });
            renderTable(tables[1], "[data-fi-side-table]", { impact: true });
            renderTable(tables[2], "[data-fi-extra-table]", { impact: true });
        }

        var explanation = q("[data-fi-explanation]");
        var explanationRows = rows(tables[4]);
        if (explanation && explanationRows.length) {
            explanation.innerHTML = escapeHtml(explanationRows[0].ExplanationText || "");
        }
    }

    function loadPage() {
        var endpoint = attr("data-fi-endpoint");
        if (!endpoint) { return; }
        var button = q("[data-fi-load]");
        var buttonText = button ? button.innerHTML : "";
        if (button) {
            button.disabled = true;
            button.classList.add("is-loading");
            button.innerHTML = "<i class=\"fas fa-spinner fa-spin\"></i>جاري التشغيل";
        }
        qa("[data-fi-error]").forEach(function (x) { x.innerHTML = ""; });

        post(endpoint, filters()).then(renderPage).catch(function (error) {
            var host = q("[data-fi-error]");
            if (host) { host.innerHTML = "<div class=\"fi-alert\">" + escapeHtml(error.message) + "</div>"; }
        }).finally(function () {
            if (button) {
                button.disabled = false;
                button.classList.remove("is-loading");
                button.innerHTML = buttonText;
            }
        });
    }

    function exportExcel() {
        if (!lastTables.length) { return; }
        var html = "";
        lastTables.forEach(function (table, index) {
            html += "<h3>Table " + (index + 1) + "</h3><table border=\"1\"><tr>";
            columns(table).forEach(function (column) { html += "<th>" + escapeHtml(column) + "</th>"; });
            html += "</tr>";
            rows(table).forEach(function (row) {
                html += "<tr>";
                columns(table).forEach(function (column) { html += "<td>" + escapeHtml(format(row[column])) + "</td>"; });
                html += "</tr>";
            });
            html += "</table>";
        });
        var blob = new Blob(["\ufeff" + html], { type: "application/vnd.ms-excel;charset=utf-8" });
        var link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = "financial-intelligence.xls";
        link.click();
        URL.revokeObjectURL(link.href);
    }

    document.addEventListener("DOMContentLoaded", function () {
        qa("[data-fi-main-table], [data-fi-side-table], [data-fi-extra-table], [data-fi-drilldown]").forEach(function (host) {
            if (!host.innerHTML) {
                host.innerHTML = "<div class=\"fi-empty\">اختر الفلاتر ثم اضغط تشغيل التشخيص لعرض النتائج</div>";
            }
        });
        renderTimeline(null);
        var button = q("[data-fi-load]");
        if (button) {
            button.innerHTML = "<i class=\"fas fa-search\"></i>تشغيل التشخيص";
            button.addEventListener("click", loadPage);
        }
        var exportButton = q("[data-fi-export]");
        if (exportButton) {
            exportButton.innerHTML = "<i class=\"fas fa-file-excel\"></i>تصدير Excel";
            exportButton.addEventListener("click", exportExcel);
        }
        document.addEventListener("click", function (event) {
            var openButton = event.target.closest ? event.target.closest("[data-fi-open-journal]") : null;
            if (openButton) {
                openJournal(openButton);
            }
        });
        if (attr("data-fi-autoload") === "true") {
            loadPage();
        }
    });
}());
