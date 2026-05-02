// Get data from barcode
function getDataFromBarcode(barcode, tr, getZeroQunatity) {
    let itemBarcode = barcode.split("-*-")[1];
    let expireDate = barcode.split("-*-")[0];

    let itemPatches;
    var selectedPatch;
    var selectedPatchId;
    var selectedPatchQty;

    let itemData = {};
    let item = items.filter(function (i) { return i.Barcode === itemBarcode; })[0];
    //console.log("item ", item)
    if (item.Id !== "" || item.Id !== null || item.Id !== undefined) {
        if (expireDate !== "" || expireDate !== null || expireDate !== undefined) {
            $.get('/Helper/PatchesByItemId?itemId=' + item.Id + '&getZeroQunatity=' + getZeroQunatity).done(function (r) {
                itemPatches = r;

                console.log("itemPatches ", itemPatches)
                selectedPatch = r.length !== 0 ? itemPatches.filter(function (i) { return convertToJavaScriptDate(i.ExpiryDate) === expireDate; })[0] : null;
                if (selectedPatch) {
                    selectedPatchId = selectedPatch.PatchId;
                    selectedPatchQty = selectedPatch.Qty;

                    itemData = {
                        itemBarcode: itemBarcode,
                        expireDate: expireDate,
                        selectedPatchId: selectedPatchId,
                        selectedPatchQty: selectedPatchQty
                    }

                    //return itemData;
                }
                //console.log("selectedPatch in getDataFromBarcode ", selectedPatch)

            }).done(function () {
                chooseItemPatch(tr, itemData);
            });
        }
    }
}

function chooseItemPatch(tr, itemData, getZeroQunatity) {
    let patchId = itemData.selectedPatchId;
    let expireDate = itemData.expireDate;
    let Qty = itemData.selectedPatchQty;
    tr.find('.PatchId').val(patchId);
    tr.find('.ExpiryDate').val(expireDate);

    // quantity of patch
    tr.find('.quantityOnSystem').val(Qty);

    if (patchId !== null && patchId !== undefined) {
        if (getZeroQunatity) {
            if (Qty === null || Qty === undefined || Qty === 0)
                alert("لا توجد كمية من هذة الشحنة");
        }

    }
    else {
        alert("لا توجد هذة الشحنة");
    }


    // Default Quantity In Pos
    //tr.find('.quantity').val('@ViewBag.DefaultQuantityInPos');
    tr.find('.quantity').trigger('change');
}