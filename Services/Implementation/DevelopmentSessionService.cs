using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using samane.Data;
using samane.Models;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    public class DevelopmentSessionService : IDevelopmentSessionService
    {
        private readonly ISqlConnectionFactory factory;
        private readonly IMemoryCache cache;
        private readonly IConfiguration config;
        public DevelopmentSessionService(ISqlConnectionFactory factory, IMemoryCache cache, IConfiguration config)
        { this.factory = factory; this.cache = cache; this.config = config; }

        public async Task CheckDatabaseAsync(CancellationToken ct)
        {
            using (var c = factory.Create()) {
                await c.OpenAsync(ct);
                using (var cmd = c.CreateCommand()) {
                    cmd.CommandText = @"SELECT TOP(0) Id,UserName,FirstName,LastName,PasswordHash,IsActive,Internal_Phone FROM dbo.AspNetUsers;
SELECT TOP(0) PE_UserID,PE_PartyID FROM dbo.TM_Party_Employee;
SELECT TOP(0) DOE_PartyID,DOE_JobID FROM dbo.TM_DepartmentOfEmployee;
SELECT TOP(0) Job_ID,Job_Title,Job_Full_Name FROM dbo.TM_AllJobs;
SELECT TOP(0) PRR_Id FROM dbo.TM_PersonnelReviewRequest;
SELECT TOP(0) UserId,GridId,SettingsJson FROM dbo.TM_UserGridPreference;";
                    using (var reader = await cmd.ExecuteReaderAsync(ct)) { }
                }
            }
        }
        private async Task<ClaimsPrincipal> PrincipalAsync(SqlConnection c, string id, string name, CancellationToken ct)
        {
            var claims = new List<Claim> {new Claim(ClaimTypes.NameIdentifier,id),new Claim(ClaimTypes.Name,name)};
            // Some host databases use UserRole rather than Identity role tables. Do not invent
            // a meaning for that field or crash applicant login if those tables are absent.
            bool hasRoles;
            using (var cmd = c.CreateCommand()) {
                cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetUserRoles',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.AspNetRoles',N'U') IS NOT NULL THEN 1 ELSE 0 END";
                hasRoles = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) == 1;
            }
            if (hasRoles) using (var cmd = c.CreateCommand()) {
                cmd.CommandText = "SELECT r.Name FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE ur.UserId=@id";
                cmd.Parameters.Add("@id",SqlDbType.NVarChar,450).Value=id;
                using (var r = await cmd.ExecuteReaderAsync(ct)) while(await r.ReadAsync(ct)) if(!r.IsDBNull(0)) claims.Add(new Claim(ClaimTypes.Role,r.GetString(0)));
            }
            foreach (var role in new[]{"PersonnelReview.Owner","PersonnelReview.Evaluator","PersonnelReview.Management","PersonnelDirectory.Manager"})
                foreach (var entry in config.GetSection("Authorization:RoleUsers:"+role).GetChildren())
                    if (string.Equals(entry.Value,id,StringComparison.Ordinal)) { claims.Add(new Claim(ClaimTypes.Role,role)); break; }
            return new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme));
        }
        // Called exclusively by the controller action guarded by IsDevelopment and Enabled.
        public async Task<ClaimsPrincipal> AuthenticateUserIdAsync(string userId, CancellationToken ct)
        {
            userId=(userId??"").Trim();
            if(userId.Length==0 || userId.Length>450) throw new ApiException(400,"شناسه کاربر را وارد کنید");
            using (var c=factory.Create()) {
                await c.OpenAsync(ct);
                string name;
                using(var cmd=c.CreateCommand()) {
                    cmd.CommandText=@"SELECT COALESCE(NULLIF(LTRIM(RTRIM(COALESCE(FirstName,N'')+N' '+COALESCE(LastName,N''))),N''),UserName,Id)
FROM dbo.AspNetUsers WHERE Id=@id AND (IsActive IS NULL OR IsActive=1)";
                    cmd.Parameters.Add("@id",SqlDbType.NVarChar,450).Value=userId;
                    var result=await cmd.ExecuteScalarAsync(ct);
                    if(result==null || result==DBNull.Value) throw new ApiException(401,"شناسه Id پیدا نشد یا کاربر غیرفعال است؛ در این روش باید مقدار AspNetUsers.Id را وارد کنید، نه UserName");
                    name=(string)result;
                }
                return await PrincipalAsync(c,userId,name,ct);
            }
        }
        public async Task<ClaimsPrincipal> AuthenticateAsync(string login,string password,CancellationToken ct)
        {
            login=(login??"").Trim();
            if(login.Length==0 || login.Length>256 || string.IsNullOrEmpty(password) || password.Length>1024)
                throw new ApiException(401,"نام کاربری یا رمز عبور معتبر نیست");
            var key="prr-login:"+login.ToUpperInvariant();
            int attempts; cache.TryGetValue(key,out attempts);
            if(attempts>=5) throw new ApiException(429,"تعداد تلاش ورود زیاد است؛ ده دقیقه دیگر تلاش کنید");
            cache.Set(key,attempts+1,TimeSpan.FromMinutes(10));
            using(var c=factory.Create()) {
                await c.OpenAsync(ct);
                DevelopmentAccount account=null;
                bool twoFactor=false;
                using(var cmd=c.CreateCommand()) {
                    cmd.CommandText=@"SELECT TOP(2) Id,UserName,PasswordHash,LockoutEnd,LockoutEnabled,TwoFactorEnabled,
COALESCE(NULLIF(LTRIM(RTRIM(COALESCE(FirstName,N'')+N' '+COALESCE(LastName,N''))),N''),UserName,Id) AS FullName
FROM dbo.AspNetUsers WHERE (NormalizedUserName=@normalized OR NormalizedEmail=@normalized OR UserName=@login OR Email=@login)
AND (IsActive IS NULL OR IsActive=1)";
                    cmd.Parameters.Add("@normalized",SqlDbType.NVarChar,256).Value=login.ToUpperInvariant();
                    cmd.Parameters.Add("@login",SqlDbType.NVarChar,256).Value=login;
                    using(var r=await cmd.ExecuteReaderAsync(ct)) {
                        if(await r.ReadAsync(ct)) {
                            account=new DevelopmentAccount { Id=r.GetString(0),UserName=r.GetString(6),PasswordHash=r.IsDBNull(2)?null:r.GetString(2),
                                LockoutEnd=!r.GetBoolean(4)||r.IsDBNull(3)?(DateTimeOffset?)null:r.GetFieldValue<DateTimeOffset>(3) };
                            twoFactor=r.GetBoolean(5);
                        }
                        if(await r.ReadAsync(ct)) account=null;
                    }
                }
                if(account==null || string.IsNullOrEmpty(account.PasswordHash) || account.LockoutEnd>DateTimeOffset.UtcNow)
                    throw new ApiException(401,"نام کاربری یا رمز عبور معتبر نیست");
                PasswordVerificationResult verified;
                try { verified=new PasswordHasher<DevelopmentAccount>().VerifyHashedPassword(account,account.PasswordHash,password); }
                catch(FormatException) { verified=PasswordVerificationResult.Failed; }
                if(verified==PasswordVerificationResult.Failed) throw new ApiException(401,"نام کاربری یا رمز عبور معتبر نیست");
                if(twoFactor) throw new ApiException(403,"ورود دومرحله‌ای حساب فعال است؛ از مسیر ورود سامانه اصلی استفاده کنید");
                cache.Remove(key);
                return await PrincipalAsync(c,account.Id,account.UserName,ct);
            }
        }
    }
}
