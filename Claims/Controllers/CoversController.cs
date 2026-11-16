using Claims.Domain;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

[ApiController]
[Route("[controller]")]
public class CoversController(CoverService coverService) : ControllerBase
{
    [HttpPost("compute")]
    public ActionResult<decimal> ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType) =>
        Ok(coverService.ComputePremium(startDate, endDate, coverType));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cover>>> GetAsync() =>
        Ok(await coverService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Cover>> GetAsync(string id) =>
        Ok(await coverService.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<Cover>> CreateAsync(Cover cover) =>
        Ok(await coverService.CreateAsync(cover));

    [HttpDelete("{id}")]
    public Task DeleteAsync(string id) => coverService.DeleteAsync(id);
}
