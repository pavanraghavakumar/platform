// Sign-in, forgot and reset password pages.
// Keeps the typed user name when the sign-in page comes back with an error,
// and focuses the first field that needs attention.
(function () {
    'use strict';

    var card = document.getElementById('hmsAuthCard');
    if (!card) {
        return;
    }

    var error = card.querySelector('.hms-alert-error');
    var userName = card.querySelector('#UserName');
    var password = card.querySelector('#Password');

    if (error && userName) {
        // The browser may have cleared the field on a failed post; keep the name the person typed.
        userName.focus();
        if (password) {
            password.value = '';
        }
    }
})();
