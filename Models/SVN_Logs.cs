using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HeatmapSystem.Models
{
    [Table("SVN_Logs")]
public class SVN_Logs
{
    [Key]
    [Column("IdLogs")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdLogs { get; set; }  // Mapping từ dLogs trong DB

    [Required]
    [StringLength(50)]
    public string SVNCode { get; set; }

    public DateTime TimeAccess { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string ActionType { get; set; }

    [StringLength(255)]
    public string Description { get; set; }
}
}