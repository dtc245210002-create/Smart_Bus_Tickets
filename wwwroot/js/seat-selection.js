// =========================================================================
// SmartBus Go - Xử lý sự kiện chọn/hủy chọn ghế & tính tổng tiền tự động
// =========================================================================

document.addEventListener("DOMContentLoaded", function () {
    const MAX_SEATS = 6; // Giới hạn số ghế tối đa mỗi lần đặt

    // 1. Xử lý mở / đóng Accordion sơ đồ chọn ghế
    const toggleButtons = document.querySelectorAll(".btn-toggle-seat-map");
    toggleButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            const tripId = this.getAttribute("data-trip-id");
            const accordion = document.getElementById("seatAccordion-" + tripId);
            if (!accordion) return;

            if (accordion.classList.contains("show")) {
                accordion.classList.remove("show");
                this.innerHTML = '<i class="bi bi-chevron-down me-1"></i> Chọn chỗ';
            } else {
                // Đóng các accordion khác trước khi mở cái mới
                document.querySelectorAll(".sbg-seat-accordion").forEach(acc => acc.classList.remove("show"));
                document.querySelectorAll(".btn-toggle-seat-map").forEach(b => {
                    b.innerHTML = '<i class="bi bi-chevron-down me-1"></i> Chọn chỗ';
                });

                accordion.classList.add("show");
                this.innerHTML = '<i class="bi bi-chevron-up me-1"></i> Đóng lại';
                
                // Cuộn nhẹ tới accordion
                accordion.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            }
        });
    });

    // 2. Xử lý sự kiện click chọn / hủy chọn ghế
    const seatBoxes = document.querySelectorAll(".sbg-seat-box");
    seatBoxes.forEach(seat => {
        seat.addEventListener("click", function () {
            const tripId = this.getAttribute("data-trip-id");
            const seatCode = this.getAttribute("data-code");

            // Nếu ghế đã bán -> Thông báo không cho chọn
            if (this.classList.contains("booked")) {
                showSeatAlert(tripId, `Ghế ${seatCode} đã có người đặt, vui lòng chọn ghế khác!`, "warning");
                return;
            }

            const accordion = document.getElementById("seatAccordion-" + tripId);
            if (!accordion) return;

            const currentSelected = accordion.querySelectorAll(".sbg-seat-box.selected");

            // Kiểm tra trạng thái: chọn hay hủy chọn
            if (this.classList.contains("selected")) {
                // HỦY CHỌN GHẾ
                this.classList.remove("selected");
            } else {
                // CHỌN GHẾ MỚI (Kiểm tra số lượng tối đa)
                if (currentSelected.length >= MAX_SEATS) {
                    showSeatAlert(tripId, `Bạn chỉ được chọn tối đa ${MAX_SEATS} ghế cho một lần đặt vé!`, "danger");
                    return;
                }
                this.classList.add("selected");
            }

            // Tự động tính toán lại tổng tiền và cập nhật giao diện
            updateSeatSummary(tripId);
        });
    });

    // 3. Hàm tính tổng tiền và cập nhật thông tin ghế đã chọn
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
                btnContinue.innerHTML = `<i class="bi bi-check-circle-fill me-1"></i> Tiếp tục đặt ${count} vé (${totalPrice.toLocaleString("vi-VN")} đ) &rarr;`;
                
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

                // Tạo URL với đầy đủ thông tin chuyến xe khách hàng đã chọn
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
                if (arrTime) params.set("arrTime", arrTime);
                if (busType) params.set("busType", busType);
                if (licensePlate) params.set("licensePlate", licensePlate);
                if (routeName) params.set("routeName", routeName);
                if (routeCode) params.set("routeCode", routeCode);
                if (driverName) params.set("driverName", driverName);
                if (driverPhone) params.set("driverPhone", driverPhone);
                if (estMinutes) params.set("estMinutes", estMinutes);
                if (distance) params.set("distance", distance);

                btnContinue.setAttribute("href", `/Ticket/Detail?${params.toString()}`);

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

    // Lắng nghe sự kiện thay đổi trạm đón / trả để cập nhật ngay vào nút đặt vé
    document.querySelectorAll(".select-boarding-point, .select-dropoff-point").forEach(select => {
        select.addEventListener("change", function () {
            const tripId = this.getAttribute("data-trip-id");
            if (tripId) {
                updateSeatSummary(tripId);
            }
        });
    });

    // 4. Hiển thị thông báo nhanh (Toast alert) trên accordion
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

    // 5. Tự động viết hoa chữ cái đầu (Title Case: "hà nội" -> "Hà Nội", "thái nguyên" -> "Thái Nguyên")
    const locationInputs = document.querySelectorAll('input[name="from"], input[name="to"]');
    locationInputs.forEach(input => {
        // Viết hoa khi người dùng nhập xong và chuyển ô (blur) hoặc gửi form
        input.addEventListener("blur", function () {
            if (this.value) {
                this.value = formatTitleCase(this.value);
            }
        });
        
        // Hỗ trợ viết hoa chữ cái đầu tiên ngay khi gõ
        input.addEventListener("input", function () {
            // CSS text-transform: capitalize đã làm hiển thị viết hoa ngay lập tức
        });
    });

    function formatTitleCase(text) {
        if (!text) return "";
        return text.trim().toLowerCase().replace(/(^|\s)\S/g, function (letter) {
            return letter.toUpperCase();
        });
    }
});
