using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongesApi.Model
{
    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RequestedDays { get; set; }
        public int ActualDays { get; set; }
        public string Status { get; set; }
        public string PrivateNotes { get; set; }
        public string EmployeeComments { get; set; }
        public string EmployeeSignaturePath { get; set; }
        public DateTime? SignatureDate { get; set; }
        public string CurrentStage { get; set; }
        public string? ProofFilePath { get; set; }

        public bool IsHalfDay { get; set; } = false;

        [Required]
        public string HalfDayPeriod { get; set; } = "FULL"; // valeur par défaut

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public int? CancelledBy { get; set; }
        public DateTime? CancellationDate { get; set; }
        public string? CancellationReason { get; set; }   // ← nullable
        public bool RequiresDirectorOverride { get; set; } = false;


        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    }
}
