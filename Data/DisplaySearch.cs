using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using samane.Models;
namespace samane.Data
{
    // Expressions are server-owned and scoped by the caller's column permissions.
    internal static class DisplaySearch
    {
        internal static string Normalize(string value) => string.Concat((value ?? "").Select(c =>
            c >= '۰' && c <= '۹' ? (char)('0' + c - '۰') : c >= '٠' && c <= '٩' ? (char)('0' + c - '٠') : c == 'ي' ? 'ی' : c == 'ك' ? 'ک' : c)).Replace("\u200e", "").Replace("\u200f", "").Replace(", ", " ").Replace("، ", " ");
        private static string SqlNormalize(string expression) {
            var sql = "COALESCE(CONVERT(nvarchar(max),(" + expression + ")),N'—')";
            for (var i=0;i<10;i++) sql = "REPLACE(REPLACE("+sql+",N'"+(char)('۰'+i)+"',N'"+i+"'),N'"+(char)('٠'+i)+"',N'"+i+"')";
            return "REPLACE(REPLACE("+sql+",N'ي',N'ی'),N'ك',N'ک')";
        }
        internal static bool Add(string key, string raw, IDictionary<string,string> fields, ISet<string> numeric,
            List<SqlParameter> parameters, ref string predicate) {
            if (!key.StartsWith("$", StringComparison.Ordinal)) return false;
            JObject rule;
            try { rule = JObject.Parse(raw); } catch { throw new ApiException(400,"ساختار شرط نامعتبر است"); }
            if (rule.Properties().Any(p => p.Value.Type != JTokenType.String)) throw new ApiException(400,"ساختار شرط نامعتبر است");
            var field=(string)rule["field"]; var op=(string)rule["op"]; var value=Normalize((string)rule["value"]).Trim();
            string expression;
            if (field == null || !fields.TryGetValue(field,out expression) || value.Length == 0 || value.Length > 100)
                throw new ApiException(400,"ستون یا مقدار شرط مجاز نیست");
            if (field == "id") value = value.TrimStart('#');
            // A calendar day applies to all times on that displayed day.
            if ((field == "createdAt" || field == "meetingDate") && value.Length == 10)
                expression = "LEFT(" + expression + ",10)";
            var operators=new Dictionary<string,string>{{"eq","="},{"ne","<>"},{"lt","<"},{"gt",">"}};
            var name="@display"+parameters.Count;
            string comparison;
            if (op == "contains" || op == "prefix") {
                comparison=SqlNormalize(expression)+" LIKE "+name+" ESCAPE N'~'";
                value=(op=="contains"?"%":"")+value.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[")+"%";
                parameters.Add(new SqlParameter(name,SqlDbType.NVarChar,210){Value=value});
            } else {
                string symbol;
                if(op == null || !operators.TryGetValue(op,out symbol)) throw new ApiException(400,"عملگر مجاز نیست");
                if (numeric.Contains(field)) {
                    decimal number;
                    if(!decimal.TryParse(value.TrimStart('#'),NumberStyles.Number,CultureInfo.InvariantCulture,out number)) throw new ApiException(400,"مقدار عددی نامعتبر است");
                    comparison="("+expression+")"+symbol+name;
                    parameters.Add(new SqlParameter(name,SqlDbType.Decimal){Value=number,Precision=28,Scale=4});
                } else {
                    comparison=SqlNormalize(expression)+symbol+name;
                    parameters.Add(new SqlParameter(name,SqlDbType.NVarChar,100){Value=value});
                }
            }
            predicate+=" AND ("+comparison+")"; return true;
        }
    }
}
