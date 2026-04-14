// login.js - Toggle password visibility & button loading

document.addEventListener('DOMContentLoaded', function () {
    // Password toggle
    const passwordInput = document.getElementById('password');
    const passwordToggle = document.getElementById('passwordToggle');
    if (passwordInput && passwordToggle) {
        passwordToggle.addEventListener('click', function () {
            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';
            passwordToggle.querySelector('.eye-open').style.display = isPassword ? 'none' : 'block';
            passwordToggle.querySelector('.eye-closed').style.display = isPassword ? 'block' : 'none';
        });
    }

    const usernameInput = document.getElementById('username');
    const demoAccountButtons = document.querySelectorAll('.demo-account');

    demoAccountButtons.forEach(function (button) {
        button.addEventListener('click', function () {
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

});
