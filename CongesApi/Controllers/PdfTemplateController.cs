using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfTemplateController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public PdfTemplateController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List() =>
            Ok(await _db.PdfTemplates.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) =>
            Ok(await _db.PdfTemplates.FindAsync(id) ?? (object)NotFound());

        [HttpPost]
        public async Task<IActionResult> Create(PdfTemplate tpl)
        {
            if (tpl.IsDefault)
            {
                var others = await _db.PdfTemplates.Where(t => t.IsDefault).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
            }
            tpl.UpdatedAt = DateTime.UtcNow;
            _db.PdfTemplates.Add(tpl);
            await _db.SaveChangesAsync();
            return Ok(tpl);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PdfTemplate tpl)
        {
            if (id != tpl.PdfTemplateId) return BadRequest();
            if (tpl.IsDefault)
            {
                var others = await _db.PdfTemplates.Where(t => t.IsDefault && t.PdfTemplateId != id).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
            }
            tpl.UpdatedAt = DateTime.UtcNow;
            _db.Entry(tpl).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tpl = await _db.PdfTemplates.FindAsync(id);
            if (tpl == null) return NotFound();
            _db.PdfTemplates.Remove(tpl);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
