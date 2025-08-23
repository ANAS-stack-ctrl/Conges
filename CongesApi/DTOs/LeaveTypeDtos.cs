namespace CongesApi.DTOs
{
    // Création
    public class CreateLeaveTypeDto
    {
        public string Name { get; set; } = "";
        public bool RequiresProof { get; set; }
        /// <summary>0 = illimité</summary>
        public int ConsecutiveDays { get; set; }
        /// <summary>"Serial" | "Parallel" (par défaut "Serial")</summary>
        public string? ApprovalFlow { get; set; } = "Serial";
        /// <summary>Optionnel. Null ou &lt;=0 = aucune policy liée</summary>
        public int? PolicyId { get; set; }
    }

    // Mise à jour
    public class UpdateLeaveTypeDto : CreateLeaveTypeDto
    {
        public int LeaveTypeId { get; set; }
    }
}
