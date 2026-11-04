using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using samane.Models;
namespace samane.Services.Implementation
{
    internal static class ReviewValidation
    {
        private static void Need(bool ok, string message)
        {
            if (!ok)
                throw new ApiException(400, message);
        }
        private static bool Text(string s) => !string.IsNullOrWhiteSpace(s);
        public static void Check(ReviewInput i)
        {
            foreach (var p in typeof(ReviewInput).GetProperties())
                if (p.PropertyType == typeof(string))
                {
                    var s = (string)p.GetValue(i);
                    var max = p.Name == "ResponsibilityName" || p.Name == "ProposedIndexName" ||
                                      p.Name == "RemovalIndexName"
                                  ? 500
                              : p.Name == "Version" ? 100
                                                    : 2000;
                    if (p.Name.EndsWith("Json"))
                        max = 50000;
                    Need(s == null || s.Length <= max, "طول فیلد " + p.Name + " بیش از حد مجاز است");
                }
            Need(i.SubjectIndex || i.SubjectAmount || i.SubjectPaymentMethod, "حداقل یک موضوع انتخاب کنید");
            Need(i.SubjectIndex ? (i.AddIndex || i.RemoveIndex) : (!i.AddIndex && !i.RemoveIndex),
                 "موضوع شاخص و نوع تغییر هماهنگ نیستند");
            Need(i.ApplicantConfirmed && Text(i.ConfirmationText) && Text(i.OrganizationalValue),
                 "تأیید و ارزش سازمانی الزامی است");
            Need(i.ApplyToSimilarJobs || Text(i.SimilarJobsDifference), "تفاوت مشاغل مشابه را بنویسید");
            if (i.AddIndex)
            {
                Need(i.HasNewResponsibility.HasValue && Text(i.ProposedIndexName) &&
                         i.IsMeasurable.HasValue && Text(i.RelatedGoal) && i.HasExistingOverlap.HasValue,
                     "اطلاعات شاخص جدید ناقص است");
                Need(i.MeasurementTypeCode == 1 || i.MeasurementTypeCode == 2,
                     "نوع اندازه‌گیری نامعتبر است");
                Need(i.ResultControlCode >= 1 && i.ResultControlCode <= 3, "میزان کنترل نامعتبر است");
                if (i.HasNewResponsibility == true)
                {
                    Need(Text(i.ResponsibilityName) && i.ResponsibilityStartDate.HasValue &&
                             i.IsInJobDescription.HasValue,
                         "مشخصات مسئولیت جدید ناقص است");
                    Need(i.IsInJobDescription == true || Text(i.MissingJobDescriptionReason),
                         "دلیل نبود در شرح شغل الزامی است");
                }
                Need(i.MeasurementTypeCode == 1 || Text(i.ActivityCriticalityReason),
                     "دلیل کلیدی بودن فعالیت الزامی است");
                Need(i.HasExistingOverlap == false || Text(i.OverlapJustification),
                     "دلیل همپوشانی الزامی است");
                Need(i.ResultControlCode == 1 || Text(i.DependentFactors), "عوامل وابسته الزامی است");
            }
            if (i.RemoveIndex)
                Need(Text(i.RemovalIndexName) && Text(i.RemovalUnsupportedGoal) &&
                         (i.RemovalJobDutyRemoved || i.RemovalNoControl || i.RemovalNotCritical),
                     "اطلاعات حذف شاخص ناقص است");
            Need(!i.ExpectedTotalAmount.HasValue ||
                     (i.ExpectedTotalAmount >= 0 && i.ExpectedTotalAmount <= 99999999999999999.99m &&
                      decimal.Round(i.ExpectedTotalAmount.Value, 2) == i.ExpectedTotalAmount),
                 "مبلغ نامعتبر است");
            var rows = Array(i.ExistingIndexesJson);
            Need(rows.Count <= 100, "حداکثر ۱۰۰ شاخص مجاز است");
            foreach (var row in rows)
            {
                var o = row as JObject;
                Need(o != null, "ساختار شاخص نامعتبر است");
                Need(o.Properties().All(p => new[] { "name", "importance", "reasons" }.Contains(p.Name)),
                     "عضو ناشناخته در شاخص");
                Need(o["name"]?.Type == JTokenType.String && Text((string)o["name"]) &&
                         ((string)o["name"]).Length <= 500,
                     "نام شاخص نامعتبر است");
                var importance = o["importance"];
                Need(IntegerInRange(importance, 1, 10), "اهمیت باید ۱ تا ۱۰ باشد");
                var reasons = o["reasons"] as JArray;
                Need(reasons != null && reasons.Count > 0 && reasons.Count <= 4 &&
                         reasons.All(r => IntegerInRange(r, 1, 4)) &&
                         reasons.Select(r => r.ToString()).Distinct().Count() == reasons.Count,
                     "دلایل اهمیت نامعتبر است");
            }
            if (i.SubjectAmount)
                Need(Text(i.AmountDescription) && i.ExpectedTotalAmount.HasValue && rows.Count > 0 &&
                         (i.AmountResponsibilityMismatch || i.AmountWeightMismatch ||
                          i.AmountComplexityIncreased),
                     "اطلاعات بازنگری مبلغ ناقص است");
            if (i.SubjectPaymentMethod)
                Need(Text(i.PaymentMethodDescription), "توضیح نحوه پرداخت الزامی است");
            i.ExistingIndexesJson = rows.ToString(Newtonsoft.Json.Formatting.None);
        }
        private static bool IntegerInRange(JToken token, int min, int max)
        {
            int value;
            return token?.Type == JTokenType.Integer && int.TryParse(token.ToString(), out value) &&
                   value >= min && value <= max;
        }
        private static JArray Array(string json)
        {
            try
            {
                return JArray.Parse(json ?? "[]");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                throw new ApiException(400, "ساختار JSON شاخص‌ها نامعتبر است");
            }
        }
    }
}
