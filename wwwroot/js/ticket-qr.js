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
    const qrPayload = document.getElementById("qrPayloadData")?.value || document.getElementById("ticketCodeDisplay")?.innerText?.trim() || "SMARTBUS_DEFAULT";
    const qrImage = document.getElementById("ticketQrImage");
    const isTicketUsed = qrContainer?.dataset.status === "USED" || qrContainer?.classList.contains("sbg-qr-box-used");
    const isTicketInactive = isTicketUsed || qrContainer?.dataset.status === "EXPIRED" || qrContainer?.dataset.status === "CANCELLED";

    // 1. Kiểm tra xem ảnh QR từ Server Base64 đã hiển thị thành công chưa
    const hasValidServerQr = qrImage && qrImage.getAttribute("src") && qrImage.getAttribute("src").startsWith("data:image/");

    if (hasValidServerQr && qrImage) {
        qrImage.style.display = "block";
        qrImage.style.visibility = "visible";
    }

    // 2. Client-side fallback: Chỉ render lại bằng QRCodeJS nếu chưa có ảnh Base64 từ Server
    if (!hasValidServerQr && qrContainer && typeof QRCode !== 'undefined' && !isTicketInactive) {
        renderClientQr();
    }

    function renderClientQr() {
        try {
            const tempDiv = document.createElement("div");
            new QRCode(tempDiv, {
                text: qrPayload,
                width: 170,
                height: 170,
                colorDark: "#0A4D46",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.M
            });

            // QRCodeJS tạo ra canvas (bị ẩn style display:none) và img
            setTimeout(() => {
                const canvas = tempDiv.querySelector("canvas");
                const img = tempDiv.querySelector("img");
                let elToInsert = null;

                if (img && img.src && img.src.length > 50) {
                    img.style.display = "block";
                    img.style.width = "170px";
                    img.style.height = "170px";
                    img.style.objectFit = "contain";
                    img.className = "img-fluid rounded";
                    elToInsert = img;
                } else if (canvas) {
                    canvas.style.display = "block"; // BẮT BUỘC: Hủy bỏ display:none mặc định của QRCodeJS
                    canvas.style.width = "170px";
                    canvas.style.height = "170px";
                    canvas.style.objectFit = "contain";
                    canvas.className = "img-fluid rounded";
                    elToInsert = canvas;
                }

                if (elToInsert) {
                    const currentImg = document.getElementById("ticketQrImage");
                    if (currentImg) {
                        currentImg.replaceWith(elToInsert);
                    } else {
                        qrContainer.prepend(elToInsert);
                    }
                }
            }, 80);
        } catch (e) {
            console.warn("QRCodeJS fallback error:", e);
        }
    }

    // Nếu ảnh bên ngoài bị lỗi tải (offline hoặc mạng chậm), tự động render QRCode client ngay lập tức
    if (qrImage && !hasValidServerQr) {
        qrImage.addEventListener("error", function () {
            if (typeof QRCode !== 'undefined' && !isTicketInactive) {
                renderClientQr();
            }
        });
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
