// =========================================================================
// SmartBus Go - Xử lý sự kiện chọn/hủy chọn ghế & lưu giữ chỗ vào Database
// Đồng bộ trạng thái ghế thời gian thực giữa các tài khoản người dùng
// =========================================================================

document.addEventListener("DOMContentLoaded", function () {
    const MAX_SEATS = 6; // Giới hạn số ghế tối đa mỗi lần đặt
    let pollInterval = null;
    let activePollingTripId = null;
    const tripBookingData = {}; // Lưu trữ thông tin bookingCode = holdId thật từ server cho từng chuyến

    // Helper: Lấy hoặc tạo SessionId đồng bộ lưu trữ qua Cookie & LocalStorage
    function getOrCreateSessionId() {
        // Kiểm tra xem tài khoản đăng nhập có thay đổi không
        const currentUserId = window.sbgCurrentUserId ? String(window.sbgCurrentUserId) : "guest";
        const savedAuthUser = localStorage.getItem("sbg_auth_user_id");
        if (savedAuthUser !== currentUserId) {
            // Đã đổi user hoặc đăng xuất -> Xóa session cũ của user trước
            localStorage.removeItem("sbg_session_id");
            localStorage.setItem("sbg_auth_user_id", currentUserId);
            document.cookie = "sbg_session_id=; path=/; max-age=0; SameSite=Lax";
        }

        const name = "sbg_session_id=";
        const decodedCookie = decodeURIComponent(document.cookie);
        const ca = decodedCookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i].trim();
            if (c.indexOf(name) === 0) {
                const val = c.substring(name.length, c.length);
                if (val && val.length > 5) return val;
            }
        }
        let stored = localStorage.getItem("sbg_session_id");
        if (!stored) {
            stored = "sess_" + Math.random().toString(36).substring(2, 12) + Date.now().toString(36);
            localStorage.setItem("sbg_session_id", stored);
        }
        document.cookie = `sbg_session_id=${stored}; path=/; max-age=604800; SameSite=Lax`;
        return stored;
    }

    // 1. Xử lý mở / đóng Accordion sơ đồ chọn ghế & Bắt đầu đồng bộ ghế
    const toggleButtons = document.querySelectorAll(".btn-toggle-seat-map");
    toggleButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            const tripId = this.getAttribute("data-trip-id");
            const accordion = document.getElementById("seatAccordion-" + tripId);
            if (!accordion) return;

            if (accordion.classList.contains("show")) {
                accordion.classList.remove("show");
                this.innerHTML = '<i class="bi bi-chevron-down me-1"></i> Chọn chỗ';
                stopSeatPolling();
            } else {
                // Đóng các accordion khác trước khi mở cái mới
                document.querySelectorAll(".sbg-seat-accordion").forEach(acc => acc.classList.remove("show"));
                document.querySelectorAll(".btn-toggle-seat-map").forEach(b => {
                    b.innerHTML = '<i class="bi bi-chevron-down me-1"></i> Chọn chỗ';
                });

                accordion.classList.add("show");
                this.innerHTML = '<i class="bi bi-chevron-up me-1"></i> Đóng lại';
                
                // Đồng bộ trạng thái ghế mới nhất từ CSDL và bắt đầu Polling
                startSeatPolling(tripId);

                // Cuộn nhẹ tới accordion
                accordion.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });
    });

    // 2. Xử lý sự kiện click chọn / hủy chọn ghế & Gọi API lưu Database
    const seatBoxes = document.querySelectorAll(".sbg-seat-box");
    seatBoxes.forEach(seat => {
        if (seat.classList.contains("booked")) {
            seat.style.pointerEvents = "none";
        }
        seat.addEventListener("click", async function () {
            const tripId = this.getAttribute("data-trip-id");
            const seatCode = this.getAttribute("data-code");

            // Nếu ghế đã bán hoặc đang được tài khoản khác giữ -> Không cho chọn
            if (this.classList.contains("booked")) {
                showSeatAlert(tripId, `Ghế ${seatCode} đang được tài khoản khác giữ chỗ hoặc đã bán. Vui lòng chọn ghế khác!`, "warning");
                return;
            }

            const accordion = document.getElementById("seatAccordion-" + tripId);
            if (!accordion) return;

            const currentSelected = accordion.querySelectorAll(".sbg-seat-box.selected");
            const isSelecting = !this.classList.contains("selected");

            // Kiểm tra số lượng tối đa
            if (isSelecting && currentSelected.length >= MAX_SEATS) {
                showSeatAlert(tripId, `Bạn chỉ được chọn tối đa ${MAX_SEATS} ghế cho một lần đặt vé!`, "danger");
                return;
            }

            // Gọi API giữ hoặc hủy giữ ghế lưu vào CSDL SQL Server
            await toggleSeatHold(tripId, seatCode, isSelecting, this);
        });
    });

    // Hàm gọi API giữ chỗ / hủy giữ ghế đồng bộ CSDL
    async function toggleSeatHold(tripId, seatCode, isSelecting, seatEl) {
        const sessionId = getOrCreateSessionId();
        const userId = window.sbgCurrentUserId || null;

        // Vô hiệu hóa tương tác và tạo hiệu ứng chờ tránh double click
        seatEl.style.pointerEvents = "none";
        seatEl.style.opacity = "0.6";

        try {
            if (isSelecting) {
                // CHỌN GHẾ -> GỬI API GIỮ CHỖ
                const res = await fetch("/api/bookings/hold-seats", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        tripId: parseInt(tripId),
                        seatNumbers: [seatCode],
                        sessionId: sessionId,
                        userId: userId,
                        holdDurationMinutes: 10
                    })
                });

                let data = null;
                try {
                    data = await res.json();
                } catch (e) {
                    data = null;
                }

                if (res.status === 409) {
                    // GHẾ BỊ XUNG ĐỘT (Chỉ HTTP 409 mới đánh dấu ghế booked/đã bán/người khác giữ)
                    seatEl.classList.remove("selected");
                    seatEl.classList.add("booked");
                    seatEl.style.pointerEvents = "none";
                    const msg = (data && data.message) ? data.message : `Ghế ${seatCode} đang được tài khoản khác giữ hoặc đã bán. Vui lòng chọn ghế khác!`;
                    showSeatAlert(tripId, msg, "warning");
                    updateSeatSummary(tripId);
                    return;
                } else if (res.status === 401 || res.status === 403) {
                    // Yêu cầu xác thực / Phân quyền (Không đánh dấu ghế booked)
                    seatEl.classList.remove("selected");
                    const msg = (data && data.message) ? data.message : "Vui lòng đăng nhập tài khoản hành khách để giữ chỗ!";
                    showSeatAlert(tripId, msg, "danger");
                    updateSeatSummary(tripId);
                    return;
                } else if (res.status === 400) {
                    // Lỗi xác thực yêu cầu (Tối đa 6 ghế hoặc dữ liệu ghế không hợp lệ, không đánh dấu booked)
                    seatEl.classList.remove("selected");
                    const msg = (data && data.message) ? data.message : "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại số lượng hoặc lựa chọn ghế!";
                    showSeatAlert(tripId, msg, "warning");
                    updateSeatSummary(tripId);
                    return;
                } else if (!res.ok || !data || !data.success) {
                    // Lỗi máy chủ 503 / lỗi tạm thời (Không đánh dấu booked để cho phép thử lại)
                    seatEl.classList.remove("selected");
                    const msg = (data && data.message) ? data.message : "Hệ thống tạm thời bận hoặc gián đoạn kết nối. Vui lòng thử lại sau giây lát!";
                    showSeatAlert(tripId, msg, "danger");
                    updateSeatSummary(tripId);
                    return;
                }

                // Thành công từ server: Đánh dấu ghế Đang chọn
                seatEl.classList.add("selected");
                seatEl.classList.remove("booked");
                const remSec = (data.data && data.data.remainingSeconds) ? data.data.remainingSeconds : 600;
                const serverHoldId = (data.data && data.data.holdId) ? data.data.holdId : null;
                const serverTotal = (data.data && data.data.totalPrice != null) ? data.data.totalPrice : null;
                if (serverHoldId) {
                    tripBookingData[tripId] = {
                        bookingCode: serverHoldId,
                        totalPrice: serverTotal,
                        remainingSeconds: remSec
                    };
                }
                showSeatAlert(tripId, `Đã giữ ghế ${seatCode} thành công (thời hạn ${Math.round(remSec / 60)} phút)!`, "success");
            } else {
                // HỦY CHỌN GHẾ -> GỬI API HỦY GIỮ CHỖ
                const res = await fetch("/api/bookings/release-seats", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        tripId: parseInt(tripId),
                        seatNumbers: [seatCode],
                        sessionId: sessionId,
                        userId: userId
                    })
                });

                let data = null;
                try {
                    data = await res.json();
                } catch (e) {
                    data = null;
                }

                if (res.ok && data && data.success) {
                    seatEl.classList.remove("selected");
                    showSeatAlert(tripId, `Đã hủy giữ ghế ${seatCode}.`, "info");
                } else {
                    // Nhả ghế thất bại: Giữ nguyên selected vì server vẫn đang ghi nhận giữ chỗ
                    seatEl.classList.add("selected");
                    const msg = (data && data.message) ? data.message : `Không thể hủy giữ ghế ${seatCode} lúc này. Vui lòng thử lại!`;
                    showSeatAlert(tripId, msg, "warning");
                }
            }

            updateSeatSummary(tripId);
        } catch (err) {
            console.error("Lỗi khi kết nối API giữ ghế:", err);
            // Lỗi mạng khi chọn: gỡ selected; lỗi mạng khi nhả: giữ selected
            if (isSelecting) {
                seatEl.classList.remove("selected");
            } else {
                seatEl.classList.add("selected");
            }
            showSeatAlert(tripId, "Lỗi kết nối máy chủ khi xử lý ghế. Vui lòng kiểm tra mạng và thử lại!", "danger");
            updateSeatSummary(tripId);
        } finally {
            seatEl.style.opacity = "1";
            if (!seatEl.classList.contains("booked")) {
                seatEl.style.pointerEvents = "auto";
            }
        }
    }

    // 3. Cơ chế Polling thời gian thực: Cập nhật trạng thái ghế giữa các tài khoản
    function startSeatPolling(tripId) {
        stopSeatPolling();
        activePollingTripId = tripId;
        syncSeatStatuses(tripId);
        pollInterval = setInterval(() => {
            if (activePollingTripId) {
                syncSeatStatuses(activePollingTripId);
            }
        }, 4000); // 4 giây đồng bộ 1 lần
    }

    function stopSeatPolling() {
        if (pollInterval) {
            clearInterval(pollInterval);
            pollInterval = null;
        }
        activePollingTripId = null;
    }

    async function syncSeatStatuses(tripId) {
        const accordion = document.getElementById("seatAccordion-" + tripId);
        if (!accordion || !accordion.classList.contains("show")) return;

        const sessionId = getOrCreateSessionId();
        const userId = window.sbgCurrentUserId || "";

        try {
            const res = await fetch(`/api/bookings/trip-seat-status?tripId=${tripId}&sessionId=${encodeURIComponent(sessionId)}&userId=${userId}`);
            if (!res.ok) return;
            const data = await res.json();
            if (!data.success) return;

            const booked = new Set((data.bookedSeats || []).map(s => s.toUpperCase()));
            const myHeld = new Set((data.myHeldSeats || []).map(s => s.toUpperCase()));

            const seatBoxes = accordion.querySelectorAll(".sbg-seat-box");
            seatBoxes.forEach(seat => {
                // Nếu ghế đang có thao tác giữ/nhả in-flight (chờ response mạng), không đè trạng thái
                if (seat.style.opacity === "0.6") {
                    return;
                }

                const code = (seat.getAttribute("data-code") || "").toUpperCase();

                if (booked.has(code)) {
                    // Ghế đã bán hoặc đang được tài khoản khác giữ -> Khóa không cho chọn
                    seat.classList.add("booked");
                    seat.classList.remove("selected");
                    seat.style.pointerEvents = "none";
                    seat.setAttribute("title", `Ghế ${code} - Đã bán hoặc đang được tài khoản khác giữ`);
                } else if (myHeld.has(code)) {
                    // Ghế do chính tài khoản này đang giữ hợp lệ trên server
                    seat.classList.add("selected");
                    seat.classList.remove("booked");
                    seat.style.pointerEvents = "auto";
                    seat.setAttribute("title", `Ghế ${code} - Bạn đang giữ chỗ`);
                } else {
                    // Ghế trống (hoặc đã hết hạn giữ chỗ trên server -> xóa selected)
                    if (seat.classList.contains("selected")) {
                        seat.classList.remove("selected");
                    }
                    seat.classList.remove("booked");
                    seat.style.pointerEvents = "auto";
                    seat.setAttribute("title", `Ghế ${code} - Còn trống, bấm để chọn`);
                }
            });

            updateSeatSummary(tripId);
        } catch (e) {
            // Giữ nguyên giao diện nếu kết nối mạng tạm gián đoạn
        }
    }

    // 4. Hàm tính tổng tiền và cập nhật thông tin ghế đã chọn
    function updateSeatSummary(tripId) {
        const accordion = document.getElementById("seatAccordion-" + tripId);
        if (!accordion) return;

        const selectedSeats = accordion.querySelectorAll(".sbg-seat-box.selected");
        const seatCodes = [];
        let totalPrice = 0;

        selectedSeats.forEach(s => {
            const code = s.getAttribute("data-code");
            const price = parseFloat(s.getAttribute("data-price") || 150000);
            seatCodes.push(code);
            totalPrice += price;
        });

        const seatListEl = accordion.querySelector(".selected-seats-list");
        const totalPriceEl = accordion.querySelector(".selected-total-price");
        const btnContinue = accordion.querySelector(".btn-continue-booking");

        if (selectedSeats.length > 0) {
            // Có ghế được chọn
            const count = selectedSeats.length;
            const booking = tripBookingData[tripId];
            const displayTotal = (booking && booking.totalPrice != null) ? booking.totalPrice : totalPrice;
            seatListEl.innerHTML = `<span class="badge bg-success me-1">${count} ghế</span> <span class="fw-bold text-dark">${seatCodes.join(", ")}</span>`;
            totalPriceEl.innerText = displayTotal.toLocaleString("vi-VN") + " VNĐ";
            
            // Kích hoạt nút tiếp tục
            if (btnContinue) {
                if (booking && booking.bookingCode) {
                    btnContinue.classList.remove("disabled");
                    btnContinue.innerHTML = `<i class="bi bi-credit-card-2-front-fill me-1"></i> Tiếp tục thanh toán ${count} vé (${displayTotal.toLocaleString("vi-VN")} đ) &rarr;`;
                    btnContinue.setAttribute("href", `/Ticket/Payment?bookingCode=${encodeURIComponent(booking.bookingCode)}&tripId=${encodeURIComponent(tripId)}`);
                } else {
                    btnContinue.classList.add("disabled");
                    btnContinue.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang đồng bộ giữ chỗ...`;
                    btnContinue.setAttribute("href", "javascript:void(0);");
                }
            }
        } else {
            // Chưa chọn ghế nào hoặc đã nhả hết ghế
            delete tripBookingData[tripId];
            seatListEl.innerText = "Chưa chọn ghế nào";
            totalPriceEl.innerText = "0 VNĐ";
            
            // Vô hiệu hóa nút tiếp tục
            if (btnContinue) {
                btnContinue.classList.add("disabled");
                btnContinue.innerHTML = `Vui lòng chọn ghế trước khi tiếp tục &rarr;`;
                btnContinue.setAttribute("href", "javascript:void(0);");
            }
        }
    }

    // Lắng nghe sự kiện thay đổi trạm đón / trả để cập nhật ngay vào nút đặt vé
    document.querySelectorAll(".select-boarding-point, .select-dropoff-point").forEach(select => {
        select.addEventListener("change", function () {
            const tripId = this.getAttribute("data-trip-id");
            if (tripId) {
                updateSeatSummary(tripId);
            }
        });
    });

    // 5. Hiển thị thông báo nhanh (Toast alert) trên accordion
    function showSeatAlert(tripId, message, type) {
        const accordion = document.getElementById("seatAccordion-" + tripId);
        if (!accordion) return;

        let alertBox = accordion.querySelector(".seat-selection-alert");
        if (!alertBox) {
            alertBox = document.createElement("div");
            alertBox.className = "seat-selection-alert alert alert-" + type + " py-2 px-3 small rounded-3 mt-2 text-center shadow-xs";
            const summaryBox = accordion.querySelector(".p-3.bg-white.rounded-3.border");
            if (summaryBox && summaryBox.parentNode) {
                summaryBox.parentNode.insertBefore(alertBox, summaryBox);
            }
        } else {
            alertBox.className = "seat-selection-alert alert alert-" + type + " py-2 px-3 small rounded-3 mt-2 text-center shadow-xs";
        }

        alertBox.innerHTML = `<i class="bi bi-info-circle-fill me-1"></i> ${message}`;
        alertBox.style.display = "block";

        // Tự động ẩn sau 3.5 giây
        setTimeout(() => {
            if (alertBox) alertBox.style.display = "none";
        }, 3500);
    }

    // 6. Tự động viết hoa chữ cái đầu (Title Case: "hà nội" -> "Hà Nội", "thái nguyên" -> "Thái Nguyên")
    const locationInputs = document.querySelectorAll('input[name="from"], input[name="to"]');
    locationInputs.forEach(input => {
        input.addEventListener("blur", function () {
            if (this.value) {
                this.value = formatTitleCase(this.value);
            }
        });
    });

    function formatTitleCase(text) {
        if (!text) return "";
        return text.trim().toLowerCase().replace(/(^|\s)\S/g, function (letter) {
            return letter.toUpperCase();
        });
    }
});
