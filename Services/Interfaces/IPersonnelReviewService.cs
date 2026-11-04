using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using samane.Models;
namespace samane.Services.Interfaces
{
    public interface IPersonnelReviewService
    {
        Task<PageResult<ReviewRow>> ListAsync(ReviewQuery query, CancellationToken ct);
        Task<IDictionary<string, object>> GetAsync(long id, CancellationToken ct);
        Task<string> CreateAsync(ReviewInput input, CancellationToken ct);
        Task UpdateAsync(long id, ReviewInput input, CancellationToken ct);
        Task DeleteAsync(long id, string version, CancellationToken ct);
        Task AddAttachmentAsync(long id, string version, AttachmentInfo attachment, CancellationToken ct);
        Task ActAsync(long id, ActionInput input, CancellationToken ct);
    }
}
