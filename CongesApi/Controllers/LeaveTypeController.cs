using CongesApi.Data;
using CongesApi.DTOs;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveTypeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public LeaveTypeController(ApplicationDbContext context) => _context = context;

    // GET: api/LeaveType  (projection légère)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _context.LeaveTypes
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
                    MaxConsecutiveDays = (int?)t.Policy.MaxConsecutiveDays,
                    MaxDurationDays = (int?)t.Policy.MaxDurationDays
                }
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/LeaveType/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _context.LeaveTypes
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

    // POST: api/LeaveType
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Le nom est requis.");

        var exists = await _context.LeaveTypes
            .AnyAsync(t => t.Name.ToLower() == name.ToLower());
        if (exists) return BadRequest("Un type de congé avec ce nom existe déjà.");

        var flow = (dto.ApprovalFlow ?? "Serial").Trim().ToLower();
        flow = flow == "parallel" ? "Parallel" : "Serial";

        int? policyId = dto.PolicyId;
        if (policyId.HasValue && policyId.Value <= 0) policyId = null;
        if (policyId.HasValue)
        {
            var ok = await _context.LeavePolicies.AnyAsync(p => p.PolicyId == policyId.Value);
            if (!ok) return BadRequest("PolicyId invalide.");
        }

        var entity = new LeaveType
        {
            Name = name,
            RequiresProof = dto.RequiresProof,
            ConsecutiveDays = dto.ConsecutiveDays < 0 ? 0 : dto.ConsecutiveDays,
            ApprovalFlow = flow,
            PolicyId = policyId
        };

        _context.LeaveTypes.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = entity.LeaveTypeId }, new
        {
            entity.LeaveTypeId,
            entity.Name,
            entity.RequiresProof,
            entity.ConsecutiveDays,
            entity.ApprovalFlow,
            entity.PolicyId
        });
    }

    // PUT: api/LeaveType/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaveTypeDto dto)
    {
        var entity = await _context.LeaveTypes.FindAsync(id);
        if (entity == null) return NotFound();

        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Le nom est requis.");

        var nameExists = await _context.LeaveTypes
            .AnyAsync(t => t.LeaveTypeId != id && t.Name.ToLower() == name.ToLower());
        if (nameExists) return BadRequest("Un type de congé avec ce nom existe déjà.");

        var flow = (dto.ApprovalFlow ?? "Serial").Trim().ToLower();
        flow = flow == "parallel" ? "Parallel" : "Serial";

        int? policyId = dto.PolicyId;
        if (policyId.HasValue && policyId.Value <= 0) policyId = null;
        if (policyId.HasValue)
        {
            var ok = await _context.LeavePolicies.AnyAsync(p => p.PolicyId == policyId.Value);
            if (!ok) return BadRequest("PolicyId invalide.");
        }

        entity.Name = name;
        entity.RequiresProof = dto.RequiresProof;
        entity.ConsecutiveDays = dto.ConsecutiveDays < 0 ? 0 : dto.ConsecutiveDays;
        entity.ApprovalFlow = flow;
        entity.PolicyId = policyId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Type de congé mis à jour" });
    }

    // DELETE: api/LeaveType/{id}
    [HttpDelete("{id:int}")]
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
