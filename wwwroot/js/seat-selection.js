// =========================================================================
// SmartBus Go - Xử lý sự kiện chọn/hủy chọn ghế & lưu giữ chỗ vào Database
// Đồng bộ trạng thái ghế thời gian thực giữa các tài khoản người dùng
// =========================================================================

document.addEventListener("DOMContentLoaded", function () {
    const MAX_SEATS = 6; // Giới hạn số ghế tối đa mỗi lần đặt
    let pollInterval = null;
    let activePollingTripId = null;
    const tripBookingData = {}; // Lưu trữ thông tin bookingCode = holdId thật từ server cho từng chuyến
    const tripMutationGen = {}; // Bộ đếm thế hệ thao tác (mutation generation) chống race condition khi polling
    const inFlightMutations = {}; // Theo dõi số lượng mutation (hold/release) đang in-flight theo từng tripId

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

        // Tăng thế hệ thao tác và đánh dấu có mutation đang in-flight
        tripMutationGen[tripId] = (tripMutationGen[tripId] || 0) + 1;
        inFlightMutations[tripId] = (inFlightMutations[tripId] || 0) + 1;

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
                } else if (res.status === 503 || !res.ok || !data || !data.success) {
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
                const remSec = (data.data && typeof data.data.remainingSeconds === "number") ? data.data.remainingSeconds : null;
                const serverHoldId = (data.data && data.data.holdId) ? data.data.holdId : null;
                const serverTotal = (data.data && data.data.totalPrice != null) ? data.data.totalPrice : null;

                // Lấy danh sách heldSeats chính thức từ response server (không suy từ DOM)
                const serverHeldSeats = (data.data && Array.isArray(data.data.heldSeats) && data.data.heldSeats.length > 0)
                    ? data.data.heldSeats.map(s => (s || "").toUpperCase())
                    : [seatCode.toUpperCase()];

                // Chỉ ghi nhận giữ chỗ và báo giá khi có holdId và remainingSeconds > 0
                if (serverHoldId && remSec !== null && remSec > 0) {
                    tripBookingData[tripId] = {
                        bookingCode: serverHoldId,
                        totalPrice: serverTotal,
                        remainingSeconds: remSec,
                        heldSeats: serverHeldSeats
                    };
                } else {
                    delete tripBookingData[tripId];
                }
                const displayMins = remSec ? Math.max(1, Math.round(remSec / 60)) : 10;
                showSeatAlert(tripId, `Đã giữ ghế ${seatCode} thành công (thời hạn ${displayMins} phút)!`, "success");
                // Lấy fresh GET snapshot để đồng bộ đầy đủ toàn bộ ghế giữ và báo giá tổng
                syncSeatStatuses(tripId);
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

                    // Tăng thế hệ thao tác để hủy các poll cũ
                    tripMutationGen[tripId] = (tripMutationGen[tripId] || 0) + 1;

                    // VÔ HIỆU HÓA BÁO GIÁ CŨ: Khi nhả một ghế, quote tổng tiền cũ (totalPrice) không còn hiệu lực
                    if (tripBookingData[tripId]) {
                        delete tripBookingData[tripId].totalPrice;
                        if (Array.isArray(tripBookingData[tripId].heldSeats)) {
                            tripBookingData[tripId].heldSeats = tripBookingData[tripId].heldSeats.filter(
                                s => s !== seatCode.toUpperCase()
                            );
                        }
                        if (!tripBookingData[tripId].heldSeats || tripBookingData[tripId].heldSeats.length === 0) {
                            delete tripBookingData[tripId];
                        }
                    }

                    // Lấy fresh GET snapshot để khôi phục báo giá chính thức cho các ghế còn lại
                    syncSeatStatuses(tripId);
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
            // TĂNG GENERATION KHI KẾT THÚC THAO TÁC:
            // Vô hiệu hóa mọi poll được bắt đầu trong lúc thao tác này đang diễn ra (in-flight)
            tripMutationGen[tripId] = (tripMutationGen[tripId] || 0) + 1;
            inFlightMutations[tripId] = Math.max(0, (inFlightMutations[tripId] || 1) - 1);

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
        const expectedGen = tripMutationGen[tripId] || 0;

        try {
            const res = await fetch(`/api/bookings/trip-seat-status?tripId=${tripId}&sessionId=${encodeURIComponent(sessionId)}&userId=${userId}`);

            const isStale = ((tripMutationGen[tripId] || 0) !== expectedGen) || ((inFlightMutations[tripId] || 0) > 0);
            if (isStale) {
                return; // Loại bỏ hoàn toàn response cũ, không để poll lỗi/chậm đè lên thao tác mới
            }

            if (!res.ok) {
                if (tripBookingData[tripId]) {
                    delete tripBookingData[tripId].totalPrice;
                }
                updateSeatSummary(tripId);
                return;
            }
            const data = await res.json();
            if (!data.success) {
                if (tripBookingData[tripId]) {
                    delete tripBookingData[tripId].totalPrice;
                }
                updateSeatSummary(tripId);
                return;
            }

            // Kiểm tra lại sau khi await json()
            if (((tripMutationGen[tripId] || 0) !== expectedGen) || ((inFlightMutations[tripId] || 0) > 0)) {
                return;
            }

            const booked = new Set((data.bookedSeats || []).map(s => s.toUpperCase()));
            const myHeld = new Set((data.myHeldSeats || []).map(s => s.toUpperCase()));
            const myHeldList = (data.myHeldSeats || []).map(s => s.toUpperCase());

            // 1. Phục hồi hoặc cập nhật dữ liệu giữ chỗ cho tripId từ server chính thức
            if (data.heldBookingCode) {
                const ttl = (typeof data.remainingSeconds === "number") ? data.remainingSeconds : 0;
                // Bắt buộc TTL > 0 và có giá từ server (không dùng || 600, không gia hạn ngầm)
                if (ttl > 0 && typeof data.heldTotalPrice === "number") {
                    tripBookingData[tripId] = {
                        bookingCode: data.heldBookingCode,
                        totalPrice: data.heldTotalPrice,
                        remainingSeconds: ttl,
                        heldSeats: myHeldList
                    };
                } else {
                    delete tripBookingData[tripId];
                }
            } else {
                // Chưa có heldBookingCode từ server: Xóa quote cũ để không tự tính toán hay suy đoán
                if (tripBookingData[tripId]) {
                    delete tripBookingData[tripId].totalPrice;
                }
            }

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
            // Chỉ xóa quote nếu poll KHÔNG BỊ STALE bởi mutation mới hơn
            const isStale = ((tripMutationGen[tripId] || 0) !== expectedGen) || ((inFlightMutations[tripId] || 0) > 0);
            if (!isStale) {
                if (tripBookingData[tripId]) {
                    delete tripBookingData[tripId].totalPrice;
                }
                updateSeatSummary(tripId);
            }
        }
    }

    // 4. Hàm tính tổng tiền và cập nhật thông tin ghế đã chọn
    function updateSeatSummary(tripId) {
        const accordion = document.getElementById("seatAccordion-" + tripId);
        if (!accordion) return;

        const selectedSeats = accordion.querySelectorAll(".sbg-seat-box.selected");
        const seatCodes = [];
        let baseTotalPrice = 0;

        selectedSeats.forEach(s => {
            const code = (s.getAttribute("data-code") || "").toUpperCase();
            const price = parseFloat(s.getAttribute("data-price") || 150000);
            seatCodes.push(code);
            baseTotalPrice += price;
        });

        const seatListEl = accordion.querySelector(".selected-seats-list");
        const totalPriceEl = accordion.querySelector(".selected-total-price");
        const btnContinue = accordion.querySelector(".btn-continue-booking");

        if (selectedSeats.length > 0) {
            const count = selectedSeats.length;
            const booking = tripBookingData[tripId];

            // Kiểm tra báo giá từ server: Chỉ hợp lệ khi booking có totalPrice VÀ tập ghế giữ khớp 100% với danh sách đang chọn
            const isQuoteValid = booking &&
                                 typeof booking.totalPrice === "number" &&
                                 Array.isArray(booking.heldSeats) &&
                                 booking.heldSeats.length === count &&
                                 seatCodes.every(code => booking.heldSeats.map(s => s.toUpperCase()).includes(code));

            if (seatListEl) {
                seatListEl.innerHTML = `<span class="badge bg-success me-1">${count} ghế</span> <span class="fw-bold text-dark">${seatCodes.join(", ")}</span>`;
            }

            if (isQuoteValid) {
                // Báo giá chính thức từ server đã xác nhận
                totalPriceEl.innerText = booking.totalPrice.toLocaleString("vi-VN") + " VNĐ";
            } else {
                // Báo giá cũ đã bị vô hiệu hóa (sau khi nhả ghế hoặc reload) -> không suy giá client là giá server
                if (booking) {
                    delete booking.totalPrice;
                }
                totalPriceEl.innerHTML = `${baseTotalPrice.toLocaleString("vi-VN")} VNĐ <small class="text-muted fw-normal">(tạm tính)</small>`;
            }

            // Cập nhật nút tiếp tục
            if (btnContinue) {
                if (booking && booking.bookingCode && isQuoteValid) {
                    // Đã có mã giữ chỗ và báo giá chính thức từ máy chủ
                    btnContinue.classList.remove("disabled");
                    btnContinue.innerHTML = `<i class="bi bi-credit-card-2-front-fill me-1"></i> Tiếp tục thanh toán ${count} vé (${booking.totalPrice.toLocaleString("vi-VN")} đ) &rarr;`;
                    btnContinue.setAttribute("href", `/Ticket/Payment?bookingCode=${encodeURIComponent(booking.bookingCode)}&tripId=${encodeURIComponent(tripId)}`);
                } else if (booking && booking.bookingCode && !isQuoteValid) {
                    // Có mã giữ chỗ nhưng số ghế đã đổi sau khi nhả ghế -> chờ đối chiếu quote từ server
                    btnContinue.classList.add("disabled");
                    btnContinue.innerHTML = `<span class="spinner-border spinner-border-sm me-1" role="status"></span> Đang chờ đối chiếu giá từ máy chủ...`;
                    btnContinue.setAttribute("href", "javascript:void(0);");
                } else {
                    // Trường hợp reload trang chưa có mã giữ chỗ thật -> thông báo rõ ràng, không treo spinner vô hạn
                    btnContinue.classList.add("disabled");
                    btnContinue.innerHTML = `<i class="bi bi-arrow-repeat me-1"></i> Bấm vào ghế để làm mới giữ chỗ &rarr;`;
                    btnContinue.setAttribute("href", "javascript:void(0);");
                }
            }
        } else {
            // Không còn ghế nào được chọn
            delete tripBookingData[tripId];
            try {
                sessionStorage.removeItem("sbg_hold_" + tripId);
            } catch (e) {}
            if (seatListEl) seatListEl.innerText = "Chưa chọn ghế nào";
            if (totalPriceEl) totalPriceEl.innerText = "0 VNĐ";

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
