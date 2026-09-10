-- Script equivalente a la migración InitialCreate.
-- Úselo en SSMS/Azure Data Studio si no puede ejecutar `dotnet ef database update`.

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SoftwareArchitectureDb')
BEGIN
    CREATE DATABASE SoftwareArchitectureDb;
END
GO

USE SoftwareArchitectureDb;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        Nombre NVARCHAR(MAX) NOT NULL,
        Correo NVARCHAR(MAX) NOT NULL,
        Telefono NVARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO
