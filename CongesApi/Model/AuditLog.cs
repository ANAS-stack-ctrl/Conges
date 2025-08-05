using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }

        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
