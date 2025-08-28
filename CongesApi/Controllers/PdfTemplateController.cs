// Controllers/PdfTemplateController.cs
using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PdfTemplateController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PdfTemplateController(ApplicationDbContext db) => _db = db;

    // GET /api/PdfTemplate
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await _db.PdfTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new {
                t.PdfTemplateId,
                t.Name,
                t.IsDefault
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET /api/PdfTemplate/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var t = await _db.PdfTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.PdfTemplateId == id);
        if (t == null) return NotFound();
        return Ok(new
        {
            t.PdfTemplateId,
            t.Name,
            t.Html,
            t.IsDefault
        });
    }

    public class SavePdfTemplateDto
    {
        public string Name { get; set; } = "";
        public string Html { get; set; } = "";
        public bool IsDefault { get; set; }
    }

    // POST /api/PdfTemplate
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SavePdfTemplateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Nom requis.");
        if (string.IsNullOrWhiteSpace(dto.Html)) return BadRequest("HTML requis.");

        var entity = new PdfTemplate
        {
            Name = dto.Name.Trim(),
            Html = dto.Html,
            IsDefault = dto.IsDefault
        };

        if (dto.IsDefault)
        {
            var all = await _db.PdfTemplates.ToListAsync();
            foreach (var x in all) x.IsDefault = false;
        }

        _db.PdfTemplates.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { id = entity.PdfTemplateId, message = "Créé" });
    }

    // PUT /api/PdfTemplate/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SavePdfTemplateDto dto)
    {
        var t = await _db.PdfTemplates.FirstOrDefaultAsync(x => x.PdfTemplateId == id);
        if (t == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Nom requis.");
        if (string.IsNullOrWhiteSpace(dto.Html)) return BadRequest("HTML requis.");

        t.Name = dto.Name.Trim();
        t.Html = dto.Html;
        t.IsDefault = dto.IsDefault;

        if (dto.IsDefault)
        {
            // s'assurer que lui seul est par défaut
            var others = await _db.PdfTemplates.Where(x => x.PdfTemplateId != id).ToListAsync();
            foreach (var x in others) x.IsDefault = false;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Mis à jour" });
    }

    // DELETE /api/PdfTemplate/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.PdfTemplates.FirstOrDefaultAsync(x => x.PdfTemplateId == id);
        if (t == null) return NotFound();
        _db.PdfTemplates.Remove(t);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Supprimé" });
    }

    // (Optionnel) POST /api/PdfTemplate/preview
    // Utilisé si tu veux prévisualiser côté serveur
    public class PreviewDto { public string Html { get; set; } = ""; public object? SampleData { get; set; } }
    [HttpPost("preview")]
    public IActionResult Preview([FromBody] PreviewDto dto)
    {
        // Ici on renvoie tel quel (le front peut faire le rendu handlebars/templating côté client).
        // Si tu as un moteur de template côté serveur, c’est l’endroit pour l’appeler.
        return Ok(new { html = dto.Html });
    }
}
