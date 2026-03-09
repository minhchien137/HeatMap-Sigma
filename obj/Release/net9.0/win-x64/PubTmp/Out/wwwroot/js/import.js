// Global variables
let currentMode = 1;
let confirmCallback = null;
let projectMode = 1; // 1 = một dự án cho tất cả, 2 = dự án riêng từng ngày
let dayDataState = {};

// ============================================================
// SEARCHABLE SELECT COMPONENT
// ============================================================

// Mở/đóng dropdown
function toggleSearchableSelect(sdId) {
    const sd = document.getElementById(sdId);
    const trigger = sd.querySelector('.searchable-select-trigger');
    const dropdown = sd.querySelector('.searchable-select-dropdown');
    const searchInput = sd.querySelector('.searchable-select-search');
    const isOpen = dropdown.classList.contains('open');
    
    // Đóng tất cả dropdown khác trước
    document.querySelectorAll('.searchable-select-dropdown.open').forEach(d => {
        d.classList.remove('open');
        d.closest('.searchable-select').querySelector('.searchable-select-trigger').classList.remove('open');
    });
    
    if (!isOpen) {
        dropdown.classList.add('open');
        trigger.classList.add('open');
        // Focus vào ô search
        setTimeout(() => searchInput && searchInput.focus(), 50);
    }
}

// Lọc options theo từ khóa tìm kiếm
function filterSearchableSelect(sdId, keyword) {
    const sd = document.getElementById(sdId);
    const items = sd.querySelectorAll('.searchable-select-item');
    const emptyMsg = sd.querySelector('.searchable-select-empty');
    const kw = keyword.toLowerCase().trim();
    let visibleCount = 0;
    
    items.forEach(item => {
        const text = item.textContent.toLowerCase();
        if (kw === '' || text.includes(kw)) {
            item.classList.remove('hidden-item');
            visibleCount++;
        } else {
            item.classList.add('hidden-item');
        }
    });
    
    if (emptyMsg) emptyMsg.remove();
    if (visibleCount === 0 && kw !== '') {
        const empty = document.createElement('div');
        empty.className = 'searchable-select-empty';
        empty.textContent = 'Không tìm thấy nhân viên';
        sd.querySelector('.searchable-select-options').appendChild(empty);
    }
}

// Chọn một item
function selectSearchableItem(sdId, value, label) {
    const sd = document.getElementById(sdId);
    const targetSelectId = sd.getAttribute('data-target');
    const display = sd.querySelector('.searchable-select-display');
    const dropdown = sd.querySelector('.searchable-select-dropdown');
    const trigger = sd.querySelector('.searchable-select-trigger');
    const searchInput = sd.querySelector('.searchable-select-search');
    
    // Cập nhật hiển thị
    display.textContent = label;
    display.classList.add('selected');
    
    // Sync với hidden <select>
    const hiddenSelect = document.getElementById(targetSelectId);
    hiddenSelect.value = value;
    // Cập nhật selectedOptions text (cho code dùng .selectedOptions[0].text)
    for (let opt of hiddenSelect.options) {
        if (opt.value === String(value)) {
            opt.selected = true;
            break;
        }
    }
    
    // Highlight item đang chọn
    sd.querySelectorAll('.searchable-select-item').forEach(i => i.classList.remove('active'));
    sd.querySelectorAll('.searchable-select-item').forEach(i => {
        if (i.getAttribute('data-value') === String(value)) i.classList.add('active');
    });
    
    // Đóng dropdown
    dropdown.classList.remove('open');
    trigger.classList.remove('open');
    if (searchInput) searchInput.value = '';
    filterSearchableSelect(sdId, '');
}

// Reset searchable select về trạng thái ban đầu
function resetSearchableSelect(sdId, placeholder) {
    const sd = document.getElementById(sdId);
    if (!sd) return;
    const display = sd.querySelector('.searchable-select-display');
    const optionsContainer = sd.querySelector('.searchable-select-options');
    const searchInput = sd.querySelector('.searchable-select-search');
    
    display.textContent = placeholder;
    display.classList.remove('selected');
    optionsContainer.innerHTML = `<div class="searchable-select-placeholder">${placeholder}</div>`;
    if (searchInput) searchInput.value = '';
}

// Populate options vào searchable select
function populateSearchableSelect(sdId, employees, placeholder) {
    const sd = document.getElementById(sdId);
    if (!sd) return;
    const optionsContainer = sd.querySelector('.searchable-select-options');
    const display = sd.querySelector('.searchable-select-display');
    
    display.textContent = placeholder;
    display.classList.remove('selected');
    optionsContainer.innerHTML = '';
    
    employees.forEach(emp => {
        const fullName = `${emp.first_name} ${emp.last_name}`.trim() || emp.nickname || emp.emp_code;
        const item = document.createElement('div');
        item.className = 'searchable-select-item';
        item.setAttribute('data-value', emp.id);
        item.textContent = fullName;
        item.onclick = () => selectSearchableItem(sdId, emp.id, fullName);
        optionsContainer.appendChild(item);
    });
}

// Đóng dropdown khi click ra ngoài
document.addEventListener('click', function(e) {
    if (!e.target.closest('.searchable-select')) {
        document.querySelectorAll('.searchable-select-dropdown.open').forEach(d => {
            d.classList.remove('open');
            d.closest('.searchable-select').querySelector('.searchable-select-trigger').classList.remove('open');
        });
    }
});

// ============================================================

// Switch between modes
function switchMode(mode) {
    currentMode = mode;
    
    // Update buttons
    document.querySelectorAll('.mode-btn').forEach(btn => btn.classList.remove('active'));
    document.getElementById(`modeBtn${mode}`).classList.add('active');
    
    // Update content
    document.querySelectorAll('.mode-content').forEach(content => content.classList.add('hidden'));
    document.getElementById(`mode${mode}`).classList.remove('hidden');
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    generateWeeks();
    generateHoursAndMinutes();
    setupDepartmentChangeListeners();
    initMode1ProjectRows(); // khởi tạo 1 row mặc định cho Mode 1
});

// Generate weeks for all modes
function generateWeeks() {
    const weekSelects = ['week1', 'week2', 'week3'];
    weekSelects.forEach(selectId => {
        const weekSelect = document.getElementById(selectId);
        if (!weekSelect) return;
        
        const currentYear = new Date().getFullYear();
        const currentWeek = getWeekNumber(new Date());
        
        while (weekSelect.options.length > 1) {
            weekSelect.remove(1);
        }
        
        for (let week = 1; week <= 52; week++) {
            const jan1 = new Date(currentYear, 0, 1);
            const daysOffset = (week - 1) * 7;
            const weekDate = new Date(jan1.setDate(jan1.getDate() + daysOffset));
            const monday = getMonday(weekDate);
            const sunday = new Date(monday);
            sunday.setDate(monday.getDate() + 6);
            
            const weekText = `Tuần ${week} (${formatDate(monday)} - ${formatDate(sunday)})`;
            const weekValue = `${week}|${formatDate(monday)}|${formatDate(sunday)}`;
            
            const option = document.createElement('option');
            option.value = weekValue;
            option.textContent = weekText;
            if (week === currentWeek) option.selected = true;
            
            weekSelect.appendChild(option);
        }
    });
}

// Generate hours and minutes for Mode 1 only
function generateHoursAndMinutes() {
    const modes = [1]; // Chỉ Mode 1, Mode 2 có dropdown riêng cho mỗi ngày
    modes.forEach(mode => {
        const hourSelect = document.getElementById(`hour${mode}`);
        const minuteSelect = document.getElementById(`minute${mode}`);
        
        if (hourSelect) {
            for (let h = 0; h <= 23; h++) {
                const option = document.createElement('option');
                option.value = h;
                option.textContent = String(h).padStart(2, '0');
                hourSelect.appendChild(option);
            }
        }
        
        if (minuteSelect) {
            for (let m = 0; m <= 59; m++) {
                const option = document.createElement('option');
                option.value = m;
                option.textContent = String(m).padStart(2, '0');
                minuteSelect.appendChild(option);
            }
        }
        
        // Add change listeners for decimal calculation
        if (hourSelect && minuteSelect) {
            const calculateDecimal = () => {
                if (hourSelect.value !== '' && minuteSelect.value !== '') {
                    const hours = parseInt(hourSelect.value);
                    const minutes = parseInt(minuteSelect.value);
                    const decimal = hours + (minutes / 60);
                    document.getElementById(`hourDecimal${mode}`).value = decimal.toFixed(2);
                    document.getElementById(`hourDisplay${mode}`).textContent = `= ${decimal.toFixed(2)} giờ`;
                }
            };
            hourSelect.addEventListener('change', calculateDecimal);
            minuteSelect.addEventListener('change', calculateDecimal);
        }
    });
}

// Setup department change listeners
function setupDepartmentChangeListeners() {
    // Mode 1
    document.getElementById('department1')?.addEventListener('change', function() {
        loadEmployees(this.value, 'employee1');
    });
    
    // Mode 2
    document.getElementById('department2')?.addEventListener('change', function() {
        loadEmployees(this.value, 'employee2');
    });
    
    // Mode 3
    document.getElementById('department3')?.addEventListener('change', function() {
        loadEmployeesAsCheckboxes(this.value);
    });
    
    // Week change listeners for Mode 2 and 3
    const week2Select = document.getElementById('week2');
    if (week2Select) {
        week2Select.addEventListener('change', function() {
            generateDayCheckboxes(this.value, 'dayCheckboxes2');
        });
        // Trigger change event nếu đã có tuần được chọn
        if (week2Select.value) {
            week2Select.dispatchEvent(new Event('change'));
        }
    }
    
    const week3Select = document.getElementById('week3');
    if (week3Select) {
        week3Select.addEventListener('change', function() {
            generateDayCheckboxes(this.value, 'dayCheckboxes3');
        });
        // Trigger change event nếu đã có tuần được chọn
        if (week3Select.value) {
            week3Select.dispatchEvent(new Event('change'));
        }
    }
}

// Filter project dropdown theo customer đã chọn
function filterProjectsByCustomer(customerName, projectSelectId) {
    const select = document.getElementById(projectSelectId);
    if (!select) return;
    
    if (!customerName) {
        // Chưa chọn customer → disable và reset
        select.innerHTML = '<option value="">-- Chọn customer trước --</option>';
        select.disabled = true;
        select.classList.add('select-disabled');
        return;
    }
    
    // Đã chọn customer → enable và load projects
    select.disabled = false;
    select.classList.remove('select-disabled');
    
    const currentVal = select.value;
    select.innerHTML = '<option value="">-- Chọn dự án --</option>';
    
    const filtered = window.projectsData.filter(p => p.NameCustomer === customerName);
    
    filtered.forEach(p => {
        const option = document.createElement('option');
        option.value = p.IdProject;
        option.textContent = p.NameProject;
        if (p.IdProject == currentVal) option.selected = true;
        select.appendChild(option);
    });
}

// Lấy tên customer từ select id
function getCustomerName(selectId) {
    const sel = document.getElementById(selectId);
    if (!sel || !sel.value) return '';
    return sel.selectedOptions[0]?.text || '';
}
function loadEmployees(departmentId, targetSelectId) {
    const employeeSelect = document.getElementById(targetSelectId);
    // sdId = searchable dropdown wrapper id (employee1-sd, employee2-sd)
    const sdId = targetSelectId + '-sd';
    const sd = document.getElementById(sdId);
    
    // Reset hidden select
    employeeSelect.innerHTML = '<option value="">Đang tải...</option>';
    
    // Reset custom dropdown hiển thị "Đang tải..."
    if (sd) {
        const display = sd.querySelector('.searchable-select-display');
        const optionsContainer = sd.querySelector('.searchable-select-options');
        if (display) { display.textContent = 'Đang tải...'; display.classList.remove('selected'); }
        if (optionsContainer) optionsContainer.innerHTML = '<div class="searchable-select-placeholder">Đang tải...</div>';
    }
    
    if (!departmentId) {
        employeeSelect.innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
        if (sd) resetSearchableSelect(sdId, '-- Chọn bộ phận trước --');
        return;
    }
    
    fetch(`/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
    .then(response => response.json())
    .then(employees => {
        // Populate hidden select (giữ cho code submit hoạt động)
        employeeSelect.innerHTML = '<option value="">-- Chọn nhân viên --</option>';
        employees.forEach(emp => {
            const fullName = `${emp.first_name} ${emp.last_name}`.trim() || emp.nickname || emp.emp_code;
            const option = document.createElement('option');
            option.value = emp.id;
            option.textContent = fullName;
            employeeSelect.appendChild(option);
        });
        
        // Populate custom searchable dropdown
        if (sd) populateSearchableSelect(sdId, employees, '-- Chọn nhân viên --');
    })
    .catch(error => {
        console.error('Error loading employees:', error);
        employeeSelect.innerHTML = '<option value="">Lỗi khi tải danh sách</option>';
        if (sd) resetSearchableSelect(sdId, 'Lỗi khi tải danh sách');
    });
}

// Load employees as checkboxes (Mode 3)
function loadEmployeesAsCheckboxes(departmentId) {
    const container = document.getElementById('employeeCheckboxes3');
    container.innerHTML = '<p class="text-gray-400 text-center py-4">Đang tải...</p>';
    
    if (!departmentId) {
        container.innerHTML = '<p class="text-gray-400 text-center py-4">Vui lòng chọn bộ phận</p>';
        return;
    }
    
    fetch(`/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
    .then(response => response.json())
    .then(employees => {
        container.innerHTML = '';
        if (employees.length === 0) {
            container.innerHTML = '<p class="text-gray-400 text-center py-4">Không có nhân viên</p>';
            return;
        }
        
        employees.forEach(emp => {
            const fullName = `${emp.first_name} ${emp.last_name}`.trim() || emp.nickname || emp.emp_code;
            const div = document.createElement('div');
            div.className = 'employee-checkbox';
            div.innerHTML = `
                    <input type="checkbox" 
                           id="emp${emp.id}" 
                           value="${emp.id}" 
                           data-name="${fullName}"
                           onchange="handleEmployeeCheckboxChange(this)">
                    <label for="emp${emp.id}" class="cursor-pointer select-none">${fullName}</label>
                `;
            container.appendChild(div);
        });
    })
    .catch(error => {
        console.error('Error loading employees:', error);
        container.innerHTML = '<p class="text-red-400 text-center py-4">Lỗi khi tải danh sách</p>';
    });
}

// Handle employee checkbox change (Mode 3)
function handleEmployeeCheckboxChange(checkbox) {
    const parent = checkbox.closest('.employee-checkbox');
    if (checkbox.checked) {
        parent.classList.add('selected');
    } else {
        parent.classList.remove('selected');
    }
    updateSelectedEmployeesDisplay();
}

// Update selected employees display (Mode 3)
function updateSelectedEmployeesDisplay() {
    const selectedCheckboxes = document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]:checked');
    const selectedCount = selectedCheckboxes.length;
    
    // Could be used for displaying count or other UI updates
    console.log(`${selectedCount} nhân viên được chọn`);
}

// Generate day checkboxes based on selected week
function generateDayCheckboxes(weekValue, containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';
    
    if (!weekValue) {
        container.innerHTML = '<p class="text-gray-400 text-center py-4">Vui lòng chọn tuần</p>';
        return;
    }
    
    const [weekNum, startDateStr, endDateStr] = weekValue.split('|');
    const startDate = new Date(startDateStr.split('/').reverse().join('-'));
    const daysOfWeek = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật'];
    
    for (let i = 0; i < 7; i++) {
        const currentDate = new Date(startDate);
        currentDate.setDate(startDate.getDate() + i);
        
        const dateStr = formatDate(currentDate);
        const dayName = daysOfWeek[i];
        
        const div = document.createElement('div');
        div.className = 'day-checkbox';
        div.innerHTML = `
            <input type="checkbox" 
                   id="day${containerId}_${i}" 
                   value="${dateStr}" 
                   data-day="${dayName}"
                   data-label="${dayName}, ${dateStr}"
                   onchange="handleDayCheckboxChange(this, '${containerId}')">
            <label for="day${containerId}_${i}" class="cursor-pointer select-none flex-1">
                ${dayName} - ${dateStr}
            </label>
        `;
        container.appendChild(div);
    }
}

// Handle day checkbox change
function handleDayCheckboxChange(checkbox, containerId) {
    const parent = checkbox.closest('.day-checkbox');
    if (checkbox.checked) {
        parent.classList.add('selected');
    } else {
        parent.classList.remove('selected');
    }
    
    // For Mode 2, update the day hours list
    if (containerId === 'dayCheckboxes2') {
        updateDayHoursList();
    }
}

// ============================================================
// MODE 1 - Multi project rows
// ============================================================
function addMode1ProjectRow(savedData) {
    const container = document.getElementById('projectRows1');
    addProjectRow(container, savedData || null);
}

function initMode1ProjectRows() {
    const container = document.getElementById('projectRows1');
    if (!container) return;
    container.innerHTML = '';
    addProjectRow(container, null);
}

// ============================================================
// MODE 2 - Multi project rows per day
// ============================================================

// Update day hours list for Mode 2 — mỗi ngày có multi-project rows
// Lưu data hiện tại trong DOM vào dayDataState trước khi re-render
function saveAllDayBlocks() {
    document.querySelectorAll('#dayHoursList2 .bulk-block').forEach(block => {
        const dateStr = block.dataset.date;
        if (!dateStr) return;
        const rows = [];
        block.querySelectorAll('.bulk-project-row').forEach(row => {
            rows.push({
                customer:     row.querySelector('.bulk-customer')?.value || '',
                project:      row.querySelector('.bulk-project')?.value || '',
                projectPhase: row.querySelector('.bulk-pp')?.value || '',
                phase:        row.querySelector('.bulk-phase')?.value || '',
                hours:        row.querySelector('.bulk-hours-input')?.value || ''
            });
        });
        if (rows.length > 0) {
            dayDataState[dateStr] = { rows };
        }
    });
}

function updateDayHoursList() {
    // Lưu data đang nhập trước khi re-render
    saveAllDayBlocks();
    renderDayHoursList();
}

// Chỉ render, KHÔNG save — dùng khi dayDataState đã được set sẵn
function renderDayHoursList() {
    const dayHoursSection = document.getElementById('dayHoursSection2');
    const container = document.getElementById('dayHoursList2');
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    
    if (selectedDays.length === 0) {
        dayHoursSection.style.display = 'none';
        container.innerHTML = '<p class="text-gray-400 text-center py-4">Chưa chọn ngày nào</p>';
        return;
    }
    dayHoursSection.style.display = 'block';
    container.innerHTML = '';
    
    selectedDays.forEach(dayCheckbox => {
        const dateStr = dayCheckbox.value;
        const dayName = dayCheckbox.dataset.day;
        
        // Wrapper block cho 1 ngày
        const block = document.createElement('div');
        block.className = 'bulk-block';
        block.dataset.date = dateStr;
        
        // Header ngày
        const header = document.createElement('div');
        header.className = 'bulk-block-header';
        header.innerHTML = `<span class="bulk-block-day">📅 ${dayName}, ${dateStr}</span>`;
        block.appendChild(header);
        
        // Column labels
        const labels = document.createElement('div');
        labels.className = 'bulk-row-label';
        labels.innerHTML = `<span>Customer</span><span>Project</span><span>Proj. Phase</span><span>Phase</span><span>Số giờ</span><span></span>`;
        block.appendChild(labels);
        
        // Rows container
        const rowsContainer = document.createElement('div');
        rowsContainer.className = 'bulk-rows-container';
        block.appendChild(rowsContainer);
        
        // Add button
        const addBtn = document.createElement('button');
        addBtn.type = 'button';
        addBtn.className = 'bulk-add-btn';
        addBtn.textContent = '+ Thêm dự án';
        addBtn.onclick = () => addProjectRow(rowsContainer, null);
        block.appendChild(addBtn);
        
        // Restore saved rows hoặc tạo 1 row mặc định
        const savedRows = dayDataState[dateStr]?.rows || [{ customer: '', project: '', hours: '' }];
        savedRows.forEach(r => addProjectRow(rowsContainer, r));
        
        container.appendChild(block);
    });
}

// Update decimal for a specific day
function updateDayDecimal(dateStr) {
    const dayRows = document.querySelectorAll('#dayHoursList2 > div');
    let targetRow;
    
    dayRows.forEach(row => {
        const hourSelect = row.querySelector('.day-hour');
        if (hourSelect && hourSelect.dataset.date === dateStr) {
            targetRow = row;
        }
    });
    
    if (!targetRow) return;
    
    const hourSelect = targetRow.querySelector('.day-hour');
    const minuteSelect = targetRow.querySelector('.day-minute');
    const decimalDisplay = targetRow.querySelector('.day-decimal');
    
    if (hourSelect.value !== '' && minuteSelect.value !== '') {
        const hours = parseInt(hourSelect.value);
        const minutes = parseInt(minuteSelect.value);
        const decimal = (hours + (minutes / 60)).toFixed(2);
        
        decimalDisplay.textContent = decimal + ' giờ';
        decimalDisplay.dataset.value = decimal;
        
        // Save to state
        saveDayData(dateStr, null, hours, minutes);
    } else {
        decimalDisplay.textContent = '';
        decimalDisplay.dataset.value = '';
    }
}

// Save day data to state
function saveDayData(dateStr, project, hours, minutes) {
    if (!dayDataState[dateStr]) {
        dayDataState[dateStr] = {};
    }
    
    if (project !== null) dayDataState[dateStr].project = project;
    if (hours !== null) dayDataState[dateStr].hours = hours;
    if (minutes !== null) dayDataState[dateStr].minutes = minutes;
    
    if (dayDataState[dateStr].hours !== undefined && dayDataState[dateStr].minutes !== undefined) {
        const decimal = (parseInt(dayDataState[dateStr].hours) + (parseInt(dayDataState[dateStr].minutes) / 60)).toFixed(2);
        dayDataState[dateStr].decimal = decimal;
    }
}

// Lưu data của tất cả nhân viên (key = empId)
let bulkAllData = {};
let bulkActiveEmpId = null;

// Show bulk input popup (Mode 3)
function showBulkInputPopup() {
    const selectedEmployees = Array.from(document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]:checked'));
    if (selectedEmployees.length === 0) { showErrorModal('Vui lòng chọn ít nhất 1 nhân viên'); return; }
    
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]:checked'));
    if (selectedDays.length === 0) { showErrorModal('Vui lòng chọn ít nhất 1 ngày'); return; }
    
    // Merge vào data cũ thay vì reset — giữ lại data đã nhập trước đó
    const newBulkAllData = {};
    selectedEmployees.forEach(empCb => {
        const empId = empCb.value;
        newBulkAllData[empId] = {
            name: empCb.dataset.name,
            days: {}
        };
        selectedDays.forEach(dayCb => {
            const date = dayCb.value;
            // Nếu đã có data cũ → giữ nguyên, chưa có → tạo mới
            newBulkAllData[empId].days[date] =
            bulkAllData[empId]?.days[date] ||
            { label: dayCb.dataset.label, rows: [{ customer: '', project: '', hours: '' }] };
        });
    });
    bulkAllData = newBulkAllData;
    
    // Render tabs nhân viên
    const tabsContainer = document.getElementById('bulkEmpTabs');
    tabsContainer.innerHTML = '';
    selectedEmployees.forEach((empCb, idx) => {
        const tab = document.createElement('button');
        tab.type = 'button';
        tab.className = 'bulk-emp-tab' + (idx === 0 ? ' active' : '');
        tab.textContent = empCb.dataset.name;
        tab.dataset.empId = empCb.value;
        tab.onclick = () => switchBulkEmp(empCb.value);
        tabsContainer.appendChild(tab);
    });
    
    const total = selectedEmployees.length * selectedDays.length;
    document.getElementById('bulkSummary').textContent =
    `${selectedEmployees.length} người × ${selectedDays.length} ngày = ${total} block`;
    
    bulkActiveEmpId = selectedEmployees[0].value;
    renderBulkBlocks(bulkActiveEmpId);
    document.getElementById('bulkInputPopup').style.display = 'flex';
}

// Lưu data DOM hiện tại vào bulkAllData trước khi switch tab
function saveBulkCurrentData() {
    if (!bulkActiveEmpId) return;
    document.querySelectorAll('#bulkInputBlocks .bulk-block').forEach(block => {
        const date = block.dataset.date;
        const rows = [];
        block.querySelectorAll('.bulk-project-row').forEach(row => {
            rows.push({
                customer:     row.querySelector('.bulk-customer').value,
                project:      row.querySelector('.bulk-project').value,
                projectPhase: row.querySelector('.bulk-pp')?.value || '',
                phase:        row.querySelector('.bulk-phase')?.value || '',
                hours:        row.querySelector('.bulk-hours-input').value
            });
        });
        if (bulkAllData[bulkActiveEmpId]?.days[date]) {
            bulkAllData[bulkActiveEmpId].days[date].rows = rows;
        }
    });
}

// Switch tab nhân viên
function switchBulkEmp(empId) {
    saveBulkCurrentData();
    bulkActiveEmpId = empId;
    document.querySelectorAll('.bulk-emp-tab').forEach(tab => {
        tab.classList.toggle('active', tab.dataset.empId === empId);
    });
    renderBulkBlocks(empId);
}

// Render tất cả blocks của 1 nhân viên
function renderBulkBlocks(empId) {
    const container = document.getElementById('bulkInputBlocks');
    container.innerHTML = '';
    const empData = bulkAllData[empId];
    if (!empData) return;
    Object.entries(empData.days).forEach(([date, dayData]) => {
        container.appendChild(createBulkBlock(empId, empData.name, date, dayData.label, dayData.rows));
    });
}

// Tạo block 1 nhân viên × 1 ngày với rows data có sẵn
function createBulkBlock(empId, empName, date, dateLabel, savedRows) {
    const block = document.createElement('div');
    block.className = 'bulk-block';
    block.dataset.empId = empId;
    block.dataset.date = date;
    
    const header = document.createElement('div');
    header.className = 'bulk-block-header';
    header.innerHTML = `<span class="bulk-block-day">📅 ${dateLabel}</span>`;
    block.appendChild(header);
    
    const labels = document.createElement('div');
    labels.className = 'bulk-row-label';
    labels.innerHTML = `<span>Customer</span><span>Project</span><span>Proj.Phase</span><span>Phase</span><span>Số giờ</span><span></span>`;
    block.appendChild(labels);
    
    const rowsContainer = document.createElement('div');
    rowsContainer.className = 'bulk-rows-container';
    block.appendChild(rowsContainer);
    
    const addBtn = document.createElement('button');
    addBtn.type = 'button';
    addBtn.className = 'bulk-add-btn';
    addBtn.textContent = '+ Thêm dự án';
    addBtn.onclick = () => addProjectRow(rowsContainer, null);
    block.appendChild(addBtn);
    
    (savedRows || [{ customer: '', project: '', hours: '' }]).forEach(r => addProjectRow(rowsContainer, r));
    return block;
}

// Thêm 1 dòng project, restore saved data nếu có
function addProjectRow(rowsContainer, savedData) {
    const row = document.createElement('div');
    row.className = 'bulk-project-row';
    
    // Customer
    const customerOpts = Array.from(new Set(window.projectsData.map(p => p.NameCustomer).filter(Boolean)))
    .map(name => `<option value="${name}"${savedData?.customer === name ? ' selected' : ''}>${name}</option>`).join('');
    const customerSelect = document.createElement('select');
    customerSelect.className = 'bulk-select bulk-customer';
    customerSelect.innerHTML = `<option value="">-- Customer --</option>${customerOpts}`;
    customerSelect.onchange = function() { onBulkCustomerChange(this); };
    
    // Project
    const projectSelect = document.createElement('select');
    projectSelect.className = 'bulk-select bulk-project select-disabled';
    projectSelect.disabled = true;
    projectSelect.innerHTML = '<option value="">-- Customer trước --</option>';
    if (savedData?.customer) {
        const filtered = window.projectsData.filter(p => p.NameCustomer === savedData.customer);
        projectSelect.innerHTML = '<option value="">-- Project --</option>' +
        filtered.map(p => `<option value="${p.IdProject}"${String(p.IdProject) === String(savedData.project) ? ' selected' : ''}>${p.NameProject}</option>`).join('');
        projectSelect.disabled = false;
        projectSelect.classList.remove('select-disabled');
    }
    
    // Project Phase
    const ppOpts = (window.projectPhasesData || [])
    .map(pp => `<option value="${pp}"${savedData?.projectPhase === pp ? ' selected' : ''}>${pp}</option>`).join('');
    const ppSelect = document.createElement('select');
    ppSelect.className = 'bulk-select bulk-pp';
    ppSelect.innerHTML = `<option value="">-- Proj. Phase --</option>${ppOpts}`;
    
    // Phase
    const phaseOpts = (window.projectPhasesData || [])
    .map(p => `<option value="${p}"${savedData?.phase === p ? ' selected' : ''}>${p}</option>`).join('');
    const phaseSelect = document.createElement('select');
    phaseSelect.className = 'bulk-select bulk-phase';
    phaseSelect.innerHTML = `<option value="">-- Phase --</option>${phaseOpts}`;
    
    // Hours
    const hoursInput = document.createElement('input');
    hoursInput.type = 'number';
    hoursInput.className = 'bulk-hours-input';
    hoursInput.placeholder = 'VD: 4.5';
    hoursInput.min = '0.5'; hoursInput.max = '24'; hoursInput.step = '0.5';
    if (savedData?.hours) hoursInput.value = savedData.hours;
    
    // Delete
    const deleteBtn = document.createElement('button');
    deleteBtn.type = 'button';
    deleteBtn.className = 'bulk-delete-btn';
    deleteBtn.innerHTML = '✕';
    deleteBtn.onclick = function() {
        if (rowsContainer.querySelectorAll('.bulk-project-row').length <= 1) {
            showErrorModal('Mỗi ngày phải có ít nhất 1 dự án'); return;
        }
        row.remove();
    };
    
    row.appendChild(customerSelect);
    row.appendChild(projectSelect);
    row.appendChild(ppSelect);
    row.appendChild(phaseSelect);
    row.appendChild(hoursInput);
    row.appendChild(deleteBtn);
    rowsContainer.appendChild(row);
}

// Khi đổi customer trong 1 dòng → filter project tương ứng
function onBulkCustomerChange(customerSelect) {
    const row = customerSelect.closest('.bulk-project-row');
    const projectSelect = row.querySelector('.bulk-project');
    const customerName = customerSelect.value;
    
    if (!customerName) {
        projectSelect.innerHTML = '<option value="">-- Chọn customer trước --</option>';
        projectSelect.disabled = true;
        projectSelect.classList.add('select-disabled');
        return;
    }
    
    const filtered = window.projectsData.filter(p => p.NameCustomer === customerName);
    projectSelect.innerHTML = '<option value="">-- Chọn dự án --</option>' +
    filtered.map(p => `<option value="${p.IdProject}">${p.NameProject}</option>`).join('');
    projectSelect.disabled = false;
    projectSelect.classList.remove('select-disabled');
}

// Close bulk input popup
function closeBulkInputPopup() {
    saveBulkCurrentData(); // Lưu data đang nhập dở trước khi đóng
    document.getElementById('bulkInputPopup').style.display = 'none';
}

// Helper functions
function getWeekNumber(date) {
    const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    const dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d - yearStart) / 86400000) + 1) / 7);
}

function getMonday(date) {
    const d = new Date(date);
    const day = d.getDay();
    const diff = d.getDate() - day + (day === 0 ? -6 : 1);
    return new Date(d.setDate(diff));
}

function formatDate(date) {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}/${month}/${year}`;
}

// Submit handlers
function handleSubmitMode1() {
    const department = document.getElementById('department1').value;
    const employee   = document.getElementById('employee1').value;
    const day        = document.getElementById('day1').value;
    
    if (!department) { showErrorModal('Vui lòng chọn bộ phận'); return; }
    if (!employee)   { showErrorModal('Vui lòng chọn nhân viên'); return; }
    if (!day)        { showErrorModal('Vui lòng chọn ngày'); return; }
    
    // Thu thập multi-project rows
    const container = document.getElementById('projectRows1');
    const projectRows = [];
    let rowError = '';
    container.querySelectorAll('.bulk-project-row').forEach((row, idx) => {
        const customer     = row.querySelector('.bulk-customer').value;
        const project      = row.querySelector('.bulk-project').value;
        const projectPhase = row.querySelector('.bulk-pp').value;
        const phase        = row.querySelector('.bulk-phase').value;
        const hours        = parseFloat(row.querySelector('.bulk-hours-input').value);
        if (!customer || !project || !projectPhase || !phase || !hours || hours <= 0) {
            rowError = `Vui lòng nhập đầy đủ tất cả thông tin (dòng ${idx + 1})`;
        }
        projectRows.push({
            Customer:     customer,
            ProjectId:    parseInt(project),
            ProjectPhase: projectPhase,
            Phase:        phase,
            WorkHours:    hours
        });
    });
    
    if (rowError) { showErrorModal(rowError); return; }
    if (projectRows.length === 0) { showErrorModal('Vui lòng thêm ít nhất 1 dự án'); return; }
    
    const totalHours = projectRows.reduce((s, r) => s + r.WorkHours, 0);
    if (totalHours > 24) { showErrorModal('Tổng giờ trong ngày vượt quá 24h'); return; }
    
    const empName = document.getElementById('employee1').selectedOptions[0]?.text || '';
    showConfirmModal(
        `Xác nhận lưu ${projectRows.length} dự án cho ${empName}?`,
        function() {
            const payload = {
                EmployeeId: parseInt(employee),
                WorkDate:   day,
                Projects:   projectRows
            };
            fetch('/Heatmap/SaveStaffDetailMulti', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    showSuccessModal(`✓ Đã lưu ${projectRows.length} dự án thành công!`);
                    document.getElementById('department1').selectedIndex = 0;
                    document.getElementById('employee1').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                    resetSearchableSelect('employee1-sd', '-- Chọn bộ phận trước --');
                    document.getElementById('day1').value = '';
                    initMode1ProjectRows();
                } else {
                    showErrorModal('Lỗi: ' + (result.message || 'Không thể lưu dữ liệu'));
                }
            })
            .catch(err => showErrorModal('Lỗi kết nối: ' + err.message));
        }
    );
}

function handleSubmitMode2() {
    const employee     = document.getElementById('employee2').value;
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    
    if (!employee)                  { showErrorModal('Vui lòng chọn nhân viên'); return; }
    if (selectedDays.length === 0)  { showErrorModal('Vui lòng chọn ít nhất 1 ngày'); return; }
    
    // Thu thập dữ liệu từ các block ngày
    const days = [];
    let rowError = '';
    
    document.querySelectorAll('#dayHoursList2 .bulk-block').forEach(block => {
        const dateStr = block.dataset.date;
        const rows = [];
        block.querySelectorAll('.bulk-project-row').forEach((row, idx) => {
            const customer     = row.querySelector('.bulk-customer').value;
            const project      = row.querySelector('.bulk-project').value;
            const projectPhase = row.querySelector('.bulk-pp').value;
            const phase        = row.querySelector('.bulk-phase').value;
            const hours        = parseFloat(row.querySelector('.bulk-hours-input').value);
            if (!customer || !project || !projectPhase || !phase || !hours || hours <= 0) {
                rowError = `Vui lòng nhập đầy đủ tất cả thông tin (${dateStr} - dòng ${idx + 1})`;
            }
            rows.push({
                Customer:     customer,
                ProjectId:    parseInt(project),
                ProjectPhase: projectPhase,
                Phase:        phase,
                WorkHours:    hours
            });
        });
        const totalHours = rows.reduce((s, r) => s + (r.WorkHours || 0), 0);
        if (totalHours > 24) rowError = `Tổng giờ ngày ${dateStr} vượt quá 24h`;
        const dateParts = dateStr.split('/');
        days.push({ Date: `${dateParts[2]}-${dateParts[1]}-${dateParts[0]}`, Projects: rows });
    });
    
    if (rowError) { showErrorModal(rowError); return; }
    
    const empName = document.getElementById('employee2').selectedOptions[0]?.text || '';
    showConfirmModal(
        `Xác nhận lưu dữ liệu cho ${empName} - ${selectedDays.length} ngày?`,
        function() {
            const payload = {
                EmployeeId: parseInt(employee),
                Days:       days
            };
            fetch('/Heatmap/SaveMultipleDaysMulti', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    showSuccessModal(`✓ Lưu thành công ${selectedDays.length} ngày!`);
                    // Reset: giữ bộ phận/tuần, xóa ngày tick và dayDataState
                    const dept2 = document.getElementById('department2').value;
                    if (dept2) loadEmployees(dept2, 'employee2');
                    else {
                        document.getElementById('employee2').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                        resetSearchableSelect('employee2-sd', '-- Chọn bộ phận trước --');
                    }
                    const week2Select = document.getElementById('week2');
                    if (week2Select?.value) week2Select.dispatchEvent(new Event('change'));
                    document.getElementById('dayHoursSection2').style.display = 'none';
                    dayDataState = {};
                } else {
                    showErrorModal(result.message || 'Có lỗi xảy ra khi lưu dữ liệu');
                }
            })
            .catch(err => showErrorModal('Lỗi kết nối: ' + err.message));
        }
    );
}

function handleSubmitMode3() {
    // Lưu data của tab đang active trước
    saveBulkCurrentData();
    
    const records = [];
    let hasError = false;
    let errorMsg = '';
    
    Object.entries(bulkAllData).forEach(([empId, empData]) => {
        Object.entries(empData.days).forEach(([date, dayData]) => {
            dayData.rows.forEach((r, idx) => {
                if (!r.customer || !r.project || !r.projectPhase || !r.phase || !r.hours || parseFloat(r.hours) <= 0) {
                    if (!hasError) {  // chỉ giữ lỗi đầu tiên gặp
                        hasError = true;
                        const missing = [];
                        if (!r.customer)     missing.push('Customer');
                        if (!r.project)      missing.push('Project');
                        if (!r.projectPhase) missing.push('Proj. Phase');
                        if (!r.phase)        missing.push('Phase');
                        if (!r.hours || parseFloat(r.hours) <= 0) missing.push('Số giờ');
                        errorMsg = `Thiếu: ${missing.join(', ')}\n(${empData.name} - ${dayData.label} - dòng ${idx + 1})`;
                    }
                    return; // bỏ qua push row lỗi này
                }
                
                // Parse date từ dd/MM/yyyy → yyyy-MM-dd cho server
                const parts = date.split('/');
                const isoDate = parts.length === 3 ? `${parts[2]}-${parts[1]}-${parts[0]}` : date;
                
                records.push({
                    EmpId:        parseInt(empId),
                    Date:         isoDate,
                    Customer:     r.customer,
                    ProjectId:    parseInt(r.project),
                    ProjectPhase: r.projectPhase,
                    Phase:        r.phase,
                    Hours:        parseFloat(r.hours)
                });
            });
        });
    });
    
    if (hasError) { showErrorModal(errorMsg); return; }
    
    // Kiểm tra tổng giờ/ngày/người không quá 24h
    const hoursCheck = {};
    records.forEach(r => {
        const key = `${r.EmpId}_${r.Date}`;
        hoursCheck[key] = (hoursCheck[key] || 0) + r.Hours;
    });
    const overload = Object.entries(hoursCheck).find(([, h]) => h > 24);
    if (overload) { showErrorModal('Tổng giờ trong 1 ngày vượt quá 24h. Vui lòng kiểm tra lại.'); return; }
    
    showConfirmModal(`Bạn sắp tạo ${records.length} bản ghi. Xác nhận lưu?`, async function() {
        try {
            const response = await fetch('/Heatmap/BulkImportMultiProject', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(records)
            });
            const result = await response.json();
            if (result.success) {
                closeBulkInputPopup();
                showSuccessModal(`✅ Đã lưu thành công ${result.total} bản ghi!`);
                // Reset Mode 3
                bulkAllData = {};
                bulkActiveEmpId = null;
                document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]').forEach(cb => {
                    cb.checked = false;
                    cb.closest('.employee-checkbox')?.classList.remove('selected');
                });
                document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]').forEach(cb => {
                    cb.checked = false;
                    cb.closest('.day-checkbox')?.classList.remove('selected');
                });
            } else {
                showErrorModal('Lỗi: ' + (result.message || 'Không thể lưu dữ liệu'));
            }
        } catch (err) {
            showErrorModal('Lỗi kết nối: ' + err.message);
        }
    });
}

// Modal functions
function showErrorModal(message) {
    document.getElementById('errorModalMessage').textContent = message;
    document.getElementById('errorModal').style.display = 'flex';
}

function closeErrorModal() {
    document.getElementById('errorModal').style.display = 'none';
}

// Thêm hàm mới để hiển thị thông báo thành công
function showSuccessModal(message) {
    // Sử dụng modal error nhưng với nội dung thành công
    document.getElementById('errorModalMessage').textContent = message;
    document.getElementById('errorModal').style.display = 'flex';
}

function showConfirmModal(message, callback) {
    confirmCallback = callback;
    document.getElementById('confirmMessage').textContent = message;
    document.getElementById('confirmModal').style.display = 'flex';
}

function closeConfirmModal() {
    document.getElementById('confirmModal').style.display = 'none';
    confirmCallback = null;
}

function confirmSubmit() {
    if (confirmCallback) {
        confirmCallback();
    }
    closeConfirmModal();
}

// Copy ngày đầu cho tất cả - Mode 3 (trong popup)
function copyFirstDayToAllMode3() {
    // Lưu data tab hiện tại trước
    saveBulkCurrentData();
    
    const empId = bulkActiveEmpId;
    const empData = bulkAllData[empId];
    if (!empData) { showErrorModal('Không có dữ liệu nhân viên'); return; }
    
    const dates = Object.keys(empData.days);
    if (dates.length <= 1) { showErrorModal('Cần có ít nhất 2 ngày để copy'); return; }
    
    // Lấy rows của ngày đầu tiên
    const firstDate = dates[0];
    const firstRows = empData.days[firstDate].rows;
    const hasData = firstRows.some(r => r.customer && r.project && r.hours);
    if (!hasData) { showErrorModal('Vui lòng nhập ít nhất 1 dự án cho ngày đầu tiên'); return; }
    
    // Copy sang tất cả ngày còn lại của nhân viên đang chọn
    dates.forEach(date => {
        if (date !== firstDate) {
            empData.days[date].rows = firstRows.map(r => ({ ...r }));
        }
    });
    
    // Re-render blocks
    renderBulkBlocks(empId);
    showSuccessModal(`✓ Đã copy ${firstRows.length} dự án từ ngày đầu cho ${dates.length - 1} ngày còn lại!`);
}

// Copy first day data to all other days
function copyFirstDayDataToAll() {
    const firstBlock = document.querySelector('#dayHoursList2 .bulk-block');
    if (!firstBlock) { showErrorModal('Không có ngày nào được chọn'); return; }
    
    // Đọc rows của ngày đầu
    const firstRows = [];
    firstBlock.querySelectorAll('.bulk-project-row').forEach(row => {
        firstRows.push({
            customer:     row.querySelector('.bulk-customer').value,
            project:      row.querySelector('.bulk-project').value,
            projectPhase: row.querySelector('.bulk-pp').value,
            phase:        row.querySelector('.bulk-phase').value,
            hours:        row.querySelector('.bulk-hours-input').value
        });
    });
    
    const hasData = firstRows.some(r => r.customer && r.project && r.hours);
    if (!hasData) { showErrorModal('Vui lòng nhập ít nhất 1 dự án cho ngày đầu tiên'); return; }
    
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    if (selectedDays.length <= 1) { showErrorModal('Cần chọn ít nhất 2 ngày để copy'); return; }
    
    const firstDate = firstBlock.dataset.date;
    
    // Bước 1: Lưu DOM hiện tại vào state (để không mất data ngày đầu)
    saveAllDayBlocks();
    
    // Bước 2: Ghi đè data cho tất cả ngày còn lại
    selectedDays.forEach(cb => {
        if (cb.value !== firstDate) {
            dayDataState[cb.value] = { rows: firstRows.map(r => ({ ...r })) };
        }
    });
    
    // Bước 3: Render lại KHÔNG gọi saveAllDayBlocks (dùng flag)
    renderDayHoursList();
    showSuccessModal(`✓ Đã copy ${firstRows.length} dự án từ ngày đầu cho ${selectedDays.length - 1} ngày còn lại!`);
}