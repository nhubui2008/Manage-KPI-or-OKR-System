using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace Manage_KPI_or_OKR_System.Models
{
    public class OKR
    {
        [Key]
        public int Id { get; set; }
        [StringLength(255)]
        public string? ObjectiveName { get; set; }
        public int? OKRTypeId { get; set; }
        [StringLength(50)]
        public string? Cycle { get; set; }
        public int? StatusId { get; set; }
        public bool? IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedById { get; set; }
        
        public virtual ICollection<OKRKeyResult> KeyResults { get; set; } = new HashSet<OKRKeyResult>();

        [NotMapped]
        public decimal TotalProgress
        {
            get
            {
                if (KeyResults == null || !KeyResults.Any()) return 0;
                
                decimal total = 0;
                int count = 0;
                foreach (var kr in KeyResults)
                {
                    if (kr.TargetValue.HasValue && kr.TargetValue.Value > 0)
                    {
                        var krProgress = (kr.CurrentValue ?? 0) / kr.TargetValue.Value * 100;
                        total += krProgress;
                        count++;
                    }
                }
                return count == 0 ? 0 : Math.Round(total / count, 2);
            }
        }
    }
}
