(function () {
    var page = document.getElementById('itemsPage');
    if (!page) return;

    function qs(id) { return document.getElementById(id); }
    function value(id) { var el = qs(id); return el ? el.value : ''; }
    function number(id) { var raw = value(id); return raw === '' ? 0 : Number(raw); }
    function intOrNull(id) { var raw = value(id); return raw === '' ? null : parseInt(raw, 10); }
    function checked(id) { var el = qs(id); return !!(el && el.checked); }

    function showStatus(message, success) {
        var status = qs('itemStatus');
        status.textContent = message || '';
        status.hidden = false;
        status.className = 'items-status ' + (success ? 'is-success' : 'is-error');
    }

    function setChecked(id, isChecked) {
        var el = qs(id);
        if (el) el.checked = !!isChecked;
    }

    function clearForm() {
        [
            'itemId', 'itemCode', 'itemName', 'itemNameEn', 'itemBarcode', 'partNo', 'catalogNo',
            'factoryNo', 'purchasePrice', 'salePrice', 'customerPrice', 'dealerPrice', 'costPrice',
            'requestLimit', 'minQty', 'maxQty', 'shortName', 'binLocation', 'guaranteeValue', 'itemNotes'
        ].forEach(function (id) {
            var el = qs(id);
            if (el) el.value = '';
        });
        qs('itemType').value = '';
        qs('itemGroupId').value = '';
        qs('guaranteeType').value = '';
        qs('haveSerial').checked = false;
        qs('haveGuarantee').checked = false;
        qs('itemUnitsBody').innerHTML = '';
        addUnitRow(true);
        showStatus('جاهز لإدخال صنف جديد.', true);
    }

    function addUnitRow(makeDefault) {
        var body = qs('itemUnitsBody');
        var template = qs('unitRowTemplate');
        body.insertAdjacentHTML('beforeend', template.innerHTML);
        if (makeDefault || body.querySelectorAll('tr').length === 1) {
            body.querySelector('tr:last-child .unit-default').checked = true;
        }
    }

    function collectUnits() {
        return Array.prototype.map.call(document.querySelectorAll('#itemUnitsBody tr'), function (row) {
            return {
                UnitId: row.querySelector('.unit-id').value ? parseInt(row.querySelector('.unit-id').value, 10) : null,
                UnitFactor: Number(row.querySelector('.unit-factor').value || 0),
                SalePrice: Number(row.querySelector('.unit-sale').value || 0),
                PurchasePrice: Number(row.querySelector('.unit-purchase').value || 0),
                MinSellingPrice: Number(row.querySelector('.unit-min-sale').value || 0),
                MaxSellingPrice: 0,
                WholeSalePrice: 0,
                IsDefault: row.querySelector('.unit-default').checked,
                Barcode: row.querySelector('.unit-barcode').value
            };
        });
    }

    function collectPayload() {
        return {
            Id: intOrNull('itemId'),
            Code: value('itemCode'),
            Name: value('itemName'),
            NameEn: value('itemNameEn'),
            GroupId: intOrNull('itemGroupId'),
            ItemType: intOrNull('itemType'),
            PartNo: value('partNo'),
            Barcode: value('itemBarcode'),
            CatalogNo: value('catalogNo'),
            FactoryNo: value('factoryNo'),
            PurchasePrice: number('purchasePrice'),
            SalePrice: number('salePrice'),
            CustomerPrice: number('customerPrice'),
            DealerPrice: number('dealerPrice'),
            CostPrice: number('costPrice'),
            MinQty: number('minQty'),
            MaxQty: number('maxQty'),
            RequestLimit: intOrNull('requestLimit'),
            HaveSerial: checked('haveSerial'),
            HaveGuarantee: checked('haveGuarantee'),
            GuaranteeValue: intOrNull('guaranteeValue'),
            GuaranteeType: intOrNull('guaranteeType'),
            IsArchived: false,
            ShortName: value('shortName'),
            BinLocation: value('binLocation'),
            Notes: value('itemNotes'),
            Units: collectUnits()
        };
    }

    function populate(data) {
        qs('itemId').value = data.Id || 0;
        qs('itemCode').value = data.Code || '';
        qs('itemName').value = data.Name || '';
        qs('itemNameEn').value = data.NameEn || '';
        qs('itemGroupId').value = data.GroupId || '';
        qs('itemType').value = data.ItemType || '';
        qs('partNo').value = data.PartNo || '';
        qs('itemBarcode').value = data.Barcode || '';
        qs('catalogNo').value = data.CatalogNo || '';
        qs('factoryNo').value = data.FactoryNo || '';
        qs('purchasePrice').value = data.PurchasePrice || 0;
        qs('salePrice').value = data.SalePrice || 0;
        qs('customerPrice').value = data.CustomerPrice || 0;
        qs('dealerPrice').value = data.DealerPrice || 0;
        qs('costPrice').value = data.CostPrice || 0;
        qs('minQty').value = data.MinQty || 0;
        qs('maxQty').value = data.MaxQty || 0;
        qs('requestLimit').value = data.RequestLimit || '';
        qs('haveSerial').checked = !!data.HaveSerial;
        qs('haveGuarantee').checked = !!data.HaveGuarantee;
        qs('guaranteeValue').value = data.GuaranteeValue || '';
        qs('guaranteeType').value = data.GuaranteeType || '';
        qs('shortName').value = data.ShortName || '';
        qs('binLocation').value = data.BinLocation || '';
        qs('itemNotes').value = data.Notes || '';
        qs('itemUnitsBody').innerHTML = '';
        (data.Units || []).forEach(function (unit) {
            addUnitRow(unit.IsDefault);
            var row = qs('itemUnitsBody').querySelector('tr:last-child');
            row.querySelector('.unit-id').value = unit.UnitId || '';
            row.querySelector('.unit-factor').value = unit.UnitFactor || 1;
            row.querySelector('.unit-sale').value = unit.SalePrice || 0;
            row.querySelector('.unit-purchase').value = unit.PurchasePrice || 0;
            row.querySelector('.unit-min-sale').value = unit.MinSellingPrice || 0;
            row.querySelector('.unit-barcode').value = unit.Barcode || '';
        });
        if (!qs('itemUnitsBody').querySelector('tr')) addUnitRow(true);
    }

    function postJson(url, payload, done) {
        var xhr = new XMLHttpRequest();
        xhr.open('POST', url, true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.onreadystatechange = function () {
            if (xhr.readyState !== 4) return;
            if (xhr.status >= 200 && xhr.status < 300) {
                done(JSON.parse(xhr.responseText || '{}'));
            } else {
                showStatus('تعذر الاتصال بالخادم.', false);
            }
        };
        xhr.send(JSON.stringify(payload));
    }

    function clearGroupForm() {
        qs('groupId').value = '';
        qs('groupCode').value = '';
        qs('groupName').value = '';
        qs('groupNameEn').value = '';
        qs('groupFullCode').value = '';
        qs('groupParentId').value = '1';
        [
            'groupIsLast', 'groupIsMaterial', 'groupPos', 'groupProducer', 'groupAdditions',
            'groupHoldingMaterials', 'groupSeparate', 'groupTransfer', 'groupShowCover', 'groupTaxExempt'
        ].forEach(function (id) { setChecked(id, false); });
        showStatus('جاهز لإدخال مجموعة جديدة.', true);
    }

    function collectGroupPayload() {
        return {
            Id: intOrNull('groupId'),
            ParentId: intOrNull('groupParentId'),
            Code: value('groupCode'),
            FullCode: value('groupFullCode'),
            Name: value('groupName'),
            NameEn: value('groupNameEn'),
            IsLastGroup: checked('groupIsLast'),
            IsMaterial: checked('groupIsMaterial'),
            IsPosGroup: checked('groupPos'),
            IsProducer: checked('groupProducer'),
            IsAdditions: checked('groupAdditions'),
            HoldingMaterials: checked('groupHoldingMaterials'),
            Separate: checked('groupSeparate'),
            IsTransfere: checked('groupTransfer'),
            IsShowCover: checked('groupShowCover'),
            TaxExempt: checked('groupTaxExempt')
        };
    }

    function populateGroup(data) {
        qs('groupId').value = data.Id || '';
        qs('groupCode').value = data.Code || '';
        qs('groupName').value = data.Name || '';
        qs('groupNameEn').value = data.NameEn || '';
        qs('groupFullCode').value = data.FullCode || '';
        qs('groupParentId').value = data.ParentId || 1;
        setChecked('groupIsLast', data.IsLastGroup);
        setChecked('groupIsMaterial', data.IsMaterial);
        setChecked('groupPos', data.IsPosGroup);
        setChecked('groupProducer', data.IsProducer);
        setChecked('groupAdditions', data.IsAdditions);
        setChecked('groupHoldingMaterials', data.HoldingMaterials);
        setChecked('groupSeparate', data.Separate);
        setChecked('groupTransfer', data.IsTransfere);
        setChecked('groupShowCover', data.IsShowCover);
        setChecked('groupTaxExempt', data.TaxExempt);
    }

    document.addEventListener('click', function (event) {
        var loadBtn = event.target.closest('.js-load-item');
        if (loadBtn) {
            var xhr = new XMLHttpRequest();
            xhr.open('GET', page.getAttribute('data-details-url') + '?id=' + encodeURIComponent(loadBtn.getAttribute('data-id')), true);
            xhr.onreadystatechange = function () {
                if (xhr.readyState !== 4) return;
                var response = JSON.parse(xhr.responseText || '{}');
                if (response.success) {
                    populate(response.data);
                    showStatus('تم تحميل الصنف.', true);
                } else {
                    showStatus(response.message || 'تعذر تحميل الصنف.', false);
                }
            };
            xhr.send();
        }

        if (event.target.closest('#addUnitBtn')) addUnitRow(false);
        if (event.target.closest('.js-remove-unit')) {
            event.target.closest('tr').remove();
            if (!qs('itemUnitsBody').querySelector('tr')) addUnitRow(true);
        }
        if (event.target.closest('#itemNewBtn')) clearForm();
        if (event.target.closest('#itemSaveBtn')) {
            postJson(page.getAttribute('data-save-url'), collectPayload(), function (response) {
                showStatus(response.Message || (response.Success ? 'تم الحفظ.' : 'لم يتم الحفظ.'), !!response.Success);
                if (response.Success) {
                    qs('itemId').value = response.Id;
                    qs('itemCode').value = response.Code || qs('itemCode').value;
                }
            });
        }
        if (event.target.closest('#itemDeleteBtn')) {
            var id = intOrNull('itemId');
            if (!id) { showStatus('اختر صنفا أولا.', false); return; }
            if (!confirm('تأكيد حذف الصنف؟')) return;
            postJson(page.getAttribute('data-delete-url'), { id: id }, function (response) {
                showStatus(response.Message || 'تم الحذف.', !!response.Success);
                if (response.Success) clearForm();
            });
        }
        var loadGroupBtn = event.target.closest('.js-load-group');
        if (loadGroupBtn) {
            var groupXhr = new XMLHttpRequest();
            groupXhr.open('GET', page.getAttribute('data-group-details-url') + '?id=' + encodeURIComponent(loadGroupBtn.getAttribute('data-id')), true);
            groupXhr.onreadystatechange = function () {
                if (groupXhr.readyState !== 4) return;
                var response = JSON.parse(groupXhr.responseText || '{}');
                if (response.success) {
                    populateGroup(response.data);
                    showStatus('تم تحميل المجموعة.', true);
                } else {
                    showStatus(response.message || 'تعذر تحميل المجموعة.', false);
                }
            };
            groupXhr.send();
        }
        if (event.target.closest('#groupNewBtn')) clearGroupForm();
        if (event.target.closest('#groupSaveBtn')) {
            postJson(page.getAttribute('data-group-save-url'), collectGroupPayload(), function (response) {
                showStatus(response.Message || (response.Success ? 'تم حفظ المجموعة.' : 'لم يتم حفظ المجموعة.'), !!response.Success);
                if (response.Success) {
                    qs('groupId').value = response.Id;
                    qs('groupCode').value = response.Code || qs('groupCode').value;
                }
            });
        }
        if (event.target.closest('#groupDeleteBtn')) {
            var groupId = intOrNull('groupId');
            if (!groupId) { showStatus('اختر مجموعة أولا.', false); return; }
            if (!confirm('تأكيد حذف المجموعة؟')) return;
            postJson(page.getAttribute('data-group-delete-url'), { id: groupId }, function (response) {
                showStatus(response.Message || 'تم حذف المجموعة.', !!response.Success);
                if (response.Success) clearGroupForm();
            });
        }
        var tab = event.target.closest('.items-tabs button');
        if (tab) {
            Array.prototype.forEach.call(document.querySelectorAll('.items-tabs button'), function (b) { b.classList.remove('active'); });
            tab.classList.add('active');
            document.querySelector(tab.getAttribute('data-target')).scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    });

    if (!qs('itemUnitsBody').querySelector('tr')) addUnitRow(true);
})();
