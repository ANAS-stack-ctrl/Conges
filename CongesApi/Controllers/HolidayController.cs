using CongesApi.Model;
using CongesApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CongesApi.Controllers
{
    public class HolidayBulkDto
    {
        [Required]
        public List<HolidayDto> Items { get; set; } = new();
    }

    public class HolidayDto
    {
        [Required] public DateTime Date { get; set; }
        [Required] public string Description { get; set; } = "";
        public bool IsRecurring { get; set; }
        public int DurationDays { get; set; } = 1;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class HolidayController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public HolidayController(ApplicationDbContext context) => _context = context;

        // ───────────────────────── LISTE
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var holidays = await _context.Holidays
                .OrderBy(h => h.IsRecurring)
                .ThenBy(h => h.Date.Month).ThenBy(h => h.Date.Day)
                .ThenBy(h => h.Date)
                .ToListAsync();

            return Ok(holidays);
        }

        // ───────────────────────── GET BY ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            return holiday is null ? NotFound() : Ok(holiday);
        }

        // ───────────────────────── RANGE (déroule récurrents) — utile pour le calcul
        // GET: api/Holiday/range?from=2025-01-01&to=2025-12-31
        [HttpGet("range")]
        public async Task<IActionResult> GetRange([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to) return BadRequest("from > to");

            var nonRecurring = await _context.Holidays
                .Where(h => !h.IsRecurring && h.Date.Date >= from.Date && h.Date.Date <= to.Date)
                .Select(h => new { h.Date, h.Description, h.DurationDays })
                .ToListAsync();

            var recurring = await _context.Holidays
                .Where(h => h.IsRecurring)
                .Select(h => new { h.Date.Month, h.Date.Day, h.Description, h.DurationDays })
                .ToListAsync();

            var results = new List<object>();
            results.AddRange(nonRecurring);

            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                var found = recurring.FirstOrDefault(r => r.Month == d.Month && r.Day == d.Day);
                if (found != default)
                {
                    results.Add(new { Date = d, Description = found.Description, DurationDays = found.DurationDays });
                }
            }

            return Ok(results.OrderBy(x => ((DateTime)x!.GetType().GetProperty("Date")!.GetValue(x)!).Date));
        }

        // ───────────────────────── CRUD
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HolidayDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.DurationDays < 1) dto.DurationDays = 1;

            var entity = new Holiday
            {
                Date = dto.Date.Date,
                Description = dto.Description.Trim(),
                IsRecurring = dto.IsRecurring,
                DurationDays = dto.DurationDays
            };

            _context.Holidays.Add(entity);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = entity.HolidayId }, entity);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] HolidayDto dto)
        {
            var entity = await _context.Holidays.FindAsync(id);
            if (entity is null) return NotFound();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            entity.Date = dto.Date.Date;
            entity.Description = dto.Description.Trim();
            entity.IsRecurring = dto.IsRecurring;
            entity.DurationDays = Math.Max(1, dto.DurationDays);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ───────────────────────── BULK (optionnel)
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] HolidayBulkDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            foreach (var item in dto.Items)
            {
                var exists = await _context.Holidays.AnyAsync(h =>
                    h.IsRecurring == item.IsRecurring &&
                    (item.IsRecurring
                        ? (h.Date.Month == item.Date.Month && h.Date.Day == item.Date.Day)
                        : (h.Date.Date == item.Date.Date)) &&
                    h.Description == item.Description);

                if (!exists)
                {
                    _context.Holidays.Add(new Holiday
                    {
                        Date = item.Date.Date,
                        Description = item.Description.Trim(),
                        IsRecurring = item.IsRecurring,
                        DurationDays = Math.Max(1, item.DurationDays)
                    });
                }
            }
            var added = await _context.SaveChangesAsync();
            return Ok(new { added });
        }

        // ───────────────────────── SEED MAROC (fixes)
        [HttpPost("seed/morocco/fixed")]
        public async Task<IActionResult> SeedMoroccoFixed()
        {
            var fixedRecurring = new (int Month, int Day, string Name, int Days)[]
            {
                (1, 1,  "Nouvel an", 1),
                (1, 11, "Manifeste de l'Indépendance", 1),
                (1, 13, "Nouvel an amazigh (Yennayer)", 1),
                (5, 1,  "Fête du Travail", 1),
                (7, 30, "Fête du Trône", 1),
                (8, 14, "Allégeance Oued Eddahab", 1),
                (8, 20, "Révolution du Roi et du Peuple", 1),
                (8, 21, "Anniversaire de SM le Roi / Fête de la Jeunesse", 1),
                (11, 6, "Marche Verte", 1),
                (11, 18,"Fête de l'Indépendance", 1)
            };

            int added = 0;
            foreach (var f in fixedRecurring)
            {
                bool exists = await _context.Holidays.AnyAsync(h =>
                    h.IsRecurring && h.Date.Month == f.Month && h.Date.Day == f.Day);

                if (!exists)
                {
                    _context.Holidays.Add(new Holiday
                    {
                        Date = new DateTime(2000, f.Month, f.Day),
                        Description = f.Name,
                        IsRecurring = true,
                        DurationDays = f.Days
                    });
                    added++;
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { added });
        }

        // ───────────────────────── SEED HÉGIRIEN (mobiles) – identique, DurationDays=1 par défaut
        [HttpPost("seed/morocco/hijri/{year:int}")]
        public async Task<IActionResult> SeedMoroccoHijriForYear([FromRoute] int year)
        {
            if (year < 1900 || year > 2100) return BadRequest("Année hors plage (1900–2100).");

            var hc = new HijriCalendar();
            var from = new DateTime(year, 1, 1);
            var to = new DateTime(year, 12, 31);

            var targets = new List<(int HijriMonth, int HijriDay, string Name, int Days)>
            {
                (10, 1,  "Aïd al-Fitr", 1),
                (10, 2,  "Aïd al-Fitr (2e jour)", 1),
                (12, 10, "Aïd al-Adha", 1),
                (12, 11, "Aïd al-Adha (2e jour)", 1),
                (1,  1,  "Nouvel an de l’Hégire", 1),
                (3,  12, "Mawlid (naissance du Prophète)", 1),
                (3,  13, "Mawlid (2e jour)", 1)
            };

            int added = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                int hm = hc.GetMonth(d);
                int hd = hc.GetDayOfMonth(d);
                var match = targets.FirstOrDefault(t => t.HijriMonth == hm && t.HijriDay == hd);

                if (!string.IsNullOrEmpty(match.Name))
                {
                    bool exists = await _context.Holidays.AnyAsync(h =>
                        !h.IsRecurring && h.Date.Date == d.Date && h.Description == match.Name);

                    if (!exists)
                    {
                        _context.Holidays.Add(new Holiday
                        {
                            Date = d.Date,
                            Description = match.Name,
                            IsRecurring = false,
                            DurationDays = match.Days
                        });
                        added++;
                    }
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { year, added });
        }
    }
}
