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

        var bell = document.getElementById('hmsBell');
        var bellCount = document.getElementById('hmsBellCount');
        var panel = document.getElementById('hmsNotificationPanel');

        function refreshUnreadCount() {
            if (!bellCount) {
                return;
            }
            fetch('/Notification/UnreadCount', { headers: { 'Accept': 'application/json' } })
                .then(function (response) { return response.ok ? response.json() : null; })
                .then(function (data) {
                    if (!data) {
                        return;
                    }
                    bellCount.textContent = data.count;
                    bellCount.hidden = !data.count;
                })
                .catch(function () { /* the bell simply stays without a count */ });
        }

        if (bell && panel) {
            bell.addEventListener('click', function (event) {
                event.stopPropagation();
                if (!panel.hidden) {
                    panel.hidden = true;
                    return;
                }
                fetch('/Notification/Panel', { headers: { 'Accept': 'text/html' } })
                    .then(function (response) { return response.ok ? response.text() : ''; })
                    .then(function (html) {
                        panel.innerHTML = html;
                        panel.hidden = false;
                        if (bellCount) {
                            bellCount.textContent = '0';
                            bellCount.hidden = true;
                        }
                    })
                    .catch(function () { /* leave the panel closed */ });
            });
            document.addEventListener('click', function (event) {
                if (!panel.hidden && !panel.contains(event.target)) {
                    panel.hidden = true;
                }
            });
            refreshUnreadCount();
        }

        var confirmDialog = document.getElementById('hmsConfirmDialog');
        if (confirmDialog) {
            var pendingForm = null;
            document.querySelectorAll('[data-confirm]').forEach(function (element) {
                element.addEventListener('click', function (event) {
                    var form = element.closest('form');
                    if (!form) {
                        return;
                    }
                    event.preventDefault();
                    pendingForm = form;
                    confirmDialog.hidden = false;
                });
            });
            confirmDialog.querySelector('[data-confirm-cancel]').addEventListener('click', function () {
                pendingForm = null;
                confirmDialog.hidden = true;
            });
            confirmDialog.querySelector('[data-confirm-ok]').addEventListener('click', function () {
                if (pendingForm) {
                    pendingForm.submit();
                }
                confirmDialog.hidden = true;
            });
        }
    });
})();
