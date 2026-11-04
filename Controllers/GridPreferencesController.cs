using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using samane.Security;
using samane.Services.Interfaces;
namespace samane.Controllers
{
    [ApiController, Authorize, Route("api/grid-preferences")]
    public class GridPreferencesController : ControllerBase
    {
        private readonly IGridPreferenceService service;
        private readonly ICurrentActor actor;
        public GridPreferencesController(IGridPreferenceService service, ICurrentActor actor)
        {
            this.service = service;
            this.actor = actor;
        }
        [HttpGet("identity")]
        public IActionResult Identity()
        {
            var columns = new System.Collections.Generic.List<string>();
            if (actor.Owner || actor.Evaluator || actor.Management)
            {
                columns.Add("ownerOpinion");
                columns.Add("evaluationManagerOpinion");
            }
            if (actor.Owner || actor.Evaluator)
                columns.Add("managementOpinion");
            return Ok(new { userId = actor.Id, name = actor.Name, columns });
        }
        [HttpGet("{gridId}")]
        public async Task<IActionResult> Get(string gridId, CancellationToken ct)
        {
            Response.Headers["Cache-Control"] = "no-store";
            return Ok(await service.GetAsync(gridId, ct));
        }
        [HttpPut("{gridId}")]
        public async Task<IActionResult> Put(string gridId, [FromBody] JObject settings, CancellationToken ct)
        {
            if (Request.Headers["X-Requested-With"] != "PersonnelReview")
                return BadRequest();
            await service.SaveAsync(gridId, settings, Request.Headers["X-Grid-User"].ToString(), ct);
            return NoContent();
        }
    }
}
