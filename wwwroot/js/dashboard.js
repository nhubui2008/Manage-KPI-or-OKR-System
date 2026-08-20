(function () {
    'use strict';

    if (window.DashboardPage?.init) {
        window.DashboardPage.init();
        return;
    }

    const chartRegistry = new Map();
    const chartFontFamily = "'Plus Jakarta Sans', 'Inter', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function readDashboardData(root) {
        const dataElement = root.querySelector('#dashboardData');
        if (!dataElement) return null;

        try {
            return JSON.parse(dataElement.textContent || '{}');
        } catch (error) {
            console.error('Dashboard data is not valid JSON.', error);
            return null;
        }
    }

    function getThemeColor(name, fallback) {
        const shellStyle = getComputedStyle(document.querySelector('.vietmach-shell') || document.body);
        const rootStyle = getComputedStyle(document.documentElement);
        const bodyStyle = getComputedStyle(document.body);
        return shellStyle.getPropertyValue(name).trim() || rootStyle.getPropertyValue(name).trim() || bodyStyle.getPropertyValue(name).trim() || fallback;
    }

    function colorWithAlpha(color, alpha, fallbackRgb) {
        const hex = color.trim().match(/^#([\da-f]{3}|[\da-f]{6})$/i);
        if (hex) {
            const raw = hex[1].length === 3
                ? hex[1].split('').map(character => character + character).join('')
                : hex[1];
            const red = parseInt(raw.slice(0, 2), 16);
            const green = parseInt(raw.slice(2, 4), 16);
            const blue = parseInt(raw.slice(4, 6), 16);
            return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
        }

        const rgb = color.match(/^rgba?\((\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
        if (rgb) return `rgba(${rgb[1]}, ${rgb[2]}, ${rgb[3]}, ${alpha})`;
        return `rgba(${fallbackRgb}, ${alpha})`;
    }

    function getChartTheme() {
        const primary = getThemeColor('--primary', '#6366f1');
        return {
            primary,
            primarySoft: colorWithAlpha(primary, 0.15, '99, 102, 241'),
            success: getThemeColor('--bs-success', '#10b981'),
            warning: getThemeColor('--bs-warning', '#f59e0b'),
            danger: getThemeColor('--bs-danger', '#f43f5e'),
            info: getThemeColor('--bs-info', '#06b6d4'),
            secondary: getThemeColor('--bs-secondary', '#8b5cf6'),
            text: getThemeColor('--vz-ink', '#0f172a'),
            muted: getThemeColor('--vz-muted', '#64748b'),
            grid: getThemeColor('--vz-border', '#f1f5f9'),
            neutral: '#e2e8f0',
            surface: '#ffffff'
        };
    }

    function destroyCharts() {
        chartRegistry.forEach(chart => {
            try {
                chart.destroy();
            } catch {
                // The previous canvas may have been replaced by instant navigation.
            }
        });
        chartRegistry.clear();
    }

    function setChartState(canvasId, stateId, showState, message) {
        const canvas = document.getElementById(canvasId);
        const state = document.getElementById(stateId);
        if (canvas) canvas.hidden = showState;
        if (!state) return;

        state.hidden = !showState;
        if (message) {
            const messageElement = state.querySelector('span');
            if (messageElement) messageElement.textContent = message;
        }
    }

    function showChartLibraryError(root) {
        const chartStates = [
            ['mainDashboardChart', 'mainDashboardChartState'],
            ['deptPerformanceChart', 'deptPerformanceChartState']
        ];
        chartStates.forEach(([canvasId, stateId]) => setChartState(canvasId, stateId, true, 'Không thể tải thư viện biểu đồ. Vui lòng tải lại trang.'));

        const okrCanvas = root.querySelector('#okrStatusChart');
        const okrNote = root.querySelector('#okrStatusChartNote');
        if (okrCanvas) okrCanvas.hidden = true;
        if (okrNote) {
            okrNote.hidden = false;
            okrNote.textContent = 'Không thể tải thư viện biểu đồ. Vui lòng tải lại trang.';
        }
    }

    function getBaseChartOptions(theme, reducedMotion) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: reducedMotion ? false : { duration: 450, easing: 'easeOutQuart' },
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    labels: {
                        color: theme.muted,
                        usePointStyle: true,
                        pointStyle: 'circle',
                        boxWidth: 7,
                        boxHeight: 7,
                        padding: 14,
                        font: { family: chartFontFamily, size: 11 }
                    }
                },
                tooltip: {
                    backgroundColor: '#212529',
                    borderColor: '#343a40',
                    borderWidth: 1,
                    titleColor: '#ffffff',
                    bodyColor: '#ffffff',
                    padding: 10,
                    displayColors: true,
                    titleFont: { family: chartFontFamily, size: 11, weight: '700' },
                    bodyFont: { family: chartFontFamily, size: 11 }
                }
            }
        };
    }

    const centerTextPlugin = {
        id: 'dashboardCenterText',
        afterDraw(chart, _args, options) {
            if (!options?.value && options?.value !== 0) return;

            const { ctx, chartArea } = chart;
            const centerX = (chartArea.left + chartArea.right) / 2;
            const centerY = (chartArea.top + chartArea.bottom) / 2;
            ctx.save();
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillStyle = options.color || '#212529';
            ctx.font = `700 21px ${chartFontFamily}`;
            ctx.fillText(String(options.value), centerX, centerY - 7);
            ctx.fillStyle = options.mutedColor || '#6d7080';
            ctx.font = `400 10px ${chartFontFamily}`;
            ctx.fillText(options.label || '', centerX, centerY + 13);
            ctx.restore();
        }
    };

    function renderTrendChart(data, theme, reducedMotion) {
        const canvas = document.getElementById('mainDashboardChart');
        if (!canvas) return;

        const values = Array.isArray(data.trendData) ? data.trendData.map(Number) : [];
        const labels = Array.isArray(data.trendLabels) ? data.trendLabels : [];
        const hasData = labels.length > 0 && (Number(data.totalCheckIns) > 0 || values.some(value => value > 0));
        setChartState('mainDashboardChart', 'mainDashboardChartState', !hasData);
        if (!hasData) return;

        const options = getBaseChartOptions(theme, reducedMotion);
        options.plugins.legend.display = false;
        options.plugins.tooltip.callbacks = {
            label: context => ` Tiến độ trung bình: ${context.parsed.y}%`
        };
        options.scales = {
            x: {
                grid: { display: false },
                border: { display: false },
                ticks: { color: theme.muted, font: { family: chartFontFamily, size: 10 }, maxRotation: 0 }
            },
            y: {
                beginAtZero: true,
                min: 0,
                max: 100,
                grid: { color: theme.grid, drawTicks: false },
                border: { display: false },
                ticks: {
                    color: theme.muted,
                    padding: 8,
                    stepSize: 20,
                    callback: value => `${value}%`,
                    font: { family: chartFontFamily, size: 10 }
                }
            }
        };

        const chart = new window.Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Tiến độ trung bình',
                    data: values,
                    borderColor: theme.primary,
                    backgroundColor: theme.primarySoft,
                    borderWidth: 2,
                    fill: true,
                    tension: 0.34,
                    pointRadius: 0,
                    pointHoverRadius: 4,
                    pointHitRadius: 12,
                    pointBackgroundColor: theme.primary,
                    pointBorderColor: theme.surface,
                    pointBorderWidth: 2
                }]
            },
            options
        });
        chartRegistry.set('trend', chart);
    }

    function renderOkrStatusChart(data, theme, reducedMotion) {
        const canvas = document.getElementById('okrStatusChart');
        if (!canvas) return;

        const labels = Array.isArray(data.okrLabels) ? data.okrLabels : [];
        const values = Array.isArray(data.okrData) ? data.okrData.map(Number) : [];
        const hasData = values.some(value => value > 0);
        const note = document.getElementById('okrStatusChartNote');
        if (note) note.hidden = hasData;
        canvas.hidden = false;

        const options = getBaseChartOptions(theme, reducedMotion);
        options.cutout = '72%';
        options.layout = { padding: { top: 2, right: 4, bottom: 0, left: 4 } };
        options.plugins.legend.display = hasData;
        options.plugins.legend.position = 'bottom';
        options.plugins.legend.labels.padding = 12;
        options.plugins.tooltip.enabled = hasData;
        options.plugins.dashboardCenterText = {
            value: hasData ? Number(data.totalOkrs || values.reduce((sum, value) => sum + value, 0)) : 0,
            label: 'OKR',
            color: theme.text,
            mutedColor: theme.muted
        };

        const palette = [theme.success, theme.warning, theme.primary, theme.danger, theme.secondary, theme.info];
        const chart = new window.Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: hasData ? labels : ['Chưa có dữ liệu'],
                datasets: [{
                    data: hasData ? values : [1],
                    backgroundColor: hasData ? values.map((_value, index) => palette[index % palette.length]) : [theme.neutral],
                    borderColor: theme.surface,
                    borderWidth: hasData ? 2 : 0,
                    hoverOffset: hasData ? 4 : 0
                }]
            },
            options,
            plugins: [centerTextPlugin]
        });
        chartRegistry.set('okr-status', chart);
    }

    function renderDepartmentChart(data, theme, reducedMotion) {
        const canvas = document.getElementById('deptPerformanceChart');
        if (!canvas) return;

        const labels = Array.isArray(data.departmentLabels) ? data.departmentLabels : [];
        const values = Array.isArray(data.departmentProgress) ? data.departmentProgress.map(Number) : [];
        const hasData = labels.length > 0 && values.some(value => value > 0);
        setChartState('deptPerformanceChart', 'deptPerformanceChartState', !hasData);
        if (!hasData) return;

        const options = getBaseChartOptions(theme, reducedMotion);
        options.indexAxis = 'y';
        options.interaction = { mode: 'nearest', axis: 'y', intersect: false };
        options.plugins.legend.display = false;
        options.plugins.tooltip.callbacks = {
            title: contexts => labels[contexts[0]?.dataIndex] || '',
            label: context => ` Hiệu suất trung bình: ${context.parsed.x}%`
        };
        options.scales = {
            y: {
                grid: { display: false },
                border: { display: false },
                ticks: {
                    color: theme.muted,
                    padding: 8,
                    callback(value) {
                        const label = this.getLabelForValue(value);
                        return label.length > 24 ? `${label.slice(0, 22)}…` : label;
                    },
                    font: { family: chartFontFamily, size: 10 }
                }
            },
            x: {
                beginAtZero: true,
                min: 0,
                max: 100,
                grid: { color: theme.grid, drawTicks: false },
                border: { display: false },
                ticks: {
                    color: theme.muted,
                    padding: 8,
                    stepSize: 20,
                    callback: value => `${value}%`,
                    font: { family: chartFontFamily, size: 10 }
                }
            }
        };

        const chart = new window.Chart(canvas, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Hiệu suất trung bình',
                    data: values,
                    backgroundColor: values.map(value => value >= 70 ? theme.success : value >= 40 ? theme.warning : theme.danger),
                    borderWidth: 0,
                    borderRadius: 3,
                    borderSkipped: false,
                    maxBarThickness: 22
                }]
            },
            options
        });
        chartRegistry.set('departments', chart);
    }

    function renderPerformanceAnalysis(data) {
        if (!data?.overview) {
            return `<div class="alert alert-light border mb-0">${escapeHtml(data?.warnings?.[0] || 'Chưa đủ check-in đã duyệt để tạo phân tích có căn cứ.')}</div>`;
        }

        const citationLabels = new Map((data.citations || []).map(citation => [
            `${citation.sourceType || ''}:${citation.sourceId || ''}`,
            citation.title || `${citation.sourceType || 'Nguồn'} #${citation.sourceId || ''}`
        ]));

        const renderInsight = (item, tone = 'primary') => {
            const sourceLabels = (item?.sourceIds || [])
                .map(sourceId => citationLabels.get(sourceId))
                .filter(Boolean);
            return `
                <div class="dashboard-insight dashboard-insight--${tone}">
                    <div class="dashboard-insight-title">${escapeHtml(item?.title || 'Nhận định')}</div>
                    <div class="dashboard-insight-detail">${escapeHtml(item?.detail || '')}</div>
                    ${sourceLabels.length ? `<div class="dashboard-insight-source"><strong>Nguồn:</strong> ${sourceLabels.map(escapeHtml).join('; ')}</div>` : ''}
                </div>`;
        };

        const renderSection = (title, items, tone) => {
            if (!items?.length) return '';
            return `
                <section aria-label="${escapeHtml(title)}">
                    <h3 class="dashboard-ai-section-title">${escapeHtml(title)}</h3>
                    ${items.map(item => renderInsight(item, tone)).join('')}
                </section>`;
        };

        return `
            <div class="ai-result-card">
                ${renderInsight(data.overview, 'primary')}
                ${renderSection('Điểm mạnh', data.strengths || [], 'success')}
                ${renderSection('Rủi ro', data.risks || [], 'warning')}
                ${renderSection('Hành động đề xuất', data.recommendedActions || [], 'info')}
                <p class="dashboard-ai-footnote">Phân tích chỉ mang tính tham khảo; không thay đổi điểm, xếp loại, trạng thái duyệt hoặc thưởng.</p>
            </div>`;
    }

    function renderCustomerSegments(segments, citations) {
        if (!segments.length) {
            return '<div class="alert alert-light border mb-0">AI chưa tìm thấy tệp khách hàng phù hợp từ dữ liệu hiện có.</div>';
        }

        const citationLabels = new Map((citations || []).map(citation => [
            `${citation.sourceType || ''}:${citation.sourceId || ''}`,
            citation.title || `${citation.sourceType || 'Nguồn'} #${citation.sourceId || ''}`
        ]));
        const cards = segments.map(item => {
            const sourceLabels = (item.sourceIds || [])
                .map(sourceId => citationLabels.get(sourceId))
                .filter(Boolean);
            return `
                <article class="dashboard-ai-segment">
                    <div class="d-flex justify-content-between gap-2 align-items-start mb-2">
                        <div>
                            <div class="fw-semibold text-dark">${escapeHtml(item.segmentName || 'Tệp khách hàng')}</div>
                            <div class="text-muted small mt-1">${escapeHtml(item.employeeFit || 'Chưa đủ dữ liệu khớp nhân viên.')}</div>
                        </div>
                        <span class="badge bg-secondary-subtle text-secondary">Tham khảo</span>
                    </div>
                    <div class="row g-2 small">
                        <div class="col-sm-6"><strong>Sản phẩm/ngành:</strong> ${escapeHtml(item.productOrService || 'Chưa rõ')}</div>
                        <div class="col-sm-6"><strong>Khu vực:</strong> ${escapeHtml(item.region || 'Chưa rõ')}</div>
                        <div class="col-sm-6"><strong>Vòng đời:</strong> ${escapeHtml(item.customerLifecycle || 'Chưa rõ')}</div>
                        <div class="col-sm-6"><strong>Căn cứ doanh thu:</strong> ${escapeHtml(item.revenueBasis || 'Thiếu dữ liệu')}</div>
                    </div>
                    <div class="small mt-2"><strong>Căn cứ đề xuất:</strong> ${escapeHtml(item.evidenceBasis || 'Chưa đủ căn cứ')}</div>
                    <div class="small mt-1"><strong>Hành động:</strong> ${escapeHtml(item.recommendedAction || 'N/A')}</div>
                    ${item.dataGaps ? `<div class="small text-muted mt-2"><i class="bi bi-info-circle me-1" aria-hidden="true"></i>${escapeHtml(item.dataGaps)}</div>` : ''}
                    ${sourceLabels.length ? `<div class="small text-muted mt-2"><strong>Nguồn:</strong> ${sourceLabels.map(escapeHtml).join('; ')}</div>` : ''}
                </article>`;
        }).join('');

        return `<div class="ai-result-card">${cards}<p class="dashboard-ai-footnote">Các phân khúc không được xếp hạng và không phải xác suất thành công.</p></div>`;
    }

    async function readResponseJson(response) {
        try {
            return await response.json();
        } catch {
            return {};
        }
    }

    function setButtonLoading(button, isLoading) {
        if (isLoading) {
            button.dataset.defaultHtml = button.innerHTML;
            button.style.width = `${Math.max(112, Math.ceil(button.getBoundingClientRect().width))}px`;
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
            button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span><span>Đang chạy</span>';
            return;
        }

        button.disabled = false;
        button.removeAttribute('aria-busy');
        button.innerHTML = button.dataset.defaultHtml || button.innerHTML;
    }

    function bindAiAction(buttonId, resultId, options) {
        const button = document.getElementById(buttonId);
        const result = document.getElementById(resultId);
        if (!button || !result || button.dataset.dashboardBound === 'true') return;
        button.dataset.dashboardBound = 'true';

        button.addEventListener('click', async () => {
            setButtonLoading(button, true);
            result.innerHTML = `<div class="text-muted small"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>${escapeHtml(options.loadingMessage)}</div>`;

            try {
                const response = await fetch(options.endpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        ...(window.antiForgeryHeaders?.() || {})
                    },
                    body: JSON.stringify({ periodId: button.dataset.periodId ? parseInt(button.dataset.periodId, 10) : null })
                });
                const data = await readResponseJson(response);
                if (!response.ok || data.success === false) {
                    result.innerHTML = `<div class="alert alert-warning mb-0">${escapeHtml(data.warnings?.[0] || options.errorMessage)}</div>`;
                    return;
                }
                result.innerHTML = options.render(data);
            } catch {
                result.innerHTML = '<div class="alert alert-danger mb-0">Không thể kết nối AI. Vui lòng kiểm tra kết nối và thử lại.</div>';
            } finally {
                setButtonLoading(button, false);
            }
        });
    }

    function bindAiActions() {
        bindAiAction('runAiPerformanceAnalysis', 'aiPerformanceAnalysisResult', {
            endpoint: '/AI/AnalyzePerformance',
            loadingMessage: 'Đang tổng hợp dữ liệu KPI/OKR...',
            errorMessage: 'Không thể phân tích AI.',
            render: renderPerformanceAnalysis
        });
        bindAiAction('runAiCustomerSegments', 'aiCustomerSegmentsResult', {
            endpoint: '/AI/SuggestCustomerSegments',
            loadingMessage: 'Đang đọc dữ liệu trong phạm vi được phép...',
            errorMessage: 'Không thể gợi ý tệp khách hàng.',
            render: data => renderCustomerSegments(data.segments || [], data.citations || [])
        });
    }

    function init() {
        const root = document.querySelector('.dashboard-page');
        if (!root) return;

        destroyCharts();
        bindAiActions();

        const data = readDashboardData(root);
        if (!data) {
            showChartLibraryError(root);
            return;
        }
        if (typeof window.Chart !== 'function') {
            showChartLibraryError(root);
            return;
        }

        const theme = getChartTheme();
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        renderTrendChart(data, theme, reducedMotion);
        renderOkrStatusChart(data, theme, reducedMotion);
        renderDepartmentChart(data, theme, reducedMotion);
    }

    window.DashboardPage = { init, destroy: destroyCharts };
    init();
})();
