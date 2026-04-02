using Manage_KPI_or_OKR_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Helpers
{
    public class CodeGeneratorHelper
    {
        private readonly MiniERPDbContext _context;

        public CodeGeneratorHelper(MiniERPDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tự động sinh mã nhân viên tiếp theo
        /// Format: EMP + số thứ tự (e.g., EMP001, EMP002, EMP003...)
        /// </summary>
        public async Task<string> GenerateEmployeeCodeAsync()
        {
            try
            {
                // Lấy tất cả mã nhân viên hiện có
                var existingCodes = await _context.Employees
                    .Where(e => e.EmployeeCode != null && e.EmployeeCode.StartsWith("EMP"))
                    .Select(e => e.EmployeeCode)
                    .ToListAsync();

                if (!existingCodes.Any())
                {
                    // Nếu chưa có mã nào, bắt đầu từ EMP001
                    return "EMP001";
                }

                // Trích xuất số từ các mã hiện có
                var numbers = existingCodes
                    .Select(code =>
                    {
                        if (code == null || code.Length <= 3) return 0;
                        // Loại bỏ "EMP" và lấy phần số
                        var numPart = code.Substring(3);
                        return int.TryParse(numPart, out int num) ? num : 0;
                    })
                    .Where(num => num > 0)
                    .ToList();

                // Tìm số lớn nhất và +1
                int nextNumber = (numbers.Any() ? numbers.Max() : 0) + 1;

                // Format theo kiểu 3 chữ số (e.g., EMP001, EMP002, ... EMP999)
                return $"EMP{nextNumber:D3}";
            }
            catch
            {
                // Nếu có lỗi, trả về mã mặc định
                return $"EMP{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        /// <summary>
        /// Tự động sinh mã phòng ban
        /// Format: DEPT + số thứ tự
        /// </summary>
        public async Task<string> GenerateDepartmentCodeAsync()
        {
            try
            {
                var existingCodes = await _context.Departments
                    .Where(d => d.DepartmentCode != null && d.DepartmentCode.StartsWith("DEPT"))
                    .Select(d => d.DepartmentCode)
                    .ToListAsync();

                if (!existingCodes.Any())
                {
                    return "DEPT001";
                }

                var numbers = existingCodes
                    .Select(code =>
                    {
                        if (code == null || code.Length <= 4) return 0;
                        var numPart = code.Substring(4);
                        return int.TryParse(numPart, out int num) ? num : 0;
                    })
                    .Where(num => num > 0)
                    .ToList();

                int nextNumber = (numbers.Any() ? numbers.Max() : 0) + 1;
                return $"DEPT{nextNumber:D3}";
            }
            catch
            {
                return $"DEPT{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        /// <summary>
        /// Tự động sinh mã vị trí
        /// Format: POS + số thứ tự
        /// </summary>
        public async Task<string> GeneratePositionCodeAsync()
        {
            try
            {
                var existingCodes = await _context.Positions
                    .Where(p => p.PositionCode != null && p.PositionCode.StartsWith("POS"))
                    .Select(p => p.PositionCode)
                    .ToListAsync();

                if (!existingCodes.Any())
                {
                    return "POS001";
                }

                var numbers = existingCodes
                    .Select(code =>
                    {
                        if (code == null || code.Length <= 3) return 0;
                        var numPart = code.Substring(3);
                        return int.TryParse(numPart, out int num) ? num : 0;
                    })
                    .Where(num => num > 0)
                    .ToList();

                int nextNumber = (numbers.Any() ? numbers.Max() : 0) + 1;
                return $"POS{nextNumber:D3}";
            }
            catch
            {
                return $"POS{DateTime.Now:yyyyMMddHHmmss}";
            }
        }
    }
}
