// SmartBus Go - Client-side Scripts
$(function () {
    // 1. Tự động cấu hình bộ chọn ngày chuẩn định dạng Việt Nam: Ngày / Tháng / Năm (dd/MM/yyyy)
    if (typeof flatpickr !== 'undefined') {
        // Thiết lập ngôn ngữ tiếng Việt mặc định
        if (flatpickr.l10ns && flatpickr.l10ns.vn) {
            flatpickr.localize(flatpickr.l10ns.vn);
        }

        $('.sbg-datepicker').each(function () {
            var $originalInput = $(this);
            var initialValue = $originalInput.val();

            // Khởi tạo Flatpickr với định dạng hiển thị: dd/MM/yyyy (Ngày trước, tháng sau, năm sau cùng)
            var fp = flatpickr(this, {
                altInput: true,
                altFormat: "d/m/Y",
                dateFormat: "Y-m-d",
                defaultDate: initialValue || "today",
                minDate: "today",
                disableMobile: true, // Đồng bộ giao diện và định dạng dd/MM/yyyy trên cả máy tính lẫn điện thoại
                altInputClass: "form-control border-start-0 border-end-0 fw-bold bg-white text-dark sbg-alt-datepicker",
                allowInput: true, // Cho phép người dùng nhập trực tiếp dd/MM/yyyy nếu muốn
                onReady: function (selectedDates, dateStr, instance) {
                    if (instance.altInput) {
                        instance.altInput.setAttribute("placeholder", "dd/mm/yyyy");
                        instance.altInput.style.cursor = "pointer";
                        // Cho phép người dùng click bất cứ đâu trên ô để mở lịch
                        $(instance.altInput).on('click', function () {
                            instance.open();
                        });
                    }
                }
            });

            // Gán sự kiện click vào icon lịch (input-group-text) kế bên để mở popup lịch
            $originalInput.closest('.input-group').find('.sbg-calendar-trigger, .input-group-text').css('cursor', 'pointer').on('click', function () {
                fp.open();
            });
        });
    }
});
