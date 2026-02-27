// Global variables
let currentPhaseView = 'hours'; // 'hours' hoặc 'pct' hoặc 'chart'
let cachedPhaseData = [];
let cachedFunctionData = null;
let cachedCustomerData = [];
let currentCustomerView = 'table';
let customerChartInstance = null;
let currentFunctionView = 'table';
let functionChartInstance = null;
let phaseChartInstance = null;
let trendChart = null;
let departmentChart = null;
let currentChartView = 'week';
let currentData = null;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeFilters();
    loadReportData();
    
    // Time range change event
    document.getElementById('timeRange').addEventListener('change', function () {
        if (this.value === 'custom') {
            document.getElementById('customDateRange').classList.remove('hidden');
        } else {
            document.getElementById('customDateRange').classList.add('hidden');
        }
    });
});

// Initialize filters
function initializeFilters() {
    // Populate years (last 5 years)
    const currentYear = new Date().getFullYear();
    const yearSelect = document.getElementById('yearFilter');
    for (let i = 0; i < 5; i++) {
        const year = currentYear - i;
        const option = document.createElement('option');
        option.value = year;
        option.textContent = year;
        if (i === 0) option.selected = true;
        yearSelect.appendChild(option);
    }
    
    // Load departments and projects
    loadDepartments();
    loadProjects();
    loadPhases();
    loadCustomers();
}

// Load departments
async function loadDepartments() {
    try {
        const response = await fetch('/Heatmap/GetDepartmentList');
        if (response.ok) {
            const departments = await response.json();
            const select = document.getElementById('departmentFilter');
            departments.forEach(dept => {
                const option = document.createElement('option');
                option.value = dept.name;
                option.textContent = dept.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading departments:', error);
    }
}

// Load projects
async function loadProjects() {
    try {
        const response = await fetch('/Heatmap/GetProjectList');
        if (response.ok) {
            const projects = await response.json();
            const select = document.getElementById('projectFilter');
            projects.forEach(project => {
                const option = document.createElement('option');
                option.value = project.name;
                option.textContent = project.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading projects:', error);
    }
}

// Load phases
async function loadPhases() {
    try {
        const response = await fetch('/Heatmap/GetPhaseList');
        if (response.ok) {
            const phases = await response.json();
            const select = document.getElementById('phaseFilter');
            if (!select) return;
            phases.forEach(p => {
                const option = document.createElement('option');
                option.value = p.name;
                option.textContent = p.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading phases:', error);
    }
}

// Load customers
async function loadCustomers() {
    try {
        const response = await fetch('/Heatmap/GetCustomerList');
        if (response.ok) {
            const customers = await response.json();
            const select = document.getElementById('customerFilter');
            if (!select) return;
            customers.forEach(c => {
                const option = document.createElement('option');
                option.value = c.name;
                option.textContent = c.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Error loading customers:', error);
    }
}

// Apply filters
function applyFilters() {
    loadReportData();
}

// Refresh data
function refreshData() {
    loadReportData();
}

// Load report data
async function loadReportData() {
    const filters = getFilters();
    
    try {
        const queryString = new URLSearchParams(filters).toString();
        const response = await fetch(`/Heatmap/GetReportData?${queryString}`);
        
        if (response.ok) {
            currentData = await response.json();
            updateDashboard(currentData);
        } else {
            showError('Không thể tải dữ liệu báo cáo');
        }
    } catch (error) {
        console.error('Error loading report data:', error);
        showError('Lỗi khi tải dữ liệu: ' + error.message);
    }
}

// Get current filters
function getFilters() {
    const timeRange = document.getElementById('timeRange').value;
    const phaseEl = document.getElementById('phaseFilter');
    const customerEl = document.getElementById('customerFilter');
    const filters = {
        timeRange: timeRange,
        year: document.getElementById('yearFilter').value,
        customer: customerEl ? customerEl.value : '',
        department: document.getElementById('departmentFilter').value,
        project: document.getElementById('projectFilter').value,
        phase: phaseEl ? phaseEl.value : ''
    };
    
    if (timeRange === 'custom') {
        filters.startDate = document.getElementById('startDate').value;
        filters.endDate = document.getElementById('endDate').value;
    }
    
    return filters;
}

// Compute a human-readable date range label from current filters
function getDateRangeLabel() {
    const filters = getFilters();
    const timeRange = filters.timeRange;
    const fmt = (d) => d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    
    let start, end;
    const now = new Date();
    
    if (timeRange === 'custom') {
        if (filters.startDate && filters.endDate) {
            start = new Date(filters.startDate);
            end   = new Date(filters.endDate);
        }
    } else if (timeRange === 'current_week' || timeRange === 'last_week') {
        const day = now.getDay(); // 0=Sun
        const diffToMon = (day === 0 ? -6 : 1 - day);
        const monday = new Date(now);
        monday.setDate(now.getDate() + diffToMon);
        if (timeRange === 'last_week') monday.setDate(monday.getDate() - 7);
        start = new Date(monday);
        end   = new Date(monday);
        end.setDate(monday.getDate() + 6);
    } else if (timeRange === 'current_month') {
        start = new Date(now.getFullYear(), now.getMonth(), 1);
        end   = new Date(now.getFullYear(), now.getMonth() + 1, 0);
    } else if (timeRange === 'last_month') {
        start = new Date(now.getFullYear(), now.getMonth() - 1, 1);
        end   = new Date(now.getFullYear(), now.getMonth(), 0);
    } else if (timeRange === 'current_quarter') {
        const q = Math.floor(now.getMonth() / 3);
        start = new Date(now.getFullYear(), q * 3, 1);
        end   = new Date(now.getFullYear(), q * 3 + 3, 0);
    } else if (timeRange === 'current_year') {
        const yr = parseInt(filters.year) || now.getFullYear();
        start = new Date(yr, 0, 1);
        end   = new Date(yr, 11, 31);
    }
    
    if (start && end) {
        // Tính ISO week number từ ngày bắt đầu
        const getWeekNumber = (d) => {
            const date = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
            const dayNum = date.getUTCDay() || 7;
            date.setUTCDate(date.getUTCDate() + 4 - dayNum);
            const yearStart = new Date(Date.UTC(date.getUTCFullYear(), 0, 1));
            return Math.ceil((((date - yearStart) / 86400000) + 1) / 7);
        };
        const weekNum = getWeekNumber(start);
        return { weekNum, dateRange: `${fmt(start)} – ${fmt(end)}` };
    }
    return null;
}

// Update week label on both section headers
function updateWeekLabels() {
    const result = getDateRangeLabel();
    ['functionWeekLabel', 'phaseWeekLabel', 'customerWeekLabel'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.textContent = result ? `Week ${result.weekNum} (${result.dateRange})` : '';
    });
}

// Update dashboard
function updateDashboard(data) {
    updateKPIs(data.kpis);
    updateTrendChart(data.trendData);
    updateDepartmentChart(data.departmentData);
    cachedFunctionData = data.functionData;
    updateFunctionTable(data.functionData, currentFunctionView);
    updateDetailTable(data.detailData);
    cachedPhaseData = data.phaseData;
    if (currentPhaseView === 'chart') {
        updatePhaseChart(data.phaseData);
    } else {
        updatePhaseTable(data.phaseData, currentPhaseView);
    }
    cachedCustomerData = data.customerData || [];
    if (currentCustomerView === 'chart') {
        updateCustomerChart(cachedCustomerData);
    } else {
        updateCustomerTable(cachedCustomerData);
    }
    updateWeekLabels();
}

// Update KPIs
function updateKPIs(kpis) {
    document.getElementById('kpi_totalHours').textContent = formatNumber(kpis.totalHours);
    document.getElementById('kpi_availableCapacity').textContent = formatNumber(kpis.availableCapacity);
    document.getElementById('kpi_avgUtilization').textContent = kpis.avgUtilization.toFixed(1) + '%';
    document.getElementById('kpi_activeProjects').textContent = kpis.activeProjects;
    document.getElementById('kpi_staffCount').textContent = kpis.staffCount;
}

// Update trend chart
function updateTrendChart(trendData) {
    const ctx = document.getElementById('trendChart').getContext('2d');
    
    // Destroy existing chart
    if (trendChart) {
        trendChart.destroy();
    }
    
    const labels = trendData.map(d => d.label);
    const hours = trendData.map(d => d.hours);
    const utilization = trendData.map(d => d.utilization);
    
    trendChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Tổng giờ làm việc',
                    data: hours,
                    borderColor: '#dc2626',
                    backgroundColor: 'rgba(220, 38, 38, 0.1)',
                    fill: true,
                    tension: 0.4,
                    yAxisID: 'y'
                },
                {
                    label: 'Hiệu suất (%)',
                    data: utilization,
                    borderColor: '#2563eb',
                    backgroundColor: 'rgba(37, 99, 235, 0.1)',
                    fill: true,
                    tension: 0.4,
                    yAxisID: 'y1'
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        font: {
                            weight: 'bold'
                        }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: {
                        size: 14,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    }
                }
            },
            scales: {
                y: {
                    type: 'linear',
                    display: true,
                    position: 'left',
                    title: {
                        display: true,
                        text: 'Giờ làm việc',
                        font: {
                            weight: 'bold'
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                y1: {
                    type: 'linear',
                    display: true,
                    position: 'right',
                    title: {
                        display: true,
                        text: 'Hiệu suất (%)',
                        font: {
                            weight: 'bold'
                        }
                    },
                    grid: {
                        drawOnChartArea: false
                    }
                }
            }
        }
    });
}

// Update department chart
function updateDepartmentChart(departmentData) {
    const ctx = document.getElementById('departmentChart').getContext('2d');
    
    // Destroy existing chart
    if (departmentChart) {
        departmentChart.destroy();
    }
    
    const labels = departmentData.map(d => d.department);
    const hours = departmentData.map(d => d.hours);
    
    departmentChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Giờ làm việc',
                data: hours,
                backgroundColor: 'rgba(220, 38, 38, 0.8)',
                borderColor: '#dc2626',
                borderWidth: 2,
                borderRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: {
                        size: 14,
                        weight: 'bold'
                    },
                    bodyFont: {
                        size: 13
                    },
                    callbacks: {
                        label: function (context) {
                            return 'Giờ: ' + formatNumber(context.parsed.y);
                        }
                    }
                },
                datalabels: {
                    anchor: 'center',
                    align: 'center',
                    color: '#ffffff',
                    font: {
                        weight: 'bold',
                        size: 14
                    },
                    formatter: function(value) {
                        return formatNumber(value) + 'h';
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Giờ làm việc',
                        font: {
                            weight: 'bold'
                        }
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.05)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}

// Switch function view
function switchFunctionView(mode) {
    currentFunctionView = mode;
    const btnTable = document.getElementById('btnFunctionTable');
    const btnChart = document.getElementById('btnFunctionChart');
    const tableView = document.getElementById('functionTableView');
    const chartView = document.getElementById('functionChartView');
    
    if (mode === 'table') {
        btnTable.className = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-red-600 bg-red-600 text-white';
        btnChart.className = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-gray-200 bg-white text-gray-500 hover:border-red-600 hover:text-red-600';
        tableView.classList.remove('hidden');
        chartView.classList.add('hidden');
    } else {
        btnChart.className = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-red-600 bg-red-600 text-white';
        btnTable.className = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-gray-200 bg-white text-gray-500 hover:border-red-600 hover:text-red-600';
        tableView.classList.add('hidden');
        chartView.classList.remove('hidden');
        updateFunctionChart(cachedFunctionData);
    }
}

// Update By Function Utilization Table
function updateFunctionTable(funcData, mode = 'table') {
    const thead = document.getElementById('functionTableHead');
    const tbody = document.getElementById('functionTableBody');
    const tfoot = document.getElementById('functionTableFoot');
    
    if (!funcData || !funcData.departments || funcData.departments.length === 0) {
        thead.innerHTML = '<tr><th colspan="5" class="phase-loading">Không có dữ liệu</th></tr>';
        tbody.innerHTML = '';
        tfoot.innerHTML = '';
        return;
    }
    
    const depts = funcData.departments;
    
    // Header
    let headHtml = '<tr>';
    headHtml += '<th class="th-phase">By Function</th>';
    depts.forEach(d => { headHtml += `<th class="th-dept">${d}</th>`; });
    headHtml += `<th class="th-total">Total</th>`;
    headHtml += '</tr>';
    thead.innerHTML = headHtml;
    
    // Rows
    const rows = [
        { label: 'Available hrs', values: funcData.availableHrs, total: funcData.totalAvailable, fmt: v => formatNumber(v) },
        { label: 'No. of HC',     values: funcData.headCount,    total: funcData.totalHC,        fmt: v => v },
        { label: 'Utilize hour',  values: funcData.utilizeHour,  total: funcData.totalUtilize,   fmt: v => formatNumber(v) },
        { label: 'Utilization rate', values: funcData.utilizationRate, total: funcData.totalRate, fmt: v => v + '%', isRate: true },
    ];
    
    let bodyHtml = '';
    rows.forEach(row => {
        bodyHtml += '<tr>';
        bodyHtml += `<td class="td-phase">${row.label}</td>`;
        row.values.forEach((val, i) => {
            const display = row.fmt(val);
            const cls = row.isRate ? (val > 100 ? 'style="color:#dc2626;font-weight:700"' : '') : '';
            bodyHtml += `<td ${cls}>${display}</td>`;
        });
        const totalDisplay = row.fmt(row.total);
        const totalCls = row.isRate ? (row.total > 100 ? 'style="color:#dc2626;font-weight:700"' : '') : '';
        bodyHtml += `<td class="td-total" ${totalCls}>${totalDisplay}</td>`;
        bodyHtml += '</tr>';
    });
    tbody.innerHTML = bodyHtml;
    
    // Footer (working days info)
    let footHtml = '<tr>';
    footHtml += `<td class="td-phase" colspan="${depts.length + 2}" style="text-align:left;color:#9ca3af;font-size:0.75rem;font-weight:500">`;
    footHtml += `Số ngày làm việc: ${funcData.workingDays} ngày · 8.5h/ngày`;
    footHtml += '</td></tr>';
    tfoot.innerHTML = footHtml;
}

// Update By Function Chart (combo: bar + line)
function updateFunctionChart(funcData) {
    const canvas = document.getElementById('functionChart');
    if (!canvas || !funcData || !funcData.departments) return;
    
    if (functionChartInstance) {
        functionChartInstance.destroy();
        functionChartInstance = null;
    }
    
    const labels = funcData.departments;
    const hcData = funcData.headCount;
    const utilizeData = funcData.utilizeHour.map(v => Number(v));
    const rateData = funcData.utilizationRate.map(v => Number(v));
    
    functionChartInstance = new Chart(canvas, {
        data: {
            labels: labels,
            datasets: [
                {
                    type: 'bar',
                    label: 'No. of HC',
                    data: hcData,
                    backgroundColor: 'rgba(59, 130, 246, 0.8)',
                    yAxisID: 'y',
                    order: 3,
                    barPercentage: 0.5
                },
                {
                    type: 'bar',
                    label: 'Utilize hour',
                    data: utilizeData,
                    backgroundColor: 'rgba(251, 146, 60, 0.85)',
                    yAxisID: 'y',
                    order: 2,
                    barPercentage: 0.5
                },
                {
                    type: 'line',
                    label: 'Utilization rate',
                    data: rateData,
                    borderColor: '#16a34a',
                    backgroundColor: 'rgba(22,163,74,0.1)',
                    pointBackgroundColor: rateData.map(v => v > 100 ? '#dc2626' : '#16a34a'),
                    pointRadius: 5,
                    pointHoverRadius: 7,
                    borderWidth: 2.5,
                    tension: 0.3,
                    yAxisID: 'y2',
                    order: 1,
                    datalabels: {
                        align: 'top',
                        color: ctx => ctx.dataset.data[ctx.dataIndex] > 100 ? '#dc2626' : '#16a34a',
                        font: { weight: 'bold', size: 11 },
                        formatter: v => v + '%'
                    }
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom', labels: { font: { size: 12 } } },
                title: { display: false },
                datalabels: {
                    display: ctx => ctx.dataset.type !== 'line',
                    anchor: 'end',
                    align: 'top',
                    color: '#374151',
                    font: { weight: 'bold', size: 11 },
                    formatter: v => v > 0 ? v : ''
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    position: 'left',
                    title: { display: false },
                    grid: { color: '#f3f4f6' }
                },
                y2: {
                    beginAtZero: true,
                    position: 'right',
                    max: 160,
                    ticks: {
                        callback: v => v + '%',
                        color: '#16a34a'
                    },
                    grid: { drawOnChartArea: false }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}

// Update detail table
function updateDetailTable(detailData) {
    const tbody = document.getElementById('detailTableBody');
    
    if (detailData.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7" class="px-6 py-12 text-center text-gray-400">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mx-auto mb-3 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    <p class="font-medium">Không có dữ liệu</p>
                </td>
            </tr>
        `;
        return;
    }
    
    // Group by project
    const projectGroups = {};
    detailData.forEach(row => {
        if (!projectGroups[row.project]) {
            projectGroups[row.project] = {
                project: row.project,
                departments: [],
                totalStaffCount: 0,
                totalHours: 0
            };
        }
        projectGroups[row.project].departments.push({
            department: row.department,
            staffCount: row.staffCount,
            totalHours: row.totalHours
        });
        projectGroups[row.project].totalStaffCount += row.staffCount;
        projectGroups[row.project].totalHours += row.totalHours;
    });
    
    let html = '';
    Object.values(projectGroups).forEach(group => {
        const avgHours = group.totalHours / group.totalStaffCount;
        const statusClass = getStatusClass(avgHours);
        const statusText = getStatusText(avgHours);
        
        // Create department badges
        const departmentBadges = group.departments.map(d => 
            `<span class="inline-block px-2 py-1 bg-gray-100 text-gray-700 rounded text-xs font-medium mr-1">${d.department}</span>`
        ).join('');
        
        html += `
            <tr class="hover:bg-gray-50 transition-colors">
                <td class="px-6 py-4 font-bold text-gray-900">${group.project}</td>
                <td class="px-6 py-4">
                    <div class="flex flex-wrap gap-1">${departmentBadges}</div>
                </td>
                <td class="px-6 py-4 text-center">
                    <span class="inline-flex items-center justify-center w-10 h-10 rounded-full bg-gray-100 font-black text-gray-900">
                        ${group.totalStaffCount}
                    </span>
                </td>
                <td class="px-6 py-4 text-center font-black text-red-600">${formatNumber(group.totalHours)}h</td>
                <td class="px-6 py-4 text-center font-bold text-gray-900">${avgHours.toFixed(1)}h</td>
                <td class="px-6 py-4 text-center">
                    <span class="px-3 py-1 rounded-full text-xs font-bold ${statusClass}">
                        ${statusText}
                    </span>
                </td>
                <td class="px-6 py-4 text-center">
                    <button onclick='showProjectDepartments(${JSON.stringify(group)})' 
                            class="px-4 py-2 text-sm font-bold text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                        Xem chi tiết
                    </button>
                </td>
            </tr>
        `;
    });
    
    tbody.innerHTML = html;
}

// Get status class
function getStatusClass(avgHours) {
    if (avgHours >= 50) return 'status-overload';
    if (avgHours >= 40) return 'status-high';
    if (avgHours >= 30) return 'status-medium';
    return 'status-low';
}

// Get status text
function getStatusText(avgHours) {
    if (avgHours >= 50) return 'Quá tải';
    if (avgHours >= 40) return 'Cao';
    if (avgHours >= 30) return 'Trung bình';
    return 'Thấp';
}

// Show cell detail
function showCellDetail(cell) {
    const modal = document.getElementById('detailModal');
    const title = document.getElementById('modalTitle');
    const content = document.getElementById('modalContent');
    
    title.textContent = `${cell.project} - ${cell.week}`;
    
    // Fetch detailed staff data
    fetchCellStaffDetail(cell.project, cell.week, cell.department).then(staffData => {
        // Calculate total days for percentage
        const totalDays = staffData.reduce((sum, staff) => sum + staff.days, 0);
        
        let html = `
            <div class="mb-6">
                <div class="grid grid-cols-2 gap-4">
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Tổng giờ</div>
                        <div class="text-2xl font-black text-gray-900">${formatNumber(cell.hours)}h</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Số nhân sự</div>
                        <div class="text-2xl font-black text-gray-900">${cell.staffCount}</div>
                    </div>
                </div>
            </div>
        
            <h4 class="text-lg font-black text-gray-900 mb-4">Danh sách nhân viên</h4>
            <div class="space-y-2">
        `;
        
        staffData.forEach(staff => {
            const dayPercentage = totalDays > 0 ? ((staff.days / totalDays) * 100).toFixed(1) : 0;
            
            html += `
                <div class="flex items-center justify-between p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors">
                    <div>
                        <div class="font-bold text-gray-900">${staff.name}</div>
                        <div class="text-sm text-gray-500">${staff.svnStaff} - ${staff.department}</div>
                    </div>
                    <div class="text-right">
                        <div class="font-black text-lg text-red-600">${staff.hours}h</div>
                        <div class="text-xs text-gray-500">${staff.days} ngày (${dayPercentage}%)</div>
                    </div>
                </div>
            `;
        });
        
        html += '</div>';
        content.innerHTML = html;
    });
    
    modal.classList.remove('hidden');
}

// Show project departments (first level detail)
function showProjectDepartments(projectGroup) {
    const modal = document.getElementById('detailModal');
    const title = document.getElementById('modalTitle');
    const content = document.getElementById('modalContent');
    
    title.textContent = projectGroup.project;
    
    let html = `
        <div class="mb-6">
            <div class="grid grid-cols-3 gap-4">
                <div class="bg-gray-50 rounded-xl p-4">
                    <div class="text-sm font-medium text-gray-500 mb-1">Tổng giờ</div>
                    <div class="text-2xl font-black text-gray-900">${formatNumber(projectGroup.totalHours)}h</div>
                </div>
                <div class="bg-gray-50 rounded-xl p-4">
                    <div class="text-sm font-medium text-gray-500 mb-1">Số nhân sự</div>
                    <div class="text-2xl font-black text-gray-900">${projectGroup.totalStaffCount}</div>
                </div>
                <div class="bg-gray-50 rounded-xl p-4">
                    <div class="text-sm font-medium text-gray-500 mb-1">TB giờ/người</div>
                    <div class="text-2xl font-black text-gray-900">${(projectGroup.totalHours / projectGroup.totalStaffCount).toFixed(1)}h</div>
                </div>
            </div>
        </div>
    
        <h4 class="text-lg font-black text-gray-900 mb-4">Phân bổ theo bộ phận</h4>
        <div class="space-y-3">
    `;
    
    projectGroup.departments.forEach(dept => {
        const avgHours = dept.totalHours / dept.staffCount;
        const statusClass = getStatusClass(avgHours);
        const statusText = getStatusText(avgHours);
        const hoursPercentage = projectGroup.totalHours > 0 ? ((dept.totalHours / projectGroup.totalHours) * 100).toFixed(1) : 0;
        
        html += `
            <div class="border border-gray-200 rounded-xl p-4 hover:shadow-md transition-all cursor-pointer"
                 onclick='showDepartmentDetail("${projectGroup.project}", "${dept.department}")'>
                <div class="flex items-center justify-between mb-3">
                    <div>
                        <h5 class="font-black text-gray-900 text-lg">${dept.department}</h5>
                        <p class="text-sm text-gray-500">${dept.staffCount} nhân viên</p>
                    </div>
                    <span class="px-3 py-1 rounded-full text-xs font-bold ${statusClass}">
                        ${statusText}
                    </span>
                </div>
                <div class="grid grid-cols-2 gap-4">
                    <div>
                        <div class="text-xs text-gray-500 mb-1">Tổng giờ</div>
                        <div class="flex items-baseline gap-2">
                            <div class="text-xl font-black text-red-600">${formatNumber(dept.totalHours)}h</div>
                            <span class="text-sm font-bold text-blue-600">(${hoursPercentage}%)</span>
                        </div>
                    </div>
                    <div>
                        <div class="text-xs text-gray-500 mb-1">TB giờ/người</div>
                        <div class="text-xl font-black text-gray-900">${avgHours.toFixed(1)}h</div>
                    </div>
                </div>
                <div class="mt-3 flex items-center text-sm font-bold text-red-600">
                    <span>Xem chi tiết nhân viên</span>
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 ml-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    content.innerHTML = html;
    modal.classList.remove('hidden');
}

// Show department detail (second level detail - staff list)
function showDepartmentDetail(project, department) {
    const modal = document.getElementById('detailModal');
    const title = document.getElementById('modalTitle');
    const content = document.getElementById('modalContent');
    
    title.textContent = `${project} - ${department}`;
    
    // Show loading
    content.innerHTML = `
        <div class="text-center py-12">
            <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-red-600"></div>
            <p class="mt-4 text-gray-500 font-medium">Đang tải dữ liệu...</p>
        </div>
    `;
    
    // Fetch detailed staff data
    fetchProjectStaffDetail(project, department).then(staffData => {
        // Calculate total days for percentage
        const totalDays = staffData.reduce((sum, staff) => sum + staff.days, 0);
        const totalHours = staffData.reduce((sum, staff) => sum + staff.hours, 0);
        
        let html = `
            <div class="mb-4">
                <button onclick='showProjectDepartments(${JSON.stringify(getCurrentProjectGroup(project))})' 
                        class="flex items-center text-sm font-bold text-gray-600 hover:text-red-600 transition-colors">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                    </svg>
                    Quay lại danh sách bộ phận
                </button>
            </div>
        
            <div class="mb-6">
                <div class="grid grid-cols-3 gap-4">
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Tổng giờ</div>
                        <div class="text-2xl font-black text-gray-900">${formatNumber(totalHours)}h</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Số nhân sự</div>
                        <div class="text-2xl font-black text-gray-900">${staffData.length}</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">TB giờ/người</div>
                        <div class="text-2xl font-black text-gray-900">${(totalHours / staffData.length).toFixed(1)}h</div>
                    </div>
                </div>
            </div>
        
            <h4 class="text-lg font-black text-gray-900 mb-4">Danh sách nhân viên</h4>
            <div class="overflow-x-auto">
                <table class="w-full">
                    <thead class="bg-gray-50 border-b-2 border-gray-200">
                        <tr>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">Nhân viên</th>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">SVN</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Số giờ</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Số ngày</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">% Bộ phận</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">% Dự án</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-100">
        `;
        
        staffData.forEach(staff => {
            const dayPercentage = totalDays > 0 ? ((staff.days / totalDays) * 100).toFixed(1) : 0;
            const hoursPercentage = totalHours > 0 ? ((staff.hours / totalHours) * 100).toFixed(1) : 0;
            
            html += `
                <tr class="hover:bg-gray-50 cursor-pointer transition-colors" 
                    onclick='showStaffDailyDetail("${project}", "${department}", "${staff.svnStaff}", "${staff.name}")'>
                    <td class="px-4 py-3 font-medium text-gray-900">${staff.name}</td>
                    <td class="px-4 py-3 text-gray-600">${staff.svnStaff}</td>
                    <td class="px-4 py-3 text-center font-bold text-red-600">${staff.hours}h</td>
                    <td class="px-4 py-3 text-center text-gray-700">${staff.days}</td>
                    <td class="px-4 py-3 text-center">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-blue-100 text-blue-800">
                            ${dayPercentage}%
                        </span>
                    </td>
                    <td class="px-4 py-3 text-center">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-green-100 text-green-800">
                            ${hoursPercentage}%
                        </span>
                    </td>
                </tr>
            `;
        });
        
        html += `
                    </tbody>
                </table>
            </div>
        `;
        
        content.innerHTML = html;
    });
}

// Show staff daily detail (fourth level - daily breakdown)
function showStaffDailyDetail(project, department, svnStaff, staffName) {
    const modal = document.getElementById('detailModal');
    const title = document.getElementById('modalTitle');
    const content = document.getElementById('modalContent');
    
    title.textContent = `${staffName} (${svnStaff})`;
    
    // Show loading
    content.innerHTML = `
        <div class="text-center py-12">
            <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-red-600"></div>
            <p class="mt-4 text-gray-500 font-medium">Đang tải dữ liệu...</p>
        </div>
    `;
    
    // Fetch staff daily detail
    fetchStaffDailyDetail(project, department, svnStaff).then(dailyData => {
        const totalHours = dailyData.reduce((sum, day) => sum + day.hours, 0);
        
        let html = `
            <div class="mb-4">
                <button onclick='showDepartmentDetail("${project}", "${department}")' 
                        class="flex items-center text-sm font-bold text-gray-600 hover:text-red-600 transition-colors">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                    </svg>
                    Quay lại danh sách nhân viên
                </button>
            </div>
        
            <div class="mb-6">
                <div class="grid grid-cols-3 gap-4">
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Dự án</div>
                        <div class="text-lg font-black text-gray-900">${project}</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Bộ phận</div>
                        <div class="text-lg font-black text-gray-900">${department}</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Tổng giờ</div>
                        <div class="text-lg font-black text-red-600">${formatNumber(totalHours)}h</div>
                    </div>
                </div>
            </div>
        
            <h4 class="text-lg font-black text-gray-900 mb-4">Chi tiết theo ngày (${dailyData.length} ngày làm việc)</h4>
            <div class="overflow-x-auto">
                <table class="w-full">
                    <thead class="bg-gray-50 border-b-2 border-gray-200">
                        <tr>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">Ngày</th>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">Thứ</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Số giờ</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Tuần</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-100">
        `;
        
        dailyData.forEach(day => {
            html += `
                <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3 font-medium text-gray-900">${day.dateFormatted}</td>
                    <td class="px-4 py-3 text-gray-600">${day.dayOfWeek}</td>
                    <td class="px-4 py-3 text-center font-bold text-red-600">${day.hours}h</td>
                    <td class="px-4 py-3 text-center text-gray-700">${day.week}</td>
                </tr>
            `;
        });
        
        html += `
                    </tbody>
                    <tfoot class="bg-gray-50 border-t-2 border-gray-200">
                        <tr>
                            <td colspan="2" class="px-4 py-3 font-black text-gray-900 text-right">TỔNG CỘNG:</td>
                            <td class="px-4 py-3 text-center font-black text-red-600 text-lg">${formatNumber(totalHours)}h</td>
                            <td class="px-4 py-3"></td>
                        </tr>
                    </tfoot>
                </table>
            </div>
        `;
        
        content.innerHTML = html;
    });
}

// Fetch staff daily detail
async function fetchStaffDailyDetail(project, department, svnStaff) {
    const filters = getFilters();
    try {
        const queryString = new URLSearchParams({
            ...filters,
            project: project,
            department: department,
            svnStaff: svnStaff
        }).toString();
        const response = await fetch(`/Heatmap/GetStaffDailyDetail?${queryString}`);
        if (response.ok) {
            return await response.json();
        }
    } catch (error) {
        console.error('Error fetching staff daily detail:', error);
    }
    return [];
}
function getCurrentProjectGroup(projectName) {
    if (!currentData || !currentData.detailData) return null;
    
    const projectGroups = {};
    currentData.detailData.forEach(row => {
        if (!projectGroups[row.project]) {
            projectGroups[row.project] = {
                project: row.project,
                departments: [],
                totalStaffCount: 0,
                totalHours: 0
            };
        }
        projectGroups[row.project].departments.push({
            department: row.department,
            staffCount: row.staffCount,
            totalHours: row.totalHours
        });
        projectGroups[row.project].totalStaffCount += row.staffCount;
        projectGroups[row.project].totalHours += row.totalHours;
    });
    
    return projectGroups[projectName];
}

// Show project detail
function showProjectDetail(row) {
    const modal = document.getElementById('detailModal');
    const title = document.getElementById('modalTitle');
    const content = document.getElementById('modalContent');
    
    title.textContent = `${row.project} - ${row.department}`;
    
    // Fetch detailed staff data
    fetchProjectStaffDetail(row.project, row.department).then(staffData => {
        // Calculate total days for percentage
        const totalDays = staffData.reduce((sum, staff) => sum + staff.days, 0);
        
        let html = `
            <div class="mb-6">
                <div class="grid grid-cols-3 gap-4">
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Tổng giờ</div>
                        <div class="text-2xl font-black text-gray-900">${formatNumber(row.totalHours)}h</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">Số nhân sự</div>
                        <div class="text-2xl font-black text-gray-900">${row.staffCount}</div>
                    </div>
                    <div class="bg-gray-50 rounded-xl p-4">
                        <div class="text-sm font-medium text-gray-500 mb-1">TB giờ/người</div>
                        <div class="text-2xl font-black text-gray-900">${(row.totalHours / row.staffCount).toFixed(1)}h</div>
                    </div>
                </div>
            </div>
        
            <h4 class="text-lg font-black text-gray-900 mb-4">Danh sách nhân viên</h4>
            <div class="overflow-x-auto">
                <table class="w-full">
                    <thead class="bg-gray-50 border-b-2 border-gray-200">
                        <tr>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">Nhân viên</th>
                            <th class="px-4 py-3 text-left text-xs font-black text-gray-700 uppercase">SVN</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Số giờ</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">Số ngày</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">% Bộ phận</th>
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">% Dự án</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-100">
        `;
        
        staffData.forEach(staff => {
            const dayPercentage = totalDays > 0 ? ((staff.days / totalDays) * 100).toFixed(1) : 0;
            const hoursPercentage = row.totalHours > 0 ? ((staff.hours / row.totalHours) * 100).toFixed(1) : 0;
            
            html += `
                <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3 font-medium text-gray-900">${staff.name}</td>
                    <td class="px-4 py-3 text-gray-600">${staff.svnStaff}</td>
                    <td class="px-4 py-3 text-center font-bold text-red-600">${staff.hours}h</td>
                    <td class="px-4 py-3 text-center text-gray-700">${staff.days}</td>
                    <td class="px-4 py-3 text-center">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-blue-100 text-blue-800">
                            ${dayPercentage}%
                        </span>
                    </td>
                    <td class="px-4 py-3 text-center">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-green-100 text-green-800">
                            ${hoursPercentage}%
                        </span>
                    </td>
                </tr>
            `;
        });
        
        html += `
                    </tbody>
                </table>
            </div>
        `;
        
        content.innerHTML = html;
    });
    
    modal.classList.remove('hidden');
}

// Fetch cell staff detail
async function fetchCellStaffDetail(project, week, department) {
    try {
        const response = await fetch(`/Heatmap/GetCellStaffDetail?project=${encodeURIComponent(project)}&week=${week}&department=${encodeURIComponent(department)}`);
        if (response.ok) {
            return await response.json();
        }
    } catch (error) {
        console.error('Error fetching cell staff detail:', error);
    }
    return [];
}

// Fetch project staff detail
async function fetchProjectStaffDetail(project, department) {
    const filters = getFilters();
    try {
        const queryString = new URLSearchParams({
            ...filters,
            project: project,
            department: department
        }).toString();
        const response = await fetch(`/Heatmap/GetProjectStaffDetail?${queryString}`);
        if (response.ok) {
            return await response.json();
        }
    } catch (error) {
        console.error('Error fetching project staff detail:', error);
    }
    return [];
}

// Close detail modal
function closeDetailModal() {
    document.getElementById('detailModal').classList.add('hidden');
}

// Toggle chart view
function toggleChartView(view) {
    currentChartView = view;
    
    // Update button states
    document.querySelectorAll('.chart-view-btn').forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.view === view) {
            btn.classList.add('active');
        }
    });
    
    // Reload chart data
    if (currentData) {
        updateTrendChart(view === 'week' ? currentData.trendData : currentData.monthlyTrendData);
    }
}

// Toggle table sort
function toggleTableSort() {
    // Implementation for sorting
    alert('Chức năng sắp xếp đang được phát triển');
}

// Export report
function exportReport() {
    const filters = getFilters();
    const queryString = new URLSearchParams(filters).toString();
    window.location.href = `/Heatmap/ExportReport?${queryString}`;
}

// Utility functions
function formatNumber(num) {
    return num.toLocaleString('vi-VN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function animateValue(id, start, end, duration) {
    const element = document.getElementById(id);
    const range = end - start;
    const increment = end > start ? 1 : -1;
    const stepTime = Math.abs(Math.floor(duration / range));
    let current = start;
    
    const timer = setInterval(() => {
        current += increment;
        element.textContent = formatNumber(current);
        if (current === end) {
            clearInterval(timer);
        }
    }, stepTime);
}

function showError(message) {
    alert(message);
}

// ========================
// PHASE PIVOT TABLE
// ========================
function formatHours(val) {
    const n = Math.round(Number(val) * 100) / 100; // làm tròn 2 chữ số thập phân, tránh floating point
    return Number.isInteger(n) ? n.toString() : n.toFixed(2).replace(/\.?0+$/, '');
}

// Switch phase view mode
function switchPhaseView(mode) {
    currentPhaseView = mode;
    
    const btnHours = document.getElementById('btnByPhase');
    const btnPct   = document.getElementById('btnByPhasePct');
    const btnChart = document.getElementById('btnByPhaseChart');
    const tableView = document.getElementById('phaseTableView');
    const chartView = document.getElementById('phaseChartView');
    
    const activeClass   = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-red-600 bg-red-600 text-white';
    const inactiveClass = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-gray-200 bg-white text-gray-500 hover:border-red-600 hover:text-red-600';
    
    btnHours.className = mode === 'hours' ? activeClass : inactiveClass;
    btnPct.className   = mode === 'pct'   ? activeClass : inactiveClass;
    btnChart.className = mode === 'chart' ? activeClass : inactiveClass;
    
    if (mode === 'chart') {
        tableView.classList.add('hidden');
        chartView.classList.remove('hidden');
        updatePhaseChart(cachedPhaseData);
    } else {
        chartView.classList.add('hidden');
        tableView.classList.remove('hidden');
        updatePhaseTable(cachedPhaseData, mode);
    }
}

// Render Phase % stacked bar chart (nhất quán với bảng By Phase %)
// X-axis = Department, mỗi bar stack theo Phase, giá trị = % giờ của phase trong dept đó
function updatePhaseChart(phaseData) {
    // BƯỚC 1: Luôn destroy chart cũ trước tiên, không return sớm
    if (phaseChartInstance) {
        phaseChartInstance.destroy();
        phaseChartInstance = null;
    }
    
    // BƯỚC 2: Đảm bảo container về trạng thái canvas sạch
    const container = document.getElementById('phaseChartView');
    if (container) {
        container.innerHTML = '<canvas id="phaseChart" style="width:100%;height:100%;"></canvas>';
    }
    
    const chartCanvas = document.getElementById('phaseChart');
    
    // BƯỚC 3: Nếu không có data thì hiện thông báo và dừng
    if (!phaseData || phaseData.length === 0) {
        if (container) {
            container.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100%;color:#9ca3af;font-size:0.875rem;font-weight:500;letter-spacing:0.05em;">KHÔNG CÓ DỮ LIỆU</div>`;
        }
        return;
    }
    
    if (!chartCanvas) return;
    
    const phases      = [...new Set(phaseData.map(d => d.phase))].sort();
    const departments = [...new Set(phaseData.map(d => d.department))].sort();
    
    // Palette cho từng phase
    const palette = [
        { bg: 'rgba(220, 38,  38,  0.85)', border: '#dc2626' },
        { bg: 'rgba(37,  99,  235, 0.85)', border: '#2563eb' },
        { bg: 'rgba(22,  163, 74,  0.85)', border: '#16a34a' },
        { bg: 'rgba(234, 179, 8,   0.85)', border: '#ca8a04' },
        { bg: 'rgba(168, 85,  247, 0.85)', border: '#9333ea' },
        { bg: 'rgba(6,   182, 212, 0.85)', border: '#0891b2' },
    ];
    
    // X-axis = Department + thêm cột SVN%
    const grandTotal = phaseData.reduce((s, d) => s + Number(d.totalHours), 0);
    const labels = [...departments, 'SVN %'];
    
    // Mỗi Phase là 1 dataset, X-axis là Department + SVN%
    // Giá trị = % giờ của phase đó / tổng giờ của dept (giống bảng By Phase %)
    const datasets = phases.map((phase, i) => {
        const color = palette[i % palette.length];
        const data = departments.map(dept => {
            const deptTotal = phaseData
            .filter(d => d.department === dept)
            .reduce((s, d) => s + Number(d.totalHours), 0);
            const found = phaseData.find(d => d.phase === phase && d.department === dept);
            const val   = found ? Number(found.totalHours) : 0;
            return deptTotal > 0 ? Math.round(val / deptTotal * 100) : 0;
        });
        // Thêm SVN%: % giờ của phase này / grand total
        const phaseTotal = phaseData
        .filter(d => d.phase === phase)
        .reduce((s, d) => s + Number(d.totalHours), 0);
        data.push(grandTotal > 0 ? Math.round(phaseTotal / grandTotal * 100) : 0);
        return {
            label: phase,
            data,
            backgroundColor: color.bg,
            borderColor: color.border,
            borderWidth: 1.5,
            borderRadius: 4,
            datalabels: {
                display: ctx => ctx.dataset.data[ctx.dataIndex] > 0,
                color: '#fff',
                font: { weight: 'bold', size: 12 },
                formatter: v => v + '%'
            }
        };
    });
    
    phaseChartInstance = new Chart(chartCanvas, {
        type: 'bar',
        data: { labels, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom', labels: { font: { size: 12 }, padding: 16 } },
                tooltip: {
                    callbacks: {
                        label: ctx => ` ${ctx.dataset.label}: ${ctx.parsed.y}%`
                    }
                },
                datalabels: {}
            },
            scales: {
                x: {
                    stacked: true,
                    grid: { display: false },
                    ticks: {
                        color: ctx => ctx.tick.label === 'SVN %' ? '#dc2626' : '#374151',
                        font: ctx => ({
                            weight: ctx.tick.label === 'SVN %' ? 'bold' : 'normal',
                            size: 12
                        })
                    }
                },
                y: {
                    stacked: true,
                    max: 100,
                    ticks: { callback: v => v + '%' },
                    grid: { color: '#f3f4f6' },
                    title: { display: true, text: 'Phân bổ theo Phase (%)', font: { weight: 'bold' } }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}

function updatePhaseTable(phaseData, mode = 'hours') {
    const thead = document.getElementById('phaseTableHead');
    const tbody = document.getElementById('phaseTableBody');
    const tfoot = document.getElementById('phaseTableFoot');
    
    if (!phaseData || phaseData.length === 0) {
        thead.innerHTML = '<tr><th colspan="10" class="phase-loading">Không có dữ liệu</th></tr>';
        tbody.innerHTML = '';
        tfoot.innerHTML = '';
        return;
    }
    
    const phases = [...new Set(phaseData.map(d => d.phase))].sort();
    const departments = [...new Set(phaseData.map(d => d.department))].sort();
    const grandTotal = phaseData.reduce((sum, d) => sum + Number(d.totalHours), 0);
    
    // ==== HEADER ====
    let headHtml = '<tr>';
    headHtml += `<th class="th-phase">By Phase</th>`;
    departments.forEach(dept => { headHtml += `<th class="th-dept">${dept}</th>`; });
    headHtml += `<th class="th-total">SVN</th>`;
    headHtml += `<th class="th-pct">SVN %</th>`;
    headHtml += '</tr>';
    thead.innerHTML = headHtml;
    
    // ==== BODY ====
    let bodyHtml = '';
    phases.forEach(phase => {
        const phaseTotal = phaseData
        .filter(d => d.phase === phase)
        .reduce((sum, d) => sum + Number(d.totalHours), 0);
        const phasePct = grandTotal > 0 ? Math.round(phaseTotal / grandTotal * 100) : 0;
        
        bodyHtml += '<tr>';
        bodyHtml += `<td class="td-phase">${phase || '—'}</td>`;
        departments.forEach(dept => {
            const found = phaseData.find(d => d.phase === phase && d.department === dept);
            const val = found ? Number(found.totalHours) : 0;
            if (mode === 'pct') {
                // % theo cột: val / tổng giờ của dept đó
                const deptTotal = phaseData
                .filter(d => d.department === dept)
                .reduce((sum, d) => sum + Number(d.totalHours), 0);
                const pct = deptTotal > 0 ? Math.round(val / deptTotal * 100) : 0;
                bodyHtml += `<td>${val > 0 ? pct + '%' : ''}</td>`;
            } else {
                bodyHtml += `<td>${val > 0 ? formatHours(val) : ''}</td>`;
            }
        });
        if (mode === 'pct') {
            bodyHtml += `<td class="td-total">${formatHours(phaseTotal)}</td>`;
            bodyHtml += `<td class="td-pct">${phasePct}%</td>`;
        } else {
            bodyHtml += `<td class="td-total">${formatHours(phaseTotal)}</td>`;
            bodyHtml += `<td class="td-pct">${phasePct}%</td>`;
        }
        bodyHtml += '</tr>';
    });
    tbody.innerHTML = bodyHtml;
    
    // ==== FOOTER ====
    let footHtml = '<tr>';
    footHtml += `<td class="td-phase"></td>`;
    departments.forEach(dept => {
        const deptTotal = phaseData
        .filter(d => d.department === dept)
        .reduce((sum, d) => sum + Number(d.totalHours), 0);
        if (mode === 'pct') {
            footHtml += `<td>${deptTotal > 0 ? '100%' : ''}</td>`;
        } else {
            footHtml += `<td>${deptTotal > 0 ? formatHours(deptTotal) : ''}</td>`;
        }
    });
    footHtml += `<td class="td-total">${formatHours(grandTotal)}</td>`;
    footHtml += `<td class="td-pct">100%</td>`;
    footHtml += '</tr>';
    tfoot.innerHTML = footHtml;
}
// ============================================================
// CUSTOMER TABLE & CHART
// ============================================================

function switchCustomerView(mode) {
    currentCustomerView = mode;
    const btnTable = document.getElementById('btnCustomerTable');
    const btnChart = document.getElementById('btnCustomerChart');
    const tableView = document.getElementById('customerTableView');
    const chartView = document.getElementById('customerChartView');
    const activeClass  = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-red-600 bg-red-600 text-white';
    const inactiveClass = 'px-4 py-2 rounded-xl text-sm font-bold border-2 transition-all border-gray-200 bg-white text-gray-500 hover:border-red-600 hover:text-red-600';
    
    if (mode === 'table') {
        btnTable.className = activeClass;
        btnChart.className = inactiveClass;
        tableView.classList.remove('hidden');
        chartView.classList.add('hidden');
        updateCustomerTable(cachedCustomerData);
    } else {
        btnChart.className = activeClass;
        btnTable.className = inactiveClass;
        tableView.classList.add('hidden');
        chartView.classList.remove('hidden');
        updateCustomerChart(cachedCustomerData);
    }
}

/**
* Pivot table:
*  Rows    = Customer (grouped header row) + Project rows under each customer
*  Columns = Departments (dynamic) + Total + SCM%
*  Logic matches excel screenshot 1: Total – By Customer
*/
function updateCustomerTable(customerData) {
    const thead = document.getElementById('customerTableHead');
    const tbody = document.getElementById('customerTableBody');
    const tfoot = document.getElementById('customerTableFoot');
    
    if (!customerData || customerData.length === 0) {
        thead.innerHTML = '<tr><th colspan="10" class="phase-loading">Không có dữ liệu</th></tr>';
        tbody.innerHTML = '';
        tfoot.innerHTML = '';
        return;
    }
    
    // Unique departments (columns) sorted
    const departments = [...new Set(customerData.map(d => d.department))].sort();
    const grandTotal  = customerData.reduce((s, d) => s + Number(d.totalHours), 0);
    
    // ==== HEADER ====
    let headHtml = '<tr>';
    headHtml += `<th class="th-phase" style="min-width:110px">Customer</th>`;
    headHtml += `<th class="th-phase" style="min-width:140px">Project</th>`;
    departments.forEach(d => { headHtml += `<th class="th-dept">${d}</th>`; });
    headHtml += `<th class="th-total">SVN</th>`;
    headHtml += `<th class="th-pct">SVN %</th>`;
    headHtml += '</tr>';
    thead.innerHTML = headHtml;
    
    // Group: customer → projects
    const customerMap = {};
    customerData.forEach(row => {
        const cust = row.customer || '(Không có)';
        if (!customerMap[cust]) customerMap[cust] = {};
        const proj = row.project;
        if (!customerMap[cust][proj]) customerMap[cust][proj] = {};
        customerMap[cust][proj][row.department] = (customerMap[cust][proj][row.department] || 0) + Number(row.totalHours);
    });
    
    let bodyHtml = '';
    const sortedCustomers = Object.keys(customerMap).sort();
    
    sortedCustomers.forEach(cust => {
        const projects = Object.keys(customerMap[cust]).sort();
        
        // Project rows — Customer name cell only on first row (rowspan = số project)
        projects.forEach((proj, idx) => {
            const projTotal = Object.values(customerMap[cust][proj]).reduce((s, h) => s + h, 0);
            const projPct   = grandTotal > 0 ? Math.round(projTotal / grandTotal * 100) : 0;
            bodyHtml += `<tr>`;
            if (idx === 0) {
                bodyHtml += `<td class="td-customer-name" rowspan="${projects.length}">${cust}</td>`;
            }
            bodyHtml += `<td class="td-project-name">${proj}</td>`;
            departments.forEach(dept => {
                const h = customerMap[cust][proj][dept] || 0;
                bodyHtml += `<td>${h > 0 ? formatHours(h) : ''}</td>`;
            });
            bodyHtml += `<td class="td-total">${formatHours(projTotal)}</td>`;
            bodyHtml += `<td class="td-pct">${projPct}%</td>`;
            bodyHtml += `</tr>`;
        });
    });
    
    tbody.innerHTML = bodyHtml;
    
    // ==== FOOTER ====
    let footHtml = '<tr>';
    footHtml += `<td class="td-phase" colspan="2"></td>`;
    departments.forEach(dept => {
        const deptTotal = customerData
        .filter(d => d.department === dept)
        .reduce((s, d) => s + Number(d.totalHours), 0);
        footHtml += `<td>${deptTotal > 0 ? formatHours(deptTotal) : ''}</td>`;
    });
    footHtml += `<td class="td-total">${formatHours(grandTotal)}</td>`;
    footHtml += `<td class="td-pct">100%</td>`;
    footHtml += '</tr>';
    tfoot.innerHTML = footHtml;
}

/** Bar chart: X = Customer, stacked bars = Departments */
function updateCustomerChart(customerData) {
    const container = document.getElementById('customerChartView');
    if (customerChartInstance) {
        customerChartInstance.destroy();
        customerChartInstance = null;
    }
    if (container) {
        container.innerHTML = '<canvas id="customerChart" style="width:100%;height:100%;"></canvas>';
    }
    const canvas = document.getElementById('customerChart');
    
    if (!customerData || customerData.length === 0) {
        if (container) container.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100%;color:#9ca3af;font-size:0.875rem;font-weight:500;">KHÔNG CÓ DỮ LIỆU</div>`;
        return;
    }
    if (!canvas) return;
    
    const customers    = [...new Set(customerData.map(d => d.customer))].sort();
    const departments  = [...new Set(customerData.map(d => d.department))].sort();
    
    const palette = [
        { bg: 'rgba(220,38,38,0.85)',   border: '#dc2626' },
        { bg: 'rgba(37,99,235,0.85)',   border: '#2563eb' },
        { bg: 'rgba(22,163,74,0.85)',   border: '#16a34a' },
        { bg: 'rgba(234,179,8,0.85)',   border: '#ca8a04' },
        { bg: 'rgba(168,85,247,0.85)',  border: '#9333ea' },
        { bg: 'rgba(6,182,212,0.85)',   border: '#0891b2' },
        { bg: 'rgba(251,146,60,0.85)',  border: '#ea580c' },
        { bg: 'rgba(99,102,241,0.85)',  border: '#6366f1' },
    ];
    
    const datasets = departments.map((dept, i) => {
        const color = palette[i % palette.length];
        return {
            label: dept,
            data: customers.map(cust => {
                return customerData
                .filter(d => d.customer === cust && d.department === dept)
                .reduce((s, d) => s + Number(d.totalHours), 0);
            }),
            backgroundColor: color.bg,
            borderColor: color.border,
            borderWidth: 1.5,
            borderRadius: 4,
        };
    });
    
    customerChartInstance = new Chart(canvas, {
        type: 'bar',
        data: { labels: customers, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'bottom', labels: { font: { size: 12 }, padding: 14 } },
                tooltip: {
                    callbacks: {
                        label: ctx => ` ${ctx.dataset.label}: ${formatHours(ctx.parsed.y)}h`
                    }
                },
                datalabels: { display: false }
            },
            scales: {
                x: { stacked: true, grid: { display: false } },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    grid: { color: '#f3f4f6' },
                    ticks: { callback: v => formatHours(v) + 'h' },
                    title: { display: true, text: 'Tổng giờ làm', font: { weight: 'bold' } }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}
