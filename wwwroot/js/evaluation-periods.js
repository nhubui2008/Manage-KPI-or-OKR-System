(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-evaluation-delete]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                var periodName = form.dataset.periodName || "kỳ đánh giá này";
                if (!window.confirm("Vô hiệu hóa " + periodName + "?")) {
                    event.preventDefault();
                }
            });
        });

        document.querySelectorAll("[data-evaluation-lifecycle]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                if (!window.confirm(form.dataset.message || "Xác nhận thay đổi trạng thái kỳ đánh giá?")) {
                    event.preventDefault();
                }
            });
        });
    });
})();
