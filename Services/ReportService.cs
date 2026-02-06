using HeatmapSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HeatmapSystem.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Public Methods

        public List<DepartmentListDto> GetDepartmentList()
        {
            try
            {
                return _context.SVN_StaffDetail
                    .Select(s => s.Department)
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => new DepartmentListDto { name = d })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department list");
                throw;
            }
        }

        public List<ProjectListDto> GetProjectList()
        {
            try
            {
                return _context.SVN_StaffDetail
                    .Select(s => s.Project)
                    .Distinct()
                    .OrderBy(p => p)
                    .Select(p => new ProjectListDto { name = p })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project list");
                throw;
            }
        }

        public ReportDataDto GetReportData(ReportFilterDto filter)
        {
            try
            {
                // Calculate date range
                var (fromDate, toDate) = GetDateRange(filter);

                // Build base query
                var query = BuildBaseQuery(filter, fromDate, toDate);

                // Get data
                var data = query.ToList();

                // Build report
                return new ReportDataDto
                {
                    kpis = CalculateKPIs(data),
                    trendData = CalculateTrendData(data, "week"),
                    monthlyTrendData = CalculateTrendData(data, "month"),
                    departmentData = CalculateDepartmentData(data),
                    heatmapData = CalculateHeatmapData(data),
                    detailData = CalculateDetailData(data)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report data");
                throw;
            }
        }

        public List<StaffDetailDto> GetCellStaffDetail(string project, string week, string department)
        {
            try
            {
                // Parse week (format: YYYY-WNN)
                var parts = week.Split('-');
                int year = int.Parse(parts[0]);
                int weekNum = int.Parse(parts[1].Replace("W", ""));

                return _context.SVN_StaffDetail
                    .Where(s => s.Project == project &&
                               s.Department == department &&
                               s.Year == year &&
                               s.WeekNo == weekNum)
                    .GroupBy(s => new { s.SVNStaff, s.NameStaff, s.Department })
                    .Select(g => new StaffDetailDto
                    {
                        name = g.Key.NameStaff,
                        svnStaff = g.Key.SVNStaff,
                        department = g.Key.Department,
                        hours = g.Sum(s => s.WorkHours ?? 0),
                        days = g.Select(s => s.WorkDate.Date).Distinct().Count()
                    })
                    .OrderByDescending(s => s.hours)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cell staff detail for project: {Project}, week: {Week}", project, week);
                throw;
            }
        }

        public List<StaffDetailDto> GetProjectStaffDetail(ReportFilterDto filter, string project, string department)
        {
            try
            {
                var (fromDate, toDate) = GetDateRange(filter);

                return _context.SVN_StaffDetail
                    .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate &&
                               s.Project == project && s.Department == department)
                    .GroupBy(s => new { s.SVNStaff, s.NameStaff, s.Department })
                    .Select(g => new StaffDetailDto
                    {
                        name = g.Key.NameStaff,
                        svnStaff = g.Key.SVNStaff,
                        department = g.Key.Department,
                        hours = g.Sum(s => s.WorkHours ?? 0),
                        days = g.Select(s => s.WorkDate.Date).Distinct().Count()
                    })
                    .OrderByDescending(s => s.hours)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project staff detail for project: {Project}", project);
                throw;
            }
        }

        // Thêm method này vào class ReportService (sau GetProjectStaffDetail)

        public List<StaffDailyDetailDto> GetStaffDailyDetail(ReportFilterDto filter, string project, string department, string svnStaff)
        {
            try
            {
                var (fromDate, toDate) = GetDateRange(filter);

                var dailyData = _context.SVN_StaffDetail
                    .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate &&
                               s.Project == project &&
                               s.Department == department &&
                               s.SVNStaff == svnStaff)
                    .GroupBy(s => s.WorkDate)
                    .Select(g => new
                    {
                        WorkDate = g.Key,
                        Hours = g.Sum(s => s.WorkHours ?? 0),
                        Year = g.First().Year,
                        WeekNo = g.First().WeekNo
                    })
                    .OrderBy(d => d.WorkDate)
                    .ToList();

                // Mảng tên ngày trong tuần
                string[] dayNames = { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy" };

                return dailyData.Select(d => new StaffDailyDetailDto
                {
                    dateFormatted = d.WorkDate.ToString("dd/MM/yyyy"),
                    dayOfWeek = dayNames[(int)d.WorkDate.DayOfWeek],
                    hours = d.Hours,
                    week = $"{d.Year}-W{d.WeekNo:D2}"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff daily detail for svnStaff: {SVNStaff}, project: {Project}", svnStaff, project);
                throw;
            }
        }

        public byte[] ExportReportToCsv(ReportFilterDto filter)
        {
            try
            {
                var (fromDate, toDate) = GetDateRange(filter);
                var query = BuildBaseQuery(filter, fromDate, toDate);
                var data = query.OrderByDescending(s => s.WorkDate).ToList();

                var csv = new StringBuilder();
                csv.AppendLine("STT,SVN Staff,Tên nhân viên,Bộ phận,Dự án,Ngày làm việc,Tuần,Năm,Giờ làm,Người tạo,Ngày tạo");

                int stt = 1;
                foreach (var item in data)
                {
                    csv.AppendLine($"{stt},{item.SVNStaff},{item.NameStaff},{item.Department},{item.Project}," +
                        $"{item.WorkDate:dd/MM/yyyy},{item.WeekNo},{item.Year},{item.WorkHours}," +
                        $"{item.CreateBy},{item.CreateDate:dd/MM/yyyy HH:mm}");
                    stt++;
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to CSV");
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<SVN_StaffDetail> BuildBaseQuery(ReportFilterDto filter, DateTime fromDate, DateTime toDate)
        {
            var query = _context.SVN_StaffDetail
                .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate);

            if (!string.IsNullOrEmpty(filter.Department))
            {
                query = query.Where(s => s.Department == filter.Department);
            }

            if (!string.IsNullOrEmpty(filter.Project))
            {
                query = query.Where(s => s.Project == filter.Project);
            }

            return query;
        }

        private (DateTime fromDate, DateTime toDate) GetDateRange(ReportFilterDto filter)
        {
            var today = DateTime.Today;
            DateTime fromDate, toDate;

            switch (filter.TimeRange)
            {
                case "current_week":
                    fromDate = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
                    toDate = fromDate.AddDays(6); // Sunday
                    break;

                case "last_week":
                    fromDate = today.AddDays(-(int)today.DayOfWeek - 6); // Last Monday
                    toDate = fromDate.AddDays(6); // Last Sunday
                    break;

                case "current_month":
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    break;

                case "last_month":
                    fromDate = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    break;

                case "current_quarter":
                    int quarter = (today.Month - 1) / 3 + 1;
                    fromDate = new DateTime(today.Year, (quarter - 1) * 3 + 1, 1);
                    toDate = fromDate.AddMonths(3).AddDays(-1);
                    break;

                case "current_year":
                    fromDate = new DateTime(today.Year, 1, 1);
                    toDate = new DateTime(today.Year, 12, 31);
                    break;

                case "custom":
                    if (!string.IsNullOrEmpty(filter.StartDate) && !string.IsNullOrEmpty(filter.EndDate))
                    {
                        fromDate = DateTime.Parse(filter.StartDate);
                        toDate = DateTime.Parse(filter.EndDate);
                    }
                    else
                    {
                        fromDate = today.AddMonths(-1);
                        toDate = today;
                    }
                    break;

                default:
                    if (!string.IsNullOrEmpty(filter.Year))
                    {
                        int yearInt = int.Parse(filter.Year);
                        fromDate = new DateTime(yearInt, 1, 1);
                        toDate = new DateTime(yearInt, 12, 31);
                    }
                    else
                    {
                        fromDate = today.AddMonths(-1);
                        toDate = today;
                    }
                    break;
            }

            return (fromDate, toDate);
        }

        private KpiDto CalculateKPIs(List<SVN_StaffDetail> data)
        {
            var totalHours = data.Sum(s => s.WorkHours ?? 0);
            var staffCount = data.Select(s => s.SVNStaff).Distinct().Count();
            var projectCount = data.Select(s => s.Project).Distinct().Count();

            // Calculate average utilization (assuming 40 hours per week per person)
            var weekCount = data.Select(s => s.WeekNo).Distinct().Count();
            var totalPossibleHours = staffCount * 40m * weekCount;
            var avgUtilization = totalPossibleHours > 0 ? (totalHours / totalPossibleHours * 100) : 0;

            return new KpiDto
            {
                totalHours = totalHours,
                avgUtilization = avgUtilization,
                activeProjects = projectCount,
                staffCount = staffCount
            };
        }

        private List<TrendDataDto> CalculateTrendData(List<SVN_StaffDetail> data, string groupBy)
        {
            if (groupBy == "week")
            {
                return data
                    .GroupBy(s => new { s.Year, s.WeekNo })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.WeekNo)
                    .Select(g =>
                    {
                        var totalHours = g.Sum(s => s.WorkHours ?? 0);
                        var staffCount = g.Select(s => s.SVNStaff).Distinct().Count();
                        var possibleHours = staffCount * 40m;
                        var utilization = possibleHours > 0 ? (totalHours / possibleHours * 100) : 0;

                        return new TrendDataDto
                        {
                            label = $"{g.Key.Year}-W{g.Key.WeekNo:D2}",
                            hours = totalHours,
                            utilization = utilization
                        };
                    })
                    .ToList();
            }
            else // month
            {
                return data
                    .GroupBy(s => new { s.WorkDate.Year, s.WorkDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g =>
                    {
                        var totalHours = g.Sum(s => s.WorkHours ?? 0);
                        var staffCount = g.Select(s => s.SVNStaff).Distinct().Count();
                        var daysInMonth = DateTime.DaysInMonth(g.Key.Year, g.Key.Month);
                        var workDays = daysInMonth * 5 / 7; // Rough estimate
                        var possibleHours = staffCount * 8m * workDays;
                        var utilization = possibleHours > 0 ? (totalHours / possibleHours * 100) : 0;

                        return new TrendDataDto
                        {
                            label = $"{g.Key.Year}-{g.Key.Month:D2}",
                            hours = totalHours,
                            utilization = utilization
                        };
                    })
                    .ToList();
            }
        }

        private List<DepartmentDataDto> CalculateDepartmentData(List<SVN_StaffDetail> data)
        {
            return data
                .GroupBy(s => s.Department)
                .Select(g => new DepartmentDataDto
                {
                    department = g.Key,
                    hours = g.Sum(s => s.WorkHours ?? 0)
                })
                .OrderByDescending(d => d.hours)
                .ToList();
        }

        private List<HeatmapDataDto> CalculateHeatmapData(List<SVN_StaffDetail> data)
        {
            return data
                .GroupBy(s => new
                {
                    s.Project,
                    Week = $"{s.Year}-W{s.WeekNo:D2}",
                    s.Department
                })
                .Select(g => new HeatmapDataDto
                {
                    project = g.Key.Project,
                    week = g.Key.Week,
                    department = g.Key.Department,
                    hours = g.Sum(s => s.WorkHours ?? 0),
                    staffCount = g.Select(s => s.SVNStaff).Distinct().Count()
                })
                .OrderBy(h => h.project)
                .ThenBy(h => h.week)
                .ToList();
        }

        private List<DetailDataDto> CalculateDetailData(List<SVN_StaffDetail> data)
        {
            return data
                .GroupBy(s => new { s.Project, s.Department })
                .Select(g => new DetailDataDto
                {
                    project = g.Key.Project,
                    department = g.Key.Department,
                    staffCount = g.Select(s => s.SVNStaff).Distinct().Count(),
                    totalHours = g.Sum(s => s.WorkHours ?? 0)
                })
                .OrderByDescending(d => d.totalHours)
                .ToList();
        }

        #endregion
    }
}
