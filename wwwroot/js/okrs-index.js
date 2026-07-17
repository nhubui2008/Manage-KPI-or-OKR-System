(function () {
    'use strict';

    function byId(id) {
        return document.getElementById(id);
    }

    function normalizeDecimalValue(value) {
        return String(value ?? '').replace(',', '.');
    }

    function closeAllOkrDropdowns() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Dropdown) return;
        document.querySelectorAll('.okr-index-page .okr-action-dropdown [data-bs-toggle="dropdown"]').forEach(toggleEl => {
            bootstrap.Dropdown.getOrCreateInstance(toggleEl).hide();
        });
    }

    function getModal(id) {
        const el = byId(id);
        if (!el || typeof bootstrap === 'undefined' || !bootstrap.Modal) {
            return null;
        }
        return bootstrap.Modal.getOrCreateInstance(el);
    }

    function setText(id, text) {
        const el = byId(id);
        if (el) el.textContent = text;
    }

    function setValue(id, value) {
        const el = byId(id);
        if (el) el.value = value;
    }

    function resetForm(form) {
        if (!form) return;
        form.reset();
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        form.querySelectorAll('[data-submitting="true"]').forEach(el => {
            el.disabled = false;
            delete el.dataset.submitting;
        });
    }

    function setSubmitLoading(button, loading, loadingText) {
        if (!button) return;
        if (loading) {
            button.dataset.originalHtml = button.innerHTML;
            button.dataset.submitting = 'true';
            button.disabled = true;
            button.innerHTML = loadingText || '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang lưu...';
        } else {
            button.disabled = false;
            delete button.dataset.submitting;
            if (button.dataset.originalHtml) {
                button.innerHTML = button.dataset.originalHtml;
            }
        }
    }

    function guardFormSubmit(form) {
        form.addEventListener('submit', function (event) {
            const submitter = event.submitter || form.querySelector('[type="submit"]');
            if (submitter && submitter.dataset.submitting === 'true') {
                event.preventDefault();
                return;
            }
            setSubmitLoading(submitter, true);
        });
    }

    function openAddKrModal(okrId, okrName) {
        closeAllOkrDropdowns();
        if (!byId('addKrModal')) return;
        const form = byId('addKrForm');
        resetForm(form);
        setValue('krOkrId', okrId);
        setText('krOkrNameDisplay', 'Objective: ' + (okrName || ''));
        if (window.applyMeasurementUnitBehavior) {
            window.applyMeasurementUnitBehavior(byId('addKrModal'));
        }
        getModal('addKrModal')?.show();
    }

    function openAllocateModal(okrId) {
        closeAllOkrDropdowns();
        if (!byId('allocateOkrModal')) return;
        document.querySelectorAll('#allocateOkrModal form').forEach(resetForm);
        setValue('allocOkrId', okrId);
        setValue('allocDeptOkrId', okrId);
        getModal('allocateOkrModal')?.show();
    }

    function openEditKrModal(id, name, target, current, unit, isInverse) {
        if (!byId('editKrModal')) return;
        const form = byId('editKrModal')?.querySelector('form');
        resetForm(form);
        setValue('editKrId', id);
        setValue('editKrName', name || '');
        setValue('editKrTarget', normalizeDecimalValue(target));
        setValue('editKrCurrent', normalizeDecimalValue(current));
        const editKrUnitSelect = byId('editKrUnit');
        if (window.setMeasurementUnitSelectValue) {
            window.setMeasurementUnitSelectValue(editKrUnitSelect, unit || '');
        } else if (editKrUnitSelect) {
            editKrUnitSelect.value = unit || '';
        }
        const inverseEl = byId('editKrIsInverse');
        if (inverseEl) {
            inverseEl.checked = isInverse === true || isInverse === 'true';
        }
        if (window.applyMeasurementUnitBehavior) {
            window.applyMeasurementUnitBehavior(byId('editKrModal'));
        }
        getModal('editKrModal')?.show();
    }

    function openUpdateProgressModal(krId, krName, currentVal, unit) {
        if (!byId('updateKrProgressModal')) return;
        const form = byId('updateKrProgressModal')?.querySelector('form');
        resetForm(form);
        setValue('updateKrId', krId);
        setText('updateKrNameDisplay', 'KR: ' + (krName || ''));
        setValue('updateKrCurrentValue', normalizeDecimalValue(currentVal));
        const config = window.applyMeasurementUnitConfigToInputs
            ? window.applyMeasurementUnitConfigToInputs(unit, [byId('updateKrCurrentValue')])
            : { suffix: unit || '' };
        setText('updateKrUnitDisplay', config.suffix || unit || '');
        getModal('updateKrProgressModal')?.show();
    }

    function showAiSuggestError(message) {
        const loading = byId('aiLoadingIndicator');
        const content = byId('aiSuggestionContent');
        if (content) content.style.display = 'none';
        if (loading) {
            loading.style.display = 'block';
            loading.innerHTML = `<div class="okr-empty" role="alert"><div class="okr-empty__icon"><i class="bi bi-exclamation-triangle"></i></div><h3>Không tải được gợi ý AI</h3><p>${message}</p><button type="button" class="btn btn-outline-primary js-okr-action" data-action="ai-suggest-retry">Thử lại</button></div>`;
        }
    }

    function resetAiSuggestLoading() {
        const loading = byId('aiLoadingIndicator');
        if (!loading) return;
        loading.style.display = 'block';
        loading.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-grow text-info" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-3 text-muted">AI đang phân tích và tạo Kết quả then chốt...</p>
            </div>`;
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function renderAiKrResults(data) {
        const items = Array.isArray(data) ? data : (data?.items || data?.Items || []);
        if (!Array.isArray(items) || items.length === 0) {
            showAiSuggestError('AI không trả về Key Result hợp lệ. Vui lòng thử lại.');
            return;
        }

        byId('aiLoadingIndicator').style.display = 'none';
        byId('aiSuggestionContent').style.display = 'block';

        const tbody = byId('aiKrListTableBody');
        let html = '';
        items.forEach((kr, index) => {
            const name = escapeHtml(kr.KeyResultName || kr.keyResultName || '');
            const target = kr.TargetValue ?? kr.targetValue ?? '';
            const unit = escapeHtml(kr.Unit || kr.unit || '');
            const isInverseChecked = kr.IsInverse || kr.isInverse ? 'checked' : '';
            html += `
                <tr>
                    <td><input class="form-check-input ai-kr-checkbox" type="checkbox" checked data-index="${index}" aria-label="Chọn KR ${index + 1}"></td>
                    <td><input type="text" class="form-control form-control-sm ai-kr-name" value="${name}" required aria-label="Tên KR ${index + 1}"></td>
                    <td><input type="number" step="0.01" class="form-control form-control-sm ai-kr-target" value="${escapeHtml(target)}" required aria-label="Chỉ tiêu KR ${index + 1}"></td>
                    <td><input type="text" class="form-control form-control-sm ai-kr-unit" value="${unit}" list="unitList" required aria-label="Đơn vị KR ${index + 1}"></td>
                    <td>
                        <div class="form-check form-switch">
                            <input class="form-check-input ai-kr-is-inverse" type="checkbox" ${isInverseChecked} aria-label="Chỉ tiêu thu nhỏ KR ${index + 1}">
                        </div>
                    </td>
                </tr>`;
        });

        if (tbody) {
            tbody.innerHTML = html;
        }
    }

    window.applyAiHistoryKR = function (parsedData) {
        renderAiKrResults(parsedData);
    };

    let aiSuggestAbort = null;
    let currentAiOkr = { id: null, name: '' };

    function openAiSuggestKrModal(okrId, okrName) {
        closeAllOkrDropdowns();
        if (!byId('aiSuggestKrModal')) return;
        currentAiOkr = { id: okrId, name: okrName || '' };
        setValue('aiOkrId', okrId);
        setText('aiOkrNameDisplay', 'Objective: ' + (okrName || ''));
        const content = byId('aiSuggestionContent');
        if (content) content.style.display = 'none';
        const tbody = byId('aiKrListTableBody');
        if (tbody) tbody.innerHTML = '';
        resetAiSuggestLoading();
        getModal('aiSuggestKrModal')?.show();
        loadAiSuggestions(okrId);
    }

    function loadAiSuggestions(okrId) {
        if (aiSuggestAbort) {
            aiSuggestAbort.abort();
        }
        aiSuggestAbort = new AbortController();
        resetAiSuggestLoading();
        byId('aiSuggestionContent').style.display = 'none';

        fetch(`/OKRs/SuggestKeyResultsAPI/${okrId}`, {
            method: 'POST',
            headers: window.antiForgeryHeaders ? window.antiForgeryHeaders() : {},
            signal: aiSuggestAbort.signal
        })
            .then(async response => {
                const payload = await response.json().catch(() => null);
                if (!response.ok) {
                    const message = payload?.message || payload?.Message || 'Không thể tải gợi ý từ AI';
                    throw new Error(message);
                }
                return payload;
            })
            .then(data => {
                if (data && data.success === false) {
                    throw new Error(data.message || 'Gợi ý AI không hợp lệ');
                }
                renderAiKrResults(data);
            })
            .catch(error => {
                if (error.name === 'AbortError') return;
                showAiSuggestError(error.message || 'Lỗi không xác định khi gọi AI');
            });
    }

    function hideOtherOkrDropdowns(currentToggleEl) {
        if (typeof bootstrap === 'undefined' || !bootstrap.Dropdown) return;
        document.querySelectorAll('.okr-index-page .okr-action-dropdown [data-bs-toggle="dropdown"]').forEach(toggleEl => {
            if (toggleEl !== currentToggleEl) {
                bootstrap.Dropdown.getOrCreateInstance(toggleEl).hide();
            }
        });
    }

    function getOpenOkrDropdown() {
        return Array.from(document.querySelectorAll('.okr-index-page .okr-action-dropdown'))
            .find(dropdownEl => dropdownEl.querySelector('.dropdown-menu.show'));
    }

    function syncOkrDropdownLayering(activeDropdownEl = null) {
        const openDropdownEl = activeDropdownEl || getOpenOkrDropdown();
        document.querySelectorAll('.okr-index-page #accordionOKRs .accordion-item').forEach(accordionItemEl => {
            accordionItemEl.classList.toggle(
                'okr-dropdown-open',
                openDropdownEl !== null && accordionItemEl.contains(openDropdownEl)
            );
        });
    }

    function wireDropdowns() {
        document.querySelectorAll('.okr-index-page .okr-action-dropdown').forEach(dropdownEl => {
            const toggleEl = dropdownEl.querySelector('[data-bs-toggle="dropdown"]');
            if (!toggleEl) return;
            toggleEl.addEventListener('click', () => hideOtherOkrDropdowns(toggleEl));
            dropdownEl.addEventListener('show.bs.dropdown', () => {
                hideOtherOkrDropdowns(toggleEl);
                syncOkrDropdownLayering(dropdownEl);
            });
            dropdownEl.addEventListener('shown.bs.dropdown', () => syncOkrDropdownLayering(dropdownEl));
            dropdownEl.addEventListener('hidden.bs.dropdown', () => syncOkrDropdownLayering());
        });
    }

    function handleOkrAction(button) {
        const action = button.dataset.action;
        const okrId = button.dataset.okrId;
        const okrName = button.dataset.okrName || '';
        const projectId = button.dataset.projectId || '';
        const projectName = button.dataset.projectName || '';

        switch (action) {
            case 'add-kr':
                openAddKrModal(okrId, okrName);
                break;
            case 'ai-suggest-kr':
                openAiSuggestKrModal(okrId, okrName);
                break;
            case 'ai-decompose':
                if (typeof window.openAiTaskDecomposeModal === 'function') {
                    window.openAiTaskDecomposeModal('OKR', okrId, okrName, projectId, projectName);
                }
                break;
            case 'allocate':
                openAllocateModal(okrId);
                break;
            case 'ai-history-kr':
                if (typeof window.openAiHistoryModal === 'function') {
                    window.openAiHistoryModal('SuggestKR', byId('aiOkrId')?.value, window.applyAiHistoryKR);
                }
                break;
            case 'ai-suggest-retry':
                if (currentAiOkr.id) {
                    loadAiSuggestions(currentAiOkr.id);
                }
                break;
            default:
                break;
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('form[data-submit-guard]').forEach(guardFormSubmit);

        document.addEventListener('click', function (event) {
            const stopEl = event.target.closest('[data-stop-propagation="true"], .js-stop-propagation');
            if (stopEl) {
                event.stopPropagation();
            }

            const actionBtn = event.target.closest('.js-okr-action');
            if (actionBtn) {
                event.preventDefault();
                event.stopPropagation();
                handleOkrAction(actionBtn);
                return;
            }

            const editBtn = event.target.closest('.js-edit-kr');
            if (editBtn) {
                event.preventDefault();
                event.stopPropagation();
                openEditKrModal(
                    editBtn.dataset.krId,
                    editBtn.dataset.krName || '',
                    editBtn.dataset.krTarget || '0',
                    editBtn.dataset.krCurrent || '0',
                    editBtn.dataset.krUnit || '',
                    editBtn.dataset.krIsInverse || 'false'
                );
                return;
            }

            const progressBtn = event.target.closest('.js-update-kr-progress');
            if (progressBtn) {
                event.preventDefault();
                event.stopPropagation();
                openUpdateProgressModal(
                    progressBtn.dataset.krId,
                    progressBtn.dataset.krName || '',
                    progressBtn.dataset.krCurrent || '0',
                    progressBtn.dataset.krUnit || ''
                );
            }
        });

        wireDropdowns();

        byId('toggleAllBtn')?.addEventListener('click', function () {
            if (typeof bootstrap === 'undefined' || !bootstrap.Collapse) return;
            const accordions = document.querySelectorAll('#accordionOKRs .accordion-collapse');
            const anyCollapsed = Array.from(accordions).some(a => !a.classList.contains('show'));
            accordions.forEach(acc => {
                const bsCollapse = bootstrap.Collapse.getOrCreateInstance(acc);
                if (anyCollapsed) bsCollapse.show();
                else bsCollapse.hide();
            });
            this.setAttribute('aria-label', anyCollapsed ? 'Thu gọn tất cả Key Results' : 'Mở rộng tất cả Key Results');
            this.innerHTML = anyCollapsed
                ? '<i class="bi bi-dash-square" aria-hidden="true"></i>'
                : '<i class="bi bi-list-task" aria-hidden="true"></i>';
        });

        byId('checkAllAiKr')?.addEventListener('change', function () {
            document.querySelectorAll('.ai-kr-checkbox').forEach(cb => { cb.checked = this.checked; });
        });

        // Reset AI modal state when closed.
        byId('aiSuggestKrModal')?.addEventListener('hidden.bs.modal', function () {
            if (aiSuggestAbort) aiSuggestAbort.abort();
            byId('aiKrListTableBody').innerHTML = '';
            byId('aiSuggestionContent').style.display = 'none';
            resetAiSuggestLoading();
            const saveBtn = byId('btnSaveAiKrs');
            if (saveBtn) setSubmitLoading(saveBtn, false);
        });

        byId('btnSaveAiKrs')?.addEventListener('click', function () {
            if (this.dataset.submitting === 'true') return;

            const okrId = byId('aiOkrId').value;
            const rows = document.querySelectorAll('#aiKrListTableBody tr');
            const payload = [];
            let invalid = false;

            rows.forEach(row => {
                const checkbox = row.querySelector('.ai-kr-checkbox');
                if (!checkbox?.checked) return;
                const name = row.querySelector('.ai-kr-name')?.value?.trim() || '';
                const target = parseFloat(row.querySelector('.ai-kr-target')?.value);
                const unit = row.querySelector('.ai-kr-unit')?.value?.trim() || '';
                if (!name || !unit || !(target > 0)) {
                    invalid = true;
                    return;
                }
                payload.push({
                    OKRId: parseInt(okrId, 10),
                    KeyResultName: name,
                    TargetValue: target,
                    Unit: unit,
                    IsInverse: !!row.querySelector('.ai-kr-is-inverse')?.checked,
                    CurrentValue: 0
                });
            });

            if (invalid) {
                window.AppFeedback.toast({ tone: 'warning', title: 'Key Result chưa hợp lệ', message: 'Mỗi KR được chọn cần có tên, đơn vị và chỉ tiêu lớn hơn 0.' });
                return;
            }
            if (payload.length === 0) {
                window.AppFeedback.toast({ tone: 'warning', title: 'Chưa chọn Key Result', message: 'Vui lòng chọn ít nhất một Key Result hợp lệ.' });
                return;
            }

            const btn = this;
            setSubmitLoading(btn, true, '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang lưu...');

            fetch('/OKRs/AddMultipleKeyResults', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...(window.antiForgeryHeaders ? window.antiForgeryHeaders() : {})
                },
                body: JSON.stringify(payload)
            })
                .then(async response => {
                    const data = await response.json().catch(() => ({}));
                    if (!response.ok) {
                        throw new Error(data.message || data.title || 'Lỗi khi lưu KR');
                    }
                    return data;
                })
                .then(data => {
                    if (data.success) {
                        window.location.reload();
                    } else {
                        throw new Error(data.message || 'Có lỗi xảy ra.');
                    }
                })
                .catch(error => {
                    window.AppFeedback.toast({ tone: 'error', title: 'Không lưu được Key Result', message: error.message || 'Vui lòng thử lại sau.' });
                    setSubmitLoading(btn, false);
                });
        });

        // Expose for any remaining callers.
        window.openAddKrModal = openAddKrModal;
        window.openAllocateModal = openAllocateModal;
        window.openAiSuggestKrModal = openAiSuggestKrModal;
        window.openEditKrModal = openEditKrModal;
        window.openUpdateProgressModal = openUpdateProgressModal;
    });
})();
