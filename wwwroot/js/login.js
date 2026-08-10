// login.js - Toggle password visibility & button loading

document.addEventListener('DOMContentLoaded', function () {
    function initializePasswordToggle(inputId, toggleId, fieldLabel) {
        const input = document.getElementById(inputId);
        const toggle = document.getElementById(toggleId);
        if (!input || !toggle) return;

        toggle.setAttribute('aria-controls', input.id);
        toggle.addEventListener('click', function () {
            const willShowPassword = input.type === 'password';
            input.type = willShowPassword ? 'text' : 'password';
            toggle.setAttribute(
                'aria-label',
                `${willShowPassword ? 'Ẩn' : 'Hiện'} ${fieldLabel}`);
        });
    }

    initializePasswordToggle('password', 'passwordToggle', 'mật khẩu');
    initializePasswordToggle('confirmPassword', 'confirmPasswordToggle', 'mật khẩu xác nhận');

    const passwordInput = document.getElementById('password');
    const usernameInput = document.getElementById('username');
    const demoAccountButtons = document.querySelectorAll('.demo-account');

    demoAccountButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            // Manage active class
            demoAccountButtons.forEach(btn => btn.classList.remove('active'));
            button.classList.add('active');

            if (usernameInput && !usernameInput.readOnly) {
                usernameInput.value = button.dataset.username || '';
                usernameInput.dispatchEvent(new Event('input', { bubbles: true }));
            }

            if (passwordInput) {
                passwordInput.value = button.dataset.password || '';
                passwordInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });
    });

    const registerForm = document.getElementById('registerForm');
    registerForm?.addEventListener('submit', function (event) {
        const hasJQueryValidation = typeof window.jQuery !== 'undefined'
            && typeof window.jQuery.fn.valid === 'function';
        const isValid = hasJQueryValidation
            ? window.jQuery(registerForm).valid()
            : registerForm.checkValidity();

        if (!isValid || event.defaultPrevented) {
            return;
        }

        const submitButton = registerForm.querySelector('.comfort-button');
        if (!submitButton || submitButton.disabled) {
            event.preventDefault();
            return;
        }

        submitButton.disabled = true;
        submitButton.classList.add('loading');
        submitButton.setAttribute('aria-busy', 'true');
    });
});
