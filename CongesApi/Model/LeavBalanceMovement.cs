using System;

namespace CongesApi.Model
{
    public class LeaveBalanceMovement
    {
        public int MovementId { get; set; }

        public int UserId { get; set; }
        public int LeaveTypeId { get; set; }
        public int LeaveRequestId { get; set; }

        public DateTime CreatedAt { get; set; }   // UTC
        public decimal Quantity { get; set; }     // n�gatif = d�bit, positif = cr�dit
        public string Reason { get; set; } = "";  // "Approval", "ManualAdjust", etc.
    }
}
