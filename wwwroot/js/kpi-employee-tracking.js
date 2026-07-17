(function () {
    "use strict";

    var page = document.querySelector("[data-employee-tracking-page]");
    if (!page) return;

    var liveRegion = page.querySelector("[data-employee-tracking-live]");

    function announce(message) {
        if (!liveRegion) return;
        liveRegion.textContent = "";
        window.setTimeout(function () {
            liveRegion.textContent = message;
        }, 30);
    }

    function normalizeSearchValue(value) {
        return (value || "")
            .toLocaleLowerCase("vi")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .replace(/đ/g, "d")
            .replace(/\s+/g, " ")
            .trim();
    }

    var employeeSearch = page.querySelector("[data-employee-search]");
    var employeeItems = Array.prototype.slice.call(page.querySelectorAll("[data-employee-item]"));
    var employeeSearchEmpty = page.querySelector("[data-employee-search-empty]");

    if (employeeSearch && employeeItems.length) {
        employeeItems.forEach(function (item) {
            item.dataset.normalizedSearchValue = normalizeSearchValue(item.dataset.searchValue);
        });

        var applyEmployeeFilter = function () {
            var query = normalizeSearchValue(employeeSearch.value);
            var visibleCount = 0;

            employeeItems.forEach(function (item) {
                var matches = !query || item.dataset.normalizedSearchValue.indexOf(query) !== -1;
                item.hidden = !matches;
                if (matches) visibleCount += 1;
            });

            if (employeeSearchEmpty) {
                employeeSearchEmpty.hidden = visibleCount !== 0;
            }

            if (query) {
                announce(visibleCount === 0
                    ? "Không tìm thấy nhân viên phù hợp."
                    : "Tìm thấy " + visibleCount + " lựa chọn nhân sự.");
            }
        };

        employeeSearch.addEventListener("input", applyEmployeeFilter);
        employeeSearch.addEventListener("keydown", function (event) {
            if (event.key !== "Escape" || !employeeSearch.value) return;
            employeeSearch.value = "";
            applyEmployeeFilter();
            announce("Đã xóa nội dung tìm kiếm nhân viên.");
        });
    }

    var mobileEmployeeSelect = page.querySelector("[data-mobile-employee-select]");
    var mobileEmployeeForm = page.querySelector("[data-mobile-employee-form]");

    if (mobileEmployeeSelect && mobileEmployeeForm) {
        mobileEmployeeSelect.addEventListener("change", function () {
            var submitButton = mobileEmployeeForm.querySelector('button[type="submit"]');
            if (typeof mobileEmployeeForm.requestSubmit === "function") {
                mobileEmployeeForm.requestSubmit(submitButton || undefined);
            }
        });
    }

    var localSubmitForms = Array.prototype.slice.call(page.querySelectorAll("[data-local-submit]"));

    function resetLocalSubmitForm(form) {
        delete form.dataset.submitting;
        form.removeAttribute("aria-busy");

        Array.prototype.forEach.call(form.querySelectorAll('button[type="submit"]'), function (button) {
            button.disabled = false;
            button.removeAttribute("aria-disabled");
            button.classList.remove("is-busy");
            if (button.dataset.originalHtml) {
                button.innerHTML = button.dataset.originalHtml;
                delete button.dataset.originalHtml;
            }
        });
    }

    localSubmitForms.forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (form.dataset.submitting === "true") {
                event.preventDefault();
                return;
            }

            form.dataset.submitting = "true";
            form.setAttribute("aria-busy", "true");

            var submitter = event.submitter;
            var buttons = Array.prototype.slice.call(form.querySelectorAll('button[type="submit"]'));
            buttons.forEach(function (button) {
                button.setAttribute("aria-disabled", "true");
            });

            if (submitter) {
                submitter.dataset.originalHtml = submitter.innerHTML;
                submitter.classList.add("is-busy");
                submitter.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span>' +
                    (form.dataset.busyLabel || "Đang xử lý...") + "</span>";
            }

            announce(form.dataset.busyLabel || "Đang xử lý yêu cầu.");

            window.setTimeout(function () {
                buttons.forEach(function (button) {
                    button.disabled = true;
                });
            }, 0);
        });
    });

    window.addEventListener("pageshow", function () {
        localSubmitForms.forEach(resetLocalSubmitForm);
    });
})();
