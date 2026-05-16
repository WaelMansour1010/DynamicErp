(function () {
    var page = document.getElementById('stocktakingPage');
    if (!page) return;

    function qs(id) { return document.getElementById(id); }
    function val(id) { var el = qs(id); return el ? el.value : ''; }
    function num(selector, row) { var el = row.querySelector(selector); return el && el.value !== '' ? Number(el.value) : 0; }
    function intOrNull(id) { var v = val(id); return v === '' ? null : parseInt(v, 10); }
    function dateOrNull(id) { var v = val(id); return v || null; }
    function checked(id) { var el = qs(id); return !!(el && el.checked); }
    var itemLookupUrl = page.getAttribute('data-lookup-items-url');
    var itemLookupTimer = null;
    var itemLookupCache = {};
    var itemLookupByValue = {};

    function show(message, success) {
        var status = qs('gardStatus');
        status.textContent = message || '';
        status.hidden = false;
        status.className = 'stocktaking-status ' + (success ? 'is-success' : 'is-error');
    }

    function post(url, payload, done) {
        var xhr = new XMLHttpRequest();
        xhr.open('POST', url, true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status >= 200 && xhr.status < 300) {
                done(JSON.parse(xhr.responseText || '{}'));
            } else {
                show('تعذر الاتصال بالخادم.', false);
            }
        };
        xhr.send(JSON.stringify(payload));
    }

    function get(url, done) {
        var xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status >= 200 && xhr.status < 300) {
                done(JSON.parse(xhr.responseText || '{}'));
            } else {
                show('تعذر تحميل البيانات.', false);
            }
        };
        xhr.send();
    }

    function itemDisplay(item) {
        return [item.Code, item.Text].filter(Boolean).join(' - ');
    }

    function rememberItem(item) {
        if (!item) return;
        var display = itemDisplay(item);
        itemLookupByValue[display] = item;
    }

    function seedItemLookupFromDatalist() {
        var list = qs('gardItemLookupList');
        if (!list) return;
        Array.prototype.forEach.call(list.querySelectorAll('option'), function (option) {
            var id = option.getAttribute('data-id');
            if (!id) return;
            itemLookupByValue[option.value] = {
                Id: id,
                Text: option.value,
                Code: '',
                Price: option.getAttribute('data-price') || 0,
                DefaultUnitId: option.getAttribute('data-unit') || '',
                DefaultUnitName: option.getAttribute('data-unit-name') || ''
            };
        });
    }

    function renderItemDatalist(items) {
        var list = qs('gardItemLookupList');
        if (!list) return;
        list.innerHTML = '';
        (items || []).forEach(function (item) {
            rememberItem(item);
            var option = document.createElement('option');
            option.value = itemDisplay(item);
            option.setAttribute('data-id', item.Id || '');
            option.setAttribute('data-price', item.Price || 0);
            option.setAttribute('data-unit', item.DefaultUnitId || '');
            option.setAttribute('data-unit-name', item.DefaultUnitName || '');
            list.appendChild(option);
        });
    }

    function lookupItems(term) {
        term = (term || '').trim();
        if (!itemLookupUrl || term.length < 2) return;
        if (itemLookupCache[term]) {
            renderItemDatalist(itemLookupCache[term]);
            return;
        }

        get(itemLookupUrl + '?term=' + encodeURIComponent(term), function (response) {
            itemLookupCache[term] = response.items || [];
            renderItemDatalist(itemLookupCache[term]);
        });
    }

    function applySelectedItem(input) {
        var row = input.closest('tr');
        var item = itemLookupByValue[input.value];
        if (!row || !item) return false;
        row.querySelector('.line-item').value = item.Id || '';
        row.querySelector('.line-price').value = item.Price || 0;
        row.querySelector('.line-unit').value = item.DefaultUnitId || '';
        updateTotals();
        return true;
    }

    function setMode(mode) {
        mode = mode === 'FrmNewGard1' ? 'FrmNewGard1' : 'FrmNewGard';
        qs('gardMode').value = mode;
        Array.prototype.forEach.call(document.querySelectorAll('.stocktaking-tabs button'), function (btn) {
            btn.classList.toggle('active', btn.getAttribute('data-mode') === mode);
        });
        page.classList.toggle('is-gard1', mode === 'FrmNewGard1');
    }

    function addLine(data) {
        var body = qs('gardLinesBody');
        body.insertAdjacentHTML('beforeend', qs('gardLineTemplate').innerHTML);
        var row = body.querySelector('tr:last-child');
        data = data || {};
        row.querySelector('.line-item').value = data.ItemId || '';
        row.querySelector('.line-item-search').value = data.ItemCode || data.ItemName ? ((data.ItemCode || '') + (data.ItemCode && data.ItemName ? ' - ' : '') + (data.ItemName || '')) : '';
        row.querySelector('.line-unit').value = data.UnitId || '';
        row.querySelector('.line-count').value = data.Count != null ? data.Count : 1;
        row.querySelector('.line-price').value = data.Price != null ? data.Price : 0;
        row.querySelector('.line-serial').value = data.Serial || '';
        row.querySelector('.line-lot').value = data.LotNo || '';
        row.querySelector('.line-gard-qty').value = data.GardQty != null ? data.GardQty : '';
        row.querySelector('.line-gard-result').value = data.GardResult != null ? data.GardResult : '';
        row.querySelector('.line-gard-result1').value = data.GardResult1 != null ? data.GardResult1 : '';
        row.querySelector('.line-gard-result2').value = data.GardResult2 != null ? data.GardResult2 : '';
        updateTotals();
    }

    function clearForm() {
        qs('gardId').value = '';
        qs('gardTransactionId').value = '';
        qs('gardSerial').value = '';
        qs('gardStoreId').value = '';
        qs('gardBranchId').value = '';
        qs('gardEntryType').value = '2';
        qs('gardStartGard').checked = false;
        qs('gardStartSettlement').checked = false;
        qs('gardAutoDetect').checked = false;
        qs('gardAccount1').value = '';
        qs('gardAccount2').value = '';
        qs('gardNotS').value = '';
        qs('gardNotS2').value = '';
        qs('gardLinesBody').innerHTML = '';
        addLine();
        show('جاهز لإدخال جرد جديد.', true);
    }

    function collectLines() {
        return Array.prototype.map.call(document.querySelectorAll('#gardLinesBody tr'), function (row) {
            return {
                ItemId: row.querySelector('.line-item').value ? parseInt(row.querySelector('.line-item').value, 10) : null,
                UnitId: row.querySelector('.line-unit').value ? parseInt(row.querySelector('.line-unit').value, 10) : null,
                Count: num('.line-count', row),
                Price: num('.line-price', row),
                Serial: row.querySelector('.line-serial').value,
                LotNo: row.querySelector('.line-lot').value,
                ItemCase: 0,
                ColorId: 1,
                ItemSize: '1',
                ClassId: 1,
                GardQty: row.querySelector('.line-gard-qty').value === '' ? null : num('.line-gard-qty', row),
                GardResult: row.querySelector('.line-gard-result').value === '' ? null : num('.line-gard-result', row),
                GardResult1: row.querySelector('.line-gard-result1').value === '' ? null : num('.line-gard-result1', row),
                GardResult2: row.querySelector('.line-gard-result2').value === '' ? null : num('.line-gard-result2', row),
                AutoDetect: 0
            };
        });
    }

    function collectPayload() {
        return {
            Id: intOrNull('gardId'),
            Serial: val('gardSerial'),
            Date: dateOrNull('gardDate'),
            FromDate: dateOrNull('gardFromDate'),
            ToDate: dateOrNull('gardToDate'),
            StoreId: intOrNull('gardStoreId'),
            BranchId: intOrNull('gardBranchId'),
            GardEntryType: parseInt(val('gardEntryType') || '2', 10),
            StartGard: checked('gardStartGard'),
            StartSettlement: checked('gardStartSettlement'),
            AutoDetect: checked('gardAutoDetect'),
            Account1: val('gardAccount1'),
            Account2: val('gardAccount2'),
            Mode: val('gardMode'),
            Lines: collectLines()
        };
    }

    function populate(data) {
        qs('gardId').value = data.Id || '';
        qs('gardTransactionId').value = data.Id || '';
        qs('gardSerial').value = data.Serial || '';
        qs('gardDate').value = toDateInput(data.Date);
        qs('gardFromDate').value = toDateInput(data.FromDate);
        qs('gardToDate').value = toDateInput(data.ToDate);
        qs('gardStoreId').value = data.StoreId || '';
        qs('gardBranchId').value = data.BranchId || '';
        qs('gardEntryType').value = data.GardEntryType != null ? data.GardEntryType : 2;
        qs('gardStartGard').checked = !!data.StartGard;
        qs('gardStartSettlement').checked = !!data.StartSettlement;
        qs('gardAutoDetect').checked = !!data.AutoDetect;
        qs('gardAccount1').value = data.Account1 || '';
        qs('gardAccount2').value = data.Account2 || '';
        qs('gardNotS').value = data.NotS || '';
        qs('gardNotS2').value = data.NotS2 || '';
        qs('gardLinesBody').innerHTML = '';
        (data.Lines || []).forEach(addLine);
        if (!qs('gardLinesBody').querySelector('tr')) addLine();
        updateTotals();
    }

    function toDateInput(value) {
        if (!value) return '';
        return String(value).substring(0, 10);
    }

    function updateTotals() {
        var qty = 0;
        var value = 0;
        Array.prototype.forEach.call(document.querySelectorAll('#gardLinesBody tr'), function (row) {
            var count = num('.line-count', row);
            var price = num('.line-price', row);
            qty += count;
            value += count * price;
        });
        qs('gardQtyTotal').textContent = qty.toFixed(2);
        qs('gardValueTotal').textContent = value.toFixed(2);
    }

    document.addEventListener('click', function (event) {
        var modeBtn = event.target.closest('.stocktaking-tabs button');
        if (modeBtn) setMode(modeBtn.getAttribute('data-mode'));

        if (event.target.closest('#gardNewBtn')) clearForm();
        if (event.target.closest('#gardAddLineBtn')) addLine();

        var remove = event.target.closest('.js-remove-line');
        if (remove) {
            remove.closest('tr').remove();
            if (!qs('gardLinesBody').querySelector('tr')) addLine();
            updateTotals();
        }

        var load = event.target.closest('.js-load-gard');
        if (load) {
            get(page.getAttribute('data-details-url') + '?id=' + encodeURIComponent(load.getAttribute('data-id')), function (response) {
                if (response.success) {
                    populate(response.data);
                    show('تم تحميل مستند الجرد.', true);
                } else {
                    show(response.message || 'تعذر تحميل الجرد.', false);
                }
            });
        }

        if (event.target.closest('#gardSaveBtn')) {
            post(page.getAttribute('data-save-url'), collectPayload(), function (response) {
                show(response.Message || response.message || '', !!response.Success);
                if (response.Success) {
                    qs('gardId').value = response.Id || '';
                    qs('gardTransactionId').value = response.Id || '';
                    qs('gardSerial').value = response.Serial || '';
                }
            });
        }

        if (event.target.closest('#gardDeleteBtn')) {
            var id = val('gardId');
            if (!id) {
                show('اختر مستند الجرد أولا.', false);
                return;
            }
            post(page.getAttribute('data-delete-url'), { id: parseInt(id, 10) }, function (response) {
                show(response.Message || response.message || '', !!response.Success);
                if (response.Success) clearForm();
            });
        }
    });

    document.addEventListener('change', function (event) {
        var itemInput = event.target.closest('.line-item-search');
        if (itemInput) {
            applySelectedItem(itemInput);
        }
    });

    document.addEventListener('input', function (event) {
        if (event.target.closest('.line-count') || event.target.closest('.line-price')) updateTotals();
        var itemInput = event.target.closest('.line-item-search');
        if (itemInput) {
            if (applySelectedItem(itemInput)) return;
            var row = itemInput.closest('tr');
            if (row) {
                row.querySelector('.line-item').value = '';
            }
            clearTimeout(itemLookupTimer);
            itemLookupTimer = setTimeout(function () { lookupItems(itemInput.value); }, 250);
        }
    });

    seedItemLookupFromDatalist();
    setMode(val('gardMode'));
    addLine();
})();
