(function () {
    "use strict";

    function unobtrusiveFormIsValid(form) {
        if (!window.jQuery || !window.jQuery.validator) {
            return true;
        }

        return window.jQuery(form).valid();
    }

    function updateCounter(input, output) {
        if (!input || !output) return;

        var maximum = Number(input.getAttribute("maxlength")) || 0;
        var current = input.value.length;
        output.textContent = maximum > 0 ? current + "/" + maximum : String(current);
        output.classList.toggle("text-danger", maximum > 0 && current >= Math.floor(maximum * 0.95));
    }

    function updateSelectionCount(root, name, output) {
        if (!name || !output) return;

        var count = root.querySelectorAll('input[name="' + name + '"]:checked').length;
        output.textContent = count + " đã chọn";
    }

    function checkAndUpdateSummary(form, errorSummary) {
        if (!errorSummary) return;

        var projectNameInput = form.querySelector('[name="ProjectName"]');
        var isProjectNameValid = !projectNameInput || projectNameInput.value.trim().length > 0;

        var activeFieldErrors = form.querySelectorAll('.field-validation-error');
        var hasActiveFieldErrors = false;
        activeFieldErrors.forEach(function (span) {
            if (span.textContent.trim().length > 0) {
                hasActiveFieldErrors = true;
            }
        });

        if (isProjectNameValid && !hasActiveFieldErrors && form.checkValidity()) {
            errorSummary.style.display = "none";
            errorSummary.classList.add("validation-summary-valid");
            errorSummary.classList.remove("validation-summary-errors");
        }
    }

    function initializeRoot(root) {
        if (!root || root.dataset.createFormReady === "true") return;
        root.dataset.createFormReady = "true";

        var form = root.querySelector("[data-create-form-element]");
        if (!form) return;

        var errorSummary = root.querySelector("[data-error-summary]");

        root.querySelectorAll("[data-character-counter]").forEach(function (output) {
            var inputId = output.getAttribute("for");
            var input = inputId ? document.getElementById(inputId) : null;
            if (!input) return;

            input.addEventListener("input", function () {
                updateCounter(input, output);
            });
            updateCounter(input, output);
        });

        root.querySelectorAll("[data-selection-count]").forEach(function (output) {
            var name = output.dataset.selectionCount;
            root.querySelectorAll('input[name="' + name + '"]').forEach(function (input) {
                input.addEventListener("change", function () {
                    updateSelectionCount(root, name, output);
                });
            });
            updateSelectionCount(root, name, output);
        });

        form.querySelectorAll("input, select, textarea").forEach(function (input) {
            input.addEventListener("input", function () {
                checkAndUpdateSummary(form, errorSummary);
            });
            input.addEventListener("change", function () {
                checkAndUpdateSummary(form, errorSummary);
            });
        });

        if (errorSummary && !errorSummary.classList.contains("validation-summary-valid") && errorSummary.textContent.trim()) {
            window.requestAnimationFrame(function () {
                errorSummary.focus({ preventScroll: true });
            });
        }

        var isSubmitting = false;
        form.addEventListener("submit", function (event) {
            if (isSubmitting) {
                event.preventDefault();
                return;
            }

            if (!form.checkValidity() || !unobtrusiveFormIsValid(form) || event.defaultPrevented) {
                return;
            }

            var submitButton = form.querySelector("[data-submit-button]");
            if (!submitButton) return;

            isSubmitting = true;
            submitButton.disabled = true;
            submitButton.setAttribute("aria-busy", "true");

            var submitLabel = submitButton.querySelector("[data-submit-label]");
            if (submitLabel && submitButton.dataset.loadingLabel) {
                submitLabel.textContent = submitButton.dataset.loadingLabel;
            }
        });

        window.addEventListener("pageshow", function () {
            var submitButton = form.querySelector("[data-submit-button]");
            if (!submitButton || !isSubmitting) return;

            isSubmitting = false;
            submitButton.disabled = false;
            submitButton.removeAttribute("aria-busy");

            var submitLabel = submitButton.querySelector("[data-submit-label]");
            if (submitLabel && submitButton.dataset.defaultLabel) {
                submitLabel.textContent = submitButton.dataset.defaultLabel;
            }
        });
    }

    function initAll() {
        document.querySelectorAll("[data-create-form]").forEach(initializeRoot);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initAll);
    } else {
        initAll();
    }

    document.addEventListener("instant:navigation-ready", initAll);
})();
