(function () {
    "use strict";
    var canvas = document.getElementById("revenueChart");
    if (!canvas || typeof Chart === "undefined") return;
    var labels = JSON.parse(canvas.dataset.labels || "[]");
    var values = JSON.parse(canvas.dataset.values || "[]");
    new Chart(canvas, { type: "line", data: { labels: labels, datasets: [{ label: "Net revenue", data: values, borderColor: "#0f9d94", backgroundColor: "rgba(15,157,148,.12)", fill: true, tension: .3, pointRadius: 3 }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true, ticks: { callback: function (value) { return "₹" + value; } } }, x: { grid: { display: false } } } } });
})();
