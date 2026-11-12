using System.Threading;
using System.Threading.Tasks;
using samane.Data;
using samane.Security;
using samane.Services.Interfaces;

namespace samane.Services.Implementation
{
    // Retain the old filename/type so copying this release over the previous project
    // overwrites the obsolete synchronous implementation rather than leaving it behind.
    // Both registrations now use the same database-backed profile lookup.
    public class ClaimsApplicantProfileResolver : IApplicantProfileResolver
    {
        private readonly SqlApplicantProfileResolver resolver;

        public ClaimsApplicantProfileResolver(ISqlConnectionFactory factory, ICurrentActor actor)
        {
            resolver = new SqlApplicantProfileResolver(factory, actor);
        }

        public Task<ApplicantProfile> ResolveAsync(CancellationToken ct)
        {
            return resolver.ResolveAsync(ct);
        }
    }
}
