using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Notification
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _context.Notifications
                .Include(n => n.NotificationType)
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/Notification/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var notification = await _context.Notifications
                .Include(n => n.NotificationType)
                .FirstOrDefaultAsync(n => n.NotificationId == id);

            if (notification == null) return NotFound();

            return Ok(notification);
        }

        // POST: api/Notification
        [HttpPost]
        public async Task<IActionResult> Create(Notification notification)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = notification.NotificationId }, notification);
        }

        // PUT: api/Notification/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Notification notification)
        {
            if (id != notification.NotificationId) return BadRequest("ID mismatch");

            _context.Entry(notification).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Notifications.Any(n => n.NotificationId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Notification/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
