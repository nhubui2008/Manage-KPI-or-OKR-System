(function () {
    "use strict";

    function setValue(id, value) {
        var element = document.getElementById(id);
        if (element) element.value = value || "";
    }

    document.addEventListener("DOMContentLoaded", function () {
        var editModalElement = document.getElementById("evaluationEditModal");
        var editButtons = document.querySelectorAll("[data-evaluation-edit]");

        editButtons.forEach(function (button) {
            button.addEventListener("click", function () {
                setValue("evaluationEditId", button.dataset.id);
                setValue("evaluationEditName", button.dataset.name);
                setValue("evaluationEditType", button.dataset.type);
                setValue("evaluationEditStart", button.dataset.start);
                setValue("evaluationEditEnd", button.dataset.end);
                setValue("evaluationEditStatus", button.dataset.statusId);

                if (editModalElement && window.bootstrap) {
                    window.bootstrap.Modal.getOrCreateInstance(editModalElement).show();
                }
            });
        });

        var editForm = document.getElementById("evaluationEditForm");
        if (editForm) {
            editForm.addEventListener("submit", function (event) {
                var startInput = document.getElementById("evaluationEditStart");
                var endInput = document.getElementById("evaluationEditEnd");
                if (!startInput || !endInput) return;

                endInput.setCustomValidity("");
                if (startInput.value && endInput.value && endInput.value < startInput.value) {
                    event.preventDefault();
                    endInput.setCustomValidity("Ngày kết thúc không thể trước ngày bắt đầu.");
                    endInput.reportValidity();
                }
            });
        }

        document.querySelectorAll("[data-evaluation-delete]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                var periodName = form.dataset.periodName || "kỳ đánh giá này";
                if (!window.confirm("Vô hiệu hóa " + periodName + "?")) {
                    event.preventDefault();
                }
            });
        });
    });
})();
