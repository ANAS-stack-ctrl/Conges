using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Approval
    {
        [Key]
        public int ApprovalId { get; set; }

        public string Level { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }

        public int ApprovedBy { get; set; }     // FK vers User
        public User User { get; set; }           // navigation vers User

        public DateTime ActionDate { get; set; }
        public int NextApprovalOrder { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; }
    }
}
