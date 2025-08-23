// Models/BlackoutPeriod.cs
namespace CongesApi.Model
{
    public class BlackoutPeriod
    {
        public int BlackoutPeriodId { get; set; }

        // Nom interne "Maintenance S1", "Inventaire", etc.
        public string Name { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // "Global" | "LeaveType" | "Department" | "User"
        public string ScopeType { get; set; } = "Global";

        // Portée optionnelle
        public int? LeaveTypeId { get; set; }
        public int? DepartmentId { get; set; }    // si tu as une table Department (sinon enlève)
        public int? UserId { get; set; }          // cibler un employé

        // "Block" = interdit | "RequireDirector" = autorisé si validation Directeur
        public string EnforceMode { get; set; } = "Block";

        public string Reason { get; set; } = "";
        public bool IsActive { get; set; } = true;

        // Navigation
        public LeaveType? LeaveType { get; set; }
        public User? User { get; set; }
    }
}
