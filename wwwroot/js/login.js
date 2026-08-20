/**
 * LIGHT 3D CRYSTAL AUTH JAVASCRIPT - VIETMACH SYSTEM
 * Features:
 * 1. Password Visibility Toggle
 * 2. Form Validation & Loading Spinner State
 * 3. Smooth 3D Mouse Parallax & Card Tilt Controller
 */

document.addEventListener('DOMContentLoaded', function () {
    // --- 1. Password Visibility Toggle ---
    document.querySelectorAll('.auth-password-toggle').forEach(function (toggle) {
        const inputId = toggle.getAttribute('aria-controls');
        const input = inputId ? document.getElementById(inputId) : null;
        if (!input) return;

        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const willShow = input.type === 'password';
            input.type = willShow ? 'text' : 'password';
            toggle.setAttribute('aria-label', willShow ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
            toggle.setAttribute('aria-pressed', String(willShow));
            
            const icon = toggle.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye', !willShow);
                icon.classList.toggle('bi-eye-slash', willShow);
            }
            input.focus();
        });
    });

    // --- 2. Login Form Validation & Loading ---
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function (event) {
            const username = document.getElementById('username');
            const password = document.getElementById('password');
            const usernameError = document.getElementById('usernameError');
            const passwordError = document.getElementById('passwordError');
            let isValid = true;

            if (usernameError) usernameError.textContent = '';
            if (passwordError) passwordError.textContent = '';
            username?.classList.remove('input-validation-error');
            password?.classList.remove('input-validation-error');

            if (!username?.value.trim()) {
                if (usernameError) usernameError.textContent = 'Vui lòng nhập tên đăng nhập hoặc email.';
                username?.classList.add('input-validation-error');
                isValid = false;
            }
            
            if (!password?.value) {
                if (passwordError) passwordError.textContent = 'Vui lòng nhập mật khẩu.';
                password?.classList.add('input-validation-error');
                isValid = false;
            }

            if (!isValid) {
                event.preventDefault();
                (username?.value.trim() ? password : username)?.focus();
                return;
            }

            setLoading(loginForm);
        });

        // Clear error on input
        ['username', 'password'].forEach(function (id) {
            const input = document.getElementById(id);
            const err = document.getElementById(id + 'Error');
            input?.addEventListener('input', function () {
                if (input.classList.contains('input-validation-error')) {
                    input.classList.remove('input-validation-error');
                    if (err) err.textContent = '';
                }
            });
        });
    }

    // --- 3. Generic Auth Forms Support ---
    document.querySelectorAll('.auth-form:not(#loginForm)').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            const hasJQueryValidation = typeof window.jQuery !== 'undefined'
                && typeof window.jQuery.fn.valid === 'function';
            const isValid = hasJQueryValidation
                ? window.jQuery(form).valid()
                : form.checkValidity();

            if (!isValid || event.defaultPrevented) return;
            setLoading(form);
        });
    });

    function setLoading(form) {
        const submitButton = form.querySelector('.auth-submit-btn, .auth-submit');
        if (!submitButton || submitButton.disabled) return;
        submitButton.disabled = true;
        submitButton.classList.add('loading');
        submitButton.setAttribute('aria-busy', 'true');
    }

    // --- 4. Smooth 3D Mouse Parallax & Card Tilt Controller ---
    const scene = document.getElementById('crystalScene');
    const card = document.getElementById('authMainCard');
    const crystals = document.querySelectorAll('.crystal-item');

    const isTouchDevice = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (scene && !isTouchDevice && !prefersReducedMotion && window.innerWidth >= 992) {
        let mouseX = 0;
        let mouseY = 0;
        let currentX = 0;
        let currentY = 0;
        let isMoving = false;
        let animationFrameId = null;

        function onMouseMove(e) {
            const rect = scene.getBoundingClientRect();
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;

            // Normalized coordinates (-1 to 1)
            mouseX = (e.clientX - centerX) / centerX;
            mouseY = (e.clientY - centerY) / centerY;

            if (!isMoving) {
                isMoving = true;
                animationFrameId = requestAnimationFrame(updateParallax);
            }
        }

        function updateParallax() {
            // Smooth easing interpolation (lerp factor 0.08)
            currentX += (mouseX - currentX) * 0.08;
            currentY += (mouseY - currentY) * 0.08;

            // Shift crystal objects according to individual depth
            crystals.forEach(function (crystal) {
                const depth = parseFloat(crystal.getAttribute('data-depth')) || 0.04;
                const moveX = currentX * depth * 800;
                const moveY = currentY * depth * 800;
                crystal.style.transform = `translate3d(${moveX.toFixed(2)}px, ${moveY.toFixed(2)}px, 0)`;
            });

            // Subtle 3D Card tilt
            if (card) {
                const tiltX = -currentY * 3.5;
                const tiltY = currentX * 3.5;
                card.style.transform = `perspective(1000px) rotateX(${tiltX.toFixed(2)}deg) rotateY(${tiltY.toFixed(2)}deg)`;
            }

            // Continue loop while mouse displacement is significant
            if (Math.abs(mouseX - currentX) > 0.001 || Math.abs(mouseY - currentY) > 0.001) {
                animationFrameId = requestAnimationFrame(updateParallax);
            } else {
                isMoving = false;
            }
        }

        window.addEventListener('mousemove', onMouseMove, { passive: true });

        // Reset on mouse leave
        document.addEventListener('mouseleave', function () {
            mouseX = 0;
            mouseY = 0;
            if (!isMoving) {
                isMoving = true;
                animationFrameId = requestAnimationFrame(updateParallax);
            }
        });
    }

    // --- 5. Quick Demo Account Autofill ---
    document.querySelectorAll('.auth-demo-chip').forEach(function (chip) {
        chip.addEventListener('click', function () {
            const username = chip.getAttribute('data-username');
            const password = chip.getAttribute('data-password');
            const usernameInput = document.getElementById('username');
            const passwordInput = document.getElementById('password');
            const usernameError = document.getElementById('usernameError');
            const passwordError = document.getElementById('passwordError');

            if (usernameInput && username) {
                usernameInput.value = username;
                usernameInput.classList.remove('input-validation-error');
                usernameInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (passwordInput && password) {
                passwordInput.value = password;
                passwordInput.classList.remove('input-validation-error');
                passwordInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (usernameError) usernameError.textContent = '';
            if (passwordError) passwordError.textContent = '';

            // Visual active indicator on chip
            document.querySelectorAll('.auth-demo-chip').forEach(function (c) {
                c.classList.remove('active');
            });
            chip.classList.add('active');

            // Focus submit button or password input
            const submitBtn = document.getElementById('loginSubmitBtn');
            if (submitBtn) {
                submitBtn.focus();
            }
        });
    });
});

