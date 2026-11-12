-- Read-only verification. Run after 005-PersonnelDirectory.sql on the target database.
USE [Hamiran_Price];
GO
SET NOCOUNT ON;
DECLARE @Expected TABLE(TableName sysname,ColumnName sysname,TypeName sysname,MaxBytes int,Nullable bit);
INSERT @Expected VALUES
(N'TM_PersonnelDirectoryState',N'PDS_UserId_Fk',N'nvarchar',900,0),
(N'TM_PersonnelDirectoryState',N'PDS_Is_deleted',N'bit',1,0),
(N'TM_PersonnelDirectoryState',N'PDS_Revision',N'uniqueidentifier',16,0),
(N'TM_PersonnelDirectoryState',N'PDS_CreatedByUserId_Fk',N'nvarchar',900,0),
(N'TM_PersonnelDirectoryState',N'PDS_CreatedAt',N'datetime2',8,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_Id',N'bigint',8,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_UserId_Fk',N'nvarchar',900,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_Action',N'varchar',10,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_CreatedByUserId_Fk',N'nvarchar',900,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_CreatedByName',N'nvarchar',1024,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_CreatedAt',N'datetime2',8,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_Is_deleted',N'bit',1,0),
(N'TM_PersonnelDirectoryHistory',N'PDH_BeforeJson',N'nvarchar',-1,1),
(N'TM_PersonnelDirectoryHistory',N'PDH_AfterJson',N'nvarchar',-1,1);

DECLARE @Problems TABLE(TableName sysname,ColumnName sysname,Problem nvarchar(200));
INSERT @Problems
SELECT e.TableName,e.ColumnName,N'Missing column or incorrect type/length/nullability/scale'
FROM @Expected e
LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName
LEFT JOIN sys.types t ON t.user_type_id=c.user_type_id
WHERE c.column_id IS NULL OR t.name<>e.TypeName OR c.max_length<>e.MaxBytes OR c.is_nullable<>e.Nullable OR (e.TypeName=N'datetime2' AND c.scale<>7);
INSERT @Problems
SELECT e.TableName,e.ColumnName,N'Missing MS_Description'
FROM @Expected e
WHERE NOT EXISTS(SELECT 1 FROM sys.extended_properties p JOIN sys.columns c ON c.object_id=p.major_id AND c.column_id=p.minor_id
 WHERE p.class=1 AND p.name=N'MS_Description' AND p.major_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName AND LEN(CONVERT(nvarchar(1000),p.value))>0);
INSERT @Problems
SELECT e.TableName,e.ColumnName,N'Missing or untrusted/disabled foreign key to AspNetUsers.Id'
FROM @Expected e WHERE e.ColumnName LIKE N'%[_]Fk'
AND NOT EXISTS(SELECT 1 FROM sys.foreign_key_columns f JOIN sys.foreign_keys k ON k.object_id=f.constraint_object_id
 JOIN sys.columns c ON c.object_id=f.parent_object_id AND c.column_id=f.parent_column_id
 JOIN sys.columns r ON r.object_id=f.referenced_object_id AND r.column_id=f.referenced_column_id
 WHERE f.parent_object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName
 AND f.referenced_object_id=OBJECT_ID(N'dbo.AspNetUsers') AND r.name=N'Id' AND k.is_disabled=0 AND k.is_not_trusted=0);
INSERT @Problems
SELECT e.TableName,e.ColumnName,N'Missing default for timestamp/soft delete/revision'
FROM @Expected e WHERE (e.ColumnName LIKE N'%[_]CreatedAt' OR e.ColumnName LIKE N'%[_]Is[_]deleted' OR e.ColumnName=N'PDS_Revision')
AND NOT EXISTS(SELECT 1 FROM sys.default_constraints d JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
 WHERE d.parent_object_id=OBJECT_ID(N'dbo.'+e.TableName) AND c.name=e.ColumnName);
SELECT TableName,ColumnName,Problem FROM @Problems ORDER BY TableName,ColumnName;
IF EXISTS(SELECT 1 FROM @Problems)
 THROW 51010,N'Personnel directory schema verification failed. Inspect the preceding result set.',1;
SELECT N'Personnel directory schema checks passed' AS Result;
GO
