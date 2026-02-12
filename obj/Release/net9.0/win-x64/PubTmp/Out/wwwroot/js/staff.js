// Global variables
let allData = [];
let filteredData = [];
let currentPage = 1;
let recordsPerPage = 50;
let sortColumn = 'emp_code';
let sortDirection = 'asc';

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    loadData();
});

// Load data from server
async function loadData() {
    try {
        const response = await fetch('/Heatmap/GetStaffData');
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

// Populate filter dropdowns
function populateFilters() {
    // Department filter
    const departments = [...new Set(allData.map(item => item.department))].filter(d => d).sort();
    const departmentSelect = document.getElementById('filterDepartment');
    departments.forEach(dept => {
        const option = document.createElement('option');
        option.value = dept;
        option.textContent = dept;
        departmentSelect.appendChild(option);
    });
}

// Apply filters
function applyFilters() {
    const department = document.getElementById('filterDepartment').value;
    const gender = document.getElementById('filterGender').value;
    const status = document.getElementById('filterStatus').value;
    const searchText = document.getElementById('searchInput').value.toLowerCase();

    filteredData = allData.filter(item => {
        let matches = true;

        if (department && item.department !== department) matches = false;
        if (gender && item.gender !== gender) matches = false;
        if (status !== '' && item.status.toString() !== status) matches = false;
        
        if (searchText) {
            const searchableText = `${item.emp_code} ${item.full_name}`.toLowerCase();
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
    document.getElementById('filterGender').value = '';
    document.getElementById('filterStatus').value = '0';
    document.getElementById('searchInput').value = '';
    
    filteredData = [...allData];
    currentPage = 1;
    updateStats();
    displayData();
}

// Update statistics
function updateStats() {
    // Total employees (filtered)
    document.getElementById('totalEmployees').textContent = filteredData.length;
    
    // Active employees
    const activeCount = filteredData.filter(item => item.status === 0).length;
    document.getElementById('activeEmployees').textContent = activeCount;
    
    // Inactive employees
    const inactiveCount = filteredData.filter(item => item.status === 100).length;
    document.getElementById('inactiveEmployees').textContent = inactiveCount;
    
    // Total departments
    const uniqueDepts = new Set(filteredData.map(item => item.department).filter(d => d)).size;
    document.getElementById('totalDepartments').textContent = uniqueDepts;
}

// Display data in table
function displayData() {
    const tbody = document.getElementById('dataTableBody');
    const mobileContainer = document.getElementById('mobileCardsContainer');
    
    if (!tbody) {
        console.error('Table body element not found');
        return;
    }
    
    // Clear both containers
    tbody.innerHTML = '';
    if (mobileContainer) {
        mobileContainer.innerHTML = '';
    }
    
    if (filteredData.length === 0) {
        const emptyMessage = `
            <div class="text-center py-12 sm:py-20">
                <div class="flex flex-col items-center justify-center">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 sm:h-16 sm:w-16 text-gray-300 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                    </svg>
                    <p class="text-gray-400 text-base sm:text-lg font-bold">Không có dữ liệu</p>
                    <p class="text-gray-400 text-xs sm:text-sm mt-2">Thử điều chỉnh bộ lọc hoặc tìm kiếm</p>
                </div>
            </div>
        `;
        
        tbody.innerHTML = `<tr><td colspan="8">${emptyMessage}</td></tr>`;
        if (mobileContainer) {
            mobileContainer.innerHTML = emptyMessage;
        }
        document.getElementById('showingRecords').textContent = '0';
        updatePagination();
        return;
    }
    
    // Sort data
    const sortedData = [...filteredData].sort((a, b) => {
        let aVal = a[sortColumn];
        let bVal = b[sortColumn];
        
        // Handle null/undefined values
        if (!aVal) aVal = '';
        if (!bVal) bVal = '';
        
        if (sortDirection === 'asc') {
            return aVal > bVal ? 1 : -1;
        } else {
            return aVal < bVal ? 1 : -1;
        }
    });
    
    // Paginate
    const startIndex = (currentPage - 1) * recordsPerPage;
    const endIndex = Math.min(startIndex + recordsPerPage, sortedData.length);
    const pageData = sortedData.slice(startIndex, endIndex);
    
    // Update showing records text
    document.getElementById('showingRecords').textContent = 
        `${startIndex + 1}-${endIndex} trong tổng số ${sortedData.length}`;
    
    // Render desktop table rows
    pageData.forEach((item) => {
        const row = document.createElement('tr');
        row.className = 'hover:bg-gray-50 transition-colors';
        
        const genderText = item.gender === 'M' ? 'Nam' : item.gender === 'F' ? 'Nữ' : '';
        const statusBadge = item.status === 0 
            ? '<span class="badge badge-green">Đang làm việc</span>'
            : '<span class="badge badge-gray">Đã nghỉ việc</span>';
        
        row.innerHTML = `
            <td class="table-cell">
                <span class="px-3 py-1 bg-blue-50 text-blue-600 rounded-lg font-bold text-sm">
                    ${item.emp_code || 'N/A'}
                </span>
            </td>
            <td class="table-cell font-semibold">${item.full_name || 'N/A'}</td>
            <td class="table-cell text-center">
                ${genderText ? `<span class="badge ${item.gender === 'M' ? 'badge-blue' : 'badge-red'}">${genderText}</span>` : 'N/A'}
            </td>
            <td class="table-cell text-center">${formatDate(item.birthday)}</td>
            <td class="table-cell">${item.city || 'N/A'}</td>
            <td class="table-cell text-center">${formatDate(item.hire_date)}</td>
            <td class="table-cell">
                <span class="badge badge-gray">${item.department || 'N/A'}</span>
            </td>
            <td class="table-cell text-center">${statusBadge}</td>
        `;
        
        tbody.appendChild(row);
    });
    
    // Render mobile cards
    if (mobileContainer) {
        pageData.forEach((item) => {
            const genderText = item.gender === 'M' ? 'Nam' : item.gender === 'F' ? 'Nữ' : '';
            const statusBadge = item.status === 0 
                ? '<span class="badge badge-green">Đang làm việc</span>'
                : '<span class="badge badge-gray">Đã nghỉ việc</span>';
            
            const card = document.createElement('div');
            card.className = 'p-4 border-b border-gray-100 hover:bg-gray-50 transition-colors';
            card.innerHTML = `
                <div class="flex items-start justify-between mb-3">
                    <div>
                        <span class="px-2.5 py-1 bg-blue-50 text-blue-600 rounded-lg font-bold text-xs">
                            ${item.emp_code || 'N/A'}
                        </span>
                    </div>
                    ${statusBadge}
                </div>
                <h4 class="font-bold text-gray-900 text-base mb-3">${item.full_name || 'N/A'}</h4>
                <div class="grid grid-cols-2 gap-3 text-sm">
                    <div>
                        <p class="text-xs text-gray-400 font-bold mb-0.5">GIỚI TÍNH</p>
                        <p class="text-gray-700">${genderText || 'N/A'}</p>
                    </div>
                    <div>
                        <p class="text-xs text-gray-400 font-bold mb-0.5">NGÀY SINH</p>
                        <p class="text-gray-700">${formatDate(item.birthday)}</p>
                    </div>
                    <div>
                        <p class="text-xs text-gray-400 font-bold mb-0.5">BỘ PHẬN</p>
                        <p class="text-gray-700">${item.department || 'N/A'}</p>
                    </div>
                    <div>
                        <p class="text-xs text-gray-400 font-bold mb-0.5">THÀNH PHỐ</p>
                        <p class="text-gray-700">${item.city || 'N/A'}</p>
                    </div>
                </div>
            `;
            mobileContainer.appendChild(card);
        });
    }
    
    updatePagination();
}

// Format date for display
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    if (isNaN(date.getTime())) return 'N/A';
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
}

// Update pagination
function updatePagination() {
    const totalPages = Math.ceil(filteredData.length / recordsPerPage);
    const container = document.getElementById('paginationContainer');
    
    if (!container) {
        console.error('Pagination container not found');
        return;
    }
    
    container.innerHTML = '';
    
    if (totalPages <= 1) return;
    
    // Previous button
    const prevBtn = createPaginationButton('‹', currentPage > 1, () => {
        if (currentPage > 1) {
            currentPage--;
            displayData();
        }
    });
    container.appendChild(prevBtn);
    
    // Page numbers
    const maxVisiblePages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    
    if (endPage - startPage + 1 < maxVisiblePages) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }
    
    if (startPage > 1) {
        container.appendChild(createPaginationButton('1', true, () => {
            currentPage = 1;
            displayData();
        }));
        if (startPage > 2) {
            const ellipsis = document.createElement('span');
            ellipsis.textContent = '...';
            ellipsis.className = 'px-2 text-gray-400';
            container.appendChild(ellipsis);
        }
    }
    
    for (let i = startPage; i <= endPage; i++) {
        const btn = createPaginationButton(i, true, () => {
            currentPage = i;
            displayData();
        }, i === currentPage);
        container.appendChild(btn);
    }
    
    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            const ellipsis = document.createElement('span');
            ellipsis.textContent = '...';
            ellipsis.className = 'px-2 text-gray-400';
            container.appendChild(ellipsis);
        }
        container.appendChild(createPaginationButton(totalPages, true, () => {
            currentPage = totalPages;
            displayData();
        }));
    }
    
    // Next button
    const nextBtn = createPaginationButton('›', currentPage < totalPages, () => {
        if (currentPage < totalPages) {
            currentPage++;
            displayData();
        }
    });
    container.appendChild(nextBtn);
}

// Create pagination button
function createPaginationButton(text, enabled, onClick, isActive = false) {
    const btn = document.createElement('button');
    btn.textContent = text;
    btn.className = `pagination-btn ${isActive ? 'active' : ''}`;
    btn.disabled = !enabled;
    if (enabled) {
        btn.onclick = onClick;
    }
    return btn;
}

// Sort table
function sortTable(column) {
    if (sortColumn === column) {
        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        sortColumn = column;
        sortDirection = 'asc';
    }
    displayData();
}

// Change records per page
function changeRecordsPerPage() {
    const select = document.getElementById('recordsPerPage');
    recordsPerPage = parseInt(select.value);
    currentPage = 1;
    displayData();
}

// Show error message
function showError(message) {
    const tbody = document.getElementById('dataTableBody');
    if (!tbody) {
        console.error('Table body element not found');
        return;
    }
    
    tbody.innerHTML = `
        <tr>
            <td colspan="8" class="text-center py-20">
                <div class="flex flex-col items-center justify-center">
                    <svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 text-red-300 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                    </svg>
                    <p class="text-red-400 text-lg font-bold">${message}</p>
                </div>
            </td>
        </tr>
    `;
}

// Export to Excel
function exportToExcel() {
    alert('Chức năng xuất Excel đang được phát triển');
}
