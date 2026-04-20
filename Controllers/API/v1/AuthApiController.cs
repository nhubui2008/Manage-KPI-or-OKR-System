using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Manage_KPI_or_OKR_System.Controllers.API.v1
{
    [Route("api/v1/[controller]")]
    public class AuthApiController : ApiControllerBase
    {
        private readonly MiniERPDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthApiController(MiniERPDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(400, "Dữ liệu không hợp lệ.", ModelState));
            }

            var user = await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive == true);

            if (user == null || user.PasswordHash == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult(401, "Tên đăng nhập hoặc mật khẩu không chính xác."));
            }

            var roleName = "User";
            if (user.RoleId.HasValue)
            {
                var role = await _context.Roles.FindAsync(user.RoleId);
                if (role != null)
                {
                    roleName = role.RoleName ?? "User";
                }
            }

            var token = GenerateJwtToken(user.Id.ToString(), user.Username ?? "Unknown", roleName);

            return Ok(ApiResponse<object>.OkResult(new
            {
                Token = token,
                UserId = user.Id,
                Username = user.Username,
                Role = roleName
            }, "Đăng nhập thành công."));
        }

        private string GenerateJwtToken(string userId, string username, string roleName)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("SystemUserId", userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, roleName)
            };

            var keyStr = _configuration["Jwt:Key"] ?? "superSecretKey-ReplaceThisInEnv-ManageKpiSystem0123";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ManageKpiOkrSystem",
                audience: _configuration["Jwt:Audience"] ?? "ManageKpiOkrSystemUsers",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
