using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Approval
    {
        public int ApprovalId { get; set; }

        public string Level { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }

        public int? ApprovedBy { get; set; }     // <- NULL tant que personne n’a agi
        public User User { get; set; }

        public DateTime? ActionDate { get; set; } // <- NULL tant que personne n’a agi
        public int NextApprovalOrder { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; }
    }

}
