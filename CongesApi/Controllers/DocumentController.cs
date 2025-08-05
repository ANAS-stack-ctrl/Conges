using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Document
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _context.Documents
                .Include(d => d.LeaveRequest)
                .Include(d => d.DocumentCategory)
                .ToListAsync();
            return Ok(documents);
        }

        // GET: api/Document/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var document = await _context.Documents
                .Include(d => d.LeaveRequest)
                .Include(d => d.DocumentCategory)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null) return NotFound();

            return Ok(document);
        }

        // POST: api/Document
        [HttpPost]
        public async Task<IActionResult> Create(Document document)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = document.DocumentId }, document);
        }

        // PUT: api/Document/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Document document)
        {
            if (id != document.DocumentId) return BadRequest("ID mismatch");

            _context.Entry(document).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Documents.Any(e => e.DocumentId == id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Document/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
