using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class HierarchyMember
    {
        public int HierarchyMemberId { get; set; } // PK

        // FKs
        public int HierarchyId { get; set; }
        public int UserId { get; set; }

        // R�le du membre dans la hi�rarchie
        [MaxLength(30)]
        public string Role { get; set; } = "Employee"; // Employee | Manager | Director | RH

        // Navigations
        public Hierarchy? Hierarchy { get; set; }
        public User? User { get; set; }
    }
}
