using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }

        public string Name { get; set; }
        public bool RequiresProof { get; set; }
        public int ConsecutiveDays { get; set; }

        public string ApprovalFlow { get; set; }
        public ApprovalFlowType ApprovalFlowType { get; set; } // ok

        public int? PolicyId { get; set; }
        public LeavePolicy? Policy { get; set; } // 👈 rendre nullable
    }
}
