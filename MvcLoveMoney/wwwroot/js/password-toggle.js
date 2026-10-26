// "Show password" checkbox: shows / hides every password input in the same form.
// Markup: <input type="checkbox" data-show-password> inside the form (see _ShowPasswordToggle.cshtml)
document.addEventListener('change', function (e) {
    const checkbox = e.target.closest('[data-show-password]');
    if (!checkbox) return;

    const form = checkbox.closest('form');
    if (!form) return;

    // Mark the password inputs the first time, so they can be found again after becoming type="text"
    form.querySelectorAll('input[type="password"], input[data-password-field]').forEach(function (input) {
        input.dataset.passwordField = 'true';
        input.type = checkbox.checked ? 'text' : 'password';
    });
});
