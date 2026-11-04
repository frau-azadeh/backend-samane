/* پروژه 1070 - درخواست بازنگری پرسنل - فقط یک جدول
مبنای طراحی: سند تحلیل نسخه 4.0 و چک لیست ارسالی.
جدول های فعلی تغییر نمی کنند؛ کارنامه پیش نیاز نیست.
تمام فیلدها Description دارند. مبلغ decimal(19,2) و طول متن ها انتخاب طراحی هستند.
فیلدهای چندانتخابی محدود با bit ذخیره می شوند؛ ردیف های تکرارشونده و تاریخچه/پیوست JSON هستند.
ساختار و مجوز اعضای JSON، ثبت همه اقدامات، سطح دسترسی و محدودیت ویرایش/حذف در وضعیت 1
باید در سرویس اعمال شود. SQL فقط معتبر بودن JSON و آرایه بودن آن را کنترل می کند.
بررسی مجدد: ابتدا نتیجه و پاسخ قبلی را در HistoryJson نگه دارید؛ سپس در همان تراکنش
دور را افزایش و وضعیت را 2 کنید و فیلدهای نتیجه/انتشار/پاسخ جاری را خالی کنید.
سرویس باید از rowversion برای جلوگیری از بازنویسی همزمان HistoryJson استفاده کند.
در داشبورد متقاضی SELECT * نکنید؛ OwnerOpinion، ManagementOpinion و HistoryJson داخلی اند.
نوع کدها و الزام مرحله ای در Description ستون ها آمده است؛ نقش واقعی کاربران در سرویس بررسی شود.
SQL Server 2016+ با compatibility حداقل 130؛ اجرای واقعی روی موتور در محیط تهیه انجام نشده است.
*/
USE [Hamiran_Price];
GO
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') IS NOT NULL
  THROW 51000, N'TM_PersonnelReviewRequest already exists; nothing was changed.', 1;
 IF OBJECT_ID(N'dbo.AspNetUsers',N'U') IS NULL
  THROW 51001, N'AspNetUsers must already exist.', 1;
 CREATE TABLE dbo.TM_PersonnelReviewRequest (
  [PRR_Id] bigint IDENTITY(1,1) NOT NULL,
  [PRR_ApplicantUserId_Fk] nvarchar(450) COLLATE DATABASE_DEFAULT NOT NULL,
  [PRR_RequestTypeCode] tinyint NOT NULL CONSTRAINT [DF_PRR_RequestTypeCode] DEFAULT (1),
  [PRR_StatusCode] tinyint NOT NULL CONSTRAINT [DF_PRR_StatusCode] DEFAULT (1),
  [PRR_ApplicantFullName] nvarchar(300) COLLATE DATABASE_DEFAULT NOT NULL,
  [PRR_JobTitle] nvarchar(300) COLLATE DATABASE_DEFAULT NULL,
  [PRR_DepartmentName] nvarchar(300) COLLATE DATABASE_DEFAULT NULL,
  [PRR_EmployeeIdReference] int NULL,
  [PRR_PartyIdReference] bigint NULL,
  [PRR_DepartmentEmployeeIdReference] int NULL,
  [PRR_JobIdReference] int NULL,
  [PRR_CompanyId] int NULL,
  [PRR_BranchId] int NULL,
  [PRR_OfficeId] int NULL,
  [PRR_PartId] int NULL,
  [PRR_PostId] int NULL,
  [PRR_SubjectIndex] bit NOT NULL CONSTRAINT [DF_PRR_SubjectIndex] DEFAULT (0),
  [PRR_SubjectAmount] bit NOT NULL CONSTRAINT [DF_PRR_SubjectAmount] DEFAULT (0),
  [PRR_SubjectPaymentMethod] bit NOT NULL CONSTRAINT [DF_PRR_SubjectPaymentMethod] DEFAULT (0),
  [PRR_AddIndex] bit NOT NULL CONSTRAINT [DF_PRR_AddIndex] DEFAULT (0),
  [PRR_RemoveIndex] bit NOT NULL CONSTRAINT [DF_PRR_RemoveIndex] DEFAULT (0),
  [PRR_HasNewResponsibility] bit NULL,
  [PRR_ResponsibilityName] nvarchar(500) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ResponsibilityStartDate] date NULL,
  [PRR_IsInJobDescription] bit NULL,
  [PRR_MissingJobDescriptionReason] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ProposedIndexName] nvarchar(500) COLLATE DATABASE_DEFAULT NULL,
  [PRR_IsMeasurable] bit NULL,
  [PRR_RelatedGoal] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_MeasurementTypeCode] tinyint NULL,
  [PRR_ActivityCriticalityReason] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_HasExistingOverlap] bit NULL,
  [PRR_OverlapJustification] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ResultControlCode] tinyint NULL,
  [PRR_DependentFactors] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_RemovalIndexName] nvarchar(500) COLLATE DATABASE_DEFAULT NULL,
  [PRR_RemovalUnsupportedGoal] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_RemovalJobDutyRemoved] bit NOT NULL CONSTRAINT [DF_PRR_RemovalJobDutyRemoved] DEFAULT (0),
  [PRR_RemovalNoControl] bit NOT NULL CONSTRAINT [DF_PRR_RemovalNoControl] DEFAULT (0),
  [PRR_RemovalNotCritical] bit NOT NULL CONSTRAINT [DF_PRR_RemovalNotCritical] DEFAULT (0),
  [PRR_AmountResponsibilityMismatch] bit NOT NULL CONSTRAINT [DF_PRR_AmountResponsibilityMismatch] DEFAULT (0),
  [PRR_AmountWeightMismatch] bit NOT NULL CONSTRAINT [DF_PRR_AmountWeightMismatch] DEFAULT (0),
  [PRR_AmountComplexityIncreased] bit NOT NULL CONSTRAINT [DF_PRR_AmountComplexityIncreased] DEFAULT (0),
  [PRR_AmountDescription] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ExistingIndexesJson] nvarchar(max) COLLATE DATABASE_DEFAULT NOT NULL CONSTRAINT [DF_PRR_ExistingIndexesJson] DEFAULT (N'[]'),
  [PRR_ExpectedTotalAmount] decimal(19,2) NULL,
  [PRR_PaymentMethodDescription] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_OrganizationalValue] nvarchar(2000) COLLATE DATABASE_DEFAULT NOT NULL,
  [PRR_ApplyToSimilarJobs] bit NOT NULL,
  [PRR_SimilarJobsDifference] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ApplicantConfirmed] bit NOT NULL,
  [PRR_ConfirmationText] nvarchar(2000) COLLATE DATABASE_DEFAULT NOT NULL,
  [PRR_ConfirmedAt] datetime2(3) NOT NULL,
  [PRR_MeetingTypeCode] tinyint NULL,
  [PRR_MeetingDate] date NULL,
  [PRR_MeetingTime] time(0) NULL,
  [PRR_MeetingLocation] nvarchar(500) COLLATE DATABASE_DEFAULT NULL,
  [PRR_InitialReview] nvarchar(max) COLLATE DATABASE_DEFAULT NULL,
  [PRR_OwnerOpinion] nvarchar(max) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ManagementOpinion] nvarchar(max) COLLATE DATABASE_DEFAULT NULL,
  [PRR_EvaluationManagerOpinion] nvarchar(max) COLLATE DATABASE_DEFAULT NULL,
  [PRR_FinalResult] nvarchar(max) COLLATE DATABASE_DEFAULT NULL,
  [PRR_FinalPublishedAt] datetime2(3) NULL,
  [PRR_FinalPublishedByUserId_Fk] nvarchar(450) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ApplicantDecisionCode] tinyint NULL,
  [PRR_ApplicantDecisionDescription] nvarchar(2000) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ApplicantDecisionAt] datetime2(3) NULL,
  [PRR_ReviewRoundNo] int NOT NULL CONSTRAINT [DF_PRR_ReviewRoundNo] DEFAULT (1),
  [PRR_ReportCardReference] nvarchar(450) COLLATE DATABASE_DEFAULT NULL,
  [PRR_AttachmentsJson] nvarchar(max) COLLATE DATABASE_DEFAULT NOT NULL CONSTRAINT [DF_PRR_AttachmentsJson] DEFAULT (N'[]'),
  [PRR_HistoryJson] nvarchar(max) COLLATE DATABASE_DEFAULT NOT NULL CONSTRAINT [DF_PRR_HistoryJson] DEFAULT (N'[]'),
  [PRR_CreatedByUserId_Fk] nvarchar(450) COLLATE DATABASE_DEFAULT NOT NULL,
  [PRR_CreatedAt] datetime2(3) NOT NULL CONSTRAINT [DF_PRR_CreatedAt] DEFAULT (SYSUTCDATETIME()),
  [PRR_ModifiedByUserId_Fk] nvarchar(450) COLLATE DATABASE_DEFAULT NULL,
  [PRR_ModifiedAt] datetime2(3) NULL,
  [PRR_Is_deleted] bit NOT NULL CONSTRAINT [DF_PRR_Is_deleted] DEFAULT (0),
  [PRR_Version] rowversion NOT NULL,
  CONSTRAINT [PK_TM_PersonnelReviewRequest] PRIMARY KEY CLUSTERED ([PRR_Id]),
  CONSTRAINT [FK_PRR_ApplicantUserId_Fk] FOREIGN KEY ([PRR_ApplicantUserId_Fk]) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT [FK_PRR_FinalPublishedByUserId_Fk] FOREIGN KEY ([PRR_FinalPublishedByUserId_Fk]) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT [FK_PRR_CreatedByUserId_Fk] FOREIGN KEY ([PRR_CreatedByUserId_Fk]) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT [FK_PRR_ModifiedByUserId_Fk] FOREIGN KEY ([PRR_ModifiedByUserId_Fk]) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT [CK_PRR_Type] CHECK (PRR_RequestTypeCode=1),
  CONSTRAINT [CK_PRR_Status] CHECK (PRR_StatusCode IN (1,2,3,4)),
  CONSTRAINT [CK_PRR_Round] CHECK (PRR_ReviewRoundNo>=1),
  CONSTRAINT [CK_PRR_Subject] CHECK (PRR_SubjectIndex=1 OR PRR_SubjectAmount=1 OR PRR_SubjectPaymentMethod=1),
  CONSTRAINT [CK_PRR_IndexChange] CHECK ((PRR_SubjectIndex=1 AND (PRR_AddIndex=1 OR PRR_RemoveIndex=1)) OR (PRR_SubjectIndex=0 AND PRR_AddIndex=0 AND PRR_RemoveIndex=0)),
  CONSTRAINT [CK_PRR_AddRequired] CHECK (PRR_AddIndex=0 OR (PRR_HasNewResponsibility IS NOT NULL AND PRR_ProposedIndexName IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_ProposedIndexName)))>0 AND PRR_IsMeasurable IS NOT NULL AND PRR_RelatedGoal IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_RelatedGoal)))>0 AND PRR_MeasurementTypeCode IS NOT NULL AND PRR_HasExistingOverlap IS NOT NULL AND PRR_ResultControlCode IS NOT NULL)),
  CONSTRAINT [CK_PRR_MeasurementType] CHECK (PRR_MeasurementTypeCode IS NULL OR PRR_MeasurementTypeCode IN (1,2)),
  CONSTRAINT [CK_PRR_Control] CHECK (PRR_ResultControlCode IS NULL OR PRR_ResultControlCode IN (1,2,3)),
  CONSTRAINT [CK_PRR_Responsibility] CHECK (PRR_AddIndex=0 OR (PRR_HasNewResponsibility IS NOT NULL AND PRR_HasNewResponsibility=0) OR (PRR_ResponsibilityName IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_ResponsibilityName)))>0 AND PRR_ResponsibilityStartDate IS NOT NULL AND PRR_IsInJobDescription IS NOT NULL)),
  CONSTRAINT [CK_PRR_JobDescription] CHECK (PRR_AddIndex=0 OR (PRR_HasNewResponsibility IS NOT NULL AND PRR_HasNewResponsibility=0) OR (PRR_IsInJobDescription IS NOT NULL AND PRR_IsInJobDescription=1) OR (PRR_MissingJobDescriptionReason IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_MissingJobDescriptionReason)))>0)),
  CONSTRAINT [CK_PRR_Activity] CHECK (PRR_AddIndex=0 OR (PRR_MeasurementTypeCode IS NOT NULL AND PRR_MeasurementTypeCode=1) OR (PRR_ActivityCriticalityReason IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_ActivityCriticalityReason)))>0)),
  CONSTRAINT [CK_PRR_Overlap] CHECK (PRR_AddIndex=0 OR (PRR_HasExistingOverlap IS NOT NULL AND PRR_HasExistingOverlap=0) OR (PRR_OverlapJustification IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_OverlapJustification)))>0)),
  CONSTRAINT [CK_PRR_Dependency] CHECK (PRR_AddIndex=0 OR (PRR_ResultControlCode IS NOT NULL AND PRR_ResultControlCode=1) OR (PRR_DependentFactors IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_DependentFactors)))>0)),
  CONSTRAINT [CK_PRR_Removal] CHECK (PRR_RemoveIndex=0 OR (PRR_RemovalIndexName IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_RemovalIndexName)))>0 AND PRR_RemovalUnsupportedGoal IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_RemovalUnsupportedGoal)))>0 AND (PRR_RemovalJobDutyRemoved=1 OR PRR_RemovalNoControl=1 OR PRR_RemovalNotCritical=1))),
  CONSTRAINT [CK_PRR_Amount] CHECK (PRR_ExpectedTotalAmount IS NULL OR PRR_ExpectedTotalAmount>=0),
  CONSTRAINT [CK_PRR_AmountRequired] CHECK (PRR_SubjectAmount=0 OR (PRR_AmountDescription IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_AmountDescription)))>0 AND PRR_ExpectedTotalAmount IS NOT NULL AND LTRIM(RTRIM(PRR_ExistingIndexesJson))<>N'[]' AND (PRR_AmountResponsibilityMismatch=1 OR PRR_AmountWeightMismatch=1 OR PRR_AmountComplexityIncreased=1))),
  CONSTRAINT [CK_PRR_PaymentMethod] CHECK (PRR_SubjectPaymentMethod=0 OR (PRR_PaymentMethodDescription IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_PaymentMethodDescription)))>0)),
  CONSTRAINT [CK_PRR_SimilarJobs] CHECK (PRR_ApplyToSimilarJobs=1 OR (PRR_SimilarJobsDifference IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_SimilarJobsDifference)))>0)),
  CONSTRAINT [CK_PRR_Confirmed] CHECK (PRR_ApplicantConfirmed=1),
  CONSTRAINT [CK_PRR_Meeting] CHECK (PRR_MeetingTypeCode IS NULL OR PRR_MeetingTypeCode IN (1,2)),
  CONSTRAINT [CK_PRR_Publication] CHECK ((PRR_FinalPublishedAt IS NULL AND PRR_FinalPublishedByUserId_Fk IS NULL) OR (PRR_StatusCode=4 AND PRR_FinalPublishedAt IS NOT NULL AND PRR_FinalPublishedByUserId_Fk IS NOT NULL AND PRR_FinalResult IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_FinalResult)))>0)),
  CONSTRAINT [CK_PRR_Feedback] CHECK ((PRR_ApplicantDecisionCode IS NULL AND PRR_ApplicantDecisionAt IS NULL) OR (PRR_ApplicantDecisionCode IS NOT NULL AND PRR_ApplicantDecisionCode IN (1,2) AND PRR_ApplicantDecisionAt IS NOT NULL AND PRR_StatusCode=4 AND PRR_FinalPublishedAt IS NOT NULL AND PRR_ApplicantDecisionAt>=PRR_FinalPublishedAt)),
  CONSTRAINT [CK_PRR_ExistingIndexesJson] CHECK (ISJSON(PRR_ExistingIndexesJson)=1 AND LEFT(LTRIM(PRR_ExistingIndexesJson),1)=N'['),
  CONSTRAINT [CK_PRR_AttachmentsJson] CHECK (ISJSON(PRR_AttachmentsJson)=1 AND LEFT(LTRIM(PRR_AttachmentsJson),1)=N'['),
  CONSTRAINT [CK_PRR_HistoryJson] CHECK (ISJSON(PRR_HistoryJson)=1 AND LEFT(LTRIM(PRR_HistoryJson),1)=N'['),
  CONSTRAINT [CK_PRR_ApplicantFullNameRequired] CHECK (PRR_ApplicantFullName IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_ApplicantFullName)))>0),
  CONSTRAINT [CK_PRR_OrganizationalValueRequired] CHECK (PRR_OrganizationalValue IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_OrganizationalValue)))>0),
  CONSTRAINT [CK_PRR_ConfirmationTextRequired] CHECK (PRR_ConfirmationText IS NOT NULL AND LEN(LTRIM(RTRIM(PRR_ConfirmationText)))>0)
 );
 CREATE INDEX IX_PRR_Applicant ON dbo.TM_PersonnelReviewRequest(PRR_ApplicantUserId_Fk, PRR_CreatedAt DESC);
 CREATE INDEX IX_PRR_Status ON dbo.TM_PersonnelReviewRequest(PRR_StatusCode, PRR_Is_deleted, PRR_CreatedAt DESC);
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'درخواست بازنگری پرسنل؛ فاز ارزیابی عملکرد؛ طراحی تک جدولی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه درخواست',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_Id';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه متقاضی از AspNetUsers.Id',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantUserId_Fk';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'1 ارزیابی عملکرد؛ تنها نوع فعال این فاز',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RequestTypeCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'1 ثبت اولیه؛ 2 در حال بررسی؛ 3 در انتظار پاسخ مدیر ارزیابی عملکرد؛ 4 بسته شد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_StatusCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نام و نام خانوادگی متقاضی در زمان ثبت',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantFullName';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'سمت متقاضی در زمان ثبت',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_JobTitle';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نام واحد در زمان ثبت؛ از مرجع سازمانی استخراج شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_DepartmentName';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'ارجاع منطقی به TM_Party_Employee.PE_ID؛ به علت نبود کلید یکتا در ساختار ارسالی FK فیزیکی نیست',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_EmployeeIdReference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه راهکاران از TM_Party_Employee.PE_PartyID',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_PartyIdReference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'ارجاع منطقی به TM_DepartmentOfEmployee.DOE_ID',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_DepartmentEmployeeIdReference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'ارجاع منطقی به TM_AllJobs.Job_ID؛ بدون FK فیزیکی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_JobIdReference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه شرکت از TM_AllJobs.Company_Id_Fk',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_CompanyId';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه شعبه از TM_AllJobs.Branch_Id_Fk',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_BranchId';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه اداره از TM_AllJobs.Office_Id_FK',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_OfficeId';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه بخش از TM_AllJobs.Part_Id_Fk',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_PartId';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه پست از TM_AllJobs.Post_Id_Fk',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_PostId';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'موضوع شاخص های ارزیابی عملکرد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_SubjectIndex';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'موضوع مبلغ پرداختی عملکرد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_SubjectAmount';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'موضوع نحوه پرداخت ارزیابی عملکرد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_SubjectPaymentMethod';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نوع درخواست افزودن شاخص',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AddIndex';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نوع درخواست حذف شاخص',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemoveIndex';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آیا مسئولیت جدیدی اضافه شده؟ در افزودن شاخص اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_HasNewResponsibility';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نام مسئولیت جدید؛ در پاسخ بله اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ResponsibilityName';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تاریخ شروع مسئولیت؛ در پاسخ بله اجباری؛ ذخیره میلادی و نمایش شمسی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ResponsibilityStartDate';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آیا مسئولیت در شرح شغل است؟ در مسئولیت جدید اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_IsInJobDescription';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'توضیح عدم وجود در شرح شغل؛ در پاسخ خیر اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MissingJobDescriptionReason';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نام شاخص پیشنهادی؛ در افزودن اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ProposedIndexName';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آیا شاخص قابل اندازه گیری است؟ در افزودن اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_IsMeasurable';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'هدف مرتبط با واحد یا سازمان؛ در افزودن اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RelatedGoal';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'1 نتیجه نهایی عملکرد؛ 2 انجام فعالیت یا فرایند',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MeasurementTypeCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'دلیل کلیدی بودن فعالیت یا فرایند؛ در نوع 2 اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ActivityCriticalityReason';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آیا با شاخص های موجود ارزیابی می شود؟ در افزودن اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_HasExistingOverlap';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'دلیل تعریف یا پرداخت مجدد؛ در صورت همپوشانی اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_OverlapJustification';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'1 کاملاً تحت کنترل؛ 2 بخشی وابسته به سایر عوامل؛ 3 عمده وابسته',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ResultControlCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'عوامل یا واحدهای وابسته؛ برای کد کنترل 2 یا 3 اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_DependentFactors';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شاخص موردنظر برای حذف؛ در حذف اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemovalIndexName';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'با حذف این شاخص از کدام هدف سازمانی پشتیبانی نخواهد شد؟ در حذف اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemovalUnsupportedGoal';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'علت حذف: مسئولیت از شرح شغل حذف شده',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemovalJobDutyRemoved';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'علت حذف: کنترلی بر نتیجه شاخص ندارد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemovalNoControl';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'علت حذف: شاخص کلیدی نیست',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_RemovalNotCritical';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'دلیل مبلغ: عدم تناسب با مسئولیت و اثرگذاری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AmountResponsibilityMismatch';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'دلیل مبلغ: عدم تناسب وزن یا ضریب شاخص ها',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AmountWeightMismatch';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'دلیل مبلغ: افزایش حجم یا پیچیدگی مسئولیت ها',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AmountComplexityIncreased';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'توضیحات بازنگری مبلغ؛ در موضوع مبلغ اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AmountDescription';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آرایه شاخص های موجود: name، importance از 1 تا 10 و reasonCodes آرایه کدهای 1 اهداف سازمان، 2 پیچیدگی، 3 دقت، 4 زمان؛ اعتبار محتوای اعضا در سرویس کنترل شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ExistingIndexesJson';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'مجموع مبلغ مورد انتظار؛ در موضوع مبلغ اجباری؛ واحد ریال/تومان باید در پروژه یکسان تعیین شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ExpectedTotalAmount';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'توضیحات نحوه پرداخت؛ در موضوع نحوه پرداخت اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_PaymentMethodDescription';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نقش درخواست در خلق ارزش؛ اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_OrganizationalValue';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آیا تغییر برای افراد یا مشاغل مشابه نیز اعمال شود؟',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplyToSimilarJobs';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تفاوت با مشاغل مشابه؛ اگر تعمیم خیر است اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_SimilarJobsDifference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تایید یکپارچه پنج اصل سند پیش از ثبت؛ باید یک باشد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantConfirmed';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'متن پنج اصل تاییدشده در زمان ثبت',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ConfirmationText';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تاریخ و ساعت تایید متقاضی به UTC',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ConfirmedAt';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نوع جلسه اختیاری: 1 حضوری؛ 2 تلفنی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MeetingTypeCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تاریخ محلی جلسه؛ اختیاری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MeetingDate';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'ساعت محلی جلسه؛ اختیاری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MeetingTime';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'مکان جلسه؛ اختیاری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_MeetingLocation';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'بررسی اولیه معاونت منابع انسانی؛ در مرحله بررسی اجباری، هنگام ثبت متقاضی خالی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_InitialReview';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نظر مالک فرایند؛ داخلی و قابل مشاهده فقط برای مدیر ارزیابی و مدیریت سازمان',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_OwnerOpinion';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نظر مدیریت سازمان؛ اختیاری؛ قابل مشاهده فقط برای مالک و مدیر ارزیابی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ManagementOpinion';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نظر مدیر واحد ارزیابی عملکرد؛ در مرحله اقدام مدیر اجباری',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_EvaluationManagerOpinion';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نتیجه نهایی قابل مشاهده برای متقاضی پس از انتشار توسط مالک',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_FinalResult';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'زمان انتشار نتیجه نهایی به UTC؛ بستن مدیر به تنهایی انتشار نیست',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_FinalPublishedAt';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'کاربر منتشرکننده نتیجه نهایی؛ نقش مالک در سرویس بررسی شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_FinalPublishedByUserId_Fk';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نظر متقاضی فقط روی نتیجه بسته و منتشرشده: 1 مورد تایید؛ 2 بررسی مجدد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantDecisionCode';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'توضیحات نظر متقاضی؛ سند الزام آن را مشخص نکرده است',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantDecisionDescription';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'زمان ثبت نظر متقاضی به UTC',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ApplicantDecisionAt';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شماره دور رسیدگی؛ در بررسی مجدد افزایش می یابد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ReviewRoundNo';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه منطقی و اختیاری کارنامه؛ جدول کارنامه پیش نیاز نیست و FK ندارد',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ReportCardReference';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آرایه اطلاعات پیوست شامل originalName، storageKey، contentType، sizeBytes، createdBy و createdAt؛ فایل در فضای فایل نگهداری شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_AttachmentsJson';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'آرایه تاریخچه با actorUserId، atUtc، action، oldStatus، newStatus، reviewRoundNo و snapshot اطلاعات قبلی؛ ثبت اتمیک توسط سرویس؛ حاوی اطلاعات داخلی',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_HistoryJson';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه کاربر ثبت کننده از AspNetUsers.Id',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_CreatedByUserId_Fk';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تاریخ و ساعت ثبت به UTC',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_CreatedAt';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'شناسه آخرین ویرایش کننده از AspNetUsers.Id',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ModifiedByUserId_Fk';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'تاریخ و ساعت آخرین ویرایش به UTC',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_ModifiedAt';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'حذف منطقی؛ صفر فعال و یک حذف شده',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_Is_deleted';
 EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=N'نسخه کنترل همزمانی؛ تاریخ نیست و درج دستی نمی شود',@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'TM_PersonnelReviewRequest',@level2type=N'COLUMN',@level2name=N'PRR_Version';
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
