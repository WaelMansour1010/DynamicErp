(function () {
    var page = document.getElementById('financialExpensesPage');
    if (!page) return;
    function qs(id) { return document.getElementById(id); }
    function val(id) { var el = qs(id); return el ? el.value : ''; }
    function intOrNull(id) { var v = val(id); return v === '' ? null : parseInt(v, 10); }
    function show(message, success) {
        var s = qs('finStatus');
        s.textContent = message || '';
        s.hidden = false;
        s.className = 'financial-expenses-status ' + (success ? 'is-success' : 'is-error');
    }
    function setMode(mode) {
        mode = mode === 'FrmExpenses30' ? 'FrmExpenses30' : 'FrmExpenses3';
        qs('finMode').value = mode;
        Array.prototype.forEach.call(document.querySelectorAll('.financial-expenses-tabs button'), function (b) {
            b.classList.toggle('active', b.getAttribute('data-mode') === mode);
        });
    }
    function post(url, payload, done) {
        var xhr = new XMLHttpRequest();
        xhr.open('POST', url, true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status >= 200 && xhr.status < 300) done(JSON.parse(xhr.responseText || '{}'));
            else show('تعذر الاتصال بالخادم.', false);
        };
        xhr.send(JSON.stringify(payload));
    }
    function get(url, done) {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status >= 200 && xhr.status < 300) done(JSON.parse(xhr.responseText || '{}'));
            else show('تعذر تحميل البيانات.', false);
        };
        xhr.send();
    }
    function addLine(line) {
        qs('finLinesBody').insertAdjacentHTML('beforeend', qs('finLineTemplate').innerHTML);
        var row = qs('finLinesBody').querySelector('tr:last-child');
        line = line || {};
        row.querySelector('.line-account').value = line.AccountCode || '';
        row.querySelector('.line-value').value = line.Value || 0;
        row.querySelector('.line-vat').value = line.Vat || 0;
        row.querySelector('.line-description').value = line.Description || '';
        updateTotal();
    }
    function clearForm() {
        ['finId', 'finDetailNoteId', 'finDocId', 'finJournalSerial', 'finVoucherSerial', 'finChequeNumber', 'finChequeDueDate', 'finCreditAccountCode', 'finGeneralDescription'].forEach(function (id) { qs(id).value = ''; });
        qs('finDate').value = new Date().toISOString().substring(0, 10);
        qs('finBranchId').value = '';
        qs('finBillType').value = '0';
        qs('finPaymentType').value = '0';
        qs('finBoxId').value = '';
        qs('finBankId').value = '';
        qs('finVendorId').value = '';
        qs('finExpensesTypeId').value = '';
        qs('finLinesBody').innerHTML = '';
        addLine();
        show('جاهز لإدخال مستند جديد.', true);
    }
    function collectLines() {
        return Array.prototype.map.call(document.querySelectorAll('#finLinesBody tr'), function (row) {
            var account = row.querySelector('.line-account');
            return {
                AccountCode: account.value,
                AccountName: account.options[account.selectedIndex] ? account.options[account.selectedIndex].text : '',
                Value: Number(row.querySelector('.line-value').value || 0),
                Vat: Number(row.querySelector('.line-vat').value || 0),
                VatPercent: 0,
                Description: row.querySelector('.line-description').value,
                BranchId: intOrNull('finBranchId')
            };
        });
    }
    function collectPayload() {
        return {
            Id: intOrNull('finId'),
            DetailNoteId: intOrNull('finDetailNoteId'),
            Mode: val('finMode'),
            Date: val('finDate') || null,
            JournalSerial: val('finJournalSerial'),
            VoucherSerial: val('finVoucherSerial'),
            BillType: parseInt(val('finBillType') || '0', 10),
            PaymentType: parseInt(val('finPaymentType') || '0', 10),
            BranchId: intOrNull('finBranchId'),
            BoxId: intOrNull('finBoxId'),
            BankId: intOrNull('finBankId'),
            VendorId: intOrNull('finVendorId'),
            ChequeNumber: val('finChequeNumber'),
            ChequeDueDate: val('finChequeDueDate') || null,
            ExpensesTypeId: intOrNull('finExpensesTypeId'),
            CreditAccountCode: val('finCreditAccountCode'),
            GeneralDescription: val('finGeneralDescription'),
            Remarks: val('finGeneralDescription'),
            Lines: collectLines()
        };
    }
    function populate(data) {
        setMode(data.Mode);
        qs('finId').value = data.Id || '';
        qs('finDetailNoteId').value = data.DetailNoteId || '';
        qs('finDocId').value = data.Id || '';
        qs('finJournalSerial').value = data.JournalSerial || '';
        qs('finVoucherSerial').value = data.VoucherSerial || '';
        qs('finDate').value = data.Date ? String(data.Date).substring(0, 10) : '';
        qs('finBranchId').value = data.BranchId || '';
        qs('finBillType').value = data.BillType || 0;
        qs('finPaymentType').value = data.PaymentType || 0;
        qs('finBoxId').value = data.BoxId || '';
        qs('finBankId').value = data.BankId || '';
        qs('finVendorId').value = data.VendorId || '';
        qs('finChequeNumber').value = data.ChequeNumber || '';
        qs('finChequeDueDate').value = data.ChequeDueDate ? String(data.ChequeDueDate).substring(0, 10) : '';
        qs('finExpensesTypeId').value = data.ExpensesTypeId || '';
        qs('finGeneralDescription').value = data.GeneralDescription || data.Remarks || '';
        resolveCreditAccount();
        qs('finLinesBody').innerHTML = '';
        (data.Lines || []).forEach(addLine);
        if (!qs('finLinesBody').querySelector('tr')) addLine();
        updateTotal();
    }
    function resolveCreditAccount() {
        var payment = val('finPaymentType');
        var select = payment === '0' ? qs('finBoxId') : (payment === '2' ? qs('finVendorId') : qs('finBankId'));
        var opt = select && select.options[select.selectedIndex];
        qs('finCreditAccountCode').value = opt ? (opt.getAttribute('data-account') || '') : '';
    }
    function updateTotal() {
        var total = 0;
        Array.prototype.forEach.call(document.querySelectorAll('.line-value'), function (el) { total += Number(el.value || 0); });
        qs('finTotal').textContent = total.toFixed(2);
    }
    document.addEventListener('click', function (event) {
        var mode = event.target.closest('.financial-expenses-tabs button');
        if (mode) setMode(mode.getAttribute('data-mode'));
        if (event.target.closest('#finNewBtn')) clearForm();
        if (event.target.closest('#finAddLineBtn')) addLine();
        var remove = event.target.closest('.js-remove-fin-line');
        if (remove) { remove.closest('tr').remove(); if (!qs('finLinesBody').querySelector('tr')) addLine(); updateTotal(); }
        var load = event.target.closest('.js-load-fin');
        if (load) get(page.getAttribute('data-details-url') + '?id=' + encodeURIComponent(load.getAttribute('data-id')), function (r) { r.success ? (populate(r.data), show('تم تحميل المستند.', true)) : show(r.message || 'تعذر التحميل.', false); });
        if (event.target.closest('#finSaveBtn')) post(page.getAttribute('data-save-url'), collectPayload(), function (r) {
            show(r.Message || r.message || '', !!r.Success);
            if (r.Success) { qs('finId').value = r.Id; qs('finDocId').value = r.Id; qs('finDetailNoteId').value = r.DetailNoteId; qs('finJournalSerial').value = r.JournalSerial; qs('finVoucherSerial').value = r.VoucherSerial; }
        });
        if (event.target.closest('#finDeleteBtn')) {
            var id = intOrNull('finId');
            if (!id) { show('اختر المستند أولا.', false); return; }
            post(page.getAttribute('data-delete-url'), { id: id }, function (r) { show(r.Message || r.message || '', !!r.Success); if (r.Success) clearForm(); });
        }
    });
    document.addEventListener('change', function (event) {
        if (event.target.id === 'finPaymentType' || event.target.id === 'finBoxId' || event.target.id === 'finBankId' || event.target.id === 'finVendorId') resolveCreditAccount();
        if (event.target.id === 'finExpensesTypeId') {
            var opt = event.target.options[event.target.selectedIndex];
            var account = opt ? opt.getAttribute('data-account') : '';
            if (account && qs('finLinesBody').querySelector('tr')) qs('finLinesBody').querySelector('tr .line-account').value = account;
        }
    });
    document.addEventListener('input', function (event) { if (event.target.classList.contains('line-value')) updateTotal(); });
    setMode(val('finMode'));
    addLine();
})();
