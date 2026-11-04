using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using samane.Data;
using samane.Models;
using samane.Security;
using samane.Services.Interfaces;
namespace samane.Services.Implementation
{
    public class GridPreferenceService : IGridPreferenceService
    {
        private readonly ISqlConnectionFactory factory;
        private readonly ICurrentActor actor;
        public GridPreferenceService(ISqlConnectionFactory factory, ICurrentActor actor)
        {
            this.factory = factory;
            this.actor = actor;
        }
        private static void Check(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 100 || !Regex.IsMatch(id, @"\A[a-zA-Z0-9._-]+\z"))
                throw new ApiException(400, "شناسه جدول نامعتبر است");
        }
        public async Task<JObject> GetAsync(string gridId, CancellationToken ct)
        {
            Check(gridId);
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT SettingsJson FROM dbo.TM_UserGridPreference WHERE UserId=@user AND GridId=@grid";
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar, 450).Value = actor.Id;
                    cmd.Parameters.Add("@grid", SqlDbType.VarChar, 100).Value = gridId;
                    var json = await cmd.ExecuteScalarAsync(ct) as string;
                    return json == null ? new JObject() : JObject.Parse(json);
                }
            }
        }
        public async Task SaveAsync(string gridId, JObject settings, string expectedUserId,
                                    CancellationToken ct)
        {
            Check(gridId);
            if (actor.Id != expectedUserId)
                throw new ApiException(
                    409,
                    "حساب کاربری تغییر کرده است؛ صفحه را تازه‌سازی " +
                        "کنید");
            var json = settings.ToString(Formatting.None);
            if (json.Length > 10000)
                throw new ApiException(400, "تنظیمات بیش از حد بزرگ است");
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var tx = c.BeginTransaction())
                {
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                            "UPDATE dbo.TM_UserGridPreference WITH(UPDLOCK,SERIALIZABLE) SET " +
                            "SettingsJson=@json,ModifiedAt=SYSUTCDATETIME() WHERE UserId=@user AND " +
                            "GridId=@grid; IF @@ROWCOUNT=0 INSERT " +
                            "dbo.TM_UserGridPreference(UserId,GridId,SettingsJson) VALUES(@user,@grid,@json);";
                        cmd.Parameters.Add("@user", SqlDbType.NVarChar, 450).Value = actor.Id;
                        cmd.Parameters.Add("@grid", SqlDbType.VarChar, 100).Value = gridId;
                        cmd.Parameters.Add("@json", SqlDbType.NVarChar, -1).Value = json;
                        await cmd.ExecuteNonQueryAsync(ct);
                        tx.Commit();
                    }
                }
            }
        }
    }
}
