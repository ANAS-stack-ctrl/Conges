using CongesApi.Data;
using CongesApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class HierarchyApprovalPolicyController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public HierarchyApprovalPolicyController(ApplicationDbContext db) { _db = db; }

    [HttpGet("{hierarchyId:int}")]
    public async Task<IActionResult> Get(int hierarchyId)
    {
        var p = await _db.HierarchyApprovalPolicies
            .FirstOrDefaultAsync(x => x.HierarchyId == hierarchyId);
        return Ok(p ?? new HierarchyApprovalPolicy { HierarchyId = hierarchyId });
    }

    [HttpPut("{hierarchyId:int}")]
    public async Task<IActionResult> Set(int hierarchyId, [FromBody] HierarchyApprovalPolicy dto)
    {
        var p = await _db.HierarchyApprovalPolicies
            .FirstOrDefaultAsync(x => x.HierarchyId == hierarchyId);

        if (p == null)
        {
            p = new HierarchyApprovalPolicy { HierarchyId = hierarchyId };
            _db.HierarchyApprovalPolicies.Add(p);
        }

        p.ManagerPeerFirst = dto.ManagerPeerFirst;
        p.RequiredPeerCount = Math.Max(1, dto.RequiredPeerCount);
        p.PeerSelectionMode = string.IsNullOrWhiteSpace(dto.PeerSelectionMode) ? "Any" : dto.PeerSelectionMode;
        p.FallbackToDirector = dto.FallbackToDirector;
        p.FallbackToHR = dto.FallbackToHR;

        await _db.SaveChangesAsync();
        return Ok(p);
    }
}
