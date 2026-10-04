document.addEventListener('DOMContentLoaded', () => {
    const timer = document.getElementById('holdCountdown'), form = document.getElementById('demoPaymentForm'); const expires = Date.parse(timer.dataset.expires);
    function tick() { const left = Math.max(0, Math.ceil((expires - Date.now()) / 1000)); timer.textContent = `${String(Math.floor(left / 60)).padStart(2,'0')}:${String(left % 60).padStart(2,'0')}`; if (!left) { form.querySelector('fieldset').disabled = true; document.getElementById('paymentExpired').classList.remove('d-none'); } }
    tick(); const interval = setInterval(tick,1000); window.addEventListener('pagehide',()=>clearInterval(interval));
    const subtotal = Number(document.querySelector('.sbg-bank-info .fs-4').textContent.replace(/[^0-9]/g,''));
    document.getElementById('applyVoucher').addEventListener('click', () => { const value = document.getElementById('voucher').value.trim().toUpperCase(); const valid = value === 'SMARTBUSNEW', discount = valid ? Math.min(20000,subtotal) : 0; document.getElementById('voucherMessage').textContent = valid ? 'Đã áp dụng giảm 20.000 đ. Máy chủ sẽ xác nhận lại khi thanh toán.' : value ? 'Mã không hợp lệ. Thử SMARTBUSNEW.' : 'Đã bỏ mã giảm giá.'; document.getElementById('discountAmount').textContent = `-${discount.toLocaleString('vi-VN')} đ`; document.querySelectorAll('.payment-total').forEach(el=>el.textContent=`${(subtotal-discount).toLocaleString('vi-VN')} đ`); });
    document.querySelectorAll('[data-copy]').forEach(button=>button.addEventListener('click',async()=>{try {await navigator.clipboard.writeText(button.dataset.copy);document.getElementById('copyMessage').textContent='Đã sao chép mã đơn.';}catch{document.getElementById('copyMessage').textContent='Không thể sao chép tự động. Bạn có thể chọn mã đơn và sao chép.';}}));
    form.addEventListener('submit', event => {
        if (form.dataset.submitting) { event.preventDefault(); return; }
        form.dataset.submitting = 'true';
        if (event.submitter?.hasAttribute('formaction')) form.action = event.submitter.formAction;
        setTimeout(() => form.querySelectorAll('button[type="submit"]').forEach(b => b.disabled = true), 0);
    });
});
