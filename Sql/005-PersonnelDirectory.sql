USE [Hamiran_Price];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

-- Only needed for legacy state records without any history. Supply real values, never a fabricated actor.
DECLARE @LegacyCreatedByUserId nvarchar(450) = NULL;
DECLARE @LegacyCreatedAt datetime2(7) = NULL;

BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
  THROW 51000, N'AspNetUsers must exist in Hamiran_Price.', 1;

 -- Upgrade the earlier delivered schema in place. Data and JSON snapshots are preserved.
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'UserId') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'PDS_UserId_Fk') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryState.UserId', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryState.UserId', N'PDS_UserId_Fk', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'IsDeleted') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'PDS_Is_deleted') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryState.IsDeleted', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryState.IsDeleted', N'PDS_Is_deleted', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'Revision') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState', N'PDS_Revision') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryState.Revision', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryState.Revision', N'PDS_Revision', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'Id') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_Id') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.Id', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.Id', N'PDH_Id', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'UserId') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_UserId_Fk') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.UserId', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.UserId', N'PDH_UserId_Fk', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'Action') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_Action') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.Action', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.Action', N'PDH_Action', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'ActorId') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_CreatedByUserId_Fk') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.ActorId', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.ActorId', N'PDH_CreatedByUserId_Fk', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'ActorName') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_CreatedByName') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.ActorName', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.ActorName', N'PDH_CreatedByName', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'ChangedAt') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_CreatedAt') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.ChangedAt', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.ChangedAt', N'PDH_CreatedAt', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'BeforeJson') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_BeforeJson') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.BeforeJson', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.BeforeJson', N'PDH_BeforeJson', N'COLUMN';
 END;
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'AfterJson') IS NOT NULL
 BEGIN
  IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory', N'PDH_AfterJson') IS NOT NULL
   THROW 51001, N'Both legacy and new column exist: TM_PersonnelDirectoryHistory.AfterJson', 1;
  EXEC sys.sp_rename N'dbo.TM_PersonnelDirectoryHistory.AfterJson', N'PDH_AfterJson', N'COLUMN';
 END;

 IF OBJECT_ID(N'dbo.TM_PersonnelDirectoryState', N'U') IS NULL
 BEGIN
  CREATE TABLE dbo.TM_PersonnelDirectoryState (
   PDS_UserId_Fk nvarchar(450) NOT NULL,
   PDS_Is_deleted bit NOT NULL CONSTRAINT DF_PDS_Is_deleted DEFAULT(0),
   PDS_Revision uniqueidentifier NOT NULL CONSTRAINT DF_PDS_Revision DEFAULT NEWID(),
   PDS_CreatedByUserId_Fk nvarchar(450) NOT NULL,
   PDS_CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_PDS_CreatedAt DEFAULT SYSUTCDATETIME(),
   CONSTRAINT PK_TM_PersonnelDirectoryState PRIMARY KEY CLUSTERED(PDS_UserId_Fk),
   CONSTRAINT FK_TM_DirectoryUser FOREIGN KEY(PDS_UserId_Fk) REFERENCES dbo.AspNetUsers(Id)
  );
 END;
 IF OBJECT_ID(N'dbo.TM_PersonnelDirectoryHistory', N'U') IS NULL
 BEGIN
  CREATE TABLE dbo.TM_PersonnelDirectoryHistory (
   PDH_Id bigint IDENTITY(1,1) NOT NULL,
   PDH_UserId_Fk nvarchar(450) NOT NULL,
   PDH_Action varchar(10) NOT NULL,
   PDH_CreatedByUserId_Fk nvarchar(450) NOT NULL,
   PDH_CreatedByName nvarchar(512) NOT NULL,
   PDH_CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_PDH_CreatedAt DEFAULT SYSUTCDATETIME(),
   PDH_Is_deleted bit NOT NULL CONSTRAINT DF_PDH_Is_deleted DEFAULT(0),
   PDH_BeforeJson nvarchar(max) NULL,
   PDH_AfterJson nvarchar(max) NULL,
   CONSTRAINT PK_TM_PersonnelDirectoryHistory PRIMARY KEY CLUSTERED(PDH_Id),
   CONSTRAINT CK_TM_DirectoryAction CHECK(PDH_Action IN ('create','update','delete'))
  );
 END;

 -- Dynamic statements deliberately compile after any sp_rename/ALTER in this transaction.
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState',N'PDS_CreatedByUserId_Fk') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryState ADD PDS_CreatedByUserId_Fk nvarchar(450) NULL;');
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryState',N'PDS_CreatedAt') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryState ADD PDS_CreatedAt datetime2(7) NULL;');
 IF COL_LENGTH(N'dbo.TM_PersonnelDirectoryHistory',N'PDH_Is_deleted') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryHistory ADD PDH_Is_deleted bit NOT NULL CONSTRAINT DF_PDH_Is_deleted DEFAULT(0) WITH VALUES;');

 EXEC sys.sp_executesql N'
  UPDATE s SET
   PDS_CreatedByUserId_Fk=COALESCE(s.PDS_CreatedByUserId_Fk,h.PDH_CreatedByUserId_Fk,@actor),
   PDS_CreatedAt=COALESCE(s.PDS_CreatedAt,h.PDH_CreatedAt,@created)
  FROM dbo.TM_PersonnelDirectoryState s
  OUTER APPLY (SELECT TOP(1) PDH_CreatedByUserId_Fk,PDH_CreatedAt
   FROM dbo.TM_PersonnelDirectoryHistory h WHERE h.PDH_UserId_Fk=s.PDS_UserId_Fk ORDER BY h.PDH_Id) h
  WHERE s.PDS_CreatedByUserId_Fk IS NULL OR s.PDS_CreatedAt IS NULL;
  IF EXISTS(SELECT 1 FROM dbo.TM_PersonnelDirectoryState WHERE PDS_CreatedByUserId_Fk IS NULL OR PDS_CreatedAt IS NULL)
   THROW 51002,N''Legacy state has no history. Set @LegacyCreatedByUserId and @LegacyCreatedAt to verified values and rerun.'',1;
  IF EXISTS(SELECT 1 FROM dbo.TM_PersonnelDirectoryState s LEFT JOIN dbo.AspNetUsers u ON u.Id=s.PDS_CreatedByUserId_Fk WHERE u.Id IS NULL)
   OR EXISTS(SELECT 1 FROM dbo.TM_PersonnelDirectoryHistory h LEFT JOIN dbo.AspNetUsers u ON u.Id=h.PDH_CreatedByUserId_Fk WHERE u.Id IS NULL)
   OR EXISTS(SELECT 1 FROM dbo.TM_PersonnelDirectoryHistory h LEFT JOIN dbo.AspNetUsers u ON u.Id=h.PDH_UserId_Fk WHERE u.Id IS NULL)
   THROW 51003,N''A legacy user reference is missing from AspNetUsers. Resolve the original identity before migration; no data was deleted.'',1;
  ALTER TABLE dbo.TM_PersonnelDirectoryState ALTER COLUMN PDS_CreatedByUserId_Fk nvarchar(450) NOT NULL;
  ALTER TABLE dbo.TM_PersonnelDirectoryState ALTER COLUMN PDS_CreatedAt datetime2(7) NOT NULL;',
  N'@actor nvarchar(450), @created datetime2(7)',@actor=@LegacyCreatedByUserId,@created=@LegacyCreatedAt;

 IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.TM_PersonnelDirectoryState') AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'dbo.TM_PersonnelDirectoryState'),N'PDS_CreatedAt','ColumnId'))
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryState ADD CONSTRAINT DF_PDS_CreatedAt DEFAULT SYSUTCDATETIME() FOR PDS_CreatedAt;');
 IF OBJECT_ID(N'dbo.FK_PDS_CreatedByUser',N'F') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryState WITH CHECK ADD CONSTRAINT FK_PDS_CreatedByUser FOREIGN KEY(PDS_CreatedByUserId_Fk) REFERENCES dbo.AspNetUsers(Id);');
 IF OBJECT_ID(N'dbo.FK_PDH_User',N'F') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryHistory WITH CHECK ADD CONSTRAINT FK_PDH_User FOREIGN KEY(PDH_UserId_Fk) REFERENCES dbo.AspNetUsers(Id);');
 IF OBJECT_ID(N'dbo.FK_PDH_CreatedByUser',N'F') IS NULL
  EXEC(N'ALTER TABLE dbo.TM_PersonnelDirectoryHistory WITH CHECK ADD CONSTRAINT FK_PDH_CreatedByUser FOREIGN KEY(PDH_CreatedByUserId_Fk) REFERENCES dbo.AspNetUsers(Id);');

 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelDirectoryHistory') AND name IN(N'IX_TM_DirectoryHistory_User',N'IX_PDH_User_History'))
  EXEC(N'CREATE INDEX IX_PDH_User_History ON dbo.TM_PersonnelDirectoryHistory(PDH_UserId_Fk,PDH_Id DESC);');
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelDirectoryState') AND name=N'IX_PDS_CreatedByUser')
  EXEC(N'CREATE INDEX IX_PDS_CreatedByUser ON dbo.TM_PersonnelDirectoryState(PDS_CreatedByUserId_Fk);');
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TM_PersonnelDirectoryHistory') AND name=N'IX_PDH_CreatedByUser')
  EXEC(N'CREATE INDEX IX_PDH_CreatedByUser ON dbo.TM_PersonnelDirectoryHistory(PDH_CreatedByUserId_Fk);');

 DECLARE @Descriptions TABLE(TableName sysname,ColumnName sysname,Description nvarchar(1000));
 INSERT @Descriptions VALUES
 (N'TM_PersonnelDirectoryState',N'PDS_UserId_Fk',N'شناسه نیروی صاحب شماره داخلی؛ کلید خارجی به AspNetUsers و کلید اصلی وضعیت دفترچه'),
 (N'TM_PersonnelDirectoryState',N'PDS_Is_deleted',N'حذف منطقی از دفترچه داخلی‌ها؛ صفر موجود و یک حذف‌شده؛ حساب ورود حذف نمی‌شود'),
 (N'TM_PersonnelDirectoryState',N'PDS_Revision',N'شناسه نسخه وضعیت دفترچه برای تشخیص ویرایش همزمان'),
 (N'TM_PersonnelDirectoryState',N'PDS_CreatedByUserId_Fk',N'شناسه کاربر ثبت‌کننده رکورد وضعیت دفترچه؛ کلید خارجی به AspNetUsers'),
 (N'TM_PersonnelDirectoryState',N'PDS_CreatedAt',N'تاریخ و زمان ایجاد رکورد وضعیت دفترچه به UTC؛ مستقل از تاریخ ثبت حساب کاربر'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_Id',N'شناسه یکتای افزایشی رویداد تغییر دفترچه داخلی‌ها'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_UserId_Fk',N'شناسه نیروی موضوع تغییر؛ کلید خارجی به AspNetUsers'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_Action',N'نوع تغییر؛ create ثبت، update ویرایش و delete حذف منطقی'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_CreatedByUserId_Fk',N'شناسه کاربر انجام‌دهنده و ثبت‌کننده رویداد؛ کلید خارجی به AspNetUsers'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_CreatedByName',N'نام کاربر انجام‌دهنده در زمان رویداد؛ مستقل از تغییر نام بعدی حساب'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_CreatedAt',N'تاریخ و زمان ثبت رویداد تغییر به UTC'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_Is_deleted',N'پرچم حذف منطقی خود رویداد سابقه؛ پیش‌فرض صفر؛ حذف نیرو این پرچم را تغییر نمی‌دهد'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_BeforeJson',N'تصویر JSON مشخصات نیرو پیش از تغییر؛ در ثبت اولیه تهی است'),
 (N'TM_PersonnelDirectoryHistory',N'PDH_AfterJson',N'تصویر JSON مشخصات نیرو پس از تغییر؛ در حذف منطقی تهی است');
 DECLARE @table sysname,@column sysname,@description nvarchar(1000);
 DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,ColumnName,Description FROM @Descriptions;
 OPEN descriptions;
 FETCH NEXT FROM descriptions INTO @table,@column,@description;
 WHILE @@FETCH_STATUS=0
 BEGIN
  IF EXISTS(SELECT 1 FROM sys.extended_properties p JOIN sys.columns c ON c.object_id=p.major_id AND c.column_id=p.minor_id
    WHERE p.class=1 AND p.name=N'MS_Description' AND p.major_id=OBJECT_ID(N'dbo.'+@table) AND c.name=@column)
   EXEC sys.sp_updateextendedproperty @name=N'MS_Description',@value=@description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@table,@level2type=N'COLUMN',@level2name=@column;
  ELSE
   EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=@description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@table,@level2type=N'COLUMN',@level2name=@column;
  FETCH NEXT FROM descriptions INTO @table,@column,@description;
 END;
 CLOSE descriptions;
 DEALLOCATE descriptions;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
GO
