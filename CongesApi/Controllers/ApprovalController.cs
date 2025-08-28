using System;
using System.Linq;
using System.Threading.Tasks;
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/Approval")]
public class ApprovalController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ApprovalController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────────
    // PENDING visibles par un reviewer
    // GET /api/Approval/pending?reviewerUserId=12  (alias: ?userId=12)
    // role est optionnel (si absent, on prend celui du reviewer)
    // ─────────────────────────────────────────────────────────────
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(
        [FromQuery] int? reviewerUserId,
        [FromQuery] int? userId,
        [FromQuery] string? role)
    {
        // compat: userId ⇢ reviewerUserId
        if (!reviewerUserId.HasValue && userId.HasValue)
            reviewerUserId = userId;

        if (!reviewerUserId.HasValue && string.IsNullOrWhiteSpace(role))
            return BadRequest("reviewerUserId (ou userId) requis.");

        // Récupérer le reviewer pour connaître son rôle et sa hiérarchie
        string reviewerRole = (role ?? "").Trim();
        int? reviewerHierarchyId = null;

        if (reviewerUserId.HasValue)
        {
            var u = await _db.Users.AsNoTracking()
                .Where(x => x.UserId == reviewerUserId.Value)
                .Select(x => new { x.Role, x.HierarchyId })
                .FirstOrDefaultAsync();

            if (u == null) return BadRequest("Reviewer inconnu.");
            reviewerRole = string.IsNullOrWhiteSpace(reviewerRole) ? u.Role : reviewerRole;
            reviewerHierarchyId = u.HierarchyId;
        }

        var roleNorm = NormalizeRole(reviewerRole); // "manager" | "director" | "rh"
        if (string.IsNullOrWhiteSpace(roleNorm))
            return Ok(Array.Empty<object>());

        // Base: approbations PENDING pour ce niveau
        var baseQ = _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .Include(a => a.LeaveRequest).ThenInclude(r => r.User)
            .Where(a => a.Level.ToLower() == roleNorm && a.Status == "Pending");

        // Filtrage par hiérarchie (Manager/Director uniquement)
        if (reviewerHierarchyId.HasValue && (roleNorm == "manager" || roleNorm == "director"))
        {
            baseQ = baseQ.Where(a => a.LeaveRequest.HierarchyId == reviewerHierarchyId.Value);
        }

        // ➕ Filtre Manager : assignment + délégation (manager effectif)
        if (roleNorm == "manager" && reviewerUserId.HasValue)
        {
            int rid = reviewerUserId.Value;

            baseQ = baseQ.Where(a =>
                // (A) Pas d'assignation active -> visible par tous les managers de la hiérarchie
                !_db.ManagerAssignments.Any(ma =>
                    ma.Active
                    && ma.HierarchyId == a.LeaveRequest.HierarchyId
                    && ma.EmployeeUserId == a.LeaveRequest.UserId)

                // (B) Assignation active sans délégation couvrante -> seulement le manager assigné
                || (
                    _db.ManagerAssignments.Any(ma =>
                        ma.Active
                        && ma.HierarchyId == a.LeaveRequest.HierarchyId
                        && ma.EmployeeUserId == a.LeaveRequest.UserId
                        && ma.ManagerUserId == rid)
                    &&
                    !_db.ManagerDelegations.Any(d =>
                        d.Active
                        && d.HierarchyId == a.LeaveRequest.HierarchyId
                        && d.ManagerUserId == rid
                        && d.StartDate <= a.LeaveRequest.EndDate
                        && d.EndDate >= a.LeaveRequest.StartDate)
                )

                // (C) Délégation couvrant la période -> seulement le manager délégué
                || _db.ManagerDelegations.Any(d =>
                    d.Active
                    && d.HierarchyId == a.LeaveRequest.HierarchyId
                    && d.DelegateManagerUserId == rid
                    && d.StartDate <= a.LeaveRequest.EndDate
                    && d.EndDate >= a.LeaveRequest.StartDate
                    && _db.ManagerAssignments.Any(ma =>
                        ma.Active
                        && ma.HierarchyId == a.LeaveRequest.HierarchyId
                        && ma.EmployeeUserId == a.LeaveRequest.UserId
                        && ma.ManagerUserId == d.ManagerUserId)
                )
            );
        }

        // Exclure ses propres demandes
        if (reviewerUserId.HasValue)
        {
            baseQ = baseQ.Where(a => a.LeaveRequest.UserId != reviewerUserId.Value
                                   && a.LeaveRequest.CreatedBy != reviewerUserId.Value);
        }

        // Flow "Serial" : seule l'étape PENDING la plus basse
        var serialQ =
            from a in baseQ
            where a.LeaveRequest.LeaveType.ApprovalFlow == "Serial"
            let minOrder = _db.Approvals
                .Where(x => x.LeaveRequestId == a.LeaveRequestId && x.Status == "Pending")
                .Select(x => (int?)x.NextApprovalOrder).Min()
            where a.NextApprovalOrder == minOrder
            select a;

        // Flow "Parallel" : toutes les PENDING de ce rôle
        var parallelQ =
            from a in baseQ
            where a.LeaveRequest.LeaveType.ApprovalFlow == "Parallel"
            select a;

        var list = await serialQ.Union(parallelQ)
            .OrderBy(a => a.LeaveRequestId)
            .Select(a => new
            {
                approvalId = a.ApprovalId,
                leaveRequestId = a.LeaveRequestId,
                employeeFullName = a.LeaveRequest.User.FirstName + " " + a.LeaveRequest.User.LastName,
                startDate = a.LeaveRequest.StartDate,
                endDate = a.LeaveRequest.EndDate,
                requestedDays = a.LeaveRequest.RequestedDays,
                isHalfDay = a.LeaveRequest.IsHalfDay,
                currentStage = a.Level,
                proofFilePath = a.LeaveRequest.ProofFilePath,
                status = a.Status,
                userId = a.LeaveRequest.UserId,
                createdBy = a.LeaveRequest.CreatedBy,
                leaveType = new
                {
                    name = a.LeaveRequest.LeaveType.Name,
                    approvalFlow = a.LeaveRequest.LeaveType.ApprovalFlow
                }
            })
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────────
    // ACTIONS (Approve/Reject)
    // ─────────────────────────────────────────────────────────────
    public class ApprovalActionDto
    {
        public string Action { get; set; } = "";   // "Approve" | "Reject"
        public string? Comments { get; set; }
        public int? ActorUserId { get; set; }
        public string? Role { get; set; }          // optionnel (utilisé pour le filtre d’étape)
    }

    // par ApprovalId (rare côté front)
    [HttpPost("{approvalId:int}/action")]
    public async Task<IActionResult> ActOnApproval(int approvalId, [FromBody] ApprovalActionDto dto)
    {
        var ap = await _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .FirstOrDefaultAsync(a => a.ApprovalId == approvalId);

        if (ap == null) return NotFound("Approbation introuvable.");
        return await ApplyDecision(ap, dto);
    }

    // par LeaveRequestId (utilisé par le front)
    [HttpPost("by-request/{requestId:int}/action")]
    public async Task<IActionResult> ActOnRequest(int requestId, [FromBody] ApprovalActionDto dto)
    {
        var roleNorm = NormalizeRole(dto.Role ?? "");
        var req = await _db.LeaveRequests.Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.LeaveRequestId == requestId);
        if (req == null) return NotFound("Demande introuvable.");

        // Contrôle d’accès : Manager/Director doivent être dans la même hiérarchie
        if (dto.ActorUserId.HasValue && (roleNorm == "manager" || roleNorm == "director"))
        {
            var actor = await _db.Users.AsNoTracking()
                .Where(u => u.UserId == dto.ActorUserId.Value)
                .Select(u => new { u.HierarchyId, u.Role })
                .FirstOrDefaultAsync();

            if (actor == null) return BadRequest("Acteur inconnu.");
            if (actor.HierarchyId != req.HierarchyId)
                return Forbid("Cette demande n'appartient pas à votre hiérarchie.");

            // ➕ Garde-fou Manager : seul le manager effectif (assignment ou délégué) peut agir
            if (roleNorm == "manager")
            {
                var effective = await GetEffectiveManagerAsync(
                    req.HierarchyId ?? 0,
                    req.UserId,
                    req.StartDate,
                    req.EndDate
                );

                if (effective.HasValue && effective.Value != dto.ActorUserId.Value)
                    return Forbid("Vous n'êtes pas le manager habilité à valider cette demande (délégation/assignation).");
            }
        }

        IQueryable<Approval> q = _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .Where(a => a.LeaveRequestId == requestId && a.Status == "Pending");

        if (!string.IsNullOrEmpty(roleNorm))
            q = q.Where(a => a.Level.ToLower() == roleNorm);

        if (req.LeaveType.ApprovalFlow == "Serial")
        {
            var minOrder = await _db.Approvals
                .Where(a => a.LeaveRequestId == requestId && a.Status == "Pending")
                .Select(a => (int?)a.NextApprovalOrder).MinAsync();

            q = q.Where(a => a.NextApprovalOrder == minOrder);
        }

        var ap = await q.FirstOrDefaultAsync();
        if (ap == null) return BadRequest("Aucune approbation active pour ce rôle.");
        return await ApplyDecision(ap, dto);
    }

    // ─────────────────────────────────────────────────────────────
    // HISTORIQUE (utilisé par RH)
    // ─────────────────────────────────────────────────────────────
    [HttpGet("history/{leaveRequestId:int}")]
    public async Task<IActionResult> GetHistory(int leaveRequestId)
    {
        var req = await _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.LeaveRequestId == leaveRequestId);

        if (req == null) return NotFound("Demande introuvable.");

        var items = await _db.Approvals
            .Where(a => a.LeaveRequestId == leaveRequestId)
            .OrderBy(a => a.NextApprovalOrder)
            .Select(a => new
            {
                a.ApprovalId,
                a.Level,
                a.Status,
                a.Comments,
                a.ActionDate,
                a.NextApprovalOrder,
                ApprovedBy = a.ApprovedBy,
                ApprovedByName = _db.Users
                    .Where(u => u.UserId == a.ApprovedBy)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            request = new
            {
                req.LeaveRequestId,
                Employee = req.User.FirstName + " " + req.User.LastName,
                Type = req.LeaveType.Name,
                req.StartDate,
                req.EndDate,
                req.RequestedDays,
                req.Status,
                req.ProofFilePath
            },
            approvals = items
        });
    }

    // ─────────────────────────────────────────────────────────────
    // RH : stats globales (pas de filtre hiérarchie)
    // ─────────────────────────────────────────────────────────────
    [HttpGet("rh/stats")]
    public async Task<IActionResult> GetRhStats()
    {
        const string EN_ATTENTE = "En attente";
        const string APPROUVEE = "Approuvée";
        const string REFUSEE = "Refusée";

        var total = await _db.LeaveRequests.CountAsync();
        var attente = await _db.LeaveRequests.CountAsync(r => r.Status == EN_ATTENTE);
        var valides = await _db.LeaveRequests.CountAsync(r => r.Status == APPROUVEE);
        var refusees = await _db.LeaveRequests.CountAsync(r => r.Status == REFUSEE);

        return Ok(new { total, valides, refusees, attente });
    }

    // ─────────────────────────────────────────────────────────────
    // Stats par rôle (Manager/Director/RH) — pour le dashboard
    // GET /api/Approval/stats/role?role=Manager
    // ─────────────────────────────────────────────────────────────
    [HttpGet("stats/role")]
    public async Task<IActionResult> GetRoleDailyStats([FromQuery] string role, [FromQuery] DateTime? date)
    {
        if (string.IsNullOrWhiteSpace(role))
            return BadRequest("Paramètre 'role' requis (Manager | Director | RH).");

        var roleNorm = NormalizeRole(role);
        var (startUtc, endUtc) = GetUtcDayRange(date);

        var approvedToday = await _db.Approvals.CountAsync(a =>
            a.Level.ToLower() == roleNorm &&
            a.Status == "Approved" &&
            a.ActionDate != null &&
            a.ActionDate >= startUtc && a.ActionDate < endUtc);

        var rejectedToday = await _db.Approvals.CountAsync(a =>
            a.Level.ToLower() == roleNorm &&
            a.Status == "Rejected" &&
            a.ActionDate != null &&
            a.ActionDate >= startUtc && a.ActionDate < endUtc);

        var pendingNow = await PendingForRoleQuery(roleNorm).CountAsync();

        return Ok(new { approvedToday, rejectedToday, pendingNow });
    }

    // Helpers pour les stats
    private static (DateTime startUtc, DateTime endUtc) GetUtcDayRange(DateTime? date)
    {
        var d = (date?.Date ?? DateTime.UtcNow.Date);
        var start = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        var end = start.AddDays(1);
        return (start, end);
    }

    private IQueryable<Approval> PendingForRoleQuery(string roleNorm)
    {
        var baseQ = _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .Where(a => a.Level.ToLower() == roleNorm && a.Status == "Pending");

        var serialQ =
            from a in baseQ
            where a.LeaveRequest.LeaveType.ApprovalFlow == "Serial"
            let minOrder = _db.Approvals
                .Where(x => x.LeaveRequestId == a.LeaveRequestId && x.Status == "Pending")
                .Select(x => (int?)x.NextApprovalOrder).Min()
            where a.NextApprovalOrder == minOrder
            select a;

        var parallelQ =
            from a in baseQ
            where a.LeaveRequest.LeaveType.ApprovalFlow == "Parallel"
            select a;

        return serialQ.Union(parallelQ);
    }

    // ─────────────────────────────────────────────────────────────
    // Logique de décision + débit du solde à l’approbation finale
    // ─────────────────────────────────────────────────────────────
    private async Task<IActionResult> ApplyDecision(Approval ap, ApprovalActionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Action))
            return BadRequest("Action requise (Approve|Reject).");

        var act = dto.Action.Trim().ToLowerInvariant();
        if (act != "approve" && act != "reject")
            return BadRequest("Action invalide (Approve|Reject).");

        ap.Status = (act == "approve") ? "Approved" : "Rejected";
        ap.Comments = dto.Comments ?? "";
        ap.ActionDate = DateTime.UtcNow;
        if (dto.ActorUserId.HasValue) ap.ApprovedBy = dto.ActorUserId.Value;

        await _db.SaveChangesAsync();

        var req = await _db.LeaveRequests
            .Include(r => r.LeaveType)
            .FirstAsync(r => r.LeaveRequestId == ap.LeaveRequestId);

        if (req.LeaveType.ApprovalFlow == "Serial" && ap.Status == "Approved")
        {
            var next = await _db.Approvals.FirstOrDefaultAsync(x =>
                x.LeaveRequestId == ap.LeaveRequestId &&
                x.Status == "Blocked" &&
                x.NextApprovalOrder == ap.NextApprovalOrder + 1);

            if (next != null)
            {
                next.Status = "Pending";
                await _db.SaveChangesAsync();
            }
        }

        var approvals = await _db.Approvals
            .Where(x => x.LeaveRequestId == ap.LeaveRequestId)
            .ToListAsync();

        var anyRejected = approvals.Any(x => x.Status == "Rejected");
        var allApproved = approvals.All(x => x.Status == "Approved");

        if (anyRejected)
        {
            req.Status = "Refusée";
            req.CurrentStage = "Rejected";
        }
        else if (allApproved)
        {
            req.Status = "Approuvée";
            req.CurrentStage = "Approved";

            // Idempotent : débiter une seule fois
            bool alreadyDebited = await _db.LeaveBalanceMovements
                .AnyAsync(m => m.LeaveRequestId == req.LeaveRequestId && m.Reason == "DEBIT_APPROVAL");

            if (!alreadyDebited)
            {
                var balance = await _db.LeaveBalances
                    .FirstOrDefaultAsync(b => b.UserId == req.UserId && b.LeaveTypeId == req.LeaveTypeId);

                if (balance != null)
                {
                    decimal qty = req.IsHalfDay ? 0.5m : (decimal)req.ActualDays;
                    if (qty > 0)
                    {
                        balance.CurrentBalance -= qty;

                        _db.LeaveBalanceMovements.Add(new LeaveBalanceMovement
                        {
                            UserId = req.UserId,
                            LeaveTypeId = req.LeaveTypeId,
                            LeaveRequestId = req.LeaveRequestId,
                            CreatedAt = DateTime.UtcNow,
                            Quantity = -qty,
                            Reason = "DEBIT_APPROVAL"
                        });

                        await _db.SaveChangesAsync();
                    }
                }
            }
        }
        else
        {
            req.Status = "En attente";
            if (req.LeaveType.ApprovalFlow == "Serial")
            {
                var nextLevel = await _db.Approvals
                    .Where(x => x.LeaveRequestId == req.LeaveRequestId && x.Status == "Pending")
                    .OrderBy(x => x.NextApprovalOrder)
                    .Select(x => x.Level)
                    .FirstOrDefaultAsync();
                req.CurrentStage = nextLevel ?? "Pending";
            }
            else
            {
                req.CurrentStage = "Parallel";
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Action enregistrée.",
            action = ap.Status,                 // "Approved" | "Rejected"
            actorUserId = ap.ApprovedBy,
            actorRole = ap.Level,
            actionDate = ap.ActionDate,
            requestStatus = req.Status
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers "manager effectif"
    // ─────────────────────────────────────────────────────────────
    private async Task<int?> GetEffectiveManagerAsync(int hierarchyId, int employeeUserId, DateTime reqStart, DateTime reqEnd)
    {
        // Manager assigné ?
        var assigned = await _db.ManagerAssignments.AsNoTracking()
            .Where(ma => ma.Active && ma.HierarchyId == hierarchyId && ma.EmployeeUserId == employeeUserId)
            .Select(ma => (int?)ma.ManagerUserId)
            .FirstOrDefaultAsync();

        if (assigned == null || assigned.Value == 0)
            return null; // pas d’assignation : tous les managers de la hiérarchie peuvent voir

        // Délégation couvrant la période ?
        var delegateId = await _db.ManagerDelegations.AsNoTracking()
            .Where(d => d.Active
                     && d.HierarchyId == hierarchyId
                     && d.ManagerUserId == assigned.Value
                     && d.StartDate <= reqEnd.Date
                     && d.EndDate >= reqStart.Date)
            .Select(d => (int?)d.DelegateManagerUserId)
            .FirstOrDefaultAsync();

        return delegateId ?? assigned.Value;
    }

    private static string NormalizeRole(string role)
    {
        var r = (role ?? "").Trim().ToLowerInvariant();
        return r switch
        {
            "human resources" or "ressources humaines" or "hr" or "rh" => "rh",
            "manager" or "line manager" => "manager",
            "director" or "directeur" => "director",
            _ => r
        };
    }
}
