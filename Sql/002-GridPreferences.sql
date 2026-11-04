-- Safe to rerun. Creates settings only when missing; does not reset user choices.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.TM_UserGridPreference',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.TM_UserGridPreference (
  UserId nvarchar(450) NOT NULL,
  GridId varchar(100) NOT NULL,
  SettingsJson nvarchar(max) NOT NULL,
  ModifiedAt datetime2(3) NOT NULL CONSTRAINT DF_UserGrid_Modified DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_UserGridPreference PRIMARY KEY(UserId,GridId),
  CONSTRAINT FK_UserGridPreference_User FOREIGN KEY(UserId) REFERENCES dbo.AspNetUsers(Id),
  CONSTRAINT CK_UserGridPreference_Json CHECK(ISJSON(SettingsJson)=1),
  CONSTRAINT CK_UserGridPreference_Size CHECK(DATALENGTH(SettingsJson)<=20000)
 );
END;
GO
-- Upgrade a previous fixed-length definition without resetting any settings.
IF COL_LENGTH(N'dbo.TM_UserGridPreference',N'SettingsJson') <> -1
 ALTER TABLE dbo.TM_UserGridPreference ALTER COLUMN SettingsJson nvarchar(max) NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.TM_UserGridPreference') AND name=N'CK_UserGridPreference_Size')
 ALTER TABLE dbo.TM_UserGridPreference WITH CHECK ADD CONSTRAINT CK_UserGridPreference_Size CHECK(DATALENGTH(SettingsJson)<=20000);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelReviewRequest') AND name=N'IX_PRR_Applicant_Active_Created')
 CREATE INDEX IX_PRR_Applicant_Active_Created ON dbo.TM_PersonnelReviewRequest
 (PRR_ApplicantUserId_Fk,PRR_CreatedAt DESC,PRR_Id DESC)
 INCLUDE(PRR_StatusCode,PRR_ApplicantFullName,PRR_JobTitle,PRR_DepartmentName)
 WHERE PRR_Is_deleted=0;
GO
