document.addEventListener('DOMContentLoaded', () => {
    const date = document.getElementById('travelDate')?.value;
    const from = document.querySelector('input[name="from"]')?.value;
    const to = document.querySelector('input[name="to"]')?.value;
    const selectionKey = `smartbus:${from}:${to}:${date}`;
    const alertBox = document.getElementById('bookingAlert');
    function notify(message) { if (alertBox) { alertBox.textContent = message; alertBox.classList.remove('d-none'); alertBox.focus(); } }
    function summary(accordion) {
        const seats = [...accordion.querySelectorAll('.sbg-seat-box.selected')];
        const total = seats.reduce((sum, seat) => sum + Number(seat.dataset.price), 0);
        accordion.querySelector('.selected-seats-list').textContent = seats.length ? seats.map(s => s.dataset.code).join(', ') : 'Chưa chọn ghế nào';
        accordion.querySelector('.selected-total-price').textContent = `${total.toLocaleString('vi-VN')} VNĐ`;
        const button = accordion.querySelector('.btn-continue-booking'); button.disabled = !seats.length;
        button.textContent = seats.length ? `Tiếp tục đặt ${seats.length} vé (${total.toLocaleString('vi-VN')} đ) →` : 'Vui lòng chọn ghế trước khi tiếp tục →';
    }
    function open(tripId) {
        document.querySelectorAll('.sbg-seat-accordion').forEach(a => a.classList.toggle('show', a.id === `seatAccordion-${tripId}`));
        document.querySelectorAll('.btn-toggle-seat-map').forEach(b => { const active = b.dataset.tripId === String(tripId); b.textContent = active ? '⌃ Đóng lại' : '⌄ Chọn chỗ'; b.setAttribute('aria-expanded', String(active)); });
    }
    document.querySelectorAll('.btn-toggle-seat-map').forEach(button => button.addEventListener('click', () => {
        const accordion = document.getElementById(`seatAccordion-${button.dataset.tripId}`);
        if (accordion.classList.contains('show')) { accordion.classList.remove('show'); button.textContent = '⌄ Chọn chỗ'; button.setAttribute('aria-expanded', 'false'); } else open(button.dataset.tripId);
    }));
    document.querySelectorAll('.sbg-seat-box').forEach(seat => seat.addEventListener('click', () => {
        if (seat.disabled) return; const accordion = seat.closest('.sbg-seat-accordion');
        if (!seat.classList.contains('selected') && accordion.querySelectorAll('.selected').length >= 6) { notify('Bạn chỉ được chọn tối đa 6 ghế cho một lần đặt vé.'); return; }
        seat.classList.toggle('selected'); seat.setAttribute('aria-pressed', String(seat.classList.contains('selected'))); summary(accordion);
    }));
    document.querySelectorAll('.btn-continue-booking').forEach(button => button.addEventListener('click', async () => {
        if (button.dataset.conflict) { location.reload(); return; }
        const accordion = button.closest('.sbg-seat-accordion'); const seats = [...accordion.querySelectorAll('.sbg-seat-box.selected')].map(s => s.dataset.code); if (!seats.length) return;
        button.disabled = true; button.textContent = 'Đang giữ ghế…';
        try {
            const response = await fetch('/Ticket/Hold', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': document.querySelector('#holdToken input').value }, body: JSON.stringify({ tripId: Number(button.dataset.tripId), date, seats, from, to, boarding: accordion.querySelector('.boarding-stop').value, dropoff: accordion.querySelector('.dropoff-stop').value }) });
            const data = await response.json();
            if (response.status === 401) { sessionStorage.setItem(selectionKey, JSON.stringify({ tripId: button.dataset.tripId, seats })); location.assign(data.loginUrl); return; }
            if (!response.ok || !data.success) { notify(data.message || 'Không thể giữ ghế. Vui lòng thử lại.'); if (response.status === 409) { button.textContent = 'Cập nhật sơ đồ ghế'; button.disabled = false; button.dataset.conflict = 'true'; } else summary(accordion); return; }
            sessionStorage.removeItem(selectionKey); location.assign(data.data.paymentUrl);
        } catch { notify('Kết nối bị gián đoạn. Vui lòng thử lại.'); summary(accordion); }
    }));
    try { const saved = JSON.parse(sessionStorage.getItem(selectionKey)); if (saved) { open(saved.tripId); const accordion = document.getElementById(`seatAccordion-${saved.tripId}`); if (accordion) { accordion.querySelectorAll('.sbg-seat-box').forEach(s => { if (!s.disabled && saved.seats.includes(s.dataset.code)) { s.classList.add('selected'); s.setAttribute('aria-pressed', 'true'); } }); summary(accordion); } sessionStorage.removeItem(selectionKey); } } catch { sessionStorage.removeItem(selectionKey); }
});
