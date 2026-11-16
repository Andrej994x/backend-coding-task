using Claims.Contracts;
using Claims.Domain;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController(CoverService coverService) : ControllerBase
{
    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremium(
        [BindRequired] DateTime startDate,
        [BindRequired] DateTime endDate,
        [BindRequired] CoverType coverType) =>
        Ok(coverService.ComputePremium(startDate.AsUtc(), endDate.AsUtc(), coverType));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync() =>
        Ok(await coverService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id) =>
        await coverService.GetByIdAsync(id) is { } cover ? Ok(cover) : NotFound();

    [HttpPost]
    public async Task<ActionResult<Cover>> CreateAsync(CreateCoverRequest request) =>
        Ok(await coverService.CreateAsync(request.ToCover()));

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id) =>
        await coverService.DeleteAsync(id) ? Ok() : NotFound();
}
