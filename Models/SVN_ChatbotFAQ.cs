using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
    [Table("SVN_ChatbotFAQ")]
    public class SVN_ChatbotFAQ
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Question { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Answer { get; set; }

        [MaxLength(500)]
        public string Keywords { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        /// <summary>'vi' hoặc 'en'</summary>
        [MaxLength(5)]
        public string Language { get; set; } = "vi";

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}