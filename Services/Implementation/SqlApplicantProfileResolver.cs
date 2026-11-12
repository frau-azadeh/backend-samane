using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using samane.Data;
using samane.Models;
using samane.Security;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    public class SqlApplicantProfileResolver : IApplicantProfileResolver
    {
        private readonly ISqlConnectionFactory factory;
        private readonly ICurrentActor actor;
        public SqlApplicantProfileResolver(ISqlConnectionFactory factory, ICurrentActor actor)
        { this.factory = factory; this.actor = actor; }

        public async Task<ApplicantProfile> ResolveAsync(CancellationToken ct)
        {
            // The provided schema has no active/current-assignment column. Never silently pick
            // TOP 1 from several different assignments. Duplicate identical rows are harmless.
            const string sql = @"
SELECT DISTINCT
    COALESCE(NULLIF(LTRIM(RTRIM(COALESCE(u.FirstName,N'') + N' ' + COALESCE(u.LastName,N''))),N''),u.UserName,u.Id) AS FullName,
    j.Job_Title AS JobTitle,
    j.Job_Full_Name AS DepartmentName
FROM dbo.AspNetUsers u
LEFT JOIN dbo.TM_Party_Employee pe ON pe.PE_UserID = u.Id
LEFT JOIN dbo.TM_DepartmentOfEmployee doe ON doe.DOE_PartyID = pe.PE_PartyID
LEFT JOIN dbo.TM_AllJobs j ON j.Job_ID = doe.DOE_JobID
WHERE u.Id = @userId;";
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.Add("@userId",SqlDbType.NVarChar,450).Value = actor.Id;
                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        if (!await r.ReadAsync(ct)) throw new ApiException(401,"کاربر نشست در دیتابیس پیدا نشد؛ دوباره وارد شوید");
                        var result = new ApplicantProfile {
                            FullName = r.GetString(0),
                            JobTitle = r.IsDBNull(1) ? null : r.GetString(1),
                            DepartmentName = r.IsDBNull(2) ? null : r.GetString(2)
                        };
                        if (await r.ReadAsync(ct)) throw new ApiException(409,"برای این کاربر چند انتساب سازمانی متفاوت وجود دارد؛ انتساب جاری را در اطلاعات منابع انسانی مشخص کنید");
                        if (result.FullName.Length > 300 || (result.JobTitle?.Length ?? 0) > 300 || (result.DepartmentName?.Length ?? 0) > 300)
                            throw new ApiException(400,"نام، سمت یا مسیر سازمانی بیش از ظرفیت ۳۰۰ نویسه رکورد ارزیابی است؛ داده مرجع یا ظرفیت ستون‌ها را اصلاح کنید");
                        return result;
                    }
                }
            }
        }
    }
}
