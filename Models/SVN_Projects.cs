using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    public class SVN_Projects
    {

        [Key]
        [Column("IdProject")]
        public int IdProject { get; set; }

        [Required]
        [Column("NameProject")]
        [StringLength(200)]
        public string NameProject { get; set; }

        [Column("NameCustomer")]
        [StringLength(200)]
        public string NameCustomer { get; set; }
    }
}