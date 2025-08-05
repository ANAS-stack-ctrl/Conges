using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ NOUVELLE ROUTE : GET /api/LeaveRequest/leave-types
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

        // GET: api/LeaveRequest/5
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

        // ✅ POST: api/LeaveRequest
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeaveRequest leaveRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifie que le type de congé existe
            var leaveType = await _context.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
            if (leaveType == null)
                return BadRequest("Type de congé invalide.");

            // Vérifie que le champ "proof" est requis si nécessaire
            if (leaveType.RequiresProof && string.IsNullOrWhiteSpace(leaveRequest.ProofPath))
                return BadRequest("Un justificatif est requis pour ce type de congé.");

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

        // GET: api/LeaveRequest/working-days?startDate=2025-08-01&endDate=2025-08-12
        [HttpGet("working-days")]
        public IActionResult GetWorkingDays(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                return BadRequest("La date de début ne peut pas être après la date de fin.");
            }

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
    }
}
