using Microsoft.AspNetCore.Mvc;
using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class EvaluationReportsController : Controller
    {
        private readonly MiniERPDbContext _context;

        public EvaluationReportsController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HasPermission("EVALREPORTS_VIEW")]
        public async Task<IActionResult> Index(int? departmentId, string cycle)
        {
            try
            {
                // Đảm bảo bảng EvaluationReportSummaries tồn tại (fix lỗi schema mismatch)
                await _context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluationReportSummaries') CREATE TABLE EvaluationReportSummaries (Id int IDENTITY(1,1) PRIMARY KEY, DepartmentId int, Cycle nvarchar(50), Content nvarchar(max), UpdatedAt datetime, UpdatedById int);");
            }
            catch { }

            // Default to Sales if not specified (per user request example)
            if (!departmentId.HasValue)
            {
                var salesDept = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName != null && d.DepartmentName.Contains("Sale"));
                departmentId = salesDept?.Id ?? 0;
            }

            if (string.IsNullOrEmpty(cycle))
            {
                cycle = $"Q1-{DateTime.Now.Year}";
            }

            // 1. Get OKRs associated with this department and cycle
            var okrIdsInDept = await _context.OKR_Department_Allocations
                .Where(da => da.DepartmentId == departmentId)
                .Select(da => da.OKRId)
                .ToListAsync();

            var okrs = await _context.OKRs
                .Where(o => okrIdsInDept.Contains(o.Id) && o.Cycle == cycle && o.IsActive == true)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();

            // 2. Get Key Results for these OKRs
            var krs = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            // 3. Get Employee and their Allocations for this department and these OKRs
            var employeesInDeptIds = await _context.EmployeeAssignments
                .Where(ea => ea.DepartmentId == departmentId && ea.IsActive == true)
                .Select(ea => ea.EmployeeId ?? 0)
                .ToListAsync();

            var allocations = await _context.OKR_Employee_Allocations
                .Where(a => okrIds.Contains(a.OKRId) && employeesInDeptIds.Contains(a.EmployeeId))
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => employeesInDeptIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            var failReasons = await _context.FailReasons.ToDictionaryAsync(r => r.Id, r => r.ReasonName);
            var departments = await _context.Departments.ToListAsync();
            var currentDept = await _context.Departments.FindAsync(departmentId);

            var cycles = await _context.OKRs
                .Where(o => !string.IsNullOrEmpty(o.Cycle))
                .Select(o => o.Cycle!)
                .Distinct()
                .ToListAsync();
            
            if (!cycles.Any()) cycles = new List<string> { $"Q1-{DateTime.Now.Year}" };

            ViewBag.OKRs = okrs;
            ViewBag.KRs = krs;
            ViewBag.Employees = employees;
            ViewBag.FailReasons = failReasons;
            ViewBag.Departments = departments;
            ViewBag.Cycles = cycles;
            ViewBag.CurrentDeptId = departmentId;
            ViewBag.CurrentDeptName = currentDept?.DepartmentName ?? "N/A";
            ViewBag.CurrentCycle = cycle;

            // 4. Get existing summary for the director
            var summary = await _context.EvaluationReportSummaries
                .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.Cycle == cycle);
            ViewBag.DirectorSummary = summary?.Content ?? "";

            return View(allocations);
        }

        [HttpPost]
        [HasPermission("EVALREPORTS_EDIT")]
        public async Task<IActionResult> SaveDirectorSummary(int departmentId, string cycle, string content)
        {
            var summary = await _context.EvaluationReportSummaries
                .FirstOrDefaultAsync(s => s.DepartmentId == departmentId && s.Cycle == cycle);

            if (summary == null)
            {
                summary = new EvaluationReportSummary
                {
                    DepartmentId = departmentId,
                    Cycle = cycle,
                    Content = content,
                    UpdatedAt = DateTime.Now
                };
                _context.EvaluationReportSummaries.Add(summary);
            }
            else
            {
                summary.Content = content;
                summary.UpdatedAt = DateTime.Now;
                _context.Update(summary);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Lưu nhận xét thành công!" });
        }
        [HttpGet]
        [HasPermission("EVALREPORTS_VIEW")]
        public async Task<IActionResult> ExportExcel(int? departmentId, string cycle)
        {
            if (!departmentId.HasValue)
            {
                var salesDept = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName != null && d.DepartmentName.Contains("Sale"));
                departmentId = salesDept?.Id ?? 0;
            }

            if (string.IsNullOrEmpty(cycle))
            {
                cycle = $"Q1-{DateTime.Now.Year}";
            }

            var okrIdsInDept = await _context.OKR_Department_Allocations
                .Where(da => da.DepartmentId == departmentId)
                .Select(da => da.OKRId)
                .ToListAsync();

            var okrs = await _context.OKRs
                .Where(o => okrIdsInDept.Contains(o.Id) && o.Cycle == cycle && o.IsActive == true)
                .ToListAsync();

            var okrIds = okrs.Select(o => o.Id).ToList();
            var krs = await _context.OKRKeyResults
                .Where(k => okrIds.Contains(k.OKRId ?? 0))
                .ToListAsync();

            var employeesInDeptIds = await _context.EmployeeAssignments
                .Where(ea => ea.DepartmentId == departmentId && ea.IsActive == true)
                .Select(ea => ea.EmployeeId ?? 0)
                .ToListAsync();

            var allocations = await _context.OKR_Employee_Allocations
                .Where(a => okrIds.Contains(a.OKRId) && employeesInDeptIds.Contains(a.EmployeeId))
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => employeesInDeptIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id);

            var failReasons = await _context.FailReasons.ToDictionaryAsync(r => r.Id, r => r.ReasonName);
            var currentDept = await _context.Departments.FindAsync(departmentId);

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("BaoCaoDanhGia");
                
                // Header Styling
                worksheet.Cells[1, 1].Value = $"BÁO CÁO PHÂN BỔ & ĐÁNH GIÁ CHỈ TIÊU - {currentDept?.DepartmentName?.ToUpper() ?? "N/A"}";
                worksheet.Cells[1, 1, 1, 8].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Chu kỳ: {cycle}";
                worksheet.Cells[2, 1, 2, 8].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[2, 1].Style.Font.Italic = true;

                // Column Headers
                string[] headers = { "Mục tiêu", "Kết quả then chốt", "Người phụ trách", "Chỉ tiêu", "Thực tế", "Hoàn thành", "Đánh giá", "Lý do" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[4, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 51, 153)); // Dark Blue
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                int row = 5;
                foreach (var okr in okrs)
                {
                    var okrKrs = krs.Where(k => k.OKRId == okr.Id).ToList();
                    foreach (var kr in okrKrs)
                    {
                        var krAllocations = allocations.Where(a => a.OKRId == okr.Id).ToList();
                        foreach (var alloc in krAllocations)
                        {
                            var emp = employees.ContainsKey(alloc.EmployeeId) ? employees[alloc.EmployeeId] : null;
                            decimal progress = ProgressHelper.CalculateProgress(kr.CurrentValue ?? 0, alloc.AllocatedValue ?? 1, kr.IsInverse);
                            string status = ProgressHelper.GetResultStatus(progress);

                            worksheet.Cells[row, 1].Value = okr.ObjectiveName;
                            worksheet.Cells[row, 2].Value = kr.KeyResultName;
                            worksheet.Cells[row, 3].Value = emp?.FullName ?? "N/A";
                            worksheet.Cells[row, 4].Value = alloc.AllocatedValue;
                            worksheet.Cells[row, 5].Value = kr.CurrentValue;
                            worksheet.Cells[row, 6].Value = progress / 100.0m;
                            worksheet.Cells[row, 6].Style.Numberformat.Format = "0%";
                            worksheet.Cells[row, 7].Value = status;
                            worksheet.Cells[row, 8].Value = kr.FailReasonId.HasValue && failReasons.ContainsKey(kr.FailReasonId.Value) ? failReasons[kr.FailReasonId.Value] : (progress < 100 ? "Chưa đạt / Đang cập nhật" : "-");

                            // Align center for numeric columns
                            worksheet.Cells[row, 4, row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                            worksheet.Cells[row, 7].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                            for (int j = 1; j <= 8; j++)
                            {
                                worksheet.Cells[row, j].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                            }
                            row++;
                        }
                    }
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"BaoCao_OKR_KPI_{currentDept?.DepartmentCode}_{cycle}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}
