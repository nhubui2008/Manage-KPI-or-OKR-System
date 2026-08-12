(function () {
    "use strict";

    function initializeEmployeesPage(root) {
        if (!root || root.dataset.employeesInitialized === "true") {
            return;
        }

        root.dataset.employeesInitialized = "true";
        initializeDeactivateModal(root);
        initializeImportForm(root);
    }

    function initializeDeactivateModal(root) {
        var modalElement = document.getElementById("employeeDeactivateModal");
        if (!modalElement || typeof bootstrap === "undefined" || !bootstrap.Modal) {
            return;
        }

        var modalInstance = bootstrap.Modal.getOrCreateInstance(modalElement);
        var form = document.getElementById("employeeDeactivateForm");
        var nameNode = document.getElementById("employeeDeactivateName");
        var codeNode = document.getElementById("employeeDeactivateCode");
        var idInput = document.getElementById("employeeDeactivateId");
        var lastTrigger = null;

        root.querySelectorAll(".js-employee-deactivate").forEach(function (trigger) {
            trigger.addEventListener("click", function (event) {
                if (!form || !nameNode || !codeNode || !idInput) {
                    return;
                }

                event.preventDefault();
                lastTrigger = trigger;

                var employeeId = trigger.getAttribute("data-employee-id") || "";
                var employeeName = trigger.getAttribute("data-employee-name") || "";
                var employeeCode = trigger.getAttribute("data-employee-code") || "";

                idInput.value = employeeId;
                nameNode.textContent = employeeName;
                codeNode.textContent = employeeCode;
                form.action = "/Employees/Delete/" + encodeURIComponent(employeeId);

                modalInstance.show();
            });
        });

        modalElement.addEventListener("shown.bs.modal", function () {
            var confirmButton = form ? form.querySelector("[type='submit']") : null;
            if (confirmButton) {
                confirmButton.focus();
            }
        });

        modalElement.addEventListener("hidden.bs.modal", function () {
            if (idInput) {
                idInput.value = "";
            }
            if (form) {
                form.action = "#";
            }
            if (lastTrigger) {
                lastTrigger.focus();
                lastTrigger = null;
            }
        });
    }

    function initializeImportForm(root) {
        var fileInput = root.querySelector("#excelFile");
        var selectedFileNode = root.querySelector("#selectedFileName") || root.querySelector("[data-selected-file]");
        var submitButton = root.querySelector("#btnSubmitImport") || root.querySelector("[data-import-submit]");

        if (!fileInput) {
            return;
        }

        fileInput.addEventListener("change", function () {
            var fileName = fileInput.files && fileInput.files.length ? fileInput.files[0].name : "";
            if (selectedFileNode) {
                selectedFileNode.textContent = fileName
                    ? "Đã chọn file: " + fileName
                    : "";
            }
        });

        var form = fileInput.closest("form");
        if (form && submitButton) {
            form.addEventListener("submit", function () {
                if (!fileInput.files || !fileInput.files.length) {
                    return;
                }
                submitButton.setAttribute("aria-busy", "true");
                var label = submitButton.querySelector("span");
                if (label) {
                    label.textContent = "Đang tải lên...";
                }
            });
        }
    }

    function scan() {
        document.querySelectorAll("[data-employees-page]").forEach(initializeEmployeesPage);
        var importPage = document.querySelector("[data-employees-import]");
        if (importPage && importPage.dataset.employeesInitialized !== "true") {
            importPage.dataset.employeesInitialized = "true";
            initializeImportForm(importPage);
        }
    }

    document.addEventListener("DOMContentLoaded", scan);
    document.addEventListener("instant:navigation-ready", scan);
})();

