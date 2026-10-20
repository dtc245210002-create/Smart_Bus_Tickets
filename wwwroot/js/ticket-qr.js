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
    const rawPayload = document.getElementById("qrPayloadData")?.value;
    const qrPayload = rawPayload ? rawPayload.trim() : "";
    const qrImage = document.getElementById("ticketQrImage");

    // Nếu không có payload vé thật hoặc là chuỗi mẫu, không tự sinh QR giả lập
    if (!qrPayload || qrPayload === "SMARTBUS_DEFAULT") {
        if (qrContainer) {
            qrContainer.innerHTML = `
                <div class="text-center p-2 text-muted">
                    <i class="bi bi-qr-code-scan fs-1 d-block mb-2 text-secondary opacity-50"></i>
                    <span class="small fw-semibold d-block text-secondary">Mã QR chưa khả dụng</span>
                    <span class="text-muted" style="font-size: 0.75rem;">Vui lòng hoàn tất thanh toán để lấy mã</span>
                </div>`;
        }
        if (qrImage) {
            qrImage.style.display = "none";
        }
    } else if (qrContainer && typeof QRCode !== 'undefined') {
        try {
            // Tạo phần tử tạm để thử vẽ QR bằng QRCodeJS
            const tempDiv = document.createElement("div");
            new QRCode(tempDiv, {
                text: qrPayload,
                width: 170,
                height: 170,
                colorDark: "#0A4D46",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.M
            });

            // Nếu vẽ thành công, thay thế nội dung của qrContainer
            setTimeout(() => {
                if (tempDiv.querySelector("canvas") || tempDiv.querySelector("img")) {
                    qrContainer.innerHTML = "";
                    qrContainer.appendChild(tempDiv.firstChild);
                }
            }, 50);
        } catch (e) {
            console.warn("QRCodeJS fallback to API image:", e);
            // Giữ nguyên qrImage fallback
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
