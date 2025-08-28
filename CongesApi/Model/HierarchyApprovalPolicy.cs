namespace CongesApi.Model
{
    /// <summary>
    /// Param�tres d�acheminement d�approbation au niveau hi�rarchie.
    /// </summary>
    public class HierarchyApprovalPolicy
    {
        public int PolicyId { get; set; }     // PK (r�f�renc� dans DbContext)
        public int HierarchyId { get; set; }  // FK -> Hierarchy

        // D�j� utilis�s par tes services/contr�leurs :
        public bool ManagerPeerFirst { get; set; } = true;   // d�abord manager pair ?
        public int RequiredPeerCount { get; set; } = 1;      // quota de pairs requis
        /// <summary>Any | All | Quota</summary>
        public string PeerSelectionMode { get; set; } = "Any";

        // Autres switches (optionnels)
        public bool FallbackToDirector { get; set; } = true;
        public bool FallbackToHR { get; set; } = true;

        // Navigation
        public Hierarchy? Hierarchy { get; set; }
    }
}
