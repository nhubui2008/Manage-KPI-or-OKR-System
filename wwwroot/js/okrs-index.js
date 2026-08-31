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
        resetKrAiPanel();
        setValue('updateKrId', krId);
        setText('updateKrNameDisplay', 'KR: ' + (krName || ''));
        setValue('updateKrCurrentValue', normalizeDecimalValue(currentVal));
        const config = window.applyMeasurementUnitConfigToInputs
            ? window.applyMeasurementUnitConfigToInputs(unit, [byId('updateKrCurrentValue')])
            : { suffix: unit || '' };
        setText('updateKrUnitDisplay', config.suffix || unit || '');
        getModal('updateKrProgressModal')?.show();
    }

    function resetKrAiPanel() {
        const panel = byId('updateKrAiPanel');
        if (!panel) return;
        panel.replaceChildren();
        panel.className = 'alert alert-light border small mt-3 mb-0 d-none';
        delete panel.dataset.candidateValue;
    }

    function appendKrAiLine(parent, text, className) {
        const line = document.createElement('div');
        if (className) line.className = className;
        line.textContent = text;
        parent.appendChild(line);
        return line;
    }

    function renderKrAiProposal(payload) {
        const panel = byId('updateKrAiPanel');
        if (!panel) return;
        const proposal = payload.proposal || {};
        const confidence = proposal.confidence || {};
        const citations = Array.isArray(proposal.citations) ? proposal.citations : [];
        const shouldAbstain = confidence.shouldAbstain === true ||
            proposal.proposedStatus === 'InsufficientEvidence';
        const confidenceBandLabels = {
            0: 'Từ chối kết luận',
            1: 'Thấp',
            2: 'Vừa',
            3: 'Cao',
            Abstain: 'Từ chối kết luận',
            Low: 'Thấp',
            Moderate: 'Vừa',
            High: 'Cao'
        };
        const confidenceBand = shouldAbstain
            ? 'Từ chối kết luận'
            : confidenceBandLabels[String(confidence.band ?? '')] || 'Chưa phân loại';
        const status = shouldAbstain
            ? 'AI từ chối kết luận'
            : proposal.proposedStatus || 'Chưa phân loại';
        const score = (Number(confidence.score || 0) * 100).toFixed(0);

        panel.replaceChildren();
        panel.className = `alert alert-${shouldAbstain ? 'warning' : 'info'} border small mt-3 mb-0`;
        panel.dataset.candidateValue = String(payload.proposedCurrentValue ?? '');
        appendKrAiLine(
            panel,
            `${status} · tiến độ quy tắc ${Number(proposal.proposedProgressPercent || 0).toFixed(2)}% · độ tin cậy nguồn ${score}% (${confidenceBand})`,
            'fw-semibold');
        appendKrAiLine(
            panel,
            proposal.rationale || 'Không có diễn giải bổ sung.',
            'mt-1');

        const sourceTitle = document.createElement('div');
        sourceTitle.className = 'fw-semibold mt-2';
        sourceTitle.textContent = 'Nguồn kiểm chứng';
        panel.appendChild(sourceTitle);
        if (citations.length === 0) {
            appendKrAiLine(panel, 'Không có nguồn độc lập; không nên dựa vào đề xuất này.', 'text-muted');
        } else {
            const list = document.createElement('ul');
            list.className = 'mb-1 ps-3';
            citations.forEach(source => {
                const item = document.createElement('li');
                const location = [
                    source.versionId ? `bản ${source.versionId}` : '',
                    source.page ? `trang ${source.page}` : '',
                    source.section ? `mục ${source.section}` : ''
                ].filter(Boolean).join(', ');
                const freshness = source.isCurrent === true
                    ? ''
                    : ' [nguồn cũ/không rõ ngày]';
                item.textContent = `${source.title || source.sourceType} #${source.sourceId}${location ? ` (${location})` : ''}${freshness}`;
                list.appendChild(item);
            });
            panel.appendChild(list);
        }
        appendKrAiLine(
            panel,
            'Đây là đề xuất provisional; AI chưa thay đổi CurrentValue hoặc ResultStatus.',
            'text-muted mt-1');

        if (payload.proposalId && payload.proposalLifecycleStatus === 'AwaitingHumanReview') {
            const actions = document.createElement('div');
            actions.className = 'd-flex flex-wrap gap-2 mt-2';
            [
                { decision: 'Accepted', label: 'Ghi nhận đồng ý', className: 'btn btn-sm btn-outline-success' },
                { decision: 'Rejected', label: 'Ghi nhận không đồng ý', className: 'btn btn-sm btn-outline-secondary' }
            ].forEach(config => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = config.className;
                button.textContent = config.label;
                button.addEventListener('click', () => decideKrAiProposal(
                    payload.proposalId,
                    config.decision,
                    actions,
                    panel));
                actions.appendChild(button);
            });
            panel.appendChild(actions);
        }
    }

    async function decideKrAiProposal(proposalId, decision, actions, panel) {
        actions.querySelectorAll('button').forEach(button => { button.disabled = true; });
        try {
            const response = await fetch('/AI/DecideOkrKeyResultProposal', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...(window.antiForgeryHeaders ? window.antiForgeryHeaders() : {})
                },
                body: JSON.stringify({ proposalId, decision })
            });
            const payload = await response.json().catch(() => ({}));
            if (!response.ok) {
                throw new Error(payload?.warnings?.[0] || 'Không thể ghi nhận quyết định AI.');
            }
            actions.remove();
            appendKrAiLine(
                panel,
                decision === 'Accepted'
                    ? 'Đã ghi nhận bạn đồng ý với đề xuất. Giá trị chính thức vẫn chỉ đổi khi bạn bấm “Con người xác nhận cập nhật”.'
                    : 'Đã ghi nhận bạn không đồng ý. Hãy chỉnh giá trị hoặc đóng biểu mẫu; AI chưa thay đổi dữ liệu chính thức.',
                `fw-semibold mt-2 ${decision === 'Accepted' ? 'text-success' : 'text-secondary'}`);
        } catch (error) {
            actions.querySelectorAll('button').forEach(button => { button.disabled = false; });
            appendKrAiLine(
                panel,
                error.message || 'Không thể ghi nhận quyết định AI.',
                'text-danger mt-2');
        }
    }

    async function evaluateKrWithAi() {
        const button = byId('updateKrAiEvaluateBtn');
        const panel = byId('updateKrAiPanel');
        const keyResultId = Number(byId('updateKrId')?.value);
        const proposedCurrentValue = Number(normalizeDecimalValue(byId('updateKrCurrentValue')?.value));
        if (!button || !panel || !keyResultId || !Number.isFinite(proposedCurrentValue) || proposedCurrentValue < 0) {
            byId('updateKrCurrentValue')?.focus();
            return;
        }

        button.disabled = true;
        const originalHtml = button.innerHTML;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang đánh giá...';
        panel.className = 'alert alert-info border small mt-3 mb-0';
        panel.textContent = 'Agent đang kiểm tra KR, nguồn được phép truy cập và độ tin cậy…';
        try {
            const response = await fetch('/AI/EvaluateOkrKeyResultProposal', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...(window.antiForgeryHeaders ? window.antiForgeryHeaders() : {})
                },
                body: JSON.stringify({
                    keyResultId,
                    proposedCurrentValue,
                    historyOperationId: window.crypto?.randomUUID ? window.crypto.randomUUID() : null
                })
            });
            const payload = await response.json().catch(() => ({}));
            if (!response.ok) {
                throw new Error(payload?.warnings?.[0] || 'Không thể đánh giá tiến độ KR.');
            }
            const liveKeyResultId = Number(byId('updateKrId')?.value);
            const liveProposedValue = Number(
                normalizeDecimalValue(byId('updateKrCurrentValue')?.value));
            if (liveKeyResultId !== keyResultId ||
                liveProposedValue !== proposedCurrentValue) {
                panel.className = 'alert alert-warning border small mt-3 mb-0';
                panel.textContent = 'Giá trị KR đã thay đổi trong lúc agent đánh giá. Kết quả cũ đã bị bỏ; hãy chạy lại AI với giá trị hiện tại.';
                return;
            }
            renderKrAiProposal(payload);
        } catch (error) {
            panel.className = 'alert alert-danger border small mt-3 mb-0';
            panel.textContent = error.message || 'Không thể đánh giá tiến độ KR.';
        } finally {
            button.disabled = false;
            button.innerHTML = originalHtml;
        }
    }

    let aiLoadingTimer = null;

    function showAiSuggestError(message, title = 'Không tải được gợi ý AI') {
        if (aiLoadingTimer) { clearInterval(aiLoadingTimer); aiLoadingTimer = null; }
        const loading = byId('aiLoadingIndicator');
        const content = byId('aiSuggestionContent');
        if (content) content.style.display = 'none';
        if (loading) {
            loading.style.display = 'block';
            loading.innerHTML = `<div class="okr-empty" role="alert"><div class="okr-empty__icon"><i class="bi bi-exclamation-triangle"></i></div><h3>${escapeHtml(title)}</h3><p>${escapeHtml(message)}</p><button type="button" class="btn btn-outline-primary js-okr-action" data-action="ai-suggest-retry">Thử lại</button></div>`;
        }
    }

    function resetAiSuggestLoading() {
        if (aiLoadingTimer) { clearInterval(aiLoadingTimer); aiLoadingTimer = null; }
        const loading = byId('aiLoadingIndicator');
        if (!loading) return;
        loading.style.display = 'block';
        loading.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-info mb-3" role="status" style="width: 2.5rem; height: 2.5rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <h6 id="aiLoadingStepText" class="fw-bold text-dark mb-1">AI đang phân tích Objective...</h6>
                <p class="small text-muted mb-0">Hệ thống đang trích xuất chỉ tiêu đo lường và tạo gợi ý Kết quả then chốt</p>
                <div class="progress progress-sm mt-3 mx-auto" style="max-width: 260px; height: 4px;">
                    <div class="progress-bar progress-bar-striped progress-bar-animated bg-info" role="progressbar" style="width: 100%"></div>
                </div>
            </div>`;
        const steps = [
            'AI đang phân tích Objective...',
            'Đang đối chiếu dữ liệu và xây dựng chỉ tiêu...',
            'Đang hoàn tất danh sách Key Results gợi ý...'
        ];
        let stepIdx = 0;
        aiLoadingTimer = setInterval(() => {
            stepIdx = (stepIdx + 1) % steps.length;
            const textEl = byId('aiLoadingStepText');
            if (textEl) textEl.textContent = steps[stepIdx];
        }, 1800);
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
        if (aiLoadingTimer) { clearInterval(aiLoadingTimer); aiLoadingTimer = null; }
        currentAiHistorySessionId = data?.historySessionId || data?.HistorySessionId || currentAiHistorySessionId;
        const items = Array.isArray(data) ? data : (data?.items || data?.Items || []);
        if (!Array.isArray(items) || items.length === 0) {
            const warnings = data?.warnings || data?.Warnings || [];
            showAiSuggestError(
                warnings[0] || 'Agent chưa có đủ bằng chứng để tạo Key Result phù hợp.',
                'Agent chưa tạo bản nháp');
            return;
        }

        byId('aiLoadingIndicator').style.display = 'none';
        byId('aiSuggestionContent').style.display = 'block';

        const tbody = byId('aiKrListTableBody');
        const allowedUnits = Array.from(document.querySelectorAll('#unitList option'))
            .map(option => option.value?.trim())
            .filter(Boolean);
        let html = '';
        items.forEach((kr, index) => {
            const name = escapeHtml(kr.KeyResultName || kr.keyResultName || '');
            const target = kr.TargetValue ?? kr.targetValue ?? '';
            const unit = escapeHtml(kr.Unit || kr.unit || '');
            const unitOptions = allowedUnits.map(value => {
                const safeValue = escapeHtml(value);
                return `<option value="${safeValue}" ${value === (kr.Unit || kr.unit || '') ? 'selected' : ''}>${safeValue}</option>`;
            }).join('');
            const rationale = escapeHtml(kr.Rationale || kr.rationale || '');
            const isInverseChecked = kr.IsInverse || kr.isInverse ? 'checked' : '';
            html += `
                <tr>
                    <td class="text-center"><input class="form-check-input ai-kr-checkbox mt-2" type="checkbox" checked data-index="${index}" aria-label="Chọn KR ${index + 1}"></td>
                    <td>
                        <input type="text" class="form-control form-control-sm ai-kr-name" value="${name}" required aria-label="Tên KR ${index + 1}">
                        ${rationale ? `<div class="small text-muted mt-1 ai-kr-rationale">${rationale}</div>` : ''}
                    </td>
                    <td><input type="number" step="0.01" class="form-control form-control-sm ai-kr-target" value="${escapeHtml(target)}" required aria-label="Chỉ tiêu KR ${index + 1}"></td>
                    <td><select class="form-select form-select-sm ai-kr-unit" required aria-label="Đơn vị KR ${index + 1}">${unitOptions || `<option value="${unit}" selected>${unit}</option>`}</select></td>
                    <td class="text-center">
                        <div class="form-check form-switch d-inline-block mt-2">
                            <input class="form-check-input ai-kr-is-inverse" type="checkbox" ${isInverseChecked} aria-label="Chỉ tiêu thu nhỏ KR ${index + 1}">
                        </div>
                    </td>
                </tr>`;
        });

        if (tbody) {
            tbody.innerHTML = html;
        }
        renderAiKrCitations(data?.citations || data?.Citations || []);
        setText('aiKrRefineStatus', 'Bạn có thể yêu cầu agent chỉnh sửa toàn bộ danh sách này.');
    }

    function renderAiKrCitations(citations) {
        const container = byId('aiKrCitationList');
        if (!container) return;
        container.replaceChildren();
        if (!Array.isArray(citations) || citations.length === 0) {
            container.textContent = 'Chưa có nguồn được trích dẫn.';
            return;
        }
        citations.forEach(citation => {
            const item = document.createElement('span');
            item.className = 'badge text-bg-light border me-2 mb-1';
            const sourceType = citation.sourceType || citation.SourceType || 'source';
            const sourceId = citation.sourceId || citation.SourceId || '';
            const title = citation.title || citation.Title || `${sourceType} #${sourceId}`;
            item.textContent = title;
            container.appendChild(item);
        });
    }

    function collectAiKrSuggestions() {
        return Array.from(document.querySelectorAll('#aiKrListTableBody tr')).map(row => ({
            keyResultName: row.querySelector('.ai-kr-name')?.value?.trim() || '',
            targetValue: Number(row.querySelector('.ai-kr-target')?.value),
            unit: row.querySelector('.ai-kr-unit')?.value?.trim() || '',
            isInverse: !!row.querySelector('.ai-kr-is-inverse')?.checked
        }));
    }

    let aiSuggestAbort = null;
    let currentAiOkr = { id: null, name: '' };
    let currentAiHistorySessionId = null;

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

        const historyOperationId = window.crypto?.randomUUID ? window.crypto.randomUUID() : '';
        fetch(`/OKRs/SuggestKeyResultsAPI/${okrId}?historyOperationId=${encodeURIComponent(historyOperationId)}`, {
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
            case 'ai-decompose-kr':
                if (typeof window.openAiTaskDecomposeModal === 'function') {
                    window.openAiTaskDecomposeModal(
                        'KR',
                        button.dataset.krId,
                        button.dataset.krName || '',
                        projectId,
                        projectName);
                }
                break;
            case 'allocate':
                openAllocateModal(okrId);
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

        byId('updateKrAiEvaluateBtn')?.addEventListener('click', evaluateKrWithAi);
        byId('updateKrCurrentValue')?.addEventListener('input', function () {
            const panel = byId('updateKrAiPanel');
            if (panel?.dataset.candidateValue !== undefined &&
                normalizeDecimalValue(this.value) !== normalizeDecimalValue(panel.dataset.candidateValue)) {
                resetKrAiPanel();
            }
        });
        byId('updateKrProgressModal')?.addEventListener('hidden.bs.modal', resetKrAiPanel);

        // Reset AI modal state when closed.
        byId('aiSuggestKrModal')?.addEventListener('hidden.bs.modal', function () {
            if (aiSuggestAbort) aiSuggestAbort.abort();
            byId('aiKrListTableBody').innerHTML = '';
            byId('aiSuggestionContent').style.display = 'none';
            resetAiSuggestLoading();
            const saveBtn = byId('btnSaveAiKrs');
            if (saveBtn) setSubmitLoading(saveBtn, false);
            setValue('aiKrRefineInput', '');
            setText('aiKrRefineStatus', '');
            currentAiHistorySessionId = null;
            byId('aiKrCitationList')?.replaceChildren();
        });

        byId('btnRefineAiKrs')?.addEventListener('click', async function () {
            const input = byId('aiKrRefineInput');
            const instruction = input?.value?.trim() || '';
            if (!instruction) {
                setText('aiKrRefineStatus', 'Hãy nhập nội dung cần chỉnh sửa.');
                input?.focus();
                return;
            }

            const button = this;
            const original = button.innerHTML;
            button.disabled = true;
            if (input) input.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang sửa...';
            setText('aiKrRefineStatus', 'Agent đang chỉnh sửa gợi ý KR...');
            try {
                const response = await fetch(`/OKRs/RefineKeyResultSuggestions/${byId('aiOkrId').value}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', ...(window.antiForgeryHeaders ? window.antiForgeryHeaders() : {}) },
                    body: JSON.stringify({
                        instruction,
                        items: collectAiKrSuggestions(),
                        historySessionId: currentAiHistorySessionId,
                        historyOperationId: window.crypto?.randomUUID ? window.crypto.randomUUID() : null
                    })
                });
                const data = await response.json().catch(() => ({}));
                if (!response.ok || data.success === false) throw new Error(data.message || 'Không thể chỉnh sửa gợi ý KR.');
                renderAiKrResults(data);
                if (input) input.value = '';
                setText('aiKrRefineStatus', 'Đã cập nhật gợi ý theo yêu cầu. Bạn có thể tiếp tục yêu cầu chỉnh sửa.');
            } catch (error) {
                setText('aiKrRefineStatus', error.message || 'Không thể chỉnh sửa gợi ý KR.');
            } finally {
                button.disabled = false;
                if (input) input.disabled = false;
                button.innerHTML = original;
                input?.focus();
            }
        });
        byId('aiKrRefineInput')?.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' && !event.isComposing) {
                event.preventDefault();
                byId('btnRefineAiKrs')?.click();
            }
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
