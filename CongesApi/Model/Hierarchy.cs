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
        public string? Code { get; set; }          // utilis� par contr�leur

        [MaxLength(500)]
        public string? Description { get; set; }   // utilis� par contr�leur

        public bool IsActive { get; set; } = true; // utilis� par UserController

        // Membres de la hi�rarchie
        public ICollection<HierarchyMember> Members { get; set; } = new List<HierarchyMember>();

        // Politique d�approbation optionnelle
        public HierarchyApprovalPolicy? ApprovalPolicy { get; set; }
    }
}
