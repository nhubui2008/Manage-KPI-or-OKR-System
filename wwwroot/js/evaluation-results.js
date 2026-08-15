/**
 * Evaluation Results Module
 * Handles modals, filtering, scoring, and AI review draft interactions
 */

(function() {
  'use strict';

  // Guard: Only run on evaluation results pages
  const pageRoot = document.querySelector('.evaluation-results-page, .evaluation-review-page');
  if (!pageRoot) {
    return;
  }

  // ===== INITIALIZATION STATE =====
  const state = {
    isInitialized: false,
    selectedRankScale: null,
    filterState: {
      searchTerm: '',
      periodFilter: '',
      statusFilter: '',
      classificationFilter: ''
    },
    aiDraft: {
      evaluationResultId: null,
      draftActionId: null,
      rowVersion: null,
      isStale: false,
      isLoading: false
    }
  };

  // ===== RANK RESOLUTION =====
  function resolveRankFromScore(score, rankScale) {
    if (!rankScale || score === undefined || score === null) {
      return { rankCode: 'N/A', description: 'Chưa phân loại', rankId: null };
    }

    const numScore = parseFloat(score);
    if (isNaN(numScore)) {
      return { rankCode: 'N/A', description: 'Chưa phân loại', rankId: null };
    }

    for (const rank of rankScale) {
      if (numScore >= rank.minScore) {
        return {
          rankId: rank.id,
          rankCode: rank.rankCode,
          description: rank.description
        };
      }
    }

    return { rankCode: 'N/A', description: 'Chưa phân loại', rankId: null };
  }

  // ===== SCORE INPUT LISTENER =====
  function initScoreInputs(rankScale) {
    const scoreInputs = document.querySelectorAll('.js-score-input, input[name="TotalScore"]');

    scoreInputs.forEach(input => {
      input.addEventListener('input', function() {
        const score = this.value;
        const rank = resolveRankFromScore(score, rankScale);

        // Update preview displays
        const previewRank = this.closest('.modal') ? this.closest('.modal').querySelector('[data-rank-display]') : null;
        const previewClass = this.closest('.modal') ? this.closest('.modal').querySelector('[data-classification-display]') : null;

        if (previewRank) {
          previewRank.textContent = rank.rankCode;
        }
        if (previewClass) {
          previewClass.textContent = rank.description;
        }
      });
    });
  }

  // ===== MODAL POPULATION =====
  function populateEditModal(btn) {
    if (!btn.classList.contains('js-edit-result')) return;

    const resultId = btn.dataset.id;
    const employeeId = btn.dataset.employeeId;
    const periodId = btn.dataset.periodId;
    const score = btn.dataset.score;
    const rankId = btn.dataset.rankId;
    const classification = btn.dataset.classification;
    const reviewComment = btn.dataset.reviewComment || '';

    const modal = document.getElementById('editModal');
    if (!modal) return;

    // Set form field values
    const editIdField = modal.querySelector('#editId');
    const editEmpIdField = modal.querySelector('#editEmployeeId');
    const editPeriodIdField = modal.querySelector('#editPeriodId');
    const editScoreField = modal.querySelector('#editTotalScore');
    const editRankDisplay = modal.querySelector('#editRankDisplay');
    const editClassDisplay = modal.querySelector('#editClassificationDisplay');
    const editCommentField = modal.querySelector('#editReviewComment');

    if (editIdField) editIdField.value = resultId || '';
    if (editEmpIdField) editEmpIdField.value = employeeId || '';
    if (editPeriodIdField) editPeriodIdField.value = periodId || '';
    if (editScoreField) editScoreField.value = score || '';
    if (editRankDisplay) editRankDisplay.textContent = rankId ? (state.selectedRankScale?.find(r => r.id == rankId)?.rankCode || 'N/A') : 'N/A';
    if (editClassDisplay) editClassDisplay.textContent = classification || 'Chưa phân loại';
    if (editCommentField) editCommentField.value = reviewComment;

    // Clear AI draft when opening modal for different record
    clearAIDraft();
  }

  // ===== MODAL LISTENERS =====
  function initModalListeners(rankScale) {
    state.selectedRankScale = rankScale;

    // Edit button listeners
    const editButtons = document.querySelectorAll('.js-edit-result');
    editButtons.forEach(btn => {
      btn.addEventListener('click', function(e) {
        e.preventDefault();
        populateEditModal(this);
      });
    });

    // Modal-triggered score update
    const editScoreInput = document.querySelector('#editTotalScore');
    if (editScoreInput) {
      editScoreInput.addEventListener('input', function() {
        const score = this.value;
        const rank = resolveRankFromScore(score, rankScale);
        const modal = this.closest('.modal');

        const rankDisplay = modal.querySelector('#editRankDisplay');
        const classDisplay = modal.querySelector('#editClassificationDisplay');

        if (rankDisplay) rankDisplay.textContent = rank.rankCode;
        if (classDisplay) classDisplay.textContent = rank.description;
      });
    }
  }

  // ===== FILTER STATE MANAGEMENT =====
  function updateFilterState(filterType, value) {
    state.filterState[filterType] = value;
    applyFilters();
  }

  function applyFilters() {
    const rows = document.querySelectorAll('tbody tr, .evaluation-review-card, [data-filterable="true"]');
    let visibleCount = 0;

    rows.forEach(row => {
      const matchesSearch = !state.filterState.searchTerm ||
        row.textContent.toLowerCase().includes(state.filterState.searchTerm.toLowerCase());

      const matchesPeriod = !state.filterState.periodFilter ||
        row.dataset.periodId === state.filterState.periodFilter;

      const matchesStatus = !state.filterState.statusFilter ||
        row.dataset.status === state.filterState.statusFilter;

      const matchesClassification = !state.filterState.classificationFilter ||
        row.dataset.classification === state.filterState.classificationFilter;

      const isVisible = matchesSearch && matchesPeriod && matchesStatus && matchesClassification;
      row.style.display = isVisible ? '' : 'none';

      if (isVisible) visibleCount++;
    });

    // Update result count display
    const resultCountEl = document.querySelector('[data-result-count]');
    if (resultCountEl) {
      resultCountEl.textContent = visibleCount;
    }

    // Show empty state if no results
    const emptyState = document.querySelector('.evaluation-empty, .rq-empty');
    if (emptyState) {
      emptyState.style.display = visibleCount === 0 ? 'block' : 'none';
    }
  }

  function clearFilters() {
    state.filterState = {
      searchTerm: '',
      periodFilter: '',
      statusFilter: '',
      classificationFilter: ''
    };

    // Reset filter inputs
    const searchInput = document.querySelector('[data-filter-search]');
    const periodSelect = document.querySelector('[data-filter-period]');
    const statusSelect = document.querySelector('[data-filter-status]');
    const classSelect = document.querySelector('[data-filter-classification]');

    if (searchInput) searchInput.value = '';
    if (periodSelect) periodSelect.value = '';
    if (statusSelect) statusSelect.value = '';
    if (classSelect) classSelect.value = '';

    applyFilters();
  }

  function initFilterListeners() {
    const searchInput = document.querySelector('[data-filter-search]');
    if (searchInput) {
      searchInput.addEventListener('input', e => {
        updateFilterState('searchTerm', e.target.value);
      });
    }

    const periodSelect = document.querySelector('[data-filter-period]');
    if (periodSelect) {
      periodSelect.addEventListener('change', e => {
        updateFilterState('periodFilter', e.target.value);
      });
    }

    const statusSelect = document.querySelector('[data-filter-status]');
    if (statusSelect) {
      statusSelect.addEventListener('change', e => {
        updateFilterState('statusFilter', e.target.value);
      });
    }

    const classSelect = document.querySelector('[data-filter-classification]');
    if (classSelect) {
      classSelect.addEventListener('change', e => {
        updateFilterState('classificationFilter', e.target.value);
      });
    }

    const clearBtn = document.querySelector('[data-filter-reset]');
    if (clearBtn) {
      clearBtn.addEventListener('click', clearFilters);
    }
  }

  // ===== AI REVIEW DRAFT =====
  function showAIDraftLoading() {
    const panel = document.querySelector('#aiReviewDraftPanel');
    const btn = document.querySelector('#aiGenerateReviewBtn');

    if (panel) {
      panel.innerHTML = '<p role="status" aria-live="polite">AI đang viết nhận xét...</p>';
      panel.style.display = 'block';
    }

    if (btn) {
      btn.disabled = true;
      const originalText = btn.textContent;
      btn.dataset.originalText = originalText;
      btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang xử lý...';
    }

    state.aiDraft.isLoading = true;
  }

  function clearAIDraft() {
    const panel = document.querySelector('#aiReviewDraftPanel');
    const applyBtn = document.querySelector('#aiApplyReviewDraftBtn');
    const rejectBtn = document.querySelector('#aiRejectReviewDraftBtn');
    const btn = document.querySelector('#aiGenerateReviewBtn');

    if (panel) panel.style.display = 'none';
    if (applyBtn) applyBtn.disabled = true;
    if (rejectBtn) rejectBtn.disabled = true;

    if (btn) {
      btn.disabled = false;
      btn.innerHTML = btn.dataset.originalText || 'Viết nhận xét bằng AI';
    }

    state.aiDraft = {
      evaluationResultId: null,
      draftActionId: null,
      rowVersion: null,
      isStale: false,
      isLoading: false
    };
  }

  function initAIDraft() {
    const generateBtn = document.querySelector('#aiGenerateReviewBtn');
    const applyBtn = document.querySelector('#aiApplyReviewDraftBtn');
    const rejectBtn = document.querySelector('#aiRejectReviewDraftBtn');

    if (!generateBtn) return;

    generateBtn.addEventListener('click', async function(e) {
      e.preventDefault();

      const modal = this.closest('.modal') || this.closest('.form-container');
      const resultId = modal?.querySelector('[name="id"]')?.value ||
                      modal?.querySelector('#editId')?.value;

      if (!resultId) {
        if (window.AppFeedback) {
          window.AppFeedback.toast('Không xác định được bản ghi. Vui lòng chọn lại.', 'error');
        }
        return;
      }

      showAIDraftLoading();
      state.aiDraft.evaluationResultId = parseInt(resultId);

      try {
        const headers = window.antiForgeryHeaders ? window.antiForgeryHeaders() : {};
        const response = await fetch('/AI/GenerateReview', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...headers
          },
          body: JSON.stringify({ evaluationResultId: parseInt(resultId) })
        });

        if (!response.ok) {
          const statusCode = response.status;
          let errorMessage = 'Lỗi khi tạo nhận xét AI';

          if (statusCode === 400) errorMessage = 'Dữ liệu bản ghi không hợp lệ';
          else if (statusCode === 403) errorMessage = 'Bạn không có quyền tạo nhận xét';
          else if (statusCode === 409) errorMessage = 'Bản ghi đã thay đổi, vui lòng tải lại';
          else if (statusCode === 502) errorMessage = 'Dịch vụ AI không khả dụng';
          else if (statusCode >= 500) errorMessage = 'Lỗi máy chủ, vui lòng thử lại';

          if (window.AppFeedback) {
            window.AppFeedback.toast(errorMessage, 'error');
          }
          clearAIDraft();
          return;
        }

        const data = await response.json();
        displayAIDraft(data);
      } catch (error) {
        console.error('AI draft error:', error);
        if (window.AppFeedback) {
          window.AppFeedback.toast('Lỗi kết nối, vui lòng thử lại', 'error');
        }
        clearAIDraft();
      }
    });

    if (applyBtn) {
      applyBtn.addEventListener('click', async function(e) {
        e.preventDefault();
        if (state.aiDraft.isStale) {
          if (window.AppFeedback) {
            window.AppFeedback.toast('Bản nháp đã hết hạn, vui lòng tạo lại', 'warning');
          }
          clearAIDraft();
          return;
        }

        await submitAIDraftDecision('Accept');
      });
    }

    if (rejectBtn) {
      rejectBtn.addEventListener('click', async function(e) {
        e.preventDefault();
        await submitAIDraftDecision('Reject');
      });
    }
  }

  function displayAIDraft(data) {
    const panel = document.querySelector('#aiReviewDraftPanel');
    const btn = document.querySelector('#aiGenerateReviewBtn');
    const applyBtn = document.querySelector('#aiApplyReviewDraftBtn');
    const rejectBtn = document.querySelector('#aiRejectReviewDraftBtn');

    if (!panel) return;

    state.aiDraft.draftActionId = data.draftActionId;
    state.aiDraft.rowVersion = data.rowVersion;
    state.aiDraft.isLoading = false;

    // Sanitize draft text (basic: no HTML)
    const draftText = document.querySelector('#aiReviewDraftText');
    if (draftText) {
      draftText.textContent = data.draftText || '';
    }

    // Display citations if any
    const citations = document.querySelector('#aiReviewDraftCitations');
    if (citations && data.citations) {
      citations.innerHTML = '';
      data.citations.forEach(cite => {
        const item = document.createElement('div');
        item.style.fontSize = '0.8rem';
        item.style.color = '#666';
        item.textContent = cite;
        citations.appendChild(item);
      });
    }

    // Show warning if draft sourced data changed
    const warning = document.querySelector('#aiReviewDraftWarning');
    if (warning && data.sourceDataChanged) {
      warning.style.display = 'block';
      state.aiDraft.isStale = true;
      if (applyBtn) applyBtn.disabled = true;
    }

    if (btn) btn.disabled = false;
    if (applyBtn) applyBtn.disabled = false;
    if (rejectBtn) rejectBtn.disabled = false;

    panel.style.display = 'block';

    if (window.AppFeedback) {
      window.AppFeedback.toast('Nhận xét AI đã tạo', 'success');
    }
  }

  async function submitAIDraftDecision(decision) {
    if (!state.aiDraft.draftActionId || !state.aiDraft.rowVersion) return;

    const applyBtn = document.querySelector('#aiApplyReviewDraftBtn');
    const rejectBtn = document.querySelector('#aiRejectReviewDraftBtn');

    if (applyBtn) applyBtn.disabled = true;
    if (rejectBtn) rejectBtn.disabled = true;

    try {
      const headers = window.antiForgeryHeaders ? window.antiForgeryHeaders() : {};
      const response = await fetch('/AI/DecideEvaluationReviewDraft', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...headers
        },
        body: JSON.stringify({
          draftActionId: state.aiDraft.draftActionId,
          rowVersion: state.aiDraft.rowVersion,
          decision: decision
        })
      });

      if (!response.ok) {
        const statusCode = response.status;
        let errorMessage = 'Lỗi khi xử lý quyết định';

        if (statusCode === 409) errorMessage = 'Bản nháp đã hết hạn';
        else if (statusCode === 403) errorMessage = 'Không có quyền xử lý';

        if (window.AppFeedback) {
          window.AppFeedback.toast(errorMessage, 'error');
        }
        return;
      }

      if (decision === 'Accept') {
        const modal = document.querySelector('#editModal');
        const commentField = modal?.querySelector('#editReviewComment');
        if (commentField) {
          const draftText = document.querySelector('#aiReviewDraftText');
          if (draftText && draftText.textContent) {
            commentField.value = draftText.textContent;
          }
        }

        if (window.AppFeedback) {
          window.AppFeedback.toast('Đã áp dụng nhận xét AI', 'success');
        }
      }

      clearAIDraft();
    } catch (error) {
      console.error('AI draft decision error:', error);
      if (window.AppFeedback) {
        window.AppFeedback.toast('Lỗi kết nối', 'error');
      }
    }
  }

  // ===== INITIALIZATION =====
  function initialize() {
    if (state.isInitialized) return;

    // Get rank scale from data attribute or global
    let rankScale = [];
    const rankDataEl = document.querySelector('[data-rank-scale]');
    if (rankDataEl) {
      try {
        rankScale = JSON.parse(rankDataEl.dataset.rankScale);
      } catch (e) {
        console.error('Failed to parse rank scale:', e);
      }
    }

    initModalListeners(rankScale);
    initScoreInputs(rankScale);
    initFilterListeners();
    initAIDraft();

    state.isInitialized = true;
  }

  // Run on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initialize);
  } else {
    initialize();
  }

  // Handle Turbo/nav re-init (preserve Copilot nav compatibility)
  document.addEventListener('turbo:load', initialize);

})();
