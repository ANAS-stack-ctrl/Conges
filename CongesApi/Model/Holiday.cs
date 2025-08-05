using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        public DateTime Date { get; set; }
        public string Description { get; set; }
        public bool IsRecurring { get; set; }
    }
}
