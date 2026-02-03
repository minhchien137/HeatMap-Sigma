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

// Load employees for dropdown
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
                    <input type="checkbox" value="${emp.id}" data-name="${fullName}" id="emp_${emp.id}">
                    <label for="emp_${emp.id}" class="cursor-pointer flex-1">${fullName}</label>
                `;
                div.addEventListener('click', function(e) {
                    if (e.target.tagName !== 'INPUT') {
                        const checkbox = this.querySelector('input[type="checkbox"]');
                        checkbox.checked = !checkbox.checked;
                    }
                    this.classList.toggle('selected', this.querySelector('input').checked);
                });
                container.appendChild(div);
            });
        })
        .catch(error => {
            console.error('Error loading employees:', error);
            container.innerHTML = '<p class="text-red-500 text-center py-4">Lỗi khi tải danh sách</p>';
        });
}

// Generate day checkboxes for selected week
function generateDayCheckboxes(weekValue, containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';

    if (!weekValue) return;

    const [week, startDate, endDate] = weekValue.split('|');
    const [startDay, startMonth, startYear] = startDate.split('/').map(Number);
    
    const daysOfWeek = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
    const startDateObj = new Date(startYear, startMonth - 1, startDay);

    for (let i = 0; i < 7; i++) {
        const currentDate = new Date(startDateObj);
        currentDate.setDate(startDateObj.getDate() + i);
        
        const dateStr = `${currentDate.getDate().toString().padStart(2, '0')}/${(currentDate.getMonth() + 1).toString().padStart(2, '0')}/${currentDate.getFullYear()}`;
        const dateValue = `${currentDate.getFullYear()}-${(currentDate.getMonth() + 1).toString().padStart(2, '0')}-${currentDate.getDate().toString().padStart(2, '0')}`;
        
        const div = document.createElement('div');
        div.className = 'day-checkbox';
        div.innerHTML = `
            <input type="checkbox" value="${dateValue}" data-label="${daysOfWeek[i]} ${currentDate.getDate()}/${currentDate.getMonth() + 1}" id="day_${containerId}_${i}">
            <label for="day_${containerId}_${i}" class="cursor-pointer text-sm">
                <div class="font-bold">${daysOfWeek[i]}</div>
                <div class="text-xs text-gray-500">${currentDate.getDate()}/${currentDate.getMonth() + 1}</div>
            </label>
        `;
        div.addEventListener('click', function(e) {
            if (e.target.tagName !== 'INPUT') {
                const checkbox = this.querySelector('input[type="checkbox"]');
                checkbox.checked = !checkbox.checked;
            }
            this.classList.toggle('selected', this.querySelector('input').checked);
            
            // Update hours list for Mode 2
            if (containerId === 'dayCheckboxes2') {
                updateDayHoursList();
            }
        });
        container.appendChild(div);
    }
}

// Update hours list for Mode 2 when days are selected
function updateDayHoursList() {
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    const dayHoursSection = document.getElementById('dayHoursSection2');
    const dayHoursList = document.getElementById('dayHoursList2');

    if (selectedDays.length === 0) {
        dayHoursSection.style.display = 'none';
        dayHoursList.innerHTML = '';
        return;
    }

    dayHoursSection.style.display = 'block';
    dayHoursList.innerHTML = '';

    let projectOptions = '<option value="">-- Chọn dự án --</option>';
    if (window.projectsData) {
        window.projectsData.forEach(p => {
            projectOptions += `<option value="${p.IdProject}">${p.NameProject}</option>`;
        });
    }

    selectedDays.forEach(dayCheckbox => {
        const dateValue = dayCheckbox.value;
        const dateLabel = dayCheckbox.dataset.label;

        const row = document.createElement('div');
        row.className = 'flex items-center gap-4 bg-white p-4 rounded-xl border-2 border-gray-100';

        row.innerHTML = `
            <div class="flex-1 font-semibold text-gray-700">${dateLabel}</div>
            <div class="flex gap-3 flex-[3]">
                ${projectMode === 2 ? `
                <div class="flex flex-col flex-1">
                    <select class="input-field py-2 text-sm day-project" data-date="${dateValue}">
                        ${projectOptions}
                    </select>
                    <div class="day-project-label text-xs mt-1 text-red-500 font-semibold"></div>
                </div>` : ``}
                <select class="input-field py-2 text-sm day-hour" data-date="${dateValue}">
                    <option value="">Giờ</option>
                    ${Array.from({length:24},(_,i)=>`<option value="${i}">${String(i).padStart(2,'0')}</option>`).join('')}
                </select>
                <select class="input-field py-2 text-sm day-minute" data-date="${dateValue}">
                    <option value="">Phút</option>
                    ${Array.from({length:60},(_,i)=>`<option value="${i}">${String(i).padStart(2,'0')}</option>`).join('')}
                </select>
                <div class="flex items-center px-3 text-sm font-mono text-gray-600 day-decimal">-</div>
            </div>
        `;

        dayHoursList.appendChild(row);

        const hour = row.querySelector('.day-hour');
        const minute = row.querySelector('.day-minute');
        const decimal = row.querySelector('.day-decimal');
        const project = row.querySelector('.day-project');
        const label = row.querySelector('.day-project-label');

        // 🔥 RESTORE DATA
        if (dayDataState[dateValue]) {
            const d = dayDataState[dateValue];

            if (project && d.project) {
                project.value = d.project;
                label.textContent = project.options[project.selectedIndex].text;
            }

            if (d.hour) hour.value = d.hour;
            if (d.minute) minute.value = d.minute;
            if (d.decimal) {
                decimal.textContent = `= ${d.decimal}`;
                decimal.dataset.value = d.decimal;
            }
        }

        // SAVE PROJECT
        if (project) {
            project.addEventListener('change', function () {
                if (!dayDataState[dateValue]) dayDataState[dateValue] = {};
                dayDataState[dateValue].project = this.value;
                label.textContent = this.options[this.selectedIndex].text;
            });
        }

        // SAVE HOURS
        const updateDecimal = () => {
            if (hour.value && minute.value) {
                const dec = (parseInt(hour.value) + parseInt(minute.value)/60).toFixed(2);
                decimal.textContent = `= ${dec}`;
                decimal.dataset.value = dec;

                if (!dayDataState[dateValue]) dayDataState[dateValue] = {};
                dayDataState[dateValue].hour = hour.value;
                dayDataState[dateValue].minute = minute.value;
                dayDataState[dateValue].decimal = dec;
            }
        };

        hour.addEventListener('change', updateDecimal);
        minute.addEventListener('change', updateDecimal);
    });
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
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

// Mode 3: Open bulk input popup
function openBulkInputPopup() {
    const selectedEmployees = Array.from(document.querySelectorAll('#employeeCheckboxes3 input[type="checkbox"]:checked'));
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes3 input[type="checkbox"]:checked'));

    if (selectedEmployees.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 nhân viên');
        return;
    }

    if (selectedDays.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 ngày');
        return;
    }

    // Generate table rows - sử dụng biến global projectsData
    const tableBody = document.getElementById('bulkInputTableBody');
    tableBody.innerHTML = '';

    selectedEmployees.forEach(empCheckbox => {
        const empId = empCheckbox.value;
        const empName = empCheckbox.dataset.name;

        // Build project options từ biến global
        let projectOptions = '<option value="">-- Chọn dự án --</option>';
        if (window.projectsData) {
            window.projectsData.forEach(project => {
                projectOptions += `<option value="${project.IdProject}">${project.NameProject}</option>`;
            });
        }

        const tr = document.createElement('tr');
        tr.className = 'border-b border-gray-100';
        tr.innerHTML = `
            <td class="px-4 py-3 font-medium">${empName}</td>
            <td class="px-4 py-3">
                <select class="input-field py-2 text-sm bulk-project" data-emp-id="${empId}">
                    ${projectOptions}
                </select>
            </td>
            <td class="px-4 py-3">
                <select class="input-field py-2 text-sm bulk-hour" data-emp-id="${empId}">
                    <option value="">Giờ</option>
                    ${Array.from({length: 24}, (_, i) => `<option value="${i}">${String(i).padStart(2, '0')}</option>`).join('')}
                </select>
            </td>
            <td class="px-4 py-3">
                <select class="input-field py-2 text-sm bulk-minute" data-emp-id="${empId}">
                    <option value="">Phút</option>
                    ${Array.from({length: 60}, (_, i) => `<option value="${i}">${String(i).padStart(2, '0')}</option>`).join('')}
                </select>
            </td>
            <td class="px-4 py-3 text-sm font-mono text-gray-600 bulk-decimal" data-emp-id="${empId}">-</td>
        `;
        tableBody.appendChild(tr);

        // Add change listeners for decimal calculation
        const hourSelect = tr.querySelector('.bulk-hour');
        const minuteSelect = tr.querySelector('.bulk-minute');
        const decimalCell = tr.querySelector('.bulk-decimal');

        const updateDecimal = () => {
        if (hourSelect.value !== '' && minuteSelect.value !== '') {
            const hours = parseInt(hourSelect.value);
            const minutes = parseInt(minuteSelect.value);
            const decimal = (hours + minutes / 60).toFixed(2);

            decimalDisplay.textContent = `= ${decimal}`;
            decimalDisplay.dataset.value = decimal;

            if (!dayDataState[dateValue]) dayDataState[dateValue] = {};
            dayDataState[dateValue].hour = hourSelect.value;
            dayDataState[dateValue].minute = minuteSelect.value;
            dayDataState[dateValue].decimal = decimal;
        }
    };

        hourSelect.addEventListener('change', updateDecimal);
        minuteSelect.addEventListener('change', updateDecimal);
    });

    document.getElementById('bulkInputPopup').classList.remove('hidden');
    document.getElementById('bulkInputPopup').style.display = 'flex';
}

function closeBulkInputPopup() {
    document.getElementById('bulkInputPopup').classList.add('hidden');
    document.getElementById('bulkInputPopup').style.display = 'none';
}

function copyFirstDayDataToAll() {
    const firstRow = document.querySelector('#dayHoursList2 .day-hour');
    if (!firstRow) return;

    const firstDate = firstRow.dataset.date;
    const firstState = dayDataState[firstDate];

    // Chỉ cần ngày đầu có dữ liệu
    if (
        !firstState ||
        !firstState.hour ||
        !firstState.minute ||
        (projectMode === 2 && !firstState.project)
    ) {
        showErrorModal('Vui lòng nhập đầy đủ Dự án / Giờ / Phút cho ngày đầu tiên');
        return;
    }

    // Lấy tất cả ngày đang tick
    const selectedDays = Array.from(
        document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked')
    ).map(cb => cb.value);

    // Copy state cho tất cả ngày
    selectedDays.forEach(date => {
        if (date !== firstDate) {
            dayDataState[date] = { ...firstState };
        }
    });

    updateDayHoursList();
}



// Submit handlers
function handleSubmitMode1() {
    // Validate input
    const department = document.getElementById('department1').value;
    const employee = document.getElementById('employee1').value;
    const project = document.getElementById('project1').value;
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

    const message = `Xác nhận lưu dữ liệu?\n\nNhân viên: ${employeeName}\nDự án: ${projectName}\nNgày: ${formattedDate}\nGiờ: ${hourDecimal} giờ`;
    
    showConfirmModal(message, function() {
        // Prepare data
        const data = {
            EmployeeId: parseInt(employee),
            ProjectId: parseInt(project),
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
                alert('✓ Lưu dữ liệu thành công!\n\n' +
                      `Nhân viên: ${result.data.employee}\n` +
                      `Bộ phận: ${result.data.department}\n` +
                      `Dự án: ${result.data.project}\n` +
                      `Ngày: ${result.data.workDate}\n` +
                      `Giờ: ${result.data.workHours} giờ`);
                
                // Reset form
                document.getElementById('department1').selectedIndex = 0;
                document.getElementById('employee1').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                document.getElementById('project1').selectedIndex = 0;
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
    const selectedDays = Array.from(document.querySelectorAll('#dayCheckboxes2 input[type="checkbox"]:checked'));
    
    if (!employee) {
        showErrorModal('Vui lòng chọn nhân viên');
        return;
    }
    
    if (selectedDays.length === 0) {
        showErrorModal('Vui lòng chọn ít nhất 1 ngày');
        return;
    }
    
    // Kiểm tra project theo mode
    if (projectMode === 1) {
        // Mode 1: Kiểm tra common project
        const commonProject = document.getElementById('commonProject').value;
        if (!commonProject) {
            showErrorModal('Vui lòng chọn dự án');
            return;
        }
    } else {
        // Mode 2: Kiểm tra từng ngày có project chưa
        const dayProjects = Array.from(document.querySelectorAll('#dayHoursList2 .day-project'));
        let hasInvalidProject = false;
        
        dayProjects.forEach(projectSelect => {
            if (!projectSelect.value) {
                hasInvalidProject = true;
            }
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
        if (!decimalDisplay.dataset.value) {
            hasInvalidHours = true;
        }
    });
    
    if (hasInvalidHours) {
        showErrorModal('Vui lòng nhập giờ cho tất cả các ngày đã chọn');
        return;
    }
    
    const message = `Bạn sắp lưu dữ liệu cho ${selectedDays.length} ngày. Xác nhận?`;
    showConfirmModal(message, function() {
        let projectInfo = '';
        if (projectMode === 1) {
            const commonProject = document.getElementById('commonProject');
            projectInfo = `- Dự án: ${commonProject.selectedOptions[0].text} (chung cho tất cả)`;
        } else {
            projectInfo = '- Dự án: Riêng cho từng ngày';
        }
        
        alert('Chức năng đang phát triển - Mode 2\n\nDữ liệu sẽ được lưu:\n' + 
              `- Nhân viên: ${document.getElementById('employee2').selectedOptions[0].text}\n` +
              projectInfo + '\n' +
              `- Số ngày: ${selectedDays.length}`);
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
