using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveBalanceAdjustmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceAdjustmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveBalanceAdjustment
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var adjustments = await _context.LeaveBalanceAdjustments
                .Include(a => a.User)
                .Include(a => a.LeaveRequest)
                .ToListAsync();

            return Ok(adjustments);
        }

        // GET: api/LeaveBalanceAdjustment/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var adjustment = await _context.LeaveBalanceAdjustments
                .Include(a => a.User)
                .Include(a => a.LeaveRequest)
                .FirstOrDefaultAsync(a => a.AdjustmentId == id);

            if (adjustment == null) return NotFound();

            return Ok(adjustment);
        }

        // POST: api/LeaveBalanceAdjustment
        [HttpPost]
        public async Task<IActionResult> Create(LeaveBalanceAdjustment adjustment)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.LeaveBalanceAdjustments.Add(adjustment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = adjustment.AdjustmentId }, adjustment);
        }

        // PUT: api/LeaveBalanceAdjustment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LeaveBalanceAdjustment adjustment)
        {
            if (id != adjustment.AdjustmentId) return BadRequest("ID mismatch");

            _context.Entry(adjustment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LeaveBalanceAdjustments.Any(a => a.AdjustmentId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/LeaveBalanceAdjustment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var adjustment = await _context.LeaveBalanceAdjustments.FindAsync(id);
            if (adjustment == null) return NotFound();

            _context.LeaveBalanceAdjustments.Remove(adjustment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
