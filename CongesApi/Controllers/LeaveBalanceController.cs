using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;
using System.Linq;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LeaveBalanceController(ApplicationDbContext context) => _context = context;

        // GET: api/LeaveBalance/user/18
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetLeaveBalances(int userId)
        {
            var balances = await _context.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.UserId == userId)
                .Select(lb => new
                {
                    lb.Id,
                    lb.UserId,
                    lb.LeaveTypeId,
                    LeaveType = lb.LeaveType.Name,
                    Balance = lb.CurrentBalance
                })
                .ToListAsync();

            if (balances.Count == 0)
                return NotFound("Aucun solde trouvé pour cet utilisateur.");

            return Ok(balances);
        }

        // POST: api/LeaveBalance/set   → apply same balance to ALL leave types
        [HttpPost("set")]
        public async Task<IActionResult> SetLeaveBalanceForAll([FromBody] SetAllLeaveBalancesDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
            if (!userExists) return BadRequest("Utilisateur invalide.");

            var leaveTypes = await _context.LeaveTypes.Select(t => t.LeaveTypeId).ToListAsync();

            foreach (var typeId in leaveTypes)
            {
                var existing = await _context.LeaveBalances
                    .SingleOrDefaultAsync(lb => lb.UserId == dto.UserId && lb.LeaveTypeId == typeId);

                if (existing == null)
                {
                    _context.LeaveBalances.Add(new LeaveBalance
                    {
                        UserId = dto.UserId,
                        LeaveTypeId = typeId,
                        CurrentBalance = dto.CurrentBalance
                    });
                }
                else
                {
                    existing.CurrentBalance = dto.CurrentBalance;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Solde {dto.CurrentBalance} appliqué à tous les types pour l'utilisateur {dto.UserId}."
            });
        }

        // POST: api/LeaveBalance/set-one  → set balance for ONE type
        [HttpPost("set-one")]
        public async Task<IActionResult> SetLeaveBalanceOne([FromBody] SetLeaveBalanceDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
            if (!userExists) return BadRequest("Utilisateur invalide.");

            var typeExists = await _context.LeaveTypes.AnyAsync(t => t.LeaveTypeId == dto.LeaveTypeId);
            if (!typeExists) return BadRequest("Type de congé invalide.");

            var lb = await _context.LeaveBalances
                .SingleOrDefaultAsync(x => x.UserId == dto.UserId && x.LeaveTypeId == dto.LeaveTypeId);

            if (lb == null)
            {
                _context.LeaveBalances.Add(new LeaveBalance
                {
                    UserId = dto.UserId,
                    LeaveTypeId = dto.LeaveTypeId,
                    CurrentBalance = dto.Balance
                });
            }
            else
            {
                lb.CurrentBalance = dto.Balance;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Solde mis à jour." });
        }
    }
}
