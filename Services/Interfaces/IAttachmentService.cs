using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using samane.Models;
namespace samane.Services.Interfaces
{
    public class AttachmentDownload
    {
        public string Path { get; set; }
        public AttachmentInfo Info { get; set; }
    }
    public interface IAttachmentService
    {
        Task UploadAsync(long requestId, string version, IFormFile file, CancellationToken ct);
        Task<AttachmentDownload> DownloadAsync(long requestId, string key, CancellationToken ct);
    }
}
