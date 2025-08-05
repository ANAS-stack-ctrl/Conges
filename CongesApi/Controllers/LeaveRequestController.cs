using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        // ✅ GET: Tous les types de congés (pour preuve obligatoire ou non)
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

        // ✅ GET: Toutes les demandes
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

        // ✅ GET: Demande par ID
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

        // ✅ POST: Créer une demande de congé
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LeaveRequest leaveRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifie que le type de congé est valide
            var leaveType = await _context.LeaveTypes.FindAsync(leaveRequest.LeaveTypeId);
            if (leaveType == null)
                return BadRequest("Type de congé invalide.");

            // Vérifie la présence d’un justificatif si requis
            if (leaveType.RequiresProof && string.IsNullOrWhiteSpace(leaveRequest.EmployeeSignaturePath))
                return BadRequest("Un justificatif est requis pour ce type de congé.");

            _context.LeaveRequests.Add(leaveRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = leaveRequest.LeaveRequestId }, leaveRequest);
        }

        // ✅ PUT: Mettre à jour une demande existante
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

        // ✅ DELETE: Supprimer une demande
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

        // ✅ GET: Calcul des jours ouvrables
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
    }
}
