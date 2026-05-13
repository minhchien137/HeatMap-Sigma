namespace HeatmapSystem.Services
{
    /// <summary>
    /// Cung cấp bản dịch cho các nhãn trong file Excel xuất ra.
    /// Thêm ngôn ngữ mới: bổ sung một block mới vào _dict bên dưới.
    /// Các key được dùng trong ExportReportToCsv qua hàm T(key).
    /// </summary>
    public static class ExcelTranslations
    {
        private static readonly Dictionary<string, Dictionary<string, string>> _dict = new()
        {
            // ══════════════════════════════════════════════════════════════
            // TIẾNG VIỆT (mặc định)
            // ══════════════════════════════════════════════════════════════
            ["vi"] = new()
            {
                // ── Tên sheet ──────────────────────────────────────────────
                ["sheet1"] = "1. Tổng quan",
                ["sheet2"] = "2. Xu hướng",
                ["sheet3"] = "3. Theo bộ phận",
                ["sheet4"] = "4. By Function",
                ["sheet5"] = "5. By Phase",
                ["sheet6"] = "6. By Customer",
                ["sheet7"] = "7. Tóm tắt nhân viên",

                // ── Sheet 1 – Tổng quan ────────────────────────────────────
                ["s1_title"] = "BÁO CÁO NĂNG SUẤT NHÂN SỰ",
                ["s1_time_prefix"] = "Thời gian",
                ["s1_kpi_efficiency"] = "HIỆU SUẤT (%)",
                ["s1_kpi_project"] = "DỰ ÁN",
                ["s1_kpi_staff"] = "NHÂN SỰ",
                ["s1_kpi_actual_hrs"] = "GIỜ THỰC TẾ",
                ["s1_kpi_avail_cap"] = "CÔNG SUẤT KHẢ DỤNG",

                // ── Sheet 2 – Xu hướng ─────────────────────────────────────
                ["s2_title"] = "BIỂU ĐỒ XU HƯỚNG – TỔNG GIỜ & HIỆU SUẤT",
                ["s2_col_week"] = "Tuần",
                ["s2_col_total_hours"] = "Tổng giờ làm việc",
                ["s2_col_efficiency"] = "Hiệu suất (%)",
                ["s2_chart_title"] = "Xu hướng giờ làm & Hiệu suất",
                ["s2_series_hours"] = "Tổng giờ làm việc",
                ["s2_series_efficiency"] = "Hiệu suất (%)",

                // ── Sheet 3 – Theo bộ phận ─────────────────────────────────
                ["s3_title"] = "PHÂN BỐ THEO BỘ PHẬN",
                ["s3_col_dept"] = "Bộ phận",
                ["s3_col_hours"] = "Giờ làm việc",
                ["s3_col_ratio"] = "Tỷ lệ %",
                ["s3_row_total"] = "TỔNG",
                ["s3_chart_title"] = "Phân bố giờ theo bộ phận",
                ["s3_series_hours"] = "Giờ làm việc",

                // ── Sheet 4 – By Function ──────────────────────────────────
                ["s4_title"] = "CÔNG SUẤT THEO BỘ PHẬN (BY FUNCTION)",
                ["s4_working_days"] = "Số ngày làm việc",
                ["s4_days_unit"] = "ngày",
                ["s4_chart_title"] = "Available hrs vs Utilize hour theo bộ phận",

                // ── Sheet 5 – By Phase ─────────────────────────────────────
                ["s5_title"] = "PHÂN BỐ GIỜ THEO PHASE (BY PHASE)",
                ["s5_col_hours"] = "Giờ",
                ["s5_chart_title"] = "Phân bố giờ theo Phase",
                ["s5_series_hours"] = "Giờ theo Phase",

                // ── Sheet 6 – By Customer ──────────────────────────────────
                ["s6_title"] = "TỔNG GIỜ THEO KHÁCH HÀNG (BY CUSTOMER)",
                ["s6_row_total"] = "TỔNG",
                ["s6_chart_title"] = "Tổng giờ theo khách hàng",

                // ── Sheet 7 – Tóm tắt nhân viên ───────────────────────────
                ["s7_title"] = "TÓM TẮT TỪNG NHÂN VIÊN – PHÂN BỐ GIỜ THEO NGÀY",
                ["s7_col_customer"] = "Customer",
                ["s7_col_project"] = "Product/Project",
                ["s7_col_phase"] = "Project Phase",
                ["s7_col_staff"] = "Staff",
                ["s7_col_dept"] = "Dept",
                ["s7_col_time_spent"] = "Time Spent (hrs)",
                ["s7_col_pct_spent"] = "% Spent",
                ["s7_avail_hrs"] = "AVAILABLE HRS:",
                ["s7_pct_spent_header"] = "% SPENT",
                ["s7_total_row"] = "TOTAL :",

                // ── Filter description ─────────────────────────────────────
                ["filter_time"] = "TG",
                ["filter_dept"] = "Bộ phận",
                ["filter_project"] = "Dự án",
                ["filter_all"] = "Tất cả",
            },

            // ══════════════════════════════════════════════════════════════
            // TIẾNG TRUNG (zh)
            // ══════════════════════════════════════════════════════════════
            ["cn"] = new()
            {
                // ── Tên sheet ──────────────────────────────────────────────
                ["sheet1"] = "1. 总览",
                ["sheet2"] = "2. 趋势",
                ["sheet3"] = "3. 按部门",
                ["sheet4"] = "4. 按职能",
                ["sheet5"] = "5. 按阶段",
                ["sheet6"] = "6. 按客户",
                ["sheet7"] = "7. 员工摘要",

                // ── Sheet 1 ────────────────────────────────────────────────
                ["s1_title"] = "人员效率报告",
                ["s1_time_prefix"] = "时间",
                ["s1_kpi_efficiency"] = "效率 (%)",
                ["s1_kpi_project"] = "项目",
                ["s1_kpi_staff"] = "人员",
                ["s1_kpi_actual_hrs"]    = "实际工时",
                ["s1_kpi_avail_cap"]     = "可用产能",


                // ── Sheet 2 ────────────────────────────────────────────────
                ["s2_title"] = "趋势图 – 总工时 & 效率",
                ["s2_col_week"] = "周",
                ["s2_col_total_hours"] = "总工时",
                ["s2_col_efficiency"] = "效率 (%)",
                ["s2_chart_title"] = "工时与效率趋势",
                ["s2_series_hours"] = "总工时",
                ["s2_series_efficiency"] = "效率 (%)",

                // ── Sheet 3 ────────────────────────────────────────────────
                ["s3_title"] = "按部门分布",
                ["s3_col_dept"] = "部门",
                ["s3_col_hours"] = "工作时间",
                ["s3_col_ratio"] = "占比 %",
                ["s3_row_total"] = "合计",
                ["s3_chart_title"] = "各部门工时分布",
                ["s3_series_hours"] = "工作时间",

                // ── Sheet 4 ────────────────────────────────────────────────
                ["s4_title"] = "按职能的产能",
                ["s4_working_days"] = "工作天数",
                ["s4_days_unit"] = "天",
                ["s4_chart_title"] = "各部门可用工时 vs 实际工时",

                // ── Sheet 5 ────────────────────────────────────────────────
                ["s5_title"] = "按阶段分布工时",
                ["s5_col_hours"] = "工时",
                ["s5_chart_title"] = "各阶段工时分布",
                ["s5_series_hours"] = "各阶段工时",

                // ── Sheet 6 ────────────────────────────────────────────────
                ["s6_title"] = "按客户统计总工时",
                ["s6_row_total"] = "合计",
                ["s6_chart_title"] = "各客户总工时",

                // ── Sheet 7 ────────────────────────────────────────────────
                ["s7_title"] = "员工摘要 – 每日工时分布",
                ["s7_col_customer"] = "客户",
                ["s7_col_project"] = "产品/项目",
                ["s7_col_phase"] = "项目阶段",
                ["s7_col_staff"] = "员工",
                ["s7_col_dept"] = "部门",
                ["s7_col_time_spent"] = "实际工时 (hrs)",
                ["s7_col_pct_spent"] = "% 使用",
                ["s7_avail_hrs"] = "可用工时:",
                ["s7_pct_spent_header"] = "% 使用",
                ["s7_total_row"] = "合计 :",

                // ── Filter description ─────────────────────────────────────
                ["filter_time"] = "时间",
                ["filter_dept"] = "部门",
                ["filter_project"] = "项目",
                ["filter_all"] = "全部",
            },

            // ══════════════════════════════════════════════════════════════
            // TIẾNG ANH (en)
            // ══════════════════════════════════════════════════════════════
            ["en"] = new()
            {
                // ── Tên sheet ──────────────────────────────────────────────
                ["sheet1"] = "1. Overview",
                ["sheet2"] = "2. Trend",
                ["sheet3"] = "3. By Department",
                ["sheet4"] = "4. By Function",
                ["sheet5"] = "5. By Phase",
                ["sheet6"] = "6. By Customer",
                ["sheet7"] = "7. Staff Summary",

                // ── Sheet 1 ────────────────────────────────────────────────
                ["s1_title"] = "STAFF PRODUCTIVITY REPORT",
                ["s1_time_prefix"] = "Period",
                ["s1_kpi_efficiency"] = "EFFICIENCY (%)",
                ["s1_kpi_project"] = "PROJECTS",
                ["s1_kpi_staff"] = "STAFF",
                ["s1_kpi_actual_hrs"]    = "ACTUAL HOURS",
                ["s1_kpi_avail_cap"]     = "AVAILABLE CAPACITY",

                // ── Sheet 2 ────────────────────────────────────────────────
                ["s2_title"] = "TREND CHART – TOTAL HOURS & EFFICIENCY",
                ["s2_col_week"] = "Week",
                ["s2_col_total_hours"] = "Total Working Hours",
                ["s2_col_efficiency"] = "Efficiency (%)",
                ["s2_chart_title"] = "Working Hours & Efficiency Trend",
                ["s2_series_hours"] = "Total Working Hours",
                ["s2_series_efficiency"] = "Efficiency (%)",

                // ── Sheet 3 ────────────────────────────────────────────────
                ["s3_title"] = "DISTRIBUTION BY DEPARTMENT",
                ["s3_col_dept"] = "Department",
                ["s3_col_hours"] = "Working Hours",
                ["s3_col_ratio"] = "Ratio %",
                ["s3_row_total"] = "TOTAL",
                ["s3_chart_title"] = "Hours Distribution by Department",
                ["s3_series_hours"] = "Working Hours",

                // ── Sheet 4 ────────────────────────────────────────────────
                ["s4_title"] = "CAPACITY BY FUNCTION",
                ["s4_working_days"] = "Working days",
                ["s4_days_unit"] = "days",
                ["s4_chart_title"] = "Available hrs vs Utilize hour by Department",

                // ── Sheet 5 ────────────────────────────────────────────────
                ["s5_title"] = "HOURS DISTRIBUTION BY PHASE",
                ["s5_col_hours"] = "Hours",
                ["s5_chart_title"] = "Hours Distribution by Phase",
                ["s5_series_hours"] = "Hours by Phase",

                // ── Sheet 6 ────────────────────────────────────────────────
                ["s6_title"] = "TOTAL HOURS BY CUSTOMER",
                ["s6_row_total"] = "TOTAL",
                ["s6_chart_title"] = "Total Hours by Customer",

                // ── Sheet 7 ────────────────────────────────────────────────
                ["s7_title"] = "STAFF SUMMARY – DAILY HOURS DISTRIBUTION",
                ["s7_col_customer"] = "Customer",
                ["s7_col_project"] = "Product/Project",
                ["s7_col_phase"] = "Project Phase",
                ["s7_col_staff"] = "Staff",
                ["s7_col_dept"] = "Dept",
                ["s7_col_time_spent"] = "Time Spent (hrs)",
                ["s7_col_pct_spent"] = "% Spent",
                ["s7_avail_hrs"] = "AVAILABLE HRS:",
                ["s7_pct_spent_header"] = "% SPENT",
                ["s7_total_row"] = "TOTAL :",

                // ── Filter description ─────────────────────────────────────
                ["filter_time"] = "Period",
                ["filter_dept"] = "Dept",
                ["filter_project"] = "Project",
                ["filter_all"] = "All",
            },
        };

        /// <summary>
        /// Lấy chuỗi dịch theo ngôn ngữ và key.
        /// Fallback: nếu không tìm thấy locale → dùng "vi"; nếu không có key → trả key gốc.
        /// </summary>
        public static string Get(string lang, string key)
        {
            var locale = (lang ?? "vi").ToLower().Trim();
            if (!_dict.ContainsKey(locale)) locale = "vi";
            return _dict[locale].TryGetValue(key, out var val) ? val : key;
        }
    }
}