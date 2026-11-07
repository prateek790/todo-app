SELECT name AS DatabaseName
FROM sys.databases
WHERE database_id > 4;

SELECT DB_ID(N'TodoAppDb') AS TodoAppDbId;


USE [TodoAppDb];
GO

SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name = N'Users';
GO

SELECT TOP (100) *FROM dbo.Users;

update dbo.Users
set isActive = 0
where id = 4;
