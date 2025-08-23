using System;
using System.Text;
using System.Threading.Tasks;
using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------
        // POST: api/Auth/login
        // ------------------------------------------------------
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                return BadRequest("Email et mot de passe requis.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                Console.WriteLine("❌ Utilisateur introuvable pour l'email : " + request.Email);
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Debug utile
            Console.WriteLine("🔍 Utilisateur trouvé :");
            Console.WriteLine($" - Email DB         : '{user.Email}'");
            Console.WriteLine($" - Password Hash DB : '{user.PasswordHash}'");
            Console.WriteLine($" - Password Entré   : '{request.Password}'");
            Console.WriteLine($" - Password Entré (Bytes) : {BitConverter.ToString(Encoding.UTF8.GetBytes(request.Password))}");

            // Normalisation simple
            string cleanPassword = request.Password.Normalize().Trim();

            // ✅ Vérification via BCrypt
            bool passwordOk = BCrypt.Net.BCrypt.Verify(cleanPassword, user.PasswordHash);
            Console.WriteLine("✅ Mot de passe correct ? " + passwordOk);

            if (!passwordOk)
                return Unauthorized("Email ou mot de passe incorrect.");

            // ✅ Réponse (avec UserId)
            var response = new LoginResponse
            {
                UserId = user.UserId,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role,
                Token = "dummy-token-temporaire" // à remplacer si tu ajoutes du JWT plus tard
            };

            return Ok(response);
        }

        // ------------------------------------------------------
        // POST: api/Auth/register
        // ------------------------------------------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null)
                return BadRequest("Requête invalide.");

            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("L'email est requis.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Le mot de passe est requis.");

            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return BadRequest("Cet utilisateur existe déjà.");

            // Normalisation + hash BCrypt
            string cleanPassword = request.Password.Normalize().Trim();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(cleanPassword);

            Console.WriteLine("🔐 Hash généré pour " + request.Password + " : " + hashedPassword);

            var user = new User
            {
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                Email = request.Email.Trim(),
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                NationalID = string.IsNullOrWhiteSpace(request.NationalID) ? null : request.NationalID.Trim(),
                PasswordHash = hashedPassword,
                IsActive = true,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Utilisateur créé avec succès !");
        }
    }
}
