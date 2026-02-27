using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    [Table("SVN_StaffDetail")]
    public class SVN_StaffDetail
    {
        [Key]
        [Column("StaffDetailId")]
        public int StaffDetailId { get; set; }

        [Required]
        [Column("SVNStaff")]
        [StringLength(20)]
        public string SVNStaff { get; set; }

        [Required]
        [Column("NameStaff")]
        [StringLength(200)]
        public string NameStaff { get; set; }

        [Required]
        [Column("Department")]
        [StringLength(150)]
        public string Department { get; set; }


        [Required]
        [Column("Customer")]
        [StringLength(100)]
        public string Customer { get; set; }

        [Required]
        [Column("Project")]
        [StringLength(100)]
        public string Project { get; set; }

        [Required]
        [Column("ProjectPhase")]
        [StringLength(10)]
        public string ProjectPhase { get; set; }

        [Required]
        [Column("Phase")]
        [StringLength(10)]
        public string Phase { get; set; }

        [Required]
        [Column("WorkDate")]
        public DateTime WorkDate { get; set; }

        [Required]
        [Column("WeekNo")]
        public int WeekNo { get; set; }

        [Required]
        [Column("Year")]
        public int Year { get; set; }
  
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? WorkHours { get; set; }

        [Required]
        [Column("CreateBy")]
        [StringLength(150)]
        public string CreateBy { get; set; }

        [Required]
        [Column("CreateDate")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}