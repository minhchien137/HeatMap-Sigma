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

        public List<PhaseListDto> GetPhaseList()
        {
            try
            {
                return _context.SVN_StaffDetail
                    .Where(s => s.Phase != null && s.Phase != "")
                    .Select(s => s.Phase)
                    .Distinct()
                    .OrderBy(p => p)
                    .Select(p => new PhaseListDto { name = p })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting phase list");
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
                var (fromDate2, toDate2) = GetDateRange(filter);
                int workingDays = 0;
                for (var d = fromDate2.Date; d <= toDate2.Date; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Sunday) workingDays++;

                return new ReportDataDto
                {
                    kpis = CalculateKPIs(data, fromDate, toDate),
                    trendData = CalculateTrendData(data, "week"),
                    monthlyTrendData = CalculateTrendData(data, "month"),
                    departmentData = CalculateDepartmentData(data),
                    heatmapData = CalculateHeatmapData(data),
                    detailData = CalculateDetailData(data),
                    phaseData = CalculatePhaseData(data),
                    functionData = CalculateFunctionData(data, workingDays)
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

            if (!string.IsNullOrEmpty(filter.Phase))
            {
                query = query.Where(s => s.Phase == filter.Phase);
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

        private KpiDto CalculateKPIs(List<SVN_StaffDetail> data, DateTime fromDate, DateTime toDate)
        {
            var totalHours = data.Sum(s => s.WorkHours ?? 0);
            var staffCount = data.Select(s => s.SVNStaff).Distinct().Count();
            var projectCount = data.Select(s => s.Project).Distinct().Count();

            // Đếm số ngày làm việc thực tế trong khoảng (Thứ 2 -> Thứ 7, bỏ Chủ nhật)
            int workingDays = 0;
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Sunday)
                    workingDays++;
            }

            // Tổng giờ có thể làm = số nhân viên × số ngày làm việc × 8.5h/ngày
            var totalPossibleHours = staffCount * workingDays * 8.5m;
            var avgUtilization = totalPossibleHours > 0 ? (totalHours / totalPossibleHours * 100) : 0;
            return new KpiDto
            {
                totalHours = totalHours,
                avgUtilization = avgUtilization,
                activeProjects = projectCount,
                staffCount = staffCount,
                availableCapacity = totalPossibleHours
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
                        // 1 tuần có 6 ngày làm việc (T2-T7), mỗi ngày 8.5h
                        var possibleHours = staffCount * 6 * 8.5m;
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
                        var workDays = daysInMonth * 6 / 7; // Rough estimate
                        var possibleHours = staffCount * 8.5m * workDays;
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


        // Tổng hợp dữ liệu giờ làm theo Phase và Department
        private List<PhaseDataDto> CalculatePhaseData(List<SVN_StaffDetail> data)
        {
            return data
                .GroupBy(s => new { s.Phase, s.Department })
                .Select(g => new PhaseDataDto
                {
                    phase = g.Key.Phase,
                    department = g.Key.Department,
                    totalHours = g.Sum(s => s.WorkHours ?? 0),
                    staffCount = g.Select(s => s.SVNStaff).Distinct().Count()
                })
                .OrderBy(p => p.phase)
                .ThenBy(p => p.department)
                .ToList();
        }

        private FunctionUtilizationDto CalculateFunctionData(List<SVN_StaffDetail> data, int workingDays)
        {
            // Group by department
            var deptGroups = data
                .GroupBy(s => s.Department)
                .OrderBy(g => g.Key)
                .ToList();

            var departments = new List<string>();
            var availableHrs = new List<decimal>();
            var headCounts = new List<int>();
            var utilizeHours = new List<decimal>();
            var utilizationRates = new List<decimal>();

            foreach (var g in deptGroups)
            {
                var hc = g.Select(s => s.SVNStaff).Distinct().Count();
                var available = hc * workingDays * 8.5m;
                var utilize = g.Sum(s => s.WorkHours ?? 0);
                var rate = available > 0 ? Math.Round(utilize / available * 100, 0) : 0;

                departments.Add(g.Key);
                availableHrs.Add(available);
                headCounts.Add(hc);
                utilizeHours.Add(utilize);
                utilizationRates.Add(rate);
            }

            var totalHC = data.Select(s => s.SVNStaff).Distinct().Count();
            var totalAvailable = totalHC * workingDays * 8.5m;
            var totalUtilize = data.Sum(s => s.WorkHours ?? 0);
            var totalRate = totalAvailable > 0 ? Math.Round(totalUtilize / totalAvailable * 100, 0) : 0;

            return new FunctionUtilizationDto
            {
                departments = departments,
                availableHrs = availableHrs,
                headCount = headCounts,
                utilizeHour = utilizeHours,
                utilizationRate = utilizationRates,
                totalAvailable = totalAvailable,
                totalHC = totalHC,
                totalUtilize = totalUtilize,
                totalRate = totalRate,
                workingDays = workingDays
            };
        }

        #endregion
    }
}