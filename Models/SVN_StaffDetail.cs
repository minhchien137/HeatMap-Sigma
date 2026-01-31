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
        [Column("IdStaff")]
        public int IdStaff { get; set; }

        [Required]
        [Column("ProjectId")]
        public int ProjectId { get; set; }

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

        [Column("Description")]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [Column("CreateBy")]
        [StringLength(50)]
        public string CreateBy { get; set; }

        [Required]
        [Column("CreateDate")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
}