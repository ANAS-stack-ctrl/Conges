using Microsoft.AspNetCore.Http;
using System;

namespace CongesApi.DTOs
{
    public class LeaveRequestDto
    {
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RequestedDays { get; set; }
        public string EmployeeComments { get; set; } = "";
        public string EmployeeSignatureBase64 { get; set; } = "";
        public int UserId { get; set; }
        public IFormFile? ProofFile { get; set; }
        public bool IsHalfDay { get; set; }
        public string HalfDayPeriod { get; set; } = "FULL";


    }
}
