using System.Threading;
using System.Threading.Tasks;
using samane.Models;

namespace samane.Services.Interfaces
{
    public interface IPersonnelDirectoryService
    {
        Task<PageResult<DirectoryRow>> ListAsync(DirectoryQuery query, CancellationToken ct);
        Task<DirectoryDetails> GetAsync(string id, CancellationToken ct);
        Task<string> CreateAsync(DirectoryInput input, CancellationToken ct);
        Task UpdateAsync(string id, DirectoryInput input, CancellationToken ct);
        Task DeleteAsync(string id, string version, CancellationToken ct);
    }
}
