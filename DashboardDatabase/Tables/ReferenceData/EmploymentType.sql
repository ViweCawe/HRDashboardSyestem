CREATE TABLE [hr].[EmploymentType]
(
    [EmploymentTypeId] int IDENTITY(1,1) NOT NULL,
    [Code] nvarchar(30) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_EmploymentType] PRIMARY KEY ([EmploymentTypeId]),
    CONSTRAINT [UQ_EmploymentType_0] UNIQUE ([Code])
);
