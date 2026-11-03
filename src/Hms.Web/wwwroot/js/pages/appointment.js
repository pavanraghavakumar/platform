// Appointment booking page: loads the free slots of a doctor and date from GET /Appointment/Slots.
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var doctor = document.getElementById('doctorId');
        var date = document.getElementById('date');
        var time = document.getElementById('time');
        if (!doctor || !date || !time) {
            return;
        }

        var selected = time.getAttribute('data-selected') || '';
        var requestGeneration = 0;

        function renderSlots(slots) {
            time.innerHTML = '';
            var freeSlots = (slots || []).filter(function (slot) { return slot.available; });
            if (freeSlots.length === 0) {
                var empty = document.createElement('option');
                empty.value = '';
                empty.textContent = 'No free slots';
                time.appendChild(empty);
                return;
            }

            var placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = 'Choose a time slot';
            time.appendChild(placeholder);

            freeSlots.forEach(function (slot) {
                var option = document.createElement('option');
                option.value = slot.time;
                option.textContent = slot.time;
                if (slot.time === selected) {
                    option.selected = true;
                }
                time.appendChild(option);
            });
        }

        function loadSlots() {
            var generation = ++requestGeneration;
            if (!doctor.value || !date.value) {
                renderSlots([]);
                return;
            }
            fetch('/Appointment/Slots?doctorId=' + encodeURIComponent(doctor.value) + '&date=' + encodeURIComponent(date.value), {
                headers: { 'Accept': 'application/json' }
            })
                .then(function (response) { return response.ok ? response.json() : { slots: [] }; })
                .then(function (data) {
                    if (generation === requestGeneration) {
                        renderSlots(data.slots);
                    }
                })
                .catch(function () {
                    if (generation === requestGeneration) {
                        renderSlots([]);
                    }
                });
        }

        doctor.addEventListener('change', loadSlots);
        date.addEventListener('change', loadSlots);

        if (doctor.value && date.value) {
            loadSlots();
        }
    });
})();
