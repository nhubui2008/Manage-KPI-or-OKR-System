using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Helpers;

namespace Manage_KPI_or_OKR_System.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly MiniERPDbContext _context;

        public EmployeesController(MiniERPDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEmployees()
        {
            // Join with EmployeeAssignment to show departments and positions optionally
            var employees = await _context.Employees
                .Where(e => e.IsActive == true)
                .Select(e => new {
                    e.Id,
                    e.EmployeeCode,
                    e.FullName,
                    e.Email,
                    e.Phone,
                    e.SystemUserId,
                    e.JoinDate,
                    e.IsActive
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound(new { message = "Không tìm thấy nhân viên" });
            }

            return employee;
        }

        public class EmployeeCreateDto
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string TaxCode { get; set; }
            public DateTime? JoinDate { get; set; }
            
            // Assignment info
            public int? DepartmentId { get; set; }
            public int? PositionId { get; set; }

            // Auto Generate account
            public bool CreateSystemUser { get; set; } = true;
            public int? DefaultRoleId { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] EmployeeCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Generate Employee Code logic
                var count = await _context.Employees.CountAsync() + 1;
                string newEmpCode = "NV" + count.ToString("D4");

                int? newSystemUserId = null;

                // 1. Generate SystemUser if requested
                if (dto.CreateSystemUser && !string.IsNullOrEmpty(dto.Email))
                {
                    // Check if email already exists
                    if (await _context.SystemUsers.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Email))
                    {
                        return BadRequest(new { message = "Email đã tồn tại trong hệ thống SystemUsers" });
                    }

                    var newUser = new SystemUser
                    {
                        Username = dto.Email,
                        Email = dto.Email,
                        PasswordHash = PasswordHelper.HashPassword("123456"), // Default password
                        RoleId = dto.DefaultRoleId,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };
                    
                    _context.SystemUsers.Add(newUser);
                    await _context.SaveChangesAsync();
                    
                    newSystemUserId = newUser.Id;
                }

                // 2. Create Employee
                var employee = new Employee
                {
                    EmployeeCode = newEmpCode,
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    DateOfBirth = dto.DateOfBirth,
                    TaxCode = dto.TaxCode,
                    JoinDate = dto.JoinDate ?? DateTime.Now,
                    SystemUserId = newSystemUserId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync(); // get Employee.Id

                // 3. Create EmployeeAssignment if dept or position is provided
                if (dto.DepartmentId.HasValue || dto.PositionId.HasValue)
                {
                    var assignment = new EmployeeAssignment
                    {
                        EmployeeId = employee.Id,
                        DepartmentId = dto.DepartmentId,
                        PositionId = dto.PositionId,
                        EffectiveDate = DateTime.Now,
                        IsActive = true
                    };
                    _context.EmployeeAssignments.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo nhân sự", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee employeeDetails)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

            employee.FullName = employeeDetails.FullName ?? employee.FullName;
            employee.Email = employeeDetails.Email ?? employee.Email;
            employee.Phone = employeeDetails.Phone ?? employee.Phone;
            employee.DateOfBirth = employeeDetails.DateOfBirth ?? employee.DateOfBirth;
            employee.TaxCode = employeeDetails.TaxCode ?? employee.TaxCode;
            
            _context.Entry(employee).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

            // Soft delete Employee
            employee.IsActive = false;

            // Soft delete assignments
            var assignments = await _context.EmployeeAssignments.Where(a => a.EmployeeId == id).ToListAsync();
            foreach (var assignment in assignments)
            {
                assignment.IsActive = false;
            }

            // Un-activate system user if linked
            if (employee.SystemUserId.HasValue)
            {
                var user = await _context.SystemUsers.FindAsync(employee.SystemUserId.Value);
                if (user != null)
                {
                    user.IsActive = false;
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
