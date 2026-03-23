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
});
