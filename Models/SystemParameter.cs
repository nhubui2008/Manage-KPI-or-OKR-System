using System.ComponentModel.DataAnnotations;

namespace Manage_KPI_or_OKR_System.Models
{
    public class SystemParameter
    {
        [Key]
        public int Id { get; set; }
        [StringLength(50)]
        public string? ParameterCode { get; set; }
        [StringLength(255)]
        public string? Value { get; set; }
        [StringLength(255)]
        public string? Description { get; set; }
        public int? UpdatedById { get; set; }
    }
}
