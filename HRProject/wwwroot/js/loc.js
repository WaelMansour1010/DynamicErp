const options = {
    enableHighAccuracy: true,
    timeout: 5000,
    maximumAge: 0
};
var orderId = '';
function successlog(pos) {
    const crd = pos.coords;
    let loc = `${crd.latitude},${crd.longitude}`;
    let time = crd.timeStamp;
    let accuracy = crd.accuracy;
    $.ajax({
        url: '/home/LocLogin',
        type: "get",
        data: { id: orderId, loc: loc, time: time, accuracy: accuracy },
        success: function (data) {
            loading(false);
            if (data.isok) {
                location.reload();
            } else {
                showError(data.msg);
            }
        },
        error: () => {
            loading(false);
            showError('Error');
        }
    });
}
function successlogout(pos) {
    const crd = pos.coords;
    let loc = `${crd.latitude},${crd.longitude}`;
    let time = crd.timeStamp;
    let accuracy = crd.accuracy;
    $.ajax({
        url: '/home/LocLogout',
        type: "get",
        data: { id: orderId, loc: loc, time: time, accuracy: accuracy },
        success: function (data) {
            loading(false);
            if (data.isok) {
                location.reload();
            } else {
                showError(data.msg);
            }
            
        },
        error:() => {
            loading(false);
            showError('Error');
        }
    });
    
}
function error(err) {
    loading(false);
    showError(`ERROR(${err.code}): ${err.message}`);
}


//function getPosition() {
     
//    return new Promise((res, rej) => {
//        navigator.geolocation.getCurrentPosition(res, rej);
//    });
//}

 
function loading(show) {
    if (show) {
        $('#ibox1').children('.ibox-content').addClass('sk-loading');
    } else {
        $('#ibox1').children('.ibox-content').removeClass('sk-loading');
    }
}
function addnew() {
    let id = $('#txtId').val();
    let orderid = $('#txtviId').val();
    let isnew = $('#txtisnew').val();
    let name = $('#txtVisitName').val();
    let date = $('#txtVisitDate').val();
    let time = $('#txttime0').val();
    let time1 = $('#txttime1').val();
  
    let visitdone = $('#chkvisitdone').is(':checked');
    let canceled = $('#chkcanceled').is(':checked');
    let contracted = $('#chkcontracted').is(':checked');
    let type = $('#txttype').val();
    if (!id) {
        showError('select customer');
        return;
    }
    let notes = $('#txtnotes').val();
    if (!notes) {
        showError('enter notes');
        return;
    }
    console.log(type);

    // Name = name , 
    //VisitDate = date,
    //    VisitFromTime = fromtime,
    //    VisitToTime = totime
    let url = isnew == '1' ? '/home/NewVisit' : '/home/editVisit';
        $.ajax({
            url: url,
            type: "get",
            data: {
                id: orderid,
                custid:id,
                notes: notes,
                name: name,
                date: date,
                fromtime: time,
                totime: time1,
                visitdone: visitdone,
                canceled: canceled,
                contracted: contracted,
                type:type
            },
            success: function(data) {
                location.reload();
            }
        });
    

    

}
async function locLog(id) {
    loading(true);
    orderId = id;
    navigator.geolocation.getCurrentPosition(successlog, error, options);
}
async function loclogOut(id) {
    loading(true);
    orderId = id;
    navigator.geolocation.getCurrentPosition(successlogout, error, options);
}
