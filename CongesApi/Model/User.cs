using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string NationalID { get; set; }

        // Propriété string clé étrangère vers UserRole
        public string Role { get; set; }

        // Propriété de navigation vers UserRole
        public UserRole UserRole { get; set; }

        public decimal CurrentLeaveBalance { get; set; }
        public int? DelegateApprovalToId { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
        public ICollection<Notification> Notifications { get; set; }
    }
}
