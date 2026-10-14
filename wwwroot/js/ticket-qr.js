// SmartBus Go - Ticket QR, WakeLock & Offline Capture Module
document.addEventListener("DOMContentLoaded", function () {
    let wakeLock = null;

    async function requestWakeLock() {
        try {
            if ('wakeLock' in navigator) {
                wakeLock = await navigator.wakeLock.request('screen');
                const statusBadge = document.getElementById('wakeLockStatus');
                if (statusBadge) {
                    statusBadge.innerHTML = '⚡ Màn hình đang được giữ sáng tối đa cho tài xế quét';
                }
            }
        } catch (err) {
            console.warn('Wake Lock không khả dụng:', err.message);
        }
    }

    requestWakeLock();

    document.addEventListener('visibilitychange', async () => {
        if (wakeLock !== null && document.visibilityState === 'visible') {
            await requestWakeLock();
        }
    });

    const qrContainer = document.getElementById("ticketQrCanvas");
    const qrPayload = document.getElementById("qrPayloadData")?.value || "SMARTBUS_DEFAULT";

    function renderFallbackQr() {
        if (!qrContainer) return;
        const encoded = encodeURIComponent(qrPayload);
        qrContainer.innerHTML = `
            <img id="ticketQrImage"
                 src="https://api.qrserver.com/v1/create-qr-code/?size=170x170&data=${encoded}&color=0A4D46" 
                 alt="Mã QR Vé Xe" 
                 class="img-fluid rounded" 
                 style="width: 170px; height: 170px; object-fit: contain; display: block; margin: 0 auto;" 
                 onerror="this.onerror=null; this.src='https://quickchart.io/qr?text=${encoded}&size=170';" />
        `;
    }

    if (qrContainer) {
        if (typeof QRCode !== 'undefined') {
            try {
                // Xóa nội dung chờ và sinh mã QR trực tiếp vào container
                qrContainer.innerHTML = "";
                new QRCode(qrContainer, {
                    text: qrPayload,
                    width: 170,
                    height: 170,
                    colorDark: "#0A4D46",
                    colorLight: "#ffffff",
                    correctLevel: QRCode.CorrectLevel.M
                });

                // QRCodeJS sinh ra cả 2 thẻ: <canvas> và <img>.
                // Chỉ giữ lại DUY NHẤT 1 thẻ để tránh bị hiển thị 2 mã QR cạnh nhau!
                setTimeout(() => {
                    const canvas = qrContainer.querySelector("canvas");
                    const img = qrContainer.querySelector("img");
                    
                    if (img && img.src && img.src.length > 50) {
                        img.style.display = "block";
                        img.style.margin = "0 auto";
                        img.style.borderRadius = "8px";
                        if (canvas) canvas.remove(); // Xóa thẻ canvas thừa, chỉ giữ duy nhất 1 thẻ ảnh QR
                    } else if (canvas) {
                        canvas.style.display = "block";
                        canvas.style.margin = "0 auto";
                        canvas.style.borderRadius = "8px";
                        if (img) img.remove(); // Xóa thẻ ảnh thừa nếu canvas đang hiển thị
                    } else {
                        renderFallbackQr();
                    }
                }, 60);
            } catch (e) {
                console.warn("QRCodeJS gặp lỗi, chuyển sang fallback API:", e);
                renderFallbackQr();
            }
        } else {
            renderFallbackQr();
        }
    }

    const btnDownload = document.getElementById("btnDownloadTicket");
    if (btnDownload) {
        btnDownload.addEventListener("click", function () {
            const ticketElement = document.getElementById("printableTicket");
            if (!ticketElement) return;

            btnDownload.innerHTML = '<span class="spinner-border spinner-border-sm" role="status"></span> Đang tạo ảnh...';
            btnDownload.disabled = true;

            if (typeof html2canvas !== 'undefined') {
                html2canvas(ticketElement, {
                    scale: 2,
                    useCORS: true,
                    backgroundColor: "#f8fafc"
                }).then(canvas => {
                    const link = document.createElement("a");
                    link.download = `Ve_SmartBus_${document.getElementById('ticketCodeDisplay')?.innerText || 'Ticket'}.png`;
                    link.href = canvas.toDataURL("image/png");
                    link.click();

                    btnDownload.innerHTML = '📥 Lưu ảnh vé vào máy (Offline)';
                    btnDownload.disabled = false;
                }).catch(err => {
                    console.error("Lỗi chụp ảnh:", err);
                    alert("Không thể tự động xuất ảnh. Bạn có thể chụp màn hình điện thoại.");
                    btnDownload.innerHTML = '📥 Lưu ảnh vé vào máy (Offline)';
                    btnDownload.disabled = false;
                });
            } else {
                window.print();
                btnDownload.innerHTML = '📥 Lưu ảnh vé vào máy (Offline)';
                btnDownload.disabled = false;
            }
        });
    }

    const btnPrint = document.getElementById("btnPrintTicket");
    if (btnPrint) {
        btnPrint.addEventListener("click", function () {
            window.print();
        });
    }
});
