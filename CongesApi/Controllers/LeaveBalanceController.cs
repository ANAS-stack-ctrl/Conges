using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongesApi.Model;      // Pour LeaveBalance, LeaveType
using CongesApi.Data;       // Pour ApplicationDbContext
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 GET: api/LeaveBalance/18
        [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetLeaveBalances(int userId)
        {
            var balances = await _context.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.UserId == userId)
                .Select(lb => new
                {
                    LeaveType = lb.LeaveType.Name, // 🔧 Corrigé ici
                    Balance = lb.CurrentBalance
                })
                .ToListAsync();

            if (!balances.Any())
            {
                return NotFound("Aucun solde trouvé pour cet utilisateur.");
            }

            return Ok(balances);
        }

        // 🔹 POST: api/LeaveBalance
        [HttpPost]
        public async Task<IActionResult> AddLeaveBalance([FromBody] LeaveBalance newBalance)
        {
            var exists = await _context.LeaveBalances
                .AnyAsync(lb => lb.UserId == newBalance.UserId && lb.LeaveTypeId == newBalance.LeaveTypeId);

            if (exists)
            {
                return Conflict("Un solde existe déjà pour ce type de congé.");
            }

            _context.LeaveBalances.Add(newBalance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeaveBalances), new { userId = newBalance.UserId }, newBalance);
        }
    }
}
