document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.auth-password-toggle').forEach(function (toggle) {
        const inputId = toggle.getAttribute('aria-controls');
        const input = inputId ? document.getElementById(inputId) : null;
        if (!input) return;

        toggle.addEventListener('click', function () {
            const willShow = input.type === 'password';
            input.type = willShow ? 'text' : 'password';
            toggle.setAttribute('aria-label', willShow ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
            toggle.setAttribute('aria-pressed', String(willShow));
            const icon = toggle.querySelector('i');
            icon?.classList.toggle('bi-eye', !willShow);
            icon?.classList.toggle('bi-eye-slash', willShow);
            input.focus();
        });
    });

    const loginForm = document.getElementById('loginForm');
    loginForm?.addEventListener('submit', function (event) {
        const username = document.getElementById('username');
        const password = document.getElementById('password');
        const usernameError = document.getElementById('usernameError');
        const passwordError = document.getElementById('passwordError');
        let isValid = true;

        if (usernameError) usernameError.textContent = '';
        if (passwordError) passwordError.textContent = '';
        if (!username?.value.trim()) {
            if (usernameError) usernameError.textContent = 'Vui lòng nhập tên đăng nhập.';
            isValid = false;
        }
        if (!password?.value) {
            if (passwordError) passwordError.textContent = 'Vui lòng nhập mật khẩu.';
            isValid = false;
        }
        if (!isValid) {
            event.preventDefault();
            (username?.value.trim() ? password : username)?.focus();
            return;
        }

        setLoading(loginForm);
    });

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
        const submitButton = form.querySelector('.auth-submit');
        if (!submitButton || submitButton.disabled) return;
        submitButton.disabled = true;
        submitButton.classList.add('loading');
        submitButton.setAttribute('aria-busy', 'true');
    }
});
