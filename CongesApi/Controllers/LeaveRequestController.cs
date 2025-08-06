using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CongesApi.DTOs;

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
            var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
            if (leaveType == null)
                return BadRequest("Type de congé invalide.");

            string proofFilePath = null;
            if (leaveType.RequiresProof)
            {
                if (dto.ProofFile == null)
                    return BadRequest("Un justificatif est requis pour ce type de congé.");

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.ProofFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProofFile.CopyToAsync(stream);
                }

                proofFilePath = $"/uploads/{fileName}";
            }

            // Sauvegarde de la signature
            string signaturePath = null;
            if (!string.IsNullOrWhiteSpace(dto.EmployeeSignatureBase64))
            {
                var imageData = dto.EmployeeSignatureBase64.Split(',')[1];
                var bytes = Convert.FromBase64String(imageData);
                var signatureFileName = Guid.NewGuid() + ".png";
                var signatureFullPath = Path.Combine(_env.WebRootPath, "signatures");
                if (!Directory.Exists(signatureFullPath))
                    Directory.CreateDirectory(signatureFullPath);

                var finalPath = Path.Combine(signatureFullPath, signatureFileName);
                await System.IO.File.WriteAllBytesAsync(finalPath, bytes);
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
                SignatureDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = dto.UserId,
                LeaveTypeId = dto.LeaveTypeId,
                UserId = dto.UserId,
                PrivateNotes = "",
                CurrentStage = "Initial",
                CancellationReason = null,
                IsHalfDay = dto.IsHalfDay,  // ✅ virgule et pas point-virgule
                HalfDayPeriod = dto.HalfDayPeriod ?? "FULL"
            };


            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = leaveRequest.LeaveRequestId }, leaveRequest);
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
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday && !holidays.Contains(date))
                {
                    workingDays++;
                }
            }

            return Ok(new { workingDays });
        }
    }
}
