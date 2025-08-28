using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Hierarchy
    {
        public int HierarchyId { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? Code { get; set; }          // utilisé par contrôleur

        [MaxLength(500)]
        public string? Description { get; set; }   // utilisé par contrôleur

        public bool IsActive { get; set; } = true; // utilisé par UserController

        // Membres de la hiérarchie
        public ICollection<HierarchyMember> Members { get; set; } = new List<HierarchyMember>();

        // Politique d’approbation optionnelle
        public HierarchyApprovalPolicy? ApprovalPolicy { get; set; }
    }
}
