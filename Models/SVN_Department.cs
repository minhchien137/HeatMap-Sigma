using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    public class SVN_Department
    {
        [Key]
        [Column("idDepartment")]
        public int idDepartment { get; set; }
        
        [Required]
        [Column("nameDepartment")]
        [StringLength(100)]
        public string nameDepartment { get; set; }
    }
}