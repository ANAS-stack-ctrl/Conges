using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveType
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leaveTypes = await _context.LeaveTypes
                .Include(lt => lt.ApprovalFlowType)
                .Include(lt => lt.Policy)
                .ToListAsync();
            return Ok(leaveTypes);
        }

        // GET: api/LeaveType/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var leaveType = await _context.LeaveTypes
                .Include(lt => lt.ApprovalFlowType)
                .Include(lt => lt.Policy)
                .FirstOrDefaultAsync(lt => lt.LeaveTypeId == id);

            if (leaveType == null) return NotFound();

            return Ok(leaveType);
        }

        // POST: api/LeaveType
        [HttpPost]
        public async Task<IActionResult> Create(LeaveType leaveType)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.LeaveTypes.Add(leaveType);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = leaveType.LeaveTypeId }, leaveType);
        }

        // PUT: api/LeaveType/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LeaveType leaveType)
        {
            if (id != leaveType.LeaveTypeId) return BadRequest("ID mismatch");

            _context.Entry(leaveType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LeaveTypes.Any(lt => lt.LeaveTypeId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/LeaveType/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null) return NotFound();

            _context.LeaveTypes.Remove(leaveType);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
