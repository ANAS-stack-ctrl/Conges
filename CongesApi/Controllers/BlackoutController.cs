// Controllers/BlackoutController.cs
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlackoutController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BlackoutController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────
    private static string NormalizeEnforceMode(string? mode)
    {
        var m = (mode ?? "").Trim().ToLowerInvariant();
        return m switch
        {
            // français
            "avertir" => "Warn",
            "bloquer" => "Block",
            // anglais (au cas où le front envoie déjà ça)
            "warn" => "Warn",
            "block" => "Block",
            "requiredirector" => "RequireDirector",
            _ => "" // inconnu
        };
    }

    private static string NormalizeScope(string? scope)
    {
        var s = (scope ?? "").Trim().ToLowerInvariant();
        return s switch
        {
            "global" => "Global",
            "leavetype" or "type" => "LeaveType",
            "department" or "departement" => "Department",
            "user" or "utilisateur" => "User",
            _ => "Global"
        };
    }

    // ─────────────────────────────────────────────────────────
    // RH/Admin: liste paginée + filtres
    // GET /api/Blackout/admin?active=true|false|null&scope=Global&from=...&to=...
    // ─────────────────────────────────────────────────────────
    [HttpGet("admin")]
    public async Task<IActionResult> ListAdmin(
        [FromQuery] bool? active,
        [FromQuery] string? scope,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var q = _db.BlackoutPeriods.AsQueryable();

        if (active.HasValue) q = q.Where(b => b.IsActive == active.Value);

        var scopeNorm = string.IsNullOrWhiteSpace(scope) ? null : NormalizeScope(scope);
        if (!string.IsNullOrEmpty(scopeNorm))
            q = q.Where(b => b.ScopeType == scopeNorm);

        if (from.HasValue && to.HasValue)
            q = q.Where(b => b.StartDate <= to.Value && from.Value <= b.EndDate);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(b => b.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, items });
    }

    // ─────────────────────────────────────────────────────────
    // Employé: blackout actifs sur une plage (calendrier)
    // GET /api/Blackout/active?from=...&to=...&userId=&leaveTypeId=
    // ─────────────────────────────────────────────────────────
    [HttpGet("active")]
    public async Task<IActionResult> Active([FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] int? userId, [FromQuery] int? leaveTypeId)
    {
        if (from == default || to == default || to < from)
            return BadRequest("Plage de dates invalide.");

        var q = _db.BlackoutPeriods
            .Where(b => b.IsActive && b.StartDate <= to && from <= b.EndDate);

        if (leaveTypeId.HasValue)
            q = q.Where(b =>
                b.ScopeType == "Global" ||
                (b.ScopeType == "LeaveType" && b.LeaveTypeId == leaveTypeId.Value));

        if (userId.HasValue)
            q = q.Where(b =>
                b.ScopeType != "User" || b.UserId == userId.Value);

        var list = await q.OrderBy(b => b.StartDate).ToListAsync();
        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/Blackout/admin
    // Body: BlackoutPeriod (ou DTO équivalent)
    // ─────────────────────────────────────────────────────────
    [HttpPost("admin")]
    public async Task<IActionResult> Create([FromBody] BlackoutPeriod dto)
    {
        if (dto == null) return BadRequest("Payload manquant.");
        if (dto.EndDate < dto.StartDate) return BadRequest("La date de fin doit être ≥ la date de début.");

        // Normalisations
        dto.ScopeType = NormalizeScope(dto.ScopeType);
        dto.EnforceMode = NormalizeEnforceMode(dto.EnforceMode);
        if (string.IsNullOrEmpty(dto.EnforceMode))
            return BadRequest("EnforceMode invalide (Avertir | Bloquer | RequireDirector).");

        // Sécurité: pour certains scopes on force les clefs associées à null
        dto.LeaveTypeId = dto.ScopeType == "LeaveType" ? dto.LeaveTypeId : null;
        dto.DepartmentId = dto.ScopeType == "Department" ? dto.DepartmentId : null;
        dto.UserId = dto.ScopeType == "User" ? dto.UserId : null;

        // Détection de doublon (EF-traduisible: chevauchement inline)
        var exists = await _db.BlackoutPeriods.AnyAsync(b =>
            b.IsActive &&
            b.ScopeType == dto.ScopeType &&
            b.LeaveTypeId == dto.LeaveTypeId &&
            b.DepartmentId == dto.DepartmentId &&
            b.UserId == dto.UserId &&
            b.StartDate <= dto.EndDate &&
            dto.StartDate <= b.EndDate
        );
        if (exists)
        {
            // On ne bloque pas la création si tu préfères : commente le return ci-dessous.
            return BadRequest("Un blackout existant chevauche cette plage pour ce scope.");
        }

        _db.BlackoutPeriods.Add(dto);
        await _db.SaveChangesAsync();
        return Ok(new { id = dto.BlackoutPeriodId });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /api/Blackout/admin/{id}
    // ─────────────────────────────────────────────────────────
    [HttpPut("admin/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BlackoutPeriod dto)
    {
        var b = await _db.BlackoutPeriods.FindAsync(id);
        if (b == null) return NotFound();

        if (dto.EndDate < dto.StartDate) return BadRequest("La date de fin doit être ≥ la date de début.");

        var scope = NormalizeScope(dto.ScopeType);
        var mode = NormalizeEnforceMode(dto.EnforceMode);
        if (string.IsNullOrEmpty(mode))
            return BadRequest("EnforceMode invalide (Avertir | Bloquer | RequireDirector).");

        // Mise à jour
        b.Name = dto.Name?.Trim() ?? "";
        b.StartDate = dto.StartDate;
        b.EndDate = dto.EndDate;
        b.ScopeType = scope;
        b.LeaveTypeId = scope == "LeaveType" ? dto.LeaveTypeId : null;
        b.DepartmentId = scope == "Department" ? dto.DepartmentId : null;
        b.UserId = scope == "User" ? dto.UserId : null;
        b.EnforceMode = mode;
        b.Reason = dto.Reason ?? "";
        b.IsActive = dto.IsActive;

        // Vérif chevauchement (hors lui-même)
        var overlap = await _db.BlackoutPeriods.AnyAsync(x =>
            x.BlackoutPeriodId != id &&
            x.IsActive &&
            x.ScopeType == b.ScopeType &&
            x.LeaveTypeId == b.LeaveTypeId &&
            x.DepartmentId == b.DepartmentId &&
            x.UserId == b.UserId &&
            x.StartDate <= b.EndDate &&
            b.StartDate <= x.EndDate
        );
        if (overlap)
            return BadRequest("Un autre blackout actif chevauche cette plage.");

        await _db.SaveChangesAsync();
        return Ok(new { message = "Updated" });
    }

    // ─────────────────────────────────────────────────────────
    // DELETE /api/Blackout/admin/{id}
    // ─────────────────────────────────────────────────────────
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.BlackoutPeriods.FindAsync(id);
        if (b == null) return NotFound();
        _db.BlackoutPeriods.Remove(b);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}
