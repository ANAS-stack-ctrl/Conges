using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeavePolicyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeavePolicyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LeavePolicy
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var policies = await _context.LeavePolicies.ToListAsync();
            return Ok(policies);
        }

        // GET: api/LeavePolicy/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var policy = await _context.LeavePolicies.FindAsync(id);
            if (policy == null) return NotFound();

            return Ok(policy);
        }

        // POST: api/LeavePolicy
        [HttpPost]
        public async Task<IActionResult> Create(LeavePolicy policy)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.LeavePolicies.Add(policy);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = policy.PolicyId }, policy);
        }

        // PUT: api/LeavePolicy/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LeavePolicy policy)
        {
            if (id != policy.PolicyId) return BadRequest("ID mismatch");

            _context.Entry(policy).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LeavePolicies.Any(p => p.PolicyId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/LeavePolicy/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _context.LeavePolicies.FindAsync(id);
            if (policy == null) return NotFound();

            _context.LeavePolicies.Remove(policy);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
