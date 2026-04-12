// ============================================
// Site JavaScript - KPI/OKR Management System
// ============================================

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
                resultsInner.innerHTML = `<div class="search-no-results">Không tìm thấy kết quả nào cho "<strong>${term}</strong>"</div>`;
                return;
            }

            // Group by type
            const groups = results.reduce((acc, item) => {
                if (!acc[item.type]) acc[item.type] = [];
                acc[item.type].push(item);
                return acc;
            }, {});

            let html = '';
            for (const type in groups) {
                html += `<div class="search-section-header">${type}</div>`;
                groups[type].forEach(item => {
                    html += `
                        <a href="${item.url}" class="search-item">
                            <div class="search-item-icon">
                                <i class="bi ${item.icon}"></i>
                            </div>
                            <div class="search-item-info">
                                <div class="search-item-title">${highlightMatch(item.title, term)}</div>
                                <div class="search-item-subtitle">${item.subtitle}</div>
                            </div>
                        </a>
                    `;
                });
            }
            resultsInner.innerHTML = html;
        }

        function highlightMatch(text, term) {
            const regex = new RegExp(`(${term})`, 'gi');
            return text.replace(regex, '<span style="color: var(--primary); font-weight: 800;">$1</span>');
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
            const allowClear = $el.prop('required') ? false : true;
            
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