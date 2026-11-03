// Healthcare Management System — notification bell and panel.
// The bell shows the unread count from GET /Notification/UnreadCount; clicking it opens
// the panel from GET /Notification/Panel, which marks the notifications as read.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var bell = document.getElementById('hmsBell');
        var bellCount = document.getElementById('hmsBellCount');
        var panel = document.getElementById('hmsNotificationPanel');
        if (!bell || !bellCount || !panel) {
            return;
        }

        var countVersion = 0;

        function setCount(count) {
            var value = Number(count) || 0;
            bellCount.textContent = String(value);
            bellCount.hidden = value <= 0;
        }

        function refreshUnreadCount() {
            var requestVersion = countVersion;
            fetch('/Notification/UnreadCount', { headers: { 'Accept': 'application/json' } })
                .then(function (response) { return response.ok ? response.json() : null; })
                .then(function (data) {
                    if (data && requestVersion === countVersion) {
                        setCount(data.count);
                    }
                })
                .catch(function () { /* the bell simply stays without a count */ });
        }

        function closePanel() {
            panel.hidden = true;
            bell.setAttribute('aria-expanded', 'false');
        }

        function openPanel() {
            fetch('/Notification/Panel', { headers: { 'Accept': 'text/html' } })
                .then(function (response) { return response.ok ? response.text() : ''; })
                .then(function (html) {
                    panel.innerHTML = html;
                    panel.hidden = false;
                    bell.setAttribute('aria-expanded', 'true');
                    // Opening the panel marks the notifications as read on the server.
                    countVersion += 1;
                    setCount(0);
                })
                .catch(function () { /* leave the panel closed */ });
        }

        bell.setAttribute('aria-expanded', 'false');
        bell.addEventListener('click', function (event) {
            event.stopPropagation();
            if (panel.hidden) {
                openPanel();
            } else {
                closePanel();
            }
        });

        document.addEventListener('click', function (event) {
            if (!panel.hidden && !panel.contains(event.target)) {
                closePanel();
            }
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !panel.hidden) {
                closePanel();
            }
        });

        refreshUnreadCount();
    });
})();
