/**
* i18n.js — HeatmapSystem Internationalization
* Usage: add data-i18n="key" to any element, or data-i18n-placeholder="key" for inputs
* Engine auto-applies on DOMContentLoaded + whenever applyI18n() is called
*/

const I18N = {
    vi: {
        // ── NAV / LAYOUT ──────────────────────────────
        'nav.home':         'Trang chủ',
        'nav.import':       'Nhập dữ liệu',
        'nav.history':      'Lịch sử',
        'nav.report':       'Báo cáo',
        'nav.staff':        'Nhân viên',
        'nav.setting':      'Cài đặt',
        'nav.account':      'Tài khoản',
        'nav.logout':       'Đăng xuất',
        
        // ── HOME ──────────────────────────────────────
        'home.title':       'Heatmap',
        'home.subtitle':    'Hệ thống quản lý nguồn lực dự án',
        'home.import.title':'Nhập dữ liệu',
        'home.import.desc': 'Nhập dữ liệu giờ làm việc của nhân viên',
        'home.history.title':'Lịch sử',
        'home.history.desc':'Xem lịch sử nhập dữ liệu',
        'home.report.title':'Báo cáo',
        'home.report.desc': 'Phân tích và báo cáo nguồn lực',
        'home.staff.title': 'Nhân viên',
        'home.staff.desc':  'Quản lý thông tin nhân viên',
        'home.setting.title':'Cài đặt',
        'home.setting.desc':'Cấu hình hệ thống, tùy chỉnh giao diện',
        'home.account.title':'Tài khoản',
        'home.account.desc':'Quản lý tài khoản người dùng',
        'home.import.desc':  'Thêm dữ liệu giờ làm việc của nhân viên theo dự án',
        'home.stat.employees':'Nhân viên',
        'home.stat.projects':'Dự án',
        'home.stat.hours':   'Giờ làm việc',
        'home.stat.efficiency':'Hiệu suất',
        
        // ── COMMON FILTERS ────────────────────────────
        'filter.title':     'Bộ lọc & Tìm kiếm',
        'filter.dept':      'Bộ phận',
        'filter.dept.all':  '-- Tất cả --',
        'filter.dept.choose':'-- Chọn bộ phận --',
        'filter.dept.first':'-- Chọn bộ phận trước --',
        'filter.customer':  'Customer',
        'filter.customer.all':'Tất cả customer',
        'filter.project':   'Dự án',
        'filter.project.all':'Tất cả dự án',
        'filter.year':      'Năm',
        'filter.year.all':  '-- Tất cả --',
        'filter.week':      'Tuần',
        'filter.week.choose':'-- Chọn tuần --',
        'filter.projphase': 'Project Phase',
        'filter.projphase.all':'-- Tất cả --',
        'filter.search':    'Tìm kiếm',
        'filter.search.ph': 'Tên ...',
        'filter.apply':     'Áp dụng bộ lọc',
        'filter.reset':     'Đặt lại',
        'filter.startdate': 'Từ ngày',
        'filter.enddate':   'Đến ngày',
        'filter.timerange': 'Khoảng thời gian',
        'filter.week.cur':  'Tuần hiện tại',
        'filter.week.last': 'Tuần trước',
        'filter.month.cur': 'Tháng hiện tại',
        'filter.month.last':'Tháng trước',
        'filter.quarter':   'Quý hiện tại',
        'filter.year.cur':  'Năm hiện tại',
        'filter.custom':    'Tùy chỉnh',
        
        // ── IMPORT PAGE ───────────────────────────────
        'import.title':     'Nhập dữ liệu',
        'import.subtitle':  'Heatmap',
        'import.mode1':     'Một người – Một ngày',
        'import.mode2':     'Một người – Nhiều ngày',
        'import.mode3':     'Nhiều người – Nhiều ngày',
        'import.dept':      'Bộ phận',
        'import.name':      'Tên',
        'import.projects':  'Dự án & Giờ làm',
        'import.week':      'Tuần',
        'import.date':      'Ngày',
        'import.save':      'Lưu dữ liệu',
        'import.back':      'Về trang chủ',
        'import.add_project':'+ Thêm dự án',
        'import.bulk.title':'Nhập dữ liệu hàng loạt',
        'import.bulk.subtitle':'Mỗi nhân viên có thể làm nhiều dự án trong một ngày',
        'import.bulk.copy': '📋 Copy ngày đầu',
        'import.bulk.cancel':'Hủy',
        'import.bulk.confirm':'Xác nhận lưu',
        'import.col.customer':'CUSTOMER',
        'import.col.project':'PROJECT',
        'import.col.projphase':'PROJ.PHASE',
        'import.col.hours': 'SỐ GIỜ',
        'import.search_emp':'Tìm nhân viên...',
        'import.select_days':'Chọn ngày',
        'import.no_days':   'Chưa chọn ngày nào',
        'import.enter_proj':'Nhập dự án & giờ làm việc',
        'import.please_dept':'Vui lòng chọn bộ phận',
        'import.please_week':'Vui lòng chọn tuần',
        
        // ── HISTORY PAGE ──────────────────────────────
        'history.title':    'Lịch sử nhập dữ liệu',
        'history.subtitle': 'Xem và quản lý các bản ghi đã nhập vào hệ thống',
        'history.col.svn':  'SVN CODE',
        'history.col.name': 'NHÂN VIÊN',
        'history.col.dept': 'BỘ PHẬN',
        'history.col.customer':'CUSTOMER',
        'history.col.project':'DỰ ÁN',
        'history.col.projphase':'PROJECT PHASE',
        'history.col.year': 'NĂM',
        'history.col.week': 'TUẦN',
        'history.col.date': 'NGÀY LÀM VIỆC',
        'history.col.hours':'GIỜ',
        'history.col.creator':'NGƯỜI TẠO',
        'history.col.created':'NGÀY TẠO',
        'history.col.action':'THAO TÁC',
        'history.total':    'Tổng bản ghi',
        'history.staff':    'Nhân viên',
        'history.project':  'Dự án',
        'history.hours':    'Tổng giờ',
        'history.list':     'Danh sách bản ghi',
        'history.showing':  'Hiển thị',
        'history.records':  'bản ghi',
        'history.per_page': 'Số bản ghi/trang:',
        'history.sort':     'Sắp xếp',
        'history.export':   'Xuất Excel',
        'history.nodata':   'Không tìm thấy dữ liệu',
        'history.nodata.sub':'Thử điều chỉnh bộ lọc của bạn',
        'history.confirm_del':'Xác nhận xóa?',
        'history.del_btn':  'Xóa',
        'history.edit_btn': 'Sửa',
        
        // ── STAFF PAGE ────────────────────────────────
        'staff.title':      'Nhân viên',
        'staff.subtitle':   'Xem và quản lý thông tin nhân viên trong hệ thống',
        'staff.total':      'Tổng nhân viên',
        'staff.list':       'Danh sách nhân viên',
        'staff.col.svn':    'SVN Code',
        'staff.col.name':   'Họ tên',
        'staff.col.dept':   'Bộ phận',
        'staff.col.gender': 'Giới tính',
        'staff.col.dob':    'Ngày sinh',
        'staff.col.start':  'Ngày vào làm',
        'staff.col.city':   'Thành phố',
        'staff.col.status': 'Trạng thái',
        'staff.male':       'Nam',
        'staff.female':     'Nữ',
        'staff.active':     'Đang làm việc',
        'staff.inactive':   'Đã nghỉ việc',
        'staff.nodata':     'Không tìm thấy nhân viên',
        
        // ── REPORT PAGE ───────────────────────────────
        'report.title':     'Báo cáo & Phân tích',
        'report.subtitle':  'Project Resource Capacity & Utilization Report',
        'report.export':    'Xuất báo cáo',
        'report.refresh':   'Làm mới',
        'report.apply':     'Áp dụng',
        'report.kpi.hours': 'Tổng giờ đã làm',
        'report.kpi.avail': 'Tổng giờ có thể làm',
        'report.kpi.util':  'Tỷ lệ sử dụng nguồn lực',
        'report.kpi.proj':  'Dự án đang hoạt động',
        'report.kpi.staff': 'Tổng số nhân viên',
        'report.trend':     'Biểu đồ xu hướng',
        'report.trend.week':'Tuần',
        'report.trend.month':'Tháng',
        'report.dept_chart':'Phân bổ theo bộ phận',
        'report.func.title':'Công suất theo bộ phận (Utilization By Function)',
        'report.func.desc': 'Phân tích công suất sử dụng theo bộ phận',
        'report.func.table':'By Table',
        'report.func.chart':'By Chart',
        'report.phase.title':'Phân bổ giờ theo giai đoạn dự án (By Phase)',
        'report.phase.desc':'Tổng giờ làm việc theo Phase × Bộ phận',
        'report.phase.hours':'By Proj. Phase',
        'report.phase.pct': 'By Proj. Phase %',
        'report.phase.chart':'By Chart',
        'report.cust.title':'Tổng giờ theo khách hàng (Total - By Customer)',
        'report.cust.desc': 'Tổng số giờ theo Customer × Bộ phận',
        'report.cust.table':'By Table',
        'report.cust.chart':'By Chart',
        'report.pivot.title':'Tóm tắt từng nhân viên',
        'report.pivot.desc':'Phân bổ giờ theo ngày — Customer × Project × Phase',
        
        // ── SETTING PAGE ──────────────────────────────
        'setting.title':    'Cài đặt hệ thống',
        'setting.subtitle': 'Tùy chỉnh trải nghiệm của bạn',
        'setting.lang.title':'Ngôn ngữ',
        'setting.lang.desc':'Chọn ngôn ngữ hiển thị cho hệ thống',
        'setting.lang.vi':  'Tiếng Việt',
        'setting.lang.vi.sub':'Vietnamese',
        'setting.lang.en':  'English',
        'setting.lang.en.sub':'Tiếng Anh',
        'setting.theme.title':'Giao diện',
        'setting.theme.desc':'Chọn chủ đề giao diện phù hợp với bạn',
        'setting.theme.light':'Sáng',
        'setting.theme.dark':'Tối',
        'setting.theme.auto':'Tự động',
        'setting.link.home':'Trang chủ',
        'setting.link.home.sub':'Quay về trang chủ',
        'setting.link.report':'Báo cáo',
        'setting.link.report.sub':'Xem báo cáo chi tiết',
        'setting.link.account':'Tài khoản',
        'setting.link.account.sub':'Quản lý tài khoản',
        
        // ── ACCOUNT PAGE ──────────────────────────────
        'account.title':    'Thông tin tài khoản',
        'account.subtitle': 'Quản lý thông tin cá nhân và bảo mật',
        'account.info.title':'Thông tin cá nhân',
        'account.svn':      'Mã SVN',
        'account.name':     'Họ tên',
        'account.dept':     'Bộ phận',
        'account.dob':      'Ngày sinh',
        'account.gender':   'Giới tính',
        'account.city':     'Thành phố',
        'account.join':     'Ngày vào làm',
        'account.status':   'Trạng thái',
        'account.pw.title': 'Thay đổi mật khẩu',
        'account.pw.cur':   'Mật khẩu hiện tại',
        'account.pw.new':   'Mật khẩu mới',
        'account.pw.confirm':'Xác nhận mật khẩu mới',
        'account.pw.save':  'Lưu mật khẩu',
        'account.logout':   'Thoát khỏi hệ thống',
        'account.back':     'Quay lại trang chủ',
        'account.created':   'Ngày tạo',
        'account.last_login':'Đăng nhập lần cuối',
        'account.no_info':   'Chưa có thông tin',
        'account.change_pw': 'Đổi mật khẩu',
        'account.change_pw.sub':'Thay đổi mật khẩu',
        'account.logout.sub':'Thoát khỏi hệ thống',
        'account.back.title':'Trang chủ',
        'account.back.sub':  'Quay lại trang chủ',
        'account.pw.modal.sub':'Vui lòng nhập thông tin bên dưới',
        'account.pw.cur.ph': 'Nhập mật khẩu hiện tại',
        'account.pw.new.ph': 'Nhập mật khẩu mới',
        'account.pw.confirm.ph':'Nhập lại mật khẩu mới',
        'account.pw.cancel': 'Hủy',
        'account.pw.submit': 'Xác nhận',
        'account.svn.label': 'Mã SVN :',
        
        // ── COMMON ────────────────────────────────────
        'common.loading':   'Đang tải...',
        'common.nodata':    'Không tìm thấy dữ liệu',
        'common.confirm':   'Xác nhận?',
        'common.cancel':    'Hủy',
        'common.save':      'Lưu',
        'common.delete':    'Xóa',
        'common.edit':      'Sửa',
        'common.close':     'Đóng',
        'common.back':      'Quay lại',
        'common.export':    'Xuất Excel',
        'common.coming_soon':'(Coming soon)',
        'common.data':      'Dữ liệu',
        'common.total':     'Tổng',
        'common.hours':     'Giờ',
        'common.date':      'Ngày',
        'common.week':      'Tuần',
        'common.year':      'Năm',
        'common.name':      'Tên',
        'common.dept':      'Bộ phận',
        'common.action':    'Thao tác',
        'common.notification':'Thông báo',
    },
    
    en: {
        // ── NAV / LAYOUT ──────────────────────────────
        'nav.home':         'Home',
        'nav.import':       'Import Data',
        'nav.history':      'History',
        'nav.report':       'Reports',
        'nav.staff':        'Staff',
        'nav.setting':      'Settings',
        'nav.account':      'Account',
        'nav.logout':       'Log out',
        
        // ── HOME ──────────────────────────────────────
        'home.title':       'Heatmap',
        'home.subtitle':    'Project Resource Management System',
        'home.import.title':'Import Data',
        'home.import.desc': 'Enter employee working hours',
        'home.history.title':'History',
        'home.history.desc':'View data entry history',
        'home.report.title':'Reports',
        'home.report.desc': 'Analyze and report resources',
        'home.staff.title': 'Staff',
        'home.staff.desc':  'Manage employee information',
        'home.setting.title':'Settings',
        'home.setting.desc':'System configuration & appearance',
        'home.account.title':'Account',
        'home.account.desc':'Manage user account',
        'home.import.desc':  'Add employee work hours by project',
        'home.stat.employees':'Employees',
        'home.stat.projects':'Projects',
        'home.stat.hours':   'Work hours',
        'home.stat.efficiency':'Efficiency',
        
        // ── COMMON FILTERS ────────────────────────────
        'filter.title':     'Filters & Search',
        'filter.dept':      'Department',
        'filter.dept.all':  '-- All --',
        'filter.dept.choose':'-- Select department --',
        'filter.dept.first':'-- Select department first --',
        'filter.customer':  'Customer',
        'filter.customer.all':'All customers',
        'filter.project':   'Project',
        'filter.project.all':'All projects',
        'filter.year':      'Year',
        'filter.year.all':  '-- All --',
        'filter.week':      'Week',
        'filter.week.choose':'-- Select week --',
        'filter.projphase': 'Project Phase',
        'filter.projphase.all':'-- All --',
        'filter.search':    'Search',
        'filter.search.ph': 'Name ...',
        'filter.apply':     'Apply filters',
        'filter.reset':     'Reset',
        'filter.startdate': 'From date',
        'filter.enddate':   'To date',
        'filter.timerange': 'Time range',
        'filter.week.cur':  'Current week',
        'filter.week.last': 'Last week',
        'filter.month.cur': 'Current month',
        'filter.month.last':'Last month',
        'filter.quarter':   'Current quarter',
        'filter.year.cur':  'Current year',
        'filter.custom':    'Custom',
        
        // ── IMPORT PAGE ───────────────────────────────
        'import.title':     'Import Data',
        'import.subtitle':  'Heatmap',
        'import.mode1':     'One person – One day',
        'import.mode2':     'One person – Multiple days',
        'import.mode3':     'Multiple people – Multiple days',
        'import.dept':      'Department',
        'import.name':      'Name',
        'import.projects':  'Projects & Hours',
        'import.week':      'Week',
        'import.date':      'Date',
        'import.save':      'Save data',
        'import.back':      'Back to home',
        'import.add_project':'+ Add project',
        'import.bulk.title':'Bulk data entry',
        'import.bulk.subtitle':'Each employee can work on multiple projects per day',
        'import.bulk.copy': '📋 Copy first day',
        'import.bulk.cancel':'Cancel',
        'import.bulk.confirm':'Confirm & save',
        'import.col.customer':'CUSTOMER',
        'import.col.project':'PROJECT',
        'import.col.projphase':'PROJ.PHASE',
        'import.col.hours': 'HOURS',
        'import.search_emp':'Search employees...',
        'import.select_days':'Select dates',
        'import.no_days':   'No dates selected',
        'import.enter_proj':'Enter projects & working hours',
        'import.please_dept':'Please select a department',
        'import.please_week':'Please select a week',
        
        // ── HISTORY PAGE ──────────────────────────────
        'history.title':    'Data Entry History',
        'history.subtitle': 'View and manage records entered into the system',
        'history.col.svn':  'SVN CODE',
        'history.col.name': 'EMPLOYEE',
        'history.col.dept': 'DEPARTMENT',
        'history.col.customer':'CUSTOMER',
        'history.col.project':'PROJECT',
        'history.col.projphase':'PROJECT PHASE',
        'history.col.year': 'YEAR',
        'history.col.week': 'WEEK',
        'history.col.date': 'WORK DATE',
        'history.col.hours':'HOURS',
        'history.col.creator':'CREATED BY',
        'history.col.created':'CREATED DATE',
        'history.col.action':'ACTION',
        'history.total':    'Total records',
        'history.staff':    'Employees',
        'history.project':  'Projects',
        'history.hours':    'Total hours',
        'history.list':     'Record list',
        'history.showing':  'Showing',
        'history.records':  'records',
        'history.per_page': 'Records per page:',
        'history.sort':     'Sort',
        'history.export':   'Export Excel',
        'history.nodata':   'No data found',
        'history.nodata.sub':'Try adjusting your filters',
        'history.confirm_del':'Confirm delete?',
        'history.del_btn':  'Delete',
        'history.edit_btn': 'Edit',
        
        // ── STAFF PAGE ────────────────────────────────
        'staff.title':      'Staff',
        'staff.subtitle':   'View and manage employee information in the system',
        'staff.total':      'Total employees',
        'staff.list':       'Employee list',
        'staff.col.svn':    'SVN Code',
        'staff.col.name':   'Full name',
        'staff.col.dept':   'Department',
        'staff.col.gender': 'Gender',
        'staff.col.dob':    'Date of birth',
        'staff.col.start':  'Start date',
        'staff.col.city':   'City',
        'staff.col.status': 'Status',
        'staff.male':       'Male',
        'staff.female':     'Female',
        'staff.active':     'Active',
        'staff.inactive':   'Resigned',
        'staff.nodata':     'No employees found',
        
        // ── REPORT PAGE ───────────────────────────────
        'report.title':     'Reports & Analytics',
        'report.subtitle':  'Project Resource Capacity & Utilization Report',
        'report.export':    'Export report',
        'report.refresh':   'Refresh',
        'report.apply':     'Apply',
        'report.kpi.hours': 'Total hours worked',
        'report.kpi.avail': 'Total available hours',
        'report.kpi.util':  'Resource utilization rate',
        'report.kpi.proj':  'Active projects',
        'report.kpi.staff': 'Total employees',
        'report.trend':     'Trend chart',
        'report.trend.week':'Week',
        'report.trend.month':'Month',
        'report.dept_chart':'Distribution by department',
        'report.func.title':'Capacity by department (Utilization By Function)',
        'report.func.desc': 'Capacity utilization analysis by department',
        'report.func.table':'By Table',
        'report.func.chart':'By Chart',
        'report.phase.title':'Hours by project phase (By Phase)',
        'report.phase.desc':'Total hours by Phase × Department',
        'report.phase.hours':'By Proj. Phase',
        'report.phase.pct': 'By Proj. Phase %',
        'report.phase.chart':'By Chart',
        'report.cust.title':'Total hours by customer (Total - By Customer)',
        'report.cust.desc': 'Total hours by Customer × Department',
        'report.cust.table':'By Table',
        'report.cust.chart':'By Chart',
        'report.pivot.title':'Employee summary',
        'report.pivot.desc':'Daily hours breakdown — Customer × Project × Phase',
        
        // ── SETTING PAGE ──────────────────────────────
        'setting.title':    'System Settings',
        'setting.subtitle': 'Customize your experience',
        'setting.lang.title':'Language',
        'setting.lang.desc':'Select the display language for the system',
        'setting.lang.vi':  'Tiếng Việt',
        'setting.lang.vi.sub':'Vietnamese',
        'setting.lang.en':  'English',
        'setting.lang.en.sub':'English',
        'setting.theme.title':'Appearance',
        'setting.theme.desc':'Choose a theme that suits you',
        'setting.theme.light':'Light',
        'setting.theme.dark':'Dark',
        'setting.theme.auto':'Auto',
        'setting.link.home':'Home',
        'setting.link.home.sub':'Back to home',
        'setting.link.report':'Reports',
        'setting.link.report.sub':'View detailed reports',
        'setting.link.account':'Account',
        'setting.link.account.sub':'Manage account',
        
        // ── ACCOUNT PAGE ──────────────────────────────
        'account.title':    'Account Information',
        'account.subtitle': 'Manage personal information and security',
        'account.info.title':'Personal Information',
        'account.svn':      'SVN Code',
        'account.name':     'Full name',
        'account.dept':     'Department',
        'account.dob':      'Date of birth',
        'account.gender':   'Gender',
        'account.city':     'City',
        'account.join':     'Start date',
        'account.status':   'Status',
        'account.pw.title': 'Change password',
        'account.pw.cur':   'Current password',
        'account.pw.new':   'New password',
        'account.pw.confirm':'Confirm new password',
        'account.pw.save':  'Save password',
        'account.logout':   'Sign out',
        'account.back':     'Back to home',
        'account.created':   'Created date',
        'account.last_login':'Last login',
        'account.no_info':   'No information',
        'account.change_pw': 'Change password',
        'account.change_pw.sub':'Update your password',
        'account.logout.sub':'Sign out of the system',
        'account.back.title':'Home',
        'account.back.sub':  'Back to home',
        'account.pw.modal.sub':'Please fill in the information below',
        'account.pw.cur.ph': 'Enter current password',
        'account.pw.new.ph': 'Enter new password',
        'account.pw.confirm.ph':'Re-enter new password',
        'account.pw.cancel': 'Cancel',
        'account.pw.submit': 'Confirm',
        'account.svn.label': 'SVN Code:',
        
        // ── COMMON ────────────────────────────────────
        'common.loading':   'Loading...',
        'common.nodata':    'No data found',
        'common.confirm':   'Confirm?',
        'common.cancel':    'Cancel',
        'common.save':      'Save',
        'common.delete':    'Delete',
        'common.edit':      'Edit',
        'common.close':     'Close',
        'common.back':      'Back to Home',
        'common.export':    'Export Excel',
        'common.coming_soon':'(Coming soon)',
        'common.data':      'Data',
        'common.total':     'Total',
        'common.hours':     'Hours',
        'common.date':      'Date',
        'common.week':      'Week',
        'common.year':      'Year',
        'common.name':      'Name',
        'common.dept':      'Department',
        'common.action':    'Action',
        'common.notification':'Notification',
    }
};

// ─── Engine ───────────────────────────────────────────────────────────────────

/**
* Get current language from localStorage (default 'vi')
*/
function getLang() {
    return localStorage.getItem('heatmap_lang') || 'vi';
}

/**
* Translate a key, fallback to key itself if not found
*/
function t(key) {
    const lang = getLang();
    return (I18N[lang] && I18N[lang][key]) || (I18N['vi'][key]) || key;
}

/**
* Apply translations to all elements with data-i18n / data-i18n-placeholder attributes
*/
function applyI18n() {
    const lang = getLang();
    const dict = I18N[lang] || I18N['vi'];
    
    // Text content
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (dict[key] !== undefined) el.textContent = dict[key];
    });
    
    // Placeholder
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
        const key = el.getAttribute('data-i18n-placeholder');
        if (dict[key] !== undefined) el.placeholder = dict[key];
    });
    
    // Title attribute (tooltip)
    document.querySelectorAll('[data-i18n-title]').forEach(el => {
        const key = el.getAttribute('data-i18n-title');
        if (dict[key] !== undefined) el.title = dict[key];
    });
    
    // Option text (for select elements)
    document.querySelectorAll('option[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if (dict[key] !== undefined) el.textContent = dict[key];
    });
    
    // Update html lang attribute
    document.documentElement.lang = lang;
    
    // Dispatch event so individual pages can react
    document.dispatchEvent(new CustomEvent('i18n:applied', { detail: { lang } }));
}

/**
* Change language, save to localStorage, re-apply
*/
function setLang(lang) {
    localStorage.setItem('heatmap_lang', lang);
    applyI18n();
    
    // Show toast
    const msg = lang === 'vi' ? '🇻🇳 Đã chuyển sang Tiếng Việt' : '🇬🇧 Switched to English';
    showLangToast(msg);
}

function showLangToast(message) {
    // Remove existing toast
    const existing = document.getElementById('i18n-toast');
    if (existing) existing.remove();
    
    const toast = document.createElement('div');
    toast.id = 'i18n-toast';
    toast.style.cssText = `
        position: fixed; top: 1rem; right: 1rem; z-index: 9999;
        background: #fff; border: 2px solid #dcfce7; border-radius: 1rem;
        padding: 0.75rem 1.25rem; box-shadow: 0 10px 40px rgba(0,0,0,0.12);
        display: flex; align-items: center; gap: 0.5rem;
        font-weight: 600; font-size: 0.9rem; color: #166534;
        animation: i18n-slide-in 0.3s ease-out;
    `;
    toast.innerHTML = `
        <svg width="18" height="18" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
        </svg>
        ${message}
    `;
    
    // Add animation keyframes once
    if (!document.getElementById('i18n-style')) {
        const style = document.createElement('style');
        style.id = 'i18n-style';
        style.textContent = `
            @keyframes i18n-slide-in {
                from { opacity:0; transform: translateY(-12px); }
                to   { opacity:1; transform: translateY(0); }
            }
        `;
        document.head.appendChild(style);
    }
    
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(-12px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 2500);
}

// ─── Auto-apply on page load ───────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', applyI18n);
