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
        /// <summary>Last meaningful update (progress, KR, allocation, edit). Used for "Mới cập nhật" sort.</summary>
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedById { get; set; }

        public virtual ICollection<OKRKeyResult> KeyResults { get; set; } = new HashSet<OKRKeyResult>();
        public virtual ICollection<WorkProject> WorkProjects { get; set; } = new HashSet<WorkProject>();

        [NotMapped]
        public decimal TotalProgress
        {
            get
            {
                if (KeyResults == null || !KeyResults.Any()) return 0;

                decimal total = 0;
                foreach (var kr in KeyResults)
                {
                    total += kr.Progress;
                }
                return Math.Round(total / KeyResults.Count, 2);
            }
        }
    }
}
