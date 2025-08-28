// Controllers/ManagerDelegationsController.cs
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")] // => "api/ManagerDelegations"
public class ManagerDelegationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ManagerDelegationsController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────
    // GET /api/ManagerDelegations/by-director/{directorId}
    // Liste TOUTES les délégations (actives + passées) de la hiérarchie du directeur
    // ─────────────────────────────────────────────────────────
    [HttpGet("by-director/{directorId:int}")]
    public async Task<IActionResult> ListByDirector(int directorId)
    {
        var dir = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == directorId && u.Role == "Director")
            .Select(u => new { u.HierarchyId })
            .FirstOrDefaultAsync();

        if (dir == null || dir.HierarchyId == null)
            return NotFound("Directeur introuvable ou hiérarchie non définie.");

        var hid = dir.HierarchyId.Value;

        var list = await _db.ManagerDelegations.AsNoTracking()
            .Where(d => d.HierarchyId == hid)
            .OrderByDescending(d => d.Active).ThenByDescending(d => d.StartDate)
            .Select(d => new
            {
                d.ManagerDelegationId,
                d.HierarchyId,
                d.ManagerUserId,
                managerName = _db.Users.Where(u => u.UserId == d.ManagerUserId)
                                       .Select(u => u.FirstName + " " + u.LastName)
                                       .FirstOrDefault(),
                d.DelegateManagerUserId,
                delegateName = _db.Users.Where(u => u.UserId == d.DelegateManagerUserId)
                                        .Select(u => u.FirstName + " " + u.LastName)
                                        .FirstOrDefault(),
                d.StartDate,
                d.EndDate,
                d.Active
            })
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/ManagerDelegations/hierarchy/{hierarchyId}
    // (optionnel) liste les délégations actives d’une hiérarchie
    // ─────────────────────────────────────────────────────────
    [HttpGet("hierarchy/{hierarchyId:int}")]
    public async Task<IActionResult> ListByHierarchy(int hierarchyId)
    {
        var list = await _db.ManagerDelegations.AsNoTracking()
            .Where(d => d.HierarchyId == hierarchyId && d.Active)
            .OrderByDescending(d => d.StartDate)
            .Select(d => new
            {
                d.ManagerDelegationId,
                d.HierarchyId,
                d.ManagerUserId,
                managerName = _db.Users.Where(u => u.UserId == d.ManagerUserId)
                                       .Select(u => u.FirstName + " " + u.LastName)
                                       .FirstOrDefault(),
                d.DelegateManagerUserId,
                delegateName = _db.Users.Where(u => u.UserId == d.DelegateManagerUserId)
                                        .Select(u => u.FirstName + " " + u.LastName)
                                        .FirstOrDefault(),
                d.StartDate,
                d.EndDate,
                d.Active
            })
            .ToListAsync();

        return Ok(list);
    }

    public class CreateDelegationDto
    {
        public int DirectorId { get; set; }              // id du directeur (sécurité légère)
        public int ManagerUserId { get; set; }           // partant en congé
        public int DelegateManagerUserId { get; set; }   // remplaçant
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/ManagerDelegations
    // Corps: { directorId, managerUserId, delegateManagerUserId, startDate, endDate }
    // ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegationDto dto)
    {
        if (dto.StartDate.Date > dto.EndDate.Date)
            return BadRequest("La date de début ne peut pas être après la date de fin.");
        if (dto.ManagerUserId == dto.DelegateManagerUserId)
            return BadRequest("Le manager délégué doit être différent du manager en congé.");

        var dir = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == dto.DirectorId && u.Role == "Director")
            .Select(u => new { u.HierarchyId })
            .FirstOrDefaultAsync();

        if (dir == null || dir.HierarchyId == null)
            return BadRequest("Directeur invalide.");
        var hid = dir.HierarchyId.Value;

        // Les deux doivent être des Managers de la même hiérarchie
        var managerOk = await _db.Users.AnyAsync(u => u.UserId == dto.ManagerUserId && u.Role == "Manager" && u.HierarchyId == hid);
        var delegateOk = await _db.Users.AnyAsync(u => u.UserId == dto.DelegateManagerUserId && u.Role == "Manager" && u.HierarchyId == hid);
        if (!managerOk || !delegateOk)
            return BadRequest("Manager ou délégué invalide pour cette hiérarchie.");

        // Overlap protection: évite plusieurs délégations actives qui se chevauchent pour le même manager
        var hasOverlap = await _db.ManagerDelegations.AnyAsync(d =>
            d.HierarchyId == hid &&
            d.ManagerUserId == dto.ManagerUserId &&
            d.Active &&
            d.StartDate <= dto.EndDate.Date &&
            dto.StartDate.Date <= d.EndDate);
        if (hasOverlap)
            return BadRequest("Une délégation active chevauche déjà cette période pour ce manager.");

        var entity = new ManagerDelegation
        {
            HierarchyId = hid,
            ManagerUserId = dto.ManagerUserId,
            DelegateManagerUserId = dto.DelegateManagerUserId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Active = true
        };

        _db.ManagerDelegations.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Délégation créée", managerDelegationId = entity.ManagerDelegationId });
    }

    // ─────────────────────────────────────────────────────────
    // POST /api/ManagerDelegations/{id}/end
    // Termine la délégation maintenant (Active=false, borne EndDate)
    // ─────────────────────────────────────────────────────────
    [HttpPost("{id:int}/end")]
    public async Task<IActionResult> EndNow(int id)
    {
        var d = await _db.ManagerDelegations.FirstOrDefaultAsync(x => x.ManagerDelegationId == id);
        if (d == null) return NotFound("Délégation introuvable.");
        if (!d.Active) return Ok(new { message = "Déjà terminée." });

        var today = DateTime.UtcNow.Date;
        if (d.EndDate > today) d.EndDate = today;
        d.Active = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Délégation terminée." });
    }

    // ─────────────────────────────────────────────────────────
    // GET /api/ManagerDelegations/acting-for/{userId}
    // Liste des managers pour lesquels {userId} est actuellement délégué
    // ─────────────────────────────────────────────────────────
    [HttpGet("acting-for/{userId:int}")]
    public async Task<IActionResult> ActingFor(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var list = await _db.ManagerDelegations.AsNoTracking()
            .Where(d => d.Active
                        && d.DelegateManagerUserId == userId
                        && d.StartDate <= today
                        && today <= d.EndDate)
            .Select(d => new
            {
                d.ManagerUserId,
                managerName = _db.Users.Where(u => u.UserId == d.ManagerUserId)
                                       .Select(u => u.FirstName + " " + u.LastName)
                                       .FirstOrDefault(),
                d.EndDate
            })
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────
    // (Optionnel) GET /api/ManagerDelegations/alerts/by-director/{directorId}
    // Managers de la hiérarchie du directeur avec congés (non refusés)
    // à venir/courant SANS délégation couvrante posée
    // ─────────────────────────────────────────────────────────
    [HttpGet("alerts/by-director/{directorId:int}")]
    public async Task<IActionResult> AlertsForDirector(int directorId)
    {
        var dir = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == directorId && u.Role == "Director")
            .Select(u => new { u.HierarchyId })
            .FirstOrDefaultAsync();

        if (dir == null || dir.HierarchyId == null)
            return NotFound("Directeur introuvable ou hiérarchie non définie.");

        var hid = dir.HierarchyId.Value;
        var today = DateTime.UtcNow.Date;

        var managerLeaves = await _db.LeaveRequests
            .Include(r => r.User)
            .Where(r => r.User.HierarchyId == hid
                     && r.User.Role == "Manager"
                     && r.Status != "Refusée"
                     && r.EndDate.Date >= today)
            .Select(r => new
            {
                r.LeaveRequestId,
                r.UserId,
                managerName = r.User.FirstName + " " + r.User.LastName,
                r.StartDate,
                r.EndDate
            })
            .ToListAsync();

        var missing = new List<object>();
        foreach (var lr in managerLeaves)
        {
            bool covered = await _db.ManagerDelegations.AnyAsync(d =>
                d.Active && d.HierarchyId == hid && d.ManagerUserId == lr.UserId &&
                d.StartDate <= lr.EndDate.Date && lr.StartDate.Date <= d.EndDate);
            if (!covered) missing.Add(lr);
        }

        return Ok(missing);
    }
}
