(function () {
    'use strict';

    const shell = document.body;
    const mainContent = document.getElementById('mainContent');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const pendingForms = new WeakSet();
    const revealSelectors = [
        '.page-header',
        '.evaluation-page-header',
        '.cf-header',
        '.dashboard-header',
        '.evaluation-summary',
        '.okr-summary',
        '.mv-summary',
        '.ops-summary',
        '.stats-grid',
        '.filter-bar',
        '.evaluation-filter-panel',
        '.content-card',
        '.evaluation-results',
        '.catalog-table-shell'
    ];

    if (!shell?.classList.contains('vietmach-shell') || !mainContent) {
        return;
    }

    function visibleElements(root, selector) {
        return Array.from(root.querySelectorAll(selector))
            .filter(element => !element.closest('.modal, [hidden]') && element.getClientRects().length > 0);
    }

    function enhanceMotion(root) {
        const candidates = [];

        revealSelectors.forEach(selector => {
            visibleElements(root, selector).forEach(element => {
                if (!candidates.includes(element) && candidates.length < 6) {
                    candidates.push(element);
                }
            });
        });

        if (reducedMotion.matches) {
            candidates.forEach(element => element.classList.add('management-reveal-complete'));
            return;
        }

        candidates.forEach((element, index) => {
            if (element.dataset.managementReveal === 'true') return;

            element.dataset.managementReveal = 'true';
            element.style.setProperty('--management-reveal-delay', `${index * 24}ms`);
            element.classList.add('management-reveal');
        });

        window.requestAnimationFrame(() => {
            window.requestAnimationFrame(() => {
                candidates.forEach(element => {
                    element.classList.add('management-reveal-complete');
                });
            });
        });
    }

    function tableLabel(container) {
        const card = container.closest('.card, .content-card, .evaluation-results, section');
        const heading = card?.querySelector('h1, h2, h3, h4, h5, .card-title');
        return heading?.textContent?.trim() || 'Bảng dữ liệu';
    }

    function syncScrollableTables(root) {
        root.querySelectorAll('.table-responsive').forEach(container => {
            const isScrollable = container.scrollWidth > container.clientWidth + 1;
            container.classList.toggle('management-scrollable-table', isScrollable);

            if (isScrollable) {
                if (!container.hasAttribute('tabindex')) {
                    container.tabIndex = 0;
                    container.dataset.managementTabindex = 'true';
                }
                if (!container.hasAttribute('aria-label')) {
                    container.setAttribute('aria-label', `${tableLabel(container)} — cuộn ngang để xem thêm`);
                    container.dataset.managementAriaLabel = 'true';
                }
                return;
            }

            if (container.dataset.managementTabindex === 'true') {
                container.removeAttribute('tabindex');
                delete container.dataset.managementTabindex;
            }
            if (container.dataset.managementAriaLabel === 'true') {
                container.removeAttribute('aria-label');
                delete container.dataset.managementAriaLabel;
            }
        });
    }

    function enhance(root) {
        shell.classList.add('management-ux-ready');
        enhanceMotion(root);
        syncScrollableTables(root);
    }

    function resetSubmitState(form) {
        pendingForms.delete(form);
        form.removeAttribute('aria-busy');
        form.querySelectorAll('.management-submit-pending').forEach(button => {
            button.classList.remove('management-submit-pending');
            button.removeAttribute('aria-disabled');
        });
    }

    document.addEventListener('submit', event => {
        const form = event.target instanceof HTMLFormElement ? event.target : null;
        if (!form || event.defaultPrevented || !form.checkValidity()) return;
        if ((form.method || 'get').toLowerCase() === 'get') return;
        if (form.id === 'globalAntiForgeryForm' ||
            form.matches('[data-no-submit-feedback], [data-app-confirm]')) return;

        if (pendingForms.has(form)) {
            event.preventDefault();
            return;
        }

        pendingForms.add(form);
        form.setAttribute('aria-busy', 'true');

        const submitter = event.submitter instanceof HTMLElement ? event.submitter : null;
        if (submitter) {
            submitter.classList.add('management-submit-pending');
            submitter.setAttribute('aria-disabled', 'true');
        }
    });

    document.addEventListener('invalid', event => {
        const form = event.target instanceof Element ? event.target.closest('form') : null;
        if (form) resetSubmitState(form);
    }, true);

    document.addEventListener('instant:navigation-ready', event => {
        const root = event.detail?.root instanceof Element ? event.detail.root : mainContent;
        enhance(root);
    });

    window.addEventListener('pageshow', () => {
        document.querySelectorAll('form[aria-busy="true"]').forEach(resetSubmitState);
        enhance(mainContent);
    });

    let resizeFrame = 0;
    window.addEventListener('resize', () => {
        window.cancelAnimationFrame(resizeFrame);
        resizeFrame = window.requestAnimationFrame(() => syncScrollableTables(mainContent));
    }, { passive: true });

    enhance(mainContent);
})();
