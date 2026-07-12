(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var root = document.querySelector("[data-workproject-create]");
        if (!root || root.dataset.workprojectCreateReady === "true") return;
        root.dataset.workprojectCreateReady = "true";

        root.querySelectorAll('input[type="date"]').forEach(function (input) {
            input.setAttribute("lang", "vi");
        });

        var okrSelect = document.getElementById("SourceOKRId");
        var kpiSelect = document.getElementById("SourceKPIId");
        var relationshipHint = root.querySelector("[data-source-relationship]");
        if (!okrSelect || !kpiSelect || !relationshipHint) return;

        function updateRelationshipHint() {
            var selectedKpi = kpiSelect.options[kpiSelect.selectedIndex];
            var linkedOkrId = selectedKpi ? selectedKpi.dataset.okrId : "";
            var linkedOkrLabel = selectedKpi ? selectedKpi.dataset.okrLabel : "";

            relationshipHint.classList.remove("cf-dynamic-hint--warning", "cf-dynamic-hint--success");
            if (!kpiSelect.value || !linkedOkrId) {
                relationshipHint.textContent = "";
                return;
            }

            if (!okrSelect.value) {
                relationshipHint.textContent = "KPI này thuộc " + (linkedOkrLabel || "một OKR nguồn") + "; hệ thống sẽ tự liên kết khi lưu.";
                relationshipHint.classList.add("cf-dynamic-hint--success");
                return;
            }

            if (okrSelect.value === linkedOkrId) {
                relationshipHint.textContent = "KPI và OKR đã chọn có quan hệ phù hợp.";
                relationshipHint.classList.add("cf-dynamic-hint--success");
                return;
            }

            relationshipHint.textContent = "KPI này thuộc một OKR khác. Hãy chọn đúng OKR hoặc bỏ lựa chọn OKR để hệ thống tự suy ra.";
            relationshipHint.classList.add("cf-dynamic-hint--warning");
        }

        okrSelect.addEventListener("change", updateRelationshipHint);
        kpiSelect.addEventListener("change", updateRelationshipHint);
        updateRelationshipHint();
    });
})();
