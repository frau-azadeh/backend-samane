using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using samane.Models;
namespace samane.Security
{
    public interface ICurrentActor
    {
        string Id { get; }
        string Name { get; }
        bool Owner { get; }
        bool Evaluator { get; }
        bool Management { get; }
    }
    public class CurrentActor : ICurrentActor
    {
        private readonly ClaimsPrincipal user;
        public CurrentActor(IHttpContextAccessor accessor)
        {
            user = accessor.HttpContext.User;
        }
        public string Id => user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            throw new ApiException(401, "ورود به سامانه الزامی است");
        public string Name => user.Identity.Name ?? Id;
        // Map these roles to the host's real authorization system. Never trust headers or frontend role
        // flags.
        public bool Owner => user.IsInRole("PersonnelReview.Owner");
        public bool Evaluator => user.IsInRole("PersonnelReview.Evaluator");
        public bool Management => user.IsInRole("PersonnelReview.Management");
    }
}
