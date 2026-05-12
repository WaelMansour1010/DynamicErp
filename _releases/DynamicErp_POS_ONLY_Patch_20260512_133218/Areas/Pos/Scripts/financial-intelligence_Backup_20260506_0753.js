(function () {
    "use strict";

    function q(selector, root) {
        return (root || document).querySelector(selector);
    }

    function qa(selector, root) {
        return Array.prototype.slice.call((root || document).querySelectorAll(selector));
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

    function format(value) {
        if (value === null || value === undefined || value === "") {
            return "-";
        }

        if (typeof value === "string" && /Date\((\d+)\)/.test(value)) {
            var ticks = parseInt(RegExp.$1, 10);
            return new Date(ticks).toISOString().slice(0, 10);
        }

        if (typeof value === "number") {
            return Math.abs(value) >= 1000 ? value.toLocaleString(undefined, { maximumFractionDigits: 2 }) : value;
        }

        return value;
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
            body: JSON.stringify(data || {})
        }).then(function (response) {
            return response.json().then(function (json) {
                if (!response.ok || !json.success) {
                    throw new Error(json.message || json.technicalMessage || "Load failed");
                }
                return json.data;
            });
        });
    }

    function renderKpis(table) {
        var host = q("[data-fi-kpis]");
        if (!host) { return; }
        host.innerHTML = "";
        var rows = table ? (table.rows || table.Rows || []) : [];
        if (!rows.length) {
            return;
        }
        var row = rows[0];
        Object.keys(row).forEach(function (key) {
            var card = document.createElement("article");
            card.className = "fi-kpi";
            card.innerHTML = "<span>" + key + "</span><strong>" + format(row[key]) + "</strong>";
            host.appendChild(card);
        });
    }

    function renderTable(table, target, options) {
        var host = typeof target === "string" ? q(target) : target;
        if (!host) { return; }
        options = options || {};
        var rows = table ? (table.rows || table.Rows || []) : [];
        var columns = table ? (table.columns || table.Columns || []) : [];
        if (!rows.length) {
            host.innerHTML = "<div class=\"fi-empty\">لا توجد بيانات ضمن الفلاتر الحالية.</div>";
            return;
        }

        var html = "<div class=\"fi-table-wrap\"><table class=\"fi-table\"><thead><tr>";
        columns.forEach(function (column) {
            html += "<th>" + column + "</th>";
        });
        if (options.rootCause) {
            html += "<th>Analyze</th>";
        }
        html += "</tr></thead><tbody>";
        rows.forEach(function (row) {
            html += "<tr>";
            columns.forEach(function (column) {
                var cell = format(row[column]);
                if (/RiskScore/i.test(column)) {
                    cell = "<span class=\"fi-risk " + riskClass(row[column]) + "\">" + format(row[column]) + "</span>";
                }
                html += "<td>" + cell + "</td>";
            });
            if (options.rootCause) {
                var account = row.AccountCode || row.Account_Code || "";
                var href = options.rootUrl + "?accountCode=" + encodeURIComponent(account);
                html += "<td><a class=\"fi-btn\" href=\"" + href + "\">Analyze Root Cause</a></td>";
            }
            html += "</tr>";
        });
        html += "</tbody></table></div>";
        host.innerHTML = html;
    }

    var chart;
    function renderTimeline(table) {
        var canvas = q("[data-fi-chart]");
        if (!canvas || !window.Chart) { return; }
        var rows = table ? (table.rows || table.Rows || []) : [];
        var labels = rows.map(function (r) { return format(r.MovementDate); });
        var data = rows.map(function (r) { return parseFloat(r.NetMovement || r.CurrentBalance || r.RunningBalance || 0); });
        if (chart) { chart.destroy(); }
        chart = new Chart(canvas.getContext("2d"), {
            type: "line",
            data: {
                labels: labels,
                datasets: [{
                    label: "Net movement",
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

    function loadPage() {
        var endpoint = document.body.getAttribute("data-fi-endpoint");
        if (!endpoint) { return; }
        var button = q("[data-fi-load]");
        if (button) { button.disabled = true; }
        qa("[data-fi-error]").forEach(function (x) { x.innerHTML = ""; });

        post(endpoint, filters()).then(function (data) {
            var tables = data.tables || data.Tables || [];
            var mode = document.body.getAttribute("data-fi-mode") || "dashboard";
            if (mode === "dashboard") {
                renderKpis(tables[0]);
                renderTable(tables[1], "[data-fi-main-table]", { rootCause: false });
                renderTable(tables[2], "[data-fi-side-table]", { rootCause: true, rootUrl: document.body.getAttribute("data-fi-root-url") || "" });
            } else if (mode === "root") {
                renderTimeline(tables[0]);
                renderTable(tables[0], "[data-fi-main-table]");
                renderTable(tables[1], "[data-fi-side-table]");
            } else {
                renderTable(tables[0], "[data-fi-main-table]", { rootCause: true, rootUrl: document.body.getAttribute("data-fi-root-url") || "" });
                renderTable(tables[1], "[data-fi-side-table]");
                renderTimeline(tables[2]);
            }
            var explanation = q("[data-fi-explanation]");
            var explanationRows = tables[4] ? (tables[4].rows || tables[4].Rows || []) : [];
            if (explanation && explanationRows.length) {
                explanation.innerHTML = explanationRows[0].ExplanationText || "";
            }
        }).catch(function (error) {
            var host = q("[data-fi-error]");
            if (host) { host.innerHTML = "<div class=\"fi-alert\">" + error.message + "</div>"; }
        }).finally(function () {
            if (button) { button.disabled = false; }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        var button = q("[data-fi-load]");
        if (button) {
            button.addEventListener("click", loadPage);
        }
        if (document.body.getAttribute("data-fi-autoload") === "true") {
            loadPage();
        }
    });
}());
