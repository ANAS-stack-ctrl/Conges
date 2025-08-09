using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CongesApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveTypeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LeaveTypeController(ApplicationDbContext context) => _context = context;

        // ─────────────────────────────────────────────────────────────
        // DTOs
        // ─────────────────────────────────────────────────────────────
        public class UpsertLeaveTypeDto
        {
            public string Name { get; set; }
            public bool RequiresProof { get; set; }
            public int ConsecutiveDays { get; set; }      // 0 = illimité
            public string ApprovalFlow { get; set; }      // clé vers ApprovalFlowType.FlowType
            public int PolicyId { get; set; }             // FK vers LeavePolicy
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/LeaveType  (projection légère pour l’UI admin)
        // ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.LeaveTypes
                // Include supprimés : projection = pas besoin de charger les entités
                .Select(t => new
                {
                    t.LeaveTypeId,
                    t.Name,
                    t.RequiresProof,
                    t.ConsecutiveDays,
                    t.ApprovalFlow,
                    Policy = t.Policy == null ? null : new
                    {
                        t.Policy.PolicyId,
                        // cast en types nullables pour éviter "Nullable object must have a value"
                        AllowHalfDay = (bool?)t.Policy.AllowHalfDay,
                        MaxConsecutiveDays = (int?)t.Policy.MaxConsecutiveDays,
                        MaxDurationDays = (int?)t.Policy.MaxDurationDays
                    }
                })
                .ToListAsync();

            return Ok(items);
        }

        // ─────────────────────────────────────────────────────────────
        // GET: api/LeaveType/{id}
        // ─────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _context.LeaveTypes
                // Include supprimés : projection = pas besoin
                .Where(t => t.LeaveTypeId == id)
                .Select(t => new
                {
                    t.LeaveTypeId,
                    t.Name,
                    t.RequiresProof,
                    t.ConsecutiveDays,
                    t.ApprovalFlow,
                    Policy = t.Policy == null ? null : new
                    {
                        t.Policy.PolicyId,
                        AllowHalfDay = (bool?)t.Policy.AllowHalfDay,
                        MinNoticeDays = (int?)t.Policy.MinNoticeDays,
                        BlackoutPeriods = t.Policy.BlackoutPeriods,
                        MaxDurationDays = (int?)t.Policy.MaxDurationDays,
                        MaxConsecutiveDays = (int?)t.Policy.MaxConsecutiveDays,
                        RequiresProof = (bool?)t.Policy.RequiresProof,
                        ProofMaxDelay = (int?)t.Policy.ProofMaxDelay,
                        RequiresHRApproval = (bool?)t.Policy.RequiresHRApproval,
                        RequiresManagerApproval = (bool?)t.Policy.RequiresManagerApproval,
                        RequiresDirectorApproval = (bool?)t.Policy.RequiresDirectorApproval,
                        IsPaid = (bool?)t.Policy.IsPaid
                    }
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            return Ok(item);
        }

        // ─────────────────────────────────────────────────────────────
        // POST: api/LeaveType   (création)
        // ─────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertLeaveTypeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Nom unique (insensible à la casse)
            var nameExists = await _context.LeaveTypes
                .AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower());
            if (nameExists) return BadRequest("Un type de congé avec ce nom existe déjà.");

            // FK valides
            var policyExists = await _context.LeavePolicies.AnyAsync(p => p.PolicyId == dto.PolicyId);
            if (!policyExists) return BadRequest("PolicyId invalide.");

            var flowExists = await _context.ApprovalFlowTypes.AnyAsync(f => f.FlowType == dto.ApprovalFlow);
            if (!flowExists) return BadRequest("ApprovalFlow invalide.");

            var entity = new LeaveType
            {
                Name = dto.Name?.Trim(),
                RequiresProof = dto.RequiresProof,
                ConsecutiveDays = dto.ConsecutiveDays < 0 ? 0 : dto.ConsecutiveDays,
                ApprovalFlow = dto.ApprovalFlow,
                PolicyId = dto.PolicyId
            };

            _context.LeaveTypes.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Type de congé créé", id = entity.LeaveTypeId });
        }

        // ─────────────────────────────────────────────────────────────
        // PUT: api/LeaveType/{id}  (mise à jour)
        // ─────────────────────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpsertLeaveTypeDto dto)
        {
            var entity = await _context.LeaveTypes.FindAsync(id);
            if (entity == null) return NotFound();

            // nom unique (hors lui-même)
            var nameExists = await _context.LeaveTypes
                .AnyAsync(t => t.LeaveTypeId != id && t.Name.ToLower() == dto.Name.Trim().ToLower());
            if (nameExists) return BadRequest("Un type de congé avec ce nom existe déjà.");

            var policyExists = await _context.LeavePolicies.AnyAsync(p => p.PolicyId == dto.PolicyId);
            if (!policyExists) return BadRequest("PolicyId invalide.");

            var flowExists = await _context.ApprovalFlowTypes.AnyAsync(f => f.FlowType == dto.ApprovalFlow);
            if (!flowExists) return BadRequest("ApprovalFlow invalide.");

            entity.Name = dto.Name?.Trim();
            entity.RequiresProof = dto.RequiresProof;
            entity.ConsecutiveDays = dto.ConsecutiveDays < 0 ? 0 : dto.ConsecutiveDays;
            entity.ApprovalFlow = dto.ApprovalFlow;
            entity.PolicyId = dto.PolicyId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Type de congé mis à jour" });
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE: api/LeaveType/{id}
        // Empêche la suppression si le type est utilisé
        // ─────────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.LeaveTypes.FindAsync(id);
            if (entity == null) return NotFound();

            var usedInRequests = await _context.LeaveRequests.AnyAsync(r => r.LeaveTypeId == id);
            var usedInBalances = await _context.LeaveBalances.AnyAsync(b => b.LeaveTypeId == id);
            if (usedInRequests || usedInBalances)
                return BadRequest("Impossible de supprimer ce type : il est déjà utilisé par des demandes ou des soldes.");

            _context.LeaveTypes.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Type de congé supprimé" });
        }
    }
}
