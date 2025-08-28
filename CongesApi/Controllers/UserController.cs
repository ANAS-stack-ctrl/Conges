using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserController(ApplicationDbContext context) => _context = context;

        private const string DEFAULT_ADMIN_HASH =
            "$2a$11$SkqIVD58mHMmggKoibrF0eHaRqtOjRNvou0h1UZQeuSfodQ9Rt0C6";

        private static readonly HashSet<string> AllowedRoles = new()
        { "Employee", "Manager", "Director", "RH", "Admin" };

        private async Task<string?> GetOrCreateRoleAsync(string roleRaw)
        {
            var norm = (roleRaw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(norm)) return null;

            var exist = await _context.UserRoles
                .Where(r => r.Role.ToLower() == norm.ToLower())
                .Select(r => r.Role)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(exist))
                return exist;

            var match = AllowedRoles.FirstOrDefault(r => r.ToLower() == norm.ToLower());
            if (match == null) return null;

            _context.UserRoles.Add(new UserRole { Role = match });
            await _context.SaveChangesAsync();
            return match;
        }

        // ─────────────────────────────────────────────────────────────
        // Parse hiérarchie : accepte 123, "123", "H1"/"H2", ou Nom exact
        // ─────────────────────────────────────────────────────────────
        private async Task<int?> ParseHierarchyAsync(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            // Essayer un entier direct
            if (int.TryParse(raw, out var idNum))
            {
                var ok = await _context.Hierarchies.AsNoTracking()
                    .AnyAsync(h => h.HierarchyId == idNum);
                return ok ? idNum : null;
            }

            var val = raw.Trim();

            // Essayer par Code exact (H1, H2, …)
            var byCode = await _context.Hierarchies.AsNoTracking()
                .Where(h => h.Code != null && h.Code.ToLower() == val.ToLower())
                .Select(h => h.HierarchyId)
                .FirstOrDefaultAsync();
            if (byCode != 0) return byCode;

            // Essayer par Nom exact
            var byName = await _context.Hierarchies.AsNoTracking()
                .Where(h => h.Name.ToLower() == val.ToLower())
                .Select(h => h.HierarchyId)
                .FirstOrDefaultAsync();
            if (byName != 0) return byName;

            return null;
        }

        // ─────────────────────────────────────────────────────────────
        // DTOs (HierarchyId sous forme de string tolérante)
        // ─────────────────────────────────────────────────────────────
        public class CreateUserDto
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "Employee";
            public string? HierarchyId { get; set; }   // ← peut être "2", "H1", "H2", "Equipe A"…
        }

        public class UpdateUserDto
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string NationalID { get; set; } = "";
            public string? HierarchyId { get; set; }   // ← idem (string tolérante)
            public string? Role { get; set; }
        }

        public class ChangeRoleDto { public string Role { get; set; } = ""; }

        // GET list
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRole)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Role = u.Role,
                    u.HierarchyId,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    NationalID = u.NationalID ?? string.Empty,
                    u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET by id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRole)
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Role = u.Role,
                    u.HierarchyId,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    NationalID = u.NationalID ?? string.Empty,
                    u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var emailNorm = (dto.Email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(emailNorm))
                return BadRequest("Email requis.");

            var exists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == emailNorm);
            if (exists) return BadRequest("Un utilisateur avec cet email existe déjà.");

            var roleCanon = await GetOrCreateRoleAsync(dto.Role ?? "Employee");
            if (string.IsNullOrEmpty(roleCanon))
                return BadRequest("Rôle invalide.");

            int? hierarchyId = null;
            if (!string.IsNullOrWhiteSpace(dto.HierarchyId))
            {
                hierarchyId = await ParseHierarchyAsync(dto.HierarchyId);
                if (hierarchyId == null) return BadRequest("Hiérarchie invalide.");
            }

            var user = new User
            {
                FirstName = (dto.FirstName ?? "").Trim(),
                LastName = (dto.LastName ?? "").Trim(),
                Email = (dto.Email ?? "").Trim(),
                Role = roleCanon,
                IsActive = true,
                PasswordHash = DEFAULT_ADMIN_HASH,
                HierarchyId = hierarchyId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (hierarchyId.HasValue)
            {
                var already = await _context.HierarchyMembers
                    .AnyAsync(m => m.UserId == user.UserId);
                if (!already)
                {
                    _context.HierarchyMembers.Add(new HierarchyMember
                    {
                        HierarchyId = hierarchyId.Value,
                        UserId = user.UserId,
                        Role = roleCanon
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Utilisateur créé", userId = user.UserId });
        }

        // PUT update
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var newEmailNorm = dto.Email.Trim().ToLower();
                var exists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId != id && u.Email.ToLower() == newEmailNorm);
                if (exists) return BadRequest("Un autre utilisateur utilise déjà cet email.");
                user.Email = dto.Email.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(dto.NationalID))
                user.NationalID = dto.NationalID.Trim();

            // Hiérarchie : accepte ID, "H1"/"H2", ou Nom
            if (!string.IsNullOrWhiteSpace(dto.HierarchyId))
            {
                var parsed = await ParseHierarchyAsync(dto.HierarchyId);
                if (parsed == null) return BadRequest("Hiérarchie invalide.");

                user.HierarchyId = parsed.Value;

                var link = await _context.HierarchyMembers
                    .FirstOrDefaultAsync(m => m.UserId == id);
                if (link == null)
                {
                    _context.HierarchyMembers.Add(new HierarchyMember
                    {
                        HierarchyId = parsed.Value,
                        UserId = id,
                        Role = user.Role
                    });
                }
                else
                {
                    link.HierarchyId = parsed.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var roleCanon = await GetOrCreateRoleAsync(dto.Role);
                if (string.IsNullOrEmpty(roleCanon))
                    return BadRequest("Rôle invalide.");

                user.Role = roleCanon;

                var link = await _context.HierarchyMembers
                    .FirstOrDefaultAsync(m => m.UserId == id);
                if (link != null) link.Role = roleCanon;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Utilisateur mis à jour" });
        }

        // PUT change role
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            var roleCanon = await GetOrCreateRoleAsync(dto.Role);
            if (string.IsNullOrEmpty(roleCanon))
                return BadRequest("Rôle invalide.");

            user.Role = roleCanon;

            var link = await _context.HierarchyMembers
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (link != null) link.Role = roleCanon;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Rôle mis à jour" });
        }

        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPasswordToDefault(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.PasswordHash = DEFAULT_ADMIN_HASH;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Mot de passe réinitialisé (hash par défaut)" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur supprimé" });
        }
    }
}
