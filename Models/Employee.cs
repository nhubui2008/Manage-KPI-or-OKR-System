using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? EmployeeCode { get; set; }
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100)]
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số điện thoại phải là số.")]
        [StringLength(15)]
        public string? Phone { get; set; }
        [Required(ErrorMessage = "Email không được để trống.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "Email phải đúng định dạng @.com")]
        [StringLength(255)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string?  TaxCode { get; set; }
        public DateTime? JoinDate { get; set; }
        public int? SystemUserId { get; set; }
        public bool? IsActive { get; set; } = true;
        public int? StrategicGoalId { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
