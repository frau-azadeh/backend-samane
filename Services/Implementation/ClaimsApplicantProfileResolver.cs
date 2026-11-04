using Microsoft.AspNetCore.Http;
using samane.Security;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    // Replace this adapter with joins to your host's employee/job tables when their full schema is available.
    public class ClaimsApplicantProfileResolver : IApplicantProfileResolver
    {
        private readonly IHttpContextAccessor context;
        private readonly ICurrentActor actor;
        public ClaimsApplicantProfileResolver(IHttpContextAccessor context, ICurrentActor actor)
        {
            this.context = context;
            this.actor = actor;
        }
        public ApplicantProfile Resolve()
        {
            var user = context.HttpContext.User;
            return new ApplicantProfile { FullName = actor.Name,
                                          JobTitle = user.FindFirst("job_title")?.Value,
                                          DepartmentName = user.FindFirst("department_name")?.Value };
        }
    }
}
