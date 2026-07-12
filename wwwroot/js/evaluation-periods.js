(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var confirmModalElement = document.getElementById("evaluationConfirmModal");
        var confirmTitle = document.getElementById("evaluationConfirmTitle");
        var confirmMessage = document.getElementById("evaluationConfirmMessage");
        var confirmSubmit = document.getElementById("evaluationConfirmSubmit");
        var pendingForm = null;

        document.querySelectorAll("[data-evaluation-confirm]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                if (!confirmModalElement || !confirmSubmit || !window.bootstrap) return;

                event.preventDefault();
                pendingForm = form;
                confirmTitle.textContent = form.dataset.confirmTitle || "Xác nhận thay đổi";
                confirmMessage.textContent = form.dataset.confirmMessage || "Bạn có chắc muốn tiếp tục?";
                confirmSubmit.classList.toggle("evaluation-primary-action--danger", form.dataset.confirmTone === "danger");
                confirmSubmit.classList.toggle("evaluation-primary-action--warning", form.dataset.confirmTone === "warning");
                window.bootstrap.Modal.getOrCreateInstance(confirmModalElement).show();
            });
        });

        if (confirmSubmit) {
            confirmSubmit.addEventListener("click", function () {
                if (!pendingForm) return;
                var formToSubmit = pendingForm;
                pendingForm = null;
                HTMLFormElement.prototype.submit.call(formToSubmit);
            });
        }

        if (confirmModalElement) {
            confirmModalElement.addEventListener("hidden.bs.modal", function () {
                pendingForm = null;
            });
        }

        var previewRoot = document.querySelector("[data-evaluation-preview]");
        if (previewRoot) {
            var nameInput = document.getElementById("PeriodName");
            var startInput = document.getElementById("StartDate");
            var endInput = document.getElementById("EndDate");
            var formatDate = function (value) {
                if (!value) return "--";
                return new Intl.DateTimeFormat("vi-VN").format(new Date(value + "T00:00:00"));
            };
            var updatePreview = function () {
                document.getElementById("previewName").textContent = nameInput.value.trim() || "--";
                document.getElementById("previewStart").textContent = formatDate(startInput.value);
                document.getElementById("previewEnd").textContent = formatDate(endInput.value);
            };
            [nameInput, startInput, endInput].forEach(function (input) {
                input.addEventListener("input", updatePreview);
            });
            updatePreview();
        }
    });
})();
