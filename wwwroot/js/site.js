// ============================================
// Site JavaScript - KPI/OKR Management System
// ============================================

(function () {
    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    window.getAntiForgeryToken = getAntiForgeryToken;

    window.appendAntiForgeryToken = function (body) {
        const token = getAntiForgeryToken();
        if (!token || !body || typeof body.append !== 'function') return body;
        body.append('__RequestVerificationToken', token);
        return body;
    };

    window.antiForgeryHeaders = function () {
        const token = getAntiForgeryToken();
        return token ? { 'RequestVerificationToken': token } : {};
    };

    window.escapeHtml = function (value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    };

    window.escapeAttribute = window.escapeHtml;

    window.escapeRegExp = function (value) {
        return String(value ?? '').replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    };

    window.sanitizeSearchUrl = function (value) {
        const url = String(value ?? '#');
        return url.startsWith('/') ? url : '#';
    };
})();

(function () {
    const feedbackToneMap = {
        success: { icon: 'bi-check-lg', fallbackTitle: 'Đã hoàn tất', meta: 'Thành công', swalIcon: 'success' },
        error: { icon: 'bi-x-lg', fallbackTitle: 'Không thể thực hiện', meta: 'Có lỗi xảy ra', swalIcon: 'error' },
        warning: { icon: 'bi-exclamation-lg', fallbackTitle: 'Cần chú ý', meta: 'Cảnh báo', swalIcon: 'warning' },
        info: { icon: 'bi-info-lg', fallbackTitle: 'Thông báo', meta: 'Thông tin', swalIcon: 'info' },
        danger: { icon: 'bi-trash3', fallbackTitle: 'Xác nhận hành động', meta: 'Hành động nguy hiểm', swalIcon: 'warning' }
    };
    const pendingForms = new WeakSet();
    const approvedForms = new WeakSet();

    function normalizeTone(tone) {
        const normalized = String(tone || 'info').trim().toLowerCase();
        return feedbackToneMap[normalized] ? normalized : 'info';
    }

    function normalizeRequiredValue(value) {
        return String(value ?? '').trim().toLocaleLowerCase('vi-VN');
    }

    function ensureToastContainer() {
        let container = document.getElementById('appToastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'appToastContainer';
            container.className = 'app-toast-container';
            container.setAttribute('aria-label', 'Thông báo hệ thống');
            document.body.appendChild(container);
        }

        return container;
    }

    function clearToastTimer(toast) {
        if (toast?._dismissTimer) {
            window.clearTimeout(toast._dismissTimer);
            toast._dismissTimer = 0;
        }
    }

    function scheduleToastDismiss(toast, delay) {
        clearToastTimer(toast);
        if (!toast || delay <= 0) {
            return;
        }

        toast._remaining = delay;
        toast._startedAt = Date.now();
        toast._dismissTimer = window.setTimeout(() => dismissToast(toast), delay);
    }

    function pauseToast(toast) {
        if (!toast || toast.dataset.closing === 'true' || toast.dataset.paused === 'true') {
            return;
        }

        toast.dataset.paused = 'true';
        if (toast._startedAt) {
            const elapsed = Date.now() - toast._startedAt;
            toast._remaining = Math.max(0, (toast._remaining || 0) - elapsed);
        }

        clearToastTimer(toast);
        toast.classList.add('is-paused');
    }

    function resumeToast(toast) {
        if (!toast || toast.dataset.closing === 'true' || toast.dataset.paused !== 'true') {
            return;
        }

        toast.dataset.paused = 'false';
        toast.classList.remove('is-paused');

        const remaining = Number(toast._remaining || 0);
        if (remaining > 0) {
            scheduleToastDismiss(toast, remaining);
        } else {
            dismissToast(toast);
        }
    }

    function dismissToast(toast) {
        if (!toast || toast.dataset.closing === 'true') {
            return;
        }

        clearToastTimer(toast);
        toast.dataset.closing = 'true';
        toast.dataset.paused = 'false';
        toast.classList.remove('is-paused');
        toast.classList.remove('is-visible');
        toast.classList.add('is-leaving');
        window.setTimeout(() => toast.remove(), 160);
    }

    function showToast(optionsOrTitle, message, tone) {
        const options = typeof optionsOrTitle === 'object' && optionsOrTitle !== null
            ? optionsOrTitle
            : { title: optionsOrTitle, message, tone };

        const normalizedTone = normalizeTone(options.tone);
        const toneConfig = feedbackToneMap[normalizedTone];
        const container = ensureToastContainer();
        const toast = document.createElement('div');
        const requestedTimeout = Number(options.timeout);
        const defaultTimeout = normalizedTone === 'error' ? 6000 : 4600;
        const timeout = Number.isFinite(requestedTimeout)
            ? (requestedTimeout <= 0 ? 0 : Math.max(1600, requestedTimeout))
            : defaultTimeout;

        toast.className = `app-toast app-toast--${normalizedTone}`;
        toast.setAttribute('role', normalizedTone === 'error' ? 'alert' : 'status');
        toast.setAttribute('aria-atomic', 'true');
        toast.style.setProperty('--toast-duration', `${timeout}ms`);
        toast.innerHTML = `
            <div class="app-toast__icon" aria-hidden="true">
                <i class="bi ${toneConfig.icon}"></i>
            </div>
            <div class="app-toast__body">
                <div class="app-toast__meta">${escapeHtml(options.eyebrow || options.meta || toneConfig.meta)}</div>
                <div class="app-toast__title">${escapeHtml(options.title || toneConfig.fallbackTitle)}</div>
                <div class="app-toast__message">${escapeHtml(options.message || '')}</div>
            </div>
            <button type="button" class="app-toast__close" aria-label="Đóng thông báo">
                <i class="bi bi-x-lg"></i>
            </button>
            <div class="app-toast__progress" aria-hidden="true"></div>
        `;

        toast.querySelector('.app-toast__close')?.addEventListener('click', () => dismissToast(toast));
        toast.addEventListener('mouseenter', () => pauseToast(toast));
        toast.addEventListener('mouseleave', () => resumeToast(toast));
        toast.addEventListener('focusin', () => pauseToast(toast));
        toast.addEventListener('focusout', () => resumeToast(toast));
        container.prepend(toast);

        while (container.children.length > 4) {
            container.lastElementChild?.remove();
        }

        requestAnimationFrame(() => toast.classList.add('is-visible'));
        if (timeout > 0) {
            scheduleToastDismiss(toast, timeout);
        }
        return toast;
    }

    function mergeDialogClasses(tone) {
        return {
            container: 'app-dialog-layer',
            popup: `app-dialog app-dialog--${tone}`,
            icon: 'app-dialog__icon',
            title: 'app-dialog__title',
            htmlContainer: 'app-dialog__content',
            actions: 'app-dialog__actions',
            confirmButton: 'app-dialog__button app-dialog__button--confirm',
            cancelButton: 'app-dialog__button app-dialog__button--cancel',
            input: 'app-dialog__input',
            inputLabel: 'app-dialog__input-label',
            validationMessage: 'app-dialog__validation'
        };
    }

    function openDialog(options = {}) {
        if (typeof window.Swal === 'undefined') {
            showToast({
                tone: 'error',
                title: 'Không thể mở hộp thoại',
                message: 'Thành phần giao diện chưa tải xong. Vui lòng tải lại trang.'
            });
            return Promise.resolve({ isConfirmed: false, isDismissed: true });
        }

        const tone = normalizeTone(options.tone);
        const toneConfig = feedbackToneMap[tone];
        const requireText = String(options.requireText ?? '').trim();
        const userDidOpen = options.didOpen;
        const config = {
            titleText: String(options.title || toneConfig.fallbackTitle),
            text: options.trustedHtml ? undefined : String(options.message || ''),
            html: options.trustedHtml || undefined,
            icon: toneConfig.swalIcon,
            iconHtml: `<i class="bi ${toneConfig.icon}" aria-hidden="true"></i>`,
            showCancelButton: Boolean(options.showCancelButton),
            confirmButtonText: String(options.confirmText || 'Đồng ý'),
            cancelButtonText: String(options.cancelText || 'Hủy'),
            buttonsStyling: false,
            reverseButtons: true,
            focusCancel: options.focusCancel ?? Boolean(options.showCancelButton),
            returnFocus: options.returnFocus ?? true,
            allowEscapeKey: options.allowEscapeKey ?? true,
            allowOutsideClick: options.allowOutsideClick ?? true,
            heightAuto: false,
            customClass: mergeDialogClasses(tone),
            showClass: {
                popup: 'app-dialog-enter',
                backdrop: 'app-dialog-backdrop-enter'
            },
            hideClass: {
                popup: 'app-dialog-exit',
                backdrop: 'app-dialog-backdrop-exit'
            },
            didOpen: (popup) => {
                if (requireText) {
                    const input = window.Swal.getInput();
                    const confirmButton = window.Swal.getConfirmButton();
                    const expected = normalizeRequiredValue(requireText);
                    const updateState = () => {
                        const matches = normalizeRequiredValue(input?.value) === expected;
                        if (confirmButton) confirmButton.disabled = !matches;
                    };

                    input?.addEventListener('input', updateState);
                    updateState();
                }

                if (typeof userDidOpen === 'function') userDidOpen(popup);
            }
        };

        if (requireText) {
            config.input = 'text';
            config.inputLabel = String(options.requireTextLabel || `Nhập “${requireText}” để tiếp tục`);
            config.inputPlaceholder = requireText;
            config.inputAttributes = {
                autocomplete: 'off',
                autocapitalize: 'off',
                spellcheck: 'false'
            };
            config.preConfirm = (value) => {
                if (normalizeRequiredValue(value) !== normalizeRequiredValue(requireText)) {
                    window.Swal.showValidationMessage(`Nội dung xác nhận phải khớp “${requireText}”.`);
                    return false;
                }
                return value;
            };
        } else if (typeof options.preConfirm === 'function') {
            config.preConfirm = options.preConfirm;
        }

        if (typeof options.willClose === 'function') config.willClose = options.willClose;
        if (typeof options.didClose === 'function') config.didClose = options.didClose;
        if (options.timer) config.timer = options.timer;
        if (options.timerProgressBar) config.timerProgressBar = true;
        if (options.showLoaderOnConfirm) config.showLoaderOnConfirm = true;

        return window.Swal.fire(config);
    }

    async function confirmDialog(options = {}) {
        const result = await openDialog({
            ...options,
            showCancelButton: true,
            confirmText: options.confirmText || 'Xác nhận',
            cancelText: options.cancelText || 'Hủy'
        });
        return Boolean(result?.isConfirmed);
    }

    async function showNotice(options = {}) {
        return openDialog({
            ...options,
            showCancelButton: false,
            focusCancel: false,
            confirmText: options.confirmText || 'Đóng'
        });
    }

    function showComingSoon(featureName) {
        return showToast({
            tone: 'warning',
            meta: 'Tính năng',
            title: 'Đang phát triển',
            message: `Chức năng “${featureName}” sẽ được cập nhật trong phiên bản tới.`
        });
    }

    window.AppFeedback = {
        toast: showToast,
        notice: showNotice,
        open: openDialog,
        confirm: confirmDialog,
        close: () => window.Swal?.close(),
        comingSoon: showComingSoon
    };

    window.AppDialog = {
        open: openDialog,
        confirm: confirmDialog,
        close: () => window.Swal?.close()
    };

    // Backward-compatible aliases while page-level calls are migrated.
    window.showAppToast = showToast;
    window.showComingSoonToast = showComingSoon;

    // Prevent legacy or third-party calls from opening browser-native dialogs.
    window.alert = function (message) {
        showToast({ tone: 'info', meta: 'Hệ thống', title: 'Thông báo', message: String(message ?? '') });
    };

    document.addEventListener('submit', async (event) => {
        const form = event.target instanceof HTMLFormElement ? event.target : null;
        if (!form?.hasAttribute('data-app-confirm')) return;

        if (approvedForms.has(form)) {
            approvedForms.delete(form);
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();
        if (pendingForms.has(form)) return;

        pendingForms.add(form);
        const submitter = event.submitter instanceof HTMLElement ? event.submitter : null;
        const confirmed = await confirmDialog({
            title: form.dataset.confirmTitle,
            message: form.dataset.confirmMessage,
            tone: form.dataset.confirmTone,
            confirmText: form.dataset.confirmLabel,
            cancelText: form.dataset.confirmCancelLabel,
            requireText: form.dataset.confirmRequireText,
            requireTextLabel: form.dataset.confirmRequireLabel
        });
        pendingForms.delete(form);

        if (!confirmed || !form.isConnected) return;

        approvedForms.add(form);
        if (typeof form.requestSubmit === 'function') {
            try {
                form.requestSubmit(submitter instanceof HTMLButtonElement || submitter instanceof HTMLInputElement ? submitter : undefined);
            } catch {
                form.requestSubmit();
            }
        } else {
            form.submit();
        }
    }, true);
})();

(function () {
    const unitOptions = [
        { value: '%', label: '% - Tỷ lệ phần trăm', kind: 'percent', step: '0.01', min: '0', max: '100', example: '0 - 100', suffix: '%' },
        { value: 'VNĐ', label: 'VNĐ - Tiền tệ', kind: 'money', step: '1000', min: '0', max: '', example: 'VD: 5000000', suffix: 'VNĐ' },
        { value: 'Triệu VNĐ', label: 'Triệu VNĐ - Tiền tệ rút gọn', kind: 'money', step: '0.01', min: '0', max: '', example: 'VD: 1500', suffix: 'triệu VNĐ' },
        { value: 'Điểm', label: 'Điểm - Thang điểm', kind: 'score', step: '0.01', min: '0', max: '100', example: '0 - 100', suffix: 'điểm' },
        { value: 'Người', label: 'Người - Nhân sự', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 10', suffix: 'người' },
        { value: 'Khách hàng', label: 'Khách hàng', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 25', suffix: 'khách hàng' },
        { value: 'Cơ hội', label: 'Cơ hội bán hàng', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 120', suffix: 'cơ hội' },
        { value: 'Hợp đồng', label: 'Hợp đồng', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 12', suffix: 'hợp đồng' },
        { value: 'Sản phẩm', label: 'Sản phẩm', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 100', suffix: 'sản phẩm' },
        { value: 'Lần', label: 'Lần', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 5', suffix: 'lần' },
        { value: 'Giờ', label: 'Giờ', kind: 'decimal', step: '0.25', min: '0', max: '', example: 'VD: 8', suffix: 'giờ' },
        { value: 'Ngày', label: 'Ngày', kind: 'decimal', step: '0.5', min: '0', max: '', example: 'VD: 15', suffix: 'ngày' },
        { value: 'Dự án', label: 'Dự án', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 3', suffix: 'dự án' },
        { value: 'Công việc', label: 'Công việc', kind: 'integer', step: '1', min: '0', max: '', example: 'VD: 20', suffix: 'công việc' }
    ];

    const defaultConfig = {
        value: '',
        label: 'Đơn vị',
        kind: 'decimal',
        step: '0.01',
        min: '0',
        max: '',
        example: 'VD: 100',
        suffix: 'đơn vị'
    };

    function normalizeUnit(value) {
        return String(value ?? '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .replace(/Đ/g, 'D')
            .toLowerCase()
            .trim();
    }

    function getMeasurementUnitConfig(unit) {
        const normalized = normalizeUnit(unit);
        if (!normalized) return defaultConfig;

        const exact = unitOptions.find(option => normalizeUnit(option.value) === normalized);
        if (exact) return exact;

        if (normalized.includes('%') || normalized.includes('phan tram') || normalized.includes('ty le')) {
            return unitOptions[0];
        }

        if (normalized.includes('vnd') || normalized.includes('vnđ') || normalized.includes('tien') || normalized.includes('doanh thu')) {
            return normalized.includes('trieu') ? unitOptions[2] : unitOptions[1];
        }

        if (normalized.includes('diem') || normalized.includes('score')) {
            return unitOptions[3];
        }

        if (normalized.includes('gio')) return unitOptions[10];
        if (normalized.includes('ngay')) return unitOptions[11];

        const integerHints = ['nguoi', 'khach', 'co hoi', 'hop dong', 'san pham', 'lan', 'du an', 'cong viec', 'ticket', 'lead'];
        if (integerHints.some(hint => normalized.includes(hint))) {
            return { ...defaultConfig, kind: 'integer', step: '1', example: 'VD: 10', suffix: unit || defaultConfig.suffix };
        }

        return { ...defaultConfig, suffix: unit || defaultConfig.suffix };
    }

    function placeholderFor(input, config) {
        const role = input.dataset.measurementRole || 'value';
        const labels = {
            target: 'Nhập chỉ tiêu',
            pass: 'Nhập ngưỡng đạt',
            fail: 'Nhập ngưỡng trượt',
            current: 'Nhập giá trị hiện tại',
            achieved: 'Nhập kết quả đạt được',
            allocated: 'Nhập giá trị phân bổ',
            value: 'Nhập giá trị'
        };

        return `${labels[role] || labels.value} (${config.example})`;
    }

    function updateSuffix(input, config) {
        const suffixId = input.dataset.measurementSuffixTarget;
        let suffixElement = suffixId ? document.getElementById(suffixId) : null;
        if (!suffixElement) {
            suffixElement = input.closest('.input-group')?.querySelector('.measurement-unit-suffix') || null;
        }

        if (suffixElement) {
            suffixElement.textContent = config.suffix || defaultConfig.suffix;
        }
    }

    function applyConfigToInput(input, config) {
        if (!input) return;

        input.type = 'number';
        input.step = config.step || defaultConfig.step;
        input.min = config.min ?? defaultConfig.min;
        input.inputMode = config.kind === 'integer' ? 'numeric' : 'decimal';
        input.placeholder = placeholderFor(input, config);

        if (config.max) {
            input.max = config.max;
        } else {
            input.removeAttribute('max');
        }

        updateSuffix(input, config);
    }

    function applyMeasurementUnitConfigToInputs(unit, inputs) {
        const config = getMeasurementUnitConfig(unit);
        const targetInputs = Array.isArray(inputs) || inputs instanceof NodeList
            ? inputs
            : [inputs].filter(Boolean);

        targetInputs.forEach(input => applyConfigToInput(input, config));
        return config;
    }

    function updateScope(select) {
        const scope = select.closest('[data-measurement-scope]') || select.closest('form') || document;
        const config = getMeasurementUnitConfig(select.value);
        scope.querySelectorAll('.measurement-value-input').forEach(input => applyConfigToInput(input, config));
        scope.querySelectorAll('[data-measurement-unit-label]').forEach(label => {
            label.textContent = config.suffix || select.value || '--';
        });
    }

    function applyMeasurementUnitBehavior(root) {
        const targetRoot = root || document;
        targetRoot.querySelectorAll('.measurement-unit-select').forEach(select => {
            if (select.dataset.measurementBehaviorReady === 'true') {
                updateScope(select);
                return;
            }

            select.dataset.measurementBehaviorReady = 'true';
            select.addEventListener('change', () => updateScope(select));
            select.addEventListener('input', () => updateScope(select));
            updateScope(select);
        });
    }

    function setMeasurementUnitSelectValue(select, value) {
        if (!select) return;

        const selectedValue = String(value ?? '').trim();
        if (selectedValue && !Array.from(select.options).some(option => normalizeUnit(option.value) === normalizeUnit(selectedValue))) {
            select.add(new Option(selectedValue, selectedValue, true, true));
        }

        select.value = selectedValue;
        select.dispatchEvent(new Event('change', { bubbles: true }));
    }

    window.measurementUnitOptions = unitOptions;
    window.getMeasurementUnitConfig = getMeasurementUnitConfig;
    window.applyMeasurementUnitConfigToInputs = applyMeasurementUnitConfigToInputs;
    window.applyMeasurementUnitBehavior = applyMeasurementUnitBehavior;
    window.setMeasurementUnitSelectValue = setMeasurementUnitSelectValue;

    document.addEventListener('DOMContentLoaded', () => applyMeasurementUnitBehavior(document));
    document.addEventListener('shown.bs.modal', event => applyMeasurementUnitBehavior(event.target));
})();

(function () {
    function initNotificationCenter(center) {
        if (!center || center.dataset.notificationReady === 'true') {
            return;
        }

        center.dataset.notificationReady = 'true';

        const endpoint = center.dataset.notificationEndpoint;
        const readEndpoint = center.dataset.notificationReadEndpoint;
        const readAllEndpoint = center.dataset.notificationReadAllEndpoint;
        const aiRefreshEndpoint = center.dataset.notificationAiRefreshEndpoint;
        const bellButton = center.querySelector('.notification-bell-btn');
        const dot = center.querySelector('[data-notification-dot]');
        const pill = center.querySelector('[data-notification-pill]');
        const subtitle = center.querySelector('[data-notification-subtitle]');
        const markAllButton = center.querySelector('[data-notification-mark-all]');
        const reloadButton = center.querySelector('[data-notification-reload]');
        const metricAll = center.querySelector('[data-notification-metric="all"]');
        const metricSystem = center.querySelector('[data-notification-metric="system"]');
        const metricAi = center.querySelector('[data-notification-metric="ai"]');
        const tabButtons = Array.from(center.querySelectorAll('[data-notification-tab]'));
        const systemBadge = center.querySelector('[data-notification-tab-badge="system"]');
        const aiBadge = center.querySelector('[data-notification-tab-badge="ai"]');
        const systemList = center.querySelector('[data-notification-list="system"]');
        const aiList = center.querySelector('[data-notification-list="ai"]');
        const refreshAiButton = center.querySelector('[data-notification-refresh-ai]');

        const state = {
            activeCategory: 'system',
            isLoaded: false,
            isLoading: false,
            lastLoadedAt: 0,
            payload: null
        };

        function activeTabButton() {
            return center.querySelector('[data-notification-tab].active') || tabButtons[0] || null;
        }

        function currentCategory() {
            return activeTabButton()?.dataset.notificationTab || state.activeCategory || 'system';
        }

        function formatCount(value) {
            const safeValue = Number(value || 0);
            return safeValue > 99 ? '99+' : String(Math.max(0, safeValue));
        }

        function formatRelativeTime(value) {
            if (!value) {
                return 'Vừa xong';
            }

            const date = new Date(value);
            if (Number.isNaN(date.getTime())) {
                return 'Vừa xong';
            }

            const diffMs = Date.now() - date.getTime();
            const absMs = Math.abs(diffMs);
            const minute = 60 * 1000;
            const hour = 60 * minute;
            const day = 24 * hour;

            if (absMs < minute) return 'Vừa xong';
            if (absMs < hour) return `${Math.round(absMs / minute)} phút trước`;
            if (absMs < day) return `${Math.round(absMs / hour)} giờ trước`;
            if (absMs < 7 * day) return `${Math.round(absMs / day)} ngày trước`;

            return new Intl.DateTimeFormat('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            }).format(date);
        }

        function formatAbsoluteTime(value) {
            if (!value) {
                return 'Vừa xong';
            }

            const date = new Date(value);
            if (Number.isNaN(date.getTime())) {
                return 'Vừa xong';
            }

            return new Intl.DateTimeFormat('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            }).format(date);
        }

        function normalizeSeverity(severity) {
            const normalized = String(severity || 'info').toLowerCase();
            if (normalized === 'high' || normalized === 'danger' || normalized === 'error') {
                return 'danger';
            }

            if (normalized === 'medium' || normalized === 'warning') {
                return 'warning';
            }

            if (normalized === 'success') {
                return 'success';
            }

            return 'info';
        }

        function severityLabel(severity) {
            if (severity === 'danger') return 'Ưu tiên cao';
            if (severity === 'warning') return 'Cần chú ý';
            if (severity === 'success') return 'Tích cực';
            return 'Cập nhật';
        }

        function iconForAlert(item, severity) {
            if (item.category === 'ai') {
                return severity === 'danger' ? 'bi-stars' : 'bi-robot';
            }

            if (severity === 'danger') return 'bi-exclamation-octagon-fill';
            if (severity === 'warning') return 'bi-exclamation-circle-fill';
            if (severity === 'success') return 'bi-check-circle-fill';
            return 'bi-info-circle-fill';
        }

        function skeletonMarkup(count) {
            return Array.from({ length: count }).map(() => `
                <div class="notification-skeleton" aria-hidden="true">
                    <div class="notification-skeleton__icon"></div>
                    <div class="notification-skeleton__body">
                        <div class="notification-skeleton__line notification-skeleton__line--title"></div>
                        <div class="notification-skeleton__line notification-skeleton__line--meta"></div>
                        <div class="notification-skeleton__line notification-skeleton__line--message"></div>
                        <div class="notification-skeleton__line notification-skeleton__line--message-short"></div>
                    </div>
                </div>
            `).join('');
        }

        function loadingMarkup(label) {
            return `
                <div class="notification-loading">
                    <span class="notification-loading__pulse" aria-hidden="true"></span>
                    <span>${escapeHtml(label)}</span>
                </div>
                <div class="notification-skeleton-list">
                    ${skeletonMarkup(3)}
                </div>
            `;
        }

        function emptyMarkup(options) {
            const actionMarkup = options.action
                ? `
                    <button type="button"
                            class="notification-empty__button"
                            data-notification-action="${escapeAttribute(options.action)}"
                            data-notification-action-category="${escapeAttribute(options.category || '')}">
                        <i class="bi ${escapeAttribute(options.actionIcon || 'bi-arrow-clockwise')}"></i>
                        ${escapeHtml(options.actionLabel || 'Thử lại')}
                    </button>
                `
                : '';

            return `
                <div class="notification-empty${options.tone === 'error' ? ' notification-empty--error' : ''}">
                    <i class="bi ${escapeAttribute(options.icon || 'bi-bell-slash')}"></i>
                    <div class="notification-empty__title">${escapeHtml(options.title || 'Không có dữ liệu')}</div>
                    <div class="notification-empty__message">${escapeHtml(options.message || '')}</div>
                    ${actionMarkup}
                </div>
            `;
        }

        function errorMarkup(message, category) {
            return emptyMarkup({
                tone: 'error',
                category,
                icon: 'bi-wifi-off',
                title: 'Không thể tải thông báo',
                message,
                action: 'reload',
                actionIcon: 'bi-arrow-clockwise',
                actionLabel: 'Tải lại'
            });
        }

        function renderList(listElement, items, category) {
            if (!listElement) {
                return;
            }

            if (!items || items.length === 0) {
                listElement.innerHTML = category === 'ai'
                    ? emptyMarkup({
                        category,
                        icon: 'bi-stars',
                        title: 'Chưa có AI insight',
                        message: 'Bấm làm mới để phân tích dữ liệu KPI/OKR mới nhất.',
                        action: 'refresh-ai',
                        actionIcon: 'bi-stars',
                        actionLabel: 'Phân tích ngay'
                    })
                    : emptyMarkup({
                        category,
                        icon: 'bi-bell-slash',
                        title: 'Không có thông báo hệ thống',
                        message: 'Mọi cập nhật mới sẽ xuất hiện tại đây.'
                    });
                return;
            }

            listElement.innerHTML = items.map(item => {
                const severity = normalizeSeverity(item.severity);
                const meta = item.contextLabel
                    ? `<span class="notification-card__meta-pill">${escapeHtml(item.contextLabel)}</span>`
                    : '';
                const unreadClass = item.isRead ? '' : ' is-unread';
                const icon = iconForAlert(item, severity);
                const categoryChipLabel = item.category === 'ai' ? 'AI insight' : 'Hệ thống';
                const absoluteTime = formatAbsoluteTime(item.createdAt);
                const statusDot = item.isRead ? '' : '<span class="notification-card__status-dot" aria-hidden="true"></span>';

                return `
                    <button type="button"
                            class="notification-card${unreadClass}"
                            data-notification-id="${escapeAttribute(item.id)}"
                            data-notification-category="${escapeAttribute(item.category)}"
                            aria-label="${escapeAttribute(item.title || 'Thông báo')}"
                            title="${escapeAttribute(absoluteTime)}">
                        <span class="notification-card__icon notification-card__icon--${severity}">
                            <i class="bi ${icon}"></i>
                        </span>
                        <span class="notification-card__content">
                            <span class="notification-card__header">
                                <span class="notification-card__title-row">
                                    ${statusDot}
                                    <span class="notification-card__title">${escapeHtml(item.title || 'Thông báo')}</span>
                                </span>
                                <span class="notification-card__time" title="${escapeAttribute(absoluteTime)}">${escapeHtml(formatRelativeTime(item.createdAt))}</span>
                            </span>
                            <span class="notification-card__message">${escapeHtml(item.content || '')}</span>
                            <span class="notification-card__meta-row">
                                <span class="notification-card__meta-chip notification-card__meta-chip--neutral">${categoryChipLabel}</span>
                                ${meta}
                                <span class="notification-card__meta-chip notification-card__meta-chip--${severity}">${severityLabel(severity)}</span>
                            </span>
                        </span>
                    </button>
                `;
            }).join('');
        }

        function updateBellState(unreadCount) {
            const hasUnread = unreadCount > 0;
            center.classList.toggle('has-unread', hasUnread);
            bellButton?.classList.toggle('has-unread', hasUnread);
        }

        function renderCounts(payload) {
            const unreadCount = Number(payload?.unreadCount || 0);
            const unreadSystemCount = Number(payload?.unreadSystemCount || 0);
            const unreadAiCount = Number(payload?.unreadAiCount || 0);
            const totalVisibleItems = Number(payload?.systemAlerts?.length || 0) + Number(payload?.aiAlerts?.length || 0);

            if (subtitle) {
                if (unreadCount > 0) {
                    subtitle.textContent = `${formatCount(unreadCount)} mục mới • ${unreadSystemCount} hệ thống • ${unreadAiCount} AI`;
                } else if (totalVisibleItems > 0) {
                    subtitle.textContent = 'Bạn đã xem hết các cập nhật mới nhất';
                } else {
                    subtitle.textContent = 'Chưa có thông báo mới trong hệ thống';
                }
            }

            // Luôn ẩn dot – chỉ dùng pill số
            if (dot) {
                dot.classList.add('d-none');
            }

            if (pill) {
                pill.classList.toggle('d-none', unreadCount === 0);
                pill.textContent = formatCount(unreadCount);
            }

            if (systemBadge) {
                systemBadge.classList.toggle('d-none', unreadSystemCount === 0);
                systemBadge.textContent = formatCount(unreadSystemCount);
            }

            if (aiBadge) {
                aiBadge.classList.toggle('d-none', unreadAiCount === 0);
                aiBadge.textContent = formatCount(unreadAiCount);
            }

            if (metricAll) {
                metricAll.textContent = formatCount(unreadCount);
            }

            if (metricSystem) {
                metricSystem.textContent = formatCount(unreadSystemCount);
            }

            if (metricAi) {
                metricAi.textContent = formatCount(unreadAiCount);
            }

            const activeCategoryUnread = currentCategory() === 'ai' ? unreadAiCount : unreadSystemCount;
            if (markAllButton) {
                markAllButton.classList.toggle('d-none', activeCategoryUnread === 0);
                markAllButton.textContent = currentCategory() === 'ai'
                    ? 'Đánh dấu AI đã đọc'
                    : 'Đánh dấu hệ thống đã đọc';
            }

            updateBellState(unreadCount);
        }

        function renderPayload() {
            const payload = state.payload || {
                unreadCount: 0,
                unreadSystemCount: 0,
                unreadAiCount: 0,
                systemAlerts: [],
                aiAlerts: []
            };

            renderCounts(payload);
            renderList(systemList, payload.systemAlerts, 'system');
            renderList(aiList, payload.aiAlerts, 'ai');
        }

        function setLoadingState() {
            if (systemList) systemList.innerHTML = loadingMarkup('Đang tải thông báo hệ thống...');
            if (aiList) aiList.innerHTML = loadingMarkup('Đang tải AI insights...');
        }

        async function requestJson(url, options) {
            const response = await fetch(url, {
                credentials: 'same-origin',
                ...options
            });

            const contentType = response.headers.get('content-type') || '';
            let data = null;

            if (contentType.includes('application/json')) {
                data = await response.json();
            } else {
                const text = await response.text();
                data = text ? { message: text } : null;
            }

            return { response, data };
        }

        async function loadNotifications(force, options) {
            const silent = options?.silent === true;
            const shouldRefresh = force === true || !state.isLoaded || (Date.now() - state.lastLoadedAt) > 60000;

            if (!endpoint || state.isLoading || !shouldRefresh) {
                renderCounts(state.payload);
                return;
            }

            state.isLoading = true;

            if (!silent) {
                setLoadingState();
            }

            try {
                const { response, data } = await requestJson(`${endpoint}?take=6`);
                if (!response.ok || !data || data.success === false) {
                    throw new Error(data?.message || 'Không thể tải danh sách thông báo.');
                }

                state.payload = data;
                state.isLoaded = true;
                state.lastLoadedAt = Date.now();
                renderPayload();
            } catch (error) {
                if (!silent) {
                    const message = error instanceof Error ? error.message : 'Vui lòng thử lại sau.';
                    if (systemList) systemList.innerHTML = errorMarkup(message, 'system');
                    if (aiList) aiList.innerHTML = errorMarkup(message, 'ai');
                }
            } finally {
                state.isLoading = false;
            }
        }

        async function markAsRead(alertId) {
            if (!readEndpoint || !alertId) {
                return;
            }

            try {
                const { response, data } = await requestJson(readEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...window.antiForgeryHeaders()
                    },
                    body: JSON.stringify({ id: Number(alertId) })
                });

                if (!response.ok || !data || data.success === false) {
                    throw new Error(data?.message || 'Không thể cập nhật trạng thái thông báo.');
                }

                state.payload = data;
                state.isLoaded = true;
                state.lastLoadedAt = Date.now();
                renderPayload();
            } catch (error) {
                window.AppFeedback.toast({
                    tone: 'warning',
                    eyebrow: 'Thông báo',
                    title: 'Thông báo',
                    message: error instanceof Error ? error.message : 'Không thể đánh dấu đã đọc.'
                });
            }
        }

        async function markAllAsRead() {
            if (!readAllEndpoint) {
                return;
            }

            const category = currentCategory();
            markAllButton?.setAttribute('disabled', 'disabled');

            try {
                const { response, data } = await requestJson(readAllEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...window.antiForgeryHeaders()
                    },
                    body: JSON.stringify({ category })
                });

                if (!response.ok || !data || data.success === false) {
                    throw new Error(data?.message || 'Không thể cập nhật trạng thái thông báo.');
                }

                state.payload = data;
                state.isLoaded = true;
                state.lastLoadedAt = Date.now();
                renderPayload();
            } catch (error) {
                window.AppFeedback.toast({
                    tone: 'warning',
                    eyebrow: 'Thông báo',
                    title: 'Thông báo',
                    message: error instanceof Error ? error.message : 'Không thể đánh dấu tất cả là đã đọc.'
                });
            } finally {
                markAllButton?.removeAttribute('disabled');
            }
        }

        async function refreshAiAlerts(event) {
            event?.preventDefault();
            event?.stopPropagation();

            if (!aiRefreshEndpoint || !refreshAiButton) {
                return;
            }

            const originalMarkup = refreshAiButton.innerHTML;
            refreshAiButton.disabled = true;
            refreshAiButton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';

            try {
                const { response, data } = await requestJson(aiRefreshEndpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...window.antiForgeryHeaders()
                    },
                    body: JSON.stringify({})
                });

                if (!response.ok || !data || data.success === false) {
                    throw new Error(data?.warnings?.[0] || data?.message || 'Không thể làm mới AI insights.');
                }

                await loadNotifications(true, { silent: false });

                window.AppFeedback.toast({
                    tone: data.warnings?.length ? 'warning' : 'success',
                    eyebrow: 'AI insights',
                    title: 'AI Insights',
                    message: data.warnings?.[0] || 'Đã cập nhật AI insights mới nhất.'
                });
            } catch (error) {
                window.AppFeedback.toast({
                    tone: 'error',
                    eyebrow: 'AI insights',
                    title: 'AI Insights',
                    message: error instanceof Error ? error.message : 'Không thể kết nối AI. Vui lòng thử lại sau.'
                });
            } finally {
                refreshAiButton.disabled = false;
                refreshAiButton.innerHTML = originalMarkup;
            }
        }

        function handleListClick(event) {
            const actionButton = event.target instanceof Element
                ? event.target.closest('[data-notification-action]')
                : null;

            if (actionButton) {
                event.preventDefault();
                event.stopPropagation();

                const action = actionButton.dataset.notificationAction;
                if (action === 'refresh-ai') {
                    refreshAiAlerts(event);
                    return;
                }

                loadNotifications(true, { silent: false });
                return;
            }

            const card = event.target instanceof Element
                ? event.target.closest('[data-notification-id]')
                : null;

            if (!card || card.classList.contains('is-pending') || !card.classList.contains('is-unread')) {
                return;
            }

            card.classList.add('is-pending');
            markAsRead(card.dataset.notificationId).finally(() => card.classList.remove('is-pending'));
        }

        center.addEventListener('show.bs.dropdown', () => {
            loadNotifications(false, { silent: false });
        });

        tabButtons.forEach(button => {
            button.addEventListener('shown.bs.tab', event => {
                state.activeCategory = event.target?.dataset?.notificationTab || 'system';
                renderCounts(state.payload);
            });
        });

        systemList?.addEventListener('click', handleListClick);
        aiList?.addEventListener('click', handleListClick);

        markAllButton?.addEventListener('click', markAllAsRead);
        reloadButton?.addEventListener('click', () => loadNotifications(true, { silent: false }));
        refreshAiButton?.addEventListener('click', refreshAiAlerts);

        const queueRefresh = () => window.setTimeout(
            () => loadNotifications(true, { silent: true }),
            5000);

        renderCounts(state.payload);
        queueRefresh();
        window.setInterval(() => {
            if (document.visibilityState === 'visible') {
                loadNotifications(true, { silent: true });
            }
        }, 90000);

        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                loadNotifications(true, { silent: true });
            }
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-notification-center]').forEach(initNotificationCenter);
    });
})();

document.addEventListener('DOMContentLoaded', function () {

    // --- Sidebar Toggle ---
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            const isExpanded = document.documentElement.classList.toggle('sidebar-expanded');
            // Ghi nhớ trạng thái người dùng chọn
            localStorage.setItem('sidebarState', isExpanded ? 'expanded' : 'collapsed');
        });
    }

    function closeMobileSidebar() {
        document.documentElement.classList.remove('sidebar-expanded');
        sidebar?.classList.remove('show');
        overlay?.classList.remove('show');
        localStorage.setItem('sidebarState', 'collapsed');
    }

    if (overlay) {
        overlay.addEventListener('click', function () {
            closeMobileSidebar();
        });
    }

    // --- Active Menu Highlighting ---
    function normalizeSidebarPath(path) {
        if (!path) return '/';

        const normalized = path.toLowerCase();
        if (normalized.length > 1 && normalized.endsWith('/')) {
            return normalized.slice(0, -1);
        }

        return normalized;
    }

    const currentPath = normalizeSidebarPath(window.location.pathname);

    const settingsPanel = document.querySelector('[data-settings-panel]');
    const settingsTriggers = Array.from(document.querySelectorAll('[data-settings-panel-trigger]'));
    const settingsCloseControls = Array.from(document.querySelectorAll('[data-settings-panel-close]'));
    const settingsItems = Array.from(document.querySelectorAll('.settings-panel__item[href]'));

    function setSettingsPanelOpen(isOpen) {
        if (!settingsPanel || !settingsTriggers.length) return;

        document.documentElement.classList.toggle('settings-panel-open', isOpen);
        settingsPanel.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        settingsTriggers.forEach(function (trigger) {
            trigger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
        });

        if (isOpen) {
            window.setTimeout(function () {
                settingsPanel.querySelector('.settings-panel__close, .settings-panel__item')?.focus({ preventScroll: true });
            }, 0);
        }
    }

    function getSettingsUrl(item) {
        try {
            return new URL(item.getAttribute('href'), window.location.origin);
        } catch {
            return null;
        }
    }

    let hasActiveSettingsItem = false;
    settingsItems.forEach(function (item) {
        const itemUrl = getSettingsUrl(item);
        if (!itemUrl || itemUrl.origin !== window.location.origin) return;

        const itemPath = normalizeSidebarPath(itemUrl.pathname);
        const isMatch = currentPath === itemPath || (itemPath !== '/' && currentPath.startsWith(itemPath + '/'));
        if (!isMatch) return;

        item.classList.add('active');
        hasActiveSettingsItem = true;
    });

    if (hasActiveSettingsItem) {
        settingsTriggers.forEach(trigger => trigger.classList.add('is-active'));
    }

    settingsTriggers.forEach(function (trigger) {
        trigger.addEventListener('click', function () {
            const isOpen = document.documentElement.classList.contains('settings-panel-open');
            setSettingsPanelOpen(!isOpen);
        });
    });

    settingsCloseControls.forEach(function (control) {
        control.addEventListener('click', function () {
            setSettingsPanelOpen(false);
        });
    });

    settingsItems.forEach(function (item) {
        item.addEventListener('click', function () {
            setSettingsPanelOpen(false);
        });
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && document.documentElement.classList.contains('settings-panel-open')) {
            setSettingsPanelOpen(false);
        }
    });

    const sidebarLinks = Array.from(document.querySelectorAll('.sidebar-link[href]'));
    let activeSidebarLink = null;
    let activeSidebarScore = -1;

    function getSidebarUrl(link) {
        try {
            return new URL(link.getAttribute('href'), window.location.origin);
        } catch {
            return null;
        }
    }

    function expandSidebarParent(link) {
        const submenu = link.closest('.sidebar-submenu');
        if (!submenu) return;

        submenu.classList.add('show');
        const toggle = submenu.previousElementSibling;
        if (toggle) {
            toggle.setAttribute('aria-expanded', 'true');
            toggle.classList.remove('collapsed');
        }
    }

    function setSidebarActiveLink(link, options = {}) {
        sidebarLinks.forEach(function (item) {
            item.classList.remove('active', 'is-pending');
            item.removeAttribute('aria-current');
            item.removeAttribute('aria-busy');
        });

        link.classList.add('active');
        if (options.pending) {
            link.classList.add('is-pending');
            link.setAttribute('aria-busy', 'true');
        } else {
            link.setAttribute('aria-current', 'page');
        }

        expandSidebarParent(link);
    }

    function isRegularSidebarNavigation(event, link) {
        if (event.defaultPrevented || event.button !== 0) return false;
        if (event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return false;
        if (link.getAttribute('data-bs-toggle') === 'collapse') return false;

        const target = (link.getAttribute('target') || '').toLowerCase();
        if (target && target !== '_self') return false;

        const href = link.getAttribute('href');
        if (!href || href === '#') return false;

        const targetUrl = getSidebarUrl(link);
        if (!targetUrl || targetUrl.origin !== window.location.origin) return false;

        const isHashOnlySamePage =
            targetUrl.pathname === window.location.pathname &&
            targetUrl.search === window.location.search &&
            Boolean(targetUrl.hash);

        return !isHashOnlySamePage;
    }

    const pageContent = document.querySelector('.page-content');
    const instantNavigationLinks = sidebarLinks.filter(link => link.dataset.instantNav === 'kpi');
    const instantNavigationCache = new Map();
    const instantNavigationRequests = new Map();
    const instantNavigationTtlMs = 60000;
    let instantNavigationSequence = 0;
    let instantNavigationSettleFrame = 0;
    let instantNavigationSettleReleaseFrame = 0;

    function getInstantNavigationKey(url) {
        return normalizeSidebarPath(url.pathname) + url.search;
    }

    function getInstantNavigationLink(url) {
        const normalizedPath = normalizeSidebarPath(url.pathname);
        return instantNavigationLinks.find(function (link) {
            const linkUrl = getSidebarUrl(link);
            return linkUrl && normalizeSidebarPath(linkUrl.pathname) === normalizedPath;
        }) || null;
    }

    function supportsInstantNavigation() {
        return Boolean(pageContent && window.history && typeof window.history.pushState === 'function' && window.DOMParser);
    }

    function isInstantNavigationUrl(url) {
        return Boolean(url && url.origin === window.location.origin && getInstantNavigationLink(url));
    }

    function getFreshInstantNavigationEntry(url) {
        const entry = instantNavigationCache.get(getInstantNavigationKey(url));
        if (!entry) return null;

        return Date.now() - entry.fetchedAt <= instantNavigationTtlMs ? entry : null;
    }

    function isExecutablePageScript(script) {
        const type = (script.getAttribute('type') || '').trim().toLowerCase();
        return !type ||
            type === 'module' ||
            type === 'text/javascript' ||
            type === 'application/javascript';
    }

    function collectPageScripts(root) {
        if (!root) return [];

        return Array.from(root.querySelectorAll('script'))
            .filter(isExecutablePageScript)
            .map(script => ({
                src: script.src || '',
                text: script.src ? '' : (script.textContent || ''),
                type: script.getAttribute('type') || ''
            }))
            .filter(script => script.src || script.text.trim().length > 0);
    }

    function collectStyles(root) {
        if (!root) return [];

        return Array.from(root.querySelectorAll('link[rel="stylesheet"][href]'))
            .map(link => link.href || link.getAttribute('href'))
            .filter(Boolean);
    }

    async function ensureInstantNavigationStyles(styles) {
        if (!styles?.length) return;

        const existing = new Set(Array.from(document.querySelectorAll('link[rel="stylesheet"][href]'))
            .map(link => link.href || link.getAttribute('href')));
        const pending = styles
            .filter(href => !existing.has(href))
            .map(href => new Promise(resolve => {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = href;
                link.dataset.instantStyle = 'true';
                link.onload = resolve;
                link.onerror = resolve;
                document.head.appendChild(link);
            }));

        await Promise.all(pending);
    }

    function createInstantNavigationEntry(contentElement, scriptsRoot, title, stylesRoot) {
        if (!contentElement) {
            throw new Error('Không tìm thấy vùng nội dung trang.');
        }

        const contentClone = contentElement.cloneNode(true);
        const pageScripts = collectPageScripts(contentClone)
            .concat(collectPageScripts(scriptsRoot));

        contentClone.querySelectorAll('script').forEach(function (script) {
            if (isExecutablePageScript(script)) {
                script.remove();
            }
        });

        return {
            html: contentClone.innerHTML,
            scripts: pageScripts,
            styles: collectStyles(stylesRoot),
            title: title || document.title,
            fetchedAt: Date.now()
        };
    }

    function parseInstantNavigationResponse(html) {
        const doc = new DOMParser().parseFromString(html, 'text/html');
        return createInstantNavigationEntry(
            doc.querySelector('main.page-content'),
            doc.querySelector('[data-page-scripts]'),
            doc.title,
            doc.head
        );
    }

    function captureCurrentInstantNavigationPage() {
        if (!supportsInstantNavigation()) return;

        const currentUrl = new URL(window.location.href);
        if (!isInstantNavigationUrl(currentUrl)) return;

        try {
            instantNavigationCache.set(
                getInstantNavigationKey(currentUrl),
                createInstantNavigationEntry(pageContent, document.querySelector('[data-page-scripts]'), document.title, document.head)
            );
        } catch (error) {
            console.warn('Instant navigation cache skipped:', error);
        }
    }

    async function fetchInstantNavigationPage(url, options = {}) {
        const key = getInstantNavigationKey(url);
        const cached = !options.force ? getFreshInstantNavigationEntry(url) : null;
        if (cached) return cached;

        if (!options.force && instantNavigationRequests.has(key)) {
            return instantNavigationRequests.get(key);
        }

        const request = fetch(url.href, {
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'X-Instant-Navigation': 'true'
            }
        })
            .then(async response => {
                if (!response.ok) {
                    throw new Error(`Không tải được trang (${response.status}).`);
                }

                const entry = parseInstantNavigationResponse(await response.text());
                instantNavigationCache.set(key, entry);
                return entry;
            })
            .finally(() => {
                instantNavigationRequests.delete(key);
            });

        instantNavigationRequests.set(key, request);
        return request;
    }

    function cleanupDynamicPageState() {
        document.querySelectorAll('.modal.show').forEach(function (modal) {
            try {
                bootstrap.Modal.getInstance(modal)?.hide();
            } catch {
                modal.classList.remove('show');
            }
        });

        document.querySelectorAll('.modal-backdrop').forEach(backdrop => backdrop.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
        document.querySelectorAll('.select2-container').forEach(container => container.remove());
    }

    async function executeInstantNavigationScripts(scripts) {
        for (const descriptor of scripts) {
            const script = document.createElement('script');
            if (descriptor.type) {
                script.type = descriptor.type;
            }

            if (descriptor.src) {
                script.src = descriptor.src;
                script.async = false;
                await new Promise((resolve, reject) => {
                    script.addEventListener('load', resolve, { once: true });
                    script.addEventListener('error', () => reject(new Error(`Không tải được script ${descriptor.src}.`)), { once: true });
                    document.body.appendChild(script);
                });
                script.remove();
                continue;
            }

            script.text = descriptor.text;
            if (descriptor.type === 'module') {
                await new Promise((resolve, reject) => {
                    script.addEventListener('load', resolve, { once: true });
                    script.addEventListener('error', () => reject(new Error('Không thực thi được module của trang.')), { once: true });
                    document.body.appendChild(script);
                });
                script.remove();
                continue;
            }

            document.body.appendChild(script);
            script.remove();
        }
    }

    function refreshDynamicPageBehaviors() {
        window.applyMeasurementUnitBehavior?.(pageContent);
        window.refreshSelect2?.();
        document.dispatchEvent(new CustomEvent('instant:navigation-ready', {
            detail: { path: window.location.pathname, root: pageContent }
        }));
    }

    function setInstantNavigationLoading(link) {
        if (!pageContent) return;

        pageContent.setAttribute('aria-busy', 'true');
        pageContent.classList.add('is-route-pending');
        pageContent.dataset.loadingLabel =
            link?.dataset.instantTitle || link?.textContent?.trim() || 'Đang mở trang';
    }

    function settleInstantNavigationContent() {
        if (!pageContent) return;

        window.cancelAnimationFrame(instantNavigationSettleFrame);
        window.cancelAnimationFrame(instantNavigationSettleReleaseFrame);
        pageContent.classList.add('is-route-settling');
        instantNavigationSettleFrame = window.requestAnimationFrame(function () {
            instantNavigationSettleReleaseFrame = window.requestAnimationFrame(function () {
                pageContent.classList.remove('is-route-settling');
            });
        });
    }

    async function applyInstantNavigationEntry(url, entry, options = {}) {
        await ensureInstantNavigationStyles(entry.styles || []);
        cleanupDynamicPageState();
        pageContent.setAttribute('aria-busy', 'true');
        pageContent.innerHTML = entry.html;

        if (entry.title) {
            document.title = entry.title;
        }

        if (!options.skipHistory) {
            history.pushState({ instantNavigation: true }, entry.title || '', url.href);
        }

        const activeLink = options.link || getInstantNavigationLink(url);
        if (activeLink) {
            setSidebarActiveLink(activeLink);
        }

        window.scrollTo({ top: 0, left: 0 });
        await executeInstantNavigationScripts(entry.scripts || []);
        refreshDynamicPageBehaviors();
        pageContent.classList.remove('is-route-pending');
        pageContent.removeAttribute('aria-busy');
        delete pageContent.dataset.loadingLabel;
        settleInstantNavigationContent();
    }

    async function navigateInstantly(url, link, options = {}) {
        if (!supportsInstantNavigation() || !isInstantNavigationUrl(url)) {
            window.location.href = url.href;
            return;
        }

        const sequence = ++instantNavigationSequence;
        const cached = getFreshInstantNavigationEntry(url);

        if (cached) {
            await applyInstantNavigationEntry(url, cached, { ...options, link });
            return;
        }

        setInstantNavigationLoading(link);

        try {
            const entry = await fetchInstantNavigationPage(url);
            if (sequence !== instantNavigationSequence) return;

            await applyInstantNavigationEntry(url, entry, { ...options, link });
        } catch (error) {
            console.warn('Instant navigation fallback:', error);
            window.location.href = url.href;
        }
    }

    function prefetchInstantNavigationLink(link) {
        if (!supportsInstantNavigation()) return;

        const url = getSidebarUrl(link);
        if (!isInstantNavigationUrl(url)) return;

        fetchInstantNavigationPage(url).catch(error => {
            console.warn('Instant navigation prefetch failed:', error);
        });
    }

    sidebarLinks.forEach(function (link) {
        const linkUrl = getSidebarUrl(link);
        if (!linkUrl || linkUrl.origin !== window.location.origin) return;

        const linkPath = normalizeSidebarPath(linkUrl.pathname);
        const isExactMatch = currentPath === linkPath;
        const isNestedMatch = linkPath !== '/' && currentPath.startsWith(linkPath + '/');

        if (!isExactMatch && !isNestedMatch) return;

        const matchScore = isExactMatch ? (1000 + linkPath.length) : linkPath.length;
        if (matchScore > activeSidebarScore) {
            activeSidebarLink = link;
            activeSidebarScore = matchScore;
        }
    });

    if (activeSidebarLink) {
        setSidebarActiveLink(activeSidebarLink);
    }

    // --- Close sidebar on link click (mobile) ---
    sidebarLinks.forEach(function (link) {
        link.addEventListener('click', function (event) {
            const isRegularNavigation = isRegularSidebarNavigation(event, link);

            if (isRegularNavigation) {
                setSidebarActiveLink(link, { pending: true });
            }

            if (window.innerWidth < 992) {
                closeMobileSidebar();
            }

            if (isRegularNavigation && link.dataset.instantNav === 'kpi') {
                const targetUrl = getSidebarUrl(link);
                if (isInstantNavigationUrl(targetUrl)) {
                    event.preventDefault();
                    // Keep the current screen visible while the destination is fetched.
                    // Replacing it with a full-page placeholder made database latency
                    // feel like a blank/frozen page.
                    navigateInstantly(targetUrl, link, { preserveContent: true });
                }
            }
        });
    });

    pageContent?.addEventListener('click', function (event) {
        if (event.defaultPrevented || event.button !== 0 ||
            event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) {
            return;
        }

        const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
        if (!link || link.hasAttribute('download') ||
            (link.getAttribute('target') && link.getAttribute('target') !== '_self')) {
            return;
        }

        const url = getSidebarUrl(link);
        if (!isInstantNavigationUrl(url) || url.hash) return;

        event.preventDefault();
        navigateInstantly(url, getInstantNavigationLink(url), { preserveContent: true });
    });

    pageContent?.addEventListener('submit', function (event) {
        const form = event.target instanceof HTMLFormElement ? event.target : null;
        if (!form || (form.method || 'get').toLowerCase() !== 'get') return;

        const url = new URL(form.action || window.location.href, window.location.origin);
        if (!isInstantNavigationUrl(url)) return;

        const query = new URLSearchParams();
        new FormData(form).forEach(function (value, key) {
            const textValue = String(value).trim();
            if (textValue) query.append(key, textValue);
        });
        url.search = query.toString();

        event.preventDefault();
        navigateInstantly(url, getInstantNavigationLink(url), { preserveContent: true });
    });

    if (supportsInstantNavigation() && instantNavigationLinks.length) {
        captureCurrentInstantNavigationPage();
        history.replaceState({ instantNavigation: true }, document.title, window.location.href);
        // Prefetch only on hover/focus; eager loading all KPI pages competes with the
        // page the user is opening and can make the first navigation feel slow.

        instantNavigationLinks.forEach(function (link) {
            link.addEventListener('mouseenter', () => prefetchInstantNavigationLink(link), { once: true });
            link.addEventListener('focus', () => prefetchInstantNavigationLink(link), { once: true });
        });

        // Warm only the primary KPI screen after the current page has settled.
        // Prefetching every KPI route competes with foreground work, while warming
        // this single high-frequency destination makes the sidebar click immediate.
        const primaryKpiLink = instantNavigationLinks.find(function (link) {
            const url = getSidebarUrl(link);
            return url && normalizeSidebarPath(url.pathname) === '/KPIs';
        });
        if (primaryKpiLink && currentPath !== '/KPIs') {
            const warmPrimaryKpi = () => prefetchInstantNavigationLink(primaryKpiLink);
            window.setTimeout(function () {
                if (typeof window.requestIdleCallback === 'function') {
                    window.requestIdleCallback(warmPrimaryKpi, { timeout: 1500 });
                } else {
                    warmPrimaryKpi();
                }
            }, 500);
        }

        window.addEventListener('popstate', function () {
            const url = new URL(window.location.href);
            const link = getInstantNavigationLink(url);
            if (!link) {
                window.location.reload();
                return;
            }

            setSidebarActiveLink(link, { pending: true });
            navigateInstantly(url, link, { skipHistory: true, preserveContent: true });
        });
    }

    // React only when crossing the mobile/desktop boundary instead of running
    // storage and class writes on every resize event.
    const desktopViewport = window.matchMedia('(min-width: 992px)');
    const clearMobileOnlySidebarState = function (event) {
        if (!event.matches) return;
        sidebar?.classList.remove('show');
        overlay?.classList.remove('show');
    };
    if (typeof desktopViewport.addEventListener === 'function') {
        desktopViewport.addEventListener('change', clearMobileOnlySidebarState);
    } else {
        desktopViewport.addListener(clearMobileOnlySidebarState);
    }

    // --- Gắn sự kiện cho các phần tử chưa có chức năng ---

    // 1. Header Search (Tìm kiếm nhanh)
    const headerSearchInput = document.querySelector('.header-search input');
    const headerSearch = document.querySelector('.header-search');
    
    if (headerSearchInput && headerSearch) {
        // Create results dropdown if not exists
        let resultsDropdown = document.createElement('div');
        resultsDropdown.className = 'search-results-dropdown';
        resultsDropdown.innerHTML = '<div class="search-results-inner"></div>';
        headerSearch.appendChild(resultsDropdown);
        
        const resultsInner = resultsDropdown.querySelector('.search-results-inner');
        let searchTimeout;

        headerSearchInput.addEventListener('input', function () {
            const term = this.value.trim();
            clearTimeout(searchTimeout);

            if (term.length < 2) {
                resultsDropdown.classList.remove('show');
                return;
            }

            searchTimeout = setTimeout(async () => {
                try {
                    const response = await fetch(`/Search/QuickSearch?term=${encodeURIComponent(term)}`);
                    const results = await response.json();
                    
                    renderSearchResults(results, term);
                    resultsDropdown.classList.add('show');
                } catch (error) {
                    console.error('Search error:', error);
                }
            }, 300);
        });

        // Hide dropdown when clicking outside
        document.addEventListener('click', function (e) {
            if (!headerSearch.contains(e.target)) {
                resultsDropdown.classList.remove('show');
            }
        });

        // Show dropdown again on focus if term is valid
        headerSearchInput.addEventListener('focus', function () {
            if (this.value.trim().length >= 2) {
                resultsDropdown.classList.add('show');
            }
        });

        function renderSearchResults(results, term) {
            if (!results || results.length === 0) {
                resultsInner.innerHTML = `<div class="search-no-results">Không tìm thấy kết quả nào cho "<strong>${escapeHtml(term)}</strong>"</div>`;
                return;
            }

            // Group by type
            const groups = results.reduce((acc, item) => {
                const type = item.type || 'Khác';
                if (!acc[type]) acc[type] = [];
                acc[type].push(item);
                return acc;
            }, Object.create(null));

            let html = '';
            for (const type in groups) {
                html += `<div class="search-section-header">${escapeHtml(type)}</div>`;
                groups[type].forEach(item => {
                    const safeUrl = escapeAttribute(sanitizeSearchUrl(item.url));
                    const safeIcon = escapeAttribute(String(item.icon || '').replace(/[^\w-]/g, ''));
                    const subtitle = escapeHtml(item.subtitle || '');
                    html += `
                        <a href="${safeUrl}" class="search-item">
                            <div class="search-item-icon">
                                <i class="bi ${safeIcon}"></i>
                            </div>
                            <div class="search-item-info">
                                <div class="search-item-title">${highlightMatch(item.title, term)}</div>
                                <div class="search-item-subtitle">${subtitle}</div>
                            </div>
                        </a>
                    `;
                });
            }
            resultsInner.innerHTML = html;
        }

        function highlightMatch(text, term) {
            const safeText = escapeHtml(text || '');
            const safeTerm = escapeRegExp(term || '');
            if (!safeTerm) return safeText;

            const regex = new RegExp(`(${safeTerm})`, 'gi');
            return safeText.replace(regex, '<span style="color: var(--primary); font-weight: 800;">$1</span>');
        }
    }

    // 2. Header Notification Bell is handled by Bootstrap dropdown in _Layout.

    // 3. Generic coming-soon dropdown items.
    document.querySelectorAll('.dropdown-item[data-coming-soon]').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            window.AppFeedback.comingSoon(link.textContent.trim());
        });
    });

    // 4. Sidebar links to non-existent pages (/Shop, /MyOrders)
    document.querySelectorAll('.sidebar-link[href="/Shop"], .sidebar-link[href="/MyOrders"]').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const name = this.textContent.trim();
            window.AppFeedback.comingSoon(name);
        });
    });

    // =========================================================
    // THÊM MỚI: Lưu và phục hồi vị trí thanh cuộn của Sidebar
    // =========================================================
    
    // Tìm thẻ chứa thanh cuộn (thường là .sidebar-nav hoặc chính thẻ #sidebar)
    const sidebarNav = document.querySelector('.sidebar-nav') || document.getElementById('sidebar');

    if (sidebarNav) {
        // 1. Phục hồi vị trí cuộn cũ ngay khi trang vừa load xong
        const savedScrollPosition = sessionStorage.getItem("sidebarScrollPosition");
        if (savedScrollPosition) {
            sidebarNav.scrollTop = parseInt(savedScrollPosition, 10);
        }

        let sidebarScrollSaveTimer = 0;
        let lastSavedSidebarScroll = Number.parseInt(savedScrollPosition || '0', 10) || 0;

        function persistSidebarScrollPosition() {
            window.clearTimeout(sidebarScrollSaveTimer);
            sidebarScrollSaveTimer = 0;
            const nextPosition = Math.round(sidebarNav.scrollTop);
            if (nextPosition === lastSavedSidebarScroll) return;

            lastSavedSidebarScroll = nextPosition;
            sessionStorage.setItem("sidebarScrollPosition", String(nextPosition));
        }

        // Storage writes are non-critical; save after scrolling settles.
        sidebarNav.addEventListener("scroll", function () {
            window.clearTimeout(sidebarScrollSaveTimer);
            sidebarScrollSaveTimer = window.setTimeout(persistSidebarScrollPosition, 120);
        }, { passive: true });

        // 3. Dự phòng: Lưu lại lần cuối trước khi trình duyệt chuyển trang
        window.addEventListener("beforeunload", persistSidebarScrollPosition);
    }

    // =========================================================
    // THÊM MỚI: Khởi tạo Select2 Toàn cục (Global Select2)
    // =========================================================

    function initGlobalSelect2() {
        if (typeof jQuery === 'undefined' || typeof jQuery.fn.select2 === 'undefined') {
            console.error('Select2: jQuery hoặc Select2 library chưa được tải.');
            return;
        }

        // Tìm tất cả các thẻ <select> chưa được khởi tạo
        $('select:not(.select2-hidden-accessible):not(.no-select2)').each(function () {
            const $el = $(this);
            
            // Cấu hình dựa trên data attributes hoặc mặc định
            const placeholder = $el.data('placeholder') || $el.find('option[value=""]').text() || 'Chọn một tùy chọn';
            const allowClearData = $el.data('allow-clear');
            const allowClear = allowClearData !== undefined
                ? allowClearData === true || allowClearData === 'true'
                : !$el.prop('required');
            
            // Chỉ hiện ô tìm kiếm nếu số lượng option > 8
            const minResultsForSearch = $el.data('minimum-results-for-search') || ($el.find('option').length > 8 ? 0 : -1);

            // Tìm component modal gần nhất nếu có để gán dropdownParent nhằm tránh lỗi focus trên Modal Bootstrap
            const modalParent = $el.closest('.modal');
            const selectOptions = {
                placeholder: placeholder,
                allowClear: allowClear,
                minimumResultsForSearch: minResultsForSearch,
                width: '100%',
                language: {
                    noResults: function () { return "Không tìm thấy kết quả"; },
                    searching: function () { return "Đang tìm kiếm..."; }
                }
            };

            if (modalParent.length > 0) {
                selectOptions.dropdownParent = modalParent;
            }

            $el.select2(selectOptions);

            // Forward Select2's jQuery-only changes once for native listeners.
            $el.on('change.select2', function (event) {
                if (!event.originalEvent) {
                    this.dispatchEvent(new Event('change', { bubbles: true }));
                }
                
                // Trigger validation if available
                if (typeof $(this).valid === 'function') {
                    $(this).valid();
                }
            });
        });
    }

    // Khởi tạo lần đầu
    initGlobalSelect2();

    // Hỗ trợ khởi tạo lại khi có dynamic content (AJAX/Modal)
    $(document).on('shown.bs.modal', function() {
        initGlobalSelect2();
    });

    // Xuất ra window để gọi thủ công nếu cần
    window.refreshSelect2 = initGlobalSelect2;
});
