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
    public Task<Claim?> GetAsync(string id) => claimService.GetByIdAsync(id);

    [HttpPost]
    public async Task<ActionResult<Claim>> CreateAsync(Claim claim) =>
        Ok(await claimService.CreateAsync(claim));

    [HttpDelete("{id}")]
    public Task DeleteAsync(string id) => claimService.DeleteAsync(id);
}
