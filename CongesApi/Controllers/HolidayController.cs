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
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string Description { get; set; } = "";
        public bool IsRecurring { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class HolidayController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HolidayController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ──────────────────────────────── LISTE
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

        // ──────────────────────────────── GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            return Ok(holiday);
        }

        // ──────────────────────────────── PLAGE (déroule les récurrents)
        // GET: api/Holiday/range?from=2025-01-01&to=2025-12-31
        [HttpGet("range")]
        public async Task<IActionResult> GetRange([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (from > to) return BadRequest("from > to");

            var nonRecurring = await _context.Holidays
                .Where(h => !h.IsRecurring && h.Date.Date >= from.Date && h.Date.Date <= to.Date)
                .Select(h => new { h.Date, h.Description })
                .ToListAsync();

            var recurring = await _context.Holidays
                .Where(h => h.IsRecurring)
                .Select(h => new { h.Date.Month, h.Date.Day, h.Description })
                .ToListAsync();

            var results = new List<object>();
            results.AddRange(nonRecurring);

            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                var found = recurring.FirstOrDefault(r => r.Month == d.Month && r.Day == d.Day);
                if (found != null)
                {
                    results.Add(new { Date = d, Description = found.Description });
                }
            }

            return Ok(results.OrderBy(x => ((DateTime)x!.GetType().GetProperty("Date")!.GetValue(x)!).Date));
        }

        // ──────────────────────────────── CRUD
        [HttpPost]
        public async Task<IActionResult> Create(Holiday holiday)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = holiday.HolidayId }, holiday);
        }

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
                        IsRecurring = item.IsRecurring
                    });
                }
            }
            var added = await _context.SaveChangesAsync();
            return Ok(new { added });
        }

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var holiday = await _context.Holidays.FindAsync(id);
            if (holiday == null) return NotFound();

            _context.Holidays.Remove(holiday);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ──────────────────────────────── SEED MAROC — FIXES (récurrents)
        // POST: api/Holiday/seed/morocco/fixed
        [HttpPost("seed/morocco/fixed")]
        public async Task<IActionResult> SeedMoroccoFixed()
        {
            // Liste officielle des jours fixes récurrents (dont Yennayer depuis 2024)
            var fixedRecurring = new (int Month, int Day, string Name)[]
            {
                (1, 1,  "Nouvel an"),
                (1, 11, "Manifeste de l'Indépendance"),
                (1, 13, "Nouvel an amazigh (Yennayer)"),
                (5, 1,  "Fête du Travail"),
                (7, 30, "Fête du Trône"),
                (8, 14, "Allégeance Oued Eddahab"),
                (8, 20, "Révolution du Roi et du Peuple"),
                (8, 21, "Anniversaire de SM le Roi / Fête de la Jeunesse"),
                (11, 6, "Marche Verte"),
                (11, 18,"Fête de l'Indépendance")
            };

            int added = 0;
            foreach (var f in fixedRecurring)
            {
                var exists = await _context.Holidays.AnyAsync(h =>
                    h.IsRecurring &&
                    h.Date.Month == f.Month && h.Date.Day == f.Day);

                if (!exists)
                {
                    _context.Holidays.Add(new Holiday
                    {
                        // Année arbitraire pour récurrents
                        Date = new DateTime(2000, f.Month, f.Day),
                        Description = f.Name,
                        IsRecurring = true
                    });
                    added++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { added });
        }

        // ──────────────────────────────── SEED MAROC — FÊTES HÉGIRIENNES (mobiles)
        // Ajoute pour UNE année grégorienne donnée des entrées non-récurrentes.
        // POST: api/Holiday/seed/morocco/hijri/2025
        [HttpPost("seed/morocco/hijri/{year:int}")]
        public async Task<IActionResult> SeedMoroccoHijriForYear([FromRoute] int year)
        {
            if (year < 1900 || year > 2100)
                return BadRequest("Année hors plage raisonnable (1900–2100).");

            var hc = new HijriCalendar();
            var from = new DateTime(year, 1, 1);
            var to = new DateTime(year, 12, 31);

            // Cibles (mois hégiriens: 1=Muharram, 3=Rabi' I, 10=Shawwal, 12=Dhu al-Hijjah)
            var targets = new List<(int HijriMonth, int HijriDay, string Name)>
            {
                (10, 1,  "Aïd al-Fitr"),
                (10, 2,  "Aïd al-Fitr (2e jour)"),

                (12, 10, "Aïd al-Adha"),
                (12, 11, "Aïd al-Adha (2e jour)"),

                (1,  1,  "Nouvel an de l’Hégire"), // 1 Muharram

                (3,  12, "Mawlid (naissance du Prophète)"),
                (3,  13, "Mawlid (2e jour)")
            };

            // Balayage de l'année grégorienne, conversion Hijri et ajout si correspondance.
            int added = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                int hm = hc.GetMonth(d);
                int hd = hc.GetDayOfMonth(d);
                var match = targets.FirstOrDefault(t => t.HijriMonth == hm && t.HijriDay == hd);

                if (!string.IsNullOrEmpty(match.Name))
                {
                    bool exists = await _context.Holidays
                        .AnyAsync(h => !h.IsRecurring && h.Date.Date == d.Date && h.Description == match.Name);

                    if (!exists)
                    {
                        _context.Holidays.Add(new Holiday
                        {
                            Date = d.Date,
                            Description = match.Name,
                            IsRecurring = false
                        });
                        added++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { year, added });
        }

        // POST: api/Holiday/seed/morocco/hijri-range?from=2025&to=2028
        [HttpPost("seed/morocco/hijri-range")]
        public async Task<IActionResult> SeedMoroccoHijriRange([FromQuery] int from = 2024, [FromQuery] int to = 2026)
        {
            if (from > to) return BadRequest("from > to");
            int total = 0;
            for (int y = from; y <= to; y++)
            {
                var res = await SeedMoroccoHijriForYear(y) as OkObjectResult;
                if (res?.Value is not null)
                {
                    var dict = res.Value.GetType()
                        .GetProperties()
                        .ToDictionary(p => p.Name, p => p.GetValue(res.Value));
                    if (dict.TryGetValue("added", out var a) && a is int ai) total += ai;
                }
            }
            return Ok(new { from, to, added = total });
        }
    }
}


