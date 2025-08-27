using CongesApi.Data;
using CongesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPdfRenderer _pdf;

        public ExportController(ApplicationDbContext db, IPdfRenderer pdf)
        {
            _db = db;
            _pdf = pdf;
        }

        [HttpGet("leave-request/{id:int}")]
        public async Task<IActionResult> LeaveRequestPdf(int id)
        {
            var req = await _db.LeaveRequests
                .Include(r => r.LeaveType)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.LeaveRequestId == id);

            if (req == null) return NotFound();

            var approvals = await _db.Approvals
                .Include(a => a.User)
                .Where(a => a.LeaveRequestId == id)
                .OrderBy(a => a.Level)
                .ToListAsync();

            // base URL absolute -> https://localhost:7233
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // LOGO (wwwroot/assets/logo.png)
            var logoUrl = $"{baseUrl}/assets/logo.png";

            // SIGNATURE (si tu stockes un chemin genre "/signatures/xxx.png")
            string? signatureUrl = null;
            if (!string.IsNullOrWhiteSpace(req.EmployeeSignaturePath))
            {
                // s’assure que c’est bien absolu
                var rel = req.EmployeeSignaturePath!.StartsWith("/")
                    ? req.EmployeeSignaturePath
                    : "/" + req.EmployeeSignaturePath;
                signatureUrl = baseUrl + rel;
            }

            var html = _pdf.BuildHtml(req, approvals, logoUrl, signatureUrl);
            var bytes = await _pdf.RenderPdfAsync(html);

            return File(bytes, "application/pdf", $"demande-{id}.pdf");
        }
    }
}
