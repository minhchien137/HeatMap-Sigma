// Global variables
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
    const filters = {
        timeRange: timeRange,
        year: document.getElementById('yearFilter').value,
        department: document.getElementById('departmentFilter').value,
        project: document.getElementById('projectFilter').value
    };

    if (timeRange === 'custom') {
        filters.startDate = document.getElementById('startDate').value;
        filters.endDate = document.getElementById('endDate').value;
    }

    return filters;
}

// Update dashboard
function updateDashboard(data) {
    updateKPIs(data.kpis);
    updateTrendChart(data.trendData);
    updateDepartmentChart(data.departmentData);
    updateHeatmap(data.heatmapData);
    updateDetailTable(data.detailData);
}

// Update KPIs
function updateKPIs(kpis) {
    document.getElementById('kpi_totalHours').textContent = formatNumber(kpis.totalHours);
    document.getElementById('kpi_avgUtilization').textContent = kpis.avgUtilization.toFixed(1) + '%';
    document.getElementById('kpi_activeProjects').textContent = kpis.activeProjects;
    document.getElementById('kpi_staffCount').textContent = kpis.staffCount;

    // Animate numbers
    animateValue('kpi_totalHours', 0, kpis.totalHours, 1000);
    animateValue('kpi_activeProjects', 0, kpis.activeProjects, 1000);
    animateValue('kpi_staffCount', 0, kpis.staffCount, 1000);
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

// Update heatmap
function updateHeatmap(heatmapData) {
    const container = document.getElementById('heatmapContainer');

    if (heatmapData.length === 0) {
        container.innerHTML = `
            <div class="text-center py-12 text-gray-400">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 mx-auto mb-4 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
                <p class="font-medium">Không có dữ liệu heatmap</p>
            </div>
        `;
        return;
    }

    // Group by project
    const projectGroups = {};
    heatmapData.forEach(item => {
        if (!projectGroups[item.project]) {
            projectGroups[item.project] = [];
        }
        projectGroups[item.project].push(item);
    });

    // Get all unique weeks
    const weeks = [...new Set(heatmapData.map(item => item.week))].sort();

    // Calculate max hours for color scaling
    const maxHours = Math.max(...heatmapData.map(item => item.hours));

    let html = '<div class="space-y-4">';

    Object.keys(projectGroups).forEach(project => {
        const projectData = projectGroups[project];

        html += `
            <div class="border border-gray-200 rounded-xl overflow-hidden">
                <div class="bg-gray-50 px-4 py-3 border-b border-gray-200">
                    <h4 class="font-black text-gray-900">${project}</h4>
                </div>
                <div class="p-4">
                    <div class="grid grid-cols-${Math.min(weeks.length, 8)} gap-2">
        `;

        weeks.forEach(week => {
            const cell = projectData.find(d => d.week === week);
            if (cell) {
                const intensity = (cell.hours / maxHours) * 100;
                const bgColor = `rgba(220, 38, 38, ${intensity / 100})`;

                html += `
                    <div class="heatmap-cell p-3 rounded-lg border border-gray-200 text-center" 
                         style="background-color: ${bgColor}"
                         onclick='showCellDetail(${JSON.stringify(cell)})'>
                        <div class="text-xs font-bold text-gray-700 mb-1">${week}</div>
                        <div class="text-lg font-black text-gray-900">${formatNumber(cell.hours)}h</div>
                        <div class="text-xs text-gray-600">${cell.staffCount} người</div>
                    </div>
                `;
            } else {
                html += `
                    <div class="p-3 rounded-lg border border-gray-200 bg-gray-50 text-center opacity-50">
                        <div class="text-xs font-bold text-gray-400 mb-1">${week}</div>
                        <div class="text-xs text-gray-400">-</div>
                    </div>
                `;
            }
        });

        html += `
                    </div>
                </div>
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;
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

    let html = '';
    detailData.forEach(row => {
        const avgHours = row.totalHours / row.staffCount;
        const statusClass = getStatusClass(avgHours);
        const statusText = getStatusText(avgHours);

        html += `
            <tr class="hover:bg-gray-50 transition-colors">
                <td class="px-6 py-4 font-bold text-gray-900">${row.project}</td>
                <td class="px-6 py-4 text-gray-700">${row.department}</td>
                <td class="px-6 py-4 text-center">
                    <span class="inline-flex items-center justify-center w-10 h-10 rounded-full bg-gray-100 font-black text-gray-900">
                        ${row.staffCount}
                    </span>
                </td>
                <td class="px-6 py-4 text-center font-black text-red-600">${formatNumber(row.totalHours)}h</td>
                <td class="px-6 py-4 text-center font-bold text-gray-900">${avgHours.toFixed(1)}h</td>
                <td class="px-6 py-4 text-center">
                    <span class="px-3 py-1 rounded-full text-xs font-bold ${statusClass}">
                        ${statusText}
                    </span>
                </td>
                <td class="px-6 py-4 text-center">
                    <button onclick='showProjectDetail(${JSON.stringify(row)})' 
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
                            <th class="px-4 py-3 text-center text-xs font-black text-gray-700 uppercase">%</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-100">
        `;

        staffData.forEach(staff => {
            const dayPercentage = totalDays > 0 ? ((staff.days / totalDays) * 100).toFixed(1) : 0;
            
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
    return num.toLocaleString('vi-VN', { maximumFractionDigits: 1 });
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
