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


});
