using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public LeaveRequestController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ───────────────────── helpers fichiers
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

    // ───────────────────── helpers règles
    private static bool RangesOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart.Date <= bEnd.Date && bStart.Date <= aEnd.Date;

    /// <summary>
    /// Vrai si l’utilisateur a déjà une demande En attente/Approuvée qui chevauche.
    /// (Règle simple: on bloque tout chevauchement de dates, y compris ½ journée.)
    /// </summary>
    private Task<bool> HasOverlappingRequestAsync(int userId, DateTime start, DateTime end)
    {
        string[] blocking = new[] { "En attente", "Approuvée" };

        return _db.LeaveRequests
            .Where(r => r.UserId == userId && blocking.Contains(r.Status))
            .AnyAsync(r => start.Date <= r.EndDate.Date && r.StartDate.Date <= end.Date);
    }

    /// <summary>
    /// Blackouts actifs qui chevauchent, par portée (Global / LeaveType / User).
    /// </summary>
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
                        // (b.ScopeType == "Department" && b.DepartmentId == <si tu relies l'user>)
                        ))
            .ToListAsync();
    }

    // ───────────────────── types (pour le form employé)
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

    // ───────────────────── RH list + stats
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

        var data = await q.OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.LeaveRequestId,
                employee = r.User.FirstName + " " + r.User.LastName,
                type = r.LeaveType.Name,
                startDate = r.StartDate,
                endDate = r.EndDate,
                requestedDays = r.RequestedDays,
                status = r.Status,
                employeeComments = r.EmployeeComments,
                proofFilePath = r.ProofFilePath
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

    // ───────────────────── CRUD basique
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

    // ───────────────────── CREATE (Blackouts d’abord, pas de solde global)
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] LeaveRequestDto dto)
    {
        // 0) Type
        var leaveType = await _db.LeaveTypes
            .Include(t => t.Policy)
            .FirstOrDefaultAsync(t => t.LeaveTypeId == dto.LeaveTypeId);
        if (leaveType == null) return BadRequest("Type de congé invalide.");

        // 0.1) Blackouts — bloque si un blackout « Block » chevauche
        var bl = await GetOverlappingBlackoutsAsync(dto.UserId, dto.LeaveTypeId, dto.StartDate, dto.EndDate);
        var blocking = bl.FirstOrDefault(b => b.EnforceMode == "Block");
        if (blocking != null)
        {
            var msg = string.IsNullOrWhiteSpace(blocking.Reason)
                ? $"Période indisponible ({blocking.Name})."
                : $"Période indisponible ({blocking.Name}) : {blocking.Reason}";
            return BadRequest(msg);
        }
        // « RequireDirector » : on laisse passer mais on forcera un passage Directeur
        bool requireDirector = bl.Any(b => b.EnforceMode == "RequireDirector");

        // 0.2) Chevauchement d’une autre demande (En attente/Approuvée)
        if (await HasOverlappingRequestAsync(dto.UserId, dto.StartDate, dto.EndDate))
            return BadRequest("Vous avez déjà une demande En attente/Approuvée qui chevauche ces dates.");

        // 0.3) Règles type + solde par type
        decimal requested = dto.IsHalfDay ? 0.5m : (decimal)dto.RequestedDays;
        if (leaveType.ConsecutiveDays > 0 && requested > leaveType.ConsecutiveDays)
            return BadRequest($"Le type '{leaveType.Name}' autorise au max {leaveType.ConsecutiveDays} jours consécutifs.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == dto.UserId && b.LeaveTypeId == dto.LeaveTypeId);
        if (balance == null) return BadRequest("Aucun solde pour ce type. Contactez les RH.");

        var debit = dto.IsHalfDay ? (dto.RequestedDays > 0 ? 0.5m : 0m) : (decimal)dto.RequestedDays;
        if (balance.CurrentBalance < debit) return BadRequest("Solde insuffisant.");

        // 1) Justificatif
        string? proofFilePath = null;
        if (leaveType.RequiresProof && dto.ProofFile == null)
            return BadRequest("Un justificatif est requis pour ce type.");
        if (dto.ProofFile is { Length: > 0 })
        {
            var webRoot = EnsureWebRoot();
            var uploads = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(dto.ProofFile.FileName);
            var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(dto.ProofFile.FileName));
            var fileName = $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var physical = Path.Combine(uploads, fileName);

            using var fs = new FileStream(physical, FileMode.Create);
            await dto.ProofFile.CopyToAsync(fs);

            proofFilePath = $"/uploads/{fileName}";
        }

        // 2) Signature employé (optionnelle)
        string? signaturePath = null;
        if (!string.IsNullOrWhiteSpace(dto.EmployeeSignatureBase64))
        {
            var user = await _db.Users.FindAsync(dto.UserId);
            var baseName = $"{SanitizeFileName(user?.LastName ?? "User")}_{SanitizeFileName(user?.FirstName ?? dto.UserId.ToString())}_{DateTime.Now:yyyyMMdd_HHmmss}";

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

        // 3) Création LeaveRequest
        var lr = new LeaveRequest
        {
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RequestedDays = dto.RequestedDays,
            ActualDays = dto.RequestedDays,
            Status = "En attente",
            EmployeeComments = dto.EmployeeComments ?? "",
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
            RequiresDirectorOverride = requireDirector // <= propriété bool sur LeaveRequest
        };

        _db.LeaveRequests.Add(lr);
        await _db.SaveChangesAsync(); // => lr.LeaveRequestId

        // 4) Générer le plan d’approbation (en tenant compte du RequireDirector)
        await CreateApprovalPlanAsync(lr.LeaveRequestId);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Demande envoyée avec succès", id = lr.LeaveRequestId });
    }

    /// <summary>
    /// Crée les Approval à partir du LeaveType.ApprovalFlow et de LeavePolicy.
    /// Prend en compte RequiresDirectorOverride : ajoute "Director" si absent.
    /// </summary>
    private async Task CreateApprovalPlanAsync(int leaveRequestId)
    {
        const string PENDING = "Pending";
        const string BLOCKED = "Blocked";

        var req = await _db.LeaveRequests
            .Include(r => r.LeaveType).ThenInclude(t => t.Policy)
            .FirstAsync(r => r.LeaveRequestId == leaveRequestId);

        var flow = (req.LeaveType.ApprovalFlow ?? "Serial").Trim();
        var steps = new List<string>();

        if (req.LeaveType.Policy != null)
        {
            if (req.LeaveType.Policy.RequiresManagerApproval) steps.Add("Manager");
            if (req.LeaveType.Policy.RequiresDirectorApproval) steps.Add("Director");
            if (req.LeaveType.Policy.RequiresHRApproval) steps.Add("RH");
        }
        if (steps.Count == 0) steps.AddRange(new[] { "Manager", "RH" });

        if (req.RequiresDirectorOverride && !steps.Contains("Director"))
        {
            // Force le passage Directeur (après Manager si possible)
            steps.Insert(Math.Min(1, steps.Count), "Director");
        }

        if (flow.Equals("Parallel", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var level in steps)
            {
                _db.Approvals.Add(new Approval
                {
                    LeaveRequestId = leaveRequestId,
                    Level = level,
                    Status = PENDING,
                    NextApprovalOrder = 1,
                    Comments = ""
                });
            }
            req.Status = "En attente";
            req.CurrentStage = "Parallel";
        }
        else
        {
            var order = 1;
            foreach (var level in steps)
            {
                _db.Approvals.Add(new Approval
                {
                    LeaveRequestId = leaveRequestId,
                    Level = level,
                    Status = (order == 1) ? PENDING : BLOCKED,
                    NextApprovalOrder = order++,
                    Comments = ""
                });
            }
            req.Status = "En attente";
            req.CurrentStage = steps.First();
        }
    }

    // ───────────────────── Update / Delete / utilitaires
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
        if (startDate > endDate) return BadRequest("La date de début ne peut pas être après la date de fin.");

        var holidays = _db.Holidays
            .Where(h => h.Date >= startDate && h.Date <= endDate)
            .Select(h => h.Date.Date)
            .ToList();

        int workingDays = 0;
        for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !holidays.Contains(d))
                workingDays++;
        }
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
                LeaveType = new { r.LeaveType.LeaveTypeId, r.LeaveType.Name }
            })
            .ToListAsync();

        return Ok(data);
    }
}
