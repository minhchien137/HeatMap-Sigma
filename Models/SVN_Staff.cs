using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    [Table("SVN_Staff")]
    public class SVN_Staff
    {
         [Key]
        [Column("IdStaff")]
        public int IdStaff { get; set; } 

        [Required]
        [Column("SVNCode")]
        [StringLength(50)]
        public string SVNCode { get; set; }

        [Required]
        [Column("StaffName")]
        [StringLength(200)]
        public string StaffName { get; set; }

        [Required]
        [Column("Department")]
        [StringLength(100)]
        public string Department { get; set; }

        [Required]
        [Column("Position")]
        [StringLength(50)]
        public string Position { get; set; }

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; }

        [Required]
        [Column("RentalDate")]
        public DateTime RentalDate { get; set; }

        
    }
}