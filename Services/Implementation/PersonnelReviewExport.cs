using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using samane.Models;
namespace samane.Services.Implementation
{
    public partial class PersonnelReviewService
    {
        private bool VisibleField(string field, bool published)
        {
            if (field == "HistoryJson" || field == "Version" || field == "Is_deleted") return false;
            if (field == "ManagementOpinion") return actor.Owner || actor.Evaluator;
            if (field == "OwnerOpinion" || field == "EvaluationManagerOpinion")
                return actor.Owner || actor.Evaluator || actor.Management;
            if (field == "FinalResult") return published || actor.Owner || actor.Evaluator || actor.Management;
            return true;
        }
        private IDictionary<string, object> PublicDetail(Dictionary<string, object> raw, bool includeHistory)
        {
            bool published = raw.ContainsKey("FinalPublishedAt") && raw["FinalPublishedAt"] != null;
            var output = new Dictionary<string, object>();
            foreach (var pair in raw)
            {
                if (!VisibleField(pair.Key, published)) continue;
                object value = pair.Value;
                if (value is DateTime && pair.Key.EndsWith("At"))
                    value = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
                // Preserve decimal(19,2) and bigint precision in JavaScript and Excel.
                if (value is decimal) value = ((decimal)value).ToString(CultureInfo.InvariantCulture);
                if (value is long) value = ((long)value).ToString(CultureInfo.InvariantCulture);
                if (value is TimeSpan) value = ((TimeSpan)value).ToString(@"hh\:mm\:ss");
                output[char.ToLowerInvariant(pair.Key[0]) + pair.Key.Substring(1)] = value;
            }
            if (includeHistory)
            {
                output["version"] = Convert.ToBase64String((byte[])raw["Version"]);
                output["allowedActions"] = GetAllowedActions((string)raw["ApplicantUserId_Fk"],
                    (byte)raw["StatusCode"], (DateTime?)raw["FinalPublishedAt"], (byte?)raw["ApplicantDecisionCode"]);
                output["history"] = ReviewHistory.Project((string)raw["HistoryJson"], field => VisibleField(field, published));
            }
            return output;
        }
        public async Task<object> ExportPageAsync(ExportQuery input, CancellationToken ct)
        {
            if (input == null || input.PageIndex < 0 || input.PageIndex > 100)
                throw new ApiException(400, "صفحه خروجی نامعتبر است؛ سقف خروجی ۵۰هزار ردیف است");
            var q = input.Query;
            ValidateQuery(q);
            string sort;
            if (!Sorts.TryGetValue(q.Sort ?? "", out sort)) throw new ApiException(400, "مرتب‌سازی نامعتبر است");
            var predicate = "PRR_Is_deleted=0 AND (" + Scope + ") AND (@status IS NULL OR PRR_StatusCode=@status) " +
                "AND (@search IS NULL OR PRR_ApplicantFullName LIKE @search ESCAPE '~' OR PRR_DepartmentName LIKE @search ESCAPE '~')";
            var parameters = BuildFilters(q, ref predicate);
            if (input.SelectedIds != null)
            {
                if (input.SelectedIds.Count > 50000) throw new ApiException(400, "تعداد انتخاب‌ها بیش از سقف خروجی است");
                foreach (var id in input.SelectedIds) {
                    long parsed;
                    if (!long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) || parsed < 1)
                        throw new ApiException(400, "شناسه انتخابی نامعتبر است");
                }
                // One JSON parameter, not 50,000 SQL parameters; always intersects role scope + filters.
                predicate += " AND PRR_Id IN (SELECT CONVERT(bigint,[value]) FROM OPENJSON(@selectedIds))";
            }
            long upper = 0, after = 0;
            var cursor = input.Cursor?.Split(':');
            if (cursor != null && (cursor.Length != 2 ||
                !long.TryParse(cursor[0], NumberStyles.None, CultureInfo.InvariantCulture, out upper) || upper < 0 ||
                !long.TryParse(cursor[1], NumberStyles.None, CultureInfo.InvariantCulture, out after) || after < 0))
                throw new ApiException(400, "محدوده خروجی نامعتبر است");
            if (input.PageIndex > 0 && cursor == null) throw new ApiException(400, "نشانگر صفحه خروجی لازم است");
            bool keyset = sort == "PRR_Id";
            using (var c = factory.Create())
            {
                await c.OpenAsync(ct);
                if (cursor == null) {
                    using (var bound = CreateCommand(c, "SELECT ISNULL(MAX(PRR_Id),0) FROM " + Table + " WHERE " + predicate)) {
                        foreach (var p in parameters) bound.Parameters.Add((System.Data.SqlClient.SqlParameter)((ICloneable)p).Clone());
                        AddExportParameters(bound, q, input.SelectedIds);
                        upper = Convert.ToInt64(await bound.ExecuteScalarAsync(ct));
                    }
                }
                // Explicit complete business projection. History never participates in bulk export.
                var columns = typeof(PersonnelReview).GetProperties().Select(p => p.Name)
                    .Where(name => name != "HistoryJson").Select(name => "[PRR_" + name + "]");
                using (var cmd = CreateCommand(c, "SELECT " + string.Join(",", columns) + " FROM " + Table +
                    " WHERE " + predicate + " AND PRR_Id<=@upper" + (keyset && cursor != null ? (q.Desc ? " AND PRR_Id<@after" : " AND PRR_Id>@after") : "") + " ORDER BY " + sort + (q.Desc ? " DESC" : " ASC") +
                    (sort == "PRR_Id" ? "" : ",PRR_Id DESC") +
                    " OFFSET @skip ROWS FETCH NEXT 501 ROWS ONLY OPTION(RECOMPILE);"))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    AddExportParameters(cmd, q, input.SelectedIds);
                    AddParameter(cmd, "@upper", SqlDbType.BigInt, upper);
                    AddParameter(cmd, "@skip", SqlDbType.Int, keyset ? 0 : input.PageIndex * 500);
                    if (keyset && cursor != null) AddParameter(cmd, "@after", SqlDbType.BigInt, after);
                    var rows = new List<IDictionary<string, object>>();
                    using (var reader = await cmd.ExecuteReaderAsync(ct)) {
                        while (await reader.ReadAsync(ct)) {
                            var raw = new Dictionary<string, object>();
                            for (int i=0;i<reader.FieldCount;i++) raw[reader.GetName(i).Substring(4)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            rows.Add(PublicDetail(raw, false));
                        }
                    }
                    var items = rows.Take(500).ToArray();
                    var last = items.Length == 0 ? after.ToString(CultureInfo.InvariantCulture) : (string)items[items.Length-1]["id"];
                    return new { items, hasMore = rows.Count > 500,
                        cursor = upper.ToString(CultureInfo.InvariantCulture) + ":" + last, appliedFilters = q.Filters };
                }
            }
        }
        private void AddExportParameters(System.Data.SqlClient.SqlCommand cmd, ReviewQuery q, List<string> selected)
        {
            AddActorParameter(cmd);
            AddParameter(cmd, "@status", SqlDbType.TinyInt, q.Status);
            var value = q.Search?.Trim();
            if (!string.IsNullOrEmpty(value) && value.Length > 100) throw new ApiException(400, "متن جستجو طولانی است");
            AddParameter(cmd, "@search", SqlDbType.NVarChar, string.IsNullOrEmpty(value) ? null :
                (q.TextMatch == "contains" ? "%" : "") + value.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[") + "%", 210);
            if (selected != null) AddParameter(cmd, "@selectedIds", SqlDbType.NVarChar, JsonConvert.SerializeObject(selected), -1);
        }
    }
}
