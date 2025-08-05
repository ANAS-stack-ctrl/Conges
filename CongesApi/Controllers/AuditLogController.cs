using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongesApi.Data;
using CongesApi.Model;

namespace CongesApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAll()
        {
            return await _context.AuditLogs.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLog>> GetById(int id)
        {
            var item = await _context.AuditLogs.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<AuditLog>> Create(AuditLog item)
        {
            _context.AuditLogs.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = item.LogId }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, AuditLog item)
        {
            if (id != item.LogId) return BadRequest();
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.AuditLogs.FindAsync(id);
            if (item == null) return NotFound();
            _context.AuditLogs.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
