using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using samane.Models;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    public class AttachmentService : IAttachmentService
    {
        private readonly IPersonnelReviewService reviews;
        private readonly string directory;
        public AttachmentService(IPersonnelReviewService reviews, IHostingEnvironment env)
        {
            this.reviews = reviews;
            directory = Path.Combine(env.ContentRootPath, "App_Data", "PersonnelReviewAttachments");
        }
        public async Task UploadAsync(long requestId, string version, IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024)
                throw new ApiException(400, "اندازه فایل باید بین ۱ بایت و ۱۰ مگابایت باشد");
            var detail = await reviews.GetAsync(requestId, ct);
            if (!((string[])detail["allowedActions"]).Contains("upload"))
                throw new ApiException(403, "بارگذاری فقط در ثبت اولیه مجاز است");
            var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
            if (name.Length > 200)
                throw new ApiException(400, "نام فایل طولانی است");
            var ext = Path.GetExtension(name).ToLowerInvariant();
            string contentType;
            byte[] signature;
            if (ext == ".pdf")
            {
                contentType = "application/pdf";
                signature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d };
            }
            else if (ext == ".png")
            {
                contentType = "image/png";
                signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            }
            else if (ext == ".jpg" || ext == ".jpeg")
            {
                contentType = "image/jpeg";
                signature = new byte[] { 255, 216, 255 };
            }
            else
                throw new ApiException(400, "فقط PDF، PNG و JPEG مجاز است");
            var key = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, key);
            try
            {
                using (var input = file.OpenReadStream()) using (
                    var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                                81920, true))
                {
                    var buffer = new byte[81920];
                    long length = 0;
                    int read;
                    int checkedBytes = 0;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        length += read;
                        if (length > 10 * 1024 * 1024)
                            throw new ApiException(400, "فایل بیش از حد بزرگ است");
                        for (int i = 0; i < read && checkedBytes < signature.Length; i++, checkedBytes++)
                            if (buffer[i] != signature[checkedBytes])
                                throw new ApiException(400, "محتوای فایل با پسوند تطابق ندارد");
                        await output.WriteAsync(buffer, 0, read, ct);
                    }
                    if (checkedBytes < signature.Length || length != file.Length)
                        throw new ApiException(400, "فایل ناقص است");
                }
                await reviews.AddAttachmentAsync(requestId, version,
                                                 new AttachmentInfo { StorageKey = key, OriginalName = name,
                                                                      ContentType = contentType,
                                                                      SizeBytes = file.Length },
                                                 ct);
            }
            catch
            {
                if (File.Exists(path))
                    File.Delete(path);
                throw;
            }
        }
        public async Task<AttachmentDownload> DownloadAsync(long requestId, string key, CancellationToken ct)
        {
            Guid parsed;
            if (!Guid.TryParseExact(key, "N", out parsed))
                throw new ApiException(404, "فایل یافت نشد");
            var detail = await reviews.GetAsync(requestId, ct);
            var files = JsonConvert.DeserializeObject<AttachmentInfo[]>((string)detail["attachmentsJson"]);
            var info = files.SingleOrDefault(f => f.StorageKey == key);
            var path = Path.Combine(directory, key);
            if (info == null || !File.Exists(path))
                throw new ApiException(404, "فایل یافت نشد");
            return new AttachmentDownload { Path = path, Info = info };
        }
    }
}
