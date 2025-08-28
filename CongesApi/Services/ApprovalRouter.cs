using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Services
{
    /// <summary>
    /// Choisit l'unique approbateur par niveau dans la même hiérarchie
    /// et construit la séquence d'approbation pour une LeaveRequest.
    /// </summary>
    public class ApprovalRouter
    {
        private readonly ApplicationDbContext _db;
        public ApprovalRouter(ApplicationDbContext db) => _db = db;

        private async Task<User?> PickOneAsync(int? hierarchyId, string role)
        {
            var q = _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.Role == role);

            if (hierarchyId.HasValue)
                q = q.Where(u => u.HierarchyId == hierarchyId.Value);

            // Si rien dans la hiérarchie pour RH, on accepte un RH global.
            if (role == "RH" && hierarchyId.HasValue && !await q.AnyAsync())
                q = _db.Users.AsNoTracking().Where(u => u.IsActive && u.Role == "RH");

            return await q
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Renvoie la liste ordonnée de niveaux à appliquer
        /// à partir de la policy du type (fallback sur Manager → RH).
        /// Injecte Director si RequireDirectorOverride.
        /// </summary>
        public List<string> ComputeLevels(LeaveRequest req)
        {
            var levels = new List<string>();

            if (req.LeaveType?.Policy != null)
            {
                var p = req.LeaveType.Policy;
                if (p.RequiresManagerApproval) levels.Add("Manager");
                if (p.RequiresDirectorApproval) levels.Add("Director");
                if (p.RequiresHRApproval) levels.Add("RH");
            }

            if (levels.Count == 0)
                levels.AddRange(new[] { "Manager", "RH" });

            // Surcharge blackout : impose Director s'il n'est pas déjà présent
            if (req.RequiresDirectorOverride && !levels.Contains("Director"))
            {
                // On insère Director juste après Manager si Manager existe, sinon en tête.
                var idx = levels.IndexOf("Manager");
                levels.Insert(idx >= 0 ? idx + 1 : 0, "Director");
            }

            return levels;
        }

        /// <summary>
        /// Construit le plan d’approbation : pour Serial → un seul Pending puis des Blocked
        /// Pour Parallel → tous en Pending. Toujours un seul approbateur / niveau.
        /// </summary>
        public async Task<List<(string level, int? approverUserId, int order, string status)>>
            BuildPlanAsync(LeaveRequest req)
        {
            var serial = (req.LeaveType?.ApprovalFlow ?? "Serial") == "Serial";
            var levels = ComputeLevels(req);

            var plan = new List<(string, int?, int, string)>();
            var order = 1;

            foreach (var lvl in levels)
            {
                var approver = await PickOneAsync(req.HierarchyId, lvl);
                var approverId = approver?.UserId;

                if (serial)
                {
                    var status = (order == 1) ? "Pending" : "Blocked";
                    plan.Add((lvl, approverId, order, status));
                }
                else
                {
                    // Parallel : tous en Pending, même ordre 1 (l'ordre n'a pas d'importance)
                    plan.Add((lvl, approverId, 1, "Pending"));
                }
                order++;
            }

            return plan;
        }
    }
}
