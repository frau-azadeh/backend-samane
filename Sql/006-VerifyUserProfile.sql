-- Read only. Set @UserId to the actual AspNetUsers.Id (not UserName).
USE [Hamiran_Price];
GO
DECLARE @UserId nvarchar(450) = N'REPLACE_WITH_REAL_AspNetUsers_Id';
SELECT u.Id,u.UserName,u.FirstName,u.LastName,u.IsActive,
       pe.PE_ID,pe.PE_PartyID,pe.PE_DepID,doe.DOE_ID,doe.DOE_JobID,
       j.Job_ID,j.Job_Title,j.Job_Full_Name,j.Office_Id_FK,j.Part_Id_Fk,j.Parent_Id_FK
FROM dbo.AspNetUsers u
LEFT JOIN dbo.TM_Party_Employee pe ON pe.PE_UserID=u.Id
LEFT JOIN dbo.TM_DepartmentOfEmployee doe ON doe.DOE_PartyID=pe.PE_PartyID
LEFT JOIN dbo.TM_AllJobs j ON j.Job_ID=doe.DOE_JobID
WHERE u.Id=@UserId;
SELECT CASE WHEN OBJECT_ID(N'dbo.AspNetRoles',N'U') IS NULL THEN N'No Identity roles table; configure explicit authorized user IDs in appsettings.' ELSE N'Identity roles table exists' END AS RoleSchema;
-- Inspect ambiguous assignments: no current-job marker was supplied in this schema.
SELECT pe.PE_UserID,COUNT(DISTINCT doe.DOE_JobID) AS DifferentJobCount
FROM dbo.TM_Party_Employee pe
JOIN dbo.TM_DepartmentOfEmployee doe ON doe.DOE_PartyID=pe.PE_PartyID
GROUP BY pe.PE_UserID
HAVING COUNT(DISTINCT doe.DOE_JobID)>1;
GO
