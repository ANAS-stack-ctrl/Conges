// Models/ManagerDelegation.cs
using System;

namespace CongesApi.Model
{
    public class ManagerDelegation
    {
        public int ManagerDelegationId { get; set; }

        public int HierarchyId { get; set; }
        public int ManagerUserId { get; set; }           // manager en congé
        public int DelegateManagerUserId { get; set; }   // manager délégué

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigations (facultatives mais propres)
        public Hierarchy? Hierarchy { get; set; }
        public User? Manager { get; set; }               // FK: ManagerUserId
        public User? Delegate { get; set; }              // FK: DelegateManagerUserId
    }
}
