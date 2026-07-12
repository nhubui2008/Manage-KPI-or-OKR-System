(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var root = document.querySelector("[data-okr-create]");
        if (!root || root.dataset.okrCreateReady === "true") return;
        root.dataset.okrCreateReady = "true";

        var departmentSelect = document.getElementById("departmentId");
        var employeeSelect = document.getElementById("employeeId");
        var emptyState = root.querySelector("[data-employee-empty]");
        var emptyText = root.querySelector("[data-employee-empty-text]");
        if (!departmentSelect || !employeeSelect) return;

        var syncingSelects = false;
        var initialEmployeeId = employeeSelect.value;
        var employeeOptions = Array.from(employeeSelect.options).map(function (option) {
            return {
                value: option.value,
                text: option.text,
                departmentId: option.dataset.departmentId || ""
            };
        });

        function refreshSelect(select) {
            if (!window.jQuery || !window.jQuery.fn || !window.jQuery.fn.select2) return;
            var element = window.jQuery(select);
            if (element.hasClass("select2-hidden-accessible")) {
                element.trigger("change.select2");
            }
        }

        function updateEmptyState(selectedDepartmentId, availableEmployeeCount) {
            if (!emptyState) return;

            var isEmpty = availableEmployeeCount === 0;
            emptyState.hidden = !isEmpty;
            if (isEmpty && emptyText) {
                emptyText.textContent = selectedDepartmentId
                    ? "Phòng ban đã chọn chưa có nhân viên phù hợp trong phạm vi phân bổ."
                    : "Chưa có nhân viên phù hợp trong phạm vi phân bổ.";
            }
        }

        function renderEmployeeOptions(selectedDepartmentId, requestedEmployeeId) {
            var selectedEmployeeId = requestedEmployeeId !== undefined
                ? requestedEmployeeId
                : employeeSelect.value;
            var allowedOptions = employeeOptions.filter(function (option) {
                return option.value === "" || !selectedDepartmentId || option.departmentId === selectedDepartmentId;
            });
            var selectedStillVisible = allowedOptions.some(function (option) {
                return option.value === selectedEmployeeId;
            });

            employeeSelect.innerHTML = "";
            allowedOptions.forEach(function (optionData) {
                var option = new Option(optionData.text, optionData.value);
                option.dataset.departmentId = optionData.departmentId;
                employeeSelect.add(option);
            });

            employeeSelect.value = selectedStillVisible ? selectedEmployeeId : "";
            var availableEmployeeCount = allowedOptions.filter(function (option) { return option.value !== ""; }).length;
            employeeSelect.disabled = availableEmployeeCount === 0;
            updateEmptyState(selectedDepartmentId, availableEmployeeCount);
            refreshSelect(employeeSelect);
        }

        function setDepartmentValue(departmentId) {
            if (!departmentId || departmentSelect.value === departmentId) return;

            departmentSelect.value = departmentId;
            refreshSelect(departmentSelect);
        }

        departmentSelect.addEventListener("change", function () {
            if (syncingSelects) return;

            syncingSelects = true;
            renderEmployeeOptions(departmentSelect.value);
            syncingSelects = false;
        });

        employeeSelect.addEventListener("change", function () {
            if (syncingSelects) return;

            var selectedEmployee = employeeOptions.find(function (option) {
                return option.value === employeeSelect.value;
            });
            if (!selectedEmployee || !selectedEmployee.departmentId) return;

            syncingSelects = true;
            setDepartmentValue(selectedEmployee.departmentId);
            renderEmployeeOptions(selectedEmployee.departmentId, selectedEmployee.value);
            syncingSelects = false;
        });

        syncingSelects = true;
        renderEmployeeOptions(departmentSelect.value, initialEmployeeId);
        syncingSelects = false;
    });
})();
