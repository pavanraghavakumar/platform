// Healthcare Management System — shared page behaviour.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var shell = document.getElementById('hmsShell');
        var sidebar = document.getElementById('hmsSidebar');
        var toggle = document.getElementById('hmsSidebarToggle');
        var backdrop = document.getElementById('hmsBackdrop');
        var closeBtn = document.getElementById('hmsSidebarClose');

        // Below this width the sidebar is a drawer behind the menu button; above it the
        // toggle collapses the sidebar to icons. Keep in step with the 900px media query.
        var drawerQuery = window.matchMedia('(max-width: 900px)');

        function isDrawer() {
            return drawerQuery.matches;
        }

        function setDrawerOpen(open) {
            if (!shell) {
                return;
            }
            shell.classList.toggle('hms-sidebar-open', open);
            if (sidebar) {
                sidebar.setAttribute('aria-hidden', isDrawer() && !open ? 'true' : 'false');
            }
            if (backdrop) {
                backdrop.hidden = !open;
            }
            if (toggle) {
                toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            }
        }

        function closeDrawer() {
            setDrawerOpen(false);
        }

        if (shell && toggle) {
            toggle.addEventListener('click', function () {
                if (isDrawer()) {
                    setDrawerOpen(!shell.classList.contains('hms-sidebar-open'));
                    return;
                }
                var collapsed = shell.classList.toggle('hms-collapsed');
                toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
            });
        }

        if (backdrop) {
            backdrop.addEventListener('click', closeDrawer);
        }

        if (closeBtn) {
            closeBtn.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                closeDrawer();
                if (toggle) {
                    toggle.focus();
                }
            });
        }

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeDrawer();
            }
        });

        // A link inside the drawer navigates away; close it so the next page starts closed.
        if (shell) {
            shell.querySelectorAll('.hms-nav a').forEach(function (link) {
                link.addEventListener('click', function () {
                    if (isDrawer()) {
                        closeDrawer();
                    }
                });
            });
        }

        // Keep the button state and drawer state aligned with the active layout mode.
        var onBreakpointChange = function () {
            if (isDrawer()) {
                closeDrawer();
            } else {
                closeDrawer();
                if (toggle && shell) {
                    toggle.setAttribute('aria-expanded', shell.classList.contains('hms-collapsed') ? 'false' : 'true');
                }
            }
        };
        if (drawerQuery.addEventListener) {
            drawerQuery.addEventListener('change', onBreakpointChange);
        } else if (drawerQuery.addListener) {
            drawerQuery.addListener(onBreakpointChange);
        }
        onBreakpointChange();

        // The design uses tables on desktop and stacked, labelled cards on phones. Add each
        // heading to its matching cell once so the same server-rendered markup supports both.
        document.querySelectorAll('.hms-table').forEach(function (table) {
            var headings = Array.prototype.map.call(table.querySelectorAll('thead th'), function (heading) {
                return heading.textContent.trim();
            });
            table.querySelectorAll('tbody tr').forEach(function (row) {
                row.querySelectorAll('td').forEach(function (cell, index) {
                    if (!cell.hasAttribute('colspan')) {
                        cell.setAttribute('data-label', headings[index] || '');
                    }
                });
            });
        });

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
