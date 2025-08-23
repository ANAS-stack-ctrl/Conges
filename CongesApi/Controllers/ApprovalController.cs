using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ApprovalController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────
    // PENDING par rôle (on filtre ce que la personne doit voir)
    // GET /api/Approval/pending?role=Manager&userId=12
    // ─────────────────────────────────────────────
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] string? role, [FromQuery] int? userId)
    {
        var effectiveRole = (role ?? "").Trim();
        if (string.IsNullOrWhiteSpace(effectiveRole) && userId.HasValue)
            effectiveRole = await _db.Users.Where(u => u.UserId == userId.Value)
                                           .Select(u => u.Role)
                                           .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(effectiveRole))
            return BadRequest("role manquant (Manager|Director|RH).");

        var roleNorm = NormalizeRole(effectiveRole); // "manager" | "director" | "rh"

        var baseQ = _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .Include(a => a.LeaveRequest).ThenInclude(r => r.User)
            .Where(a => a.Level.ToLower() == roleNorm && a.Status == "Pending");

        // en série => seule l’étape active
        var serialQ =
            from a in baseQ
            where a.LeaveRequest.LeaveType.ApprovalFlow == "Serial"
            let minOrder = _db.Approvals
                .Where(x => x.LeaveRequestId == a.LeaveRequestId && x.Status == "Pending")
                .Select(x => (int?)x.NextApprovalOrder).Min()
            where a.NextApprovalOrder == minOrder
            select a;

        // en parallèle => toutes les pending pour ce rôle
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
                leaveType = new
                {
                    name = a.LeaveRequest.LeaveType.Name,
                    approvalFlow = a.LeaveRequest.LeaveType.ApprovalFlow
                }
            })
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────
    public class ApprovalActionDto
    {
        public string Action { get; set; } = "";   // "Approve" | "Reject"
        public string? Comments { get; set; }
        public int? ActorUserId { get; set; }
        public string? Role { get; set; }          // pour by-request
    }

    [HttpPost("{approvalId:int}/action")]
    public async Task<IActionResult> ActOnApproval(int approvalId, [FromBody] ApprovalActionDto dto)
    {
        var ap = await _db.Approvals
            .Include(a => a.LeaveRequest).ThenInclude(r => r.LeaveType)
            .FirstOrDefaultAsync(a => a.ApprovalId == approvalId);

        if (ap == null) return NotFound("Approbation introuvable.");
        return await ApplyDecision(ap, dto);
    }

    // Utilisé par le front
    [HttpPost("by-request/{requestId:int}/action")]
    public async Task<IActionResult> ActOnRequest(int requestId, [FromBody] ApprovalActionDto dto)
    {
        var roleNorm = NormalizeRole(dto.Role ?? "");
        var req = await _db.LeaveRequests.Include(r => r.LeaveType)
            .FirstOrDefaultAsync(r => r.LeaveRequestId == requestId);
        if (req == null) return NotFound("Demande introuvable.");

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

    // ─────────────────────────────────────────────
    // HISTORIQUE d’une demande
    // ─────────────────────────────────────────────
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
                req.Status
            },
            approvals = items
        });
    }

    // ─────────────────────────────────────────────
    // RH : stats globales
    // ─────────────────────────────────────────────
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

    // ─────────────────────────────────────────────
    // logique commune + débit du solde à l’approbation finale
    // ─────────────────────────────────────────────
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

        // Débloquer l’étape suivante si flow Serial et APPROVED
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

        // Statut global (FR) de la demande
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

            // ─── Débit du solde (idempotent) ───
            // On ne débite qu'une seule fois par LeaveRequest.
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
                            Quantity = -qty,                 // négatif = débit
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

        // payload utile au front
        return Ok(new
        {
            message = "Action enregistrée.",
            action = ap.Status,                 // "Approved" | "Rejected"
            actorUserId = ap.ApprovedBy,
            actorRole = ap.Level,
            actionDate = ap.ActionDate,
            requestStatus = req.Status          // "En attente" | "Approuvée" | "Refusée"
        });
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

    // ─────────────────────────────────────────────
    // Helpers stats rôle/jour (pour tuiles du front)
    // ─────────────────────────────────────────────
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
}
