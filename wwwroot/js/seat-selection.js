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
                
                // Cập nhật link điều hướng kèm danh sách ghế đã chọn
                const currentHref = btnContinue.getAttribute("data-base-href") || btnContinue.getAttribute("href") || "/Ticket/Detail";
                btnContinue.setAttribute("data-base-href", currentHref.split("?")[0]);
                btnContinue.setAttribute("href", `${currentHref.split("?")[0]}?tripId=${tripId}&seats=${encodeURIComponent(seatCodes.join(","))}&total=${totalPrice}`);
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
