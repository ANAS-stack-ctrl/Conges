using System;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class LeavePolicy
    {
        [Key]
        public int PolicyId { get; set; }

        public bool AllowHalfDay { get; set; }
        public int MinNoticeDays { get; set; }
        public string BlackoutPeriods { get; set; }
        public int MaxDurationDays { get; set; }
        public int MaxConsecutiveDays { get; set; }
        public bool RequiresProof { get; set; }
        public int ProofMaxDelay { get; set; }
        public bool RequiresHRApproval { get; set; }
        public bool RequiresManagerApproval { get; set; }
        public bool RequiresDirectorApproval { get; set; }
        public bool IsPaid { get; set; }
    }
}
