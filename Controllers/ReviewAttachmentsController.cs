using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using samane.Services.Interfaces;
namespace samane.Controllers
{
    [Authorize, Route("api/personnel-reviews/{id:long}/attachments")]
    public class ReviewAttachmentsController : ControllerBase
    {
        private readonly IAttachmentService service;
        public ReviewAttachmentsController(IAttachmentService service)
        {
            this.service = service;
        }
        [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
        public async Task<IActionResult> Upload(long id, [FromForm] string version, [FromForm] IFormFile file,
                                                CancellationToken ct)
        {
            await service.UploadAsync(id, version, file, ct);
            return NoContent();
        }
        [HttpGet("{key}")]
        public async Task<IActionResult> Download(long id, string key, CancellationToken ct)
        {
            var result = await service.DownloadAsync(id, key, ct);
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(result.Path, result.Info.ContentType, result.Info.OriginalName);
        }
    }
}
