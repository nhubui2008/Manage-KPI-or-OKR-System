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
    const toastToneMap = {
        success: { icon: 'bi-check-circle-fill', fallbackTitle: 'Thành công', eyebrow: 'Hoàn tất' },
        error: { icon: 'bi-x-circle-fill', fallbackTitle: 'Có lỗi xảy ra', eyebrow: 'Lỗi' },
        danger: { icon: 'bi-x-circle-fill', fallbackTitle: 'Có lỗi xảy ra', eyebrow: 'Lỗi' },
        warning: { icon: 'bi-exclamation-triangle-fill', fallbackTitle: 'Lưu ý', eyebrow: 'Cảnh báo' },
        info: { icon: 'bi-info-circle-fill', fallbackTitle: 'Thông báo', eyebrow: 'Thông tin' }
    };

    function normalizeTone(tone) {
        const normalized = String(tone || 'info').toLowerCase();
        if (normalized === 'danger') return 'error';
        return toastToneMap[normalized] ? normalized : 'info';
    }

    function ensureToastContainer() {
        let container = document.getElementById('appToastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'appToastContainer';
            container.className = 'app-toast-container';
            container.setAttribute('aria-live', 'polite');
            container.setAttribute('aria-atomic', 'true');
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
        window.setTimeout(() => toast.remove(), 260);
    }

    window.showAppToast = function (optionsOrTitle, message, tone) {
        const options = typeof optionsOrTitle === 'object' && optionsOrTitle !== null
            ? optionsOrTitle
            : { title: optionsOrTitle, message, tone };

        const normalizedTone = normalizeTone(options.tone);
        const toneConfig = toastToneMap[normalizedTone];
        const container = ensureToastContainer();
        const toast = document.createElement('div');
        const requestedTimeout = Number(options.timeout);
        const timeout = Number.isFinite(requestedTimeout) ? Math.max(1400, requestedTimeout) : 4600;

        toast.className = `app-toast app-toast--${normalizedTone}`;
        toast.style.setProperty('--toast-duration', `${timeout}ms`);
        toast.innerHTML = `
            <div class="app-toast__icon">
                <i class="bi ${toneConfig.icon}"></i>
            </div>
            <div class="app-toast__body">
                <div class="app-toast__eyebrow">${escapeHtml(options.eyebrow || toneConfig.eyebrow)}</div>
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
        container.prepend(toast);

        while (container.children.length > 4) {
            container.lastElementChild?.remove();
        }

        requestAnimationFrame(() => toast.classList.add('is-visible'));
        if (timeout > 0) {
            scheduleToastDismiss(toast, timeout);
        }
    };

    window.showComingSoonToast = function (featureName) {
        window.showAppToast({
            tone: 'warning',
            eyebrow: 'Tính năng',
            title: 'Đang phát triển',
            message: `Chức năng "${featureName}" sẽ được cập nhật trong phiên bản tới.`
        });
    };

    const nativeAlert = typeof window.alert === 'function' ? window.alert.bind(window) : null;
    window.alert = function (message) {
        if (document.body && typeof window.showAppToast === 'function') {
            window.showAppToast({
                tone: 'info',
                eyebrow: 'Thông báo hệ thống',
                title: 'Thông báo',
                message: String(message ?? '')
            });
            return;
        }

        nativeAlert?.(message);
    };
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
                window.showAppToast({
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
                window.showAppToast({
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

                window.showAppToast({
                    tone: data.warnings?.length ? 'warning' : 'success',
                    eyebrow: 'AI insights',
                    title: 'AI Insights',
                    message: data.warnings?.[0] || 'Đã cập nhật AI insights mới nhất.'
                });
            } catch (error) {
                window.showAppToast({
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

        const queueRefresh = typeof window.requestIdleCallback === 'function'
            ? () => window.requestIdleCallback(() => loadNotifications(true, { silent: true }), { timeout: 1500 })
            : () => window.setTimeout(() => loadNotifications(true, { silent: true }), 800);

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
        sidebar?.classList.remove('show');
        overlay?.classList.remove('show');
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
    const sidebarLinks = Array.from(document.querySelectorAll('.sidebar-link[href]'));
    let activeSidebarLink = null;
    let activeSidebarScore = -1;

    sidebarLinks.forEach(function (link) {
        const href = link.getAttribute('href');
        if (!href || href === '#') return;

        const linkPath = normalizeSidebarPath(href);
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
        activeSidebarLink.classList.add('active');

        // Expand parent submenu if exists
        const submenu = activeSidebarLink.closest('.sidebar-submenu');
        if (submenu) {
            submenu.classList.add('show');
            const toggle = submenu.previousElementSibling;
            if (toggle) {
                toggle.setAttribute('aria-expanded', 'true');
                toggle.classList.remove('collapsed');
            }
        }
    }

    // --- Close sidebar on link click (mobile) ---
    sidebarLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth < 992) {
                closeMobileSidebar();
            }
        });
    });

    // --- Handle window resize ---
    window.addEventListener('resize', function () {
        // Chỉ đóng 'show' (phiên bản mobile overlay) khi quay lại màn hình lớn
        if (window.innerWidth >= 992) {
            closeMobileSidebar();
        }
    });

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
            showComingSoonToast(link.textContent.trim());
        });
    });

    // 4. Sidebar links to non-existent pages (/Shop, /MyOrders)
    document.querySelectorAll('.sidebar-link[href="/Shop"], .sidebar-link[href="/MyOrders"]').forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            const name = this.textContent.trim();
            showComingSoonToast(name);
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

        // 2. Lắng nghe sự kiện cuộn để lưu vị trí mới nhất vào sessionStorage
        sidebarNav.addEventListener("scroll", function () {
            sessionStorage.setItem("sidebarScrollPosition", sidebarNav.scrollTop);
        });

        // 3. Dự phòng: Lưu lại lần cuối trước khi trình duyệt chuyển trang
        window.addEventListener("beforeunload", function () {
            sessionStorage.setItem("sidebarScrollPosition", sidebarNav.scrollTop);
        });
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

            $el.select2({
                placeholder: placeholder,
                allowClear: allowClear,
                minimumResultsForSearch: minResultsForSearch,
                width: '100%',
                dropdownParent: $el.closest('.modal').length ? $el.closest('.modal') : $(document.body),
                language: {
                    noResults: function () { return "Không tìm thấy kết quả"; },
                    searching: function () { return "Đang tìm kiếm..."; }
                }
            });

            // Trigger change event to ensure native and jQuery listeners are notified
            $el.on('change.select2', function () {
                // Trigger natural change event for non-jQuery listeners
                this.dispatchEvent(new Event('change', { bubbles: true }));
                
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
