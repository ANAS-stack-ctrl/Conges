using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HolidayController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HolidayController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Holiday
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var holidays = await _context.Holidays.ToListAsync();
            return Ok(holidays);
        }

        // GET: api/Holiday/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            return Ok(holiday);
        }

        // POST: api/Holiday
        [HttpPost]
        public async Task<IActionResult> Create(Holiday holiday)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = holiday.HolidayId }, holiday);
        }

        // PUT: api/Holiday/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Holiday holiday)
        {
            if (id != holiday.HolidayId) return BadRequest("ID mismatch");

            _context.Entry(holiday).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Holidays.Any(h => h.HolidayId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Holiday/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
