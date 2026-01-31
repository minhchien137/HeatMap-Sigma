using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    public class SVN_Projects
    {

        [Key]
        [Column("IdProject")]
        public int IdProject { get; set; }

        public string NameProject { get; set; }

        public string NameCustomer { get; set; }
    }
}