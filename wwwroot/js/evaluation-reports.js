/**
 * Evaluation Reports Module JavaScript
 * Handles Director summary saving, incident additions, print syncing.
 */

(function () {
    'use strict';

    function initEvaluationReports() {
        const page = document.querySelector('.evaluation-report-page');
        if (!page) return;

        const directorSummaryInput = document.getElementById('directorSummaryText');
        const directorPrintSummary = document.querySelector('.print-director-summary');

        function syncDirectorSummaryForPrint() {
            if (!directorSummaryInput || !directorPrintSummary) return;
            const content = directorSummaryInput.value.trim();
            directorPrintSummary.textContent = content || 'Chưa có nhận xét.';
        }

        if (directorSummaryInput && !directorSummaryInput.dataset.printSynced) {
            directorSummaryInput.dataset.printSynced = 'true';
            directorSummaryInput.addEventListener('input', syncDirectorSummaryForPrint);
            window.addEventListener('beforeprint', syncDirectorSummaryForPrint);
            syncDirectorSummaryForPrint();
        }

        // Save Summary Button
        const btnSaveSummary = document.getElementById('btnSaveSummary');
        if (btnSaveSummary && !btnSaveSummary.dataset.initialized) {
            btnSaveSummary.dataset.initialized = 'true';
            btnSaveSummary.addEventListener('click', async function () {
                const btn = this;
                const textarea = document.getElementById('directorSummaryText');
                if (!textarea) return;

                const content = textarea.value;
                const departmentId = btn.dataset.departmentId || '';
                const cycle = btn.dataset.cycle || '';

                if (!content.trim()) {
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'warning',
                            eyebrow: 'Lưu nhận xét',
                            title: 'Thiếu nội dung',
                            message: 'Vui lòng nhập nội dung nhận xét trước khi lưu.'
                        });
                    } else {
                        alert('Vui lòng nhập nội dung nhận xét trước khi lưu.');
                    }
                    return;
                }

                btn.disabled = true;
                const originalHtml = btn.innerHTML;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Đang lưu...';

                try {
                    const formData = new URLSearchParams();
                    formData.append('departmentId', departmentId);
                    formData.append('cycle', cycle);
                    formData.append('content', content);

                    const antiForgeryToken = window.getAntiForgeryToken?.() || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                    if (antiForgeryToken) {
                        formData.append('__RequestVerificationToken', antiForgeryToken);
                    }

                    const response = await fetch('/EvaluationReports/SaveDirectorSummary', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        body: formData
                    });

                    const result = await response.json();

                    if (response.ok && result.success) {
                        if (window.AppFeedback?.toast) {
                            window.AppFeedback.toast({
                                tone: 'success',
                                eyebrow: 'Báo cáo Giám đốc',
                                title: 'Lưu nhận xét thành công',
                                message: result.message || 'Nhận xét của Giám đốc đã được lưu thành công!'
                            });
                        }
                    } else {
                        throw new Error(result.message || 'Có lỗi xảy ra từ máy chủ.');
                    }
                } catch (error) {
                    console.error('Error saving summary:', error);
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'error',
                            eyebrow: 'Báo cáo Giám đốc',
                            title: 'Lỗi khi lưu',
                            message: error?.message || 'Không thể kết nối đến máy chủ. Vui lòng thử lại.'
                        });
                    } else {
                        alert(error?.message || 'Không thể kết nối đến máy chủ.');
                    }
                } finally {
                    btn.disabled = false;
                    btn.innerHTML = originalHtml;
                }
            });
        }

        // Save Incident Button
        const btnSaveIncident = document.getElementById('btnSaveIncident');
        if (btnSaveIncident && !btnSaveIncident.dataset.initialized) {
            btnSaveIncident.dataset.initialized = 'true';
            btnSaveIncident.addEventListener('click', async function () {
                const btn = this;
                const severitySelect = document.getElementById('incidentSeverity');
                const contentInput = document.getElementById('incidentContent');
                const severity = severitySelect?.value || 'Warning';
                const content = contentInput?.value.trim() || '';

                const departmentId = btn.dataset.departmentId || '';
                const cycle = btn.dataset.cycle || '';

                if (!content) {
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'warning',
                            eyebrow: 'Sự cố vận hành',
                            title: 'Thiếu nội dung',
                            message: 'Vui lòng nhập nội dung sự cố.'
                        });
                    } else {
                        alert('Vui lòng nhập nội dung sự cố.');
                    }
                    return;
                }

                btn.disabled = true;
                const originalHtml = btn.innerHTML;
                btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Đang lưu...';

                try {
                    const formData = new URLSearchParams();
                    formData.append('departmentId', departmentId);
                    formData.append('cycle', cycle);
                    formData.append('severity', severity);
                    formData.append('content', content);

                    const antiForgeryToken = window.getAntiForgeryToken?.() || document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                    if (antiForgeryToken) {
                        formData.append('__RequestVerificationToken', antiForgeryToken);
                    }

                    const response = await fetch('/EvaluationReports/AddIncident', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: formData
                    });

                    const result = await response.json();
                    if (!response.ok || !result.success) {
                        throw new Error(result.message || 'Không thể lưu sự cố.');
                    }

                    appendIncident(result.incident);
                    if (contentInput) contentInput.value = '';

                    const modalElement = document.getElementById('incidentModal');
                    if (modalElement && window.bootstrap?.Modal) {
                        const modalInstance = window.bootstrap.Modal.getInstance(modalElement) || new window.bootstrap.Modal(modalElement);
                        modalInstance.hide();
                    }

                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'success',
                            eyebrow: 'Sự cố vận hành',
                            title: 'Đã lưu sự cố',
                            message: result.message || 'Thêm sự cố thành công.'
                        });
                    }
                } catch (error) {
                    console.error('Error saving incident:', error);
                    if (window.AppFeedback?.toast) {
                        window.AppFeedback.toast({
                            tone: 'error',
                            eyebrow: 'Sự cố vận hành',
                            title: 'Lỗi khi lưu',
                            message: error?.message || 'Không thể kết nối máy chủ.'
                        });
                    } else {
                        alert(error?.message || 'Không thể kết nối đến máy chủ.');
                    }
                } finally {
                    btn.disabled = false;
                    btn.innerHTML = originalHtml;
                }
            });
        }
    }

    function appendIncident(incident) {
        const list = document.getElementById('incidentList');
        if (!list || !incident) return;

        const emptyText = document.getElementById('emptyIncidentText');
        if (emptyText) emptyText.remove();

        const addButton = list.querySelector('[data-bs-target="#incidentModal"]');
        const li = document.createElement('li');
        const isCritical = incident.severity === 'Critical';
        const severityText = isCritical ? 'Nghiêm trọng' : 'Cảnh báo';
        const badgeClass = isCritical ? 'bg-danger' : 'bg-warning text-dark';
        const safeContent = window.escapeHtml ? window.escapeHtml(incident.content) : incident.content;

        li.innerHTML = `<span class="badge ${badgeClass} rounded-pill me-2">${severityText}</span> <div><span class="fw-semibold">${safeContent}</span> <span class="text-muted small d-block">${incident.createdAt}</span></div>`;

        if (addButton) {
            list.insertBefore(li, addButton.closest('li') || addButton);
        } else {
            list.appendChild(li);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initEvaluationReports);
    } else {
        initEvaluationReports();
    }
})();
