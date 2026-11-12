-- Existing database only: run after 001, 002, 003. No data is deleted.
-- Run in the application's database, during an appropriate maintenance window.
-- Prefix searches can use the existing name/department/job indexes from 003.
-- These additional leading-user keys help applicant scope and latest-first paging/counts.
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.TM_PersonnelReviewRequest', N'U') IS NULL
 THROW 51000, N'Run the PersonnelReview schema migrations first.', 1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_ApplicantCreated')
 CREATE INDEX IX_PRR_ApplicantCreated ON dbo.TM_PersonnelReviewRequest
 (PRR_ApplicantUserId_Fk, PRR_CreatedAt DESC, PRR_Id DESC)
 INCLUDE(PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_ApplicantName')
 CREATE INDEX IX_PRR_ApplicantName ON dbo.TM_PersonnelReviewRequest
 (PRR_ApplicantUserId_Fk, PRR_ApplicantFullName, PRR_Id DESC)
 INCLUDE(PRR_CreatedAt,PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_StatusCreated')
 CREATE INDEX IX_PRR_StatusCreated ON dbo.TM_PersonnelReviewRequest
 (PRR_StatusCode, PRR_CreatedAt DESC, PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_ApplicantId')
 CREATE INDEX IX_PRR_ApplicantId ON dbo.TM_PersonnelReviewRequest
 (PRR_ApplicantUserId_Fk, PRR_Id DESC)
 INCLUDE(PRR_CreatedAt,PRR_StatusCode) WHERE PRR_Is_deleted=0;
COMMIT TRANSACTION;
