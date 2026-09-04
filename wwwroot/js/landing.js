/**
 * LIGHT 3D CRYSTAL LANDING JAVASCRIPT - NEXTGEN SYSTEM
 * Interactive Sticky Scroll Showcase, Role Simulator, FAQ Accordion, Header Blur
 */

document.addEventListener('DOMContentLoaded', function () {
    // --- 1. Header Scroll Shadow & Blur ---
    const header = document.querySelector('.nova-header');
    function updateHeader() {
        if (header) {
            header.classList.toggle('scrolled', window.scrollY > 20);
        }
    }
    window.addEventListener('scroll', updateHeader, { passive: true });
    updateHeader();

    // --- 2. Interactive Sticky Scroll Showcase (Left Scroll, Right Sticky Switch) ---
    const scrollySteps = document.querySelectorAll('.nova-scrolly-step');
    const stickyScreens = document.querySelectorAll('.nova-sticky-screen');
    const scrollyAddress = document.getElementById('scrollyAddress');
    const scrollyBadge = document.getElementById('scrollyPageBadge');

    function activateScreen(screenId) {
        let activeScreenEl = null;

        stickyScreens.forEach(function (screen) {
            if (screen.id === screenId) {
                screen.classList.add('active');
                activeScreenEl = screen;
            } else {
                screen.classList.remove('active');
            }
        });

        if (activeScreenEl) {
            const url = activeScreenEl.getAttribute('data-url');
            const index = activeScreenEl.getAttribute('data-index');
            if (scrollyAddress && url) {
                scrollyAddress.innerHTML = '<i class="bi bi-lock-fill"></i> ' + url;
            }
            if (scrollyBadge && index) {
                scrollyBadge.textContent = index;
            }
        }
    }

    if ('IntersectionObserver' in window && scrollySteps.length > 0) {
        const stepObserver = new IntersectionObserver(
            function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        const step = entry.target;
                        const screenId = step.getAttribute('data-screen');

                        scrollySteps.forEach(s => s.classList.remove('active'));
                        step.classList.add('active');

                        if (screenId) {
                            activateScreen(screenId);
                        }
                    }
                });
            },
            {
                rootMargin: '-15% 0px -35% 0px',
                threshold: 0.15
            }
        );

        scrollySteps.forEach(function (step) {
            stepObserver.observe(step);

            // Optional click to smooth-scroll
            step.addEventListener('click', function () {
                const screenId = step.getAttribute('data-screen');
                scrollySteps.forEach(s => s.classList.remove('active'));
                step.classList.add('active');
                if (screenId) activateScreen(screenId);
            });
        });
    }

    // Direct scroll spy for ultra-responsive sync
    function checkScrollSpy() {
        if (window.innerWidth < 1024) return;
        const triggerY = window.innerHeight * 0.45;
        let currentActive = null;

        scrollySteps.forEach(function (step) {
            const rect = step.getBoundingClientRect();
            if (rect.top <= triggerY && rect.bottom >= triggerY) {
                currentActive = step;
            }
        });

        if (currentActive && !currentActive.classList.contains('active')) {
            const screenId = currentActive.getAttribute('data-screen');
            scrollySteps.forEach(s => s.classList.remove('active'));
            currentActive.classList.add('active');
            if (screenId) activateScreen(screenId);
        }
    }
    window.addEventListener('scroll', checkScrollSpy, { passive: true });


    // --- 3. Role Simulator Switcher ---
    const roleButtons = document.querySelectorAll('.nova-role-btn');
    const roleCards = document.querySelectorAll('.nova-role-card');

    roleButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            const roleKey = btn.getAttribute('data-role');
            if (!roleKey) return;

            roleButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            roleCards.forEach(card => {
                if (card.getAttribute('data-role-card') === roleKey) {
                    card.style.display = 'block';
                    card.style.animation = 'fadeInScale 0.3s ease forwards';
                } else {
                    card.style.display = 'none';
                }
            });
        });
    });

    // --- 4. FAQ Accordion ---
    const faqItems = document.querySelectorAll('.nova-faq-item');
    faqItems.forEach(function (item) {
        const btn = item.querySelector('.nova-faq-question');
        if (btn) {
            btn.addEventListener('click', function () {
                const wasActive = item.classList.contains('active');
                faqItems.forEach(i => i.classList.remove('active'));
                if (!wasActive) {
                    item.classList.add('active');
                }
            });
        }
    });

    // --- 5. Smooth Scroll for Internal Anchors ---
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            const href = this.getAttribute('href');
            if (href === '#' || href.length <= 1) return;
            const target = document.querySelector(href);
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });
});
