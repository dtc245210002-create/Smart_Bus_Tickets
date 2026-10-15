// =========================================================================
// SmartBus Go - Xử lý sự kiện chọn/hủy chọn ghế & lưu giữ chỗ vào Database
// Đồng bộ trạng thái ghế thời gian thực giữa các tài khoản người dùng
// =========================================================================

document.addEventListener("DOMContentLoaded", function () {
    const MAX_SEATS = 6; // Giới hạn số ghế tối đa mỗi lần đặt
    let pollInterval = null;
    let activePollingTripId = null;

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

    // 2. Xử lý sự kiện click chọn / hủy chọn ghế trực tiếp và mượt mà
    const seatBoxes = document.querySelectorAll(".sbg-seat-box");
    seatBoxes.forEach(seat => {
        if (seat.classList.contains("booked")) {
            seat.style.pointerEvents = "none";
        }
        seat.addEventListener("click", function () {
            const tripId = this.getAttribute("data-trip-id");
            const seatCode = this.getAttribute("data-code");

            // Nếu ghế đã bán -> Không cho chọn
            if (this.classList.contains("booked")) {
                showSeatAlert(tripId, `Ghế ${seatCode} đã bán hoặc đang được người khác giữ. Vui lòng chọn ghế khác!`, "warning");
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

            // Chọn hoặc bỏ chọn ghế ngay lập tức, giữ nguyên trạng thái không bị tắt
            if (isSelecting) {
                this.classList.add("selected");
                this.classList.remove("booked");
            } else {
                this.classList.remove("selected");
            }

            updateSeatSummary(tripId);
        });
    });

    // 3. Cơ chế Polling thời gian thực: Cập nhật trạng thái ghế đã bán từ CSDL
    function startSeatPolling(tripId) {
        stopSeatPolling();
        activePollingTripId = tripId;
        syncSeatStatuses(tripId);
        pollInterval = setInterval(() => {
            if (activePollingTripId) {
                syncSeatStatuses(activePollingTripId);
            }
        }, 5000); // 5 giây đồng bộ 1 lần
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

        try {
            const res = await fetch(`/api/bookings/trip-seat-status?tripId=${tripId}`);
            if (!res.ok) return;
            const data = await res.json();
            if (!data.success) return;

            const booked = new Set((data.bookedSeats || []).map(s => s.toUpperCase()));

            const seatBoxes = accordion.querySelectorAll(".sbg-seat-box");
            seatBoxes.forEach(seat => {
                const code = (seat.getAttribute("data-code") || "").toUpperCase();

                if (booked.has(code)) {
                    // Ghế đã bán thật trong CSDL
                    seat.classList.add("booked");
                    if (seat.classList.contains("selected")) {
                        seat.classList.remove("selected");
                        showSeatAlert(tripId, `Ghế ${code} vừa được người khác hoàn tất đặt. Vui lòng chọn ghế khác!`, "warning");
                    }
                    seat.style.pointerEvents = "none";
                    seat.setAttribute("title", `Ghế ${code} - Đã bán`);
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
            seatListEl.innerHTML = `<span class="badge bg-success me-1">${count} ghế</span> <span class="fw-bold text-dark">${seatCodes.join(", ")}</span>`;
            totalPriceEl.innerText = totalPrice.toLocaleString("vi-VN") + " VNĐ";
            
            // Kích hoạt nút tiếp tục
            if (btnContinue) {
                btnContinue.classList.remove("disabled");
                btnContinue.innerHTML = `<i class="bi bi-credit-card-2-front-fill me-1"></i> Tiếp tục thanh toán ${count} vé (${totalPrice.toLocaleString("vi-VN")} đ) &rarr;`;
                
                // Đọc thông tin hành trình từ data attribute & dropdowns
                const from = btnContinue.getAttribute("data-from") || "";
                const to = btnContinue.getAttribute("data-to") || "";
                const date = btnContinue.getAttribute("data-date") || "";
                const depTime = btnContinue.getAttribute("data-deptime") || "";
                const arrTime = btnContinue.getAttribute("data-arrtime") || "";
                const busType = btnContinue.getAttribute("data-bustype") || "";
                const licensePlate = btnContinue.getAttribute("data-licenseplate") || "";
                const routeName = btnContinue.getAttribute("data-routename") || "";
                const routeCode = btnContinue.getAttribute("data-routecode") || "";
                const driverName = btnContinue.getAttribute("data-drivername") || "";
                const driverPhone = btnContinue.getAttribute("data-driverphone") || "";
                const estMinutes = btnContinue.getAttribute("data-estminutes") || "";
                const distance = btnContinue.getAttribute("data-distance") || "";

                const boardingSelect = accordion.querySelector(".select-boarding-point");
                const dropoffSelect = accordion.querySelector(".select-dropoff-point");
                const boarding = boardingSelect ? boardingSelect.value : "";
                const dropoff = dropoffSelect ? dropoffSelect.value : "";

                // Lưu Cookie & localStorage để giữ trạng thái
                try {
                    const cookieMaxAge = 604800; // 7 ngày
                    document.cookie = `sbg_last_from=${encodeURIComponent(from)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_to=${encodeURIComponent(to)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_date=${encodeURIComponent(date)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_seats=${encodeURIComponent(seatCodes.join(","))}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_total=${totalPrice}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_boarding=${encodeURIComponent(boarding)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_dropoff=${encodeURIComponent(dropoff)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_bustype=${encodeURIComponent(busType)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_licenseplate=${encodeURIComponent(licensePlate)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_driver=${encodeURIComponent(driverName)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_driverphone=${encodeURIComponent(driverPhone)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_routecode=${encodeURIComponent(routeCode)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_routename=${encodeURIComponent(routeName)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_deptime=${encodeURIComponent(depTime)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_arrtime=${encodeURIComponent(arrTime)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_estminutes=${encodeURIComponent(estMinutes)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_distance=${encodeURIComponent(distance)}; path=/; max-age=${cookieMaxAge}`;
                    document.cookie = `sbg_last_tripid=${encodeURIComponent(tripId)}; path=/; max-age=${cookieMaxAge}`;

                    localStorage.setItem("sbg_booking_draft", JSON.stringify({
                        tripId, seats: seatCodes, total: totalPrice, from, to, date, boarding, dropoff, depTime, arrTime, busType, licensePlate, driverName, driverPhone, routeCode, routeName, estMinutes, distance
                    }));
                } catch (e) { }
            }
        } else {
            // Chưa chọn ghế nào
            seatListEl.innerText = "Chưa chọn ghế nào";
            totalPriceEl.innerText = "0 VNĐ";
            
            // Vô hiệu hóa nút tiếp tục
            if (btnContinue) {
                btnContinue.classList.add("disabled");
                btnContinue.innerHTML = `Vui lòng chọn ghế trước khi tiếp tục &rarr;`;
            }
        }
    }

    // 4b. Xử lý sự kiện bấm "Tiếp tục thanh toán" -> Truy vấn CSDL kiểm tra ai đặt trước
    document.querySelectorAll(".btn-continue-booking").forEach(btn => {
        btn.addEventListener("click", async function (e) {
            e.preventDefault();
            if (this.classList.contains("disabled")) return;

            const tripId = this.getAttribute("data-trip-id");
            const accordion = document.getElementById("seatAccordion-" + tripId);
            if (!accordion) return;

            const selectedSeats = accordion.querySelectorAll(".sbg-seat-box.selected");
            const seatCodes = Array.from(selectedSeats).map(s => s.getAttribute("data-code"));

            if (seatCodes.length === 0) {
                showSeatAlert(tripId, "Vui lòng chọn ít nhất 1 ghế trước khi thanh toán!", "warning");
                return;
            }

            const originalHtml = this.innerHTML;
            this.classList.add("disabled");
            this.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Đang kiểm tra ghế trong CSDL...';

            const from = this.getAttribute("data-from") || "";
            const to = this.getAttribute("data-to") || "";
            const date = this.getAttribute("data-date") || "";
            const depTime = this.getAttribute("data-deptime") || "";
            const arrTime = this.getAttribute("data-arrtime") || "";
            const busType = this.getAttribute("data-bustype") || "";
            const licensePlate = this.getAttribute("data-licenseplate") || "";
            const routeName = this.getAttribute("data-routename") || "";
            const routeCode = this.getAttribute("data-routecode") || "";
            const driverName = this.getAttribute("data-drivername") || "";
            const driverPhone = this.getAttribute("data-driverphone") || "";
            const estMinutes = this.getAttribute("data-estminutes") || "";
            const distance = this.getAttribute("data-distance") || "";

            const boardingSelect = accordion.querySelector(".select-boarding-point");
            const dropoffSelect = accordion.querySelector(".select-dropoff-point");
            const boarding = boardingSelect ? boardingSelect.value : "";
            const dropoff = dropoffSelect ? dropoffSelect.value : "";

            let totalPrice = 0;
            selectedSeats.forEach(s => {
                totalPrice += parseFloat(s.getAttribute("data-price") || 150000);
            });

            const sessionId = getOrCreateSessionId();
            const userId = window.sbgCurrentUserId || null;

            try {
                // Truy vấn trực tiếp xuống CSDL SQL Server kiểm tra ai đặt trước
                const res = await fetch("/api/bookings/create-booking", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        tripId: parseInt(tripId),
                        seatNumbers: seatCodes,
                        totalPrice: totalPrice,
                        sessionId: sessionId,
                        userId: userId,
                        from: from,
                        to: to,
                        date: date,
                        boarding: boarding,
                        dropoff: dropoff,
                        depTime: depTime,
                        arrTime: arrTime,
                        busType: busType,
                        licensePlate: licensePlate,
                        routeName: routeName,
                        routeCode: routeCode,
                        driverName: driverName,
                        driverPhone: driverPhone,
                        estMinutes: estMinutes,
                        distance: distance
                    })
                });

                const data = await res.json();

                if (!res.ok || !data.success) {
                    // GHẾ ĐÃ BỊ NGƯỜI KHÁC ĐẶT TRƯỚC -> THÔNG BÁO NGAY CHO NGƯỜI ĐẶT SAU
                    const conflicted = data.conflictedSeats || [];
                    const msg = data.message || `Ghế ${seatCodes.join(", ")} đã bị người khác đặt trước. Vui lòng chọn ghế khác!`;
                    showSeatAlert(tripId, msg, "danger");

                    // Đánh dấu các ghế bị trùng thành booked
                    conflicted.forEach(code => {
                        const seatEl = accordion.querySelector(`.sbg-seat-box[data-code="${code}"]`);
                        if (seatEl) {
                            seatEl.classList.remove("selected");
                            seatEl.classList.add("booked");
                            seatEl.style.pointerEvents = "none";
                            seatEl.setAttribute("title", `Ghế ${code} - Đã bị người khác đặt trước`);
                        }
                    });

                    updateSeatSummary(tripId);
                    this.innerHTML = originalHtml;
                    this.classList.remove("disabled");
                    return;
                }

                // GHẾ CÒN TRỐNG -> ĐẶT THÀNH CÔNG, HIỆN RA CỔNG THANH TOÁN
                showSeatAlert(tripId, "Đặt chỗ thành công! Đang chuyển đến cổng thanh toán...", "success");
                setTimeout(() => {
                    window.location.href = data.redirectUrl || `/Ticket/Payment?tripId=${tripId}&seats=${encodeURIComponent(seatCodes.join(","))}&total=${totalPrice}`;
                }, 350);

            } catch (err) {
                console.error("Lỗi khi kiểm tra ghế và tạo đơn:", err);
                showSeatAlert(tripId, "Đang chuyển đến cổng thanh toán...", "info");
                const params = new URLSearchParams();
                params.set("tripId", tripId);
                params.set("seats", seatCodes.join(","));
                params.set("total", totalPrice);
                if (from) params.set("from", from);
                if (to) params.set("to", to);
                if (date) params.set("date", date);
                if (boarding) params.set("boarding", boarding);
                if (dropoff) params.set("dropoff", dropoff);
                if (depTime) params.set("depTime", depTime);
                if (busType) params.set("busType", busType);
                if (licensePlate) params.set("licensePlate", licensePlate);
                window.location.href = `/Ticket/Payment?${params.toString()}`;
            }
        });
    });

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
