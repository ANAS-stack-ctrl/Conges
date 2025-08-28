using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;
using CongesApi.Services; // ApprovalRouter

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ApprovalRouter _router;

    public LeaveRequestController(ApplicationDbContext db, IWebHostEnvironment env, ApprovalRouter router)
    {
        _db = db;
        _env = env;
        _router = router;
    }

    // ───────── Fichiers justificatifs (PNG / PDF)
    private static readonly HashSet<string> AllowedProofExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".pdf" };

    private static async Task<bool> IsAllowedProofFileAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedProofExtensions.Contains(ext)) return false;

        try
        {
            using var s = file.OpenReadStream();
            byte[] header = new byte[Math.Min(8, file.Length)];
            int read = await s.ReadAsync(header, 0, header.Length);

            // PDF
            if (ext == ".pdf")
                return read >= 5 &&
                       header[0] == 0x25 && header[1] == 0x50 &&
                       header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D;

            // PNG
            if (ext == ".png")
                return read >= 8 &&
                       header[0] == 0x89 && header[1] == 0x50 &&
                       header[2] == 0x4E && header[3] == 0x47 &&
                       header[4] == 0x0D && header[5] == 0x0A &&
                       header[6] == 0x1A && header[7] == 0x0A;
        }
        catch { /* ignore */ }

        return false;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim().Replace(" ", "_");
    }

    private string EnsureWebRoot()
    {
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : _env.WebRootPath;
        if (!Directory.Exists(webRoot)) Directory.CreateDirectory(webRoot);
        return webRoot;
    }

    private static bool RangesOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart.Date <= bEnd.Date && bStart.Date <= aEnd.Date;

    private Task<bool> HasOverlappingRequestAsync(int userId, DateTime start, DateTime end)
    {
        string[] blocking = new[] { "En attente", "Approuvée" };

        return _db.LeaveRequests
            .Where(r => r.UserId == userId && blocking.Contains(r.Status))
            .AnyAsync(r => start.Date <= r.EndDate.Date && r.StartDate.Date <= end.Date);
    }

    private async Task<List<BlackoutPeriod>> GetOverlappingBlackoutsAsync(
        int userId, int leaveTypeId, DateTime start, DateTime end)
    {
        return await _db.BlackoutPeriods
            .Where(b => b.IsActive &&
                        b.StartDate <= end &&
                        start <= b.EndDate &&
                        (
                            b.ScopeType == "Global" ||
                            (b.ScopeType == "LeaveType" && b.LeaveTypeId == leaveTypeId) ||
                            (b.ScopeType == "User" && b.UserId == userId)
                        ))
            .ToListAsync();
    }

    // ───────── Types pour le formulaire
    [HttpGet("leave-types")]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var types = await _db.LeaveTypes
            .Select(t => new
            {
                t.LeaveTypeId,
                t.Name,
                t.RequiresProof,
                t.ConsecutiveDays,
                t.ApprovalFlow
            })
            .ToListAsync();

        return Ok(types);
    }

    // ───────── RH admin list (+ filtre optionnel par hiérarchie)
    // LeaveRequestController.cs  (extrait complet de la méthode)
    [HttpGet("admin/requests")]
    public async Task<IActionResult> GetAllRequestsForRh(
        [FromQuery] string? status, [FromQuery] int? typeId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var q = _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(r => r.Status == status);
        if (typeId.HasValue) q = q.Where(r => r.LeaveTypeId == typeId.Value);
        if (from.HasValue) q = q.Where(r => r.StartDate >= from.Value);
        if (to.HasValue) q = q.Where(r => r.EndDate <= to.Value);

        // Si tu as une table Hierarchies, on projette le nom via un sous-select.
        var data = await q.OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.LeaveRequestId,
                // ← Nom (ancien 'employee')
                name = r.User.FirstName + " " + r.User.LastName,
                // ← Type libellé inchangé
                type = r.LeaveType.Name,
                startDate = r.StartDate,
                endDate = r.EndDate,
                requestedDays = r.RequestedDays,
                status = r.Status,
                employeeComments = r.EmployeeComments,
                proofFilePath = r.ProofFilePath,
                // NOUVEAU
                role = r.User.Role, // "Employee" | "Manager" | "Director" | "RH"
                hierarchy = _db.Hierarchies
                    .Where(h => h.HierarchyId == r.HierarchyId)
                    .Select(h => h.Name)
                    .FirstOrDefault() ?? ""  // si null
            })
            .ToListAsync();

        return Ok(data);
    }


    [HttpGet("admin/stats")]
    public async Task<IActionResult> GetRhStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var q = _db.LeaveRequests.AsQueryable();
        if (from.HasValue) q = q.Where(r => r.StartDate >= from.Value);
        if (to.HasValue) q = q.Where(r => r.EndDate <= to.Value);

        var total = await q.CountAsync();
        var valides = await q.CountAsync(r => r.Status == "Approuvée");
        var refusees = await q.CountAsync(r => r.Status == "Refusée");
        var attente = await q.CountAsync(r => r.Status == "En attente");

        return Ok(new { total, valides, refusees, attente });
    }

    // ───────── CRUD simple
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.LeaveRequests
            .Include(lr => lr.User).ThenInclude(u => u.UserRole)
            .Include(lr => lr.LeaveType)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.LeaveRequests
            .Include(lr => lr.User).ThenInclude(u => u.UserRole)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

        return item == null ? NotFound() : Ok(item);
    }

    // ───────── Liste “à valider” par approbateur (filtre hiérarchie pour Manager, Director et RH)
    [HttpGet("approvals/pending")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] int approverUserId)
    {
        var approver = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == approverUserId);
        if (approver == null) return BadRequest("Approver invalide.");

        IQueryable<LeaveRequest> q = _db.LeaveRequests
            .Include(r => r.User)
            .Include(r => r.LeaveType)
            .Where(r => r.Status == "En attente" && r.UserId != approverUserId);

        if (approver.Role == "Manager")
            q = q.Where(r => r.CurrentStage == "Manager" && r.HierarchyId == approver.HierarchyId);
        else if (approver.Role == "Director")
            q = q.Where(r => r.CurrentStage == "Director" && r.HierarchyId == approver.HierarchyId);
        else if (approver.Role == "RH")
            q = q.Where(r => r.CurrentStage == "RH" && r.HierarchyId == approver.HierarchyId); // ★ RH aussi limité à sa hiérarchie
        else
            return Ok(Array.Empty<object>());

        var data = await q.OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.LeaveRequestId,
                employeeFullName = r.User.FirstName + " " + r.User.LastName,
                leaveType = new { r.LeaveType.LeaveTypeId, name = r.LeaveType.Name, approvalFlow = r.LeaveType.ApprovalFlow },
                r.StartDate,
                r.EndDate,
                r.RequestedDays,
                r.IsHalfDay,
                r.CurrentStage,
                r.ProofFilePath,
                r.HierarchyId
            })
            .ToListAsync();

        return Ok(data);
    }

    // ───────── CREATE (pose la HierarchyId de l’employé)
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] LeaveRequestDto dto)
    {
        if (dto.StartDate.Date > dto.EndDate.Date)
            return BadRequest("La date de début ne peut pas être après la date de fin.");
        if (!dto.IsHalfDay && dto.RequestedDays <= 0)
            return BadRequest("Le nombre de jours demandés doit être supérieur à 0.");

        var requester = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
        if (requester == null) return BadRequest("Utilisateur inconnu.");

        var leaveType = await _db.LeaveTypes
            .Include(t => t.Policy)
            .FirstOrDefaultAsync(t => t.LeaveTypeId == dto.LeaveTypeId);
        if (leaveType == null) return BadRequest("Type de congé invalide.");

        var bl = await GetOverlappingBlackoutsAsync(dto.UserId, dto.LeaveTypeId, dto.StartDate, dto.EndDate);
        var blocking = bl.FirstOrDefault(b => b.EnforceMode == "Block");
        if (blocking != null)
        {
            var msg = string.IsNullOrWhiteSpace(blocking.Reason)
                ? $"Période indisponible ({blocking.Name})."
                : $"Période indisponible ({blocking.Name}) : {blocking.Reason}";
            return BadRequest(msg);
        }
        bool requireDirector = bl.Any(b => b.EnforceMode == "RequireDirector");

        if (await HasOverlappingRequestAsync(dto.UserId, dto.StartDate, dto.EndDate))
            return BadRequest("Vous avez déjà une demande En attente/Approuvée qui chevauche ces dates.");

        decimal requested = dto.IsHalfDay ? 0.5m : (decimal)dto.RequestedDays;
        if (leaveType.ConsecutiveDays > 0 && requested > leaveType.ConsecutiveDays)
            return BadRequest($"Le type '{leaveType.Name}' autorise au max {leaveType.ConsecutiveDays} jours consécutifs.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == dto.UserId && b.LeaveTypeId == dto.LeaveTypeId);
        if (balance == null) return BadRequest("Aucun solde pour ce type. Contactez les RH.");

        var debit = dto.IsHalfDay ? (dto.RequestedDays > 0 ? 0.5m : 0m) : (decimal)dto.RequestedDays;
        if (balance.CurrentBalance < debit) return BadRequest("Solde insuffisant.");

        string? proofFilePath = null;
        if (leaveType.RequiresProof && dto.ProofFile == null)
            return BadRequest("Un justificatif est requis pour ce type.");

        if (dto.ProofFile is { Length: > 0 })
        {
            if (!await IsAllowedProofFileAsync(dto.ProofFile))
                return BadRequest("Format de justificatif non valide. Seuls PNG/PDF sont autorisés.");

            var webRoot = EnsureWebRoot();
            var uploads = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(dto.ProofFile.FileName).ToLowerInvariant();
            var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(dto.ProofFile.FileName));
            var fileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var physical = Path.Combine(uploads, fileName);

            using var fs = new FileStream(physical, FileMode.Create);
            await dto.ProofFile.CopyToAsync(fs);

            proofFilePath = $"/uploads/{fileName}";
        }

        string? signaturePath = null;
        if (!string.IsNullOrWhiteSpace(dto.EmployeeSignatureBase64))
        {
            var baseName = $"{SanitizeFileName(requester.LastName ?? "User")}_{SanitizeFileName(requester.FirstName ?? dto.UserId.ToString())}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var webRoot = EnsureWebRoot();
            var sigDir = Path.Combine(webRoot, "signatures");
            Directory.CreateDirectory(sigDir);

            var payload = dto.EmployeeSignatureBase64.Split(',');
            var b64 = payload.Length > 1 ? payload[1] : payload[0];
            var bytes = Convert.FromBase64String(b64);

            var fileName = $"{baseName}.png";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(sigDir, fileName), bytes);
            signaturePath = $"/signatures/{fileName}";
        }

        var lr = new LeaveRequest
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RequestedDays = dto.RequestedDays,
            ActualDays = dto.RequestedDays,
            Status = "En attente",
            EmployeeComments = dto.EmployeeComments ?? string.Empty,
            EmployeeSignaturePath = signaturePath,
            SignatureDate = DateTime.Now,
            CreatedAt = DateTime.Now,
            CreatedBy = dto.UserId,
            LeaveTypeId = dto.LeaveTypeId,
            UserId = dto.UserId,
            PrivateNotes = "",
            CurrentStage = "Initial",
            CancellationReason = null,
            IsHalfDay = dto.IsHalfDay,
            HalfDayPeriod = dto.HalfDayPeriod ?? "FULL",
            ProofFilePath = proofFilePath,
            RequiresDirectorOverride = requireDirector,
            HierarchyId = requester.HierarchyId // ★ attache la demande à la hiérarchie
        };

        _db.LeaveRequests.Add(lr);
        await _db.SaveChangesAsync();

        await CreateApprovalPlanAsync(lr.LeaveRequestId);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Demande envoyée avec succès", id = lr.LeaveRequestId });
    }

    private async Task CreateApprovalPlanAsync(int leaveRequestId)
    {
        var req = await _db.LeaveRequests
            .Include(r => r.LeaveType).ThenInclude(t => t.Policy)
            .Include(r => r.User)
            .FirstAsync(r => r.LeaveRequestId == leaveRequestId);

        // Demande déjà soldée ? (sécurité)
        if (await _db.Approvals.AnyAsync(a => a.LeaveRequestId == leaveRequestId))
            return;

        // --- patch MANAGER: forcer Director/Pending quand c’est un manager qui demande
        if (string.Equals(req.User.Role, "Manager", StringComparison.OrdinalIgnoreCase))
        {
            // (facultatif) récupérer un directeur de la même hiérarchie si besoin d'afficher plus tard
            var directorId = await _db.Users.AsNoTracking()
                .Where(u => u.Role == "Director" && u.HierarchyId == req.HierarchyId)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();

            _db.Approvals.Add(new Approval
            {
                LeaveRequestId = req.LeaveRequestId,
                Level = "Director",
                Status = "Pending",
                NextApprovalOrder = 1,
                Comments = string.Empty
            });

            req.Status = "En attente";
            req.CurrentStage = "Director";
            await _db.SaveChangesAsync();
            return;
        }
        // --- fin patch

        // Construire le plan standard via le routeur
        var plan = await _router.BuildPlanAsync(req);

        if (plan.Count == 0)
        {
            // Aucun niveau : auto-approbation
            req.Status = "Approuvée";
            req.CurrentStage = "Approved";
            await _db.SaveChangesAsync();
            return;
        }

        // Enregistrer les approvals
        foreach (var (level, approverId, order, status) in plan)
        {
            _db.Approvals.Add(new Approval
            {
                LeaveRequestId = req.LeaveRequestId,
                Level = level,           // "Manager" | "Director" | "RH"
                Status = status,         // Serial: 1er Pending, suivants Blocked ; Parallel: tous Pending
                NextApprovalOrder = order,
                Comments = string.Empty,
                // si tu as un champ ApproverUserId, renseigne-le ici
                // ApproverUserId   = approverId
            });
        }

        // Poser l’étape courante
        var firstPending = plan.FirstOrDefault(p => p.status == "Pending");
        req.Status = "En attente";
        req.CurrentStage = firstPending.level;

        await _db.SaveChangesAsync();
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LeaveRequest leaveRequest)
    {
        if (id != leaveRequest.LeaveRequestId) return BadRequest("ID mismatch.");
        _db.Entry(leaveRequest).State = EntityState.Modified;
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.LeaveRequests.AnyAsync(lr => lr.LeaveRequestId == id)) return NotFound();
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.LeaveRequests.FindAsync(id);
        if (entity == null) return NotFound();
        _db.LeaveRequests.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("working-days")]
    public IActionResult GetWorkingDays(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("La date de début ne peut pas être après la date de fin.");

        var start = startDate.Date;
        var end = endDate.Date;

        var nonRecurring = _db.Holidays
            .Where(h => !h.IsRecurring && h.Date >= start && h.Date <= end)
            .Select(h => h.Date.Date)
            .ToList();

        var recurring = _db.Holidays
            .Where(h => h.IsRecurring)
            .Select(h => new { h.Date.Month, h.Date.Day })
            .ToList();

        var holidays = new HashSet<DateTime>(nonRecurring);
        for (var d = start; d <= end; d = d.AddDays(1))
            if (recurring.Any(r => r.Month == d.Month && r.Day == d.Day))
                holidays.Add(d);

        int workingDays = 0;
        for (var d = start; d <= end; d = d.AddDays(1))
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday &&
                !holidays.Contains(d))
                workingDays++;

        return Ok(new { workingDays });
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetRequestsByUser(int userId)
    {
        var data = await _db.LeaveRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new
            {
                r.LeaveRequestId,
                r.StartDate,
                r.EndDate,
                r.RequestedDays,
                r.Status,
                r.EmployeeComments,
                r.EmployeeSignaturePath,
                r.SignatureDate,
                r.CreatedAt,
                r.IsHalfDay,           // ⬅️ ajouté
                r.HalfDayPeriod,       // ⬅️ ajouté  ("AM"/"PM" ou "MORNING"/"AFTERNOON")
                LeaveType = new { r.LeaveType.LeaveTypeId, r.LeaveType.Name }
            })
            .ToListAsync();

        return Ok(data);
    }
    // GET: /api/LeaveRequest/check-overlap?userId=...&startDate=...&endDate=...
    [HttpGet("check-overlap")]
    public async Task<IActionResult> CheckOverlap([FromQuery] int userId,
                                                  [FromQuery] DateTime startDate,
                                                  [FromQuery] DateTime endDate)
    {
        if (startDate.Date > endDate.Date)
            return BadRequest("startDate > endDate");

        var blocking = new[] { "En attente", "Approuvée" };

        bool exists = await _db.LeaveRequests
            .Where(r => r.UserId == userId && blocking.Contains(r.Status))
            .AnyAsync(r => startDate.Date <= r.EndDate.Date && r.StartDate.Date <= endDate.Date);

        return Ok(new { overlap = exists });
    }

}
