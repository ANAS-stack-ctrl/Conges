// Controllers/ManagerAssignmentController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ManagerAssignmentController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ManagerAssignmentController(ApplicationDbContext db) => _db = db;

    // ─────────────────────────────────────────────────────────
    // ► PAR HIÉRARCHIE : liste des affectations actives (avec noms)
    // GET /api/ManagerAssignment/hierarchy/{hierarchyId}
    // ─────────────────────────────────────────────────────────
    [HttpGet("hierarchy/{hierarchyId:int}")]
    public async Task<IActionResult> GetByHierarchy(int hierarchyId)
    {
        // On retourne SEULEMENT les affectations actives pour cette hiérarchie
        var list = await _db.ManagerAssignments
            .AsNoTracking()
            .Where(a => a.HierarchyId == hierarchyId && a.Active)
            .Select(a => new
            {
                a.ManagerAssignmentId,
                a.HierarchyId,
                a.EmployeeUserId,
                a.ManagerUserId,
                EmployeeName = _db.Users.Where(u => u.UserId == a.EmployeeUserId)
                                        .Select(u => u.FirstName + " " + u.LastName)
                                        .FirstOrDefault(),
                ManagerName = _db.Users.Where(u => u.UserId == a.ManagerUserId)
                                       .Select(u => u.FirstName + " " + u.LastName)
                                       .FirstOrDefault(),
            })
            .OrderBy(x => x.EmployeeName)
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────
    // ► PAR DIRECTEUR : membres (managers + employés) de SA hiérarchie
    // GET /api/ManagerAssignment/by-director/{directorId}/members
    // ─────────────────────────────────────────────────────────
    [HttpGet("by-director/{directorId:int}/members")]
    public async Task<IActionResult> GetMembersByDirector(int directorId)
    {
        var dir = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == directorId && u.Role == "Director")
            .Select(u => new { u.HierarchyId })
            .FirstOrDefaultAsync();

        if (dir == null || dir.HierarchyId == null)
            return NotFound("Directeur introuvable ou hiérarchie non définie.");

        var hid = dir.HierarchyId.Value;

        var managers = await _db.Users.AsNoTracking()
            .Where(u => u.HierarchyId == hid && u.Role == "Manager")
            .Select(u => new { u.UserId, fullName = u.FirstName + " " + u.LastName, u.Email })
            .OrderBy(x => x.fullName)
            .ToListAsync();

        var employees = await _db.Users.AsNoTracking()
            .Where(u => u.HierarchyId == hid && u.Role == "Employee")
            .Select(u => new { u.UserId, fullName = u.FirstName + " " + u.LastName, u.Email })
            .OrderBy(x => x.fullName)
            .ToListAsync();

        return Ok(new { managers, employees });
    }

    // ─────────────────────────────────────────────────────────
    // ► PAR DIRECTEUR : liste des affectations (actives) de SA hiérarchie
    // GET /api/ManagerAssignment/by-director/{directorId}/assignments
    // ─────────────────────────────────────────────────────────
    [HttpGet("by-director/{directorId:int}/assignments")]
    public async Task<IActionResult> GetAssignmentsByDirector(int directorId)
    {
        var dir = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == directorId && u.Role == "Director")
            .Select(u => new { u.HierarchyId })
            .FirstOrDefaultAsync();

        if (dir == null || dir.HierarchyId == null)
            return NotFound("Directeur introuvable ou hiérarchie non définie.");

        var hid = dir.HierarchyId.Value;

        var list = await _db.ManagerAssignments.AsNoTracking()
            .Where(a => a.HierarchyId == hid && a.Active)
            .Select(a => new
            {
                a.ManagerAssignmentId,
                a.HierarchyId,
                a.EmployeeUserId,
                a.ManagerUserId
            })
            .ToListAsync();

        return Ok(list);
    }

    // ─────────────────────────────────────────────────────────
    // ► AFFECTATION (bulk) : plusieurs employés → 1 manager
    // POST /api/ManagerAssignment/bulk
    // ─────────────────────────────────────────────────────────
    public class BulkAssignDto
    {
        public int HierarchyId { get; set; }
        public int ManagerUserId { get; set; }
        public int[] EmployeeUserIds { get; set; } = Array.Empty<int>();
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> AssignEmployeesBulk([FromBody] BulkAssignDto dto)
    {
        if (dto.HierarchyId <= 0) return BadRequest("HierarchyId requis.");
        if (dto.ManagerUserId <= 0) return BadRequest("ManagerUserId requis.");
        if (dto.EmployeeUserIds == null || dto.EmployeeUserIds.Length == 0)
            return BadRequest("Aucun employé fourni.");

        // Validations : le manager est bien "Manager" dans cette hiérarchie
        var managerOk = await _db.Users.AnyAsync(u =>
            u.UserId == dto.ManagerUserId &&
            u.HierarchyId == dto.HierarchyId &&
            u.Role == "Manager");

        if (!managerOk)
            return BadRequest("Manager invalide pour cette hiérarchie.");

        // Validations : les employés appartiennent bien à cette hiérarchie et sont des "Employee"
        var empIds = dto.EmployeeUserIds.Distinct().ToArray();
        var employeesOk = await _db.Users
            .Where(u => empIds.Contains(u.UserId))
            .AllAsync(u => u.HierarchyId == dto.HierarchyId && u.Role == "Employee");

        if (!employeesOk)
            return BadRequest("Un ou plusieurs employés ne sont pas dans la hiérarchie ou n'ont pas le rôle Employee.");

        // Upsert : une seule affectation ACTIVE par employé dans une hiérarchie
        foreach (var empId in empIds)
        {
            // Y a-t-il déjà une affectation active ?
            var current = await _db.ManagerAssignments
                .FirstOrDefaultAsync(a =>
                    a.HierarchyId == dto.HierarchyId &&
                    a.EmployeeUserId == empId &&
                    a.Active);

            if (current == null)
            {
                _db.ManagerAssignments.Add(new ManagerAssignment
                {
                    HierarchyId = dto.HierarchyId,
                    EmployeeUserId = empId,
                    ManagerUserId = dto.ManagerUserId,
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // si le manager change, on remplace
                if (current.ManagerUserId != dto.ManagerUserId)
                {
                    current.ManagerUserId = dto.ManagerUserId;
                    // on garde Active = true
                }
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Affectations enregistrées." });
    }

    // ─────────────────────────────────────────────────────────
    // ► SUPPRIMER (désactiver) une affectation
    // DELETE /api/ManagerAssignment/{assignmentId}
    // ─────────────────────────────────────────────────────────
    [HttpDelete("{assignmentId:int}")]
    public async Task<IActionResult> Remove(int assignmentId)
    {
        var a = await _db.ManagerAssignments.FirstOrDefaultAsync(x => x.ManagerAssignmentId == assignmentId);
        if (a == null) return NotFound();

        // Soft delete (recommandé car tu as la colonne Active)
        a.Active = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Affectation désactivée." });
    }
}
