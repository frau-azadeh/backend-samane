using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using samane.Models;
using samane.Security;
namespace samane.Services.Implementation
{
    public static class ReviewListFields
    {
        public static string[] Editable(ICurrentActor actor)
        {
            var fields = new List<string>();
            if (actor.Owner) fields.AddRange(new[] { "statusCode", "reviewRoundNo", "meetingTypeCode", "meetingDate", "meetingTime", "meetingLocation", "initialReview", "ownerOpinion", "finalResult", "applicantDecisionCode", "applicantDecisionDescription" });
            if (actor.Owner || actor.Management) fields.Add("managementOpinion");
            if (actor.Owner || actor.Evaluator) fields.Add("evaluationManagerOpinion");
            return fields.ToArray();
        }
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string> {
            {"applicantFullName","ApplicantFullName"},{"jobTitle","JobTitle"},{"departmentName","DepartmentName"},
            {"statusCode","StatusCode"},{"reviewRoundNo","ReviewRoundNo"},{"meetingTypeCode","MeetingTypeCode"},
            {"meetingDate","MeetingDate"},{"meetingTime","MeetingTime"},{"meetingLocation","MeetingLocation"},
            {"initialReview","InitialReview"},{"ownerOpinion","OwnerOpinion"},{"managementOpinion","ManagementOpinion"},
            {"evaluationManagerOpinion","EvaluationManagerOpinion"},{"finalResult","FinalResult"},
            {"applicantDecisionCode","ApplicantDecisionCode"},{"applicantDecisionDescription","ApplicantDecisionDescription"}
        };
        public static Dictionary<string, object> Build(JObject input, ICurrentActor actor,
                                                        IDictionary<string, object> previous = null)
        {
            var values = new Dictionary<string, object>();
            if (input == null) return values;
            var allowed = new HashSet<string>(Editable(actor));
            if (input.Count > 30) throw new ApiException(400, "تعداد فیلدهای ثبت نامعتبر است");
            foreach (var item in input.Properties())
            {
                string name;
                if (!Names.TryGetValue(item.Name, out name)) throw new ApiException(400, "فیلد ثبت ناشناخته است: " + item.Name);
                if (!allowed.Contains(item.Name)) throw new ApiException(403, "مجوز تغییر این فیلد را ندارید: " + item.Name);
                bool empty = item.Value.Type == JTokenType.Null || item.Value.Type == JTokenType.String && string.IsNullOrWhiteSpace((string)item.Value);
                if (name == "StatusCode" || name == "ReviewRoundNo" || name == "MeetingTypeCode" || name == "ApplicantDecisionCode")
                {
                    if (empty && (name == "MeetingTypeCode" || name == "ApplicantDecisionCode")) { values[name] = null; continue; }
                    int number;
                    int max = name == "StatusCode" ? 4 : name == "ReviewRoundNo" ? int.MaxValue : 2;
                    if (item.Value.Type != JTokenType.Integer || !int.TryParse(item.Value.ToString(), out number) || number < 1 || number > max)
                        throw new ApiException(400, "مقدار عددی نامعتبر است: " + item.Name);
                    values[name] = name == "ReviewRoundNo" ? (object)number : (byte)number;
                }
                else if (name == "MeetingDate")
                {
                    DateTime date;
                    if (empty) values[name] = null;
                    else if (item.Value.Type == JTokenType.String && DateTime.TryParseExact((string)item.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) values[name] = date;
                    else throw new ApiException(400, "تاریخ جلسه معتبر نیست");
                }
                else if (name == "MeetingTime")
                {
                    TimeSpan time;
                    if (empty) values[name] = null;
                    else if (item.Value.Type == JTokenType.String && TimeSpan.TryParseExact((string)item.Value, new[] { @"hh\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out time) && time.TotalHours < 24) values[name] = time;
                    else throw new ApiException(400, "ساعت جلسه معتبر نیست");
                }
                else
                {
                    if (item.Value.Type != JTokenType.String && item.Value.Type != JTokenType.Null) throw new ApiException(400, "فیلد متنی معتبر نیست: " + item.Name);
                    var text = ((string)item.Value)?.Trim();
                    var max = name == "ApplicantFullName" || name == "JobTitle" || name == "DepartmentName" ? 300 : name == "MeetingLocation" ? 500 : name == "ApplicantDecisionDescription" ? 2000 : 10000;
                    if (text != null && text.Length > max) throw new ApiException(400, "طول فیلد بیش از حد مجاز است: " + item.Name);
                    if (name == "ApplicantFullName" && string.IsNullOrEmpty(text)) throw new ApiException(400, "نام و نام خانوادگی الزامی است");
                    values[name] = string.IsNullOrEmpty(text) ? null : text;
                }
            }
            Func<string, object> current = name => values.ContainsKey(name) ? values[name] : previous != null && previous.ContainsKey(name) ? previous[name] : null;
            var status = Convert.ToByte(current("StatusCode") ?? (byte)1);
            var final = current("FinalResult") as string;
            var decision = current("ApplicantDecisionCode");
            if (status != 4 && (!string.IsNullOrWhiteSpace(final) || decision != null))
                throw new ApiException(400, "نتیجه نهایی و نظر متقاضی فقط در وضعیت بسته‌شده قابل ثبت‌اند");
            if (status == 4 && string.IsNullOrWhiteSpace(final)) throw new ApiException(400, "برای ثبت وضعیت بسته‌شده، نتیجه نهایی را بنویسید");
            if (status == 4 && actor.Owner)
            {
                var now = DateTime.UtcNow;
                values["FinalPublishedAt"] = now;
                values["FinalPublishedByUserId_Fk"] = actor.Id;
                if (decision != null) values["ApplicantDecisionAt"] = now;
            }
            return values;
        }
    }
}
