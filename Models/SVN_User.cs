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


        [Column("Permission")]
        [StringLength(20)]
        public string Permission { get; set; } = "None";


        /* Helper properties*/

        [NotMapped]
        public string RoleName => IsAdmin ? "Admin" : "User";


        [NotMapped]
        public bool HasReadPermission => IsAdmin || Permission == "Read" || Permission == "Update";

        [NotMapped]
        public bool HasUpdatePermission => IsAdmin || Permission == "Update";

         [NotMapped]
        public bool HasNoPermission => !IsAdmin && Permission == "None";
        
    }
}