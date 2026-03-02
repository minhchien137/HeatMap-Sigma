using HeatmapSystem.Models;

namespace HeatmapSystem.Services
{
    public interface IReportService
    {
        /// Lấy danh sách bộ phận
        List<DepartmentListDto> GetDepartmentList();

        /// Lấy danh sách dự án

        List<ProjectListDto> GetProjectList();

        /// Lấy danh sách phase
        List<PhaseListDto> GetPhaseList();


        /// Lấy dữ liệu báo cáo tổng hợp

        ReportDataDto GetReportData(ReportFilterDto filter);


        /// Lấy chi tiết nhân viên trong một cell của heatmap
  
        List<StaffDetailDto> GetCellStaffDetail(string project, string week, string department);


        /// Lấy chi tiết nhân viên theo dự án

        List<StaffDetailDto> GetProjectStaffDetail(ReportFilterDto filter, string project, string department);
        

         /// Lấy chi tiết theo ngày của nhân viên
        List<StaffDailyDetailDto> GetStaffDailyDetail(ReportFilterDto filter, string project, string department, string svnStaff);

   
        /// Xuất dữ liệu báo cáo sang CSV
        byte[] ExportReportToCsv(ReportFilterDto filter);

        /// Lấy danh sách customer
        List<CustomerListDto> GetCustomerList();

        /// Lấy dữ liệu tổng giờ theo Customer
        List<CustomerDataDto> GetCustomerData(ReportFilterDto filter);
    }

    // DTO Classes
    public class CustomerListDto
    {
        public string name { get; set; }
    }

    public class CustomerDataDto
    {
        public string customer { get; set; }
        public string project { get; set; }
        public string department { get; set; }
        public decimal totalHours { get; set; }
    }

    // DTO Classes
    public class DepartmentListDto
    {
        public string name { get; set; }
    }

    public class StaffDailyDetailDto
    {
        public string dateFormatted { get; set; }
        public string dayOfWeek { get; set; }
        public decimal hours { get; set; }
        public string week { get; set; }
    }
    

    public class ProjectListDto
    {
        public string name { get; set; }
    }

    public class PhaseListDto
    {
        public string name { get; set; }
    }

    public class ReportFilterDto
    {
        public string TimeRange { get; set; }
        public string Year { get; set; }
        public string Customer { get; set; }
        public string Department { get; set; }
        public string Project { get; set; }
        public string Phase { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

    public class ReportDataDto
    {
        public KpiDto kpis { get; set; }
        public List<TrendDataDto> trendData { get; set; }
        public List<TrendDataDto> monthlyTrendData { get; set; }
        public List<DepartmentDataDto> departmentData { get; set; }
        public List<HeatmapDataDto> heatmapData { get; set; }
        public List<DetailDataDto> detailData { get; set; }
        public List<PhaseDataDto> phaseData { get; set; }
        public FunctionUtilizationDto functionData { get; set; }
        public List<CustomerDataDto> customerData { get; set; }
        public DetailPivotDto detailPivotData { get; set; }
    }

    public class FunctionUtilizationDto
    {
        public List<string> departments { get; set; }
        public List<decimal> availableHrs { get; set; }
        public List<int> headCount { get; set; }
        public List<decimal> utilizeHour { get; set; }
        public List<decimal> utilizationRate { get; set; }
        public decimal totalAvailable { get; set; }
        public int totalHC { get; set; }
        public decimal totalUtilize { get; set; }
        public decimal totalRate { get; set; }
        public int workingDays { get; set; }
    }

    public class PhaseDataDto
    {
        public string phase { get; set; }
        public string department { get; set; }
        public decimal totalHours { get; set; }
        public int staffCount { get; set; }
    }

    public class KpiDto
    {
        public decimal totalHours { get; set; }
        public decimal availableCapacity { get; set; }
        public decimal avgUtilization { get; set; }
        public int activeProjects { get; set; }
        public int staffCount { get; set; }
    }

    public class TrendDataDto
    {
        public string label { get; set; }
        public decimal hours { get; set; }
        public decimal utilization { get; set; }
    }

    public class DepartmentDataDto
    {
        public string department { get; set; }
        public decimal hours { get; set; }
    }

    public class HeatmapDataDto
    {
        public string project { get; set; }
        public string week { get; set; }
        public string department { get; set; }
        public decimal hours { get; set; }
        public int staffCount { get; set; }
    }

    public class DetailDataDto
    {
        public string project { get; set; }
        public string department { get; set; }
        public int staffCount { get; set; }
        public decimal totalHours { get; set; }
    }

    // Pivot detail: mỗi row = Customer × Project × ProjectPhase × Phase, mỗi ô = giờ theo ngày
    public class DetailPivotRowDto
    {
        public string customer { get; set; }
        public string project { get; set; }
        public string projectPhase { get; set; }
        public string phase { get; set; }
        public string staffName { get; set; }
        public string svnStaff { get; set; }
        public string department { get; set; }
        // key = "yyyy-MM-dd", value = total hours
        public Dictionary<string, decimal> dailyHours { get; set; }
        public decimal totalHours { get; set; }
    }

    public class DetailPivotDto
    {
        // Danh sách ngày làm việc trong khoảng lọc (sorted)
        public List<string> dates { get; set; }           // ["2026-05-19", ...]
        public List<string> dateLabels { get; set; }      // ["5/19", ...]
        public List<string> weekLabels { get; set; }      // ["wk21", "wk21", ...]  per date
        public List<decimal> availableHrsByDate { get; set; } // available hrs per week
        // Rows
        public List<DetailPivotRowDto> rows { get; set; }
        // Totals per date
        public Dictionary<string, decimal> totalByDate { get; set; }
        public decimal grandTotal { get; set; }
    }

    public class StaffDetailDto
    {
        public string name { get; set; }
        public string svnStaff { get; set; }
        public string department { get; set; }
        public decimal hours { get; set; }
        public int days { get; set; }
    }

    
}