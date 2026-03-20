using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class DeliveryStaff
    {
        [Key]
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        [StringLength(100)]
        public string? AssignedArea { get; set; }
        [StringLength(20)]
        public string? LicensePlate { get; set; }
    }
}
