using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        [StringLength(20)]
        public string? EmployeeCode { get; set; }
        [StringLength(100)]
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(15)]
        public string? Phone { get; set; }
        [StringLength(255)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string?  TaxCode { get; set; }
        public DateTime? JoinDate { get; set; }
        public int? SystemUserId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
