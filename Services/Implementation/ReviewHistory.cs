using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json.Linq;
using samane.Models;
namespace samane.Services.Implementation
{
    // Delta rows contain BOTH previous and next values. Never expose raw legacy snapshots.
    public static class ReviewHistory
    {
        private static readonly HashSet<string> Fields = new HashSet<string>(
            typeof(PersonnelReview).GetProperties().Select(p => p.Name)
                .Except(new[] { "HistoryJson", "Version", "ModifiedAt", "ModifiedByUserId_Fk", "ConfirmedAt" }));
        public static JObject CreateEntry(IDictionary<string, object> previous,
                                          IDictionary<string, object> changes, string action,
                                          string actorId, string actorName, string text)
        {
            var rows = new JArray();
            foreach (var item in changes.Where(x => Fields.Contains(x.Key)))
            {
                object old;
                var hasBefore = previous.TryGetValue(item.Key, out old);
                var before = Token(item.Key, old);
                var after = Token(item.Key, item.Value);
                if (JToken.DeepEquals(before, after)) continue;
                rows.Add(new JObject { ["field"] = item.Key, ["before"] = before,
                                       ["after"] = after, ["hasBefore"] = hasBefore });
            }
            return JObject.FromObject(new { action, actorUserId = actorId, actorName,
                atUtc = DateTime.UtcNow, fieldChanges = rows, text });
        }
        public static JArray Project(string json, Func<string, bool> visible)
        {
            var result = new JArray();
            foreach (var entry in JArray.Parse(json ?? "[]").OfType<JObject>())
            {
                var rows = new JArray();
                var modern = entry["fieldChanges"] as JArray;
                if (modern != null)
                {
                    foreach (var row in modern.OfType<JObject>())
                    {
                        var name = (string)row["field"];
                        if (name == null || !Fields.Contains(name) || !visible(name)) continue;
                        rows.Add(new JObject { ["field"] = Camel(name),
                            ["before"] = row["before"]?.DeepClone() ?? JValue.CreateNull(),
                            ["after"] = row["after"]?.DeepClone() ?? JValue.CreateNull(),
                            ["hasBefore"] = (bool?)row["hasBefore"] ?? true });
                    }
                }
                else
                {
                    // Old edits did not save every before-value. Label missing values honestly.
                    var changes = entry["changes"] as JObject;
                    var snapshot = entry["snapshot"] as JObject;
                    if (changes != null) foreach (var item in changes.Properties())
                    {
                        if (!Fields.Contains(item.Name) || !visible(item.Name)) continue;
                        var old = snapshot?.Property(item.Name);
                        rows.Add(new JObject { ["field"] = Camel(item.Name),
                            ["before"] = old?.Value.DeepClone() ?? JValue.CreateNull(),
                            ["after"] = item.Value.DeepClone(), ["hasBefore"] = old != null });
                    }
                }
                result.Add(new JObject {
                    ["action"] = entry["action"]?.DeepClone(),
                    ["actorName"] = entry["actorName"]?.DeepClone() ?? entry["actorUserId"]?.DeepClone(),
                    ["atUtc"] = entry["atUtc"]?.DeepClone(), ["fieldChanges"] = rows,
                    ["legacy"] = modern == null
                });
            }
            return result;
        }
        private static JToken Token(string field, object value)
        {
            if (value == null) return JValue.CreateNull();
            if (value is decimal) value = ((decimal)value).ToString(CultureInfo.InvariantCulture);
            if (value is long) value = ((long)value).ToString(CultureInfo.InvariantCulture);
            if (value is DateTime && field.EndsWith("At")) value = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            return JToken.FromObject(value);
        }
        private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
