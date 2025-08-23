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
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public UserController(ApplicationDbContext context) => _context = context;

        // The fixed bcrypt hash for password "admin"
        private const string DEFAULT_ADMIN_HASH =
            "$2a$11$SkqIVD58mHMmggKoibrF0eHaRqtOjRNvou0h1UZQeuSfodQ9Rt0C6";

        // --------------------------------------------------------------------
        // DTOs
        // --------------------------------------------------------------------
        public class CreateUserDto
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Role { get; set; } = "Employee"; // default
        }

        public class UpdateUserDto
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = ""; // optional
            public string NationalID { get; set; } = "";  // optional
        }

        public class ChangeRoleDto
        {
            public string Role { get; set; } = "";
        }

        // --------------------------------------------------------------------
        // GET: api/User  → list (null-safe)
        // --------------------------------------------------------------------
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
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    NationalID = u.NationalID ?? string.Empty,
                    u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // --------------------------------------------------------------------
        // GET: api/User/{id}  → details (null-safe)
        // --------------------------------------------------------------------
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
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    NationalID = u.NationalID ?? string.Empty,
                    u.IsActive
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        // --------------------------------------------------------------------
        // POST: api/User  → create (ALWAYS sets the same password hash)
        // --------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            // normalize email & basic checks
            var emailNorm = (dto.Email ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrWhiteSpace(emailNorm))
                return BadRequest("Email requis.");

            var exists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == emailNorm);
            if (exists) return BadRequest("Un utilisateur avec cet email existe déjà.");

            // role validation (if you maintain a UserRoles table)
            var roleToSet = string.IsNullOrWhiteSpace(dto.Role) ? "Employee" : dto.Role.Trim();
            var roleOk = await _context.UserRoles.AsNoTracking().AnyAsync(r => r.Role == roleToSet);
            if (!roleOk)
                return BadRequest("Rôle invalide.");

            var user = new User
            {
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                Email = (dto.Email ?? string.Empty).Trim(),
                Role = roleToSet,
                IsActive = true,
                PasswordHash = DEFAULT_ADMIN_HASH    // <<< fixed hash for all new users
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur créé", user.UserId });
        }

        // --------------------------------------------------------------------
        // PUT: api/User/{id}  → update basic info (null-safe)
        // --------------------------------------------------------------------
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

            await _context.SaveChangesAsync();
            return Ok(new { message = "Utilisateur mis à jour" });
        }

        // --------------------------------------------------------------------
        // PUT: api/User/{id}/role  → change role
        // --------------------------------------------------------------------
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            var roleToSet = (dto.Role ?? "").Trim();
            if (string.IsNullOrWhiteSpace(roleToSet))
                return BadRequest("Rôle requis.");

            var roleOk = await _context.UserRoles.AsNoTracking().AnyAsync(r => r.Role == roleToSet);
            if (!roleOk) return BadRequest("Rôle invalide.");

            user.Role = roleToSet;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rôle mis à jour" });
        }

        // --------------------------------------------------------------------
        // (Optional) POST: api/User/{id}/reset-password  → back to default hash
        // --------------------------------------------------------------------
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPasswordToDefault(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            user.PasswordHash = DEFAULT_ADMIN_HASH;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Mot de passe réinitialisé (hash par défaut)" });
        }

        // --------------------------------------------------------------------
        // DELETE: api/User/{id}
        // --------------------------------------------------------------------
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