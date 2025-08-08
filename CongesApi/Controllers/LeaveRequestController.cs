using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LeaveRequestController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ─────────────────────────────
        // Helpers
        // ─────────────────────────────
        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim().Replace(" ", "_");
        }

        private string EnsureWebRoot()
        {
            var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
                : _env.WebRootPath;

            if (!Directory.Exists(webRoot)) Directory.CreateDirectory(webRoot);
            return webRoot;
        }

        // GET: api/LeaveRequest/leave-types
        [HttpGet("leave-types")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var types = await _context.LeaveTypes
                .Select(t => new
                {
                    t.LeaveTypeId,
                    t.Name,
                    t.RequiresProof
                })
                .ToListAsync();

            return Ok(types);
        }

        // GET: api/LeaveRequest
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var leaveRequests = await _context.LeaveRequests
                .Include(lr => lr.User)
                    .ThenInclude(u => u.UserRole)
                .Include(lr => lr.LeaveType)
                .ToListAsync();

            return Ok(leaveRequests);
        }

        // GET: api/LeaveRequest/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.User)
                    .ThenInclude(u => u.UserRole)
                .Include(lr => lr.LeaveType)
                .FirstOrDefaultAsync(lr => lr.LeaveRequestId == id);

            if (leaveRequest == null)
                return NotFound();

            return Ok(leaveRequest);
        }

        // POST: api/LeaveRequest
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] LeaveRequestDto dto)
        {
            Console.WriteLine($"[DEBUG DTO] UserId={dto.UserId}, LeaveTypeId={dto.LeaveTypeId}, RequestedDays={dto.RequestedDays}");

            var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
            if (leaveType == null)
                return BadRequest("Type de congé invalide.");

            var userBalance = await _context.LeaveBalances
                .Where(lb => lb.UserId == dto.UserId && lb.LeaveTypeId == dto.LeaveTypeId)
                .Select(lb => (decimal?)lb.CurrentBalance)
                .FirstOrDefaultAsync() ?? 0m;

            if (userBalance < dto.RequestedDays)
                return BadRequest("Solde de congé insuffisant pour cette demande.");

            // ─────────────────────────────
            // 1) Gestion du justificatif
            // ─────────────────────────────
            if (leaveType.RequiresProof && dto.ProofFile == null)
                return BadRequest("Un justificatif est requis pour ce type de congé.");

            string proofFilePath = null;
            if (dto.ProofFile != null && dto.ProofFile.Length > 0)
            {
                var webRoot = EnsureWebRoot();
                var uploadsDir = Path.Combine(webRoot, "uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(dto.ProofFile.FileName);
                var originalName = SanitizeFileName(Path.GetFileNameWithoutExtension(dto.ProofFile.FileName));
                var proofFileName = $"{originalName}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                var proofPhysicalPath = Path.Combine(uploadsDir, proofFileName);

                using (var stream = new FileStream(proofPhysicalPath, FileMode.Create))
                    await dto.ProofFile.CopyToAsync(stream);

                proofFilePath = $"/uploads/{proofFileName}";
            }

            // ─────────────────────────────
            // 2) Sauvegarde de la signature
            // ─────────────────────────────
            string signaturePath = null;
            if (!string.IsNullOrWhiteSpace(dto.EmployeeSignatureBase64))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == dto.UserId);
                var first = user?.FirstName ?? "User";
                var last = user?.LastName ?? dto.UserId.ToString();

                var baseName = $"{SanitizeFileName(last)}_{SanitizeFileName(first)}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var webRoot = EnsureWebRoot();
                var signaturesDir = Path.Combine(webRoot, "signatures");
                if (!Directory.Exists(signaturesDir)) Directory.CreateDirectory(signaturesDir);

                var parts = dto.EmployeeSignatureBase64.Split(',');
                var base64 = parts.Length > 1 ? parts[1] : parts[0];
                var bytes = Convert.FromBase64String(base64);

                var signatureFileName = $"{baseName}.png";
                var signaturePhysicalPath = Path.Combine(signaturesDir, signatureFileName);
                await System.IO.File.WriteAllBytesAsync(signaturePhysicalPath, bytes);

                signaturePath = $"/signatures/{signatureFileName}";
            }

            var leaveRequest = new LeaveRequest
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                RequestedDays = dto.RequestedDays,
                ActualDays = dto.RequestedDays,
                Status = "En attente",
                EmployeeComments = dto.EmployeeComments,
                EmployeeSignaturePath = signaturePath,
                // Pas de ProofFilePath car non présent dans le modèle
                SignatureDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = dto.UserId,
                LeaveTypeId = dto.LeaveTypeId,
                UserId = dto.UserId,
                PrivateNotes = "",
                CurrentStage = "Initial",
                CancellationReason = null,
                IsHalfDay = dto.IsHalfDay,
                HalfDayPeriod = dto.HalfDayPeriod ?? "FULL"
            };

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Demande envoyée avec succès", id = leaveRequest.LeaveRequestId });
        }

        // PUT: api/LeaveRequest/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, LeaveRequest leaveRequest)
        {
            if (id != leaveRequest.LeaveRequestId)
                return BadRequest("ID mismatch");

            _context.Entry(leaveRequest).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LeaveRequests.Any(lr => lr.LeaveRequestId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/LeaveRequest/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest == null)
                return NotFound();

            _context.LeaveRequests.Remove(leaveRequest);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/LeaveRequest/working-days
        [HttpGet("working-days")]
        public IActionResult GetWorkingDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest("La date de début ne peut pas être après la date de fin.");

            var holidays = _context.Holidays
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .Select(h => h.Date.Date)
                .ToList();

            int workingDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday &&
                    date.DayOfWeek != DayOfWeek.Sunday &&
                    !holidays.Contains(date))
                {
                    workingDays++;
                }
            }

            return Ok(new { workingDays });
        }

        // GET: api/LeaveRequest/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRequestsByUser(int userId)
        {
            var requests = await _context.LeaveRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartDate)
                .Select(r => new
                {
                    r.LeaveRequestId,
                    r.StartDate,
                    r.EndDate,
                    r.RequestedDays,
                    r.Status,
                    r.EmployeeComments,
                    r.EmployeeSignaturePath,
                    r.SignatureDate,
                    r.CreatedAt,
                    LeaveType = new
                    {
                        r.LeaveType.LeaveTypeId,
                        r.LeaveType.Name
                    }
                })
                .ToListAsync();

            return Ok(requests);
        }
    }
}
