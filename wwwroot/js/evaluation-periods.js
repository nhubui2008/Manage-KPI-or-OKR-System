(function () {
    "use strict";

    function initializeConfirmations() {
        var confirmModalElement = document.getElementById("evaluationConfirmModal");
        var confirmTitle = document.getElementById("evaluationConfirmTitle");
        var confirmMessage = document.getElementById("evaluationConfirmMessage");
        var confirmSubmit = document.getElementById("evaluationConfirmSubmit");
        var pendingForm = null;

        document.querySelectorAll("[data-evaluation-confirm]").forEach(function (form) {
            if (form.dataset.evaluationConfirmReady === "true") return;
            form.dataset.evaluationConfirmReady = "true";

            form.addEventListener("submit", function (event) {
                if (!confirmModalElement || !confirmSubmit || !window.bootstrap) return;

                event.preventDefault();
                pendingForm = form;
                if (confirmTitle) {
                    confirmTitle.textContent = form.dataset.confirmTitle || "Xác nhận thay đổi";
                }
                if (confirmMessage) {
                    confirmMessage.textContent = form.dataset.confirmMessage || "Bạn có chắc muốn tiếp tục?";
                }
                confirmSubmit.classList.toggle("evaluation-primary-action--danger", form.dataset.confirmTone === "danger");
                confirmSubmit.classList.toggle("evaluation-primary-action--warning", form.dataset.confirmTone === "warning");
                window.bootstrap.Modal.getOrCreateInstance(confirmModalElement).show();
            });
        });

        if (confirmSubmit && confirmSubmit.dataset.evaluationConfirmReady !== "true") {
            confirmSubmit.dataset.evaluationConfirmReady = "true";
            confirmSubmit.addEventListener("click", function () {
                if (!pendingForm) return;

                var formToSubmit = pendingForm;
                pendingForm = null;
                HTMLFormElement.prototype.submit.call(formToSubmit);
            });
        }

        if (confirmModalElement && confirmModalElement.dataset.evaluationConfirmReady !== "true") {
            confirmModalElement.dataset.evaluationConfirmReady = "true";
            confirmModalElement.addEventListener("hidden.bs.modal", function () {
                pendingForm = null;
            });
        }
    }

    function parseIsoDate(value) {
        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || "");
        if (!match) return null;

        var year = Number(match[1]);
        var month = Number(match[2]);
        var day = Number(match[3]);
        var date = new Date(year, month - 1, day);
        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
            return null;
        }

        return date;
    }

    function inclusiveDays(start, end) {
        var startUtc = Date.UTC(start.getFullYear(), start.getMonth(), start.getDate());
        var endUtc = Date.UTC(end.getFullYear(), end.getMonth(), end.getDate());
        return Math.round((endUtc - startUtc) / 86400000) + 1;
    }

    function initializePreview(root) {
        if (!root || root.dataset.evaluationPreviewReady === "true") return;
        root.dataset.evaluationPreviewReady = "true";

        var nameInput = document.getElementById("PeriodName");
        var typeInput = document.getElementById("PeriodType");
        var startInput = document.getElementById("StartDate");
        var endInput = document.getElementById("EndDate");
        var previewName = document.getElementById("previewName");
        var previewType = document.getElementById("previewType");
        var previewStart = document.getElementById("previewStart");
        var previewEnd = document.getElementById("previewEnd");
        var previewDuration = document.getElementById("previewDuration");
        var previewStatus = document.getElementById("previewStatus");
        var durationRule = root.querySelector("[data-duration-rule]");
        var requiredElements = [
            nameInput,
            typeInput,
            startInput,
            endInput,
            previewName,
            previewType,
            previewStart,
            previewEnd,
            previewDuration,
            previewStatus
        ];
        if (requiredElements.some(function (element) { return !element; })) return;

        var typeConfiguration = {
            MONTH: { label: "Hàng tháng", minimum: 28, maximum: 31, rule: "Chọn từ 28 đến 31 ngày cho kỳ tháng." },
            QUARTER: { label: "Hàng quý", minimum: 89, maximum: 92, rule: "Chọn từ 89 đến 92 ngày cho kỳ quý." },
            YEAR: { label: "Hàng năm", minimum: 365, maximum: 366, rule: "Chọn 365 hoặc 366 ngày cho kỳ năm." }
        };
        var dateFormatter = new Intl.DateTimeFormat("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric"
        });

        function setStatus(message, tone) {
            if (previewStatus.textContent !== message) {
                previewStatus.textContent = message;
            }
            previewStatus.classList.toggle("is-valid", tone === "valid");
            previewStatus.classList.toggle("is-invalid", tone === "invalid");
        }

        function updatePreview() {
            var configuration = typeConfiguration[typeInput.value] || null;
            var start = parseIsoDate(startInput.value);
            var end = parseIsoDate(endInput.value);

            previewName.textContent = nameInput.value.trim() || "Chưa đặt tên kỳ";
            previewType.textContent = configuration ? configuration.label : "Không xác định";
            previewStart.textContent = start ? dateFormatter.format(start) : "Chưa chọn";
            previewEnd.textContent = end ? dateFormatter.format(end) : "Chưa chọn";
            if (durationRule) {
                durationRule.textContent = configuration ? configuration.rule : "Loại kỳ không hợp lệ.";
            }

            if (!startInput.value || !endInput.value) {
                previewDuration.textContent = "Chưa tính";
                setStatus("Nhập đủ ngày để kiểm tra độ dài kỳ.", "neutral");
                return;
            }

            if (!start || !end) {
                previewDuration.textContent = "Không hợp lệ";
                setStatus("Ngày đã nhập không thể đọc được. Hãy chọn lại từ trình chọn ngày.", "invalid");
                return;
            }

            if (end < start) {
                previewDuration.textContent = "Không hợp lệ";
                setStatus("Ngày kết thúc không thể trước ngày bắt đầu.", "invalid");
                return;
            }

            var duration = inclusiveDays(start, end);
            previewDuration.textContent = duration + " ngày";
            if (!configuration) {
                setStatus("Loại kỳ không hợp lệ. Server sẽ từ chối giá trị này.", "invalid");
                return;
            }

            var durationIsValid = duration >= configuration.minimum && duration <= configuration.maximum;
            setStatus(
                durationIsValid
                    ? "Độ dài phù hợp với loại kỳ đã chọn."
                    : "Độ dài chưa phù hợp. " + configuration.rule,
                durationIsValid ? "valid" : "invalid"
            );
        }

        nameInput.addEventListener("input", updatePreview);
        typeInput.addEventListener("change", updatePreview);
        startInput.addEventListener("input", updatePreview);
        endInput.addEventListener("input", updatePreview);
        updatePreview();
    }

    document.addEventListener("DOMContentLoaded", function () {
        initializeConfirmations();
        initializePreview(document.querySelector("[data-evaluation-preview]"));
    });
})();
