using BCrypt.Net;
using CongesApi.Data;
using CongesApi.Model;
using System.Threading.Tasks;

namespace CongesApi.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(string email, string password, string firstName, string lastName)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword,
                FirstName = firstName,
                LastName = lastName,
                Role = "User", // ou autre selon ta logique
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = 1,
               
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
