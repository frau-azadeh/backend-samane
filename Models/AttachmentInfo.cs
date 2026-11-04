using System;
namespace samane.Models
{
    public class AttachmentInfo
    {
        public string OriginalName { get; set; }
        public string StorageKey { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
