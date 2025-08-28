using System;

namespace CongesApi.Model
{
    public class ApprovalDelegation
    {
        public int DelegationId { get; set; } // PK

        public int FromUserId { get; set; }
        public int ToUserId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public int? HierarchyId { get; set; }

        // Navigations
        public User? FromUser { get; set; }
        public User? ToUser { get; set; }
        public Hierarchy? Hierarchy { get; set; }
    }
}
