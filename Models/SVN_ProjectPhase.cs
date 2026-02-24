using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatMap_Sigma.Models
{
    public class SVN_ProjectPhase
    {
        [Key]
        [Column("IdProjectPhase")]
        [StringLength(10)]
        public int IdProjectPhase { get; set; }

        [Required]
        [Column("ProjectPhase")]
        [StringLength(10)]
        public string ProjectPhase { get; set; }
    }
}