using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using samane.Models;
using samane.Services.Interfaces;
namespace samane.Controllers
{
    [ApiController, Authorize, Route("api/personnel-reviews")]
    public class PersonnelReviewsController : ControllerBase
    {
        private readonly IPersonnelReviewService service;
        public PersonnelReviewsController(IPersonnelReviewService service)
        {
            this.service = service;
        }
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ReviewQuery query,
                                              CancellationToken ct) => Ok(await service.ListAsync(query, ct));
        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id,
                                             CancellationToken ct) => Ok(await service.GetAsync(id, ct));
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReviewInput input, CancellationToken ct)
        {
            var id = await service.CreateAsync(input, ct);
            return Created("api/personnel-reviews/" + id, new { id });
        }
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] ReviewInput input, CancellationToken ct)
        {
            await service.UpdateAsync(id, input, ct);
            return NoContent();
        }
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, [FromBody] VersionInput input, CancellationToken ct)
        {
            await service.DeleteAsync(id, input.Version, ct);
            return NoContent();
        }
        [HttpPost("{id:long}/actions")]
        public async Task<IActionResult> Act(long id, [FromBody] ActionInput input, CancellationToken ct)
        {
            await service.ActAsync(id, input, ct);
            return NoContent();
        }
    }
}
