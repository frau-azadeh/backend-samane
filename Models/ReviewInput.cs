using System;
using Newtonsoft.Json.Linq;
namespace samane.Models
{
    public class ReviewInput
    {
        public JObject ListFields { get; set; }
        public string Version { get; set; }
        public bool SubjectIndex { get; set; }
        public bool SubjectAmount { get; set; }
        public bool SubjectPaymentMethod { get; set; }
        public bool AddIndex { get; set; }
        public bool RemoveIndex { get; set; }
        public bool? HasNewResponsibility { get; set; }
        public string ResponsibilityName { get; set; }
        public DateTime? ResponsibilityStartDate { get; set; }
        public bool? IsInJobDescription { get; set; }
        public string MissingJobDescriptionReason { get; set; }
        public string ProposedIndexName { get; set; }
        public bool? IsMeasurable { get; set; }
        public string RelatedGoal { get; set; }
        public byte? MeasurementTypeCode { get; set; }
        public string ActivityCriticalityReason { get; set; }
        public bool? HasExistingOverlap { get; set; }
        public string OverlapJustification { get; set; }
        public byte? ResultControlCode { get; set; }
        public string DependentFactors { get; set; }
        public string RemovalIndexName { get; set; }
        public string RemovalUnsupportedGoal { get; set; }
        public bool RemovalJobDutyRemoved { get; set; }
        public bool RemovalNoControl { get; set; }
        public bool RemovalNotCritical { get; set; }
        public bool AmountResponsibilityMismatch { get; set; }
        public bool AmountWeightMismatch { get; set; }
        public bool AmountComplexityIncreased { get; set; }
        public string AmountDescription { get; set; }
        public string ExistingIndexesJson { get; set; }
        public decimal? ExpectedTotalAmount { get; set; }
        public string PaymentMethodDescription { get; set; }
        public string OrganizationalValue { get; set; }
        public bool ApplyToSimilarJobs { get; set; }
        public string SimilarJobsDifference { get; set; }
        public bool ApplicantConfirmed { get; set; }
        public string ConfirmationText { get; set; }
    }
}
