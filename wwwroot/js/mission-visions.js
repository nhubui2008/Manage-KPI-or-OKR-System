/**
 * Velzon Mission & Visions Module Script
 * Handles interactive behaviors for MissionVision form (Create/Edit).
 */
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('missionVisionForm');
    if (!form || form.dataset.initialized === 'true') return;
    form.dataset.initialized = 'true';

    const currentYear = new Date().getFullYear().toString();
    const typeInputs = Array.from(form.querySelectorAll('input[name="MissionVisionType"]'));
    const contentLabelText = document.getElementById('contentLabelText');
    const contentInput = document.getElementById('missionContent');
    const contentHint = document.getElementById('contentHint');
    const contentCounter = document.getElementById('contentCounter');
    const inputModeHint = document.getElementById('inputModeHint');
    const financialTargetLabel = document.getElementById('financialTargetLabel');
    const financialTargetInput = document.getElementById('financialTargetInput');
    const financialPreview = document.getElementById('financialPreview');
    const targetYearWrapper = document.getElementById('targetYearWrapper');
    const targetYearInput = document.getElementById('targetYearInput');
    const guideIcon = document.getElementById('guideIcon');
    const guideBadge = document.getElementById('guideBadge');
    const guideTitle = document.getElementById('guideTitle');
    const guideDescription = document.getElementById('guideDescription');
    const guideCheckOne = document.getElementById('guideCheckOne');
    const guideCheckTwo = document.getElementById('guideCheckTwo');
    const guideRule = document.getElementById('guideRule');
    const submitButton = document.getElementById('submitMissionVision');
    const submitText = document.getElementById('submitMissionVisionText');

    let rememberedYear = (targetYearInput && targetYearInput.value) ? targetYearInput.value : currentYear;

    const configs = {
        'YearlyGoal': {
            label: 'Nội dung mục tiêu chiến lược',
            placeholder: 'Ví dụ: Chuẩn hóa chất lượng và giao hàng đúng hạn trên toàn chuỗi cung ứng...',
            hint: 'Nêu kết quả cụ thể, có thể theo dõi và liên kết với OKR trong năm.',
            modeHint: 'Cần chọn năm áp dụng; mục tiêu tài chính là tùy chọn.',
            financialLabel: 'Mục tiêu tài chính năm (VNĐ)',
            showYear: true,
            icon: 'bi-calendar-check',
            badge: 'Mục tiêu theo năm',
            title: 'Kết quả cần đạt',
            description: 'Viết một kết quả cụ thể mà tổ chức cần đạt trong năm đã chọn.',
            checkOne: 'Nội dung đủ rõ để liên kết với OKR.',
            checkTwo: 'Năm áp dụng nằm trong khoảng 2000-2100.',
            rule: 'Có thể tạo nhiều mục tiêu chiến lược cho cùng một năm.'
        },
        'Vision': {
            label: 'Nội dung tầm nhìn',
            placeholder: 'Ví dụ: Trở thành thương hiệu thực phẩm được tin chọn tại các kênh bán lẻ trọng điểm...',
            hint: 'Mô tả trạng thái tương lai dài hạn mà toàn tổ chức cùng hướng tới.',
            modeHint: 'Không cần năm áp dụng; hệ thống chỉ cho phép một Tầm nhìn đang hoạt động.',
            financialLabel: 'Mục tiêu tài chính dài hạn (VNĐ)',
            showYear: false,
            icon: 'bi-compass',
            badge: 'Tầm nhìn',
            title: 'Đích đến dài hạn',
            description: 'Diễn đạt doanh nghiệp muốn trở thành ai trong tương lai, ngắn gọn và dễ nhớ.',
            checkOne: 'Tập trung vào trạng thái tương lai mong muốn.',
            checkTwo: 'Không gắn với một năm vận hành cụ thể.',
            rule: 'Hệ thống chỉ duy trì một Tầm nhìn đang hoạt động.'
        },
        'Mission': {
            label: 'Nội dung sứ mệnh',
            placeholder: 'Ví dụ: Tạo ra sản phẩm an toàn, minh bạch và thuận tiện cho người tiêu dùng...',
            hint: 'Nêu đối tượng phục vụ, giá trị mang lại và lý do doanh nghiệp tồn tại.',
            modeHint: 'Không cần năm áp dụng; hệ thống chỉ cho phép một Sứ mệnh đang hoạt động.',
            financialLabel: 'Mục tiêu tài chính hỗ trợ (VNĐ)',
            showYear: false,
            icon: 'bi-bullseye',
            badge: 'Sứ mệnh',
            title: 'Giá trị cốt lõi',
            description: 'Làm rõ doanh nghiệp phục vụ ai, tạo ra giá trị gì và vì sao điều đó quan trọng.',
            checkOne: 'Nêu rõ đối tượng hoặc nhu cầu được phục vụ.',
            checkTwo: 'Thể hiện giá trị bền vững của tổ chức.',
            rule: 'Hệ thống chỉ duy trì một Sứ mệnh đang hoạt động.'
        }
    };

    function getSelectedType() {
        const selected = typeInputs.find(input => input.checked);
        return selected ? selected.value : 'YearlyGoal';
    }

    function applyTypeConfig() {
        const typeVal = getSelectedType();
        const config = configs[typeVal] || configs['YearlyGoal'];

        if (contentLabelText) contentLabelText.textContent = config.label;
        if (contentInput) contentInput.placeholder = config.placeholder;
        if (contentHint) contentHint.textContent = config.hint;
        if (inputModeHint) inputModeHint.textContent = config.modeHint;
        if (financialTargetLabel) financialTargetLabel.textContent = config.financialLabel;
        if (guideIcon) guideIcon.className = 'bi ' + config.icon;
        if (guideBadge) guideBadge.textContent = config.badge;
        if (guideTitle) guideTitle.textContent = config.title;
        if (guideDescription) guideDescription.textContent = config.description;
        if (guideCheckOne) guideCheckOne.textContent = config.checkOne;
        if (guideCheckTwo) guideCheckTwo.textContent = config.checkTwo;
        if (guideRule) guideRule.textContent = config.rule;

        if (targetYearWrapper && targetYearInput) {
            if (config.showYear) {
                targetYearWrapper.classList.remove('is-hidden');
                targetYearWrapper.setAttribute('aria-hidden', 'false');
                targetYearInput.disabled = false;
                targetYearInput.required = true;
                targetYearInput.value = targetYearInput.value || rememberedYear || currentYear;
            } else {
                if (targetYearInput.value) rememberedYear = targetYearInput.value;
                targetYearWrapper.classList.add('is-hidden');
                targetYearWrapper.setAttribute('aria-hidden', 'true');
                targetYearInput.required = false;
                targetYearInput.disabled = true;
                targetYearInput.value = '';
            }
        }
    }

    function updateContentCounter() {
        if (!contentInput || !contentCounter) return;
        const length = contentInput.value.length;
        contentCounter.textContent = length + '/1000';
        contentCounter.classList.toggle('text-danger', length >= 950);
    }

    function updateFinancialPreview() {
        if (!financialTargetInput || !financialPreview) return;
        const rawValue = financialTargetInput.value.trim();
        if (!rawValue) {
            financialPreview.textContent = 'Chưa đặt mục tiêu tài chính';
            return;
        }

        const isNegative = rawValue.startsWith('-');
        const digits = rawValue.replace(/\D/g, '');
        const formatted = digits.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        financialPreview.textContent = (isNegative ? '-' : '') + formatted + ' đ';
    }

    typeInputs.forEach(input => {
        input.addEventListener('change', applyTypeConfig);
    });

    if (contentInput) {
        contentInput.addEventListener('input', updateContentCounter);
    }

    if (financialTargetInput) {
        financialTargetInput.addEventListener('input', updateFinancialPreview);
    }

    form.addEventListener('submit', function (e) {
        const passesNativeValidation = form.checkValidity();
        const passesUnobtrusiveValidation = !window.jQuery || !window.jQuery.validator || window.jQuery(form).valid();

        if (!passesNativeValidation || !passesUnobtrusiveValidation) {
            return;
        }

        if (submitButton) {
            submitButton.disabled = true;
            submitButton.setAttribute('aria-busy', 'true');
            if (submitText) {
                const isEdit = form.getAttribute('action') && form.getAttribute('action').includes('Edit');
                submitText.textContent = isEdit ? 'Đang cập nhật...' : 'Đang lưu...';
            }
        }
    });

    applyTypeConfig();
    updateContentCounter();
    updateFinancialPreview();
});
