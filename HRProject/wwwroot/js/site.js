
 
function showError(title, body) {
    Swal.fire(
        title,
        body,
        'error'
    );
}
function showSuccess(title, body) {
    Swal.fire(
        title,
        body,
        'success'
    );
}
function isNumber(value) {
    const conv = +value;
    if (conv) {
        return true;
    } else {
        return false;
    }
}


 
    function changep() {

        $('#myModal').modal('show');

}

function getname(code) {
    console.log(code);
   showLoading('Please wait....');
    $.ajax({
        url: '/Account/getname',
        type: "get",
        data: {code:code},
       dataType: 'json',
        error: function (jqXHR, textStatus, errorThrown) {
              showError("An error occured");
          
        },
        success: function (resp) {
             closeloading('getname');
            if (!resp.isok) {
               //howError("not found");
                $('#txtname').val('');
            } else {
                $('#txtname').val(resp.name);

            }


        }
    });
}


function dochange() {
    $('#myModal').modal('hide');
    showLoading('Please wait....');
    var t1 = $('#txtpass').val();
    var t2 = $('#txtnpass').val();
    var t3 = $('#txtcnpass').val();
    $.ajax({
        url: '/Account/Changeps',
        type: "get",
        data: {
            txt1:t1,   txt2:t2,   txt3:t3
        },
        dataType: 'json',
        error: err => {
            closeloading('change');
            showError("An error occured");
        },

        success: function (resp) {
            closeloading('change');
            if (!resp.isok) {
          
              
                showError(resp.msg);
                if (resp.reload) {
                    location.reload();
                }
            } else {
                
                showSuccess("Password Changed");
             
            }
           

        }
    });
}
function showLoading(str) {
    //var msg = ' Please wait ... ';
    //if (str) {
    //    msg = msg;
    //} else {
    //    msg = str;
    //}
    //Swal.fire({
    //    title: msg,
    //    allowOutsideClick: false,
    //    onBeforeOpen: () => {
    //        Swal.showLoading();
    //    }
    //});
   
    $('.spiner').removeClass('d-none');

      
}


function closeloading(t) {
    // alert(t);
    $('.spiner').addClass('d-none');
}