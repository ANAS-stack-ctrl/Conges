using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApprovalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Approval
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var approvals = await _context.Approvals
                .Include(a => a.LeaveRequest)
                .Include(a => a.User)
                .ToListAsync();

            return Ok(approvals);
        }

        // GET: api/Approval/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var approval = await _context.Approvals
                .Include(a => a.LeaveRequest)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.ApprovalId == id);

            if (approval == null) return NotFound();

            return Ok(approval);
        }

        // POST: api/Approval
        [HttpPost]
        public async Task<IActionResult> Create(Approval approval)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = approval.ApprovalId }, approval);
        }

        // PUT: api/Approval/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Approval approval)
        {
            if (id != approval.ApprovalId) return BadRequest("ID mismatch");

            _context.Entry(approval).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Approvals.Any(a => a.ApprovalId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Approval/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var approval = await _context.Approvals.FindAsync(id);
            if (approval == null) return NotFound();

            _context.Approvals.Remove(approval);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
