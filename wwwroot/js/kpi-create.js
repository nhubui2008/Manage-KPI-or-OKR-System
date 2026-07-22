(function () {
    "use strict";

    var root = document.querySelector("[data-kpi-create]");
    if (!root) return;

    var form = root.querySelector("[data-create-form-element]");
    var employeeRows = Array.from(root.querySelectorAll("[data-employee-row]"));
    var employeeChecks = Array.from(root.querySelectorAll("[data-employee-check]"));
    var weightSummary = root.querySelector("[data-weight-summary]");
    var weightTotal = root.querySelector("[data-weight-total]");
    var weightMessage = root.querySelector("[data-weight-message]");
    var aiState = { keyResults: [], requestId: 0, suggestions: [] };

    function q(selector) { return root.querySelector(selector); }

    function refreshSelect(select) {
        if (!select || !window.jQuery || !window.jQuery.fn || !window.jQuery.fn.select2) return;
        var element = window.jQuery(select);
        if (element.hasClass("select2-hidden-accessible")) element.trigger("change.select2");
    }

    function parseNumber(value) {
        if (value === null || value === undefined || value === "") return null;
        var parsed = Number(String(value).replace(/,/g, "."));
        return Number.isFinite(parsed) ? parsed : null;
    }

    function numberOrNull(value) {
        var parsed = parseInt(value || "", 10);
        return Number.isFinite(parsed) ? parsed : null;
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function toast(options) {
        if (window.AppFeedback && typeof window.AppFeedback.toast === "function") {
            window.AppFeedback.toast(options);
        } else if (typeof window.showAppToast === "function") {
            window.showAppToast(options);
        }
    }

    function updateSelectionCounts() {
        root.querySelectorAll("[data-selection-count]").forEach(function (output) {
            var name = output.dataset.selectionCount;
            var count = root.querySelectorAll('input[name="' + name + '"]:checked').length;
            output.textContent = count + " đã chọn";
        });
    }

    function selectedWeightInputs() {
        return employeeChecks
            .filter(function (checkbox) { return checkbox.checked; })
            .map(function (checkbox) {
                return checkbox.closest("[data-employee-row]")?.querySelector("[data-weight-input]");
            })
            .filter(Boolean);
    }

    function writeEqualWeights(inputs) {
        if (!inputs.length) return;
        var equal = Math.round((100 / inputs.length) * 100) / 100;
        inputs.forEach(function (input, index) {
            var value = equal;
            if (index === inputs.length - 1) {
                value = Math.round((100 - equal * (inputs.length - 1)) * 100) / 100;
            }
            input.value = String(value).replace(/\.00$/, "");
        });
    }

    function updateWeightSummary() {
        var inputs = selectedWeightInputs();
        var total = inputs.reduce(function (sum, input) {
            return sum + (parseNumber(input.value) || 0);
        }, 0);
        total = Math.round(total * 100) / 100;
        if (weightTotal) weightTotal.textContent = total.toFixed(2).replace(/\.00$/, "") + "%";

        if (!inputs.length) {
            weightSummary?.classList.remove("is-valid", "is-invalid");
            if (weightMessage) weightMessage.textContent = "Chưa phân bổ nhân viên";
            return;
        }

        var valid = Math.abs(total - 100) <= 0.05 && inputs.every(function (input) {
            var value = parseNumber(input.value);
            return value !== null && value > 0 && value <= 100;
        });
        weightSummary?.classList.toggle("is-valid", valid);
        weightSummary?.classList.toggle("is-invalid", !valid);
        if (weightMessage) {
            weightMessage.textContent = valid
                ? "Tỷ trọng hợp lệ để gửi duyệt"
                : "Cần phân bổ đủ 100% trước khi lưu";
        }
    }

    function syncEmployeeRow(checkbox, shouldRebalance) {
        var row = checkbox.closest("[data-employee-row]");
        var weightInput = row?.querySelector("[data-weight-input]");
        if (!row || !weightInput) return;
        row.classList.toggle("is-selected", checkbox.checked);
        weightInput.disabled = !checkbox.checked;
        if (!checkbox.checked) weightInput.value = "";
        if (shouldRebalance) writeEqualWeights(selectedWeightInputs());
        updateSelectionCounts();
        updateWeightSummary();
    }

    employeeChecks.forEach(function (checkbox) {
        checkbox.addEventListener("change", function () { syncEmployeeRow(checkbox, true); });
    });
    root.querySelectorAll("[data-weight-input]").forEach(function (input) {
        input.addEventListener("input", updateWeightSummary);
    });
    updateSelectionCounts();
    updateWeightSummary();

    function filterRows(inputSelector, rowSelector) {
        var input = root.querySelector(inputSelector);
        if (!input) return;
        input.addEventListener("input", function () {
            var needle = input.value.trim().toLowerCase();
            var rows = Array.from(root.querySelectorAll(rowSelector));
            var matches = 0;
            rows.forEach(function (row) {
                var match = !needle || (row.dataset.search || "").toLowerCase().includes(needle);
                row.hidden = !match;
                if (match) matches += 1;
            });
            var empty = input.closest(".kpi-assignment-box")?.querySelector("[data-filter-empty]");
            if (empty) empty.hidden = matches !== 0 || !needle;
        });
    }

    filterRows("#employeeSearch", "[data-employee-row]");
    filterRows("#departmentSearch", ".kpi-department-row");

    function setupOkrLinks() {
        var okr = q("#okrSelect");
        var kr = q("#keyResultSelect");
        if (!okr || !kr) return;
        var source = Array.from(kr.querySelectorAll("option[data-okr-id]")).map(function (option) {
            return { value: option.value, text: option.textContent, okrId: option.dataset.okrId };
        });

        function refresh() {
            var selected = kr.value;
            var selectedOkr = okr.value;
            var matches = selectedOkr ? source.filter(function (item) { return item.okrId === selectedOkr; }) : [];
            kr.innerHTML = "";
            var placeholder = document.createElement("option");
            placeholder.value = "";
            placeholder.textContent = selectedOkr
                ? (matches.length ? "Không liên kết Key Result" : "OKR chưa có Key Result")
                : "Chọn OKR trước";
            kr.appendChild(placeholder);
            matches.forEach(function (item) {
                var option = document.createElement("option");
                option.value = item.value;
                option.textContent = item.text;
                option.dataset.okrId = item.okrId;
                kr.appendChild(option);
            });
            kr.disabled = !selectedOkr || !matches.length;
            if (matches.some(function (item) { return item.value === selected; })) kr.value = selected;
            refreshSelect(kr);
            updatePreview();
        }

        okr.addEventListener("change", refresh);
        kr.addEventListener("change", updatePreview);
        refresh();
    }

    function setupMeasurement() {
        var unit = q("#measurementUnit");
        var inverse = q("#IsInverse");
        var label = q("[data-inverse-label]");
        var direction = q("[data-preview-direction]");
        var note = q("[data-threshold-note]");
        function update() {
            var unitValue = unit?.value || "đơn vị";
            root.querySelectorAll(".measurement-unit-suffix").forEach(function (suffix) { suffix.textContent = unitValue; });
            var inverseValue = !!inverse?.checked;
            if (label) label.textContent = inverseValue ? "Càng thấp càng tốt" : "Càng cao càng tốt";
            if (direction) direction.textContent = inverseValue ? "Càng thấp càng tốt" : "Càng cao càng tốt";
            if (note) {
                note.classList.remove("is-warning", "is-success");
                note.classList.add(inverseValue ? "is-warning" : "is-success");
                note.querySelector("span").textContent = inverseValue
                    ? "KPI nghịch đảo: ngưỡng đạt và trượt phải lớn hơn hoặc bằng chỉ tiêu."
                    : "KPI thuận: ngưỡng đạt và trượt nên thấp hơn hoặc bằng chỉ tiêu. Một số ngưỡng có thể để trống.";
            }
            updatePreview();
        }
        unit?.addEventListener("change", update);
        inverse?.addEventListener("change", update);
        update();
    }

    function updatePreview() {
        var name = q("#KPIName")?.value.trim();
        var target = q("#TargetValue")?.value;
        var unit = q("#measurementUnit")?.value;
        var type = q("#KPITypeId");
        var period = q("#PeriodId");
        var okr = q("#okrSelect");
        var nameOutput = q("[data-preview-name]");
        var targetOutput = q("[data-preview-target]");
        var unitOutput = q("[data-preview-unit]");
        var typeOutput = q("[data-preview-type]");
        var periodOutput = q("[data-preview-period]");
        var linkOutput = q("[data-preview-link]");
        if (nameOutput) nameOutput.textContent = name || "Chưa đặt tên KPI";
        if (targetOutput) targetOutput.textContent = target ? target : "—";
        if (unitOutput) unitOutput.textContent = unit || "Chọn đơn vị đo";
        if (typeOutput) typeOutput.textContent = type?.selectedOptions[0]?.textContent || "Chưa chọn";
        if (periodOutput) periodOutput.textContent = period?.selectedOptions[0]?.textContent || "Chưa chọn";
        if (linkOutput) linkOutput.textContent = okr?.value ? "Có liên kết OKR" : "Độc lập";
    }

    ["#KPIName", "#TargetValue", "#KPITypeId", "#PeriodId"].forEach(function (selector) {
        q(selector)?.addEventListener("input", updatePreview);
        q(selector)?.addEventListener("change", updatePreview);
    });
    setupOkrLinks();
    setupMeasurement();
    updatePreview();

    function validateWeights(event) {
        var inputs = selectedWeightInputs();
        if (!inputs.length) return;
        var total = inputs.reduce(function (sum, input) { return sum + (parseNumber(input.value) || 0); }, 0);
        var invalid = inputs.some(function (input) {
            var value = parseNumber(input.value);
            return value === null || value <= 0 || value > 100;
        }) || Math.abs(total - 100) > 0.05;
        if (!invalid) return;
        event.preventDefault();
        updateWeightSummary();
        var message = q("[data-weight-message]");
        if (message) message.textContent = "Tổng tỷ trọng phải bằng 100% và mỗi người phải lớn hơn 0%.";
        inputs[0].focus();
    }
    form?.addEventListener("submit", validateWeights, true);

    function setSelectOptions(select, placeholder, items, selected, disabled) {
        if (!select) return;
        select.innerHTML = "";
        var first = document.createElement("option");
        first.value = "";
        first.textContent = placeholder;
        select.appendChild(first);
        (items || []).forEach(function (item) {
            var option = document.createElement("option");
            option.value = item.id;
            option.textContent = item.text;
            if (item.parentId !== undefined && item.parentId !== null) option.dataset.okrId = item.parentId;
            select.appendChild(option);
        });
        if ((items || []).some(function (item) { return String(item.id) === String(selected || ""); })) select.value = selected;
        select.disabled = !!disabled;
        refreshSelect(select);
    }

    function aiControls() {
        return {
            employee: document.getElementById("aiSuggestEmployeeId"),
            department: document.getElementById("aiSuggestDepartmentId"),
            period: document.getElementById("aiSuggestPeriodId"),
            okr: document.getElementById("aiSuggestOkrId"),
            kr: document.getElementById("aiSuggestKrId"),
            status: document.getElementById("aiSuggestOptionsStatus")
        };
    }

    function renderAiKeyResults(selected) {
        var controls = aiControls();
        if (!controls.kr) return;
        var okrId = controls.okr?.value || "";
        var matches = aiState.keyResults.filter(function (item) { return String(item.parentId) === String(okrId); });
        setSelectOptions(controls.kr, okrId ? (matches.length ? "Không liên kết Key Result" : "OKR chưa có Key Result") : "Chọn OKR trước", matches, selected, !okrId || !matches.length);
    }

    async function refreshAiOptions(source) {
        var controls = aiControls();
        if (!controls.employee || !controls.department || !controls.period || !controls.okr) return;
        if (source === "department") controls.employee.value = "";
        var previous = {
            employee: controls.employee.value,
            department: controls.department.value,
            period: controls.period.value,
            okr: controls.okr.value,
            kr: controls.kr?.value || ""
        };
        var params = new URLSearchParams();
        if (previous.employee) params.set("employeeId", previous.employee);
        if (previous.department) params.set("departmentId", previous.department);
        var requestId = ++aiState.requestId;
        if (controls.status) controls.status.textContent = "Đang lọc dữ liệu theo phân công...";
        try {
            var response = await fetch("/AI/SuggestKpiOptions" + (params.toString() ? "?" + params.toString() : ""));
            var data = await response.json();
            if (requestId !== aiState.requestId) return;
            if (!response.ok || data.success === false) throw new Error(data.warnings?.[0] || "Không thể tải dữ liệu gợi ý.");
            var employeeItems = (data.employees || []).map(function (item) { return { id: item.id, text: item.text || item.name }; });
            var departmentItems = (data.departments || []).map(function (item) { return { id: item.id, text: item.text || item.name }; });
            var periodItems = (data.periods || []).map(function (item) { return { id: item.id, text: item.text || item.name }; });
            var okrItems = (data.okrs || []).map(function (item) { return { id: item.id, text: item.text || item.name }; });
            setSelectOptions(controls.department, "Không chọn cụ thể", departmentItems, previous.department, false);
            setSelectOptions(controls.employee, "Không chọn cụ thể", employeeItems, previous.employee, false);
            setSelectOptions(controls.period, periodItems.length ? "Chọn kỳ gần nhất" : "Chưa có kỳ khả dụng", periodItems, previous.period, !periodItems.length);
            setSelectOptions(controls.okr, okrItems.length ? "Không bắt buộc" : "Chưa có OKR khả dụng", okrItems, previous.okr, !okrItems.length);
            aiState.keyResults = (data.keyResults || []).map(function (item) { return { id: item.id, text: item.text || item.name, parentId: item.parentId ?? item.okrId }; });
            renderAiKeyResults(previous.kr);
            if (controls.status) controls.status.textContent = data.warnings?.[0] || "";
        } catch (error) {
            if (requestId !== aiState.requestId) return;
            if (controls.status) controls.status.textContent = "";
            toast({ tone: "warning", eyebrow: "AI Gợi ý KPI", title: "Không thể tải dữ liệu", message: error.message });
        }
    }

    function setupAi() {
        var controls = aiControls();
        if (!controls.employee || !controls.department || !controls.okr) return;
        controls.employee.addEventListener("change", function () { refreshAiOptions("employee"); });
        controls.department.addEventListener("change", function () { refreshAiOptions("department"); });
        controls.okr.addEventListener("change", function () { renderAiKeyResults(); });
        document.getElementById("aiKpiSuggestModal")?.addEventListener("shown.bs.modal", function () { refreshAiOptions("modal"); });
        renderAiKeyResults();
        if (root.dataset.createAi === "true") {
            window.setTimeout(function () {
                var modal = document.getElementById("aiKpiSuggestModal");
                if (modal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(modal).show();
            }, 80);
        }
    }

    function fieldValue(item, names) {
        for (var i = 0; i < names.length; i++) {
            if (item[names[i]] !== undefined && item[names[i]] !== null) return item[names[i]];
        }
        return "";
    }

    function renderKpiSuggestions(suggestions) {
        var results = document.getElementById("aiKpiSuggestResults");
        var refineChat = document.getElementById("aiKpiRefineChat");
        aiState.suggestions = suggestions || [];
        if (!results) return;
        if (!aiState.suggestions.length) {
            results.innerHTML = '<p class="evaluation-form-hint">AI chưa trả về gợi ý phù hợp.</p>';
            refineChat?.classList.add("d-none");
            return;
        }
        refineChat?.classList.remove("d-none");
        results.innerHTML = aiState.suggestions.map(function (item, index) {
            var name = fieldValue(item, ["name", "Name"]) || "KPI đề xuất";
            var rationale = fieldValue(item, ["rationale", "Rationale"]);
            var target = fieldValue(item, ["targetValue", "TargetValue"]);
            var pass = fieldValue(item, ["passThreshold", "PassThreshold"]);
            var fail = fieldValue(item, ["failThreshold", "FailThreshold"]);
            var unit = fieldValue(item, ["unit", "Unit", "measurementUnit", "MeasurementUnit"]);
            return '<article class="kpi-ai-suggestion"><div class="kpi-ai-suggestion__top"><div><div class="kpi-ai-suggestion__name">' + escapeHtml(name) + '</div><div class="kpi-ai-suggestion__rationale">' + escapeHtml(rationale) + '</div></div><button type="button" class="btn btn-sm btn-outline-success" data-apply-kpi="' + index + '">Áp dụng</button></div><div class="kpi-ai-suggestion__metrics"><span>Target: ' + escapeHtml(target || "N/A") + (unit ? " " + escapeHtml(unit) : "") + '</span><span>Đạt: ' + escapeHtml(pass || "N/A") + '</span><span>Trượt: ' + escapeHtml(fail || "N/A") + '</span></div></article>';
        }).join("");
        results.querySelectorAll("[data-apply-kpi]").forEach(function (button) {
            button.addEventListener("click", function () { applyKpiSuggestion(aiState.suggestions[Number(button.dataset.applyKpi)]); });
        });
    }

    function applyKpiSuggestion(item) {
        if (!item) return;
        var name = fieldValue(item, ["name", "Name"]);
        var target = fieldValue(item, ["targetValue", "TargetValue"]);
        var pass = fieldValue(item, ["passThreshold", "PassThreshold"]);
        var fail = fieldValue(item, ["failThreshold", "FailThreshold"]);
        var unit = fieldValue(item, ["unit", "Unit", "measurementUnit", "MeasurementUnit"]);
        var fields = { "#KPIName": name, "#TargetValue": target, "#PassThreshold": pass, "#FailThreshold": fail, "#measurementUnit": unit };
        Object.keys(fields).forEach(function (selector) {
            var field = q(selector);
            if (!field) return;
            field.value = fields[selector] ?? "";
            field.dispatchEvent(new Event("input", { bubbles: true }));
            field.dispatchEvent(new Event("change", { bubbles: true }));
        });
        var controls = aiControls();
        if (controls.period?.value) q("#PeriodId").value = controls.period.value;
        if (controls.okr?.value) {
            var okr = q("#okrSelect");
            if (okr) { okr.value = controls.okr.value; okr.dispatchEvent(new Event("change", { bubbles: true })); }
        }
        window.setTimeout(function () {
            if (controls.kr?.value) {
                var keyResult = q("#keyResultSelect");
                if (keyResult) {
                    keyResult.value = controls.kr.value;
                    refreshSelect(keyResult);
                }
            }
            updatePreview();
        }, 0);
        var modal = document.getElementById("aiKpiSuggestModal");
        if (modal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(modal).hide();
        toast({ tone: "success", eyebrow: "AI Gợi ý KPI", title: "Đã áp dụng bản nháp", message: "Hãy kiểm tra lại các ngưỡng và phạm vi phân bổ trước khi tạo." });
    }

    document.getElementById("aiRunKpiSuggestBtn")?.addEventListener("click", async function () {
        var button = this;
        var results = document.getElementById("aiKpiSuggestResults");
        var original = button.innerHTML;
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang tạo...';
        if (results) results.innerHTML = '<p class="evaluation-form-hint">Đang phân tích dữ liệu KPI/OKR...</p>';
        var controls = aiControls();
        var payload = {
            employeeId: numberOrNull(controls.employee?.value),
            departmentId: numberOrNull(controls.department?.value),
            okrId: numberOrNull(controls.okr?.value),
            okrKeyResultId: numberOrNull(controls.kr?.value),
            periodId: numberOrNull(controls.period?.value)
        };
        try {
            var headers = typeof window.antiForgeryHeaders === "function" ? window.antiForgeryHeaders() : {};
            var response = await fetch("/AI/SuggestKPI", { method: "POST", headers: Object.assign({ "Content-Type": "application/json" }, headers), body: JSON.stringify(payload) });
            var data = await response.json();
            if (!response.ok || data.success === false) throw new Error(data.warnings?.[0] || "Không thể tạo gợi ý KPI.");
            renderKpiSuggestions(data.suggestions || data);
        } catch (error) {
            if (results) results.innerHTML = "";
            toast({ tone: "warning", eyebrow: "AI Gợi ý KPI", title: "AI chưa sẵn sàng", message: error.message });
        } finally {
            button.disabled = false;
            button.innerHTML = original;
        }
    });

    document.getElementById("aiKpiRefineBtn")?.addEventListener("click", async function () {
        var button = this;
        var input = document.getElementById("aiKpiRefineInput");
        var status = document.getElementById("aiKpiRefineStatus");
        var instruction = input?.value?.trim() || "";
        if (!instruction) {
            if (status) status.textContent = "Hãy nhập nội dung cần chỉnh sửa.";
            input?.focus();
            return;
        }

        var original = button.innerHTML;
        button.disabled = true;
        if (input) input.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang sửa...';
        if (status) status.textContent = "Agent đang chỉnh sửa gợi ý KPI...";
        try {
            var headers = typeof window.antiForgeryHeaders === "function" ? window.antiForgeryHeaders() : {};
            var response = await fetch("/AI/RefineKpiSuggestions", {
                method: "POST",
                headers: Object.assign({ "Content-Type": "application/json" }, headers),
                body: JSON.stringify({ instruction: instruction, suggestions: aiState.suggestions })
            });
            var data = await response.json().catch(function () { return {}; });
            if (!response.ok || data.success === false) throw new Error(data.warnings?.[0] || "Không thể chỉnh sửa gợi ý KPI.");
            renderKpiSuggestions(data.suggestions || data);
            if (input) input.value = "";
            if (status) status.textContent = "Đã cập nhật gợi ý theo yêu cầu. Bạn có thể tiếp tục yêu cầu chỉnh sửa.";
        } catch (error) {
            if (status) status.textContent = error.message || "Không thể chỉnh sửa gợi ý KPI.";
        } finally {
            button.disabled = false;
            if (input) input.disabled = false;
            button.innerHTML = original;
            input?.focus();
        }
    });
    document.getElementById("aiKpiRefineInput")?.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.isComposing) {
            event.preventDefault();
            document.getElementById("aiKpiRefineBtn")?.click();
        }
    });

    window.renderKpiSuggestions = renderKpiSuggestions;
    setupAi();
})();
