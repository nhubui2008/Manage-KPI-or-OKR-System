(() => {
    const root = document.querySelector('.kpi-allocation-page');
    if (!root || root.dataset.kpiAllocationInitialized === 'true') return;

    const configElement = document.getElementById('kpiAllocationConfig');
    if (!configElement) return;

    let config;
    try {
        config = JSON.parse(configElement.textContent);
    } catch (error) {
        console.error('Không thể đọc cấu hình phân bổ KPI.', error);
        return;
    }

    root.dataset.kpiAllocationInitialized = 'true';

    const totalKpiTarget = Number(config.totalKpiTarget) || 0;
    const initialAssignments = config.initialAssignments || {};
    const employeesData = config.employeesData || {};
    const unit = config.unit || '';
    const allocationList = root.querySelector('#allocationList');
    const emptyState = root.querySelector('#emptyState');

    const updateDepartmentSelectionState = checkbox => {
        const item = checkbox.closest('.department-select-item');
        if (!item) return;

        item.classList.toggle('border-info', checkbox.checked);
        item.classList.toggle('bg-info', checkbox.checked);
        item.classList.toggle('bg-opacity-10', checkbox.checked);
    };

    const updateDepartmentCount = () => {
        const display = root.querySelector('#departmentCountDisplay');
        if (display) {
            display.textContent = root.querySelectorAll('.department-cb:checked').length;
        }
    };

    const updateCalculatedTarget = (employeeId, weight) => {
        const targetLabel = root.querySelector(`#card-${employeeId} .target-calc`);
        if (!targetLabel) return;

        const calculatedValue = (totalKpiTarget * (Number(weight) / 100)).toLocaleString('vi-VN');
        targetLabel.textContent = `${calculatedValue} ${unit}`.trim();
    };

    const updateTotals = () => {
        const total = Array.from(root.querySelectorAll('.weight-input'))
            .reduce((sum, input) => sum + (Number.parseFloat(input.value) || 0), 0);

        const display = root.querySelector('#totalPercentageDisplay');
        const summaryCard = root.querySelector('#summaryCard');
        const icon = root.querySelector('#validationIcon');
        const saveButton = root.querySelector('#saveBtn');
        if (!display || !summaryCard || !icon || !saveButton) return;

        display.textContent = `${Math.round(total)}%`;
        summaryCard.classList.remove('valid', 'invalid');

        if (Math.abs(total - 100) < 0.1) {
            summaryCard.classList.add('valid');
            icon.className = 'bi bi-check-circle-fill text-success fs-4';
            saveButton.disabled = false;
            saveButton.innerHTML = '<i class="bi bi-check2-circle me-2"></i> Lưu phân bổ KPI';
            return;
        }

        summaryCard.classList.add('invalid');
        icon.className = 'bi bi-exclamation-circle-fill text-danger fs-4';
        saveButton.innerHTML = `<i class="bi bi-exclamation-triangle me-2"></i> Tổng ${Math.round(total)}% (Cần 100%)`;
    };

    const renderAllocationCard = (employeeId, weight) => {
        if (!allocationList || root.querySelector(`#card-${employeeId}`)) return;

        const employee = employeesData[employeeId];
        const template = document.getElementById('allocationCardTemplate');
        if (!employee || !template) return;

        const html = template.innerHTML
            .replaceAll('{id}', String(employeeId))
            .replace('{weight}', String(weight));
        const wrapper = document.createElement('div');
        wrapper.innerHTML = html;

        const card = wrapper.firstElementChild;
        if (!card) return;

        card.querySelector('.name-label').textContent = employee.name || '';
        card.querySelector('.code-label').textContent = employee.code || '';
        card.querySelector('.weight-slider').value = weight;
        allocationList.appendChild(card);
        emptyState?.classList.add('d-none');
        updateCalculatedTarget(employeeId, weight);
    };

    const updateWeight = (employeeId, value) => {
        const slider = root.querySelector(`#card-${employeeId} .weight-slider`);
        const input = root.querySelector(`#input-${employeeId}`);
        if (!slider || !input) return;

        slider.value = value;
        input.value = value;
        updateCalculatedTarget(employeeId, value);
        updateTotals();
    };

    const removeEmployee = (employeeId, updateCheckbox = true) => {
        const card = root.querySelector(`#card-${employeeId}`);
        if (card) {
            card.querySelector('input[name="employeeIds"]')?.setAttribute('disabled', 'disabled');
            card.classList.replace('animate__fadeInUp', 'animate__fadeOutDown');
            window.setTimeout(() => {
                card.remove();
                if (!root.querySelector('.allocation-card')) {
                    emptyState?.classList.remove('d-none');
                }
                updateTotals();
            }, 200);
        }

        if (!updateCheckbox) return;

        const checkbox = root.querySelector(`#cb-${employeeId}`);
        if (!checkbox) return;

        checkbox.checked = false;
        checkbox.closest('.employee-select-item')?.classList.remove('border-primary', 'bg-primary', 'bg-opacity-5');
    };

    const handleEmployeeSelection = checkbox => {
        const employeeId = Number.parseInt(checkbox.value, 10);
        const item = checkbox.closest('.employee-select-item');

        if (checkbox.checked) {
            item?.classList.add('border-primary', 'bg-primary', 'bg-opacity-5');
            renderAllocationCard(employeeId, 0);
            updateTotals();
        } else {
            item?.classList.remove('border-primary', 'bg-primary', 'bg-opacity-5');
            removeEmployee(employeeId, false);
        }
    };

    root.addEventListener('change', event => {
        const target = event.target;
        if (!(target instanceof HTMLInputElement)) return;

        if (target.matches('.department-cb')) {
            updateDepartmentSelectionState(target);
            updateDepartmentCount();
        } else if (target.matches('.employee-cb')) {
            handleEmployeeSelection(target);
        } else if (target.matches('.dept-toggle')) {
            root.querySelectorAll('.employee-cb').forEach(checkbox => {
                if (checkbox.dataset.dept === target.dataset.deptName && checkbox.checked !== target.checked) {
                    checkbox.checked = target.checked;
                    handleEmployeeSelection(checkbox);
                }
            });
        }
    });

    allocationList?.addEventListener('input', event => {
        const target = event.target;
        if (!(target instanceof HTMLInputElement) || !target.matches('.weight-slider, .weight-input')) return;
        updateWeight(target.dataset.employeeId, target.value);
    });

    allocationList?.addEventListener('click', event => {
        const removeButton = event.target.closest('[data-remove-employee]');
        if (removeButton) {
            removeEmployee(removeButton.dataset.removeEmployee);
        }
    });

    root.querySelector('#equalizeWeightsButton')?.addEventListener('click', () => {
        const cards = root.querySelectorAll('.allocation-card');
        if (!cards.length) return;

        const equalWeight = (100 / cards.length).toFixed(1);
        cards.forEach(card => updateWeight(card.id.replace('card-', ''), equalWeight));
    });

    root.querySelector('#employeeSearch')?.addEventListener('input', event => {
        const searchText = event.target.value.toLocaleLowerCase('vi-VN');

        root.querySelectorAll('.department-group').forEach(group => {
            let hasVisibleEmployee = false;
            group.querySelectorAll('.employee-select-item').forEach(item => {
                const isVisible = (item.dataset.search || '').includes(searchText);
                item.hidden = !isVisible;
                hasVisibleEmployee ||= isVisible;
            });
            group.hidden = !hasVisibleEmployee;
        });
    });

    Object.entries(initialAssignments).forEach(([employeeId, weight]) => {
        renderAllocationCard(Number.parseInt(employeeId, 10), weight);
    });
    root.querySelectorAll('.department-cb').forEach(updateDepartmentSelectionState);
    updateDepartmentCount();
    updateTotals();
})();
