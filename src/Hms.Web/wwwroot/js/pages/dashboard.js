// Admin dashboard — draws the revenue and appointment charts with the local Chart.js.
(function () {
    'use strict';

    function readPoints(canvas) {
        var labels = (canvas.getAttribute('data-labels') || '')
            .split('|')
            .filter(function (label) { return label.length > 0; });
        var values = (canvas.getAttribute('data-values') || '')
            .split('|')
            .filter(function (value) { return value.length > 0; })
            .map(function (value) { return Number(value) || 0; });
        return { labels: labels, values: values };
    }

    function draw(canvas, type, colour, fill) {
        if (!canvas || typeof Chart === 'undefined') {
            return;
        }

        var points = readPoints(canvas);
        new Chart(canvas, {
            type: type,
            data: {
                labels: points.labels,
                datasets: [{
                    label: canvas.getAttribute('aria-label') || '',
                    data: points.values,
                    borderColor: colour,
                    backgroundColor: fill,
                    borderWidth: 2,
                    fill: type === 'line',
                    tension: 0.3,
                    pointRadius: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        draw(document.getElementById('hmsRevenueChart'), 'bar', '#0f9d94', 'rgba(15, 157, 148, 0.35)');
        draw(document.getElementById('hmsAppointmentChart'), 'line', '#0f2f5c', 'rgba(15, 47, 92, 0.12)');

        var patientSearch = document.getElementById('receptionistPatientSearch');
        var openPatient = document.getElementById('openPatientProfile');
        var patientHelp = document.getElementById('patientSearchHelp');

        function selectedPatientUrl() {
            if (!patientSearch) {
                return '';
            }

            var options = document.querySelectorAll('#receptionistPatients option');
            for (var i = 0; i < options.length; i += 1) {
                if (options[i].value === patientSearch.value) {
                    return options[i].getAttribute('data-url') || '';
                }
            }
            return '';
        }

        function openSelectedPatient() {
            var url = selectedPatientUrl();
            if (url) {
                window.location.assign(url);
                return;
            }
            if (patientHelp) {
                patientHelp.textContent = 'Pick a patient from the matching results.';
            }
        }

        if (patientSearch) {
            patientSearch.addEventListener('change', openSelectedPatient);
            patientSearch.addEventListener('input', function () {
                if (patientHelp) {
                    patientHelp.textContent = '';
                }
            });
        }
        if (openPatient) {
            openPatient.addEventListener('click', openSelectedPatient);
        }

        var invoicePatient = document.getElementById('invoicePatient');
        var newInvoice = document.getElementById('newInvoiceButton');
        var invoiceHelp = document.getElementById('invoicePatientHelp');

        function updateInvoiceButton() {
            if (!invoicePatient || !newInvoice) {
                return;
            }
            var option = invoicePatient.options[invoicePatient.selectedIndex];
            var url = option ? option.getAttribute('data-url') : '';
            newInvoice.disabled = !url;
            newInvoice.setAttribute('data-url', url || '');
            if (invoiceHelp) {
                invoiceHelp.textContent = '';
            }
        }

        if (invoicePatient && newInvoice) {
            invoicePatient.addEventListener('change', updateInvoiceButton);
            newInvoice.addEventListener('click', function () {
                var url = newInvoice.getAttribute('data-url');
                if (url) {
                    window.location.assign(url);
                } else if (invoiceHelp) {
                    invoiceHelp.textContent = 'Pick a patient before creating an invoice.';
                }
            });
            updateInvoiceButton();
        }
    });
})();
