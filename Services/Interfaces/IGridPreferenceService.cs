using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace samane.Services.Interfaces
{
    public interface IGridPreferenceService
    {
        Task<JObject> GetAsync(string gridId, CancellationToken ct);
        Task SaveAsync(string gridId, JObject settings, string expectedUserId, CancellationToken ct);
    }
}
