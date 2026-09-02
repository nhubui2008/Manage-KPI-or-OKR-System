(function () {
    "use strict";

    function readJson(id) {
        const element = document.getElementById(id);
        if (!element) return {};

        try {
            return JSON.parse(element.textContent || "{}");
        } catch {
            return {};
        }
    }

    function initializeCheckInForm() {
        const kpiMap = readJson("checkInKpiData");
        const assignmentWeights = readJson("checkInAssignmentWeights");
        const employeeKpiIds = readJson("checkInEmployeeKpiIds");
        const allKpiOptions = readJson("checkInKpiOptions");
        const progressSnapshots = readJson("checkInProgressSnapshots");

        const kpiSelect = document.getElementById("kpiSelect");
        const employeeSelect = document.getElementById("employeeSelect");
        const achievedInput = document.getElementById("achievedInput");
        if (!kpiSelect || !employeeSelect || !achievedInput) return;

        const targetDisplay = document.getElementById("targetDisplay");
        const individualTargetDisplay = document.getElementById("individualTargetDisplay");
        const weightLabel = document.getElementById("weightLabel");
        const unitDisplay = document.getElementById("unitDisplay");
        const achievedUnitSuffix = document.getElementById("achievedUnitSuffix");
        const modeDisplay = document.getElementById("modeDisplay");
        const progressPercentage = document.getElementById("progressPercentage");
        const progressScopeLabel = document.getElementById("progressScopeLabel");
        const liveProgressBar = document.getElementById("liveProgressBar");
        const deadlineDisplay = document.getElementById("deadlineDisplay");
        const deadlineTargetDisplay = document.getElementById("deadlineTargetDisplay");
        const reminderDisplay = document.getElementById("reminderDisplay");
        const previousEmployeeAchieved = document.getElementById("previousEmployeeAchieved");
        const departmentAchievedDisplay = document.getElementById("departmentAchievedDisplay");
        const departmentProgressLabel = document.getElementById("departmentProgressLabel");
        const previousProgressBar = document.getElementById("previousProgressBar");
        const previousProgressCaption = document.getElementById("previousProgressCaption");

        function resolveCurrentDeadline(data) {
            const now = new Date();
            const frequencyDays = Math.max(1, Number.parseInt(data.frequencyDays || 1, 10));
            const periodStart = data.periodStart ? new Date(data.periodStart) : new Date(now);
            const periodEnd = data.periodEnd ? new Date(data.periodEnd) : null;
            const configuredDeadline = data.deadlineDate ? new Date(data.deadlineDate) : null;
            const baseDate = new Date(now.getFullYear(), now.getMonth(), now.getDate());
            const startDate = new Date(periodStart.getFullYear(), periodStart.getMonth(), periodStart.getDate());
            let targetDate = baseDate < startDate ? startDate : baseDate;

            if (periodEnd) {
                const endDate = new Date(periodEnd.getFullYear(), periodEnd.getMonth(), periodEnd.getDate());
                if (targetDate > endDate) targetDate = endDate;
            }

            if (configuredDeadline) {
                const deadlineDate = new Date(configuredDeadline.getFullYear(), configuredDeadline.getMonth(), configuredDeadline.getDate());
                if (targetDate > deadlineDate) targetDate = deadlineDate;
            }

            const offsetDays = Math.max(0, Math.floor((targetDate - startDate) / 86400000));
            const slotDate = new Date(startDate);
            slotDate.setDate(startDate.getDate() + Math.floor(offsetDays / frequencyDays) * frequencyDays);
            const [hour, minute] = String(data.deadlineTime || "10:00").split(":").map(Number);
            slotDate.setHours(hour || 0, minute || 0, 0, 0);
            return slotDate;
        }

        function calculateExpectedAtDeadline(individualTarget, data, deadline) {
            if (!data.periodStart || !data.periodEnd || !individualTarget) return individualTarget;

            const periodStart = new Date(data.periodStart);
            const periodEnd = new Date(data.periodEnd);
            const startDate = new Date(periodStart.getFullYear(), periodStart.getMonth(), periodStart.getDate());
            let endDate = new Date(periodEnd.getFullYear(), periodEnd.getMonth(), periodEnd.getDate());

            if (data.deadlineDate) {
                const configuredDeadline = new Date(data.deadlineDate);
                const deadlineDate = new Date(configuredDeadline.getFullYear(), configuredDeadline.getMonth(), configuredDeadline.getDate());
                if (deadlineDate < endDate) endDate = deadlineDate;
            }

            const dueDate = new Date(deadline.getFullYear(), deadline.getMonth(), deadline.getDate());
            const totalDays = Math.max(1, Math.floor((endDate - startDate) / 86400000) + 1);
            const elapsedDays = Math.min(totalDays, Math.max(1, Math.floor((dueDate - startDate) / 86400000) + 1));
            return individualTarget * (elapsedDays / totalDays);
        }

        function calculateProgressValue(value, target, isInverse) {
            if (!target) return isInverse ? (value === 0 ? 100 : 0) : (value > 0 ? 100 : 0);

            if (isInverse) {
                if (value <= target) return 100;
                return Math.max(0, 100 - (((value - target) / target) * 100));
            }

            return (value / target) * 100;
        }

        function formatMetric(value, unit) {
            const numeric = Number(value || 0);
            return new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 2 }).format(numeric) + (unit ? ` ${unit}` : "");
        }

        function filterKpisByEmployee() {
            const employeeId = employeeSelect.value;
            const previousKpiId = kpiSelect.value;
            const allowedKpiIds = new Set((employeeKpiIds[employeeId] || []).map(String));
            const filteredKpis = employeeId
                ? allKpiOptions.filter(kpi => allowedKpiIds.has(String(kpi.id)))
                : [];

            kpiSelect.innerHTML = "";
            const placeholderText = employeeId
                ? (filteredKpis.length ? "Chọn KPI cần cập nhật" : "Nhân viên này chưa có KPI được phân bổ")
                : "Chọn nhân viên trước";
            kpiSelect.add(new Option(placeholderText, ""));

            filteredKpis.forEach(kpi => kpiSelect.add(new Option(kpi.name, kpi.id)));

            if (previousKpiId && filteredKpis.some(kpi => String(kpi.id) === String(previousKpiId))) {
                kpiSelect.value = previousKpiId;
            } else if (filteredKpis.length === 1) {
                kpiSelect.value = String(filteredKpis[0].id);
            }

            if (window.AppComboBox && typeof window.AppComboBox.sync === "function") {
                window.AppComboBox.sync(kpiSelect);
            }
        }

        function setEmptyPreview() {
            targetDisplay.textContent = "--";
            individualTargetDisplay.textContent = "--";
            weightLabel.textContent = "100%";
            unitDisplay.textContent = "--";
            deadlineDisplay.textContent = "--";
            deadlineTargetDisplay.textContent = "--";
            reminderDisplay.textContent = "--";
            previousEmployeeAchieved.textContent = "--";
            departmentAchievedDisplay.textContent = "--";
            departmentProgressLabel.textContent = "Phòng ban";
            previousProgressBar.style.width = "0%";
            previousProgressCaption.textContent = "Chưa có check-in đã xác nhận.";
            modeDisplay.textContent = "--";
            progressScopeLabel.textContent = "Tiến độ ước tính";
            progressPercentage.textContent = "0%";
            liveProgressBar.style.width = "0%";
            liveProgressBar.className = "progress-bar bg-primary transition-all";

            const unitConfig = window.applyMeasurementUnitConfigToInputs
                ? window.applyMeasurementUnitConfigToInputs("", [achievedInput])
                : { suffix: "đơn vị" };
            achievedUnitSuffix.textContent = unitConfig.suffix || "đơn vị";
        }

        function updateStats() {
            const kpiId = kpiSelect.value;
            const value = Number.parseFloat(achievedInput.value) || 0;
            const data = kpiMap[kpiId];
            if (!kpiId || !data) {
                setEmptyPreview();
                return;
            }

            const target = Number(data.target || 0);
            const employeeId = employeeSelect.value;
            const employeeWeights = assignmentWeights[employeeId] || {};
            const weight = Number(employeeWeights[kpiId] || 100);
            const individualTarget = target * (weight / 100);
            const unit = data.unit || "đơn vị";
            const isInverse = data.isInverse === true;
            const snapshot = progressSnapshots[employeeId]?.[kpiId] || {};
            const latestAchieved = Number(snapshot.latestAchievedValue || 0);
            const latestProgress = Number(snapshot.latestProgressPercentage || 0);
            const departmentAchieved = Number(snapshot.departmentAchievedValue || 0);
            const departmentProgress = Number(snapshot.departmentProgressPercentage || 0);
            const isDepartmentScope = snapshot.isDepartmentAssigned === true;
            const projectedDepartmentAchieved = Math.max(0, departmentAchieved - latestAchieved + value);
            const deadline = resolveCurrentDeadline(data);
            const expectedAtDeadline = calculateExpectedAtDeadline(individualTarget, data, deadline);
            const unitConfig = window.applyMeasurementUnitConfigToInputs
                ? window.applyMeasurementUnitConfigToInputs(unit, [achievedInput])
                : { suffix: unit };

            targetDisplay.textContent = formatMetric(target, unit);
            individualTargetDisplay.textContent = formatMetric(individualTarget, unit);
            weightLabel.textContent = `${weight}%`;
            unitDisplay.textContent = unit;
            achievedUnitSuffix.textContent = unitConfig.suffix || unit;
            modeDisplay.textContent = isInverse ? "Càng thấp càng tốt" : "Càng cao càng tốt";
            deadlineDisplay.textContent = deadline.toLocaleString("vi-VN", {
                day: "2-digit",
                month: "2-digit",
                year: "numeric",
                hour: "2-digit",
                minute: "2-digit"
            });
            deadlineTargetDisplay.textContent = `${formatMetric(expectedAtDeadline, unit)} · chỉ tiêu cá nhân`;
            reminderDisplay.textContent = `${data.reminderBeforeHours || 24} giờ`;
            previousEmployeeAchieved.textContent = formatMetric(latestAchieved, unit);
            departmentAchievedDisplay.textContent = isDepartmentScope
                ? formatMetric(departmentAchieved, unit)
                : "Không phân bổ phòng ban";
            departmentProgressLabel.textContent = snapshot.departmentName
                ? `Phòng ban · ${snapshot.departmentName}`
                : "Phòng ban";

            const previousProgress = isDepartmentScope ? departmentProgress : latestProgress;
            previousProgressBar.style.width = `${Math.min(100, previousProgress)}%`;
            previousProgressCaption.textContent = previousProgress > 0
                ? `Đã ghi nhận ${Math.round(previousProgress * 100) / 100}% trước lần check-in này.`
                : "Chưa có check-in đã xác nhận.";

            const progressTarget = isDepartmentScope ? target : individualTarget;
            const progressValue = isDepartmentScope ? projectedDepartmentAchieved : value;
            const progress = calculateProgressValue(progressValue, progressTarget, isInverse);
            const displayProgress = Math.min(200, Math.round(progress * 100) / 100);
            progressScopeLabel.textContent = isDepartmentScope
                ? "Tiến độ phòng ban sau check-in"
                : "Tiến độ cá nhân sau check-in";
            progressPercentage.textContent = `${displayProgress}%`;
            liveProgressBar.style.width = `${Math.min(100, displayProgress)}%`;
            liveProgressBar.className = displayProgress < 50
                ? "progress-bar bg-danger transition-all"
                : displayProgress < 90
                    ? "progress-bar bg-warning transition-all"
                    : "progress-bar bg-success transition-all";
        }

        kpiSelect.addEventListener("change", updateStats);
        employeeSelect.addEventListener("change", function () {
            filterKpisByEmployee();
            updateStats();
        });
        achievedInput.addEventListener("input", updateStats);

        filterKpisByEmployee();
        updateStats();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeCheckInForm, { once: true });
    } else {
        initializeCheckInForm();
    }
})();
