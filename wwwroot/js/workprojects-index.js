/* wwwroot/js/workprojects-index.js - Velzon Work Projects Index logic */
(function () {
    "use strict";

    function initWorkProjectFilters() {
        var form = document.querySelector(".workprojects-filter");
        if (!form || form.dataset.workprojectsIndexReady === "true") return;
        form.dataset.workprojectsIndexReady = "true";

        var submitButton = form.querySelector('button[type="submit"]');
        var quickFilterLinks = form.querySelectorAll(".quick-filter-row a");
        var originalSubmitHtml = submitButton ? submitButton.innerHTML : "";

        var setLoading = function () {
            form.classList.add("is-loading");
            form.setAttribute("aria-busy", "true");

            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang lọc';
            }
        };

        var resetLoading = function () {
            form.classList.remove("is-loading");
            form.removeAttribute("aria-busy");

            if (submitButton) {
                submitButton.disabled = false;
                submitButton.innerHTML = originalSubmitHtml;
            }
        };

        form.addEventListener("submit", setLoading);

        quickFilterLinks.forEach(function (link) {
            link.addEventListener("click", function (event) {
                if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;

                quickFilterLinks.forEach(function (item) {
                    item.classList.remove("is-active");
                });
                link.classList.add("is-active");
                setLoading();
            });
        });

        window.addEventListener("pageshow", resetLoading);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initWorkProjectFilters);
    } else {
        initWorkProjectFilters();
    }

    document.addEventListener("instant:navigation-ready", initWorkProjectFilters);
})();
