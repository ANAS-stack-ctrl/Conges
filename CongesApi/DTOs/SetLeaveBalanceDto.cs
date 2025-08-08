namespace CongesApi.DTOs
{
    public class SetLeaveBalanceDto
    {
        public int UserId { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal Balance { get; set; }
    }
}
