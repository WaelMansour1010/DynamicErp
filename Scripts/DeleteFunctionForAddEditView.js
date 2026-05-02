function Delete(id,controllerName) {
    if (confirm('هل تريد الحذف؟')) {

        $('#pleasewait').fadeIn();
        $.ajax(
            {

                url: '/'+controllerName+'/Delete',
                type: 'POST',
                data: {
                    "Id": id
                },
                error: function (e) {
                    
                },
                success: function (r) {
                    if (r == 'true') {

                        window.location.replace("/" + controllerName + "/Index");
                    }
                }
            });
    }
}