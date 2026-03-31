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
            if (window.innerWidth < 992) {
                sidebar.classList.toggle('show');
                overlay.classList.toggle('show');
            } else {
                document.body.classList.toggle('sidebar-collapsed');
            }
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