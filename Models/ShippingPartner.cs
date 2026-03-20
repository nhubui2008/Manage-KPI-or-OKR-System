using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ShippingPartner
    {
        [Key]
        public int Id { get; set; }
        [StringLength(100)]
        public string? PartnerName { get; set; }
        [StringLength(255)]
        public string? APIEndpoint { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
