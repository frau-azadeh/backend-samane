using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
namespace samane.Services.Interfaces
{
    public interface IDevelopmentSessionService
    {
        Task<ClaimsPrincipal> AuthenticateAsync(string login, string password, CancellationToken ct);
        Task CheckDatabaseAsync(CancellationToken ct);
    }
}
