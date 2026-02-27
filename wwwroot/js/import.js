// Global variables
let currentMode = 1;
let confirmCallback = null;
let projectMode = 1; // 1 = một dự án cho tất cả, 2 = dự án riêng từng ngày
let dayDataState = {};

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

// Switch between project modes in Mode 2
function switchProjectMode(mode) {
    projectMode = mode;
    
    // Update buttons
    document.querySelectorAll('.project-mode-btn').forEach(btn => btn.classList.remove('active'));
    document.getElementById(`projectModeBtn${mode}`).classList.add('active');
    
    // Show/hide common project section
    const commonProjectSection = document.getElementById('commonProjectSection');
    if (mode === 1) {
        commonProjectSection.style.display = 'block';
    } else {
        commonProjectSection.style.display = 'none';
    }
    
    // Re-generate day hours list if days are already selected
    updateDayHoursList();
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    generateWeeks();
    generateHoursAndMinutes();
    setupDepartmentChangeListeners();
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
    
    // Customer change → filter project dropdown
    document.getElementById('customer1')?.addEventListener('change', function() {
        filterProjectsByCustomer(this.value, 'project1');
    });
    
    document.getElementById('customer2')?.addEventListener('change', function() {
        // Filter commonProject dropdown
        filterProjectsByCustomer(this.value, 'commonProject');
        // Re-render day hours list để cập nhật dropdown dự án riêng từng ngày
        updateDayHoursList();
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
    
    const currentVal = select.value;
    select.innerHTML = '<option value="">-- Chọn dự án --</option>';
    
    const filtered = customerName
    ? window.projectsData.filter(p => p.NameCustomer === customerName)
    : window.projectsData;
    
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
    employeeSelect.innerHTML = '<option value="">Đang tải...</option>';
    
    if (!departmentId) {
        employeeSelect.innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
        return;
    }
    
    fetch(`/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
    .then(response => response.json())
    .then(employees => {
        employeeSelect.innerHTML = '<option value="">-- Chọn nhân viên --</option>';
        employees.forEach(emp => {
            const fullName = `${emp.first_name} ${emp.last_name}`.trim() || emp.nickname || emp.emp_code;
            const option = document.createElement('option');
            option.value = emp.id;
            option.textContent = fullName;
            employeeSelect.appendChild(option);
        });
    })
    .catch(error => {
        console.error('Error loading employees:', error);
        employeeSelect.innerHTML = '<option value="">Lỗi khi tải danh sách</option>';
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

// Update day hours list for Mode 2
function updateDayHoursList() {
    const dayHoursSection = document.getElementById('dayHoursSection2');
    const container = document.getElementById('dayHoursList2');
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    
    // Show/hide section
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
        
        const dayRow = document.createElement('div');
        dayRow.className = 'flex items-center gap-4 p-4 bg-gray-50 rounded-xl border-2 border-gray-100';
        
        // Preserve existing data if switching modes
        const existingData = dayDataState[dateStr] || { hours: '', minutes: '', project: '' };
        
        let projectDropdown = '';
        if (projectMode === 2) {
            // Mode 2: Individual project per day - filter by customer2
            const customerName = getCustomerName('customer2');
            const projectOptions = (customerName
                ? window.projectsData.filter(p => p.NameCustomer === customerName)
                : window.projectsData
            ).map(p =>
                `<option value="${p.IdProject}" ${p.IdProject == existingData.project ? 'selected' : ''}>${p.NameProject}</option>`
            ).join('');
            
            projectDropdown = `
                <select class="day-project input-field flex-1" data-date="${dateStr}" onchange="saveDayData('${dateStr}', this.value, null, null)">
                    <option value="">-- Chọn dự án --</option>
                    ${projectOptions}
                </select>
            `;
        }
        
        dayRow.innerHTML = `
            <div class="flex-1">
                <p class="font-bold text-gray-900">${dayName}</p>
                <p class="text-sm text-gray-500">${dateStr}</p>
            </div>
            ${projectDropdown}
            <div class="flex gap-2">
                <select class="day-hour input-field" style="width: 80px;" data-date="${dateStr}" onchange="updateDayDecimal('${dateStr}')">
                    <option value="">Giờ</option>
                    ${Array.from({length: 24}, (_, h) => 
        `<option value="${h}" ${h == existingData.hours ? 'selected' : ''}>${String(h).padStart(2, '0')}</option>`
    ).join('')}
                </select>
                <select class="day-minute input-field" style="width: 80px;" data-date="${dateStr}" onchange="updateDayDecimal('${dateStr}')">
                    <option value="">Phút</option>
                    ${Array.from({length: 60}, (_, m) => 
`<option value="${m}" ${m == existingData.minutes ? 'selected' : ''}>${String(m).padStart(2, '0')}</option>`
).join('')}
                </select>
            </div>
            <div class="day-decimal text-sm text-gray-500 font-bold" data-value="${existingData.decimal || ''}">
                ${existingData.decimal ? existingData.decimal + ' giờ' : ''}
            </div>
        `;

container.appendChild(dayRow);

// Restore decimal if exists
if (existingData.decimal) {
    updateDayDecimal(dateStr);
}
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

// Show bulk input popup (Mode 3)
function showBulkInputPopup() {
    const selectedEmployees = Array.from(document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]:checked'));
    
    if (selectedEmployees.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 nhân viên');
        return;
    }
    
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]:checked'));
    if (selectedDays.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 ngày');
        return;
    }
    
    const tbody = document.getElementById('bulkInputTableBody');
    tbody.innerHTML = '';
    
    selectedEmployees.forEach(empCheckbox => {
        const empId = empCheckbox.value;
        const empName = empCheckbox.dataset.name;
        
        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="px-6 py-4 border-b border-gray-100">${empName}</td>
            <td class="px-6 py-4 border-b border-gray-100">
                <select class="bulk-project input-field" data-emp="${empId}">
                    <option value="">-- Chọn dự án --</option>
                    ${window.projectsData.map(p => 
        `<option value="${p.IdProject}">${p.NameProject}</option>`
    ).join('')}
                </select>
            </td>
            <td class="px-6 py-4 border-b border-gray-100">
                <div class="flex gap-2">
                    <select class="bulk-hour input-field" style="width: 80px;" data-emp="${empId}" onchange="updateBulkDecimal('${empId}')">
                        <option value="">Giờ</option>
                        ${Array.from({length: 24}, (_, h) => 
    `<option value="${h}">${String(h).padStart(2, '0')}</option>`
).join('')}
                    </select>
                    <select class="bulk-minute input-field" style="width: 80px;" data-emp="${empId}" onchange="updateBulkDecimal('${empId}')">
                        <option value="">Phút</option>
                        ${Array.from({length: 60}, (_, m) => 
`<option value="${m}">${String(m).padStart(2, '0')}</option>`
).join('')}
                    </select>
                </div>
            </td>
            <td class="px-6 py-4 border-b border-gray-100">
                <div class="bulk-decimal text-sm text-gray-500 font-bold" data-value=""></div>
            </td>
        `;
tbody.appendChild(row);
});

document.getElementById('bulkInputPopup').style.display = 'flex';
}

// Update bulk decimal for Mode 3
function updateBulkDecimal(empId) {
    const rows = document.querySelectorAll('#bulkInputTableBody tr');
    
    rows.forEach(row => {
        const hourSelect = row.querySelector(`.bulk-hour[data-emp="${empId}"]`);
        if (!hourSelect) return;
        
        const minuteSelect = row.querySelector(`.bulk-minute[data-emp="${empId}"]`);
        const decimalDisplay = row.querySelector('.bulk-decimal');
        
        if (hourSelect.value !== '' && minuteSelect.value !== '') {
            const hours = parseInt(hourSelect.value);
            const minutes = parseInt(minuteSelect.value);
            const decimal = (hours + (minutes / 60)).toFixed(2);
            
            decimalDisplay.textContent = decimal + ' giờ';
            decimalDisplay.dataset.value = decimal;
        } else {
            decimalDisplay.textContent = '';
            decimalDisplay.dataset.value = '';
        }
    });
}

// Close bulk input popup
function closeBulkInputPopup() {
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
    // Validate input
    const department = document.getElementById('department1').value;
    const employee = document.getElementById('employee1').value;
    const project = document.getElementById('project1').value;
    const projectPhase = document.getElementById('projectPhase1').value;
    const phase = document.getElementById('phase1').value;
    const day = document.getElementById('day1').value;
    const hourDecimal = document.getElementById('hourDecimal1').value;
    
    if (!department) {
        showErrorModal('Vui lòng chọn bộ phận');
        return;
    }
    
    if (!employee) {
        showErrorModal('Vui lòng chọn nhân viên');
        return;
    }
    
    if (!project) {
        showErrorModal('Vui lòng chọn dự án');
        return;
    }
    
    if (!projectPhase) {
        showErrorModal('Vui lòng chọn Project Phase');
        return;
    }
    
    if (!phase) {
        showErrorModal('Vui lòng chọn Phase');
        return;
    }
    
    if (!day) {
        showErrorModal('Vui lòng chọn ngày');
        return;
    }
    
    if (!hourDecimal || parseFloat(hourDecimal) <= 0) {
        showErrorModal('Vui lòng nhập giờ làm việc hợp lệ');
        return;
    }
    
    // Get selected text for confirmation
    const employeeName = document.getElementById('employee1').selectedOptions[0].text;
    const projectName = document.getElementById('project1').selectedOptions[0].text;
    const formattedDate = new Date(day).toLocaleDateString('vi-VN');
    
    const message = `Xác nhận lưu dữ liệu?\n\nNhân viên: ${employeeName}\nDự án: ${projectName}\nProject Phase: ${projectPhase}\nPhase: ${phase}\nNgày: ${formattedDate}\nGiờ: ${hourDecimal} giờ`;
    
    showConfirmModal(message, function() {
        // Prepare data
        const data = {
            EmployeeId: parseInt(employee),
            ProjectId: parseInt(project),
            Customer: getCustomerName('customer1'),
            ProjectPhase: projectPhase,
            Phase: phase,
            WorkDate: day,
            WorkHours: parseFloat(hourDecimal)
        };
        
        // Send to server
        fetch('/Heatmap/SaveStaffDetail', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data)
        })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                showSuccessModal(
                    '✓ Lưu dữ liệu thành công!\n\n' +
                    `Nhân viên: ${result.data.employee}\n` +
                    `Bộ phận: ${result.data.department}\n` +
                    `Dự án: ${result.data.project}\n` +
                    `Ngày: ${result.data.workDate}\n` +
                    `Giờ: ${result.data.workHours} giờ`
                );
                
                // Reset form
                document.getElementById('department1').selectedIndex = 0;
                document.getElementById('employee1').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                document.getElementById('project1').selectedIndex = 0;
                document.getElementById('projectPhase1').selectedIndex = 0;
                document.getElementById('phase1').selectedIndex = 0;
                document.getElementById('day1').value = '';
                document.getElementById('hour1').selectedIndex = 0;
                document.getElementById('minute1').selectedIndex = 0;
                document.getElementById('hourDecimal1').value = '';
                document.getElementById('hourDisplay1').textContent = '';
            } else {
                showErrorModal(result.message || 'Có lỗi xảy ra khi lưu dữ liệu');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showErrorModal('Lỗi kết nối đến server: ' + error.message);
        });
    });
}

function handleSubmitMode2() {
    const employee = document.getElementById('employee2').value;
    const projectPhase = document.getElementById('projectPhase2').value;
    const phase = document.getElementById('phase2').value;
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    
    if (!employee) {
        showErrorModal('Vui lòng chọn nhân viên');
        return;
    }
    
    if (!projectPhase) {
        showErrorModal('Vui lòng chọn Project Phase');
        return;
    }
    
    if (!phase) {
        showErrorModal('Vui lòng chọn Phase');
        return;
    }
    
    if (selectedDays.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 ngày');
        return;
    }
    
    // Kiểm tra project theo mode
    if (projectMode === 1) {
        const commonProject = document.getElementById('commonProject').value;
        if (!commonProject) {
            showErrorModal('Vui lòng chọn dự án');
            return;
        }
    } else {
        const dayProjects = Array.from(document.querySelectorAll('#dayHoursList2 .day-project'));
        let hasInvalidProject = false;
        dayProjects.forEach(projectSelect => {
            if (!projectSelect.value) hasInvalidProject = true;
        });
        if (hasInvalidProject) {
            showErrorModal('Vui lòng chọn dự án cho tất cả các ngày');
            return;
        }
    }
    
    // Kiểm tra tất cả các ngày đã nhập giờ chưa
    const dayHours = Array.from(document.querySelectorAll('#dayHoursList2 .day-decimal'));
    let hasInvalidHours = false;
    dayHours.forEach(decimalDisplay => {
        if (!decimalDisplay.dataset.value) hasInvalidHours = true;
    });
    if (hasInvalidHours) {
        showErrorModal('Vui lòng nhập giờ cho tất cả các ngày đã chọn');
        return;
    }
    
    const message = `Bạn sắp lưu dữ liệu cho ${selectedDays.length} ngày. Xác nhận?`;
    showConfirmModal(message, function() {
        // Chuẩn bị dữ liệu để gửi
        const days = [];
        selectedDays.forEach(dayCheckbox => {
            const dateStr = dayCheckbox.value;
            const dayState = dayDataState[dateStr];
            if (!dayState || !dayState.decimal) return;
            
            const dateParts = dateStr.split('/');
            const formattedDate = `${dateParts[2]}-${dateParts[1]}-${dateParts[0]}`;
            days.push({
                Date: formattedDate,
                WorkHours: parseFloat(dayState.decimal),
                ProjectId: projectMode === 2 ? parseInt(dayState.project) : null
            });
        });
        
        const requestData = {
            EmployeeId: parseInt(employee),
            ProjectMode: projectMode,
            CommonProjectId: projectMode === 1 ? parseInt(document.getElementById('commonProject').value) : null,
            Customer: getCustomerName('customer2'),
            ProjectPhase: projectPhase,
            Phase: phase,
            Days: days
        };
        
        // Gửi dữ liệu đến server
        fetch('/Heatmap/SaveMultipleDays', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        })
        .then(response => response.json())
        .then(result => {
            if (result.success) {
                const projectText = projectMode === 1
                ? document.getElementById('commonProject').selectedOptions[0].text + ' (chung)'
                : 'Riêng từng ngày';
                
                showSuccessModal(
                    '✓ Lưu dữ liệu thành công!\n\n' +
                    `Nhân viên: ${document.getElementById('employee2').selectedOptions[0].text}\n` +
                    `Bộ phận: ${result.data.department}\n` +
                    `Dự án: ${projectText}\n` +
                    `Project Phase: ${projectPhase}\n` +
                    `Phase: ${phase}\n` +
                    `Tổng số ngày: ${result.data.totalDays}\n` +
                    `   - Mới tạo: ${result.data.savedCount}\n` +
                    `   - Cập nhật: ${result.data.updatedCount}\n\n` +
                    `Các ngày: ${result.data.dates.join(', ')}`
                );
                
                // Reset form - giữ lại: bộ phận, project phase, phase, tuần
                // Reload lại danh sách nhân viên theo bộ phận đang chọn
                const dept2 = document.getElementById('department2').value;
                if (dept2) {
                    loadEmployees(dept2, 'employee2');
                } else {
                    document.getElementById('employee2').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                }
                if (projectMode === 1) {
                    document.getElementById('commonProject').selectedIndex = 0;
                }
                // Reset customer và re-filter project list
                document.getElementById('customer2').selectedIndex = 0;
                filterProjectsByCustomer('', 'commonProject');
                // Giữ tuần và reload danh sách ngày (bỏ tick)
                const week2Select = document.getElementById('week2');
                if (week2Select && week2Select.value) {
                    week2Select.dispatchEvent(new Event('change'));
                }
                document.getElementById('dayHoursSection2').style.display = 'none';
                dayDataState = {};
            } else {
                showErrorModal(result.message || 'Có lỗi xảy ra khi lưu dữ liệu');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showErrorModal('Lỗi kết nối đến server: ' + error.message);
        });
    });
}

function handleSubmitMode3() {
    const rows = document.querySelectorAll('#bulkInputTableBody tr');
    let valid = true;
    let totalRecords = 0;
    
    rows.forEach(row => {
        const project = row.querySelector('.bulk-project').value;
        const decimal = row.querySelector('.bulk-decimal').dataset.value;
        
        if (!project || !decimal) {
            valid = false;
        }
    });
    
    if (!valid) {
        showErrorModal('Vui lòng nhập đầy đủ thông tin cho tất cả nhân viên');
        return;
    }
    
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]:checked'));
    totalRecords = rows.length * selectedDays.length;
    
    const message = `Bạn sắp tạo ${totalRecords} bản ghi (${rows.length} người × ${selectedDays.length} ngày). Xác nhận?`;
    showConfirmModal(message, function() {
        alert('Chức năng đang phát triển - Mode 3');
        closeBulkInputPopup();
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
// Copy first day data to all other days
function copyFirstDayDataToAll() {
    const firstRow = document.querySelector('#dayHoursList2 .day-hour');
    if (!firstRow) {
        showErrorModal('Không có ngày nào được chọn');
        return;
    }
    
    const firstDate = firstRow.dataset.date;
    const firstState = dayDataState[firstDate];
    
    // Validate first day data
    if (
        !firstState ||
        firstState.hours === undefined || firstState.hours === null || firstState.hours === '' ||
        firstState.minutes === undefined || firstState.minutes === null || firstState.minutes === '' ||
        (projectMode === 2 && !firstState.project)
    ) {
        showErrorModal('Vui lòng nhập đầy đủ thông tin cho ngày đầu tiên');
        return;
    }
    
    // Get all selected days
    const selectedDays = Array.from(
        document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked')
    ).map(cb => cb.value);
    
    if (selectedDays.length <= 1) {
        showErrorModal('Cần chọn ít nhất 2 ngày để copy');
        return;
    }
    
    // Copy state to all days
    selectedDays.forEach(date => {
        if (date !== firstDate) {
            dayDataState[date] = { ...firstState };
        }
    });
    
    // Refresh UI
    updateDayHoursList();
    
    showSuccessModal('Đã copy dữ liệu ngày đầu cho tất cả các ngày!');
}