using HeatmapSystem.Models;

namespace HeatmapSystem.Services
{
    public interface IReportService
    {
        /// Lấy danh sách bộ phận
        List<DepartmentListDto> GetDepartmentList();

        /// Lấy danh sách dự án

        List<ProjectListDto> GetProjectList();


        /// Lấy dữ liệu báo cáo tổng hợp

        ReportDataDto GetReportData(ReportFilterDto filter);


        /// Lấy chi tiết nhân viên trong một cell của heatmap
  
        List<StaffDetailDto> GetCellStaffDetail(string project, string week, string department);


        /// Lấy chi tiết nhân viên theo dự án
   
        List<StaffDetailDto> GetProjectStaffDetail(ReportFilterDto filter, string project, string department);

   
        /// Xuất dữ liệu báo cáo sang CSV
        byte[] ExportReportToCsv(ReportFilterDto filter);
    }

    // DTO Classes
    public class DepartmentListDto
    {
        public string name { get; set; }
    }

    public class ProjectListDto
    {
        public string name { get; set; }
    }

    public class ReportFilterDto
    {
        public string TimeRange { get; set; }
        public string Year { get; set; }
        public string Department { get; set; }
        public string Project { get; set; }
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
    }

    public class KpiDto
    {
        public decimal totalHours { get; set; }
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

    public class StaffDetailDto
    {
        public string name { get; set; }
        public string svnStaff { get; set; }
        public string department { get; set; }
        public decimal hours { get; set; }
        public int days { get; set; }
    }
}
