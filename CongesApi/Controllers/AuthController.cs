using CongesApi.DTOs;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongesApi.Data;
using CongesApi.Services;
using System.Text;

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

        // ✅ LOGIN
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Email et mot de passe requis.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                Console.WriteLine("❌ Utilisateur introuvable pour l'email : " + request.Email);
                return Unauthorized("Email ou mot de passe incorrect.");
            }

            // Debug : Log toutes les infos
            Console.WriteLine("🔍 Utilisateur trouvé :");
            Console.WriteLine($" - Email DB         : '{user.Email}'");
            Console.WriteLine($" - Password Hash DB : '{user.PasswordHash}'");
            Console.WriteLine($" - Password Entré   : '{request.Password}'");
            Console.WriteLine($" - Password Entré (Bytes) : {BitConverter.ToString(Encoding.UTF8.GetBytes(request.Password))}");

            // Normalisation
            string cleanPassword = request.Password.Normalize().Trim();

            // Comparaison
            bool passwordOk = PasswordHasher.Verify(cleanPassword, user.PasswordHash);

            Console.WriteLine("✅ Mot de passe correct ? " + passwordOk);

            if (!passwordOk)
                return Unauthorized("Email ou mot de passe incorrect.");

            // ✅ Réponse complète avec UserId
            var response = new LoginResponse
            {
                UserId = user.UserId,  // ✅ ici on utilise bien le bon nom de propriété
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role,
                Token = "dummy-token-temporaire"
            };


            return Ok(response);
        }

        // ✅ REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Le mot de passe est requis.");

            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
                return BadRequest("Cet utilisateur existe déjà.");

            string cleanPassword = request.Password.Normalize().Trim();
            string hashedPassword = PasswordHasher.Hash(cleanPassword);

            Console.WriteLine("🔐 Hash généré pour " + request.Password + " : " + hashedPassword);

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Role = request.Role,
                PhoneNumber = request.PhoneNumber,
                NationalID = request.NationalID,
                PasswordHash = hashedPassword,
                IsActive = true,
               
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Utilisateur créé avec succès !");
        }
    }
}
