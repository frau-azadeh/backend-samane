using System;
namespace samane.Models
{
    // Exact mapping to the supplied SQL table. Never serialize this entity directly.
    public class PersonnelReview
    {
        public long Id { get; set; }
        public string ApplicantUserId_Fk { get; set; }
        public byte RequestTypeCode { get; set; }
        public byte StatusCode { get; set; }
        public string ApplicantFullName { get; set; }
        public string JobTitle { get; set; }
        public string DepartmentName { get; set; }
        public int? EmployeeIdReference { get; set; }
        public long? PartyIdReference { get; set; }
        public int? DepartmentEmployeeIdReference { get; set; }
        public int? JobIdReference { get; set; }
        public int? CompanyId { get; set; }
        public int? BranchId { get; set; }
        public int? OfficeId { get; set; }
        public int? PartId { get; set; }
        public int? PostId { get; set; }
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
        public DateTime ConfirmedAt { get; set; }
        public byte? MeetingTypeCode { get; set; }
        public DateTime? MeetingDate { get; set; }
        public TimeSpan? MeetingTime { get; set; }
        public string MeetingLocation { get; set; }
        public string InitialReview { get; set; }
        public string OwnerOpinion { get; set; }
        public string ManagementOpinion { get; set; }
        public string EvaluationManagerOpinion { get; set; }
        public string FinalResult { get; set; }
        public DateTime? FinalPublishedAt { get; set; }
        public string FinalPublishedByUserId_Fk { get; set; }
        public byte? ApplicantDecisionCode { get; set; }
        public string ApplicantDecisionDescription { get; set; }
        public DateTime? ApplicantDecisionAt { get; set; }
        public int ReviewRoundNo { get; set; }
        public string ReportCardReference { get; set; }
        public string AttachmentsJson { get; set; }
        public string HistoryJson { get; set; }
        public string CreatedByUserId_Fk { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ModifiedByUserId_Fk { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool Is_deleted { get; set; }
        public byte[] Version { get; set; }
    }
}
