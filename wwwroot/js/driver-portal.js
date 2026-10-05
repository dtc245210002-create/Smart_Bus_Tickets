(() => {
    'use strict';
    const password = document.getElementById('driverPassword');
    const toggle = document.getElementById('toggleDriverPassword');
    if (password && toggle) toggle.addEventListener('click', () => {
        const visible = password.type === 'password';
        password.type = visible ? 'text' : 'password';
        toggle.setAttribute('aria-label', visible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
        toggle.setAttribute('aria-pressed', String(visible));
        toggle.querySelector('i').className = visible ? 'bi bi-eye-slash' : 'bi bi-eye';
    });
    const login = document.getElementById('driverLoginForm');
    if (login) login.addEventListener('submit', event => {
        if (!login.checkValidity() || (window.jQuery && !window.jQuery(login).valid())) return;
        const button = login.querySelector('[type="submit"]');
        if (button.disabled) { event.preventDefault(); return; }
        button.disabled = true;
        button.textContent = 'Đang đăng nhập…';
    });
    const dashboard = document.getElementById('driverDemoDashboard');
    const scanner = document.getElementById('driverScanner');
    const tripElement = dashboard || scanner;
    const checkInKey = 'smartbus-driver-demo:' + (tripElement?.dataset.tripDate || '') + ':' + (tripElement?.dataset.tripId || '1');
    if (dashboard) {
        let checked = [];
        try { checked = JSON.parse(sessionStorage.getItem(checkInKey) || '[]'); } catch {}
        let total = 0;
        dashboard.querySelectorAll('[data-ticket-code]').forEach(row => {
            const done = checked.includes(row.dataset.ticketCode);
            const state = row.querySelector('.sc-person-state');
            state.textContent = done ? 'Đã check-in' : 'Chưa check-in';
            state.classList.toggle('sc-checked', done);
            if (done) total++;
        });
        dashboard.querySelector('.sc-trip-progress strong').textContent = total + ' / 3';
        const progress = dashboard.querySelector('[role="progressbar"]');
        progress.setAttribute('aria-valuenow', total);
        progress.querySelector('span').style.width = (100 * total / 3) + '%';
    }
    if (scanner) {
        const form = document.getElementById('verifyDriverTicket');
        const input = document.getElementById('driverTicketCode');
        const submit = form.querySelector('[type="submit"]');
        const video = document.getElementById('driverCamera');
        const status = document.getElementById('cameraStatus');
        const start = document.getElementById('startDriverCamera');
        const stop = document.getElementById('stopDriverCamera');
        let stream = null, scanning = false, timer = null, starting = false;
        function stopCamera() {
            scanning = false; clearTimeout(timer);
            stream?.getTracks().forEach(track => track.stop()); stream = null;
            video.srcObject = null; video.hidden = true;
            document.getElementById('scanPlaceholder').hidden = false;
            stop.hidden = true; start.disabled = false;
        }
        function showResult(data) {
            document.getElementById('ticketResultEmpty').hidden = true;
            document.getElementById('ticketResult').hidden = false;
            const badge = document.getElementById('ticketResultBadge');
            badge.textContent = data.success ? 'CHECK-IN THÀNH CÔNG' : 'KHÔNG THỂ CHECK-IN';
            badge.dataset.kind = data.success ? 'success' : 'error';
            document.getElementById('ticketPassenger').textContent = data.passengerName || (data.success ? 'Vé hợp lệ' : 'Kiểm tra lại vé');
            document.getElementById('ticketRoute').textContent = data.routeName || '';
            document.getElementById('ticketSeat').textContent = data.seatNumber || '—';
            document.getElementById('ticketCodeResult').textContent = data.ticketCode || input.value.trim();
            document.getElementById('ticketResultMessage').textContent = data.message || 'Chưa có kết quả xác thực.';
        }
        form.addEventListener('submit', async event => {
            event.preventDefault(); if (submit.disabled || !form.reportValidity()) return;
            submit.disabled = true; submit.textContent = 'Đang kiểm tra…';
            try {
                const code = input.value.trim().toUpperCase();
                const tickets = {
                    'SBG-001': { passengerName: 'Nguyễn Văn An', seatNumber: 'A01' },
                    'SBG-002': { passengerName: 'Trần Văn Bình', seatNumber: 'A02' },
                    'SBG-003': { passengerName: 'Lê Văn Minh', seatNumber: 'B01' }
                };
                const ticket = tickets[code];
                let checked = [];
                try { checked = JSON.parse(sessionStorage.getItem(checkInKey) || '[]'); } catch {}
                if (code === 'SBG-999') showResult({ success: false, message: 'Vé mẫu thuộc chuyến khác.' });
                else if (!ticket) showResult({ success: false, message: 'Mã vé mẫu không hợp lệ.' });
                else if (checked.includes(code)) showResult({ ...ticket, success: false, ticketCode: code, message: 'Vé đã được check-in trước đó trong phiên demo này.' });
                else {
                    checked.push(code);
                    sessionStorage.setItem(checkInKey, JSON.stringify(checked));
                    showResult({ ...ticket, success: true, ticketCode: code, routeName: 'Thái Nguyên → Hà Nội', message: 'Check-in mẫu thành công. Chỉ lưu trong phiên trình duyệt, không cập nhật dữ liệu thật.' });
                }
            } catch {
                showResult({ success: false, message: 'Không lưu được trạng thái demo trên trình duyệt này.' });
            } finally { submit.disabled = false; submit.textContent = 'Kiểm tra & check-in'; }
        });
        document.getElementById('nextDriverTicket').addEventListener('click', () => {
            document.getElementById('ticketResult').hidden = true;
            document.getElementById('ticketResultEmpty').hidden = false;
            input.value = ''; input.focus();
        });
        start.addEventListener('click', async () => {
            if (starting || scanning) return;
            if (!('BarcodeDetector' in window) || !navigator.mediaDevices?.getUserMedia) {
                status.textContent = 'Trình duyệt chưa hỗ trợ quét QR. Vui lòng nhập mã vé thủ công.'; return;
            }
            starting = true; start.disabled = true;
            try {
                const detector = new window.BarcodeDetector({ formats: ['qr_code'] });
                stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
                video.srcObject = stream; video.hidden = false;
                document.getElementById('scanPlaceholder').hidden = true;
                await video.play(); scanning = true; stop.hidden = false;
                status.textContent = 'CAMERA ĐANG HOẠT ĐỘNG';
                async function scan() {
                    if (!scanning) return;
                    try {
                        const results = await detector.detect(video);
                        if (results.length) {
                            input.value = results[0].rawValue; stopCamera();
                            status.textContent = 'Đã đọc QR. Bấm Kiểm tra & check-in để xác nhận.'; input.focus(); return;
                        }
                    } catch { stopCamera(); status.textContent = 'Không đọc được camera. Vui lòng nhập mã vé.'; return; }
                    if (scanning) timer = setTimeout(scan, 400);
                }
                scan();
            } catch { stopCamera(); status.textContent = 'Không mở được camera. Kiểm tra quyền truy cập hoặc nhập mã vé.'; }
            finally { starting = false; }
        });
        stop.addEventListener('click', () => { stopCamera(); status.textContent = 'CAMERA ĐÃ TẮT'; });
        window.addEventListener('pagehide', stopCamera);
        document.addEventListener('visibilitychange', () => { if (document.hidden) stopCamera(); });
    }
    const incident = document.getElementById('driverIncidentForm');
    if (incident) {
        const fields = ['type', 'delay', 'location', 'description'];
        const key = 'smartbus-driver-incident:' + incident.dataset.tripId;
        const description = document.getElementById('incidentDescription');
        const count = document.getElementById('incidentChars');
        const feedback = document.getElementById('incidentFeedback');
        try {
            const saved = JSON.parse(sessionStorage.getItem(key));
            if (saved) fields.forEach(name => { if (typeof saved[name] === 'string') incident.elements[name].value = saved[name]; });
        } catch { /* A draft is optional; the form remains usable without storage. */ }
        count.textContent = description.value.length;
        description.addEventListener('input', () => { count.textContent = description.value.length; });
        let imageUrl = null;
        document.getElementById('incidentPhoto').addEventListener('change', event => {
            const preview = document.getElementById('incidentPreview'); preview.hidden = true;
            if (imageUrl) URL.revokeObjectURL(imageUrl); imageUrl = null;
            const file = event.target.files[0];
            if (!file) return;
            if (!file.type.startsWith('image/') || file.size > 5 * 1024 * 1024) {
                feedback.textContent = 'Vui lòng chọn ảnh dưới 5 MB.'; feedback.hidden = false; event.target.value = ''; return;
            }
            imageUrl = URL.createObjectURL(file); preview.src = imageUrl; preview.hidden = false;
        });
        window.addEventListener('pagehide', () => { if (imageUrl) URL.revokeObjectURL(imageUrl); });
        incident.addEventListener('submit', event => {
            event.preventDefault(); if (!incident.reportValidity()) return;
            const data = Object.fromEntries(fields.map(name => [name, incident.elements[name].value.trim()]));
            if (!data.location || !data.description) { feedback.textContent = 'Vui lòng nhập vị trí và mô tả sự cố.'; feedback.hidden = false; return; }
            try {
                sessionStorage.setItem(key, JSON.stringify(data));
                feedback.textContent = 'Đã lưu bản nháp trong phiên trình duyệt này. Chưa gửi đến điều hành. Ảnh cần được chọn lại khi tải lại trang.';
            } catch { feedback.textContent = 'Không lưu được bản nháp trên thiết bị. Vui lòng giữ trang này để bảo toàn thông tin.'; }
            feedback.hidden = false;
        });
    }
})();

