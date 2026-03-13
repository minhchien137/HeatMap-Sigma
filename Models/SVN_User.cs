using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HeatmapSystem.Models
{
    [Table("SVN_User")]
    public class SVN_User
    {
        [Key]
        [Required]
        [Column("SVNCode")]
        [StringLength(50)]
        public string SVNCode { get; set; }

        [Required]
        [Column("Password")]
        [StringLength(200)]
        public string Password { get; set; }

        [Column("CreateDate")]
        public DateTime? CreateDate { get; set; } = DateTime.Now;

        [Column("LastLogin")]
        public DateTime? LastLogin { get; set; }

        [Column("IsAdmin")]
        public bool IsAdmin { get; set; } = false;

        [Column("IsHR")]
        public bool IsHR { get; set; } = false;

        [Column("Permission")]
        [StringLength(20)]
        public string Permission { get; set; } = "Update";


        /* Helper properties */

        [NotMapped]
        public string RoleName => IsAdmin ? "Admin" : IsHR ? "HR" : "User";

        [NotMapped]
        public bool HasReadPermission => IsAdmin || IsHR || Permission == "Read" || Permission == "Update";

        [NotMapped]
        public bool HasUpdatePermission => IsAdmin || IsHR || Permission == "Update";

        [NotMapped]
        public bool HasNoPermission => !IsAdmin && !IsHR && Permission == "None";

        // HR và Admin đều xem được toàn bộ dữ liệu (không bị lọc bộ phận)
  
        [NotMapped]
        public bool CanViewAllDepartments => IsAdmin || IsHR;
    }
}