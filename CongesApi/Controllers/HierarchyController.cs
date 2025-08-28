using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HierarchyController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public HierarchyController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────
        // DTOs
        // ─────────────────────────────────────────────────────
        public class CreateHierarchyDto
        {
            public string Name { get; set; } = "";
            public string? Code { get; set; }
            public string? Description { get; set; }
        }
        public class UpdateHierarchyDto : CreateHierarchyDto { }

        // ► Rôle optionnel : s’il est omis on prendra le rôle de l’utilisateur
        public class AddMemberDto
        {
            public int UserId { get; set; }
            public string? Role { get; set; }
        }

        // ─────────────────────────────────────────────────────
        // GET /api/Hierarchy
        // ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Hierarchies
                .AsNoTracking()
                .Select(h => new
                {
                    h.HierarchyId,
                    h.Name,
                    h.Code,
                    h.Description,
                    members = h.Members.Count
                })
                .ToListAsync();

            return Ok(list);
        }

        // ─────────────────────────────────────────────────────
        // GET /api/Hierarchy/{id}
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var h = await _db.Hierarchies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HierarchyId == id);

            return h == null ? NotFound() : Ok(h);
        }

        // ─────────────────────────────────────────────────────
        // POST /api/Hierarchy
        // ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHierarchyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Nom requis.");

            var h = new Hierarchy
            {
                Name = dto.Name.Trim(),
                Code = dto.Code?.Trim(),
                Description = dto.Description?.Trim()
            };

            _db.Hierarchies.Add(h);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Hiérarchie créée", id = h.HierarchyId });
        }

        // ─────────────────────────────────────────────────────
        // PUT /api/Hierarchy/{id}
        // ─────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateHierarchyDto dto)
        {
            var h = await _db.Hierarchies.FirstOrDefaultAsync(x => x.HierarchyId == id);
            if (h == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Name)) h.Name = dto.Name.Trim();
            h.Code = dto.Code?.Trim();
            h.Description = dto.Description?.Trim();

            await _db.SaveChangesAsync();
            return Ok(new { message = "Hiérarchie mise à jour" });
        }

        // ─────────────────────────────────────────────────────
        // DELETE /api/Hierarchy/{id}
        // ─────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var h = await _db.Hierarchies.FirstOrDefaultAsync(x => x.HierarchyId == id);
            if (h == null) return NotFound();

            _db.Hierarchies.Remove(h);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Hiérarchie supprimée" });
        }

        // ─────────────────────────────────────────────────────
        // GET /api/Hierarchy/{id}/members
        // ─────────────────────────────────────────────────────
        [HttpGet("{id:int}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            var exists = await _db.Hierarchies.AnyAsync(h => h.HierarchyId == id);
            if (!exists) return NotFound();

            var members = await _db.HierarchyMembers
                .Where(m => m.HierarchyId == id)
                .Include(m => m.User)
                .AsNoTracking()
                .Select(m => new
                {
                    m.UserId,
                    fullName = m.User!.FirstName + " " + m.User!.LastName,
                    email = m.User!.Email,
                    role = m.Role ?? m.User!.Role
                })
                .OrderBy(m => m.fullName)
                .ToListAsync();

            return Ok(members);
        }

        // ─────────────────────────────────────────────────────
        // POST /api/Hierarchy/{id}/members  body: { userId, role? }
        // ─────────────────────────────────────────────────────
        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberDto dto)
        {
            var exists = await _db.Hierarchies.AnyAsync(h => h.HierarchyId == id);
            if (!exists) return NotFound("Hiérarchie inexistante.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
            if (user == null) return BadRequest("Utilisateur introuvable.");

            var already = await _db.HierarchyMembers.AnyAsync(m => m.UserId == dto.UserId);
            if (already) return Conflict("Cet utilisateur appartient déjà à une hiérarchie.");

            var member = new HierarchyMember
            {
                HierarchyId = id,
                UserId = dto.UserId,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? user.Role : dto.Role!.Trim()
            };

            _db.HierarchyMembers.Add(member);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Membre ajouté." });
        }

        // ─────────────────────────────────────────────────────
        // DELETE /api/Hierarchy/{id}/members/{userId}
        // ─────────────────────────────────────────────────────
        [HttpDelete("{id:int}/members/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int id, int userId)
        {
            var m = await _db.HierarchyMembers
                .FirstOrDefaultAsync(x => x.HierarchyId == id && x.UserId == userId);

            if (m == null) return NotFound();

            _db.HierarchyMembers.Remove(m);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Membre retiré." });
        }

        // ─────────────────────────────────────────────────────
        // GET /api/Hierarchy/candidates?role=Employee
        // Utilisateurs sans hiérarchie, filtrage par rôle optionnel
        // ─────────────────────────────────────────────────────
        [HttpGet("candidates")]
        [HttpGet("{id:int}/candidates")]
        public async Task<IActionResult> GetCandidates([FromQuery] string? role)
        {
            var inUse = await _db.HierarchyMembers.Select(m => m.UserId).ToListAsync();
            var q = _db.Users.AsNoTracking().Where(u => !inUse.Contains(u.UserId));

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role);

            var list = await q
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    role = u.Role
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}
