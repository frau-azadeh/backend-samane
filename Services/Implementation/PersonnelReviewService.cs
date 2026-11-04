using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
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
    public class PersonnelReviewService : IPersonnelReviewService
    {
        private readonly ISqlConnectionFactory factory;
        private readonly ICurrentActor actor;
        private readonly IApplicantProfileResolver profileResolver;
        public PersonnelReviewService(ISqlConnectionFactory factory, ICurrentActor actor,
                                      IApplicantProfileResolver profileResolver)
        {
            this.factory = factory;
            this.actor = actor;
            this.profileResolver = profileResolver;
        }
        private const string Table = "dbo.TM_PersonnelReviewRequest";
        private static readonly IDictionary<string, string> Sorts = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase) { { "id", "PRR_Id" },
                                                { "createdAt", "PRR_CreatedAt" },
                                                { "applicantFullName", "PRR_ApplicantFullName" },
                                                { "departmentName", "PRR_DepartmentName" },
                                                { "jobTitle", "PRR_JobTitle" },
                                                { "statusCode", "PRR_StatusCode" },
                                                { "meetingDate", "PRR_MeetingDate" } };
        private SqlCommand CreateCommand(SqlConnection c, string sql, SqlTransaction tx = null)
        {
            return new SqlCommand(sql, c, tx) { CommandTimeout = 30 };
        }
        private static void AddParameter(SqlCommand c, string name, SqlDbType type, object value,
                                         int size = 0)
        {
            var p = c.Parameters.Add(name, type);
            if (size != 0)
                p.Size = size;
            p.Value = value ?? DBNull.Value;
        }
        private string Scope => actor.Owner || actor.Management ? "1=1"
                                : actor.Evaluator
                                    ? "(PRR_StatusCode=3 OR PRR_EvaluationManagerOpinion IS NOT NULL)"
                                    : "PRR_ApplicantUserId_Fk=@actor";
        private void AddActorParameter(SqlCommand cmd)
        {
            AddParameter(cmd, "@actor", SqlDbType.NVarChar, actor.Id, 450);
        }
        // SQL identifiers are chosen exclusively from this allowlist. Values are always parameters.
        private List<SqlParameter> BuildFilters(ReviewQuery q, ref string predicate)
        {
            var parameters = new List<SqlParameter>();
            var text = new Dictionary<string, string> {
                { "applicantFullName", "PRR_ApplicantFullName" }, { "jobTitle", "PRR_JobTitle" },
                { "departmentName", "PRR_DepartmentName" }, { "meetingLocation", "PRR_MeetingLocation" },
                { "initialReview", "PRR_InitialReview" },
                { "finalResult", "CASE WHEN PRR_FinalPublishedAt IS NOT NULL THEN PRR_FinalResult END" }
            };
            if (actor.Owner || actor.Evaluator || actor.Management) {
                text["ownerOpinion"] = "PRR_OwnerOpinion";
                text["evaluationManagerOpinion"] = "PRR_EvaluationManagerOpinion";
            }
            if (actor.Owner || actor.Evaluator) text["managementOpinion"] = "PRR_ManagementOpinion";
            var numbers = new Dictionary<string, string> { { "id", "PRR_Id" },
                { "reviewRoundNo", "PRR_ReviewRoundNo" }, { "statusCode", "PRR_StatusCode" },
                { "meetingTypeCode", "PRR_MeetingTypeCode" }, { "applicantDecisionCode", "PRR_ApplicantDecisionCode" } };
            if (q.Filters == null) return parameters;
            if (q.Filters.Count > 30) throw new ApiException(400, "تعداد فیلترها نامعتبر است");
            foreach (var f in q.Filters) {
                var value = f.Value?.Trim();
                if (string.IsNullOrEmpty(value)) continue;
                if (value.Length > 100) throw new ApiException(400, "طول فیلتر بیش از حد مجاز است");
                var name = "@filter" + parameters.Count;
                string column;
                if (text.TryGetValue(f.Key, out column)) {
                    // Contains matching: find the term anywhere, while escaping SQL LIKE metacharacters.
                    predicate += " AND (" + column + ") LIKE " + name + " ESCAPE '~'";
                    parameters.Add(new SqlParameter(name, SqlDbType.NVarChar, 210) { Value =
                        "%" + value.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%" });
                } else if (numbers.TryGetValue(f.Key, out column)) {
                    long n;
                    if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out n) || (n < 1 && f.Key != "id" && f.Key != "reviewRoundNo") ||
                        (f.Key == "statusCode" && n > 4) ||
                        ((f.Key == "meetingTypeCode" || f.Key == "applicantDecisionCode") && n > 2) ||
                        (f.Key == "reviewRoundNo" && n > int.MaxValue))
                        throw new ApiException(400, "فیلتر عددی نامعتبر است");
                    if (f.Key == "id" || f.Key == "reviewRoundNo") {
                        predicate += " AND CONVERT(nvarchar(20)," + column + ") LIKE " + name;
                        parameters.Add(new SqlParameter(name, SqlDbType.NVarChar, 102) { Value = "%" + value + "%" });
                        continue;
                    }
                    predicate += " AND " + column + "=" + name;
                    parameters.Add(new SqlParameter(name, f.Key == "id" ? SqlDbType.BigInt :
                        f.Key == "reviewRoundNo" ? SqlDbType.Int : SqlDbType.TinyInt) { Value = n });
                } else if (f.Key == "createdAtFrom" || f.Key == "createdAtTo" ||
                           f.Key == "meetingDateFrom" || f.Key == "meetingDateTo") {
                    DateTime date;
                    if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out date) || date.Year >= 9999)
                        throw new ApiException(400, "بازه تاریخ نامعتبر است");
                    var created = f.Key.StartsWith("createdAt");
                    var end = f.Key.EndsWith("To");
                    if (end) date = date.AddDays(1);
                    // CreatedAt is UTC; the UI's calendar day is Tehran (UTC+03:30).
                    if (created) date = date.AddMinutes(-210);
                    predicate += " AND " + (created ? "PRR_CreatedAt" : "PRR_MeetingDate") + (end ? "<" : ">=") + name;
                    parameters.Add(new SqlParameter(name, created ? SqlDbType.DateTime2 : SqlDbType.Date) { Value = date });
                } else if (f.Key == "subjects" && new[] { "subjectIndex", "subjectAmount", "subjectPaymentMethod" }.Contains(value)) {
                    predicate += " AND PRR_" + char.ToUpperInvariant(value[0]) + value.Substring(1) + "=1";
                } else if (f.Key == "meetingTime") {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"\A[0-9:]{1,8}\z"))
                        throw new ApiException(400, "ساعت نامعتبر است");
                    predicate += " AND CONVERT(char(8),PRR_MeetingTime,108) LIKE " + name;
                    parameters.Add(new SqlParameter(name, SqlDbType.VarChar, 10) { Value = "%" + value + "%" });
                } else throw new ApiException(400, "ستون فیلتر مجاز نیست");
            }
            foreach (var field in new[] { "createdAt", "meetingDate" }) {
                string from, to;
                if (q.Filters.TryGetValue(field + "From", out from) && q.Filters.TryGetValue(field + "To", out to) &&
                    !string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) && string.CompareOrdinal(from, to) > 0)
                    throw new ApiException(400, "تاریخ شروع باید قبل از تاریخ پایان باشد");
            }
            return parameters;
        }
        public async Task<PageResult<ReviewRow>> ListAsync(ReviewQuery q, CancellationToken ct)
        {
            // One scalar query parameter avoids Dictionary model-binding differences across hosts.
            if (!string.IsNullOrWhiteSpace(q.FiltersJson)) {
                if (q.FiltersJson.Length > 6000) throw new ApiException(400, "حجم فیلتر نامعتبر است");
                try {
                    var json = JObject.Parse(q.FiltersJson);
                    if (json.Properties().Count() > 30 || json.Properties().Any(p => p.Value.Type != JTokenType.String))
                        throw new ApiException(400, "ساختار فیلتر نامعتبر است");
                    q.Filters = json.Properties().ToDictionary(p => p.Name, p => (string)p.Value);
                } catch (JsonException) { throw new ApiException(400, "ساختار فیلتر نامعتبر است"); }
            }
            string sort;
            if (!Sorts.TryGetValue(q.Sort ?? "", out sort))
                throw new ApiException(400, "ستون مرتب‌سازی مجاز نیست");
            if (q.Page < 1 || q.Page > 100000 || q.PageSize < 1 || q.PageSize > 100)
                throw new ApiException(400, "صفحه‌بندی نامعتبر است");
            var predicate =
                "PRR_Is_deleted=0 AND (" + Scope +
                (") AND (@status IS NULL OR PRR_StatusCode=@status) AND (@search IS NULL OR PRR_ApplicantFullName LIKE " +
                 "@search ESCAPE '~' OR PRR_DepartmentName LIKE @search ESCAPE '~')");
            var filterParameters = BuildFilters(q, ref predicate);
            // Explicit projection: confidential opinions and history never enter the grid payload.
            var columns =
                "PRR_Id,PRR_ApplicantFullName,PRR_JobTitle,PRR_DepartmentName,PRR_CreatedAt,PRR_StatusCode,PRR_" +
                "SubjectIndex,PRR_SubjectAmount,PRR_SubjectPaymentMethod,PRR_MeetingTypeCode,PRR_MeetingDate," +
                "PRR_MeetingTime,PRR_MeetingLocation,PRR_InitialReview,CASE WHEN PRR_FinalPublishedAt IS NOT " +
                "NULL THEN PRR_FinalResult ELSE NULL END AS " +
                "PRR_FinalResult,PRR_ApplicantDecisionCode,PRR_ReviewRoundNo,PRR_Version,PRR_ApplicantUserId_" +
                "Fk,PRR_FinalPublishedAt";
            columns += "," +
                       (actor.Owner || actor.Evaluator || actor.Management ? "LEFT(PRR_OwnerOpinion,500)"
                                                                           : "CAST(NULL AS nvarchar(500))") +
                       " AS PRR_OwnerOpinion";
            columns += "," +
                       (actor.Owner || actor.Evaluator ? "LEFT(PRR_ManagementOpinion,500)"
                                                       : "CAST(NULL AS nvarchar(500))") +
                       " AS PRR_ManagementOpinion";
            columns +=
                "," +
                (actor.Owner || actor.Evaluator || actor.Management ? "LEFT(PRR_EvaluationManagerOpinion,500)"
                                                                    : "CAST(NULL AS nvarchar(500))") +
                " AS PRR_EvaluationManagerOpinion";
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd = CreateCommand(
                           c, "SELECT COUNT_BIG(1) FROM " + Table + " WHERE " + predicate + " OPTION(RECOMPILE); SELECT " +
                                  columns + " FROM " + Table + " WHERE " + predicate + " ORDER BY " + sort +
                                  (q.Desc ? " DESC" : " ASC") + (sort == "PRR_Id" ? "" : ",PRR_Id DESC") +
                                  " OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY OPTION(RECOMPILE);"))
                {
                    cmd.Parameters.AddRange(filterParameters.ToArray());
                    AddActorParameter(cmd);
                    AddParameter(cmd, "@status", SqlDbType.TinyInt, q.Status);
                    var search = q.Search?.Trim();
                    AddParameter(
                        cmd, "@search", SqlDbType.NVarChar,
                        string.IsNullOrEmpty(search)
                            ? null
                            : "%" +
                                  search.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace(
                                      "[", "~[") +
                                  "%",
                        210);
                    AddParameter(cmd, "@skip", SqlDbType.Int, checked((q.Page - 1) * q.PageSize));
                    AddParameter(cmd, "@take", SqlDbType.Int, q.PageSize);
                    using (var r = await cmd.ExecuteReaderAsync(ct))
                    {
                        await r.ReadAsync(ct);
                        var total = r.GetInt64(0);
                        await r.NextResultAsync(ct);
                        var rows = new List<ReviewRow>();
                        while (await r.ReadAsync(ct))
                            rows.Add(Row(r));
                        return new PageResult<ReviewRow>
                        {
                            AppliedFilters = q.Filters ?? new Dictionary<string, string>(),
                            Items = rows,
                            TotalCount = total,
                            Page = q.Page,
                            PageSize = q.PageSize
                        };
                    }
                }
            }
        }
        private static T ReadValue<T>(SqlDataReader r, string key)
        {
            var v = r["PRR_" + key];
            return v == DBNull.Value ? default(T) : (T)v;
        }
        private ReviewRow Row(SqlDataReader r)
        {
            return new ReviewRow
            {
                Id = ReadValue<long>(r, "Id").ToString(CultureInfo.InvariantCulture),
                ApplicantFullName = ReadValue<string>(r, "ApplicantFullName"),
                JobTitle = ReadValue<string>(r, "JobTitle"),
                DepartmentName = ReadValue<string>(r, "DepartmentName"),
                CreatedAt = DateTime.SpecifyKind(ReadValue<DateTime>(r, "CreatedAt"), DateTimeKind.Utc),
                StatusCode = ReadValue<byte>(r, "StatusCode"),
                SubjectIndex = ReadValue<bool>(r, "SubjectIndex"),
                SubjectAmount = ReadValue<bool>(r, "SubjectAmount"),
                SubjectPaymentMethod = ReadValue<bool>(r, "SubjectPaymentMethod"),
                MeetingTypeCode = ReadValue<byte?>(r, "MeetingTypeCode"),
                MeetingDate = ReadValue<DateTime?>(r, "MeetingDate"),
                MeetingTime = ReadValue<TimeSpan?>(r, "MeetingTime")?.ToString(@"hh\:mm"),
                MeetingLocation = ReadValue<string>(r, "MeetingLocation"),
                InitialReview = ReadValue<string>(r, "InitialReview"),
                OwnerOpinion = ReadValue<string>(r, "OwnerOpinion"),
                ManagementOpinion = ReadValue<string>(r, "ManagementOpinion"),
                EvaluationManagerOpinion = ReadValue<string>(r, "EvaluationManagerOpinion"),
                FinalResult = ReadValue<string>(r, "FinalResult"),
                ApplicantDecisionCode = ReadValue<byte?>(r, "ApplicantDecisionCode"),
                ReviewRoundNo = ReadValue<int>(r, "ReviewRoundNo"),
                Version = Convert.ToBase64String(ReadValue<byte[]>(r, "Version")),
                AllowedActions = GetAllowedActions(
                    ReadValue<string>(r, "ApplicantUserId_Fk"), ReadValue<byte>(r, "StatusCode"),
                    ReadValue<DateTime?>(r, "FinalPublishedAt"), ReadValue<byte?>(r, "ApplicantDecisionCode"))
            };
        }
        private string[] GetAllowedActions(string applicant, byte status, DateTime? published, byte? decision)
        {
            var a = new List<string> { "view", "attachments" };
            if (applicant == actor.Id && status == 1)
            {
                a.Add("edit");
                a.Add("delete");
                a.Add("upload");
            }
            if (actor.Owner && status != 4)
            {
                a.Add("review");
                a.Add("refer");
                a.Add("publish");
            }
            if (actor.Owner && status == 4 && !published.HasValue)
                a.Add("publish");
            if (actor.Evaluator && status == 3)
                a.Add("evaluate");
            if (actor.Management)
                a.Add("management");
            if (applicant == actor.Id && status == 4 && published.HasValue && !decision.HasValue)
            {
                a.Add("approve");
                a.Add("reopen");
            }
            return a.ToArray();
        }
        private async Task<Dictionary<string, object>> ReadAuthorizedAsync(SqlConnection c, long id,
                                                                           CancellationToken ct,
                                                                           SqlTransaction tx = null,
                                                                           bool locked = false)
        {
            // Explicit full detail projection; entity is filtered before serialization.
            var names = typeof(PersonnelReview).GetProperties().Select(p => "[PRR_" + p.Name + "]");
            using (var cmd = CreateCommand(c,
                                           "SELECT " + string.Join(",", names) + " FROM " + Table +
                                               (locked ? " WITH (UPDLOCK,ROWLOCK)" : "") +
                                               " WHERE PRR_Id=@id AND PRR_Is_deleted=0 AND (" + Scope + ")",
                                           tx))
            {
                AddActorParameter(cmd);
                AddParameter(cmd, "@id", SqlDbType.BigInt, id);
                using (var r = await cmd.ExecuteReaderAsync(ct))
                {
                    if (!await r.ReadAsync(ct))
                        throw new ApiException(404, "درخواست یافت نشد");
                    var d = new Dictionary<string, object>();
                    for (int i = 0; i < r.FieldCount; i++)
                        d[r.GetName(i).Substring(4)] = r.IsDBNull(i) ? null : r.GetValue(i);
                    return d;
                }
            }
        }
        public async Task<IDictionary<string, object>> GetAsync(long id, CancellationToken ct)
        {
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                var d = await ReadAuthorizedAsync(c, id, ct);
                var status = (byte)d["StatusCode"];
                d["AllowedActions"] =
                    GetAllowedActions((string)d["ApplicantUserId_Fk"], status,
                                      (DateTime?)d["FinalPublishedAt"], (byte?)d["ApplicantDecisionCode"]);
                d["Id"] = id.ToString();
                d["Version"] = Convert.ToBase64String((byte[])d["Version"]);
                if (!actor.Owner && !actor.Evaluator && !actor.Management)
                {
                    foreach (var k in new[] { "OwnerOpinion", "ManagementOpinion", "EvaluationManagerOpinion",
                                              "HistoryJson" })
                        d.Remove(k);
                }
                else if (actor.Management)
                    d.Remove("ManagementOpinion");
                if (d["FinalPublishedAt"] == null && !actor.Owner && !actor.Evaluator && !actor.Management)
                    d["FinalResult"] = null;
                return d.ToDictionary(k => char.ToLowerInvariant(k.Key[0]) + k.Key.Substring(1),
                                      k => k.Value);
            }
        }
        private static byte[] ParseVersion(string v)
        {
            try
            {
                var b = Convert.FromBase64String(v ?? "");
                if (b.Length != 8)
                    throw new FormatException();
                return b;
            }
            catch (FormatException)
            {
                throw new ApiException(400, "نسخه رکورد معتبر نیست");
            }
        }
        private async Task UpdateRowAsync(SqlConnection c, SqlTransaction tx, long id, byte[] version,
                                          Dictionary<string, object> values, CancellationToken ct)
        {
            values["ModifiedByUserId_Fk"] = actor.Id;
            values["ModifiedAt"] = DateTime.UtcNow;
            using (var cmd = CreateCommand(
                       c,
                       "UPDATE " + Table + " SET " +
                           string.Join(",", values.Keys.Select((k, i) => "[PRR_" + k + "]=@v" + i)) +
                           " WHERE PRR_Id=@id AND PRR_Version=@version AND PRR_Is_deleted=0",
                       tx))
            {
                int i = 0;
                foreach (var kv in values)
                    AddValue(cmd, "@v" + i++, kv.Key, kv.Value);
                AddParameter(cmd, "@id", SqlDbType.BigInt, id);
                AddParameter(cmd, "@version", SqlDbType.Timestamp, version, 8);
                if (await cmd.ExecuteNonQueryAsync(ct) != 1)
                    throw new ApiException(
                        409,
                        "رکورد تغییر کرده است؛ اطلاعات را تازه‌سازی " +
                            "کنید");
            }
        }
        private static void AddValue(SqlCommand cmd, string name, string field, object value)
        {
            var p = typeof(PersonnelReview).GetProperty(field);
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            var type = t == typeof(bool) ? SqlDbType.Bit
                       : t == typeof(byte) ? SqlDbType.TinyInt
                       : t == typeof(int) ? SqlDbType.Int
                       : t == typeof(long) ? SqlDbType.BigInt
                       : t == typeof(decimal) ? SqlDbType.Decimal
                       : t == typeof(TimeSpan) ? SqlDbType.Time
                       : t == typeof(DateTime)
                           ? (field.EndsWith("Date") ? SqlDbType.Date : SqlDbType.DateTime2)
                           : SqlDbType.NVarChar;
            AddParameter(cmd, name, type, value, type == SqlDbType.NVarChar ? -1 : 0);
            if (type == SqlDbType.Decimal)
            {
                cmd.Parameters[name].Precision = 19;
                cmd.Parameters[name].Scale = 2;
            }
        }
        private void AppendHistory(Dictionary<string, object> d, Dictionary<string, object> changes,
                                   string action, string text = null)
        {
            var h = JArray.Parse((string)d["HistoryJson"]);
            h.Add(JObject.FromObject(
                new
                {
                    action,
                    actorUserId = actor.Id,
                    atUtc = DateTime.UtcNow,
                    oldStatus = d["StatusCode"],
                    newStatus = changes.ContainsKey("StatusCode") ? changes["StatusCode"] : d["StatusCode"],
                    reviewRoundNo = d["ReviewRoundNo"],
                    text,
                    changes,
                    snapshot = new[] { "StatusCode", "FinalResult", "FinalPublishedAt",
                                         "ApplicantDecisionCode", "ApplicantDecisionDescription",
                                         "OwnerOpinion", "ManagementOpinion", "EvaluationManagerOpinion",
                                         "InitialReview", "MeetingDate", "MeetingTime", "MeetingLocation" }
                                     .ToDictionary(k => k, k => d[k])
                }));
            changes["HistoryJson"] = h.ToString(Formatting.None);
        }
        private async Task MutateAsync(long id, string version, string action,
                                       Func<Dictionary<string, object>, Dictionary<string, object>> change,
                                       CancellationToken ct, string text = null)
        {
            var v = ParseVersion(version);
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var tx = c.BeginTransaction())
                {
                    var d = await ReadAuthorizedAsync(c, id, ct, tx, true);
                    if (!((byte[])d["Version"]).SequenceEqual(v))
                        throw new ApiException(
                            409,
                            "رکورد تغییر کرده است؛ اطلاعات را تازه‌سازی " +
                                "کنید");
                    if (!GetAllowedActions((string)d["ApplicantUserId_Fk"], (byte)d["StatusCode"],
                                           (DateTime?)d["FinalPublishedAt"],
                                           (byte?)d["ApplicantDecisionCode"])
                             .Contains(action))
                        throw new ApiException(403, "این عملیات مجاز نیست");
                    var changes = change(d);
                    AppendHistory(d, changes, action, text);
                    await UpdateRowAsync(c, tx, id, v, changes, ct);
                    tx.Commit();
                }
            }
        }
        public Task AddAttachmentAsync(long id, string version, AttachmentInfo attachment,
                                       CancellationToken ct)
        {
            return MutateAsync(
                id, version, "upload",
                d =>
                {
                    var attachments = JArray.Parse((string)d["AttachmentsJson"]);
                    if (attachments.Count >= 20)
                        throw new ApiException(400, "حداکثر ۲۰ پیوست مجاز است");
                    attachment.CreatedBy = actor.Id;
                    attachment.CreatedAt = DateTime.UtcNow;
                    attachments.Add(JObject.FromObject(attachment, new JsonSerializer
                    {
                        ContractResolver =
                            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                    }));
                    return new Dictionary<string, object> { { "AttachmentsJson",
                                                              attachments.ToString(Formatting.None) } };
                },
                ct);
        }
        public Task DeleteAsync(long id, string version, CancellationToken ct)
        {
            return MutateAsync(id, version, "delete",
                               d => new Dictionary<string, object> { { "Is_deleted", true } }, ct);
        }
        private static Dictionary<string, object> ApplicantValues(ReviewInput input)
        {
            return ApplicantColumns.Names.ToDictionary(
                k => k, k => typeof(ReviewInput).GetProperty(k).GetValue(input));
        }
        public Task UpdateAsync(long id, ReviewInput input, CancellationToken ct)
        {
            ReviewValidation.Check(input);
            return MutateAsync(id, input.Version, "edit",
                               d =>
                               {
                                   var v = ApplicantValues(input);
                                   v["ConfirmedAt"] = DateTime.UtcNow;
                                   return v;
                               },
                               ct);
        }
        public async Task<string> CreateAsync(ReviewInput input, CancellationToken ct)
        {
            ReviewValidation.Check(input);
            var v = ApplicantValues(input);
            v["ApplicantUserId_Fk"] = actor.Id;
            var profile = profileResolver.Resolve();
            v["ApplicantFullName"] = profile.FullName;
            v["JobTitle"] = profile.JobTitle;
            v["DepartmentName"] = profile.DepartmentName;
            v["CreatedByUserId_Fk"] = actor.Id;
            v["ConfirmedAt"] = DateTime.UtcNow;
            v["HistoryJson"] = JsonConvert.SerializeObject(
                new[] { new { action = "create", actorUserId = actor.Id, atUtc = DateTime.UtcNow,
                              oldStatus = (byte?)null, newStatus = (byte)1, reviewRoundNo = 1 } });
            // Resolve JobTitle/Department and organization references using your host's existing employee
            // service before insert.
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                using (var cmd =
                           CreateCommand(c, "INSERT INTO " + Table + " (" +
                                                string.Join(",", v.Keys.Select(k => "[PRR_" + k + "]")) +
                                                ") OUTPUT INSERTED.PRR_Id VALUES (" +
                                                string.Join(",", v.Keys.Select((k, i) => "@v" + i)) + ")"))
                {
                    int i = 0;
                    foreach (var kv in v)
                        AddValue(cmd, "@v" + i++, kv.Key, kv.Value);
                    return Convert.ToString(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
                }
            }
        }
        private static void Required(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ApiException(400, "توضیحات این عملیات الزامی است");
        }
        public Task ActAsync(long id, ActionInput input, CancellationToken ct)
        {
            return MutateAsync(
                id, input.Version, input.Action,
                d =>
                {
                    var v = new Dictionary<string, object>();
                    switch (input.Action)
                    {
                        case "review":
                        case "refer":
                        case "publish":
                            Required(input.InitialReview);
                            Required(input.OwnerOpinion);
                            v["InitialReview"] = input.InitialReview;
                            v["OwnerOpinion"] = input.OwnerOpinion;
                            v["StatusCode"] = (byte)(input.Action == "refer" ? 3
                                                     : input.Action == "publish" ? 4
                                                                                 : 2);
                            if (input.Action == "refer")
                            {
                                v["MeetingTypeCode"] = input.MeetingTypeCode;
                                v["MeetingDate"] = input.MeetingDate;
                                v["MeetingTime"] = input.MeetingTime;
                                v["MeetingLocation"] = input.MeetingLocation;
                            }
                            if (input.Action == "publish")
                            {
                                Required(input.Text);
                                v["FinalResult"] = input.Text;
                                v["FinalPublishedAt"] = DateTime.UtcNow;
                                v["FinalPublishedByUserId_Fk"] = actor.Id;
                            }
                            break;
                        case "evaluate":
                            Required(input.Text);
                            v["EvaluationManagerOpinion"] = input.Text;
                            v["StatusCode"] = input.TargetStatus ?? (byte)2;
                            break;
                        case "management":
                            Required(input.Text);
                            v["ManagementOpinion"] = input.Text;
                            break;
                        case "approve":
                            v["ApplicantDecisionCode"] = (byte)1;
                            v["ApplicantDecisionDescription"] = input.Text;
                            v["ApplicantDecisionAt"] = DateTime.UtcNow;
                            break;
                        case "reopen":
                            Required(input.Text);
                            v["StatusCode"] = (byte)2;
                            v["ReviewRoundNo"] = (int)d["ReviewRoundNo"] + 1;
                            foreach (var k in new[] { "FinalResult", "FinalPublishedAt",
                                                  "FinalPublishedByUserId_Fk", "ApplicantDecisionCode",
                                                  "ApplicantDecisionDescription", "ApplicantDecisionAt",
                                                  "EvaluationManagerOpinion", "OwnerOpinion",
                                                  "ManagementOpinion", "InitialReview", "MeetingTypeCode",
                                                  "MeetingDate", "MeetingTime", "MeetingLocation" })
                                v[k] = null;
                            // Save previous result and applicant's new reason atomically with round increment.
                            break;
                        default:
                            throw new ApiException(400, "عملیات نامعتبر است");
                    }
                    return v;
                },
                ct, input.Text);
        }
    }
}
