using Claims.Contracts;
using Claims.Domain;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class ClaimsController(ClaimService claimService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<Claim>> GetAsync() => claimService.GetAllAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Claim>> GetAsync(string id) =>
        await claimService.GetByIdAsync(id) is { } claim ? Ok(claim) : NotFound();

    [HttpPost]
    public async Task<ActionResult<Claim>> CreateAsync(CreateClaimRequest request) =>
        Ok(await claimService.CreateAsync(request.ToClaim()));

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id) =>
        await claimService.DeleteAsync(id) ? Ok() : NotFound();
}
