/**
 * Bonus Rules Module JavaScript
 * Handles modal editing, number formatting, and dot-stripping on form submit.
 */

(function () {
    'use strict';

    window.showEditModal = function (id, rankId, bonus, amount) {
        const editId = document.getElementById('editId');
        if (editId) editId.value = id;

        const editRankId = document.getElementById('editRankId');
        if (editRankId) editRankId.value = rankId;

        const editBonusPercentage = document.getElementById('editBonusPercentage');
        if (editBonusPercentage) {
            editBonusPercentage.value = bonus ? parseFloat(bonus).toString() : '';
        }

        const editFixedAmount = document.getElementById('editFixedAmount');
        if (editFixedAmount) {
            const formattedAmount = amount ? amount.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".") : '';
            editFixedAmount.value = formattedAmount;
        }

        const modalEl = document.getElementById('editModal');
        if (modalEl && window.bootstrap?.Modal) {
            const modalInstance = window.bootstrap.Modal.getInstance(modalEl) || new window.bootstrap.Modal(modalEl);
            modalInstance.show();
        }
    };

    function initBonusRules() {
        const page = document.querySelector('.bonus-rules-page');
        if (!page) return;

        // Event delegation for number-format inputs
        document.addEventListener('input', function (e) {
            if (e.target && e.target.classList.contains('number-format')) {
                let val = e.target.value.replace(/\D/g, '');
                e.target.value = val.replace(/\B(?=(\d{3})+(?!\d))/g, ".");
            }
        });

        // Strip dots before form submit
        document.querySelectorAll('form').forEach(form => {
            if (form.dataset.bonusRuleSubmitted) return;
            form.dataset.bonusRuleSubmitted = 'true';

            form.addEventListener('submit', function () {
                this.querySelectorAll('.number-format').forEach(input => {
                    input.value = input.value.replace(/\./g, '');
                });
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initBonusRules);
    } else {
        initBonusRules();
    }
})();
