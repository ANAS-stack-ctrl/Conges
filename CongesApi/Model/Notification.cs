using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public string Type { get; set; }
        public NotificationType NotificationType { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public string DeepLink { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
