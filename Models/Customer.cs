using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string? CustomerCode { get; set; }
        [StringLength(200)]
        public string? CustomerName { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [StringLength(255)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
    }
}
