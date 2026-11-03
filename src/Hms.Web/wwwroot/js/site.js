// Healthcare Management System — shared page behaviour.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var shell = document.getElementById('hmsShell');
        var toggle = document.getElementById('hmsSidebarToggle');
        if (shell && toggle) {
            toggle.addEventListener('click', function () {
                shell.classList.toggle('hms-collapsed');
            });
        }

        var profileBtn = document.getElementById('hmsProfileBtn');
        var profileMenu = document.getElementById('hmsProfileMenu');
        if (profileBtn && profileMenu) {
            profileBtn.addEventListener('click', function (event) {
                event.stopPropagation();
                profileMenu.hidden = !profileMenu.hidden;
            });
            document.addEventListener('click', function () {
                profileMenu.hidden = true;
            });
        }

        // The notification bell and panel live in js/pages/notification.js.

        var confirmDialog = document.getElementById('hmsConfirmDialog');
        if (confirmDialog) {
            var pendingForm = null;
            var pendingUrl = null;
            document.querySelectorAll('[data-confirm]').forEach(function (element) {
                element.addEventListener('click', function (event) {
                    var form = element.closest('form');
                    var url = element.getAttribute('href');
                    if (!form && !url) {
                        return;
                    }
                    event.preventDefault();
                    pendingForm = form;
                    pendingUrl = form ? null : url;
                    confirmDialog.hidden = false;
                });
            });
            confirmDialog.querySelector('[data-confirm-cancel]').addEventListener('click', function () {
                pendingForm = null;
                pendingUrl = null;
                confirmDialog.hidden = true;
            });
            confirmDialog.querySelector('[data-confirm-ok]').addEventListener('click', function () {
                if (pendingForm) {
                    pendingForm.submit();
                } else if (pendingUrl) {
                    window.location.assign(pendingUrl);
                }
                pendingForm = null;
                pendingUrl = null;
                confirmDialog.hidden = true;
            });
        }
    });
})();
