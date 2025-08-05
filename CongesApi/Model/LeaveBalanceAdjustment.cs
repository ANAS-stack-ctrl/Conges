using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class LeaveBalanceAdjustment
    {
        [Key]
        public int AdjustmentId { get; set; }

        public decimal OldBalance { get; set; }
        public decimal NewBalance { get; set; }
        public string Reason { get; set; }
        public DateTime AdjustmentDate { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; }
    }
}
