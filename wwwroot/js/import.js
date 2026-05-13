// Global variables
let currentMode = 1;
let confirmCallback = null;
let projectMode = 1; // 1 = một dự án cho tất cả, 2 = dự án riêng từng ngày
let dayDataState = {};
const pathBase = window.pathBase || '';

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
    
    // Nếu user thường (có userDepartmentId) -> auto-load nhân viên cho cả 3 mode
    if (window.userDepartmentId) {
        const deptId = String(window.userDepartmentId);
        loadEmployees(deptId, 'employee1');
        loadEmployees(deptId, 'employee2');
        loadEmployeesAsCheckboxes(deptId);
    }
});

// Generate weeks for all modes
// function generateWeeks() {
//     const weekSelects = ['week1', 'week2', 'week3'];
//     weekSelects.forEach(selectId => {
    //         const weekSelect = document.getElementById(selectId);
//         if (!weekSelect) return;

//         const currentYear = new Date().getFullYear();
//         const currentWeek = getWeekNumber(new Date());

//         while (weekSelect.options.length > 1) {
//             weekSelect.remove(1);
//         }

//         for (let week = 1; week <= 52; week++) {
//             const jan1 = new Date(currentYear, 0, 1);
//             const daysOffset = (week - 1) * 7;
//             const weekDate = new Date(jan1.setDate(jan1.getDate() + daysOffset));
//             const monday = getMonday(weekDate);
//             const sunday = new Date(monday);
//             sunday.setDate(monday.getDate() + 6);

//             const weekText = `Tuần ${week} (${formatDate(monday)} - ${formatDate(sunday)})`;
//             const weekValue = `${week}|${formatDate(monday)}|${formatDate(sunday)}`;

//             const option = document.createElement('option');
//             option.value = weekValue;
//             option.textContent = weekText;
//             if (week === currentWeek) option.selected = true;

//             weekSelect.appendChild(option);
//         }
//     });
// }

// Tên tháng theo ngôn ngữ
const MONTH_NAMES = {
    vi: ['Tháng 1','Tháng 2','Tháng 3','Tháng 4','Tháng 5','Tháng 6', 'Tháng 7','Tháng 8','Tháng 9','Tháng 10','Tháng 11','Tháng 12'],
    en: ['Jan','Feb','Mar','Apr','May','Jun', 'Jul','Aug','Sep','Oct','Nov','Dec'],
    cn: ['1月','2月','3月','4月','5月','6月','7月','8月','9月','10月','11月','12月'],
};

const DAY_NAMES = {
    vi: ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'Chủ nhật'],
    en: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
    cn: ['周一', '周二', '周三', '周四', '周五', '周六', '周日'],
};

function getDayName(index) {
    const lang = localStorage.getItem('heatmap_lang') || 'vi';
    return (DAY_NAMES[lang] || DAY_NAMES['vi'])[index];
}

// Format ngày trong checkbox theo ngôn ngữ
// vi: "Thứ 2 - 11/05/2026"
// en: "Monday - 2026 11 May"
// cn: "周一 - 2026年5月11日"
function formatDayLabel(index, dateObj) {
    const dayName = getDayName(index);
    const lang    = localStorage.getItem('heatmap_lang') || 'vi';
    const d       = dateObj.getDate();
    const m       = dateObj.getMonth();
    const y       = dateObj.getFullYear();
    const dd      = String(d).padStart(2, '0');
    const mm      = String(m + 1).padStart(2, '0');
    const mon     = MONTH_NAMES[lang]?.[m] || MONTH_NAMES['en'][m]; // dùng MONTH_NAMES từ patch tuần
    
    let dateStr;
    if (lang === 'en') dateStr = `${y} ${dd} ${mon}`;
    else if (lang === 'cn') dateStr = `${y}年${mon}${dd}日`;
    else dateStr = `${dd}/${mm}/${y}`;
    
    return { dayName, dateStr, label: `${dayName} - ${dateStr}` };
}


// Label "Tuần" / "Week" / "第X周" theo ngôn ngữ
function getWeekLabel(weekNum) {
    const lang = localStorage.getItem('heatmap_lang') || 'vi';
    if (lang === 'en') return `Week ${weekNum}`;
    if (lang === 'cn') return `第 ${weekNum} 周`;
    return `Tuần ${weekNum}`;
}

// Format 1 ngày thành chuỗi theo ngôn ngữ
// vi: "11/05/2026"  |  en: "11 May 2026"  |  cn: "2026年5月11日"
function formatDateLocale(date) {
    const lang = localStorage.getItem('heatmap_lang') || 'vi';
    const d   = date.getDate();
    const m   = date.getMonth();      // 0-based
    const y   = date.getFullYear();
    const dd  = String(d).padStart(2, '0');
    const mm  = String(m + 1).padStart(2, '0');
    const mon = MONTH_NAMES[lang]?.[m] || MONTH_NAMES['en'][m];
    
    if (lang === 'en') return `${y} ${dd} ${mon} `;
    if (lang === 'cn') return `${y}年${mon}${dd}日`;
    return `${dd}/${mm}/${y}`;          // vi — giữ dd/mm/yyyy để parse ngược lại dễ
}

// Generate weeks cho tất cả select — gọi lại khi đổi ngôn ngữ
function generateWeeks() {
    const weekSelects = ['week1', 'week2', 'week3'];
    weekSelects.forEach(selectId => {
        const weekSelect = document.getElementById(selectId);
        if (!weekSelect) return;
        
        // Nhớ giá trị đang chọn (để restore sau khi rebuild)
        const previousValue = weekSelect.value;
        
        const currentYear = new Date().getFullYear();
        const currentWeek = getWeekNumber(new Date());
        
        // Xóa hết option (trừ option đầu trống)
        while (weekSelect.options.length > 1) weekSelect.remove(1);
        
        for (let week = 1; week <= 52; week++) {
            const jan1       = new Date(currentYear, 0, 1);
            const daysOffset = (week - 1) * 7;
            const weekDate   = new Date(jan1.setDate(jan1.getDate() + daysOffset));
            const monday     = getMonday(weekDate);
            const sunday     = new Date(monday);
            sunday.setDate(monday.getDate() + 6);
            
            // Text hiển thị theo ngôn ngữ
            const weekLabel = getWeekLabel(week);
            const startStr  = formatDateLocale(monday);
            const endStr    = formatDateLocale(sunday);
            const weekText  = `${weekLabel} (${startStr} - ${endStr})`;
            
            // Value vẫn giữ định dạng cố định "week|dd/mm/yyyy|dd/mm/yyyy"
            // để các hàm parse (generateDayCheckboxes, submit) không cần đổi
            const weekValue = `${week}|${formatDate(monday)}|${formatDate(sunday)}`;
            
            const option       = document.createElement('option');
            option.value       = weekText.startsWith('Tuần') || weekText.startsWith('Week') || weekText.startsWith('第')
            ? weekValue   // đúng rồi — value = chuỗi parse được
            : weekValue;
            option.value       = weekValue;
            option.textContent = weekText;
            if (week === currentWeek) option.selected = true;
            weekSelect.appendChild(option);
        }
        
        // Restore previous selection nếu vẫn còn trong list
        if (previousValue) {
            // value format: "20|11/05/2026|17/05/2026" — weekNum là phần đầu
            const prevWeekNum = previousValue.split('|')[0];
            for (let opt of weekSelect.options) {
                if (opt.value.startsWith(prevWeekNum + '|')) {
                    weekSelect.value = opt.value;
                    break;
                }
            }
        }
    });
}

// ── Lắng nghe sự kiện đổi ngôn ngữ từ i18n.js → rebuild weeks ──
document.addEventListener('i18n:applied', function () {
    generateWeeks();
});


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
    
    fetch(`${pathBase}/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
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
    
    // Reset ô search khi load lại
    const searchBox = document.getElementById('searchEmployee3');
    if (searchBox) searchBox.value = '';
    
    fetch(`${pathBase}/Heatmap/GetEmployeesByDepartment?departmentId=${departmentId}`)
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

// Filter employee checkboxes by search text (Mode 3)
function filterEmployeeCheckboxes(searchText) {
    const keyword = searchText.toLowerCase().trim();
    const boxes = document.querySelectorAll('#employeeCheckboxes3 .employee-checkbox');
    boxes.forEach(box => {
        const name = box.querySelector('label')?.textContent.toLowerCase() ?? '';
        box.style.display = (!keyword || name.includes(keyword)) ? '' : 'none';
    });
}

// Filter employee checkboxes by search text (Mode 3)
function filterEmployeeCheckboxes(searchText) {
    const keyword = searchText.toLowerCase().trim();
    const boxes = document.querySelectorAll('#employeeCheckboxes3 .employee-checkbox');
    boxes.forEach(box => {
        const name = box.querySelector('label')?.textContent.toLowerCase() ?? '';
        box.style.display = (!keyword || name.includes(keyword)) ? '' : 'none';
    });
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
        const lang = localStorage.getItem('heatmap_lang') || 'vi';
        const msg = lang === 'en' ? 'Please select a week'
        : lang === 'cn' ? '请先选择周'
        : 'Vui lòng chọn tuần';
        container.innerHTML = `<p class="text-gray-400 text-center py-4">${msg}</p>`;
        return;
    }
    
    const [weekNum, startDateStr] = weekValue.split('|');
    // startDateStr là dd/mm/yyyy (luôn cố định, không phụ thuộc ngôn ngữ)
    const startDate = new Date(startDateStr.split('/').reverse().join('-'));
    
    for (let i = 0; i < 7; i++) {
        const currentDate = new Date(startDate);
        currentDate.setDate(startDate.getDate() + i);
        
        const { dayName, dateStr, label } = formatDayLabel(i, currentDate);
        // value luôn là dd/mm/yyyy để submit không bị ảnh hưởng
        const valueStr = formatDate(currentDate); // hàm formatDate gốc dd/mm/yyyy
        
        const div = document.createElement('div');
        div.className = 'day-checkbox';
        div.innerHTML = `
            <input type="checkbox"
                   id="day${containerId}_${i}"
                   value="${valueStr}"
                   data-day="${dayName}"
                   data-label="${label}"
                   onchange="handleDayCheckboxChange(this, '${containerId}')">
            <label for="day${containerId}_${i}" class="cursor-pointer select-none flex-1">
                ${label}
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
    // Không dùng trực tiếp nữa — multi-select xử lý việc thêm row
    // Giữ lại để không break các chỗ gọi khác nếu có
    const container = document.getElementById('projectRows1');
    addProjectRow(container, savedData || null);
}

function initMode1ProjectRows() {
    const container = document.getElementById('projectRows1');
    if (!container) return;
    container.innerHTML = '';
    initProjectBlock(container, null);
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
            const pid = row.querySelector('.bulk-project')?.value || '';
            const found = pid ? window.projectsData.find(p => String(p.IdProject) === String(pid)) : null;
            rows.push({
                customer:     row.querySelector('.bulk-customer')?.value || '',
                project:      pid,
                projectName:  found ? found.NameProject : '',
                projectPhase: row.querySelector('.bulk-pp')?.value || '',
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
        labels.innerHTML = `<span>${t('import.col.project')}</span><span>${t('import.col.customer')}</span><span>${t('import.col.projphase')}</span><span>${t('import.label.hours')}</span><span></span>`;
        block.appendChild(labels);
        
        // Rows container
        const rowsContainer = document.createElement('div');
        rowsContainer.className = 'bulk-rows-container';
        block.appendChild(rowsContainer);
        
        container.appendChild(block);
        
        // Restore saved rows hoặc tạo 1 row mặc định — initProjectBlock tạo multi-select + rows
        const savedRows = (dayDataState[dateStr]?.rows?.length > 0)
        ? dayDataState[dateStr].rows.filter(r => r.project)
        : null;
        initProjectBlock(rowsContainer, savedRows);
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
            { label: dayCb.dataset.label, rows: [] };
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
    document.getElementById('bulkInputPopup').classList.remove('hidden'); document.getElementById('bulkInputPopup').classList.add('flex');
}

// Lưu data DOM hiện tại vào bulkAllData trước khi switch tab
function saveBulkCurrentData() {
    if (!bulkActiveEmpId) return;
    document.querySelectorAll('#bulkInputBlocks .bulk-block').forEach(block => {
        const date = block.dataset.date;
        const rows = [];
        block.querySelectorAll('.bulk-project-row').forEach(row => {
            const pid = row.querySelector('.bulk-project')?.value || '';
            const found = pid ? window.projectsData.find(p => String(p.IdProject) === String(pid)) : null;
            rows.push({
                customer:     row.querySelector('.bulk-customer').value,
                project:      pid,
                projectName:  found ? found.NameProject : '',
                projectPhase: row.querySelector('.bulk-pp')?.value || '',
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
    labels.innerHTML = `<span>${t('import.col.project')}</span><span>${t('import.col.customer')}</span><span>${t('import.col.projphase')}</span><span>${t('import.label.hours')}</span><span></span>`;
    block.appendChild(labels);
    
    const rowsContainer = document.createElement('div');
    rowsContainer.className = 'bulk-rows-container';
    block.appendChild(rowsContainer);
    
    const validRows = (savedRows || []).filter(r => r.project);
    initProjectBlock(rowsContainer, validRows.length > 0 ? validRows : null);
    return block;
}

// ============================================================
// MULTI-SELECT PROJECT DROPDOWN
// Mỗi block (rowsContainer) có 1 custom multi-select dropdown ở đầu.
// Tick project → thêm row; bỏ tick → xóa row.
// ============================================================

/**
* Tạo toàn bộ khu vực nhập cho 1 ngày/block.
* Gồm: 1 multi-select dropdown project ở trên + các rows bên dưới.
* Gọi thay cho addProjectRow khi khởi tạo block.
*/
function initProjectBlock(rowsContainer, savedRows) {
    // Wrapper bao gồm multi-select + rows
    rowsContainer._multiSelectOpen = false;
    
    // --- Tạo multi-select dropdown ---
    const msWrap = document.createElement('div');
    msWrap.className = 'ms-wrap';
    msWrap.style.cssText = 'position:relative; margin-bottom:0;';
    
    const msTrigger = document.createElement('div');
    msTrigger.className = 'bulk-select ms-trigger';
    msTrigger.style.cssText = 'display:flex; align-items:center; justify-content:space-between; cursor:pointer; user-select:none; background:white;';
    msTrigger.innerHTML = `<span class="ms-trigger-label" style="color:#6b7280;">${t('import.col.project')}</span>
    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="none" viewBox="0 0 24 24" stroke="currentColor" style="flex-shrink:0;color:#9ca3af;transition:transform 0.2s;"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/></svg>`;
    const msDropdown = document.createElement('div');
    msDropdown.className = 'ms-dropdown';
    msDropdown.style.cssText = `display:none; position:fixed; z-index:9999;
    background:white; border:2px solid #dc2626; border-top:none;
    border-bottom-left-radius:1rem; border-bottom-right-radius:1rem;
    box-shadow:0 10px 40px rgba(0,0,0,0.12); max-height:220px; overflow-y:auto;`;
    // Append vào body để thoát khỏi overflow:hidden của popup
    document.body.appendChild(msDropdown);
    msDropdown._triggerEl = msTrigger; // reference để đóng từ ngoài
    
    // Search box trong dropdown
    const msSearch = document.createElement('input');
    msSearch.type = 'text';
    msSearch.placeholder = t('import.search_proj');
    msSearch.style.cssText = 'width:100%; padding:8px 12px; border:none; border-bottom:1px solid #f3f4f6; font-size:0.85rem; outline:none; box-sizing:border-box;';
    msSearch.oninput = function() {
        const kw = this.value.toLowerCase();
        msDropdown.querySelectorAll('.ms-item').forEach(item => {
            item.style.display = item.dataset.name.toLowerCase().includes(kw) ? '' : 'none';
        });
    };
    msDropdown.appendChild(msSearch);
    
    // Render checkbox items
    window.projectsData.forEach(p => {
        const item = document.createElement('label');
        item.className = 'ms-item';
        item.dataset.name = p.NameProject;
        item.style.cssText = 'display:flex; align-items:center; gap:10px; padding:9px 14px; cursor:pointer; font-size:0.875rem; color:#374151; transition:background 0.1s;';
        item.onmouseenter = () => item.style.background = '#fef2f2';
        item.onmouseleave = () => { if (!item.querySelector('input').checked) item.style.background = ''; else item.style.background = '#fff5f5'; }
        
        const cb = document.createElement('input');
        cb.type = 'checkbox';
        cb.value = p.IdProject;
        cb.dataset.customer = p.NameCustomer || '';
        cb.dataset.projectName = p.NameProject;
        cb.style.cssText = 'accent-color:#dc2626; width:16px; height:16px; flex-shrink:0; cursor:pointer;';
        
        cb.onchange = function() {
            if (this.checked) {
                item.style.background = '#fff5f5';
                addProjectRow(rowsContainer, { project: p.IdProject, customer: p.NameCustomer, projectName: p.NameProject });
            } else {
                item.style.background = '';
                const toRemove = rowsContainer.querySelector(`.bulk-project-row[data-project-id="${p.IdProject}"]`);
                if (toRemove) toRemove.remove();
            }
            updateMsTriggerLabel(msTrigger, msDropdown);
        };
        
        item.appendChild(cb);
        item.appendChild(document.createTextNode(p.NameProject));
        msDropdown.appendChild(item);
    });
    
    // Toggle dropdown
    msTrigger.onclick = function(e) {
        const isOpen = msDropdown.style.display === 'block';
        if (isOpen) {
            msDropdown.style.display = 'none';
            msTrigger.style.borderBottomLeftRadius = '';
            msTrigger.style.borderBottomRightRadius = '';
            msTrigger.querySelector('svg').style.transform = '';
        } else {
            // Đóng tất cả dropdown khác
            document.querySelectorAll('.ms-dropdown').forEach(d => {
                if (d !== msDropdown) {
                    d.style.display = 'none';
                    const t = d._triggerEl;
                    if (t) { t.style.borderBottomLeftRadius = ''; t.style.borderBottomRightRadius = ''; t.querySelector('svg').style.transform = ''; }
                }
            });
            // Tính vị trí fixed dựa theo msTrigger
            const triggerRect = msTrigger.getBoundingClientRect();
            msDropdown.style.top = triggerRect.bottom + 'px';
            msDropdown.style.left = triggerRect.left + 'px';
            msDropdown.style.width = triggerRect.width + 'px';
            msDropdown.style.display = 'block';
            msTrigger.style.borderBottomLeftRadius = '0';
            msTrigger.style.borderBottomRightRadius = '0';
            msTrigger.querySelector('svg').style.transform = 'rotate(180deg)';
            setTimeout(() => msSearch.focus(), 50);
        }
    };
    
    msWrap.appendChild(msTrigger);
    // msDropdown đã được append vào body ở trên
    
    // Insert trước rowsContainer (cùng parent)
    rowsContainer.parentElement.insertBefore(msWrap, rowsContainer);
    rowsContainer._msWrap = msWrap;
    rowsContainer._msDropdown = msDropdown; // reference trực tiếp vì msDropdown ở body
    
    // Restore saved rows
    if (savedRows && savedRows.length > 0) {
        savedRows.forEach(r => {
            addProjectRow(rowsContainer, r);
            // Tick checkbox tương ứng
            if (r.project) {
                const cb = msDropdown.querySelector(`input[value="${r.project}"]`);
                if (cb) {
                    cb.checked = true;
                    cb.closest('.ms-item').style.background = '#fff5f5';
                }
            }
        });
        updateMsTriggerLabel(msTrigger, msDropdown);
    }
    // Không tạo row trống mặc định — user tick project để thêm row
}

function updateMsTriggerLabel(msTrigger, msDropdown) {
    const checked = msDropdown.querySelectorAll('input:checked');
    const label = msTrigger.querySelector('.ms-trigger-label');
    if (checked.length === 0) {
        label.textContent = t('import.col.project');
        label.style.color = '#6b7280';
    } else {
        label.textContent = Array.from(checked).map(cb => cb.dataset.projectName).join(', ');
        label.style.color = '#111827';
    }
}

// Đóng multi-select khi click ra ngoài
document.addEventListener('click', function(e) {
    if (!e.target.closest('.ms-wrap') && !e.target.closest('.ms-dropdown')) {
        document.querySelectorAll('.ms-dropdown').forEach(d => {
            if (d.style.display === 'block') {
                d.style.display = 'none';
                const t = d._triggerEl;
                if (t) { t.style.borderBottomLeftRadius = ''; t.style.borderBottomRightRadius = ''; t.querySelector('svg').style.transform = ''; }
            }
        });
    }
});

// Thêm 1 dòng project row (được gọi từ initProjectBlock hoặc nút "+ Thêm dự án")
function addProjectRow(rowsContainer, savedData) {
    const row = document.createElement('div');
    row.className = 'bulk-project-row';
    if (savedData?.project) row.dataset.projectId = savedData.project;
    
    // Project label (readonly text, thay vì select — vì đã chọn qua multi-select)
    const projectLabel = document.createElement('div');
    projectLabel.className = 'bulk-select bulk-project-label';
    projectLabel.style.cssText = 'display:flex; align-items:center; padding:0 12px; background:#f9fafb; border-radius:0.75rem; border:2px solid #e5e7eb; font-size:0.875rem; color:#111827; min-height:44px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;';
    // Lưu projectId vào hidden input để saveBulkCurrentData đọc được
    const hiddenProject = document.createElement('input');
    hiddenProject.type = 'hidden';
    hiddenProject.className = 'bulk-project';
    hiddenProject.value = savedData?.project || '';
    if (savedData?.project) {
        const found = window.projectsData.find(p => String(p.IdProject) === String(savedData.project));
        projectLabel.textContent = found ? found.NameProject : savedData.projectName || '';
    } else {
        projectLabel.textContent = '-- Project --';
        projectLabel.style.color = '#9ca3af';
    }
    projectLabel.appendChild(hiddenProject);
    
    // Customer (auto fill, disabled)
    const customerSelect = document.createElement('select');
    customerSelect.className = 'bulk-select bulk-customer select-disabled';
    customerSelect.disabled = true;
    customerSelect.style.cssText = 'background:#f3f4f6; color:#6b7280; cursor:not-allowed; opacity:0.85;';
    const customerNames = Array.from(new Set(window.projectsData.map(p => p.NameCustomer).filter(Boolean)));
    customerSelect.innerHTML = `<option value="">-- Customer --</option>` +
    customerNames.map(name => `<option value="${name}">${name}</option>`).join('');
    // Auto set customer
    const customerName = savedData?.customer ||
    (savedData?.project ? window.projectsData.find(p => String(p.IdProject) === String(savedData.project))?.NameCustomer : '');
    if (customerName) customerSelect.value = customerName;
    
    // Project Phase
    const ppOpts = (window.projectPhasesData || [])
    .map(pp => `<option value="${pp}"${savedData?.projectPhase === pp ? ' selected' : ''}>${pp}</option>`).join('');
    const ppSelect = document.createElement('select');
    ppSelect.className = 'bulk-select bulk-pp';
    ppSelect.innerHTML = `<option value="">-- Proj. Phase --</option>${ppOpts}`;
    
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
        // Bỏ tick checkbox tương ứng trong multi-select
        const pid = row.dataset.projectId;
        const msDropdownRef = rowsContainer._msDropdown;
        if (pid && msDropdownRef) {
            const cb = msDropdownRef.querySelector(`input[value="${pid}"]`);
            if (cb) {
                cb.checked = false;
                cb.closest('.ms-item').style.background = '';
                updateMsTriggerLabel(
                    rowsContainer._msWrap.querySelector('.ms-trigger'),
                    msDropdownRef
                );
            }
        }
        row.remove();
    };
    
    row.appendChild(projectLabel);
    row.appendChild(customerSelect);
    row.appendChild(ppSelect);
    row.appendChild(hoursInput);
    row.appendChild(deleteBtn);
    rowsContainer.appendChild(row);
}

// Khi chọn project → tự động set customer tương ứng (display only) — kept for compat
function onBulkProjectChange(projectSelect) {
    const row = projectSelect.closest('.bulk-project-row');
    const customerSelect = row.querySelector('.bulk-customer');
    const selectedOption = projectSelect.options[projectSelect.selectedIndex];
    const customerName = selectedOption?.dataset?.customer || '';
    customerSelect.value = customerName || '';
}

// Close bulk input popup
function closeBulkInputPopup() {
    saveBulkCurrentData(); // Lưu data đang nhập dở trước khi đóng
    document.getElementById('bulkInputPopup').classList.add('hidden'); document.getElementById('bulkInputPopup').classList.remove('flex');
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
        const hours        = parseFloat(row.querySelector('.bulk-hours-input').value);
        if (!customer || !project || !projectPhase || !hours || hours <= 0) {
            rowError = `Vui lòng nhập đầy đủ tất cả thông tin (dòng ${idx + 1})`;
        }
        projectRows.push({
            Customer:     customer,
            ProjectId:    parseInt(project),
            ProjectPhase: projectPhase,
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
            fetch(`${pathBase}/Heatmap/SaveStaffDetailMulti`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    showSuccessModal(typeof t==="function"?t('import.success.save1').replace('{n}',projectRows.length):`✓ Saved ${projectRows.length} projects!`);
                    if (!window.userDepartmentId) {
                        document.getElementById('department1').selectedIndex = 0;
                    }
                    document.getElementById('employee1').innerHTML = '<option value="">-- Chọn bộ phận trước --</option>';
                    resetSearchableSelect('employee1-sd', '-- Chọn bộ phận trước --');
                    if (window.userDepartmentId) {
                        loadEmployees(String(window.userDepartmentId), 'employee1');
                    }
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
            const hours        = parseFloat(row.querySelector('.bulk-hours-input').value);
            if (!customer || !project || !projectPhase || !hours || hours <= 0) {
                rowError = `Vui lòng nhập đầy đủ tất cả thông tin (${dateStr} - dòng ${idx + 1})`;
            }
            rows.push({
                Customer:     customer,
                ProjectId:    parseInt(project),
                ProjectPhase: projectPhase,
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
            fetch(`${pathBase}/Heatmap/SaveMultipleDaysMulti`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
            .then(r => r.json())
            .then(result => {
                if (result.success) {
                    showSuccessModal(typeof t==="function"?t('import.success.save2').replace('{n}',selectedDays.length):`✓ Saved ${selectedDays.length} days!`);
                    // Reset: giữ bộ phận/tuần, xóa ngày tick và dayDataState
                    const dept2 = window.userDepartmentId ? String(window.userDepartmentId) : document.getElementById('department2').value;
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
                if (!r.customer || !r.project || !r.projectPhase || !r.hours || parseFloat(r.hours) <= 0) {
                    if (!hasError) {  // chỉ giữ lỗi đầu tiên gặp
                        hasError = true;
                        const missing = [];
                        if (!r.customer)     missing.push('Customer');
                        if (!r.project)      missing.push('Project');
                        if (!r.projectPhase) missing.push('Proj. Phase');
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
            const response = await fetch(`${pathBase}/Heatmap/BulkImportMultiProject`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(records)
            });
            const result = await response.json();
            if (result.success) {
                closeBulkInputPopup();
                showSuccessModal(typeof t==="function"?t('import.success.save3').replace('{n}',result.total):`✅ Saved ${result.total} records!`);
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
    document.getElementById('errorModal').classList.remove('hidden'); document.getElementById('errorModal').classList.add('flex');
}

function closeErrorModal() {
    document.getElementById('errorModal').classList.add('hidden'); document.getElementById('errorModal').classList.remove('flex');
}

function showSuccessModal(message) {
    document.getElementById('successModalMessage').textContent = message;
    const modal = document.getElementById('successModal');
    modal.classList.remove('hidden');
    modal.classList.add('flex');
    if (typeof applyI18n === 'function') applyI18n();
}

function closeSuccessModal() {
    const modal = document.getElementById('successModal');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
}

function showConfirmModal(message, callback) {
    confirmCallback = callback;
    document.getElementById('confirmMessage').textContent = message;
    document.getElementById('confirmModal').classList.remove('hidden'); document.getElementById('confirmModal').classList.add('flex');
}

function closeConfirmModal() {
    document.getElementById('confirmModal').classList.add('hidden'); document.getElementById('confirmModal').classList.remove('flex');
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

// Re-generate day checkboxes khi đổi ngôn ngữ (nếu đã có tuần được chọn)
document.addEventListener('i18n:applied', function () {
    ['week2', 'week3'].forEach(weekId => {
        const weekSelect = document.getElementById(weekId);
        if (weekSelect?.value) {
            const containerId = weekId === 'week2' ? 'dayCheckboxes2' : 'dayCheckboxes3';
            generateDayCheckboxes(weekSelect.value, containerId);
        }
    });
});


// Đặt ở cuối file, ngang hàng với các listener i18n:applied khác
document.addEventListener('i18n:applied', function () {
    // Cập nhật placeholder ô tìm kiếm project
    document.querySelectorAll('.ms-dropdown input[type="text"]').forEach(input => {
        input.placeholder = t('import.search_proj');
    });
    
    // Cập nhật label trigger nếu chưa chọn project nào
    document.querySelectorAll('.ms-trigger').forEach(trigger => {
        const label = trigger.querySelector('.ms-trigger-label');
        if (!label) return;
        if (label.style.color === 'rgb(107, 114, 128)' || label.style.color === '#6b7280') {
            label.textContent = t('import.col.project');
        }
    });
});



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