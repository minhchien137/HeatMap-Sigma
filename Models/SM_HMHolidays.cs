using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeatmapSystem.Models
{
   public class SM_HMHolidays
{
    public int Id { get; set; }
    public DateTime HolidayDate { get; set; }
    public string Name { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
}
