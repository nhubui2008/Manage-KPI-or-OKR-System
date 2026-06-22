using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionCode { get; set; }

        public int RegistrationId { get; set; }

        public int PackageId { get; set; }

        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } // "Thành công", "Đang xử lý", "Thất bại"

        [ForeignKey("RegistrationId")]
        public virtual PurchaseRegistration Registration { get; set; }

        [ForeignKey("PackageId")]
        public virtual SaaSPackage Package { get; set; }
    }
}
