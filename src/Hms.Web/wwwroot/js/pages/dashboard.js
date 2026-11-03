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
    });
})();
