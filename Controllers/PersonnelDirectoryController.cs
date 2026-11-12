using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using samane.Models;
using samane.Security;
using samane.Services.Interfaces;

namespace samane.Controllers
{
    [ApiController, Authorize(Roles = "PersonnelDirectory.Manager"), Route("api/personnel-directory")]
    public class PersonnelDirectoryController : ControllerBase
    {
        private readonly IPersonnelDirectoryService service;
        private readonly ICurrentActor actor;
        public PersonnelDirectoryController(IPersonnelDirectoryService service, ICurrentActor actor)
        {
            this.service = service;
            this.actor = actor;
        }

        private void CheckActor()
        {
            if (Request.Headers["X-Directory-User"] != actor.Id)
                throw new ApiException(409, "حساب کاربری تغییر کرده است؛ صفحه را تازه‌سازی کنید");
        }

        [HttpGet("identity")]
        public IActionResult Identity() => Ok(new { userId = actor.Id, name = actor.Name });
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] DirectoryQuery query, CancellationToken ct) => Ok(await service.ListAsync(query, ct));
        [HttpPost("search"), RequestSizeLimit(64 * 1024)]
        public async Task<IActionResult> Search([FromBody] DirectoryQuery query, CancellationToken ct)
        {
            Response.Headers["Cache-Control"] = "no-store";
            return Ok(await service.ListAsync(query, ct));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id, CancellationToken ct) => Ok(await service.GetAsync(id, ct));
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DirectoryInput input, CancellationToken ct)
        {
            CheckActor();
            var id = await service.CreateAsync(input, ct);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpPost("{id}/update")]
        public async Task<IActionResult> Update(string id, [FromBody] DirectoryInput input, CancellationToken ct)
        {
            CheckActor();
            await service.UpdateAsync(id, input, ct);
            return NoContent();
        }

        [HttpPost("{id}/delete")]
        public async Task<IActionResult> Delete(string id, [FromBody] VersionInput input, CancellationToken ct)
        {
            CheckActor();
            await service.DeleteAsync(id, input.Version, ct);
            return NoContent();
        }
    }
}
