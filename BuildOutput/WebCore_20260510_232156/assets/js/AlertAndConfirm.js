$(document).ready(function () {

    window.alert = function (e) {
        ShowAlert(e)
    }
   
})




Swal.fire({
    title: '@MyERP.Properties.Resources.AreYouSureYouWantToDelete',
    text: " ",
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: '@MyERP.Properties.Resources.Ok',
    cancelButtonText: '@MyERP.Properties.Resources.Cancel',
    confirmButtonColor: '#3085d6',
    cancelButtonColor: '#d33',
    reverseButtons: true
}).then((result) => {
    if (result.isConfirmed) { }
    else { result.dismiss;}
});

Swal.fire({
    title: '@MyERP.Properties.Resources.AreYouSureYouWantToSave',
    text: " ",
    icon: 'info',
    showCancelButton: true,
    confirmButtonText: '@MyERP.Properties.Resources.Ok',
    cancelButtonText: '@MyERP.Properties.Resources.Cancel',
    confirmButtonColor: '#3085d6',
    cancelButtonColor: '#d33',
    reverseButtons: true
}).then((result) => {
    if (result.isConfirmed) { }
    else { result.dismiss; }
});



function ShowAlert(msg) {
    Swal.fire({
            title: '@MyERP.Properties.Resources.',
            text: " ",
            icon: 'success',
            showCancelButton: false,
            confirmButtonText: '@MyERP.Properties.Resources.Ok',
            confirmButtonColor: '#3085d6'
        });

    Swal.fire({
        title: '@MyERP.Properties.Resources.',
        text: " ",
        icon: 'info',
        showCancelButton: false,
        confirmButtonText: '@MyERP.Properties.Resources.Ok',
        confirmButtonColor: '#3085d6'
    });

    Swal.fire({
        title: '@MyERP.Properties.Resources.',
        text: " ",
        icon: 'error',
        showCancelButton: false,
        confirmButtonText: '@MyERP.Properties.Resources.Ok',
        confirmButtonColor: '#3085d6'
    });
}

Swal.fire({
    title: '@MyERP.Properties.Resources.DeleteDoesNotSucceeded',
    text: " ",
    icon: 'error',
    showCancelButton: false,
    confirmButtonText: '@MyERP.Properties.Resources.Ok',
    confirmButtonColor: '#3085d6'
});

Swal.fire({
    title: '@MyERP.Properties.Resources.Saved',
    text: " ",
    icon: 'success',
    showCancelButton: false,
    confirmButtonText: '@MyERP.Properties.Resources.Ok',
    confirmButtonColor: '#3085d6'
});
Swal.fire({
    title: '@MyERP.Properties.Resources.NotSaved !',
    text: " ",
    icon: 'error',
    showCancelButton: false,
    confirmButtonText: '@MyERP.Properties.Resources.Ok',
    confirmButtonColor: '#3085d6'
});
var r = true
function ShowConfirmWithCancel(msg) {
    Swal.fire({
        title: '@MyERP.Properties.Resources.AreYouSure',
        text: " ",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '@MyERP.Properties.Resources.Ok',
        cancelButtonText: '@MyERP.Properties.Resources.Cancel',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            // console.log(result, "result")
            if (r === true) {
                Swal.fire(
                    'Deleted!',
                    'Your file has been deleted.',
                    'success'
                )
            }
            else {
                Swal.fire(
                    'Not Deleted!',
                    'Your file has been Not deleted.',
                    'error'
                )
            }

        }
        else if (
            /* Read more about handling dismissals below */
            result.dismiss === Swal.DismissReason.cancel
        ) {
            Swal.fire(
                'Cancelled',
                'Your imaginary file is safe :)',
                'error'
            )
        }
    })
}

//function ShowModal() {
//    Swal.fire({
//        title: 'Are you sure?',
//        text: "You won't be able to revert this!",
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonColor: '#3085d6',
//        cancelButtonColor: '#d33',
//        confirmButtonText: 'Yes, delete it!',
//        cancelButtonText: 'No, cancel!',
//    }).then((result) => {
//        if (result.isConfirmed) {
//            x();
//            Swal.fire(
//                'Deleted!',
//                'Your file has been deleted.',
//                'success'
//            )
//        }
//    })
//}

//function x() {
//    console.log('A')
//}
