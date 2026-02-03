// Global variables
let allData = [];
let filteredData = [];
let currentPage = 1;
let recordsPerPage = 25;
let sortColumn = 'WorkDate';
let sortDirection = 'desc';
let deleteRecordId = null;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    loadData();
});

// Load data from server
async function loadData() {
    try {
        const response = await fetch('/Heatmap/GetHistoryData');
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        
        const data = await response.json();
        allData = data;
        filteredData = [...allData];
        
        // Populate filter dropdowns
        populateFilters();
        
        // Update stats
        updateStats();
        
        // Display data
        displayData();
    } catch (error) {
        console.error('Error loading data:', error);
        showError('Không thể tải dữ liệu. Vui lòng thử lại.');
    }
}

// Populate filter dropdowns with unique values
function populateFilters() {
    // Department filter
    const departments = [...new Set(allData.map(item => item.department))].sort();
    const departmentSelect = document.getElementById('filterDepartment');
    departments.forEach(dept => {
        const option = document.createElement('option');
        option.value = dept;
        option.textContent = dept;
        departmentSelect.appendChild(option);
    });

    // Project filter
    const projects = [...new Set(allData.map(item => item.project))].sort();
    const projectSelect = document.getElementById('filterProject');
    projects.forEach(proj => {
        const option = document.createElement('option');
        option.value = proj;
        option.textContent = proj;
        projectSelect.appendChild(option);
    });

    // Year filter
    const years = [...new Set(allData.map(item => item.year))].sort((a, b) => b - a);
    const yearSelect = document.getElementById('filterYear');
    years.forEach(year => {
        const option = document.createElement('option');
        option.value = year;
        option.textContent = year;
        yearSelect.appendChild(option);
    });

    // Week filter (1-53)
    const weekSelect = document.getElementById('filterWeek');
    for (let i = 1; i <= 53; i++) {
        const option = document.createElement('option');
        option.value = i;
        option.textContent = `Tuần ${i}`;
        weekSelect.appendChild(option);
    }
}

// Apply filters
function applyFilters() {
    const department = document.getElementById('filterDepartment').value;
    const project = document.getElementById('filterProject').value;
    const year = document.getElementById('filterYear').value;
    const week = document.getElementById('filterWeek').value;
    const searchText = document.getElementById('searchInput').value.toLowerCase();

    filteredData = allData.filter(item => {
        let matches = true;

        if (department && item.department !== department) matches = false;
        if (project && item.project !== project) matches = false;
        if (year && item.year.toString() !== year) matches = false;
        if (week && item.weekNo.toString() !== week) matches = false;
        
        if (searchText) {
            const searchableText = `${item.svnStaff} ${item.nameStaff}`.toLowerCase();
            if (!searchableText.includes(searchText)) matches = false;
        }

        return matches;
    });

    currentPage = 1;
    updateStats();
    displayData();
}

// Reset filters
function resetFilters() {
    document.getElementById('filterDepartment').value = '';
    document.getElementById('filterProject').value = '';
    document.getElementById('filterYear').value = '';
    document.getElementById('filterWeek').value = '';
    document.getElementById('searchInput').value = '';
    
    filteredData = [...allData];
    currentPage = 1;
    updateStats();
    displayData();
}

// Update statistics
function updateStats() {
    document.getElementById('totalRecords').textContent = filteredData.length.toLocaleString();
    
    const uniqueStaff = new Set(filteredData.map(item => item.svnStaff));
    document.getElementById('totalStaff').textContent = uniqueStaff.size.toLocaleString();
    
    const uniqueProjects = new Set(filteredData.map(item => item.project));
    document.getElementById('totalProjects').textContent = uniqueProjects.size.toLocaleString();
    
    const totalHours = filteredData.reduce((sum, item) => sum + (item.workHours || 0), 0);
    document.getElementById('totalHours').textContent = totalHours.toFixed(1);
}

// Sort table
function sortTable(column) {
    if (sortColumn === column) {
        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        sortColumn = column;
        sortDirection = 'asc';
    }

    filteredData.sort((a, b) => {
        let aVal = a[column.charAt(0).toLowerCase() + column.slice(1)];
        let bVal = b[column.charAt(0).toLowerCase() + column.slice(1)];

        if (column === 'WorkDate') {
            aVal = new Date(aVal);
            bVal = new Date(bVal);
        }

        if (sortDirection === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });

    displayData();
}

// Display data in table
function displayData() {
    const tbody = document.getElementById('dataTableBody');
    const start = (currentPage - 1) * recordsPerPage;
    const end = start + recordsPerPage;
    const pageData = filteredData.slice(start, end);

    if (pageData.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="11" class="text-center py-20">
                    <div class="flex flex-col items-center justify-center">
                        <div class="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center mb-4">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-10 w-10 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                        </div>
                        <p class="text-gray-500 text-lg font-semibold">Không tìm thấy dữ liệu</p>
                        <p class="text-gray-400 text-sm mt-2">Thử điều chỉnh bộ lọc của bạn</p>
                    </div>
                </td>
            </tr>
        `;
    } else {
        tbody.innerHTML = pageData.map((item, index) => {
            const rowNumber = start + index + 1;
            const workDate = new Date(item.workDate);
            const createDate = new Date(item.createDate);
            
            return `
                <tr class="table-row">
                    <td class="table-cell">
                        <span class="badge badge-blue">${item.svnStaff}</span>
                    </td>
                    <td class="table-cell font-semibold">${item.nameStaff}</td>
                    <td class="table-cell">
                        <span class="badge badge-green">${item.department}</span>
                    </td>
                    <td class="table-cell">${item.project}</td>
                    <td class="table-cell text-center font-semibold">
                        ${formatDate(workDate)}
                    </td>
                    <td class="table-cell text-center">
                        <span class="badge badge-red">Tuần ${item.weekNo}</span>
                    </td>
                    <td class="table-cell text-center font-bold">${item.year}</td>
                    <td class="table-cell text-right">
                        <span class="text-lg font-black text-red-600">${item.workHours || 0}</span>
                        <span class="text-sm text-gray-400 ml-1">giờ</span>
                    </td>
                    <td class="table-cell text-gray-600">${item.createBy}</td>
                    <td class="table-cell text-center text-sm text-gray-500">
                        ${formatDateTime(createDate)}
                    </td>
                </tr>
            `;
        }).join('');
    }

    // Update showing records text
    const showing = pageData.length > 0 
        ? `${start + 1}-${Math.min(end, filteredData.length)} / ${filteredData.length}`
        : '0';
    document.getElementById('showingRecords').textContent = showing;

    // Update pagination
    renderPagination();
}

// Format date
function formatDate(date) {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

// Format datetime
function formatDateTime(date) {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}/${month}/${year} ${hours}:${minutes}`;
}

// Render pagination
function renderPagination() {
    const totalPages = Math.ceil(filteredData.length / recordsPerPage);
    const container = document.getElementById('paginationContainer');
    
    if (totalPages <= 1) {
        container.innerHTML = '';
        return;
    }

    let html = '';

    // Previous button
    html += `
        <button onclick="goToPage(${currentPage - 1})" 
                class="pagination-btn" 
                ${currentPage === 1 ? 'disabled' : ''}>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
        </button>
    `;

    // Page numbers
    const maxVisible = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
    let endPage = Math.min(totalPages, startPage + maxVisible - 1);

    if (endPage - startPage < maxVisible - 1) {
        startPage = Math.max(1, endPage - maxVisible + 1);
    }

    if (startPage > 1) {
        html += `<button onclick="goToPage(1)" class="pagination-btn">1</button>`;
        if (startPage > 2) {
            html += `<span class="pagination-btn" disabled>...</span>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `
            <button onclick="goToPage(${i})" 
                    class="pagination-btn ${i === currentPage ? 'active' : ''}">
                ${i}
            </button>
        `;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            html += `<span class="pagination-btn" disabled>...</span>`;
        }
        html += `<button onclick="goToPage(${totalPages})" class="pagination-btn">${totalPages}</button>`;
    }

    // Next button
    html += `
        <button onclick="goToPage(${currentPage + 1})" 
                class="pagination-btn" 
                ${currentPage === totalPages ? 'disabled' : ''}>
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
        </button>
    `;

    container.innerHTML = html;
}

// Go to specific page
function goToPage(page) {
    const totalPages = Math.ceil(filteredData.length / recordsPerPage);
    if (page < 1 || page > totalPages) return;
    
    currentPage = page;
    displayData();
    
    // Scroll to top of table
    document.querySelector('.bg-white.rounded-\\[2\\.5rem\\]').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Change records per page
function changeRecordsPerPage() {
    recordsPerPage = parseInt(document.getElementById('recordsPerPage').value);
    currentPage = 1;
    displayData();
}

// Export to Excel
function exportToExcel() {
    if (filteredData.length === 0) {
        showError('Không có dữ liệu để xuất');
        return;
    }

    // Prepare data for export
    const exportData = filteredData.map(item => ({
        'STT': filteredData.indexOf(item) + 1,
        'SVN Staff': item.svnStaff,
        'Tên nhân viên': item.nameStaff,
        'Bộ phận': item.department,
        'Dự án': item.project,
        'Ngày làm việc': formatDate(new Date(item.workDate)),
        'Tuần': item.weekNo,
        'Năm': item.year,
        'Giờ làm': item.workHours,
        'Người tạo': item.createBy,
        'Ngày tạo': formatDateTime(new Date(item.createDate))
    }));

    // Create CSV content
    const headers = Object.keys(exportData[0]);
    const csvContent = [
        headers.join(','),
        ...exportData.map(row => headers.map(header => {
            const value = row[header];
            // Escape commas and quotes in values
            if (typeof value === 'string' && (value.includes(',') || value.includes('"'))) {
                return `"${value.replace(/"/g, '""')}"`;
            }
            return value;
        }).join(','))
    ].join('\n');

    // Add BOM for UTF-8
    const BOM = '\uFEFF';
    const blob = new Blob([BOM + csvContent], { type: 'text/csv;charset=utf-8;' });
    
    // Create download link
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `Lich_su_nhap_lieu_${new Date().getTime()}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Show error message
function showError(message) {
    alert(message);
}

// Delete modal functions
function openDeleteModal(id) {
    deleteRecordId = id;
    const modal = document.getElementById('deleteModal');
    modal.classList.remove('hidden');
    modal.classList.add('flex');
}

function closeDeleteModal() {
    const modal = document.getElementById('deleteModal');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
    deleteRecordId = null;
}

async function confirmDelete() {
    if (!deleteRecordId) return;

    try {
        const response = await fetch(`/Heatmap/DeleteStaffDetail/${deleteRecordId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Delete failed');
        }

        const result = await response.json();
        if (result.success) {
            closeDeleteModal();
            loadData(); // Reload data
        } else {
            showError(result.message || 'Xóa thất bại');
        }
    } catch (error) {
        console.error('Error deleting record:', error);
        showError('Không thể xóa bản ghi. Vui lòng thử lại.');
    }
}

// Add event listener for Enter key in search input
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                applyFilters();
            }
        });
    }
});
