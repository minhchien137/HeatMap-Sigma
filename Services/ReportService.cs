using HeatmapSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using OfficeOpenXml;

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
                    functionData = CalculateFunctionData(data, workingDays),
                    customerData = CalculateCustomerData(data),
                    detailPivotData = CalculateDetailPivotData(data, fromDate, toDate)
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
                var data = query.ToList();

                int workingDays = 0;
                for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Sunday) workingDays++;

                var kpis        = CalculateKPIs(data, fromDate, toDate);
                var functionData = CalculateFunctionData(data, workingDays);
                var phaseData   = CalculatePhaseData(data);
                var customerData = CalculateCustomerData(data);
                var trendData   = CalculateTrendData(data, "week");
                var deptData    = CalculateDepartmentData(data);
                var pivotData   = CalculateDetailPivotData(data, fromDate, toDate);

                // Filter description
                var filterDesc = new List<string>();
                if (!string.IsNullOrEmpty(filter.TimeRange))  filterDesc.Add($"TG: {filter.TimeRange}");
                if (!string.IsNullOrEmpty(filter.Customer))   filterDesc.Add($"Customer: {filter.Customer}");
                if (!string.IsNullOrEmpty(filter.Department)) filterDesc.Add($"Bộ phận: {filter.Department}");
                if (!string.IsNullOrEmpty(filter.Project))    filterDesc.Add($"Dự án: {filter.Project}");
                if (!string.IsNullOrEmpty(filter.Phase))      filterDesc.Add($"Phase: {filter.Phase}");
                string filterSummary = filterDesc.Count > 0 ? string.Join(" | ", filterDesc) : "Tất cả";

                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using var package = new OfficeOpenXml.ExcelPackage();

                // ── Palette ──────────────────────────────────────────────────
                var darkBg     = System.Drawing.Color.FromArgb(30, 42, 58);
                var redColor   = System.Drawing.Color.FromArgb(229, 62, 62);
                var yellowBg   = System.Drawing.Color.FromArgb(255, 249, 219);
                var lightGray  = System.Drawing.Color.FromArgb(248, 249, 250);
                var borderClr  = System.Drawing.Color.FromArgb(220, 220, 220);
                var white      = System.Drawing.Color.White;

                // ── Helpers ───────────────────────────────────────────────────
                void SetHeader(OfficeOpenXml.ExcelRange c, bool dark = true)
                {
                    c.Style.Font.Bold = true;
                    c.Style.Font.Color.SetColor(dark ? white : darkBg);
                    c.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    c.Style.Fill.BackgroundColor.SetColor(dark ? darkBg : lightGray);
                    c.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    c.Style.VerticalAlignment   = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    c.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                }
                void SetData(OfficeOpenXml.ExcelRange c, bool bold = false, bool center = true, System.Drawing.Color? fc = null)
                {
                    if (bold) c.Style.Font.Bold = true;
                    if (fc.HasValue) c.Style.Font.Color.SetColor(fc.Value);
                    if (center) c.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    c.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    c.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                }
                void SetFill(OfficeOpenXml.ExcelRange c, System.Drawing.Color bg)
                {
                    c.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    c.Style.Fill.BackgroundColor.SetColor(bg);
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 1 – Tổng quan (KPI)
                // ════════════════════════════════════════════════════════════
                var ws1 = package.Workbook.Worksheets.Add("1. Tổng quan");
                ws1.DefaultRowHeight = 18;
                ws1.Cells[1, 1, 1, 8].Merge = true;
                ws1.Cells[1, 1].Value = "BÁO CÁO NĂNG SUẤT NHÂN SỰ";
                ws1.Cells[1, 1].Style.Font.Size = 18; ws1.Cells[1, 1].Style.Font.Bold = true;
                ws1.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws1.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws1.Row(1).Height = 32;

                ws1.Cells[2, 1, 2, 8].Merge = true;
                ws1.Cells[2, 1].Value = $"Thời gian: {fromDate:dd/MM/yyyy} – {toDate:dd/MM/yyyy}    |    {filterSummary}";
                ws1.Cells[2, 1].Style.Font.Italic = true;
                ws1.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(100,100,100));
                ws1.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws1.Row(3).Height = 8;

                // KPI cards
                var kpiLabels = new[] { "ACTUAL HOURS", "AVAILABLE CAPACITY", "HIỆU SUẤT (%)", "DỰ ÁN", "NHÂN SỰ" };
                object[] kpiValues = { kpis.totalHours, kpis.availableCapacity, $"{Math.Round(kpis.avgUtilization,1)}%", kpis.activeProjects, kpis.staffCount };
                int[] kpiStartCols = { 1, 2, 4, 6, 8 };
                int[] kpiSpans     = { 1, 2, 2, 2, 1 };
                for (int k = 0; k < kpiLabels.Length; k++)
                {
                    int c = kpiStartCols[k], s = kpiSpans[k];
                    var lbl = ws1.Cells[4, c, 4, c + s - 1]; lbl.Merge = true;
                    lbl.Value = kpiLabels[k];
                    lbl.Style.Font.Bold = true; lbl.Style.Font.Size = 9;
                    lbl.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(120,120,120));
                    lbl.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    SetFill(lbl, lightGray);
                    lbl.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                    var val = ws1.Cells[5, c, 6, c + s - 1]; val.Merge = true;
                    val.Value = kpiValues[k];
                    val.Style.Font.Bold = true; val.Style.Font.Size = 20;
                    val.Style.Font.Color.SetColor(darkBg);
                    val.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    val.Style.VerticalAlignment   = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    SetFill(val, white);
                    val.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Medium, borderClr);
                }
                ws1.Row(5).Height = 24; ws1.Row(6).Height = 24;
                for (int i = 1; i <= 8; i++) ws1.Column(i).Width = 16;

                // ════════════════════════════════════════════════════════════
                // SHEET 2 – Biểu đồ xu hướng
                // ════════════════════════════════════════════════════════════
                var ws2 = package.Workbook.Worksheets.Add("2. Xu hướng");
                ws2.DefaultRowHeight = 18;
                ws2.Cells[1, 1, 1, 5].Merge = true;
                ws2.Cells[1, 1].Value = "BIỂU ĐỒ XU HƯỚNG – TỔNG GIỜ & HIỆU SUẤT";
                ws2.Cells[1, 1].Style.Font.Size = 14; ws2.Cells[1, 1].Style.Font.Bold = true;
                ws2.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws2.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws2.Row(1).Height = 26;

                SetHeader(ws2.Cells[3, 1]); ws2.Cells[3, 1].Value = "Tuần";
                SetHeader(ws2.Cells[3, 2]); ws2.Cells[3, 2].Value = "Tổng giờ làm việc";
                SetHeader(ws2.Cells[3, 3]); ws2.Cells[3, 3].Value = "Hiệu suất (%)";
                ws2.Column(1).Width = 16; ws2.Column(2).Width = 20; ws2.Column(3).Width = 18;

                for (int i = 0; i < trendData.Count; i++)
                {
                    int row = 4 + i;
                    ws2.Cells[row, 1].Value = trendData[i].label;
                    ws2.Cells[row, 2].Value = (double)trendData[i].hours;
                    ws2.Cells[row, 3].Value = (double)Math.Round(trendData[i].utilization, 1);
                    SetData(ws2.Cells[row, 1], center: false);
                    SetData(ws2.Cells[row, 2]);
                    SetData(ws2.Cells[row, 3]);
                }

                if (trendData.Count > 0)
                {
                    var chart = ws2.Drawings.AddChart("TrendChart", OfficeOpenXml.Drawing.Chart.eChartType.Line) as OfficeOpenXml.Drawing.Chart.ExcelLineChart;
                    if (chart != null)
                    {
                        chart.Title.Text = "Xu hướng giờ làm & Hiệu suất";
                        chart.SetPosition(2, 0, 4, 0); chart.SetSize(620, 340);
                        var s1 = chart.Series.Add(ws2.Cells[4, 2, 3 + trendData.Count, 2], ws2.Cells[4, 1, 3 + trendData.Count, 1]);
                        s1.Header = "Tổng giờ làm việc";
                        var s2 = chart.Series.Add(ws2.Cells[4, 3, 3 + trendData.Count, 3], ws2.Cells[4, 1, 3 + trendData.Count, 1]);
                        s2.Header = "Hiệu suất (%)";
                    }
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 3 – Phân bố theo bộ phận
                // ════════════════════════════════════════════════════════════
                var ws3 = package.Workbook.Worksheets.Add("3. Theo bộ phận");
                ws3.DefaultRowHeight = 18;
                ws3.Cells[1, 1, 1, 4].Merge = true;
                ws3.Cells[1, 1].Value = "PHÂN BỐ THEO BỘ PHẬN";
                ws3.Cells[1, 1].Style.Font.Size = 14; ws3.Cells[1, 1].Style.Font.Bold = true;
                ws3.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws3.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws3.Row(1).Height = 26;

                SetHeader(ws3.Cells[3, 1]); ws3.Cells[3, 1].Value = "Bộ phận";
                SetHeader(ws3.Cells[3, 2]); ws3.Cells[3, 2].Value = "Giờ làm việc";
                SetHeader(ws3.Cells[3, 3]); ws3.Cells[3, 3].Value = "Tỷ lệ %";
                ws3.Column(1).Width = 20; ws3.Column(2).Width = 18; ws3.Column(3).Width = 12;

                decimal totalDeptHrs = deptData.Sum(d => d.hours);
                for (int i = 0; i < deptData.Count; i++)
                {
                    int row = 4 + i;
                    var pct = totalDeptHrs > 0 ? Math.Round(deptData[i].hours / totalDeptHrs * 100, 1) : 0;
                    ws3.Cells[row, 1].Value = deptData[i].department; SetData(ws3.Cells[row, 1], center: false);
                    ws3.Cells[row, 2].Value = (double)deptData[i].hours; SetData(ws3.Cells[row, 2]);
                    ws3.Cells[row, 3].Value = $"{pct}%"; SetData(ws3.Cells[row, 3], fc: redColor);
                }
                int deptTotRow = 4 + deptData.Count;
                ws3.Cells[deptTotRow, 1].Value = "TỔNG"; SetData(ws3.Cells[deptTotRow, 1], bold: true, center: false);
                ws3.Cells[deptTotRow, 2].Value = (double)totalDeptHrs; SetData(ws3.Cells[deptTotRow, 2], bold: true);
                ws3.Cells[deptTotRow, 3].Value = "100%"; SetData(ws3.Cells[deptTotRow, 3], bold: true, fc: redColor);

                if (deptData.Count > 0)
                {
                    var dChart = ws3.Drawings.AddChart("DeptChart", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered) as OfficeOpenXml.Drawing.Chart.ExcelBarChart;
                    if (dChart != null)
                    {
                        dChart.Title.Text = "Phân bố giờ theo bộ phận";
                        dChart.SetPosition(2, 0, 4, 0); dChart.SetSize(500, 300);
                        var ds = dChart.Series.Add(ws3.Cells[4, 2, 3 + deptData.Count, 2], ws3.Cells[4, 1, 3 + deptData.Count, 1]);
                        ds.Header = "Giờ làm việc";
                    }
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 4 – By Function
                // ════════════════════════════════════════════════════════════
                var ws4 = package.Workbook.Worksheets.Add("4. By Function");
                ws4.DefaultRowHeight = 18;
                int fd = functionData.departments.Count;
                ws4.Cells[1, 1, 1, fd + 2].Merge = true;
                ws4.Cells[1, 1].Value = "CÔNG SUẤT THEO BỘ PHẬN (BY FUNCTION)";
                ws4.Cells[1, 1].Style.Font.Size = 14; ws4.Cells[1, 1].Style.Font.Bold = true;
                ws4.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws4.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws4.Row(1).Height = 26;
                ws4.Cells[2, 1].Value = $"Số ngày làm việc: {workingDays} ngày · 8.5h/ngày";
                ws4.Cells[2, 1].Style.Font.Italic = true;
                ws4.Cells[2, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(100,100,100));

                SetHeader(ws4.Cells[4, 1]); ws4.Cells[4, 1].Value = "BY FUNCTION";
                for (int i = 0; i < fd; i++) { SetHeader(ws4.Cells[4, i + 2]); ws4.Cells[4, i + 2].Value = functionData.departments[i]; }
                SetHeader(ws4.Cells[4, fd + 2]); ws4.Cells[4, fd + 2].Value = "TOTAL";

                var funcRowLabels = new[] { "Available hrs", "No. of HC", "Utilize hour", "Utilization rate" };
                for (int r = 0; r < funcRowLabels.Length; r++)
                {
                    int row = 5 + r;
                    ws4.Cells[row, 1].Value = funcRowLabels[r]; ws4.Cells[row, 1].Style.Font.Bold = true;
                    ws4.Cells[row, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                    for (int i = 0; i < fd; i++)
                    {
                        object v = r switch { 0 => (object)(double)functionData.availableHrs[i], 1 => functionData.headCount[i], 2 => (double)functionData.utilizeHour[i], 3 => $"{functionData.utilizationRate[i]}%", _ => "" };
                        ws4.Cells[row, i + 2].Value = v is string sv ? (object)sv : v;
                        SetData(ws4.Cells[row, i + 2]);
                    }
                    object tv = r switch { 0 => (object)(double)functionData.totalAvailable, 1 => functionData.totalHC, 2 => (double)functionData.totalUtilize, 3 => $"{functionData.totalRate}%", _ => "" };
                    ws4.Cells[row, fd + 2].Value = tv is string stv ? (object)stv : tv;
                    SetData(ws4.Cells[row, fd + 2], bold: true);
                }
                ws4.Column(1).Width = 20;
                for (int i = 0; i < fd + 1; i++) ws4.Column(i + 2).Width = 14;

                // Helper data for chart
                if (fd > 0)
                {
                    int cdr = 11;
                    ws4.Cells[cdr, 1].Value = "Dept"; ws4.Cells[cdr, 2].Value = "Available hrs"; ws4.Cells[cdr, 3].Value = "Utilize hour";
                    for (int i = 0; i < fd; i++)
                    {
                        ws4.Cells[cdr + 1 + i, 1].Value = functionData.departments[i];
                        ws4.Cells[cdr + 1 + i, 2].Value = (double)functionData.availableHrs[i];
                        ws4.Cells[cdr + 1 + i, 3].Value = (double)functionData.utilizeHour[i];
                    }
                    var fc = ws4.Drawings.AddChart("FuncChart", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered) as OfficeOpenXml.Drawing.Chart.ExcelBarChart;
                    if (fc != null)
                    {
                        fc.Title.Text = "Available hrs vs Utilize hour theo bộ phận";
                        fc.SetPosition(cdr + fd + 1, 0, 0, 0); fc.SetSize(560, 300);
                        var sa = fc.Series.Add(ws4.Cells[cdr + 1, 2, cdr + fd, 2], ws4.Cells[cdr + 1, 1, cdr + fd, 1]); sa.Header = "Available hrs";
                        var su = fc.Series.Add(ws4.Cells[cdr + 1, 3, cdr + fd, 3], ws4.Cells[cdr + 1, 1, cdr + fd, 1]); su.Header = "Utilize hour";
                    }
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 5 – By Phase
                // ════════════════════════════════════════════════════════════
                var ws5 = package.Workbook.Worksheets.Add("5. By Phase");
                ws5.DefaultRowHeight = 18;
                var phaseDepts = phaseData.Select(p => p.department).Distinct().OrderBy(d => d).ToList();
                var phases     = phaseData.Select(p => p.phase).Distinct().OrderBy(p => p).ToList();
                int pdCols = phaseDepts.Count + 3;
                ws5.Cells[1, 1, 1, pdCols].Merge = true;
                ws5.Cells[1, 1].Value = "PHÂN BỐ GIỜ THEO PHASE (BY PHASE)";
                ws5.Cells[1, 1].Style.Font.Size = 14; ws5.Cells[1, 1].Style.Font.Bold = true;
                ws5.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws5.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws5.Row(1).Height = 26;

                SetHeader(ws5.Cells[3, 1]); ws5.Cells[3, 1].Value = "BY PHASE";
                for (int i = 0; i < phaseDepts.Count; i++) { SetHeader(ws5.Cells[3, i + 2]); ws5.Cells[3, i + 2].Value = phaseDepts[i]; }
                SetHeader(ws5.Cells[3, phaseDepts.Count + 2]); ws5.Cells[3, phaseDepts.Count + 2].Value = "SVN";
                SetHeader(ws5.Cells[3, phaseDepts.Count + 3]); ws5.Cells[3, phaseDepts.Count + 3].Value = "SVN %";
                ws5.Cells[3, phaseDepts.Count + 3].Style.Font.Color.SetColor(redColor);
                ws5.Column(1).Width = 16;
                for (int i = 0; i < phaseDepts.Count + 2; i++) ws5.Column(i + 2).Width = 14;

                decimal totalPhaseHrs = phaseData.Sum(p => p.totalHours);
                int phRow = 4;
                foreach (var ph in phases)
                {
                    ws5.Cells[phRow, 1].Value = ph; ws5.Cells[phRow, 1].Style.Font.Bold = true;
                    ws5.Cells[phRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                    decimal svnTotal = 0;
                    for (int i = 0; i < phaseDepts.Count; i++)
                    {
                        var v = phaseData.FirstOrDefault(p => p.phase == ph && p.department == phaseDepts[i])?.totalHours ?? 0;
                        if (v > 0) ws5.Cells[phRow, i + 2].Value = (double)v;
                        svnTotal += v; SetData(ws5.Cells[phRow, i + 2]);
                    }
                    ws5.Cells[phRow, phaseDepts.Count + 2].Value = (double)svnTotal; SetData(ws5.Cells[phRow, phaseDepts.Count + 2], bold: true);
                    var phPct = totalPhaseHrs > 0 ? Math.Round(svnTotal / totalPhaseHrs * 100, 0) : 0;
                    ws5.Cells[phRow, phaseDepts.Count + 3].Value = $"{phPct}%"; SetData(ws5.Cells[phRow, phaseDepts.Count + 3], fc: redColor);
                    phRow++;
                }
                // Phase total row
                ws5.Cells[phRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                for (int i = 0; i < phaseDepts.Count; i++)
                {
                    var ct = phaseData.Where(p => p.department == phaseDepts[i]).Sum(p => p.totalHours);
                    if (ct > 0) ws5.Cells[phRow, i + 2].Value = (double)ct;
                    SetData(ws5.Cells[phRow, i + 2], bold: true);
                }
                ws5.Cells[phRow, phaseDepts.Count + 2].Value = (double)totalPhaseHrs; SetData(ws5.Cells[phRow, phaseDepts.Count + 2], bold: true);
                ws5.Cells[phRow, phaseDepts.Count + 3].Value = "100%"; SetData(ws5.Cells[phRow, phaseDepts.Count + 3], bold: true, fc: redColor);

                // Pie chart by phase
                if (phases.Count > 0)
                {
                    int pcdr = phRow + 2;
                    ws5.Cells[pcdr, 1].Value = "Phase"; ws5.Cells[pcdr, 2].Value = "Giờ";
                    for (int i = 0; i < phases.Count; i++)
                    {
                        ws5.Cells[pcdr + 1 + i, 1].Value = phases[i];
                        ws5.Cells[pcdr + 1 + i, 2].Value = (double)phaseData.Where(p => p.phase == phases[i]).Sum(p => p.totalHours);
                    }
                    var pc = ws5.Drawings.AddChart("PhaseChart", OfficeOpenXml.Drawing.Chart.eChartType.Pie) as OfficeOpenXml.Drawing.Chart.ExcelPieChart;
                    if (pc != null)
                    {
                        pc.Title.Text = "Phân bố giờ theo Phase";
                        pc.SetPosition(pcdr + phases.Count + 1, 0, 0, 0); pc.SetSize(480, 300);
                        var ps = pc.Series.Add(ws5.Cells[pcdr + 1, 2, pcdr + phases.Count, 2], ws5.Cells[pcdr + 1, 1, pcdr + phases.Count, 1]);
                        ps.Header = "Giờ theo Phase";
                    }
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 6 – By Customer
                // ════════════════════════════════════════════════════════════
                var ws6 = package.Workbook.Worksheets.Add("6. By Customer");
                ws6.DefaultRowHeight = 18;
                var custDepts = customerData.Select(c => c.department).Distinct().OrderBy(d => d).ToList();
                int cdCols = custDepts.Count + 4;
                ws6.Cells[1, 1, 1, cdCols].Merge = true;
                ws6.Cells[1, 1].Value = "TỔNG GIỜ THEO KHÁCH HÀNG (BY CUSTOMER)";
                ws6.Cells[1, 1].Style.Font.Size = 14; ws6.Cells[1, 1].Style.Font.Bold = true;
                ws6.Cells[1, 1].Style.Font.Color.SetColor(darkBg);
                ws6.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                ws6.Row(1).Height = 26;

                SetHeader(ws6.Cells[3, 1]); ws6.Cells[3, 1].Value = "CUSTOMER";
                SetHeader(ws6.Cells[3, 2]); ws6.Cells[3, 2].Value = "PROJECT";
                for (int i = 0; i < custDepts.Count; i++) { SetHeader(ws6.Cells[3, i + 3]); ws6.Cells[3, i + 3].Value = custDepts[i]; }
                SetHeader(ws6.Cells[3, custDepts.Count + 3]); ws6.Cells[3, custDepts.Count + 3].Value = "SVN";
                SetHeader(ws6.Cells[3, custDepts.Count + 4]); ws6.Cells[3, custDepts.Count + 4].Value = "SVN %";
                ws6.Cells[3, custDepts.Count + 4].Style.Font.Color.SetColor(redColor);
                ws6.Column(1).Width = 18; ws6.Column(2).Width = 18;
                for (int i = 0; i < custDepts.Count + 2; i++) ws6.Column(i + 3).Width = 14;

                var custProjects = customerData.Select(c => new { c.customer, c.project }).Distinct()
                    .OrderBy(c => c.customer).ThenBy(c => c.project).ToList();
                decimal grandSvn = customerData.Sum(c => c.totalHours);
                int custRow = 4; string lastCust = "";
                foreach (var cp in custProjects)
                {
                    ws6.Cells[custRow, 1].Value = cp.customer != lastCust ? cp.customer : "";
                    ws6.Cells[custRow, 1].Style.Font.Bold = true;
                    ws6.Cells[custRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                    lastCust = cp.customer;
                    ws6.Cells[custRow, 2].Value = cp.project;
                    ws6.Cells[custRow, 2].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                    decimal rowSvn = 0;
                    for (int i = 0; i < custDepts.Count; i++)
                    {
                        var v = customerData.FirstOrDefault(c => c.customer == cp.customer && c.project == cp.project && c.department == custDepts[i])?.totalHours ?? 0;
                        if (v > 0) ws6.Cells[custRow, i + 3].Value = (double)v;
                        rowSvn += v; SetData(ws6.Cells[custRow, i + 3]);
                    }
                    ws6.Cells[custRow, custDepts.Count + 3].Value = (double)rowSvn; SetData(ws6.Cells[custRow, custDepts.Count + 3], bold: true);
                    var cp2 = grandSvn > 0 ? Math.Round(rowSvn / grandSvn * 100, 0) : 0;
                    ws6.Cells[custRow, custDepts.Count + 4].Value = $"{cp2}%"; SetData(ws6.Cells[custRow, custDepts.Count + 4], fc: redColor);
                    custRow++;
                }
                ws6.Cells[custRow, 1, custRow, 2].Merge = true;
                ws6.Cells[custRow, 1].Value = "TỔNG"; SetData(ws6.Cells[custRow, 1], bold: true);
                for (int i = 0; i < custDepts.Count; i++)
                {
                    var ct = customerData.Where(c => c.department == custDepts[i]).Sum(c => c.totalHours);
                    if (ct > 0) ws6.Cells[custRow, i + 3].Value = (double)ct;
                    SetData(ws6.Cells[custRow, i + 3], bold: true);
                }
                ws6.Cells[custRow, custDepts.Count + 3].Value = (double)grandSvn; SetData(ws6.Cells[custRow, custDepts.Count + 3], bold: true);
                ws6.Cells[custRow, custDepts.Count + 4].Value = "100%"; SetData(ws6.Cells[custRow, custDepts.Count + 4], bold: true, fc: redColor);

                // Bar chart by customer
                if (custProjects.Count > 0)
                {
                    int ccdr = custRow + 2;
                    ws6.Cells[ccdr, 1].Value = "Customer"; ws6.Cells[ccdr, 2].Value = "SVN hrs";
                    var cg = customerData.GroupBy(c => c.customer).Select(g => new { customer = g.Key, total = g.Sum(x => x.totalHours) }).OrderByDescending(x => x.total).ToList();
                    for (int i = 0; i < cg.Count; i++) { ws6.Cells[ccdr + 1 + i, 1].Value = cg[i].customer; ws6.Cells[ccdr + 1 + i, 2].Value = (double)cg[i].total; }
                    var cc = ws6.Drawings.AddChart("CustChart", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered) as OfficeOpenXml.Drawing.Chart.ExcelBarChart;
                    if (cc != null)
                    {
                        cc.Title.Text = "Tổng giờ theo khách hàng";
                        cc.SetPosition(ccdr + cg.Count + 1, 0, 0, 0); cc.SetSize(500, 300);
                        var cs = cc.Series.Add(ws6.Cells[ccdr + 1, 2, ccdr + cg.Count, 2], ws6.Cells[ccdr + 1, 1, ccdr + cg.Count, 1]); cs.Header = "SVN hrs";
                    }
                }

                // ════════════════════════════════════════════════════════════
                // SHEET 7 – Tóm tắt từng nhân viên (Detail Pivot)
                // ════════════════════════════════════════════════════════════
                var ws7 = package.Workbook.Worksheets.Add("7. Tóm tắt nhân viên");
                ws7.DefaultRowHeight = 18;
                ws7.Cells[1, 1].Value = "TÓM TẮT TỪNG NHÂN VIÊN – PHÂN BỐ GIỜ THEO NGÀY";
                ws7.Cells[1, 1].Style.Font.Size = 14; ws7.Cells[1, 1].Style.Font.Bold = true;
                ws7.Cells[1, 1].Style.Font.Color.SetColor(darkBg);

                if (pivotData?.rows?.Count > 0)
                {
                    var dates      = pivotData.dates;
                    var dateLabels = pivotData.dateLabels;
                    var weekLabels = pivotData.weekLabels;
                    var pivotRows  = pivotData.rows;
                    var totalByDate = pivotData.totalByDate;
                    int fixedCols  = 6;

                    // Build week groups (ordered as they appear)
                    var weekGroups = new List<(string week, List<int> idxs)>();
                    foreach (var wk in weekLabels)
                    {
                        if (!weekGroups.Any(g => g.week == wk))
                            weekGroups.Add((wk, weekLabels.Select((w, i) => (w, i)).Where(x => x.w == wk).Select(x => x.i).ToList()));
                    }

                    // ── Row 3: Week group headers ──────────────────────────
                    ws7.Cells[3, 1, 3, fixedCols].Merge = true;
                    SetFill(ws7.Cells[3, 1, 3, fixedCols], lightGray);
                    ws7.Cells[3, 1, 3, fixedCols].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                    int col = fixedCols + 1;
                    foreach (var (wk, idxs) in weekGroups)
                    {
                        ws7.Cells[3, col, 3, col + idxs.Count - 1].Merge = true;
                        ws7.Cells[3, col].Value = wk.ToUpper();
                        SetFill(ws7.Cells[3, col], yellowBg);
                        ws7.Cells[3, col].Style.Font.Bold = true;
                        ws7.Cells[3, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[3, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                        ws7.Cells[3, col + idxs.Count].Value = "AVAILABLE HRS:";
                        SetFill(ws7.Cells[3, col + idxs.Count], yellowBg);
                        ws7.Cells[3, col + idxs.Count].Style.Font.Bold = true;
                        ws7.Cells[3, col + idxs.Count].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[3, col + idxs.Count].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                        ws7.Cells[3, col + idxs.Count + 1].Value = "% SPENT";
                        SetFill(ws7.Cells[3, col + idxs.Count + 1], redColor);
                        ws7.Cells[3, col + idxs.Count + 1].Style.Font.Bold = true;
                        ws7.Cells[3, col + idxs.Count + 1].Style.Font.Color.SetColor(white);
                        ws7.Cells[3, col + idxs.Count + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[3, col + idxs.Count + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                        col += idxs.Count + 2;
                    }

                    // ── Row 4: TOTAL + available hrs per day ───────────────
                    ws7.Cells[4, 1, 4, fixedCols].Merge = true;
                    ws7.Cells[4, 1].Value = "TOTAL :"; ws7.Cells[4, 1].Style.Font.Bold = true;
                    ws7.Cells[4, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                    col = fixedCols + 1;
                    foreach (var (wk, idxs) in weekGroups)
                    {
                        decimal availPerDay = pivotData.availableHrsByDate != null && idxs.Count > 0 ? pivotData.availableHrsByDate[idxs[0]] : 0;
                        decimal weekTotAvail = availPerDay * idxs.Count;
                        decimal weekSpent = idxs.Sum(i => totalByDate.ContainsKey(dates[i]) ? totalByDate[dates[i]] : 0);
                        decimal weekPct = weekTotAvail > 0 ? Math.Round(weekSpent / weekTotAvail * 100, 0) : 0;

                        foreach (var idx in idxs)
                        {
                            ws7.Cells[4, col].Value = (double)availPerDay;
                            SetFill(ws7.Cells[4, col], yellowBg);
                            ws7.Cells[4, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            ws7.Cells[4, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                            col++;
                        }
                        ws7.Cells[4, col].Value = (double)weekTotAvail; ws7.Cells[4, col].Style.Font.Bold = true;
                        SetFill(ws7.Cells[4, col], yellowBg);
                        ws7.Cells[4, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[4, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                        col++;
                        ws7.Cells[4, col].Value = $"{weekPct}%"; ws7.Cells[4, col].Style.Font.Bold = true;
                        ws7.Cells[4, col].Style.Font.Color.SetColor(white);
                        SetFill(ws7.Cells[4, col], redColor);
                        ws7.Cells[4, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[4, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                        col++;
                    }

                    // ── Row 5: Column labels ───────────────────────────────
                    string[] fixedLabels = { "Customer", "Product/Project", "Project Phase", "Phase", "Staff", "Dept" };
                    for (int i = 0; i < fixedLabels.Length; i++) { SetHeader(ws7.Cells[5, i + 1]); ws7.Cells[5, i + 1].Value = fixedLabels[i]; }
                    col = fixedCols + 1;
                    foreach (var (wk, idxs) in weekGroups)
                    {
                        foreach (var idx in idxs)
                        {
                            ws7.Cells[5, col].Value = dateLabels[idx];
                            SetFill(ws7.Cells[5, col], yellowBg);
                            ws7.Cells[5, col].Style.Font.Bold = true;
                            ws7.Cells[5, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            ws7.Cells[5, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                            col++;
                        }
                        ws7.Cells[5, col].Value = "Time Spent (hrs)"; SetHeader(ws7.Cells[5, col]); col++;
                        ws7.Cells[5, col].Value = "% Spent"; SetHeader(ws7.Cells[5, col]);
                        ws7.Cells[5, col].Style.Fill.BackgroundColor.SetColor(redColor); col++;
                    }

                    // ── Data rows ──────────────────────────────────────────
                    int dataRow = 6;
                    foreach (var custGrp in pivotRows.GroupBy(r => r.customer))
                    {
                        int custStart = dataRow;
                        foreach (var projGrp in custGrp.GroupBy(r => new { r.project, r.projectPhase, r.phase }))
                        {
                            int projStart = dataRow;
                            foreach (var row in projGrp)
                            {
                                ws7.Cells[dataRow, 2].Value = row.project;
                                ws7.Cells[dataRow, 3].Value = row.projectPhase;
                                ws7.Cells[dataRow, 4].Value = row.phase;
                                ws7.Cells[dataRow, 5].Value = row.staffName;
                                ws7.Cells[dataRow, 6].Value = row.department;
                                for (int i = 1; i <= 6; i++) ws7.Cells[dataRow, i].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                                col = fixedCols + 1;
                                foreach (var (wk, idxs) in weekGroups)
                                {
                                    decimal availPerDay = pivotData.availableHrsByDate != null && idxs.Count > 0 ? pivotData.availableHrsByDate[idxs[0]] : 0;
                                    decimal weekTotAvail = availPerDay * idxs.Count;
                                    decimal weekRowTot = 0;
                                    foreach (var idx in idxs)
                                    {
                                        var v = row.dailyHours.ContainsKey(dates[idx]) ? row.dailyHours[dates[idx]] : 0;
                                        weekRowTot += v;
                                        if (v > 0) { ws7.Cells[dataRow, col].Value = (double)v; SetFill(ws7.Cells[dataRow, col], System.Drawing.Color.FromArgb(255,253,235)); }
                                        ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                        ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                                        col++;
                                    }
                                    if (weekRowTot > 0) ws7.Cells[dataRow, col].Value = (double)weekRowTot;
                                    ws7.Cells[dataRow, col].Style.Font.Bold = true;
                                    ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                    ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                                    col++;
                                    decimal pct = weekTotAvail > 0 ? Math.Round(weekRowTot / weekTotAvail * 100, 0) : 0;
                                    if (weekRowTot > 0) ws7.Cells[dataRow, col].Value = $"{pct}%";
                                    ws7.Cells[dataRow, col].Style.Font.Color.SetColor(redColor);
                                    ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                    ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                                    col++;
                                }
                                dataRow++;
                            }
                            // Merge project/phase cols if multiple staff
                            if (projGrp.Count() > 1)
                            {
                                for (int mc = 2; mc <= 4; mc++)
                                {
                                    ws7.Cells[projStart, mc, dataRow - 1, mc].Merge = true;
                                    ws7.Cells[projStart, mc].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                                }
                            }
                        }
                        // Merge customer col
                        ws7.Cells[custStart, 1].Value = custGrp.Key; ws7.Cells[custStart, 1].Style.Font.Bold = true;
                        if (dataRow - custStart > 1) { ws7.Cells[custStart, 1, dataRow - 1, 1].Merge = true; ws7.Cells[custStart, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center; }
                    }

                    // ── Footer total row ───────────────────────────────────
                    ws7.Cells[dataRow, 1, dataRow, fixedCols].Merge = true;
                    SetFill(ws7.Cells[dataRow, 1, dataRow, fixedCols], darkBg);
                    ws7.Cells[dataRow, 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);

                    col = fixedCols + 1;
                    foreach (var (wk, idxs) in weekGroups)
                    {
                        decimal availPerDay = pivotData.availableHrsByDate != null && idxs.Count > 0 ? pivotData.availableHrsByDate[idxs[0]] : 0;
                        decimal weekTotAvail = availPerDay * idxs.Count;
                        decimal wkTot = 0;
                        foreach (var idx in idxs)
                        {
                            var v = totalByDate.ContainsKey(dates[idx]) ? totalByDate[dates[idx]] : 0; wkTot += v;
                            if (v > 0) ws7.Cells[dataRow, col].Value = (double)v;
                            ws7.Cells[dataRow, col].Style.Font.Bold = true; ws7.Cells[dataRow, col].Style.Font.Color.SetColor(white);
                            SetFill(ws7.Cells[dataRow, col], darkBg);
                            ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                            col++;
                        }
                        if (wkTot > 0) ws7.Cells[dataRow, col].Value = (double)wkTot;
                        ws7.Cells[dataRow, col].Style.Font.Bold = true; ws7.Cells[dataRow, col].Style.Font.Color.SetColor(white);
                        SetFill(ws7.Cells[dataRow, col], darkBg);
                        ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                        col++;
                        decimal fp = weekTotAvail > 0 ? Math.Round(wkTot / weekTotAvail * 100, 0) : 0;
                        ws7.Cells[dataRow, col].Value = $"{fp}%"; ws7.Cells[dataRow, col].Style.Font.Bold = true; ws7.Cells[dataRow, col].Style.Font.Color.SetColor(white);
                        SetFill(ws7.Cells[dataRow, col], redColor);
                        ws7.Cells[dataRow, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        ws7.Cells[dataRow, col].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin, borderClr);
                        col++;
                    }

                    // Column widths sheet 7
                    ws7.Column(1).Width = 16; ws7.Column(2).Width = 18; ws7.Column(3).Width = 14;
                    ws7.Column(4).Width = 10; ws7.Column(5).Width = 24; ws7.Column(6).Width = 10;
                    int cw = 7;
                    foreach (var (_, idxs) in weekGroups) { foreach (var _ in idxs) { ws7.Column(cw).Width = 8; cw++; } ws7.Column(cw).Width = 16; cw++; ws7.Column(cw).Width = 10; cw++; }
                }

                using var ms = new System.IO.MemoryStream();
                package.SaveAs(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to Excel");
                throw;
            }
        }

        public List<CustomerListDto> GetCustomerList()
        {
            try
            {
                return _context.SVN_StaffDetail
                    .Where(s => s.Customer != null && s.Customer != "")
                    .Select(s => s.Customer)
                    .Distinct()
                    .OrderBy(c => c)
                    .Select(c => new CustomerListDto { name = c })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer list");
                throw;
            }
        }

        public List<CustomerDataDto> GetCustomerData(ReportFilterDto filter)
        {
            try
            {
                var (fromDate, toDate) = GetDateRange(filter);
                var query = BuildBaseQuery(filter, fromDate, toDate);
                var data = query.ToList();
                return CalculateCustomerData(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer data");
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private IQueryable<SVN_StaffDetail> BuildBaseQuery(ReportFilterDto filter, DateTime fromDate, DateTime toDate)
        {
            var query = _context.SVN_StaffDetail
                .Where(s => s.WorkDate >= fromDate && s.WorkDate <= toDate);

            if (!string.IsNullOrEmpty(filter.Customer))
            {
                query = query.Where(s => s.Customer == filter.Customer);
            }

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

        // Tổng hợp dữ liệu giờ làm theo Customer, Project và Department (pivot: Customer×Project = row, Dept = col)
        private List<CustomerDataDto> CalculateCustomerData(List<SVN_StaffDetail> data)
        {
            return data
                .GroupBy(s => new { s.Customer, s.Project, s.Department })
                .Select(g => new CustomerDataDto
                {
                    customer = g.Key.Customer,
                    project = g.Key.Project,
                    department = g.Key.Department,
                    totalHours = g.Sum(s => s.WorkHours ?? 0)
                })
                .OrderBy(c => c.customer)
                .ThenBy(c => c.project)
                .ThenBy(c => c.department)
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

        private DetailPivotDto CalculateDetailPivotData(List<SVN_StaffDetail> data, DateTime fromDate, DateTime toDate)
        {
            // Lấy tất cả ngày làm việc (Thứ 2 - Thứ 7) trong khoảng
            var workDates = new List<DateTime>();
            for (var d = fromDate.Date; d <= toDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Sunday)
                    workDates.Add(d);
            }

            // Lấy danh sách ngày thực sự có dữ liệu (hoặc toàn bộ ngày nếu muốn hiển thị cả ngày trống)
            // Để compact, chỉ lấy ngày có data
            var datesWithData = data.Select(s => s.WorkDate.Date).Distinct().OrderBy(d => d).ToList();
            // Dùng datesWithData nếu có, else workDates
            var displayDates = datesWithData.Count > 0 ? datesWithData : workDates;

            var dateStrings = displayDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var dateLabels = displayDates.Select(d => $"{d.Month}/{d.Day}").ToList();

            // WeekLabel per date: "wkNN"
            var weekLabels = displayDates.Select(d =>
            {
                var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(d);
                return $"wk{weekNum}";
            }).ToList();

            // Available hrs per date (8.5h/ngày mỗi nhân viên — sẽ tính ở frontend từ staffCount)
            // Ở đây trả về số nhân viên unique per week để frontend tính
            var staffPerWeek = data
                .GroupBy(s => s.WeekNo)
                .ToDictionary(g => g.Key, g => g.Select(s => s.SVNStaff).Distinct().Count());

            // Tính available hrs mỗi ngày = staffCount của tuần đó × 8.5
            var availableHrsByDate = displayDates.Select(d =>
            {
                var weekNum = System.Globalization.ISOWeek.GetWeekOfYear(d);
                var staff = staffPerWeek.ContainsKey(weekNum) ? staffPerWeek[weekNum] : 0;
                return (decimal)(staff * 8.5);
            }).ToList();

            // Group data: Customer × Project × ProjectPhase × Phase × SVNStaff × WorkDate
            // Pivot rows: Customer × Project × ProjectPhase × Phase (unique)
            var rowKeys = data
                .Select(s => new { s.Customer, s.Project, s.ProjectPhase, s.Phase, s.SVNStaff, s.NameStaff, s.Department })
                .Distinct()
                .OrderBy(k => k.Customer)
                .ThenBy(k => k.Project)
                .ThenBy(k => k.ProjectPhase)
                .ThenBy(k => k.Phase)
                .ThenBy(k => k.SVNStaff)
                .ToList();

            var rows = rowKeys.Select(key =>
            {
                var rowData = data.Where(s =>
                    s.Customer == key.Customer &&
                    s.Project == key.Project &&
                    s.ProjectPhase == key.ProjectPhase &&
                    s.Phase == key.Phase &&
                    s.SVNStaff == key.SVNStaff).ToList();

                var dailyHours = rowData
                    .GroupBy(s => s.WorkDate.Date)
                    .ToDictionary(
                        g => g.Key.ToString("yyyy-MM-dd"),
                        g => g.Sum(s => s.WorkHours ?? 0)
                    );

                return new DetailPivotRowDto
                {
                    customer = key.Customer,
                    project = key.Project,
                    projectPhase = key.ProjectPhase,
                    phase = key.Phase,
                    staffName = key.NameStaff,
                    svnStaff = key.SVNStaff,
                    department = key.Department,
                    dailyHours = dailyHours,
                    totalHours = rowData.Sum(s => s.WorkHours ?? 0)
                };
            }).ToList();

            // Total per date
            var totalByDate = dateStrings.ToDictionary(
                dateStr => dateStr,
                dateStr => data
                    .Where(s => s.WorkDate.Date.ToString("yyyy-MM-dd") == dateStr)
                    .Sum(s => s.WorkHours ?? 0)
            );

            return new DetailPivotDto
            {
                dates = dateStrings,
                dateLabels = dateLabels,
                weekLabels = weekLabels,
                availableHrsByDate = availableHrsByDate,
                rows = rows,
                totalByDate = totalByDate,
                grandTotal = data.Sum(s => s.WorkHours ?? 0)
            };
        }

        #endregion
    }
}