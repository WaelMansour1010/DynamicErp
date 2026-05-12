(function () {
    'use strict';

    var page = document.getElementById('posClosingPage');
    if (!page) {
        return;
    }

    var lastValues = null;

    function byId(id) {
        return document.getElementById(id);
    }

    function setValue(id, value) {
        var element = byId(id);
        if (element) {
            element.value = value == null ? '' : value;
        }
    }

    function money(value) {
        var number = Number(value || 0);
        return number.toFixed(2);
    }

    function todayIso() {
        var now = new Date();
        return now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0') + '-' + String(now.getDate()).padStart(2, '0');
    }

    function postJson(url, data) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json; charset=utf-8'
            },
            body: JSON.stringify(data || {})
        }).then(function (response) {
            return response.json().then(function (payload) {
                if (!response.ok || payload.success === false) {
                    var error = new Error(payload.message || 'تعذر تنفيذ العملية');
                    error.payload = payload;
                    throw error;
                }
                return payload;
            });
        });
    }

    function currentRequest() {
        return {
            ClosingDate: byId('closingDate').value,
            BranchId: Number(byId('branchId').value || page.getAttribute('data-default-branch-id') || 0)
        };
    }

    function showMessage(text, isError) {
        var message = byId('closingMessage');
        if (!message) {
            return;
        }
        message.textContent = text || '';
        message.classList.toggle('error-text', !!isError);
    }

    function fillValues(values) {
        lastValues = values || {};
        setValue('noteSerial', lastValues.NoteSerial || '');
        setValue('closingStatus', lastValues.AlreadyClosed ? 'مغلق سابقاً' : 'غير مغلق');
        setValue('openBalance', money(lastValues.OpenBalance));
        setValue('totalSaleDay', money(lastValues.TotalSaleDay));
        setValue('lastBalance', money(lastValues.LastBalance));
        setValue('countTransaction', money(lastValues.CountTransaction));
        setValue('countCards', money(lastValues.CountCards));
        setValue('totalRechargeValue', money(lastValues.TotalRechargeValue));
        setValue('totalRev', money(lastValues.TotalRev));
        setValue('totalRevVat', money(lastValues.TotalRevVat));
        setValue('cashOutTotal', money(lastValues.CashOutTotal));
        setValue('cashOutDisc', money(lastValues.CashOutDisc));
        setValue('boxBalance', money(lastValues.BoxBalance));
        setValue('totalSupply', money(lastValues.TotalSupply));
        setValue('totalWallet', money(lastValues.TotalWallet));
        setValue('totalPOS', money(lastValues.TotalPOS));
        setValue('totalRev2', money(lastValues.TotalRev2));
        setValue('totalSaleDay2', money(lastValues.TotalSaleDay2));
        setValue('net', money(lastValues.Net));
        setValue('actValue', money(lastValues.ActValue));
        setValue('diff', money(lastValues.Diff));
        setValue('boxBalanceAccount', [lastValues.BoxBalanceAccountSerial, lastValues.BoxBalanceAccountCode].filter(Boolean).join(' / '));
        renderRechargeRows(lastValues.RechargeRows || []);
        renderVoucherHeaders(lastValues.Vouchers || []);
        renderCreatedVouchers(lastValues.VoucherLines || []);
    }

    function renderRechargeRows(rows) {
        var body = byId('rechargeRows');
        if (!body) {
            return;
        }

        if (!rows.length) {
            body.innerHTML = '<tr><td colspan="6">لا توجد تفاصيل شحن</td></tr>';
            return;
        }

        body.innerHTML = rows.map(function (row) {
            return '<tr>' +
                '<td>' + escapeHtml(row.NameShow || '') + '</td>' +
                '<td>' + escapeHtml(row.Account_Code || '') + '</td>' +
                '<td>' + escapeHtml(row.BankAccountCode || '') + '</td>' +
                '<td>' + money(row.RechargeValue) + '</td>' +
                '<td>' + money(row.Comm1) + '</td>' +
                '<td>' + money(row.Comm2) + '</td>' +
                '</tr>';
        }).join('');
    }

    function renderCreatedVouchers(lines) {
        var body = byId('createdVouchers');
        if (!body) {
            return;
        }

        if (!lines || !lines.length) {
            body.innerHTML = '<tr><td colspan="5">لم يتم تنفيذ إغلاق بعد</td></tr>';
            return;
        }

        var debitTotal = 0;
        var creditTotal = 0;
        body.innerHTML = lines.map(function (line) {
            debitTotal += Number(line.Debit || 0);
            creditTotal += Number(line.Credit || 0);
            return '<tr>' +
                '<td>' + escapeHtml(line.AccountSerial || line.AccountCode || '') + '</td>' +
                '<td>' + escapeHtml(line.AccountName || '') + '</td>' +
                '<td>' + escapeHtml(line.Description || '') + '</td>' +
                '<td>' + money(line.Debit) + '</td>' +
                '<td>' + money(line.Credit) + '</td>' +
                '</tr>';
        }).join('') +
            '<tr class="voucher-total-row">' +
            '<td colspan="3">إجمالي المدين / إجمالي الدائن / الفرق</td>' +
            '<td>' + money(debitTotal) + '</td>' +
            '<td>' + money(creditTotal) + '<br /><span>الفرق: ' + money(debitTotal - creditTotal) + '</span></td>' +
            '</tr>';
    }

    function displayDate(value) {
        if (!value) {
            return '';
        }

        var date = null;
        if (Object.prototype.toString.call(value) === '[object Date]') {
            date = value;
        } else {
            var text = String(value).trim();
            var match = /\/?Date\((-?\d+)(?:[+-]\d+)?\)\/?/.exec(text);
            if (match) {
                date = new Date(parseInt(match[1], 10));
            } else if (/^\d{4}-\d{2}-\d{2}/.test(text)) {
                date = new Date(parseInt(text.substring(0, 4), 10), parseInt(text.substring(5, 7), 10) - 1, parseInt(text.substring(8, 10), 10));
            } else {
                date = new Date(text);
            }
        }

        if (!date || isNaN(date.getTime())) {
            return '';
        }

        return ('0' + date.getDate()).slice(-2) + '/' + ('0' + (date.getMonth() + 1)).slice(-2) + '/' + date.getFullYear();
    }

    function renderVoucherHeaders(vouchers) {
        var container = byId('voucherHeaders');
        if (!container) {
            return;
        }

        if (!vouchers || !vouchers.length) {
            container.innerHTML = '';
            return;
        }

        container.innerHTML = vouchers.map(function (voucher) {
            return '<div class="voucher-header-card">' +
                '<span>نوع القيد: ' + escapeHtml(voucher.VoucherType || '') + '</span>' +
                '<span>رقم القيد: ' + escapeHtml(voucher.NoteSerial || voucher.NoteId || '') + '</span>' +
                '<span>التاريخ: ' + escapeHtml(displayDate(voucher.NoteDate)) + '</span>' +
                '</div>';
        }).join('');
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function loadValues() {
        showMessage('جاري تحديث بيانات الإغلاق...', false);
        return postJson(page.getAttribute('data-load-url'), currentRequest())
            .then(function (payload) {
                fillValues(payload.values);
                showMessage('تم تحديث بيانات الإغلاق', false);
            })
            .catch(function (error) {
                var payload = error.payload || {};
                showMessage((payload.message || error.message) + (payload.technicalMessage ? ' - التفاصيل: ' + payload.technicalMessage : ''), true);
            });
    }

    function executeClosing() {
        if (!lastValues) {
            showMessage('اضغط تحديث البيانات قبل تنفيذ الإغلاق', true);
            return;
        }

        var password = byId('closingPassword').value;
        if (!password) {
            showMessage('كلمة المرور مطلوبة لتنفيذ الإغلاق', true);
            return;
        }

        if (!confirm('سيتم إنشاء قيد إغلاق فعلي. هل تريد المتابعة؟')) {
            return;
        }

        var request = currentRequest();
        request.Password = password;
        request.ActualValue = Number(byId('actValue').value || 0);

        showMessage('جاري تنفيذ الإغلاق...', false);
        postJson(page.getAttribute('data-execute-url'), request)
            .then(function (payload) {
                var result = payload.result || {};
                setValue('closingResult', result.Message || 'تم إنشاء قيد الإغلاق');
                setValue('noteSerial', result.NoteSerial || '');
                renderVoucherHeaders(result.Vouchers || []);
                renderCreatedVouchers(result.VoucherLines || []);
                showMessage('تم إنشاء قيد الإغلاق بنجاح. رقم القيد: ' + (result.NoteSerial || result.NoteId || ''), false);
                return loadValues();
            })
            .catch(function (error) {
                var payload = error.payload || {};
                showMessage((payload.message || error.message) + (payload.technicalMessage ? ' - التفاصيل: ' + payload.technicalMessage : ''), true);
            });
    }

    function updateDiff() {
        if (!lastValues) {
            return;
        }
        var actual = Number(byId('actValue').value || 0);
        setValue('diff', money(actual - Number(lastValues.Net || 0)));
    }

    byId('closingDate').value = todayIso();
     byId('loadClosingBtn').addEventListener('click', loadValues);
     byId('executeClosingBtn').addEventListener('click', executeClosing);
     byId('actValue').addEventListener('input', updateDiff);
     showMessage('اضغط تحديث البيانات لعرض أرقام الإغلاق.', false);
 })();
