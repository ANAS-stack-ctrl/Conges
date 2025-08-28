// Models/ManagerAssignment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongesApi.Model
{
    public class ManagerAssignment
    {
        [Key]
        public int ManagerAssignmentId { get; set; }

        public int HierarchyId { get; set; }

        // CHAMPS QUI DOIVENT CORRESPONDRE À LA DB (migration existante)
        public int EmployeeUserId { get; set; }
        public int ManagerUserId { get; set; }

        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(HierarchyId))] public Hierarchy? Hierarchy { get; set; }
        [ForeignKey(nameof(EmployeeUserId))] public User? Employee { get; set; }
        [ForeignKey(nameof(ManagerUserId))] public User? Manager { get; set; }
    }
}
