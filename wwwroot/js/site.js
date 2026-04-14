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

    if (overlay) {
        overlay.addEventListener('click', function () {
            sidebar.classList.remove('show');
            overlay.classList.remove('show');
        });
    }

    // --- Active Menu Highlighting ---
    const currentPath = window.location.pathname.toLowerCase();
    const sidebarLinks = document.querySelectorAll('.sidebar-link[href]');

    sidebarLinks.forEach(function (link) {
        const href = link.getAttribute('href');
        if (!href || href === '#') return;

        const linkPath = href.toLowerCase();

        if (currentPath === linkPath || (linkPath !== '/' && currentPath.startsWith(linkPath))) {
            link.classList.add('active');

            // Expand parent submenu if exists
            const submenu = link.closest('.sidebar-submenu');
            if (submenu) {
                submenu.classList.add('show');
                const toggle = submenu.previousElementSibling;
                if (toggle) {
                    toggle.setAttribute('aria-expanded', 'true');
                    toggle.classList.remove('collapsed');
                }
            }
        }
    });

    // --- Close sidebar on link click (mobile) ---
    sidebarLinks.forEach(function (link) {
        link.addEventListener('click', function () {
            if (window.innerWidth < 992) {
                sidebar.classList.remove('show');
                overlay.classList.remove('show');
            }
        });
    });

    // --- Handle window resize ---
    window.addEventListener('resize', function () {
        // Chỉ đóng 'show' (phiên bản mobile overlay) khi quay lại màn hình lớn
        if (window.innerWidth >= 992) {
            sidebar.classList.remove('show');
            overlay.classList.remove('show');
        }
    });

    // =========================================================
    // TOAST "CHỨC NĂNG ĐANG PHÁT TRIỂN"
    // =========================================================

    // Tạo toast container nếu chưa có
    if (!document.getElementById('comingSoonToastContainer')) {
        const toastContainer = document.createElement('div');
        toastContainer.id = 'comingSoonToastContainer';
        toastContainer.style.cssText = 'position:fixed;top:24px;right:24px;z-index:99999;display:flex;flex-direction:column;gap:10px;pointer-events:none;';
        document.body.appendChild(toastContainer);
    }

    /**
     * Hiển thị toast thông báo "Chức năng đang phát triển"
     * @param {string} featureName - Tên chức năng
     */
    window.showComingSoonToast = function (featureName) {
        const container = document.getElementById('comingSoonToastContainer');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = 'coming-soon-toast';
        toast.style.cssText = `
            pointer-events:auto;
            display:flex;align-items:center;gap:12px;
            background:linear-gradient(135deg,#1e293b 0%,#334155 100%);
            color:#f1f5f9;
            padding:14px 22px;
            border-radius:14px;
            box-shadow:0 8px 32px rgba(0,0,0,0.28),0 0 0 1px rgba(255,255,255,0.06);
            font-family:'Inter',sans-serif;font-size:0.9rem;font-weight:500;
            max-width:420px;
            opacity:0;transform:translateX(40px);
            transition:all .4s cubic-bezier(.16,1,.3,1);
            backdrop-filter:blur(12px);
            border-left:4px solid #f59e0b;
        `;
        toast.innerHTML = `
            <div style="flex-shrink:0;width:38px;height:38px;border-radius:10px;background:linear-gradient(135deg,#f59e0b,#d97706);display:flex;align-items:center;justify-content:center;">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#fff" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M12 9v4"/><path d="M12 17h.01"/>
                    <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
                </svg>
            </div>
            <div style="flex:1;">
                <div style="font-weight:700;font-size:0.85rem;color:#fbbf24;margin-bottom:2px;">🚧 Đang phát triển</div>
                <div style="font-size:0.84rem;line-height:1.4;color:#cbd5e1;">Chức năng <strong style="color:#f8fafc;">"${featureName}"</strong> sẽ được cập nhật trong phiên bản tới.</div>
            </div>
            <button onclick="this.parentElement.style.opacity='0';this.parentElement.style.transform='translateX(40px)';setTimeout(()=>this.parentElement.remove(),400);"
                style="flex-shrink:0;background:none;border:none;color:#94a3b8;cursor:pointer;padding:4px;font-size:1.2rem;line-height:1;">&times;</button>
        `;
        container.appendChild(toast);

        // Animate in
        requestAnimationFrame(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateX(0)';
        });

        // Auto-dismiss after 4s
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(40px)';
            setTimeout(() => toast.remove(), 400);
        }, 4000);
    };

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

    // 2. Header Notification Bell (Thông báo)
    const notifBtn = document.querySelector('.header-icon-btn[title="Thông báo hệ thống"]');
    if (notifBtn) {
        notifBtn.addEventListener('click', function (e) {
            e.preventDefault();
            showComingSoonToast('Thông báo hệ thống');
        });
    }

    // 3. Settings dropdown item (Cài đặt)
    const settingsLink = document.querySelector('.dropdown-item[href="#"]');
    if (settingsLink && settingsLink.textContent.includes('Cài đặt')) {
        settingsLink.addEventListener('click', function (e) {
            e.preventDefault();
            showComingSoonToast('Cài đặt tài khoản');
        });
    }

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
            
            // Xóa bỏ hoàn toàn nút 'x' (clear button) trên toàn dự án theo yêu cầu
            const allowClear = false;
            
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

    // =========================================================
    // THÊM MỚI: Tối ưu hóa giao diện Thông báo đẩy (Push Notifications) Toàn Cục
    // Chuyển rải rác các alert HTML thô thành Toast Notification hiện đại
    // =========================================================
    const flashAlerts = document.querySelectorAll('.alert-success[role="alert"], .alert-danger[role="alert"]');
    
    flashAlerts.forEach(function (alertEl) {
        if (alertEl.querySelector('ul') || alertEl.classList.contains('validation-summary-errors')) return;

        let message = '';
        alertEl.childNodes.forEach(node => {
            if (node.nodeType === 3) { 
                message += node.textContent;
            } else if (node.tagName && node.tagName.toLowerCase() !== 'i' && node.tagName.toLowerCase() !== 'svg') {
                message += node.innerText || node.textContent;
            }
        });
        
        message = message.trim();
        if (!message) return;

        let toastIcon = alertEl.classList.contains('alert-success') ? 'success' : 'error';
        
        // Ẩn vĩnh viễn HTML cũ
        alertEl.style.setProperty('display', 'none', 'important');
        alertEl.classList.remove('d-flex');
        
        // Đẩy Toast xịn xò
        setTimeout(() => {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: toastIcon,
                title: message,
                showConfirmButton: false,
                timer: 4000,
                timerProgressBar: true,
                background: '#ffffff',
                color: '#1e293b',
                customClass: {
                    popup: 'shadow-lg border-0 rounded-4',
                    title: 'fw-medium fs-6'
                },
                didOpen: (toast) => {
                    toast.addEventListener('mouseenter', Swal.stopTimer);
                    toast.addEventListener('mouseleave', Swal.resumeTimer);
                }
            });
        }, 100); // delay nhẹ để UI ổn định
    });
});
