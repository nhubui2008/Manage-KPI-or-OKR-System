(() => {
    const root = document.querySelector('.kpi-detail-page');
    if (!root || root.dataset.kpiDetailsInitialized === 'true') return;

    root.dataset.kpiDetailsInitialized = 'true';

    root.querySelectorAll('[data-okr-link-scope]').forEach(scope => {
        if (scope.dataset.okrLinkInitialized === 'true') return;

        const okrSelect = scope.querySelector('.js-okr-select');
        const keyResultSelect = scope.querySelector('.js-kr-select');
        if (!okrSelect || !keyResultSelect) return;

        scope.dataset.okrLinkInitialized = 'true';

        const allKeyResults = Array.from(keyResultSelect.querySelectorAll('option[data-okr-id]')).map(option => ({
            value: option.value,
            text: option.textContent,
            okrId: option.dataset.okrId
        }));

        const filterKeyResults = () => {
            const selectedOkrId = okrSelect.value;
            const selectedKeyResultId = keyResultSelect.value;
            const matchingKeyResults = allKeyResults.filter(option => option.okrId === selectedOkrId);

            keyResultSelect.replaceChildren();
            keyResultSelect.disabled = !selectedOkrId;

            const placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = !selectedOkrId
                ? '-- Chọn OKR trước --'
                : matchingKeyResults.length
                    ? '-- Không liên kết Key Result --'
                    : '-- OKR này chưa có Key Result --';
            keyResultSelect.appendChild(placeholder);

            matchingKeyResults.forEach(item => {
                const option = document.createElement('option');
                option.value = item.value;
                option.textContent = item.text;
                option.dataset.okrId = item.okrId;
                keyResultSelect.appendChild(option);
            });

            if (matchingKeyResults.some(item => item.value === selectedKeyResultId)) {
                keyResultSelect.value = selectedKeyResultId;
            }
        };

        okrSelect.addEventListener('change', filterKeyResults);
        filterKeyResults();
    });
})();
