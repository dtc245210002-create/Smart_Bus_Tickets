// SmartBus Go - Interactive Inline Seat Selection (Accordion)
document.addEventListener("DOMContentLoaded", function () {
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
                document.querySelectorAll(".sbg-seat-accordion").forEach(acc => acc.classList.remove("show"));
                document.querySelectorAll(".btn-toggle-seat-map").forEach(b => b.innerHTML = '<i class="bi bi-chevron-down me-1"></i> Chọn chỗ');

                accordion.classList.add("show");
                this.innerHTML = '<i class="bi bi-chevron-up me-1"></i> Đóng lại';
            }
        });
    });

    const seatBoxes = document.querySelectorAll(".sbg-seat-box:not(.booked)");
    seatBoxes.forEach(seat => {
        seat.addEventListener("click", function () {
            const tripId = this.getAttribute("data-trip-id");
            this.classList.toggle("selected");
            updateSeatSummary(tripId);
        });
    });

    function updateSeatSummary(tripId) {
        const accordion = document.getElementById("seatAccordion-" + tripId);
        if (!accordion) return;

        const selectedSeats = accordion.querySelectorAll(".sbg-seat-box.selected");
        const seatCodes = [];
        let totalPrice = 0;

        selectedSeats.forEach(s => {
            seatCodes.push(s.getAttribute("data-code"));
            totalPrice += parseFloat(s.getAttribute("data-price") || 150000);
        });

        const seatListEl = accordion.querySelector(".selected-seats-list");
        const totalPriceEl = accordion.querySelector(".selected-total-price");
        const btnContinue = accordion.querySelector(".btn-continue-booking");

        if (selectedSeats.length > 0) {
            seatListEl.innerText = seatCodes.join(", ");
            totalPriceEl.innerText = totalPrice.toLocaleString("vi-VN") + " VNĐ";
            btnContinue.classList.remove("disabled");
        } else {
            seatListEl.innerText = "Chưa chọn ghế nào";
            totalPriceEl.innerText = "0 VNĐ";
            btnContinue.classList.add("disabled");
        }
    }
});
