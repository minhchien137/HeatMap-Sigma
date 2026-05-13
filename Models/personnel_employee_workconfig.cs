using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    [Table("personnel_employee_workconfig")]
    public class personnel_employee_workconfig
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [Column("SMStaff")]
        [StringLength(20)]
        public string SMStaff { get; set; }

        [Required]
        [Column("WorkHoursPerDay")]
        public decimal WorkHoursPerDay { get; set; } = 8.5m;

        [Required]
        [Column("EffectiveFrom")]
        public DateTime EffectiveFrom { get; set; }

        [Column("EffectiveTo")]
        public DateTime? EffectiveTo { get; set; }   // NULL = còn hiệu lực

        [Column("UpdatedBy")]
        [StringLength(50)]
        public string? UpdatedBy { get; set; }

        [Column("UpdatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ── Helper properties ──────────────────────────────

        [NotMapped]
        public bool IsActive => EffectiveTo == null || EffectiveTo >= DateTime.Today;

        public bool IsEffectiveOn(DateTime date) =>
            EffectiveFrom.Date <= date.Date &&
            (EffectiveTo == null || EffectiveTo.Value.Date >= date.Date);
    }
}