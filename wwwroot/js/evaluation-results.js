/**
 * Evaluation Results Module JavaScript
 * Handles edit modal population, score calculation, rank resolution, and AI review drafts.
 */

(function () {
    'use strict';

    let evaluationRankScale = [];
    let activeEvaluationReviewDraft = null;
    let evaluationReviewSourceSnapshot = null;

    function getRankScale() {
        if (evaluationRankScale.length > 0) return evaluationRankScale;
        const configElement = document.getElementById('evaluationRankScaleConfig');
        if (configElement && configElement.textContent) {
            try {
                evaluationRankScale = JSON.parse(configElement.textContent);
            } catch (e) {
                console.error('Failed to parse evaluation rank scale config', e);
            }
        }
        return evaluationRankScale;
    }

    function captureEvaluationReviewSource() {
        const scoreInput = document.getElementById('editTotalScore');
        const score = Number.parseFloat(normalizeDecimalValue(scoreInput?.value || '0'));
        return {
            resultId: document.getElementById('editId')?.value || '',
            employeeId: document.getElementById('editEmployeeId')?.value || '',
            periodId: document.getElementById('editPeriodId')?.value || '',
            score: Number.isNaN(score) ? null : score
        };
    }

    function evaluationReviewSourceMatches(snapshot) {
        if (!snapshot) return false;
        const current = captureEvaluationReviewSource();
        return current.resultId === snapshot.resultId &&
            current.employeeId === snapshot.employeeId &&
            current.periodId === snapshot.periodId &&
            current.score === snapshot.score;
    }

    function syncEvaluationReviewDraftEligibility() {
        if (!activeEvaluationReviewDraft) return;
        const sourceMatches = evaluationReviewSourceMatches(activeEvaluationReviewDraft.sourceSnapshot);
        const warning = document.getElementById('aiReviewDraftSourceWarning');
        if (warning) warning.classList.toggle('d-none', sourceMatches);

        const applyButton = document.getElementById('aiApplyReviewDraftBtn');
        if (applyButton) {
            applyButton.disabled = !activeEvaluationReviewDraft.hasCitations || !sourceMatches;
        }
    }

    function resetEvaluationReviewDraft() {
        activeEvaluationReviewDraft = null;
        evaluationReviewSourceSnapshot = null;
        const panel = document.getElementById('aiReviewDraftPanel');
        if (panel) panel.classList.add('d-none');
        const warning = document.getElementById('aiReviewDraftSourceWarning');
        if (warning) warning.classList.add('d-none');

        const text = document.getElementById('aiReviewDraftText');
        if (text) text.textContent = '';

        const citations = document.getElementById('aiReviewDraftCitations');
        if (citations) citations.replaceChildren();
    }

    function showEvaluationReviewDraft(data) {
        if (!evaluationReviewSourceSnapshot ||
            String(data.evaluationResultId || '') !== evaluationReviewSourceSnapshot.resultId) {
            if (window.AppFeedback?.toast) {
                window.AppFeedback.toast({
                    tone: 'warning',
                    eyebrow: 'AI Nhận xét',
                    title: 'Bản nháp không còn khớp',
                    message: 'Kết quả đang mở đã thay đổi. Hãy tạo lại bản nháp AI.'
                });
            } else {
                alert('Bản nháp không còn khớp. Kết quả đang mở đã thay đổi.');
            }
            return;
        }

        activeEvaluationReviewDraft = {
            draftActionId: data.draftActionId,
            rowVersion: data.rowVersion,
            text: data.text || '',
            hasCitations: Array.isArray(data.citations) && data.citations.length > 0,
            sourceSnapshot: { ...evaluationReviewSourceSnapshot }
        };

        const text = document.getElementById('aiReviewDraftText');
        if (text) text.textContent = activeEvaluationReviewDraft.text;

        const citations = document.getElementById('aiReviewDraftCitations');
        if (citations) {
            citations.replaceChildren();
            (data.citations || []).forEach(citation => {
                const item = document.createElement('li');
                const label = citation.title || `${citation.sourceType || 'Nguồn'} #${citation.sourceId || ''}`;
                item.textContent = citation.versionId ? `${label} · phiên bản ${citation.versionId}` : label;
                citations.appendChild(item);
            });
            if (!citations.children.length) {
                const item = document.createElement('li');
                item.textContent = 'Chưa có citation hợp lệ; không thể áp dụng bản nháp.';
                citations.appendChild(item);
            }
        }

        const panel = document.getElementById('aiReviewDraftPanel');
        if (panel) panel.classList.remove('d-none');

        syncEvaluationReviewDraftEligibility();
    }

    function normalizeDecimalValue(value) {
        return String(value ?? '').replace(',', '.');
    }

    function resolveRank(score) {
        const numericScore = Number.parseFloat(normalizeDecimalValue(score));
        if (Number.isNaN(numericScore)) return null;
        const scale = getRankScale();
        return scale.find(rank => numericScore >= Number(rank.minScore));
    }

    function updateRankOutputs(scoreInput) {
        if (!scoreInput) return;
        const rankOutput = document.getElementById(scoreInput.dataset.rankOutput);
        const classificationOutput = document.getElementById(scoreInput.dataset.classificationOutput);
        const rank = resolveRank(scoreInput.value);

        if (rank) {
            if (rankOutput) rankOutput.value = rank.rankCode;
            if (classificationOutput) classificationOutput.value = rank.description;
            return;
        }

        if (rankOutput) rankOutput.value = 'Chưa có hạng phù hợp';
        if (classificationOutput) classificationOutput.value = 'Chưa phân loại';
    }

    function editResult(id, empId, periodId, score, rankId, classification, reviewComment) {
        resetEvaluationReviewDraft();
        const normalizedScore = normalizeDecimalValue(score || '0');

        const editId = document.getElementById('editId');
        if (editId) editId.value = id;

        const editEmployeeId = document.getElementById('editEmployeeId');
        if (editEmployeeId) editEmployeeId.value = empId;

        const editPeriodId = document.getElementById('editPeriodId');
        if (editPeriodId) editPeriodId.value = periodId;

        const editTotalScore = document.getElementById('editTotalScore');
        if (editTotalScore) {
            editTotalScore.value = normalizedScore;
            updateRankOutputs(editTotalScore);
        }

        const editReviewComment = document.getElementById('editReviewComment');
        if (editReviewComment) editReviewComment.value = reviewComment || '';

        evaluationReviewSourceSnapshot = captureEvaluationReviewSource();

        const editModalScoreHeader = document.getElementById('editModalScoreHeader');
        if (editModalScoreHeader) {
            editModalScoreHeader.innerText = Number.parseFloat(normalizedScore || '0').toLocaleString('vi-VN');
        }

        const editModalEl = document.getElementById('editModal');
        if (editModalEl && window.bootstrap?.Modal) {
            const modalInstance = window.bootstrap.Modal.getInstance(editModalEl) || new window.bootstrap.Modal(editModalEl);
            modalInstance.show();
        }
    }

    function initEvaluationResults() {
        const page = document.querySelector('.evaluation-results-page');
        if (!page) return;

        getRankScale();

        document.querySelectorAll('.js-edit-result').forEach(button => {
            if (button.dataset.initialized) return;
            button.dataset.initialized = 'true';
            button.addEventListener('click', function () {
                editResult(
                    this.dataset.id,
                    this.dataset.employeeId,
                    this.dataset.periodId,
                    this.dataset.score,
                    this.dataset.rankId,
                    this.dataset.classification,
                    this.dataset.reviewComment
                );
            });
        });

        document.querySelectorAll('.js-score-input').forEach(input => {
            if (input.dataset.initialized) return;
            input.dataset.initialized = 'true';
            input.addEventListener('input', function () {
                updateRankOutputs(this);
                if (this.id === 'editTotalScore') {
                    const numericScore = Number.parseFloat(normalizeDecimalValue(this.value || '0'));
                    const editModalScoreHeader = document.getElementById('editModalScoreHeader');
                    if (editModalScoreHeader) {
                        editModalScoreHeader.innerText = Number.isNaN(numericScore)
                            ? '0'
                            : numericScore.toLocaleString('vi-VN');
                    }
                }
                syncEvaluationReviewDraftEligibility();
            });
            updateRankOutputs(input);
        });

        ['editEmployeeId', 'editPeriodId'].forEach(id => {
            const el = document.getElementById(id);
            if (el && !el.dataset.initialized) {
                el.dataset.initialized = 'true';
                el.addEventListener('change', syncEvaluationReviewDraftEligibility);
            }
        });

        // AI Generate Review Button
        const aiBtn = document.getElementById('aiGenerateReviewBtn');
        if (aiBtn && !aiBtn.dataset.initialized) {
            aiBtn.dataset.initialized = 'true';
            aiBtn.addEventListener('click', async function () {
                const editIdEl = document.getElementById('editId');
                const resultId = parseInt(editIdEl?.value || '0', 10);
                if (!resultId) return;

                if (!evaluationReviewSourceMatches(evaluationReviewSourceSnapshot)) {
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'warning',
                            eyebrow: 'AI Nhận xét',
                            title: 'Dữ liệu chưa được lưu',
                            message: 'Hãy lưu nhân viên, kỳ và tổng điểm trước khi tạo bản nháp AI.'
                        });
                    }
                    return;
                }

                const btn = this;
                const originalHtml = btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> AI đang viết...';

                try {
                    const headers = {
                        'Content-Type': 'application/json'
                    };
                    const antiForgeryToken = window.getAntiForgeryToken?.() || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                    if (antiForgeryToken) {
                        headers['RequestVerificationToken'] = antiForgeryToken;
                        headers['X-CSRF-TOKEN'] = antiForgeryToken;
                    }

                    const response = await fetch('/AI/GenerateReview', {
                        method: 'POST',
                        headers,
                        body: JSON.stringify({ evaluationResultId: resultId })
                    });

                    const data = await response.json();
                    if (!response.ok || data.success === false) {
                        const message = data.warnings?.[0] || 'Không thể sinh nhận xét AI.';
                        if (window.AppFeedback?.toast) {
                            window.AppFeedback.toast({ tone: 'warning', eyebrow: 'AI Nhận xét', title: 'AI chưa sẵn sàng', message });
                        }
                        return;
                    }
                    showEvaluationReviewDraft(data);
                } catch (error) {
                    console.error('AI Generate review error:', error);
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({ tone: 'error', eyebrow: 'Kết nối AI', title: 'Lỗi kết nối', message: 'Không thể kết nối AI. Vui lòng thử lại sau.' });
                    }
                } finally {
                    btn.disabled = false;
                    btn.innerHTML = originalHtml;
                }
            });
        }

        async function decideEvaluationReviewDraft(decision) {
            if (!activeEvaluationReviewDraft) return;
            if (decision === 'Accepted' &&
                !evaluationReviewSourceMatches(activeEvaluationReviewDraft.sourceSnapshot)) {
                syncEvaluationReviewDraftEligibility();
                if (window.AppFeedback?.toast) {
                    window.AppFeedback.toast({
                        tone: 'warning',
                        eyebrow: 'AI Nhận xét',
                        title: 'Nguồn đã thay đổi',
                        message: 'Hãy lưu kết quả trước rồi tạo lại bản nháp AI.'
                    });
                }
                return;
            }

            if (decision === 'Accepted') {
                const currentText = document.getElementById('editReviewComment')?.value || '';
                if (currentText.trim() && currentText !== activeEvaluationReviewDraft.text &&
                    !window.confirm('Ô nhận xét đang có nội dung. Bạn có muốn thay bằng bản nháp AI vừa duyệt không?')) {
                    return;
                }
            }

            const applyButton = document.getElementById('aiApplyReviewDraftBtn');
            const rejectButton = document.getElementById('aiRejectReviewDraftBtn');
            if (applyButton) applyButton.disabled = true;
            if (rejectButton) rejectButton.disabled = true;

            try {
                const headers = {
                    'Content-Type': 'application/json'
                };
                const antiForgeryToken = window.getAntiForgeryToken?.() || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                if (antiForgeryToken) {
                    headers['RequestVerificationToken'] = antiForgeryToken;
                    headers['X-CSRF-TOKEN'] = antiForgeryToken;
                }

                const response = await fetch('/AI/DecideEvaluationReviewDraft', {
                    method: 'POST',
                    headers,
                    body: JSON.stringify({
                        draftActionId: activeEvaluationReviewDraft.draftActionId,
                        rowVersion: activeEvaluationReviewDraft.rowVersion,
                        decision
                    })
                });

                const data = await response.json();
                if (!response.ok || data.success === false) {
                    const message = data.warnings?.[0] || 'Bản nháp AI không còn hợp lệ.';
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({ tone: 'warning', eyebrow: 'AI Nhận xét', title: 'Không thể quyết định bản nháp', message });
                    }
                    return;
                }

                if (decision === 'Accepted') {
                    const input = document.getElementById('editReviewComment');
                    const draftText = data.text || activeEvaluationReviewDraft.text;
                    if (input) input.value = draftText;
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({ tone: 'success', eyebrow: 'AI Nhận xét', title: 'Đã chèn vào bản nháp', message: 'Hãy chỉnh sửa nếu cần và bấm Cập nhật kết quả để lưu chính thức.' });
                    }
                }
                resetEvaluationReviewDraft();
            } catch (error) {
                console.error('AI Decide review draft error:', error);
                if (window.AppFeedback?.toast) {
                    window.AppFeedback.toast({ tone: 'error', eyebrow: 'Kết nối AI', title: 'Lỗi kết nối', message: 'Không thể cập nhật bản nháp AI. Vui lòng thử lại.' });
                }
            } finally {
                if (applyButton) applyButton.disabled = false;
                if (rejectButton) rejectButton.disabled = false;
            }
        }

        const applyBtn = document.getElementById('aiApplyReviewDraftBtn');
        if (applyBtn && !applyBtn.dataset.initialized) {
            applyBtn.dataset.initialized = 'true';
            applyBtn.addEventListener('click', () => decideEvaluationReviewDraft('Accepted'));
        }

        const rejectBtn = document.getElementById('aiRejectReviewDraftBtn');
        if (rejectBtn && !rejectBtn.dataset.initialized) {
            rejectBtn.dataset.initialized = 'true';
            rejectBtn.addEventListener('click', () => decideEvaluationReviewDraft('Rejected'));
        }

        const editModalEl = document.getElementById('editModal');
        if (editModalEl && !editModalEl.dataset.initialized) {
            editModalEl.dataset.initialized = 'true';
            editModalEl.addEventListener('hidden.bs.modal', resetEvaluationReviewDraft);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initEvaluationResults);
    } else {
        initEvaluationResults();
    }
})();
