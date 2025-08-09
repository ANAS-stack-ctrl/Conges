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

        // --------------------------------------------------------------------
        // DTOs
        // --------------------------------------------------------------------
        public class CreateUserDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; } = "Employee"; // valeur par défaut
        }

        public class UpdateUserDto
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }

        public class ChangeRoleDto
        {
            public string Role { get; set; }
        }

        // --------------------------------------------------------------------
        // GET: api/User  → liste légère pour tableau admin
        // --------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users
                .Include(u => u.UserRole) // navigation (clé = Role)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // --------------------------------------------------------------------
        // GET: api/User/{id}
        // --------------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    Role = u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();
            return Ok(user);
        }

        // --------------------------------------------------------------------
        // POST: api/User  → créer un utilisateur
        // --------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // (Optionnel) unicité email
            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists) return BadRequest("Un utilisateur avec cet email existe déjà.");

            // Rôle valide ?
            var roleOk = await _context.UserRoles.AnyAsync(r => r.Role == dto.Role);
            if (!roleOk) return BadRequest("Rôle invalide.");

            var user = new User
            {
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                Email = dto.Email?.Trim(),
                Role = dto.Role,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur créé", user.UserId });
        }

        // --------------------------------------------------------------------
        // PUT: api/User/{id}  → modifier infos de base (nom, prénom, email)
        // --------------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName?.Trim() ?? user.FirstName;
            user.LastName = dto.LastName?.Trim() ?? user.LastName;
            user.Email = dto.Email?.Trim() ?? user.Email;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Utilisateur mis à jour" });
        }

        // --------------------------------------------------------------------
        // PUT: api/User/{id}/role  → changer le rôle
        // --------------------------------------------------------------------
        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var roleOk = await _context.UserRoles.AnyAsync(r => r.Role == dto.Role);
            if (!roleOk) return BadRequest("Rôle invalide.");

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rôle mis à jour" });
        }

        // --------------------------------------------------------------------
        // DELETE: api/User/{id}
        // --------------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur supprimé" });
        }
    }
}
