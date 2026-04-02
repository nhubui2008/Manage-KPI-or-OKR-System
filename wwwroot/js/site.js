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
    if (headerSearchInput) {
        headerSearchInput.addEventListener('focus', function () {
            showComingSoonToast('Tìm kiếm nhanh toàn hệ thống');
            this.blur();
        });
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
});