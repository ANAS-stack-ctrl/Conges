using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveRequest
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.User)
                    .ThenInclude(u => u.UserRole)   // Inclusion correcte de UserRole
                .Include(lr => lr.LeaveType)
                .ToListAsync();

            return Ok(leaveRequests);
        }

        // GET: api/LeaveRequest/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.User)
                    .ThenInclude(u => u.UserRole)   // Inclusion correcte de UserRole
                .Include(lr => lr.LeaveType)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            if (leaveRequest == null) return NotFound();

            return Ok(leaveRequest);
        }

        // POST: api/LeaveRequest
        [HttpPost]
        public async Task<IActionResult> Create(LeaveRequest leaveRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = leaveRequest.LeaveRequestId }, leaveRequest);
        }

        // PUT: api/LeaveRequest/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LeaveRequest leaveRequest)
        {
            if (id != leaveRequest.LeaveRequestId) return BadRequest("ID mismatch");

            _context.Entry(leaveRequest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LeaveRequests.Any(lr => lr.LeaveRequestId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/LeaveRequest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null) return NotFound();

            _context.LeaveRequests.Remove(leaveRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
