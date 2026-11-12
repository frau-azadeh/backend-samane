using System.Threading;
using System.Threading.Tasks;
namespace samane.Services.Interfaces
{
    public class ApplicantProfile
    {
        public string FullName { get; set; }
        public string JobTitle { get; set; }
        public string DepartmentName { get; set; }
    }
    public interface IApplicantProfileResolver
    {
        Task<ApplicantProfile> ResolveAsync(CancellationToken ct);
    }
}
