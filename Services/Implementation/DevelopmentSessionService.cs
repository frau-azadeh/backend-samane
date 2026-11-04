using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using samane.Data;
using samane.Models;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    // Used only by the development-only controller. No test users or roles are created.
    public class DevelopmentSessionService : IDevelopmentSessionService
    {
        private readonly ISqlConnectionFactory factory;
        private readonly IMemoryCache cache;
        public DevelopmentSessionService(ISqlConnectionFactory factory, IMemoryCache cache)
        {
            this.factory = factory;
            this.cache = cache;
        }
        public async Task CheckDatabaseAsync(CancellationToken ct)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT TOP (0) PRR_Id FROM dbo.TM_PersonnelReviewRequest; SELECT TOP (0) UserId,GridId,SettingsJson FROM dbo.TM_UserGridPreference;";
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                    }
                }
            }
        }
        public async Task<ClaimsPrincipal> AuthenticateAsync(string login, string password,
                                                             CancellationToken ct)
        {
            login = (login ?? "").Trim();
            var key = "prr-login:" + login.ToUpperInvariant();
            int attempts;
            cache.TryGetValue(key, out attempts);
            if (attempts >= 5)
                throw new ApiException(429, "تعداد تلاش ورود زیاد است؛ ده دقیقه دیگر تلاش کنید");
            cache.Set(key, attempts + 1, TimeSpan.FromMinutes(10));
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                DevelopmentAccount account = null;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT TOP (2) Id,UserName,PasswordHash,LockoutEnd FROM dbo.AspNetUsers WHERE UserName=@login OR Email=@login";
                    cmd.Parameters.Add("@login", SqlDbType.NVarChar, 256).Value = login;
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        if (await reader.ReadAsync(ct))
                            account = new DevelopmentAccount {
                                Id = reader.GetString(0),
                                UserName = reader.IsDBNull(1) ? login : reader.GetString(1),
                                PasswordHash = reader.IsDBNull(2) ? null : reader.GetString(2),
                                LockoutEnd = reader.IsDBNull(3) ? (DateTimeOffset?)null
                                                                : reader.GetFieldValue<DateTimeOffset>(3)
                            };
                        if (await reader.ReadAsync(ct))
                            account = null;
                    }
                }
                if (account == null || string.IsNullOrEmpty(account.PasswordHash) ||
                    account.LockoutEnd > DateTimeOffset.UtcNow)
                    throw new ApiException(401, "نام کاربری یا رمز عبور معتبر نیست");
                PasswordVerificationResult verified;
                try
                {
                    verified = new PasswordHasher<DevelopmentAccount>().VerifyHashedPassword(
                        account, account.PasswordHash, password);
                }
                catch (FormatException)
                {
                    verified = PasswordVerificationResult.Failed;
                }
                if (verified == PasswordVerificationResult.Failed)
                    throw new ApiException(401, "نام کاربری یا رمز عبور معتبر نیست");
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, account.Id),
                                               new Claim(ClaimTypes.Name, account.UserName) };
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT r.Name FROM dbo.AspNetUserRoles ur INNER JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE ur.UserId=@id";
                    cmd.Parameters.Add("@id", SqlDbType.NVarChar, 450).Value = account.Id;
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                            if (!reader.IsDBNull(0))
                                claims.Add(new Claim(ClaimTypes.Role, reader.GetString(0)));
                    }
                }
                cache.Remove(key);
                return new ClaimsPrincipal(
                    new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            }
        }
    }
}
