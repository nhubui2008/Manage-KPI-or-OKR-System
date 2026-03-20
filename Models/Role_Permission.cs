using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manage_KPI_or_OKR_System.Models
{
    public class Role_Permission
    {
        [Key, Column(Order = 0)]
        public int RoleId { get; set; }
        [Key, Column(Order = 1)]
        public int PermissionId { get; set; }
    }
}
