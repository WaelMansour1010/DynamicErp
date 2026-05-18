/*
    POS / Shared HR employee photo support for printable employee and medical insurance cards.
    SQL Server 2012 compatible.
*/

IF COL_LENGTH(N'dbo.TblEmployee', N'EmployeePhotoDataUrl') IS NULL
BEGIN
    ALTER TABLE dbo.TblEmployee ADD EmployeePhotoDataUrl NVARCHAR(MAX) NULL;
END
GO
