-- Run after 001 and 002 in the application's SQL Server database.
-- Existing tables/data are preserved. Index creation can take time on large tables.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_ActiveCreated')
 CREATE INDEX IX_PRR_ActiveCreated ON dbo.TM_PersonnelReviewRequest(PRR_CreatedAt DESC, PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk,PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_FilterName')
 CREATE INDEX IX_PRR_FilterName ON dbo.TM_PersonnelReviewRequest(PRR_ApplicantFullName,PRR_CreatedAt DESC,PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk,PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_FilterDepartment')
 CREATE INDEX IX_PRR_FilterDepartment ON dbo.TM_PersonnelReviewRequest(PRR_DepartmentName,PRR_CreatedAt DESC,PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk,PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_FilterJob')
 CREATE INDEX IX_PRR_FilterJob ON dbo.TM_PersonnelReviewRequest(PRR_JobTitle,PRR_CreatedAt DESC,PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk,PRR_StatusCode) WHERE PRR_Is_deleted=0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_FilterMeetingDate')
 CREATE INDEX IX_PRR_FilterMeetingDate ON dbo.TM_PersonnelReviewRequest(PRR_MeetingDate,PRR_Id DESC)
 INCLUDE(PRR_ApplicantUserId_Fk,PRR_StatusCode,PRR_CreatedAt) WHERE PRR_Is_deleted=0;
COMMIT TRANSACTION;
GO
