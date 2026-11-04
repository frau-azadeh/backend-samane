using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace samane.Models
{
    public class ReviewQuery
    {
        [Range(1, 100000)]
        public int Page { get; set; } = 1;
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
        [StringLength(100)]
        public string Search { get; set; }
        [Range(1, 4)]
        public byte? Status { get; set; }
        [StringLength(6000)]
        public string FiltersJson { get; set; }
        public Dictionary<string, string> Filters { get; set; } = new Dictionary<string, string>();
        public string Sort { get; set; } = "createdAt";
        public bool Desc { get; set; } = true;
    }
    public class PageResult<T>
    {
        public Dictionary<string, string> AppliedFilters { get; set; }
        public IList<T> Items { get; set; }
        public long TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
    public class ReviewRow
    {
        // String avoids loss of precision for SQL bigint in JavaScript.
        public string Id { get; set; }
        public string ApplicantFullName { get; set; }
        public string JobTitle { get; set; }
        public string DepartmentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte StatusCode { get; set; }
        public bool SubjectIndex { get; set; }
        public bool SubjectAmount { get; set; }
        public bool SubjectPaymentMethod { get; set; }
        public byte? MeetingTypeCode { get; set; }
        public DateTime? MeetingDate { get; set; }
        public string MeetingTime { get; set; }
        public string MeetingLocation { get; set; }
        public string InitialReview { get; set; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string OwnerOpinion { get; set; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string ManagementOpinion { get; set; }
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string EvaluationManagerOpinion { get; set; }
        public string FinalResult { get; set; }
        public byte? ApplicantDecisionCode { get; set; }
        public int ReviewRoundNo { get; set; }
        public string Version { get; set; }
        public string[] AllowedActions { get; set; }
    }
    public class VersionInput
    {
        [Required]
        public string Version { get; set; }
    }
    public class ActionInput : VersionInput
    {
        [Required]
        public string Action { get; set; }
        [StringLength(10000)]
        public string Text {
            get; set;
        }
        [StringLength(10000)]
        public string InitialReview {
            get; set;
        }
        [StringLength(10000)]
        public string OwnerOpinion {
            get; set;
        }
        [Range(1, 2)]
        public byte? MeetingTypeCode {
            get; set;
        }
        public DateTime? MeetingDate { get; set; }
        public TimeSpan? MeetingTime { get; set; }
        [StringLength(500)]
        public string MeetingLocation {
            get; set;
        }
        [Range(2, 4)]
        public byte? TargetStatus {
            get; set;
        }
    }
    public class ApiException : Exception
    {
        public int Status { get; }
        public ApiException(int status, string message) : base(message)
        {
            Status = status;
        }
    }
}
