(() => {
    const form = document.querySelector('[data-portal-booking]');
    if (!form) return;

    const date = form.querySelector('[data-portal-date]');
    const slots = form.querySelector('[data-portal-slots]');
    const doctors = form.querySelectorAll('input[name="DoctorId"]');
    const slotsUrl = form.dataset.slotsUrl;

    async function loadSlots() {
        const doctor = form.querySelector('input[name="DoctorId"]:checked');
        slots.innerHTML = '<option value="">Choose a doctor and date</option>';
        if (!doctor || !date.value) return;

        slots.disabled = true;
        try {
            const response = await fetch(`${slotsUrl}?doctorId=${encodeURIComponent(doctor.value)}&date=${encodeURIComponent(date.value)}`);
            if (!response.ok) throw new Error('Could not load slots.');
            const result = await response.json();
            const available = result.slots.filter(slot => slot.available);
            slots.innerHTML = available.length
                ? '<option value="">Choose a free slot</option>'
                : '<option value="">No free slots</option>';
            available.forEach(slot => slots.add(new Option(slot.time, slot.time)));
        } catch {
            slots.innerHTML = '<option value="">Could not load slots</option>';
        } finally {
            slots.disabled = false;
        }
    }

    doctors.forEach(doctor => doctor.addEventListener('change', loadSlots));
    date.addEventListener('change', loadSlots);
    if (form.querySelector('input[name="DoctorId"]:checked') && date.value) loadSlots();
})();
