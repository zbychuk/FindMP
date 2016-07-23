(function ($) {
    var MP = {}, MPEmail;
    var msg ='';
    $.get('/MPLetter.html').done(function(res) {
         msg = res;
    });
    
    
    function call(path, func, d, onSuccess, onFailure, context) {
        var def = $.Deferred();
        $.ajax({
            type: "POST",
            url: (path ? path : "/Home/") + func,
            cache: false,
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(d),
            dataType: "json",
            success: function (r) {
                //$.fixDate(r);
                if (onSuccess) onSuccess(r, context);
                def.resolve(r);
            },
            error: function (r) {
                if (onFailure) onFailure(r, context);
                def.reject(r);
            }
        });
        return def.promise();
    }
    function prepareLetter() {
        var m = msg.replace(/\{mpname\}/g, MP.FullTitle)
            .replace(/\{postcode\}/g, $('#postcode').val())
            .replace(/\{name\}/g, $('#yourname').val())
            .replace(/\{country\}/g, $('#country').val())
        .replace(/\{years\}/g, $('#howlong').val());
        $('#letter').html(m);
    }
    $('#prepareletter').click(function () {
        $('#letter').show();
        prepareLetter();
    });
    $('#sendletter')
        //""
        .click(function () {
            if (!$('#cbox1').prop('checked')) {
                alert("You have to accept our Terms and Conditions to continue;");
                return false;
            }
            prepareLetter();
            var postcode = $('#postcode').val();
            if (postcode.length < 3 || MP.length < 5) {
                alert("You have to specify your postcode");
                return false;
            }
            var email = $('#email').val();
            if (email.length < 3 ) {
                alert("You have to specify your email");
                return false;
            }
            var name = $('#yourname').val();
            if (name.length < 3) {
                alert("You have to specify your name");
                return false;
            }
            call('', 'sendEmail',
                {
                    data: {
                        Subject: 'The rights of EU citizens living in Britain',
                        Body: $('#letter').html(), Postcode: postcode,
                        Country: $('#country').val(), MP: MP.DisplayAs,
                        MPEmail: MPEmail, EMail: email,
                        Name: name
                    }
                    
                })
            .done(function(r) {
                    if (r && r.success) {
                        alert('You will receive an email from us soon. You have to click on confirm in this email');
                        window.location.href = 'http://immigrants.help';
                    } else {
                        alert('There was an error while sending. Please try again.');
                    }
                });
        });
    $('#postcode')
        .on('change',
            function () {
                $('#mptooshort').hide();
                function hide() {
                    $('#mpnamediv').hide();
                    $('#prepareletter').hide();
                }
                function show(d) {
                    MP = d.Members.Member;
                    var mpName = MP.DisplayAs;
                    $('#mptooshort').hide();
                    $('#mpname').text(mpName);
                    $('#sendletter').text('Send letter to ' + mpName).show();
                    var ad = MP.Addresses.Address.length ? MP.Addresses.Address : [MP.Addresses.Address];
                    MPEmail = '';
                    for (var i = 0; i < ad.length; i++) {
                        if (ad[i]["@Type_Id"] == 1 && ad[i].Email) {
                            MPEmail = ad[i].Email;
                            break;
                        }
                    }
                    if (!MPEmail) {
                        for (var i = 0; i < ad.length; i++) {
                            if (ad[i].Email) {
                                MPEmail = ad[i].Email;
                                break;
                            }
                        }
                    }
                    $('#mpemail').text(MPEmail);
                    $('#prepareletter').show();
                    $('#mpnamediv').show();
                }

                call("", "FindMP", { postcode: $('#postcode').val() })
                    .then(function (res) {
                        var d = (new Function('return ' + res.data))();
                        if (!d.Members || !d.Members.Member) {
                            hide();
                        } else
                        if (!d.Members.Member.length) {
                            show(d);
                        } else {
                            hide();
                            if ($('#postcode').val().length < 6) {
                                $('#mptooshort').show();
                            }
                        }
                    });
            });
    $('#yourname').change(prepareLetter);
    $('#country').change(prepareLetter);
    $('#howlong').change(prepareLetter);
    
})(jQuery);