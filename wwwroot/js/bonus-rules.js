(function () {
    'use strict';
    const normalize = value => String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
    const rawNumber = value => { const parsed = Number.parseFloat(value); return Number.isFinite(parsed) ? parsed : 0; };
    const formatNumber = value => { const digits = String(value || '').replace(/\D/g, ''); return digits ? new Intl.NumberFormat('vi-VN').format(Number(digits)) : ''; };
    function initNumberFormat(root) {
        root.querySelectorAll('[data-number-format]').forEach(input => {
            if (input.dataset.bonusFormatBound) return;
            input.dataset.bonusFormatBound = 'true';
            input.addEventListener('input', () => { input.value = formatNumber(input.value); });
        });
    }
    function setSubmitting(form) {
        if (form.dataset.submitting || !form.checkValidity()) return false;
        form.dataset.submitting = 'true'; form.classList.add('is-submitting');
        form.querySelectorAll('[data-number-format]').forEach(input => { input.value = input.value.replace(/\./g, ''); });
        const button = form.querySelector('button[type="submit"]'); const spinner = button && button.querySelector('.spinner-border');
        if (button) button.disabled = true; if (spinner) spinner.hidden = false;
        return true;
    }
    function initForms(root) {
        root.querySelectorAll('form[data-bonus-submit]').forEach(form => {
            if (form.dataset.bonusSubmitBound) return;
            form.dataset.bonusSubmitBound = 'true';
            form.addEventListener('submit', event => { if (!setSubmitting(form)) event.preventDefault(); });
        });
    }
    function initList(root) {
        const search = root.querySelector('#bonusSearch'), filter = root.querySelector('#bonusFilter'), sort = root.querySelector('#bonusSort');
        if (!search || !filter || !sort || root.dataset.listBound) return;
        root.dataset.listBound = 'true'; const reset = root.querySelector('#bonusReset'), clear = root.querySelector('#bonusSearchClear'), result = root.querySelector('#bonusResultCount'), empty = root.querySelector('#bonusFilterEmpty');
        const items = Array.from(root.querySelectorAll('.bonus-rule-item')).map((node, index) => ({ node, index, id: node.dataset.ruleId }));
        const containers = [root.querySelector('#bonusRulesTableBody'), root.querySelector('#bonusRulesMobileList')].filter(Boolean);
        const matches = item => { const pct = rawNumber(item.node.dataset.bonusPercentage), fixed = rawNumber(item.node.dataset.fixedAmount); const term = normalize(search.value); const haystack = normalize(item.node.dataset.rankCode + ' ' + item.node.dataset.rankDescription); const kind = filter.value; return (!term || haystack.includes(term)) && (kind === 'all' || (kind === 'percentage' && pct > 0) || (kind === 'fixed' && fixed > 0) || (kind === 'both' && pct > 0 && fixed > 0) || (kind === 'empty' && pct <= 0 && fixed <= 0)); };
        function compare(a,b) { const key = sort.value; if (key === 'default') return a.index-b.index; const dir = key.endsWith('desc') ? -1 : 1; const value = item => key.startsWith('rank') ? normalize(item.node.dataset.rankCode) : rawNumber(key.startsWith('percentage') ? item.node.dataset.bonusPercentage : item.node.dataset.fixedAmount); const av=value(a),bv=value(b); return (av < bv ? -1 : av > bv ? 1 : a.index-b.index) * dir; }
        function apply() { const visible = new Set(items.filter(matches).map(item => item.id)); items.forEach(item => { item.node.hidden = !visible.has(item.id); }); const ordered = items.slice().sort(compare); containers.forEach(container => { ordered.forEach(item => { const clone = Array.from(container.children).find(child => child.dataset.ruleId === item.id); if (clone) container.appendChild(clone); }); }); const count = visible.size; if (result) result.textContent = `${count} quy tắc${count === 0 ? ' phù hợp' : ''}`; if (empty) empty.hidden = count !== 0; const dirty = Boolean(search.value || filter.value !== 'all' || sort.value !== 'default'); reset.disabled = !dirty; clear.hidden = !search.value; }
        function resetAll() { search.value=''; filter.value='all'; sort.value='default'; apply(); search.focus(); }
        [search,filter,sort].forEach(control => control.addEventListener(control === search ? 'input' : 'change', apply)); reset.addEventListener('click',resetAll); clear.addEventListener('click',resetAll); root.querySelectorAll('[data-bonus-reset]').forEach(button => button.addEventListener('click',resetAll)); apply();
    }
    function initCreatePreview(root) { const rank=root.querySelector('#RankId'), pct=root.querySelector('#BonusPercentage'), fixed=root.querySelector('#FixedAmount'); if (!rank || !pct || !fixed) return; const update=()=>{ const option=rank.options[rank.selectedIndex]; root.querySelector('#previewRank').textContent=option && option.value ? option.text : '--'; root.querySelector('#previewPct').textContent=pct.value ? `${pct.value}%` : '--'; root.querySelector('#previewFixed').textContent=fixed.value ? `${new Intl.NumberFormat('vi-VN').format(Number(fixed.value))} ₫` : '--'; }; [rank,pct,fixed].forEach(input=>input.addEventListener(input===rank?'change':'input',update)); update(); }
    window.showEditModal = function (id, rankId, bonus, amount) { const modalElement=document.getElementById('editModal'); if (!modalElement) return; const idInput=modalElement.querySelector('#editId'), rankInput=modalElement.querySelector('#editRankId'), pctInput=modalElement.querySelector('#editBonusPercentage'), amountInput=modalElement.querySelector('#editFixedAmount'), error=modalElement.querySelector('#editModalError'); idInput.value=id || ''; rankInput.value=rankId || ''; pctInput.value=bonus == null || bonus === '' ? '' : String(bonus); amountInput.value=formatNumber(amount); error.hidden = Boolean(rankInput.value); if (!rankInput.value) { error.textContent='Không tìm thấy xếp loại tương ứng; không thể lưu thay đổi an toàn.'; } if (window.bootstrap) window.bootstrap.Modal.getOrCreateInstance(modalElement).show(); };
    function init(root) { initNumberFormat(root); initForms(root); initList(root); initCreatePreview(root); }
    function boot() { document.querySelectorAll('.bonus-rules-page').forEach(init); }
    document.addEventListener('DOMContentLoaded',boot); document.addEventListener('instant:navigation-ready',boot);
}());
