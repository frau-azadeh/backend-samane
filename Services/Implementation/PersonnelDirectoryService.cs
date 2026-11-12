using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using samane.Data;
using samane.Models;
using samane.Security;
using samane.Services.Interfaces;

namespace samane.Services.Implementation
{
    public class PersonnelDirectoryService : IPersonnelDirectoryService
    {
        private readonly ISqlConnectionFactory factory;
        private readonly ICurrentActor actor;
        public PersonnelDirectoryService(ISqlConnectionFactory factory, ICurrentActor actor)
        {
            this.factory = factory;
            this.actor = actor;
        }

        // Fixed expressions only: client-supplied identifiers never enter SQL.
        private static readonly Dictionary<string, string> Fields = new Dictionary<string, string>
        {
            {
                "rowNumber",
                "u.Id"
            },
            {
                "fullName",
                "LTRIM(RTRIM(COALESCE(u.FirstName,N'')+N' '+COALESCE(u.LastName,N'')))"
            },
            {
                "firstName",
                "u.FirstName"
            },
            {
                "lastName",
                "u.LastName"
            },
            {
                "internalPhone",
                "u.Internal_Phone"
            }
        };
        private const string Select = "SELECT u.Id,u.FirstName,u.LastName,u.Internal_Phone,u.ConcurrencyStamp,d.PDS_Revision,CAST(COALESCE(d.PDS_Is_deleted,0) AS bit),CAST(0 AS bigint) AS RowNumber ";
        // Window numbering is computed by SQL once for the filtered query plan, not a
        // correlated COUNT for every returned or sorted row. Explicit projection avoids blobs.
        private const string Numbered = ";WITH NumberedUsers AS (SELECT Id,FirstName,LastName,Internal_Phone,ConcurrencyStamp,ROW_NUMBER() OVER(ORDER BY Id ASC) AS RowNumber FROM dbo.AspNetUsers) ";
        private const string ListFrom = " FROM NumberedUsers u LEFT JOIN dbo.TM_PersonnelDirectoryState d ON d.PDS_UserId_Fk=u.Id ";
        private const string From = " FROM dbo.AspNetUsers u LEFT JOIN dbo.TM_PersonnelDirectoryState d ON d.PDS_UserId_Fk=u.Id ";
        private static void Param(SqlCommand cmd, string name, object value, int length = -1)
        {
            cmd.Parameters.Add(name, SqlDbType.NVarChar, length).Value = value ?? DBNull.Value;
        }

        private string[] Actions => new[]
        {
            "view",
            "edit",
            "delete"
        };

        private DirectoryRow Read(SqlDataReader r)
        {
            var row = new DirectoryRow
            {
                Id = r.GetString(0),
                RowNumber = r.GetInt64(7),
                FirstName = r.IsDBNull(1) ? null : r.GetString(1),
                LastName = r.IsDBNull(2) ? null : r.GetString(2),
                InternalPhone = r.IsDBNull(3) ? null : r.GetString(3),
                IsDeleted = r.GetBoolean(6),
                AllowedActions = r.GetBoolean(6) ? new[]
                {
                    "view"
                }

                : Actions
            };
            var data = JsonConvert.SerializeObject(new object[] { row.Id, row.FirstName, row.LastName, row.InternalPhone, r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : (object)r.GetGuid(5) });
            using (var sha = SHA256.Create())
                row.Version = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(data)));
            return row;
        }

        private static string Digits(string value) => string.Concat((value ?? "").Select(c => c >= '۰' && c <= '۹' ? (char)('0' + c - '۰') : c >= '٠' && c <= '٩' ? (char)('0' + c - '٠') : c));
        private static string Pattern(string value) => "%" + value.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
        private static void Validate(DirectoryInput input)
        {
            if (input == null)
                throw new ApiException(400, "اطلاعات الزامی است");
            input.FirstName = input.FirstName?.Trim();
            input.LastName = input.LastName?.Trim();
            input.InternalPhone = Digits(input.InternalPhone).Trim();
            if (string.IsNullOrWhiteSpace(input.FirstName) || input.FirstName.Length > 100 || string.IsNullOrWhiteSpace(input.LastName) || input.LastName.Length > 100 || !Regex.IsMatch(input.InternalPhone, @"\A[0-9]{1,20}\z"))
                throw new ApiException(400, "نام و نام خانوادگی و شماره داخلی عددی (حداکثر ۲۰ رقم) الزامی است");
        }

        public async Task<PageResult<DirectoryRow>> ListAsync(DirectoryQuery q, CancellationToken ct)
        {
            if (!Fields.ContainsKey(q.Sort ?? "") || (q.TextMatch != "prefix" && q.TextMatch != "contains"))
                throw new ApiException(400, "مرتب‌سازی یا روش جستجو نامعتبر است");
            if (q.Page < 1 || q.Page > 100000 || q.PageSize < 1 || q.PageSize > 100 || (q.FiltersJson?.Length ?? 0) > 6000)
                throw new ApiException(400,"صفحه‌بندی یا حجم فیلتر نامعتبر است");
            Dictionary<string, string> filters;
            try
            {
                filters = string.IsNullOrWhiteSpace(q.FiltersJson) ? new Dictionary<string, string>() : JsonConvert.DeserializeObject<Dictionary<string, string>>(q.FiltersJson);
            }
            catch (JsonException)
            {
                throw new ApiException(400, "فیلتر نامعتبر است");
            }

            if (filters == null || filters.Count > 30 || filters.Any(f => f.Key == "rowNumber" || (!Fields.ContainsKey(f.Key) && f.Key != "isDeleted" && !f.Key.StartsWith("$")) || (f.Value?.Length ?? 0) > (f.Key.StartsWith("$") ? 1000 : 100)))
                throw new ApiException(400, "فیلتر نامعتبر است");
            filters = filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToDictionary(f => f.Key, f => f.Value.Trim());
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd = c.CreateCommand())
                {
                    if (filters.ContainsKey("isDeleted") && filters["isDeleted"] != "0" && filters["isDeleted"] != "1")
                        throw new ApiException(400, "وضعیت دفترچه نامعتبر است");
                    var where = " WHERE COALESCE(d.PDS_Is_deleted,0)=@deleted";
                    cmd.Parameters.Add("@deleted", SqlDbType.Bit).Value = filters.ContainsKey("isDeleted") && filters["isDeleted"] == "1";
                    var display = new Dictionary<string,string>(Fields);
                    display["rowNumber"] = "u.RowNumber";
                    var extra = new List<SqlParameter>();
                    int i = 0;
                    foreach (var f in filters)
                    {
                        if (f.Key == "isDeleted")
                            continue;
                        if (samane.Data.DisplaySearch.Add(f.Key,f.Value,display,new HashSet<string>{"rowNumber"},extra,ref where)) continue;
                        var key = "@f" + i++;
                        where += " AND " + Fields[f.Key] + " LIKE " + key + " ESCAPE N'~'";
                        var value = f.Key == "internalPhone" ? Digits(f.Value) : f.Value;
                        var escaped = value.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[");
                        Param(cmd, key, (q.TextMatch == "contains" ? "%" : "") + escaped + "%");
                    }

                    cmd.Parameters.AddRange(extra.ToArray());
                    if (!string.IsNullOrWhiteSpace(q.Search))
                    {
                        where += " AND (" + Fields["fullName"] + " LIKE @search ESCAPE N'~' OR u.Internal_Phone LIKE @search ESCAPE N'~')";
                        Param(cmd, "@search", Pattern(Digits(q.Search.Trim())));
                    }

                    cmd.CommandText = Numbered + "SELECT COUNT_BIG(*)" + ListFrom + where + ";" +
                        Numbered + Select.Replace("CAST(0 AS bigint)", "u.RowNumber") + ListFrom + where +
                        " ORDER BY " + Fields[q.Sort] + (q.Desc ? " DESC" : " ASC") +
                        (q.Sort == "rowNumber" ? "" : ",u.Id ASC") + " OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY OPTION(RECOMPILE);";
                    cmd.Parameters.Add("@offset", SqlDbType.Int).Value = (q.Page - 1) * q.PageSize;
                    cmd.Parameters.Add("@take", SqlDbType.Int).Value = q.PageSize;
                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        await r.ReadAsync(ct);
                        var total = r.GetInt64(0);
                        await r.NextResultAsync(ct);
                        var items = new List<DirectoryRow>();
                        while (await r.ReadAsync(ct))
                            items.Add(Read(r));
                        return new PageResult<DirectoryRow>
                        {
                            Items = items,
                            TotalCount = total,
                            Page = q.Page,
                            PageSize = q.PageSize,
                            AppliedFilters = filters
                        };
                    }
                }
            }
        }

        private async Task<DirectoryRow> Load(SqlConnection c, SqlTransaction tx, string id, CancellationToken ct, bool includeDeleted = false)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = Select + (tx == null ? From : " FROM dbo.AspNetUsers u WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.TM_PersonnelDirectoryState d WITH(UPDLOCK,HOLDLOCK) ON d.PDS_UserId_Fk=u.Id ") + " WHERE u.Id=@id" + (includeDeleted ? "" : " AND COALESCE(d.PDS_Is_deleted,0)=0");
                Param(cmd, "@id", id, 450);
                using (var r = await cmd.ExecuteReaderAsync(ct))
                {
                    if (!await r.ReadAsync(ct))
                        throw new ApiException(404, "نیرو پیدا نشد یا از فهرست حذف شده است");
                    return Read(r);
                }
            }
        }

        public async Task<DirectoryDetails> GetAsync(string id, CancellationToken ct)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                var row = await Load(c, null, id, ct, true);
                var history = new List<DirectoryHistory>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT PDH_Action,PDH_CreatedByUserId_Fk,PDH_CreatedByName,PDH_CreatedAt,PDH_BeforeJson,PDH_AfterJson FROM dbo.TM_PersonnelDirectoryHistory WHERE PDH_UserId_Fk=@id AND PDH_Is_deleted=0 ORDER BY PDH_Id DESC";
                    Param(cmd, "@id", id, 450);
                    using (var r = await cmd.ExecuteReaderAsync(ct))
                        while (await r.ReadAsync(ct))
                            history.Add(new DirectoryHistory { Action = r.GetString(0), ActorId = r.GetString(1), ActorName = r.GetString(2), ChangedAt = DateTime.SpecifyKind(r.GetDateTime(3), DateTimeKind.Utc), Before = r.IsDBNull(4) ? null : JsonConvert.DeserializeObject<DirectoryRow>(r.GetString(4)), After = r.IsDBNull(5) ? null : JsonConvert.DeserializeObject<DirectoryRow>(r.GetString(5)) });
                }

                return new DirectoryDetails
                {
                    User = row,
                    History = history
                };
            }
        }

        private async Task Audit(SqlConnection c, SqlTransaction tx, string id, string action, DirectoryRow before, DirectoryRow after, CancellationToken ct)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT dbo.TM_PersonnelDirectoryHistory(PDH_UserId_Fk,PDH_Action,PDH_CreatedByUserId_Fk,PDH_CreatedByName,PDH_BeforeJson,PDH_AfterJson) VALUES(@id,@action,@actor,@name,@before,@after)";
                Param(cmd, "@id", id, 450);
                Param(cmd, "@action", action, 10);
                Param(cmd, "@actor", actor.Id, 450);
                Param(cmd, "@name", actor.Name, 512);
                Param(cmd, "@before", before == null ? null : JsonConvert.SerializeObject(before));
                Param(cmd, "@after", after == null ? null : JsonConvert.SerializeObject(after));
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        public async Task<string> CreateAsync(DirectoryInput input, CancellationToken ct)
        {
            Validate(input);
            var id = Guid.NewGuid().ToString();
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var tx = c.BeginTransaction())
                {
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        // Directory personnel have no login credentials. Host account provisioning is a separate operation.
                        cmd.CommandText = "INSERT dbo.AspNetUsers(Id,FirstName,LastName,Internal_Phone,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,IsActive,LockoutEnd,RegisterDate,SecurityStamp,ConcurrencyStamp,UserRole,Price_buy_manager) VALUES(@id,@first,@last,@phone,0,0,0,1,0,0,'9999-12-31T00:00:00+00:00',SYSUTCDATETIME(),@stamp,@stamp,NULL,NULL); INSERT dbo.TM_PersonnelDirectoryState(PDS_UserId_Fk,PDS_CreatedByUserId_Fk) VALUES(@id,@actor);";
                        WriteParams(cmd, id, input);
                        Param(cmd, "@actor", actor.Id, 450);
                        Param(cmd, "@stamp", Guid.NewGuid().ToString());
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    var row = await Load(c, tx, id, ct);
                    await Audit(c, tx, id, "create", null, row, ct);
                    tx.Commit();
                    return id;
                }
            }
        }

        private static void WriteParams(SqlCommand cmd, string id, DirectoryInput input)
        {
            Param(cmd, "@id", id, 450);
            Param(cmd, "@first", input.FirstName);
            Param(cmd, "@last", input.LastName);
            Param(cmd, "@phone", input.InternalPhone);
        }

        public Task UpdateAsync(string id, DirectoryInput input, CancellationToken ct)
        {
            Validate(input);
            return Change(id, input, input.Version, ct);
        }

        public Task DeleteAsync(string id, string version, CancellationToken ct) => Change(id, null, version, ct);
        private async Task Change(string id, DirectoryInput input, string version, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ApiException(400, "نسخه رکورد الزامی است");
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var tx = c.BeginTransaction())
                {
                    var before = await Load(c, tx, id, ct);
                    if (before.Version != version)
                        throw new ApiException(409, "این رکورد تغییر کرده است؛ پنجره را ببندید و اطلاعات را دوباره دریافت کنید");
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "UPDATE dbo.TM_PersonnelDirectoryState SET PDS_Revision=NEWID(),PDS_Is_deleted=@deleted WHERE PDS_UserId_Fk=@id; IF @@ROWCOUNT=0 INSERT dbo.TM_PersonnelDirectoryState(PDS_UserId_Fk,PDS_Is_deleted,PDS_CreatedByUserId_Fk) VALUES(@id,@deleted,@actor);";
                        Param(cmd, "@actor", actor.Id, 450);
                        cmd.Parameters.Add("@deleted", SqlDbType.Bit).Value = input == null;
                        if (input == null)
                            Param(cmd, "@id", id, 450);
                        else
                        {
                            WriteParams(cmd, id, input);
                            cmd.CommandText += "UPDATE dbo.AspNetUsers SET FirstName=@first,LastName=@last,Internal_Phone=@phone,ConcurrencyStamp=@stamp WHERE Id=@id;";
                            Param(cmd, "@stamp", Guid.NewGuid().ToString());
                        }

                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    var after = input == null ? null : await Load(c, tx, id, ct);
                    await Audit(c, tx, id, input == null ? "delete" : "update", before, after, ct);
                    tx.Commit();
                }
            }
        }
    }
}
